using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Passer {

    public class FunctionCall_Editor {

        public static void AppendNewFunctionCall(SerializedProperty functionCallsProp) {
            int functionCallCount = functionCallsProp.arraySize;

            functionCallsProp.InsertArrayElementAtIndex(functionCallCount);
            SerializedProperty functionCallProp = functionCallsProp.GetArrayElementAtIndex(functionCallCount);
            SerializedProperty targetGameObjectProp = functionCallProp.FindPropertyRelative("targetGameObject");
            targetGameObjectProp.objectReferenceValue = null;
        }

        public static void FunctionCallInspector(SerializedProperty functionCallProp) {
            if (functionCallProp == null)
                return;

            GUIStyle style = new GUIStyle(GUI.skin.box) {
                margin = new RectOffset(0, 0, 0, 0)
            };

            GUI.SetNextControlName("rect");
            Rect rect = EditorGUILayout.BeginVertical(style);
            GUI.Box(rect, "", style);

            int indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 1;

            FunctionCallTargetInspector(functionCallProp);

            EditorGUI.indentLevel = indentLevel;
            EditorGUILayout.EndVertical();
        }

        private static void FunctionCallTargetInspector(SerializedProperty functionCallProp) {
            SerializedProperty targetGameObjectProp = functionCallProp.FindPropertyRelative("targetGameObject");
            GUIContent text = new GUIContent(
                "Target",
                "The Object on which the method is called"
                );
            EditorGUILayout.ObjectField(targetGameObjectProp, text);

            // Component / Method
            string[] methodNames = Event_Editor.GetMethodNames(targetGameObjectProp, Event_Editor.EventMethodCheck);

            SerializedProperty methodNameProp = functionCallProp.FindPropertyRelative("methodName");
            int methodNameIndex = Event_Editor.GetMethodIndex(methodNameProp.stringValue, methodNames);
            methodNameIndex = EditorGUILayout.Popup("Method", methodNameIndex, methodNames);

            if (methodNameIndex >= 0 && methodNameIndex < methodNames.Length) {
                string fullMethodName = methodNames[methodNameIndex];

                Event_Editor.SetMethod(functionCallProp, fullMethodName);

                MethodParametersInspector(functionCallProp);
            }
        }

        private static void MethodParametersInspector(SerializedProperty functionCallProp) {
            SerializedProperty parametersProp = functionCallProp.FindPropertyRelative("parameters");
            if (parametersProp.arraySize == 0)
                return;

            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();
            SerializedProperty parameterProp = parametersProp.GetArrayElementAtIndex(0);
            SerializedProperty parameterTypeProp = parameterProp.FindPropertyRelative("type");

            switch ((FunctionCall.ParameterType)parameterTypeProp.intValue) {
                case FunctionCall.ParameterType.Bool:
                    SerializedProperty boolValueProp = parameterProp.FindPropertyRelative("boolConstant");
                    boolValueProp.boolValue = EditorGUILayout.Toggle("Parameter", boolValueProp.boolValue);
                    break;
                case FunctionCall.ParameterType.Float:
                    SerializedProperty floatValueProp = parameterProp.FindPropertyRelative("floatConstant");
                    floatValueProp.floatValue = EditorGUILayout.FloatField("Parameter", floatValueProp.floatValue);
                    break;
                case FunctionCall.ParameterType.Int:
                    SerializedProperty intValueProp = parameterProp.FindPropertyRelative("intConstant");
                    intValueProp.intValue = EditorGUILayout.IntField("Parameter", intValueProp.intValue);
                    break;
                case FunctionCall.ParameterType.Vector3:
                    SerializedProperty vector3ValueProp = parameterProp.FindPropertyRelative("vector3Constant");
                    vector3ValueProp.vector3Value = EditorGUILayout.Vector3Field("Parameter", vector3ValueProp.vector3Value);
                    break;
                case FunctionCall.ParameterType.GameObject:
                    SerializedProperty gameObjectValueProp = parameterProp.FindPropertyRelative("gameObjectConstant");
                    gameObjectValueProp.objectReferenceValue = EditorGUILayout.ObjectField("Parameter", gameObjectValueProp.objectReferenceValue, typeof(GameObject), false);
                    break;
                case FunctionCall.ParameterType.Rigidbody:
                    SerializedProperty rigidbodyValueProp = parameterProp.FindPropertyRelative("rigidbodyConstant");
                    rigidbodyValueProp.objectReferenceValue = EditorGUILayout.ObjectField("Parameter", rigidbodyValueProp.objectReferenceValue, typeof(Rigidbody), false);
                    break;
                case FunctionCall.ParameterType.Void:
                    break;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }

        public static string GetFunctionCallLabel(SerializedProperty functionCallProp) {
            SerializedProperty eventTargetGameObjectProp = functionCallProp.FindPropertyRelative("targetGameObject");
            if (eventTargetGameObjectProp.objectReferenceValue == null)
                return "";

            string targetName = eventTargetGameObjectProp.objectReferenceValue.ToString();
            int braceIndex = targetName.IndexOf('(');
            if (braceIndex > 0)
                targetName = targetName.Substring(0, braceIndex - 1);

            SerializedProperty methodNameProp = functionCallProp.FindPropertyRelative("methodName");
            string targetMethodName = methodNameProp.stringValue;
            string label = "[" + targetName + "]";

            if (targetMethodName.Substring(0, 4) == "set_")
                label += targetMethodName.Substring(4) + GetLabelSetter(functionCallProp);
            else
                label += targetMethodName + GetLabel1Parameter(functionCallProp);

            return label;
        }

        private static string GetLabel1Parameter(SerializedProperty functionCallProp) {
            SerializedProperty parametersProp = functionCallProp.FindPropertyRelative("parameters");
            if (parametersProp.arraySize == 1) {
                SerializedProperty parameterProp = parametersProp.GetArrayElementAtIndex(0);
                SerializedProperty parameterTypeProp = parameterProp.FindPropertyRelative("type");
                switch ((FunctionCall.ParameterType)parameterTypeProp.intValue) {
                    case FunctionCall.ParameterType.Void:
                        return "()";
                    case FunctionCall.ParameterType.Float:
                        SerializedProperty floatValueProp = parameterProp.FindPropertyRelative("floatConstant");
                        return "(" + floatValueProp.floatValue + ")";
                    case FunctionCall.ParameterType.Bool:
                        SerializedProperty boolValueProp = parameterProp.FindPropertyRelative("boolConstant");
                        return "(" + boolValueProp.boolValue + ")";
                    case FunctionCall.ParameterType.Int:
                        SerializedProperty intValueProp = parameterProp.FindPropertyRelative("intConstant");
                        return "(" + intValueProp.intValue + ")";
                    default:
                        break;
                }
            }
            return "(...)";
        }

        private static string GetLabelSetter(SerializedProperty functionCallProp) {
            SerializedProperty parametersProp = functionCallProp.FindPropertyRelative("parameters");
            if (parametersProp.arraySize == 1) {
                SerializedProperty parameterProp = parametersProp.GetArrayElementAtIndex(0);
                SerializedProperty parameterTypeProp = parameterProp.FindPropertyRelative("type");
                switch ((FunctionCall.ParameterType)parameterTypeProp.intValue) {
                    case FunctionCall.ParameterType.Float:
                        SerializedProperty floatValueProp = parameterProp.FindPropertyRelative("floatConstant");
                        return " = " + floatValueProp.floatValue;
                    case FunctionCall.ParameterType.Bool:
                        SerializedProperty boolValueProp = parameterProp.FindPropertyRelative("boolConstant");
                        return " = " + boolValueProp.boolValue;
                    case FunctionCall.ParameterType.Int:
                        SerializedProperty intValueProp = parameterProp.FindPropertyRelative("intConstant");
                        return " = " + intValueProp.intValue;
                    default:
                        break;
                }
            }
            return " = ...";
        }

        #region Properties

        public static void PropertyParameterInspector(SerializedObject serializedObject, SerializedProperty parameterProp, System.Type propertyType) {
            string[] propertyNames = GetPropertyNames(serializedObject, propertyType);
            SerializedProperty propertyNameProp = parameterProp.FindPropertyRelative("localProperty");
            SerializedProperty fromEventProp = parameterProp.FindPropertyRelative("fromEvent");

            int propertyNameIndex = GetPropertyIndex(propertyNameProp.stringValue, propertyNames);
            if (propertyNameProp.stringValue == "" || propertyNameProp.stringValue == "Constant") {
                propertyNameProp.stringValue = "Constant";
                propertyNameIndex = 0;
                propertyNameIndex = EditorGUILayout.Popup(propertyNameIndex, propertyNames, GUILayout.Width(70));
                ConstantParameterInspector(parameterProp);
                fromEventProp.boolValue = false;
            }
            else {
                propertyNameIndex = EditorGUILayout.Popup(propertyNameIndex, propertyNames);
                fromEventProp.boolValue = true;
            }

            if (propertyNameIndex >= 0 && propertyNameIndex < propertyNames.Length) {
                string propertyName = propertyNames[propertyNameIndex];

                propertyNameProp.stringValue = propertyName;
            }
        }

        protected static void ConstantParameterInspector(SerializedProperty parameterProp) {
            SerializedProperty parameterTypeProp = parameterProp.FindPropertyRelative("type");

            switch ((FunctionCall.ParameterType)parameterTypeProp.intValue) {
                case FunctionCall.ParameterType.Bool:
                    SerializedProperty boolValueProp = parameterProp.FindPropertyRelative("boolConstant");
                    boolValueProp.boolValue = EditorGUILayout.Toggle(boolValueProp.boolValue);
                    break;
                case FunctionCall.ParameterType.Float:
                    SerializedProperty floatValueProp = parameterProp.FindPropertyRelative("floatConstant");
                    floatValueProp.floatValue = EditorGUILayout.FloatField(floatValueProp.floatValue);
                    break;
                case FunctionCall.ParameterType.Int:
                    SerializedProperty intValueProp = parameterProp.FindPropertyRelative("intConstant");
                    intValueProp.intValue = EditorGUILayout.IntField(intValueProp.intValue);
                    break;
                case FunctionCall.ParameterType.String:
                    SerializedProperty stringValueProp = parameterProp.FindPropertyRelative("stringConstant");
                    stringValueProp.stringValue = EditorGUILayout.TextField(stringValueProp.stringValue);
                    break;
                case FunctionCall.ParameterType.Vector3:
                    SerializedProperty vector3ValueProp = parameterProp.FindPropertyRelative("vector3Constant");
                    vector3ValueProp.vector3Value = EditorGUILayout.Vector3Field("", vector3ValueProp.vector3Value);
                    break;
                case FunctionCall.ParameterType.GameObject:
                    SerializedProperty gameObjectValueProp = parameterProp.FindPropertyRelative("gameObjectConstant");
                    gameObjectValueProp.objectReferenceValue = EditorGUILayout.ObjectField(gameObjectValueProp.objectReferenceValue, typeof(GameObject), true);
                    break;
                case FunctionCall.ParameterType.Rigidbody:
                    SerializedProperty rigidbodyValueProp = parameterProp.FindPropertyRelative("rigidbodyConstant");
                    rigidbodyValueProp.objectReferenceValue = EditorGUILayout.ObjectField(rigidbodyValueProp.objectReferenceValue, typeof(Rigidbody), true);
                    break;
                case FunctionCall.ParameterType.Void:
                    break;
            }
        }

        protected static string[] GetPropertyNames(SerializedObject serializedObject, System.Type propertyType) {
            Object targetObject = serializedObject.targetObject;
            if (targetObject == null)
                return new string[0];

            //Component targetComponent = (Component)targetObject;
            //GameObject gameObject = targetComponent.gameObject;
            //Component[] components = gameObject.GetComponents<Component>();

            List<string> nameList = new List<string> {
                "Constant",
                "",
                "From Event"
            };

            //AddLocalPropertyNames(ref nameList, targetComponent, propertyType);
            //nameList.Add("");
            //AddPropertyNames(ref nameList, gameObject, propertyType);
            //foreach (Component component in components) {
            //    AddPropertyNames(ref nameList, component, propertyType);
            //}

            string[] names = nameList.ToArray();
            return names;
        }

        protected static void AddPropertyNames(ref List<string> names, Object component, System.Type propertyType) {
            if (component == null)
                return;

            System.Type componentType = component.GetType();

            //FieldInfo[] fields = componentType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            //for (int i = 0; i < fields.Length; i++) {
            //    //if (BlackListed(methods[i].Name))
            //    //    continue;

            //    names.Add(componentType.Name + "/" + fields[i].Name);
            //}

            PropertyInfo[] properties = componentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < properties.Length; i++) {
                //if (BlackListed(methods[i].Name))
                //    continue;

                if (properties[i].PropertyType == propertyType)
                    names.Add(componentType.Name + "/" + properties[i].Name);
            }

        }

        protected static void AddLocalPropertyNames(ref List<string> names, Object component, System.Type propertyType) {
            if (component == null)
                return;

            System.Type componentType = component.GetType();

            //FieldInfo[] fields = componentType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            //for (int i = 0; i < fields.Length; i++) {
            //    //if (BlackListed(methods[i].Name))
            //    //    continue;

            //    names.Add(fields[i].Name);
            //}

            PropertyInfo[] properties = componentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < properties.Length; i++) {
                //if (BlackListed(methods[i].Name))
                //    continue;

                if (properties[i].PropertyType == propertyType)
                    names.Add(properties[i].Name);
            }

        }

        public static int GetPropertyIndex(string propertyName, string[] propertyNames) {
            for (int i = 0; i < propertyNames.Length; i++) {
                if (propertyName == propertyNames[i])
                    return i;
            }
            return -1;
        }

        #endregion
    }

}