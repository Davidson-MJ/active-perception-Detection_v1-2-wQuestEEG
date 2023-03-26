using UnityEngine;
using UnityEditor;

namespace Passer {

    [CustomEditor(typeof(Counter), true)]
    public class Counter_Editor : Editor {

        public override void OnInspectorGUI() {
            serializedObject.Update();

            SerializedProperty valueProp = serializedObject.FindProperty("_value");
            valueProp.intValue = EditorGUILayout.IntField("Value", valueProp.intValue);

            SerializedProperty minProp = serializedObject.FindProperty("min");
            minProp.intValue = EditorGUILayout.IntField("Minimum", minProp.intValue);

            SerializedProperty maxProp = serializedObject.FindProperty("max");
            maxProp.intValue = EditorGUILayout.IntField("Maximum", maxProp.intValue);

            // START: need to use SerializedProperties!
            Counter counter = (Counter)target;
            EventsInspector(counter);

            if (Application.isPlaying) {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Decrement"))
                    counter.Decrement();
                if (GUILayout.Button("Increment"))
                    counter.Increment();

                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }

        #region Events

        protected bool showEvents;
        protected int selectedEventSource = -1;
        protected int selectedEvent;

        protected virtual void EventsInspector(Counter counter) {
            showEvents = EditorGUILayout.Foldout(showEvents, "Events", true);
            if (showEvents) {
                EditorGUI.indentLevel++;

                SerializedProperty counterEventProp = serializedObject.FindProperty("counterEvent");
                IntEvent_Editor.EventInspector(counterEventProp, counter.counterEvent, ref selectedEventSource, ref selectedEvent);
            }
        }

        #endregion
    }
}