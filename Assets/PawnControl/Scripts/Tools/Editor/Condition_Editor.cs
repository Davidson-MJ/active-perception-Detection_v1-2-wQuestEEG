using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Passer {

    public class Condition_Editor {

        public static string GetConditionLabel(SerializedProperty conditionProp) {
            SerializedProperty targetProp = conditionProp.FindPropertyRelative("targetGameObject");
            if (targetProp.objectReferenceValue == null)
                return "";

            string targetName = targetProp.objectReferenceValue.ToString();
            int braceIndex = targetName.IndexOf('(');
            if (braceIndex > 0)
                targetName = targetName.Substring(0, braceIndex - 1);
            string label = "[" + targetName + "]";

            SerializedProperty propertyNameProp = conditionProp.FindPropertyRelative("fullPropertyName");
            string propertyName = propertyNameProp.stringValue;
            label += propertyNameProp.stringValue;

            SerializedProperty propertyTypeProp = conditionProp.FindPropertyRelative("propertyType");
            Condition.PropertyType propertyType = (Condition.PropertyType)propertyTypeProp.intValue;

            SerializedProperty operandIndexProp = conditionProp.FindPropertyRelative("operandIndex");
            int operandIndex = operandIndexProp.intValue;

            switch (propertyType) {
                case Condition.PropertyType.Bool:
                    label += " " + Condition.boolOperands[operandIndex];
                    break;
                case Condition.PropertyType.Int:
                    label += " " + Condition.intOperands[operandIndex];
                    SerializedProperty intConstantProp = conditionProp.FindPropertyRelative("intConstant");
                    label += " " + intConstantProp.intValue;
                    break;
                case Condition.PropertyType.Float:
                    label += " " + Condition.floatOperands[operandIndex];
                    SerializedProperty floatConstantProp = conditionProp.FindPropertyRelative("floatConstant");
                    label += " " + floatConstantProp.floatValue;
                    break;
                case Condition.PropertyType.Object:
                    label += " " + Condition.objectOperands[operandIndex];
                    break;

            }

            return label;
        }

        public static void ConditionInspector(SerializedProperty conditionProp) {
            if (conditionProp == null)
                return;

            GUIStyle style = new GUIStyle(GUI.skin.box) {
                margin = new RectOffset(0, 0, 0, 0)
            };

            Rect rect = EditorGUILayout.BeginVertical(style);
            GUI.Box(rect, "", style);

            int indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 1;

            SerializedProperty targetProp = ConditionTargetInspector(conditionProp);
            ConditionPropertyInspector(conditionProp, targetProp);
            EditorGUILayout.BeginHorizontal();
            ConditionOperandInspector(conditionProp);
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel = indentLevel;
            EditorGUILayout.EndVertical();
        }

        public static void AppendNewCondition(SerializedProperty conditionsProp) {
            int conditionsCount = conditionsProp.arraySize;

            conditionsProp.InsertArrayElementAtIndex(conditionsCount);
            SerializedProperty functionCallProp = conditionsProp.GetArrayElementAtIndex(conditionsCount);
            SerializedProperty targetProp = functionCallProp.FindPropertyRelative("targetGameObject");
            targetProp.objectReferenceValue = null;
        }

        #region Target

        protected static SerializedProperty ConditionTargetInspector(SerializedProperty conditionProp) {
            SerializedProperty targetProp = conditionProp.FindPropertyRelative("targetGameObject");
            GUIContent text = new GUIContent(
                "Target",
                "The Object on which the method is called"
                );
            EditorGUILayout.ObjectField(targetProp, text);

            return targetProp;
        }

        #endregion

        #region Property

        private static System.Type ConditionPropertyInspector(SerializedProperty conditionProp, SerializedProperty targetProp) {
            string componentType = "";
            Object callTarget = targetProp.objectReferenceValue;
            if (callTarget != null && callTarget.GetType() != typeof(GameObject))
                componentType = callTarget.GetType().Name;

            List<Condition.PropertyType> propertyTypes;
            string[] propertyNames = GetPropertyNames(targetProp, out propertyTypes);

            SerializedProperty fullPropertyNameProp = conditionProp.FindPropertyRelative("fullPropertyName");
            //SerializedProperty propertyNameProp = conditionProp.FindPropertyRelative("propertyName");
            int propertyNameIndex = GetPropertyIndex(/*componentType,*/ fullPropertyNameProp.stringValue, propertyNames);
            propertyNameIndex = EditorGUILayout.Popup("Property", propertyNameIndex, propertyNames);

            if (propertyNameIndex >= 0 && propertyNameIndex < propertyNames.Length) {
                string fullPropertyName = propertyNames[propertyNameIndex];
                SetProperty(conditionProp, targetProp, fullPropertyName, propertyTypes[propertyNameIndex]);
            }
            return null;
        }

        private static string[] GetPropertyNames(SerializedProperty targetProp, out List<Condition.PropertyType> propertyTypes) {
            GameObject targetObject = (GameObject)targetProp.objectReferenceValue;
            if (targetObject == null) {
                propertyTypes = new List<Condition.PropertyType>();
                return new string[0];
            }

            Component[] components = targetObject.GetComponents<Component>();

            List<string> nameList = new List<string>();
            propertyTypes = new List<Condition.PropertyType>();
            AddPropertyNames(ref nameList, ref propertyTypes, targetObject);
            foreach (Component component in components) {
                AddPropertyNames(ref nameList, ref propertyTypes, component);
            }

            string[] names = new string[nameList.Count];
            for (int i = 0; i < nameList.Count; i++) {
                names[i] = nameList[i];
            }
            return names;
        }

        private static void AddPropertyNames(ref List<string> names, ref List<Condition.PropertyType> types, Object component) {
            if (component == null)
                return;

            System.Type componentType = component.GetType();
            PropertyInfo[] properties = componentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            for (int i = 0; i < properties.Length; i++) {
                if (BlackListed(properties[i].Name))
                    continue;

                if (properties[i].PropertyType == typeof(float) ||
                    properties[i].PropertyType == typeof(int) ||
                    properties[i].PropertyType == typeof(bool) ||
                    properties[i].PropertyType == typeof(Object)
                    ) {

                    names.Add(componentType.Name + "/" + properties[i].Name);
                    types.Add(Condition.GetFromType(properties[i].PropertyType));
                }
            }
        }

        private static string[] blackList = {

        };
        private static bool BlackListed(string propertyName) {
            foreach (string blackListEntry in blackList) {
                if (propertyName == blackListEntry)
                    return true;
            }
            return false;
        }

        private static int GetPropertyIndex(/*string componentType,*/ string propertyName, string[] propertyNames) {
            for (int i = 0; i < propertyNames.Length; i++) {
                if (propertyName == propertyNames[i])
                    return i;
            }
            return -1;
        }

        private static void SetProperty(
            SerializedProperty conditionProp, SerializedProperty targetProp,
            string fullPropertyName, Condition.PropertyType propertyType) {

            //int slashPos = fullPropertyName.LastIndexOf("/");

            GameObject callTargetObject = Event_Editor.GetTargetGameObject(targetProp);
            if (callTargetObject != null) {
                //string componentType = fullPropertyName.Substring(0, slashPos);
                targetProp.objectReferenceValue = callTargetObject;
            }

            SerializedProperty fullPropertyNameProp = conditionProp.FindPropertyRelative("fullPropertyName");
            fullPropertyNameProp.stringValue = fullPropertyName;
            SerializedProperty propertyTypeProp = conditionProp.FindPropertyRelative("propertyType");
            propertyTypeProp.intValue = (int)propertyType;
        }

        #endregion

        #region Operand

        private static void ConditionOperandInspector(SerializedProperty conditionProp) {
            SerializedProperty propertyTypeProp = conditionProp.FindPropertyRelative("propertyType");
            SerializedProperty operandIndexProp = conditionProp.FindPropertyRelative("operandIndex");
            Condition.PropertyType propertyType = (Condition.PropertyType)propertyTypeProp.intValue;
            switch (propertyType) {
                case Condition.PropertyType.Bool:
                    operandIndexProp.intValue = EditorGUILayout.Popup(" ", operandIndexProp.intValue, Condition.boolOperands);
                    break;
                case Condition.PropertyType.Int:
                    operandIndexProp.intValue = EditorGUILayout.Popup(operandIndexProp.intValue, Condition.intOperands, GUILayout.MaxWidth(120));
                    SerializedProperty intConstantProp = conditionProp.FindPropertyRelative("intConstant");
                    intConstantProp.intValue = EditorGUILayout.IntField(intConstantProp.intValue);
                    break;
                case Condition.PropertyType.Float:
                    operandIndexProp.intValue = EditorGUILayout.Popup(operandIndexProp.intValue, Condition.floatOperands, GUILayout.MaxWidth(120));
                    SerializedProperty floatConstantProp = conditionProp.FindPropertyRelative("floatConstant");
                    floatConstantProp.floatValue = EditorGUILayout.FloatField(floatConstantProp.floatValue);
                    break;
                case Condition.PropertyType.Object:
                    operandIndexProp.intValue = EditorGUILayout.Popup(operandIndexProp.intValue, Condition.objectOperands);
                    break;
                default:
                    return;
            }
        }

        #endregion
    }

}