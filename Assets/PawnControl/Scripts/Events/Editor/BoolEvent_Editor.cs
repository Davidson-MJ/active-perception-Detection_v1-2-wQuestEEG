using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Passer {

    public class BoolEvent_Editor : Event_Editor {

        public static void EventInspector(
            SerializedProperty eventSourceProp, BoolEventHandlers eventSource,
            ref int selectedEventSourceIx, ref int selectedEventIx) {

            //UnityEventBase unityEventBase = eventSource.events[selectedEventIx].GetUnityEventBase();

            EventInspector(eventSourceProp, ref selectedEventSourceIx, ref selectedEventIx, 
                BoolMethodCheck, InitBoolEvent);
        }

        protected static void InitBoolEvent(SerializedProperty eventProp) {
        }


        protected static bool BoolMethodCheck(MethodInfo method, out string label) {
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 0 && method.ReturnType == typeof(void)) {
                label = method.Name + " ()";
                return true;
            }
            else if (parameters.Length == 1 && (
                parameters[0].ParameterType == typeof(bool)
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
                "OnStart: when the boolean becomes true\n" +
                "OnEnd: when the boolean becomes false\n" +
                "WhileActive: while the bool is true\n" +
                "WhileInactive: while the bool is false\n" +
                "OnChange: when the bool value changes\n" +
                "Continuous: the function is called for every frame"
                );
            SerializedProperty eventTypeProp = eventProp.FindPropertyRelative("eventType");
            eventTypeProp.intValue = (int)(EventHandler.Type)EditorGUILayout.EnumPopup(text, (EventHandler.Type)eventTypeProp.intValue);
            return eventTypeProp;
        }

    }

}