using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Passer.Humanoid {

    public class PoseEvent_Editor : Event_Editor {

        public static void EventInspector(
            SerializedProperty eventSourceProp, PoseEventList eventSource,
            ref int selectedEventSourceIx, ref int selectedEventIx) {

            //UnityEventBase unityEventBase = eventSource.events[selectedEventIx].GetUnityEventBase();

            EventInspector(eventSourceProp, ref selectedEventSourceIx, ref selectedEventIx, 
                PoseMethodCheck, InitPoseEvent);
        }

        protected static void InitPoseEvent(SerializedProperty eventProp) {
        }

        protected static bool PoseMethodCheck(MethodInfo method, out string label) {
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 0 && method.ReturnType == typeof(void)) {
                label = method.Name + " ()";
                return true;
            }
            else if (parameters.Length == 1 && (
                parameters[0].ParameterType == typeof(bool) ||
                parameters[0].ParameterType == typeof(Pose)
                )) {

                label = method.Name + " (" + parameters[0].ParameterType.Name + ")";
                return true;
            }

            label = "";
            return false;
        }

        public static void DetailsInspector(SerializedProperty eventProp, string label) {
            if (eventProp == null)
                return;

            DetailsTypeInspector(eventProp);

            PoseDetailsInspector(eventProp, label);
        }

        protected static SerializedProperty DetailsTypeInspector(SerializedProperty eventProp) {
            GUIContent text = new GUIContent(
                "Event Type",
                "Never: the function is never called\n" +
                "OnStart: when the Pose becomes non-null\n" +
                "OnEnd: when the Pose becomes null\n" +
                "WhileActive: while Pose is non-null\n" +
                "WhileInactive: while Pose is null\n" +
                "OnChange: when the Pose changes\n" +
                "Continuous: the function is called for every frame"
                );
            SerializedProperty eventTypeProp = eventProp.FindPropertyRelative("eventType");
            eventTypeProp.intValue = (int)(EventHandler.Type)EditorGUILayout.EnumPopup(text, (EventHandler.Type)eventTypeProp.intValue);
            return eventTypeProp;
        }

        protected static void PoseDetailsInspector(SerializedProperty eventProp, string label) {
            SerializedProperty poseEventProp = eventProp.FindPropertyRelative("poseEvent");
            EditorGUILayout.PropertyField(poseEventProp, new GUIContent("Pose"));
        }
    }

}