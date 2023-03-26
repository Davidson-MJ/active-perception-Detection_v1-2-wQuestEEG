using UnityEngine.Events;
using UnityEditor;

namespace Passer {

    public class IntEvent_Editor : Event_Editor {

        public static void EventInspector(
            SerializedProperty eventSourceProp, IntEventHandlers eventSource,
            ref int selectedEventSourceIx, ref int selectedEventIx) {

            //UnityEventBase unityEventBase = (selectedEventSourceIx == eventSource.id) ?
            //    eventSource.events[selectedEventIx].GetUnityEventBase() : null;

            EventInspector(eventSourceProp, ref selectedEventSourceIx, ref selectedEventIx,
                EventMethodCheck, InitIntEvent);
        }

        protected static void InitIntEvent(SerializedProperty eventProp) {
            eventProp.FindPropertyRelative("intTriggerLow").intValue = 0;
            eventProp.FindPropertyRelative("intTriggerHigh").intValue = 1;
            eventProp.FindPropertyRelative("multiplicationFactor").intValue = 1;
        }

    }

}