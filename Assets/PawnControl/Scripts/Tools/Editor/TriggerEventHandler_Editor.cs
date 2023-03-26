using UnityEditor;
using UnityEngine;

namespace Passer.Humanoid {

    [CustomEditor(typeof(TriggerEventHandler))]
    public class TriggerEventHandler_Editor : Editor {
        protected TriggerEventHandler eventHandler;

        #region Enable
        protected virtual void OnEnable() {
            eventHandler = (TriggerEventHandler)target;

            InitEvents();
            InitControllerInput();
        }
        #endregion

        #region Disable
        protected virtual void OnDisable() {
            CleanupEvents();
        }
        #endregion

        #region Inspector
        public override void OnInspectorGUI() {
            serializedObject.Update();

            EventsInspector();

            ControllerInputInspector(serializedObject.FindProperty("inputEvents"));

            serializedObject.ApplyModifiedProperties();
        }

        #region Events

        protected GameObjectEventHandlers[] eventLists;
        protected SerializedProperty[] eventListProps;
        protected SerializedProperty eventSourcesProp;
        protected SerializedProperty triggerEventProp;

        protected void InitEvents() {
            eventLists = new GameObjectEventHandlers[] {
                eventHandler.triggerHandlers,
            };

            eventListProps = new SerializedProperty[] {
                serializedObject.FindProperty("triggerEvents"),
                serializedObject.FindProperty("clickEvents"),
            };

            //eventHandler.eventSources = new GameObjectEventList[] {
            //    eventHandler.triggerEvents,
            //};
            //eventSourcesProp = serializedObject.FindProperty("eventSources");
            triggerEventProp = serializedObject.FindProperty("triggerHandlers");
        }

        protected int selectedEvent = -1;

        protected void EventsInspector() {
            //selectedEvent = GameObjectEvent_Editor.GameObjectEventsInspector(eventLists, eventListProps, selectedEvent);
            GameObjectEvent_Editor.EventInspector(triggerEventProp, eventHandler.triggerHandlers, ref selectedEvent, ref selectedSub);
        }

        protected void CleanupEvents() {
            foreach (GameObjectEventHandlers eventList in eventLists)
                GameObjectEvent_Editor.Cleanup(eventList);
        }
        #endregion

        #region Controller Input
        protected SerializedProperty leftEventsProp;
        protected SerializedProperty rightEventsProp;

        protected void InitControllerInput() {
            leftEventsProp = serializedObject.FindProperty("leftInputEvents");
            rightEventsProp = serializedObject.FindProperty("rightInputEvents");
        }

        protected bool showControllerInput = false;
        protected bool showControllerLeft = true;
        protected bool showControllerRight = true;

        protected GameControllers viewControllerType;

        protected int selectedLeft = -1;
        protected int selectedRight = -1;
        protected int selectedSub = -1;

        private void ControllerInputInspector(SerializedProperty inputObj) {
            showControllerInput = EditorGUILayout.Foldout(showControllerInput, "Controller Input", true);
            if (showControllerInput) {
                EditorGUI.indentLevel++;
                viewControllerType = (GameControllers)EditorGUILayout.EnumPopup(viewControllerType);

                showControllerLeft = EditorGUILayout.Foldout(showControllerLeft, "Left", true);
                if (showControllerLeft) {
                    for (int i = 0; i < leftEventsProp.arraySize; i++) {
                        ControllerEvent_Editor.EventInspector(
                            leftEventsProp.GetArrayElementAtIndex(i), /*eventHandler.leftInputEvents[i],*/
                            ref selectedLeft, ref selectedSub
                            );
                    }
                }

                showControllerRight = EditorGUILayout.Foldout(showControllerRight, "Right", true);
                if (showControllerRight) {
                    for (int i = 0; i < rightEventsProp.arraySize; i++) {
                        ControllerEvent_Editor.EventInspector(
                            rightEventsProp.GetArrayElementAtIndex(i), /*eventHandler.rightInputEvents[i],*/
                            ref selectedRight, ref selectedSub
                            );
                    }
                }
                EditorGUI.indentLevel--;
            }
        }
        #endregion

        #endregion
    }
}