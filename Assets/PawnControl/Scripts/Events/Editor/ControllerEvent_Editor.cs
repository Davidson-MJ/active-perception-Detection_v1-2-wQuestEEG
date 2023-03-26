using UnityEditor;
using UnityEngine.Events;

namespace Passer {

    public class ControllerEvent_Editor : FloatEvent_Editor {

        public static void EventInspector(
            SerializedProperty eventHandlerProp,
            ref int selectedEventIx, ref int selectedEventHandlerIx) {

            EventInspector(eventHandlerProp, ref selectedEventIx, ref selectedEventHandlerIx,
                EventMethodCheck, InitControllerEvent);

            SetParameterOnEvents(eventHandlerProp);
        }

        public static void SetParameterOnEvents(SerializedProperty eventHandlersProp) {
            SerializedProperty defaultParameterPropertyProp = eventHandlersProp.FindPropertyRelative("defaultParameterProperty");
            string defaultParameterProperty = defaultParameterPropertyProp.stringValue;

            SerializedProperty eventsProp = eventHandlersProp.FindPropertyRelative("events");
            int eventCount = eventsProp.arraySize;
            for (int i = 0; i < eventCount; i++) {
                SerializedProperty eventHandlerProp = eventsProp.GetArrayElementAtIndex(i);
                SetParameterOnEvent(eventHandlerProp, defaultParameterProperty);
            }
        }

        protected static void SetParameterOnEvent(SerializedProperty eventHandlerProp, string defaultParameterProperty) {

            SerializedProperty parametersProp = eventHandlerProp.FindPropertyRelative("functionCall.parameters");
            int parameterCount = parametersProp.arraySize;
            if (parameterCount != 1)
                return; // no support for more than 1 parameter yet, 0 parameters: nothing to do

            SerializedProperty parameterProp = parametersProp.GetArrayElementAtIndex(0);
            SerializedProperty parameterTypeProp = parameterProp.FindPropertyRelative("type");
            SerializedProperty propertyNameProp = parameterProp.FindPropertyRelative("localProperty");
            if (propertyNameProp.stringValue == "") {
                switch ((FunctionCall.ParameterType)parameterTypeProp.intValue) {
                    case FunctionCall.ParameterType.Float:
                        propertyNameProp.stringValue = defaultParameterProperty;
                        break;
                }
            }
        }

        public static void InitControllerEvent(SerializedProperty eventProp) {
            SerializedProperty functionParametersProp = eventProp.FindPropertyRelative("functionCall.parameters");
            if (functionParametersProp.arraySize == 0)
                functionParametersProp.InsertArrayElementAtIndex(0);
            SerializedProperty parameter0Prop = functionParametersProp.GetArrayElementAtIndex(0);
            parameter0Prop.FindPropertyRelative("fromEvent").boolValue = true;
            parameter0Prop.FindPropertyRelative("localProperty").stringValue = "From Event";
            InitFloatEvent(eventProp);
        }

    }
}
