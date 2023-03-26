using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Passer {

    public class Vector3Event_Editor : Event_Editor {

        public static void EventInspector(
            SerializedProperty eventSourceProp, Vector3EventList eventSource,
            ref int selectedEventSourceIx, ref int selectedEventIx) {

            EventInspector(eventSourceProp, ref selectedEventSourceIx, ref selectedEventIx,
                Vector3MethodCheck, InitVector3Event);
        }

        protected static void InitVector3Event(SerializedProperty eventProp) {
        }

        protected static bool Vector3MethodCheck(MethodInfo method, out string label) {
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 0 && method.ReturnType == typeof(void)) {
                label = method.Name + " ()";
                return true;
            }
            else if (parameters.Length == 1 && (
                parameters[0].ParameterType == typeof(Vector3)
                )) {

                label = method.Name + " (" + parameters[0].ParameterType.Name + ")";
                return true;
            }

            label = "";
            return false;
        }


        protected static SerializedProperty DetailsTypeInspector(SerializedProperty eventProp) {
            GUIContent text = new GUIContent(
                "Event Type",
                "Never: the function is never called\n" +
                "OnStart: when the Vector3 becomes non zero\n" +
                "OnEnd: when the Vector becomes zero\n" +
                "WhileActive: while the Vector3 is non zero\n" +
                "WhileInactive: while the Vector3 is zero\n" +
                "OnChange: when the Vector3 changes\n" +
                "Continuous: the function is called for every frame"
                );
            SerializedProperty eventTypeProp = eventProp.FindPropertyRelative("eventType");
            eventTypeProp.intValue = (int)(EventHandler.Type)EditorGUILayout.EnumPopup(text, (EventHandler.Type)eventTypeProp.intValue);
            return eventTypeProp;
        }
    }

}