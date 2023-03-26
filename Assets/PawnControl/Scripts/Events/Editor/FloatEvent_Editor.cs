using UnityEditor;


namespace Passer {

    public class FloatEvent_Editor : Event_Editor {

        public static void EventInspector(
            SerializedProperty eventSourceProp, FloatEventHandlers eventSource,
            ref int selectedEventSourceIx, ref int selectedEventIx) {

            EventInspector(eventSourceProp, ref selectedEventSourceIx, ref selectedEventIx,
                EventMethodCheck, InitFloatEvent);

        }

        protected static void InitFloatEvent(SerializedProperty eventProp) {
            eventProp.FindPropertyRelative("floatTriggerLow").floatValue = 0.01F;
            eventProp.FindPropertyRelative("floatTriggerHigh").floatValue = 0.99F;
            eventProp.FindPropertyRelative("multiplicationFactor").floatValue = 1;

            eventProp.FindPropertyRelative("intTriggerLow").intValue = 0;
            eventProp.FindPropertyRelative("intTriggerHigh").intValue = 1;
        }

    }

}