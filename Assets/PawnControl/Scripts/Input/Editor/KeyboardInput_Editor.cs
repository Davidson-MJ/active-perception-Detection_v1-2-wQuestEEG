using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Passer {

    [CustomEditor(typeof(KeyboardInput))]
    public class KeyboardInput_Editor : Editor {

        #region Disable

        protected virtual void OnDisable() {
            KeyboardInput keyboardInput = (KeyboardInput)target;
            Cleanup(keyboardInput.keyboardHandlers);
        }

        protected static void Cleanup(List<KeyboardEventHandlers> eventHandlers) {
            foreach (KeyboardEventHandlers inputEventList in eventHandlers) {
                inputEventList.events.RemoveAll(triggerEvent => triggerEvent.isDead);
            }
        }

        #endregion

        #region Inspector

        protected int selectedKey = -1;
        protected int selectedSub = -1;

        public override void OnInspectorGUI() {
            serializedObject.Update();

            SerializedProperty handlersProp = serializedObject.FindProperty("keyboardHandlers");
            for (int i = 0; i < handlersProp.arraySize; i++) {
                SerializedProperty handlerProp = handlersProp.GetArrayElementAtIndex(i);
                KeyEventHandlers(handlerProp);
            }
            CleanKeyboardHandlers(handlersProp);

            EditorGUILayout.BeginHorizontal();
            KeyCode keyCode = (KeyCode)EditorGUILayout.EnumPopup(KeyCode.None, GUILayout.Width(140)); ;
            if (keyCode != KeyCode.None)
                AddKeyboardInput(handlersProp, keyCode);
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        private void KeyEventHandlers(SerializedProperty eventHandlerProp) {
            Event_Editor.EventInspector(eventHandlerProp,
                ref selectedKey, ref selectedSub,
                Event_Editor.EventMethodCheck,
                ControllerEvent_Editor.InitControllerEvent,
                LabelField
                );

            ControllerEvent_Editor.SetParameterOnEvents(eventHandlerProp);
        }

        protected static void LabelField(SerializedProperty eventHandlerProp) {
            SerializedProperty handlerKeyProp = eventHandlerProp.FindPropertyRelative("keyCode");
            handlerKeyProp.intValue = (int)(KeyCode)EditorGUILayout.EnumPopup((KeyCode)handlerKeyProp.intValue, GUILayout.Width(140));
        }


        private void AddKeyboardInput(SerializedProperty handlersProp, KeyCode keyCode) {
            int handlerIndex = handlersProp.arraySize;
            handlersProp.InsertArrayElementAtIndex(handlerIndex);
            SerializedProperty newHandlerProp = handlersProp.GetArrayElementAtIndex(handlerIndex);

            SerializedProperty handlerIdProp = newHandlerProp.FindPropertyRelative("id");
            handlerIdProp.intValue = handlerIndex;

            SerializedProperty eventTypeLabelsProp = newHandlerProp.FindPropertyRelative("eventTypeLabels");
            for (int i = 0; i < KeyboardInput.eventTypeLabels.Length; i++) {
                eventTypeLabelsProp.InsertArrayElementAtIndex(i);
                SerializedProperty eventTypeLabelProp = eventTypeLabelsProp.GetArrayElementAtIndex(i);
                eventTypeLabelProp.stringValue = KeyboardInput.eventTypeLabels[i];
            }

            SerializedProperty keyCodeProp = newHandlerProp.FindPropertyRelative("keyCode");
            keyCodeProp.intValue = (int)keyCode;

            // Remove all events becuase the new array element is a copy of the last element...
            SerializedProperty eventsProp = newHandlerProp.FindPropertyRelative("events");
            eventsProp.arraySize = 0;
        }


        private void CleanKeyboardHandlers(SerializedProperty handlersProp) {
            int i;
            do {
                i = FindKeyboardHandler(handlersProp, KeyCode.None);
                if (i >= 0)
                    handlersProp.DeleteArrayElementAtIndex(i);
            } while (i >= 0);
        }

        /// <summary>
        /// Find a keyboardHandler for a specific key
        /// </summary>
        /// <param name="handlersProp">The keyboard handlers List property</param>
        /// <param name="code">The KeyCode of the key</param>
        /// <returns>Index of the first keyboardhandler in the list for the given ket.
        /// This is -1 when no keyboardHandler could be found.</returns>
        protected int FindKeyboardHandler(SerializedProperty handlersProp, KeyCode code) {
            for (int i = 0; i < handlersProp.arraySize; i++) {
                SerializedProperty handlerProp = handlersProp.GetArrayElementAtIndex(i);
                SerializedProperty keyCodeProp = handlerProp.FindPropertyRelative("keyCode");
                if ((KeyCode)keyCodeProp.intValue == code)
                    return i;
            }
            return -1;
        }

        #endregion

    }
}