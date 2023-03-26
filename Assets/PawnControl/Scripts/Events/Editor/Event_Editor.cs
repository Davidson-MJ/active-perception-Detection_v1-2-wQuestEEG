using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Passer {

    public class Event_Editor {

        public static bool showParameterSettings = false;

        public delegate bool MethodCheck(MethodInfo method, out string label);
        public delegate void InitEvent(SerializedProperty eventProp);
        public delegate void LabelField(SerializedProperty eventHandlerProp);

        public static void EventInspector(
            SerializedProperty eventHandlerProp,
            ref int selectedEventSourceIx, ref int selectedEventIx,
            MethodCheck methodCheck,
            InitEvent InitEvent,
            LabelField LabelField = null
            ) {

            if (!Application.isPlaying)
                CheckEmptySlot(eventHandlerProp, InitEvent);

            SerializedProperty eventsProp = eventHandlerProp.FindPropertyRelative("events");
            int eventCount = eventsProp.arraySize;

            string[] eventTypeLabels = GetEventTypeLabels(eventHandlerProp);

            SerializedProperty eventIdProp = eventHandlerProp.FindPropertyRelative("id");
            bool selected = eventIdProp.intValue == selectedEventSourceIx;

            for (int i = 0; i < eventCount; i++) {
                EditorGUILayout.BeginHorizontal();
                if (i == 0) {
                    if (LabelField == null) {
                        SerializedProperty eventLabel = eventHandlerProp.FindPropertyRelative("label");
                        EditorGUILayout.LabelField(eventLabel.stringValue, GUILayout.Width(140));
                    }
                    else
                        LabelField(eventHandlerProp);
                }
                else
                    EditorGUILayout.LabelField(" ", GUILayout.Width(140));

                SerializedProperty selectedButton = eventsProp.GetArrayElementAtIndex(i);

                if (selected && selectedEventIx == i) {
                    EventDetails(eventHandlerProp, selectedButton, methodCheck, eventTypeLabels, InitEvent);
                }
                else {
                    string label = GetButtonLabel(selectedButton, eventTypeLabels);
                    if (GUILayout.Button(label)) {
                        selectedEventSourceIx = eventIdProp.intValue;
                        selectedEventIx = i;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            if (eventCount > 1)
                EditorGUILayout.Space();
        }

        protected static void CheckEmptySlot(
            SerializedProperty eventHandlerProp,
            InitEvent InitEvent) {

            SerializedProperty eventsProp = eventHandlerProp.FindPropertyRelative("events");
            int eventCount = eventsProp.arraySize;

            if (eventCount == 0) {
                eventsProp.InsertArrayElementAtIndex(0);
                SerializedProperty eventProp = eventsProp.GetArrayElementAtIndex(0);
                InitEvent(eventProp);
                SerializedProperty eventTypeProp = eventProp.FindPropertyRelative("eventType");
                eventTypeProp.intValue = 0;
                SerializedProperty functionCallProp = eventProp.FindPropertyRelative("functionCall");
                SerializedProperty eventTargetGameObjectProp = functionCallProp.FindPropertyRelative("targetGameObject");
                eventTargetGameObjectProp.objectReferenceValue = null;
            }
            else {
                SerializedProperty lastEventProp = eventsProp.GetArrayElementAtIndex(eventCount - 1);
                SerializedProperty lastEventTypeProp = lastEventProp.FindPropertyRelative("eventType");
                if (lastEventTypeProp.intValue != 0) {
                    eventsProp.InsertArrayElementAtIndex(eventCount);
                    SerializedProperty eventProp = eventsProp.GetArrayElementAtIndex(eventCount);
                    InitEvent(eventProp);
                    SerializedProperty eventTypeProp = eventProp.FindPropertyRelative("eventType");
                    eventTypeProp.intValue = 0;
                    SerializedProperty functionCallProp = eventProp.FindPropertyRelative("functionCall");
                    SerializedProperty eventTargetGameObjectProp = functionCallProp.FindPropertyRelative("targetGameObject");
                    eventTargetGameObjectProp.objectReferenceValue = null;
                }
            }
        }

        public static string GetButtonLabel(SerializedProperty eventProp, string[] eventTypeLabels) {
            SerializedProperty eventTypeProp = eventProp.FindPropertyRelative("eventType");
            EventHandler.Type eventType = (EventHandler.Type)eventTypeProp.intValue;
            //if (eventType == EventHandler.Type.Never)
            //    return "";
            string eventTypeLabel = eventTypeLabels[eventTypeProp.intValue];

            SerializedProperty functionCallProp = eventProp.FindPropertyRelative("functionCall");
            SerializedProperty eventTargetGameObjectProp = functionCallProp.FindPropertyRelative("targetGameObject");
            if (eventTargetGameObjectProp.objectReferenceValue == null)
                return "";

            string targetName = eventTargetGameObjectProp.objectReferenceValue.ToString();
            int braceIndex = targetName.IndexOf('(');
            if (braceIndex > 0)
                targetName = targetName.Substring(0, braceIndex - 1);

            string methodName = GetTargetMethodName(eventProp);
            methodName = methodName.Replace('/', '.');
            string label = eventTypeLabel + ": [" + targetName + "]" + methodName;

            SerializedProperty parametersProp = functionCallProp.FindPropertyRelative("parameters");
            if (parametersProp.arraySize == 0) {
                label += "()";
                return label;
            }

            SerializedProperty parameterProp = parametersProp.GetArrayElementAtIndex(0);
            SerializedProperty fromEventProp = parameterProp.FindPropertyRelative("fromEvent");
            if (fromEventProp.boolValue) {
                label += "(...)";
                return label;
            }

            SerializedProperty parameterTypeProp = parameterProp.FindPropertyRelative("type");
            switch ((FunctionCall.ParameterType)parameterTypeProp.intValue) {
                case FunctionCall.ParameterType.Void:
                    label += "()";
                    break;
                case FunctionCall.ParameterType.Float:
                    SerializedProperty floatValueProp = parameterProp.FindPropertyRelative("floatConstant");
                    label += "(" + floatValueProp.floatValue + ")";
                    break;
                case FunctionCall.ParameterType.Bool:
                    SerializedProperty boolValueProp = parameterProp.FindPropertyRelative("boolConstant");
                    label += "(" + boolValueProp.boolValue + ")";
                    break;
                case FunctionCall.ParameterType.Int:
                    SerializedProperty intValueProp = parameterProp.FindPropertyRelative("intConstant");
                    label += "(" + intValueProp.intValue + ")";
                    break;
                case FunctionCall.ParameterType.String:
                    SerializedProperty stringValueProp = parameterProp.FindPropertyRelative("stringConstant");
                    label += "(" + stringValueProp.stringValue + ")";
                    break;
                default:
                    label += "(...)";
                    break;
            }

            return label;
        }

        protected static string GetTargetMethodName(SerializedProperty eventProp) {
            SerializedProperty functionCallProp = eventProp.FindPropertyRelative("functionCall");
            SerializedProperty methodNameProp = functionCallProp.FindPropertyRelative("methodName");
            string methodName = methodNameProp.stringValue;
            if (methodName == "Execute") {
                // user Custom Script Name
                SerializedProperty eventTargetProp = functionCallProp.FindPropertyRelative("target");
                Object eventTarget = eventTargetProp.objectReferenceValue;
                if (eventTarget is Script) {
                    Script script = (Script)eventTarget;
                    return script.scriptName;
                }
            }
            int ix = methodName.LastIndexOf('.');
            if (ix > -1) {
                methodName = methodName.Substring(ix + 1);
            }
            return methodName;
        }

        #region Details

        protected static void EventDetails(
            SerializedProperty eventHandlerProp, SerializedProperty eventSourceProp,
            MethodCheck methodCheck, string[] eventTypeLabels, InitEvent InitEvent) {

            if (eventSourceProp == null)
                return;

            GUIStyle style = new GUIStyle(GUI.skin.box) {
                margin = new RectOffset(0, 0, 4, 10)
            };

            Rect rect = EditorGUILayout.BeginVertical(style);
            GUI.Box(rect, "", style);

            int indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 1;

            EventTypeInspector(eventSourceProp, eventTypeLabels);
            EventTargetInspector(eventHandlerProp, eventSourceProp, methodCheck, InitEvent);
#if hNW_UNET
            //EventNetworkingInspector(eventSourceProp);
#endif

            ParameterOptionsInspector(eventSourceProp);

            EditorGUI.indentLevel = indentLevel;
            EditorGUILayout.EndVertical();
        }

        private static string[] GetEventTypeLabels(SerializedProperty eventSourceProp) {
            SerializedProperty eventTypeLabelsProp = eventSourceProp.FindPropertyRelative("eventTypeLabels");
            string[] eventTypeLabels = new string[7];
            if (eventTypeLabelsProp.arraySize == 7) {
                for (int i = 0; i < eventTypeLabelsProp.arraySize; i++) {
                    SerializedProperty eventTypeLabelProp = eventTypeLabelsProp.GetArrayElementAtIndex(i);
                    eventTypeLabels[i] = eventTypeLabelProp.stringValue;
                }
            }
            else {
                for (int i = 0; i < 7; i++) {
                    eventTypeLabels[i] = ((EventHandler.Type)i).ToString();
                }
            }
            return eventTypeLabels;
        }

        public static SerializedProperty EventTypeInspector(SerializedProperty eventProp, string[] eventTypeLabels) {
            GUIContent text = new GUIContent(
                "Event Type",
                "Never: the function is never called\n" +
                "OnStart: when the button is pressed\n" +
                "OnEnd: when the button is released\n" +
                "WhileActive: while the button is pressed\n" +
                "WhileInactive: while the button is released\n" +
                "OnChange: when the button press changes\n" +
                "Continuous: the function is called for every frame"
                );
            SerializedProperty eventTypeProp = eventProp.FindPropertyRelative("eventType");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(text.text, GUILayout.Width(90));
            eventTypeProp.intValue = EditorGUILayout.Popup(eventTypeProp.intValue, eventTypeLabels);
            EditorGUILayout.EndHorizontal();
            return eventTypeProp;
        }

        #region Event Target

        protected static void EventTargetInspector(
            SerializedProperty eventHandlerProp, SerializedProperty eventProp,
            MethodCheck methodCheck, InitEvent InitEvent) {

            SerializedProperty functionCallProp = eventProp.FindPropertyRelative("functionCall");
            SerializedProperty functionParametersProp = eventProp.FindPropertyRelative("functionCall.parameters");
            if (functionParametersProp.arraySize == 0)
                InitEvent(eventProp);

            SerializedProperty eventTargetGameObjectProp = functionCallProp.FindPropertyRelative("targetGameObject");
            GUIContent text = new GUIContent(
                "Target",
                "The Object on which the method is called"
                );
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(text, GUILayout.Width(90));
            eventTargetGameObjectProp.objectReferenceValue = EditorGUILayout.ObjectField(eventTargetGameObjectProp.objectReferenceValue, typeof(GameObject), true);
            EditorGUILayout.EndHorizontal();

            // Component / Method
            string[] methodNames = GetMethodNames(eventTargetGameObjectProp, methodCheck);

            SerializedProperty methodNameProp = functionCallProp.FindPropertyRelative("methodName");

            int methodNameIndex = GetMethodIndex(methodNameProp.stringValue, methodNames);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Method", GUILayout.Width(90));
            methodNameIndex = EditorGUILayout.Popup(methodNameIndex, methodNames);
            EditorGUILayout.EndHorizontal();

            if (methodNameIndex >= 0 && methodNameIndex < methodNames.Length) {
                string fullMethodName = methodNames[methodNameIndex];

                SetMethod(functionCallProp, fullMethodName);

                SerializedProperty newFunctionCallProp = eventProp.FindPropertyRelative("functionCallFloat");
                if (newFunctionCallProp != null) {
                    SerializedProperty newEventTargetGameObjectProp = newFunctionCallProp.FindPropertyRelative("targetGameObject");

                    SerializedProperty newMethodNameProp = newFunctionCallProp.FindPropertyRelative("methodName");

                    newEventTargetGameObjectProp.objectReferenceValue = eventTargetGameObjectProp.objectReferenceValue;
                    newMethodNameProp.stringValue = methodNameProp.stringValue;
                }

                MethodParametersInspector(eventProp, eventHandlerProp);
            }
        }

        #endregion

        #region Method

        public static int GetMethodIndex(string methodName, string[] methodNames) {
            for (int i = 0; i < methodNames.Length; i++) {
                int brace1Pos = methodNames[i].LastIndexOf("(");
                if (methodName == methodNames[i].Substring(0, brace1Pos - 1))
                    return i;
            }
            return -1;
        }

        public static string[] GetMethodNames(SerializedProperty targetGameObjectProp, MethodCheck methodCheck) {
            GameObject callTargetObject = (GameObject)targetGameObjectProp.objectReferenceValue;
            if (callTargetObject == null)
                return new string[0];

            Component[] components = callTargetObject.GetComponents<Component>();

            List<string> nameList = new List<string>();
            AddMethodNames(ref nameList, callTargetObject, methodCheck);
            foreach (Component component in components) {
                AddMethodNames(ref nameList, component, methodCheck);
            }

            string[] names = new string[nameList.Count];
            for (int i = 0; i < nameList.Count; i++) {
                names[i] = nameList[i];
            }
            return names;
        }

        public static GameObject GetTargetGameObject(SerializedProperty callTargetProp) {
            Object callTarget = callTargetProp.objectReferenceValue;
            if (callTarget == null)
                return null;

            if (callTarget.GetType() == typeof(GameObject))
                return (GameObject)callTarget;
            else
                return ((Component)callTarget).gameObject;
        }

        protected static void AddMethodNames(ref List<string> names, Object component, MethodCheck methodCheck) {
            if (component == null)
                return;

            System.Type componentType = component.GetType();
            MethodInfo[] methods = componentType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            for (int i = 0; i < methods.Length; i++) {
                if (BlackListed(methods[i].Name))
                    continue;
                if (methods[i].ReturnType != typeof(void))
                    continue;

                if (componentType == typeof(Script) && component is Script) {
                    Script script = (Script)component;
                    names.Add("Script/" + script.scriptName + " ()");
                }
                else {
                    string methodLabel;
                    if (methodCheck(methods[i], out methodLabel)) {
                        names.Add(componentType.FullName + "/" + methodLabel);
                    }
                }
            }
#if pHUMANOID
            if (componentType == typeof(Humanoid.HumanoidControl)) {
                AddAnimatorParameterMethods(ref names, component);
            }
#endif

        }

        protected static void AddAnimatorParameterMethods(ref List<string> names, Object component) {
#if pHUMANOID
            Humanoid.HumanoidControl humanoid = component as Humanoid.HumanoidControl;
            if (humanoid == null || humanoid.targetsRig.runtimeAnimatorController == null)
                return;

            AnimatorControllerParameter[] animatorParameters = humanoid.targetsRig.parameters;
            for (int i = 0; i < animatorParameters.Length; i++) {
                string fullMethodName = "SetAnimatorParameter/" + animatorParameters[i].name;
                switch (animatorParameters[i].type) {
                    case AnimatorControllerParameterType.Bool:
                        fullMethodName += " (Boolean)";
                        break;
                    case AnimatorControllerParameterType.Float:
                        fullMethodName += " (Single)";
                        break;
                    case AnimatorControllerParameterType.Int:
                        fullMethodName += " (Int32)";
                        break;
                    case AnimatorControllerParameterType.Trigger:
                    default:
                        break;
                }
                names.Add(fullMethodName);
            }

#endif
        }

        protected static string[] blackList = {
            "GetComponentInChildren",
            "GetComponentsInChildren",
            "GetComponentsInParent",
            "SetSiblingIndex",
            "set_hasChanged",
            "set_active",
            "GetChild",
            "set_hierarchyCapacity",
            "set_useGUILayout",
            "set_runInEditMode",
            "CancelInvoke",
            "StopAllCoroutines",
            "Awake",
            "Start",
            "Update",
            "FixedUpdate",
            "OnApplicationQuit",
            "DetachChildren",
            "SetAsFirstSibling",
            "SetAsLastSibling",
            "IsInvoking",
            "StopCoroutine",
            "SetAnimationParameterTrigger" // is done via SetAnimationParamater construction
        };
        public static bool BlackListed(string methodName) {
            foreach (string blackListEntry in blackList) {
                if (methodName == blackListEntry)
                    return true;
            }
            return false;
        }


        public static void SetMethod(SerializedProperty functionCallProp, string fullMethodName) {
            int brace1Pos = fullMethodName.LastIndexOf("(");
            int brace2Pos = fullMethodName.LastIndexOf(")");
            string methodName = fullMethodName.Substring(0, brace1Pos - 1);

            SerializedProperty methodNameProp = functionCallProp.FindPropertyRelative("methodName");
            methodNameProp.stringValue = methodName;

            string parameterTypeName = fullMethodName.Substring(brace1Pos + 1, brace2Pos - brace1Pos - 1);
            SerializedProperty parametersProp = functionCallProp.FindPropertyRelative("parameters");
            SerializedProperty parameterProp;
            if (parametersProp.arraySize <= 0) {
                parametersProp.InsertArrayElementAtIndex(0);
                parameterProp = parametersProp.GetArrayElementAtIndex(0);
                InitParameter(parameterProp);
            }
            else {
                parameterProp = parametersProp.GetArrayElementAtIndex(0);
            }
            SerializedProperty parameterTypeProp = parameterProp.FindPropertyRelative("type");

            switch (parameterTypeName) {
                case "Single":
                    parameterTypeProp.intValue = (int)FunctionCall.ParameterType.Float;
                    break;
                case "Int32":
                    parameterTypeProp.intValue = (int)FunctionCall.ParameterType.Int;
                    break;
                case "Boolean":
                    parameterTypeProp.intValue = (int)FunctionCall.ParameterType.Bool;
                    break;
                case "String":
                    parameterTypeProp.intValue = (int)FunctionCall.ParameterType.String;
                    break;
                case "Vector3":
                    parameterTypeProp.intValue = (int)FunctionCall.ParameterType.Vector3;
                    break;
                case "GameObject":
                    parameterTypeProp.intValue = (int)FunctionCall.ParameterType.GameObject;
                    break;
                case "Rigidbody":
                    parameterTypeProp.intValue = (int)FunctionCall.ParameterType.Rigidbody;
                    break;
                case "Object":
                    parameterTypeProp.intValue = (int)FunctionCall.ParameterType.Void;
                    break;
                default:
                    parameterTypeProp.intValue = (int)FunctionCall.ParameterType.Void;
                    break;
            }
        }

        protected static void InitParameter(SerializedProperty parameterProp) {
            parameterProp.FindPropertyRelative("fromEvent").boolValue = true;
        }

        public static bool EventMethodCheck(MethodInfo method, out string label) {
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 0 && method.ReturnType == typeof(void)) {
                label = method.Name + " ()";
                return true;
            }
            else if (parameters.Length == 1 && (
                parameters[0].ParameterType == typeof(float) ||
                parameters[0].ParameterType == typeof(int) ||
                parameters[0].ParameterType == typeof(bool) ||
                parameters[0].ParameterType == typeof(string) ||
                parameters[0].ParameterType == typeof(Vector3) ||
                parameters[0].ParameterType == typeof(GameObject) ||
                parameters[0].ParameterType == typeof(Rigidbody) ||

                parameters[0].ParameterType.IsEnum
                )) {

                label = method.Name + " (" + parameters[0].ParameterType.Name + ")";
                return true;
            }
            else if (parameters.Length == 1 && parameters[0].ParameterType.IsSubclassOf(typeof(Object))) {
                label = method.Name + " (Object)";
                return true;
            }

            label = "";
            return false;
        }

        #endregion

        #region Parameters

        public static void MethodParametersInspector(SerializedProperty eventProp, SerializedProperty eventHandlerProp) {
            SerializedProperty functionCallProp = eventProp.FindPropertyRelative("functionCall");
            SerializedProperty parametersProp = functionCallProp.FindPropertyRelative("parameters");
            if (parametersProp.arraySize == 0)
                return;

            SerializedProperty parameterProp = parametersProp.GetArrayElementAtIndex(0);
            SerializedProperty parameterTypeProp = parameterProp.FindPropertyRelative("type");

            FunctionCall.ParameterType parameterType = (FunctionCall.ParameterType)parameterTypeProp.intValue;
            if (parameterType == FunctionCall.ParameterType.Void)
                return;

            System.Type parameterSystemType = FunctionCall.ToSystemType(parameterType);

            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Parameter", GUILayout.Width(120));
            int indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            FunctionCall_Editor.PropertyParameterInspector(parameterProp.serializedObject, parameterProp, parameterSystemType);

            EditorGUI.indentLevel = indentLevel;
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }

        #endregion

        protected static void EventNetworkingInspector(SerializedProperty eventProp) {
            GUIContent text = new GUIContent(
                "Networking",
                "When enabled, the event will be synchronized across the network"
                );
            SerializedProperty eventNetworkingProp = eventProp.FindPropertyRelative("eventNetworking");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(text, GUILayout.Width(90));
            eventNetworkingProp.boolValue = EditorGUILayout.Toggle(eventNetworkingProp.boolValue);
            EditorGUILayout.EndHorizontal();
        }

        public static void ParameterOptionsInspector(SerializedProperty eventSourceProp) {
            SerializedProperty functionCallProp = eventSourceProp.FindPropertyRelative("functionCall");
            SerializedProperty parametersProp = functionCallProp.FindPropertyRelative("parameters");
            if (parametersProp.arraySize == 0)
                return;

            SerializedProperty parameterProp = parametersProp.GetArrayElementAtIndex(0);
            SerializedProperty parameterTypeProp = parameterProp.FindPropertyRelative("type");
            FunctionCall.ParameterType parameterType = (FunctionCall.ParameterType)parameterTypeProp.intValue;

            if (parameterType == FunctionCall.ParameterType.Void)
                return;

            showParameterSettings = EditorGUILayout.Foldout(showParameterSettings, "Options", true);
            if (showParameterSettings) {
                //ParameterOptionsInspector(eventSourceProp);

                EditorGUI.indentLevel++;

                switch (parameterType) {
                    case FunctionCall.ParameterType.Float:
                        MultiplicationInspector(eventSourceProp);
                        TriggerLevelInspector(eventSourceProp);
                        break;
                    case FunctionCall.ParameterType.Int:
                        MultiplicationInspector(eventSourceProp);
                        break;
                    case FunctionCall.ParameterType.Bool:
                        InverseInspector(eventSourceProp);
                        break;
                }
                EditorGUI.indentLevel--;
            }
        }

        protected static SerializedProperty MultiplicationInspector(SerializedProperty eventProp) {
            GUIContent text = new GUIContent(
                "Multiplication",
                "Multiply the value before calling event trigger"
                );
            SerializedProperty multiplicationProp = eventProp.FindPropertyRelative("multiplicationFactor");
            multiplicationProp.floatValue = EditorGUILayout.FloatField(text, multiplicationProp.floatValue);
            return multiplicationProp;
        }

        protected static void TriggerLevelInspector(SerializedProperty eventProp) {
            SerializedProperty floatTriggerLowProp = eventProp.FindPropertyRelative("floatTriggerLow");
            SerializedProperty floatTriggerHighProp = eventProp.FindPropertyRelative("floatTriggerHigh");
            float floatTriggerLow = floatTriggerLowProp.floatValue;
            float floatTriggerHigh = floatTriggerHighProp.floatValue;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Trigger Level", GUILayout.MinWidth(115));
            int indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            floatTriggerLow = EditorGUILayout.FloatField(floatTriggerLow, GUILayout.Width(40));
            EditorGUILayout.MinMaxSlider(ref floatTriggerLow, ref floatTriggerHigh, -1, 1);
            floatTriggerHigh = EditorGUILayout.FloatField(floatTriggerHigh, GUILayout.Width(40));
            EditorGUI.indentLevel = indentLevel;
            EditorGUILayout.EndHorizontal();

            floatTriggerLowProp.floatValue = floatTriggerLow;
            floatTriggerHighProp.floatValue = floatTriggerHigh;
        }

        protected static SerializedProperty InverseInspector(SerializedProperty eventProp) {
            GUIContent text = new GUIContent(
                "Inverse",
                "Negate the boolean state before calling event trigger"
                );
            SerializedProperty boolInverseProp = eventProp.FindPropertyRelative("boolInverse");
            boolInverseProp.boolValue = EditorGUILayout.Toggle(text, boolInverseProp.boolValue);
            return boolInverseProp;
        }

        #endregion

    }

}