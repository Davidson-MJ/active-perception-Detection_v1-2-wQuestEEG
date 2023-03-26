using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Passer {

    [CustomEditor(typeof(Script))]
    public class Script_Editor : Editor {

        public void OnEnable() {
            SerializedProperty conditionsProp = serializedObject.FindProperty("conditions");
            if (conditionsProp.arraySize > 0) // 1 = just empty condition
                showConditions = true;
        }

        public void OnDestroy() {
            CleanupStuff();
        }

        protected bool showConditions = false;
        public override void OnInspectorGUI() {
            serializedObject.Update();

            CheckEmptyConditionSlot();
            CheckEmptyFunctionCallSlot();

            Script script = (Script)target;
            if (script.enabled)
                EditorGUILayout.HelpBox(
                    "This script will run automatically at scene start.\n" +
                    "Disable component to prevent this.",
                    MessageType.Info);

            SerializedProperty scriptNameProp = serializedObject.FindProperty("scriptName");
            scriptNameProp.stringValue = EditorGUILayout.TextField("Name", scriptNameProp.stringValue);
            showConditions = EditorGUILayout.Foldout(showConditions, "Conditions", true);
            if (showConditions) {
                EditorGUI.indentLevel += 2;
                ConditionsInspector();
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }

            ScriptInspector();

            serializedObject.ApplyModifiedProperties();
        }

        #region Conditions

        protected int selectedCondition = -1;

        protected void CheckEmptyConditionSlot() {
            SerializedProperty conditionsProp = serializedObject.FindProperty("conditions");

            int conditionsCount = conditionsProp.arraySize;
            if (conditionsCount == 0)
                Condition_Editor.AppendNewCondition(conditionsProp);
            else {
                SerializedProperty lastFunctionCallProp = conditionsProp.GetArrayElementAtIndex(conditionsCount - 1);
                SerializedProperty lastFunctionCallTargetProp = lastFunctionCallProp.FindPropertyRelative("targetGameObject");
                if (lastFunctionCallTargetProp.objectReferenceValue != null) {
                    Condition_Editor.AppendNewCondition(conditionsProp);
                }
            }
        }

        protected virtual void ConditionsInspector() {
            SerializedProperty conditionsProp = serializedObject.FindProperty("conditions");

            int conditionsCount = conditionsProp.arraySize;
            for (int i = 0; i < conditionsCount; i++) {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                SerializedProperty conditionProp = conditionsProp.GetArrayElementAtIndex(i);
                if (selectedCondition == i) {
                    Condition_Editor.ConditionInspector(conditionProp);
                }
                else {
                    string label = Condition_Editor.GetConditionLabel(conditionProp);
                    if (GUILayout.Button(label))
                        selectedCondition = i;
                }
                GUILayout.Space(20);
                EditorGUILayout.EndHorizontal();
            }
        }

        #endregion

        #region Function Calls

        protected virtual void CheckEmptyFunctionCallSlot() {
            SerializedProperty functionCallsProp = serializedObject.FindProperty("functionCalls");

            int functionCallCount = functionCallsProp.arraySize;
            if (functionCallCount == 0)
                FunctionCall_Editor.AppendNewFunctionCall(functionCallsProp);
            else {
                SerializedProperty lastFunctionCallProp = functionCallsProp.GetArrayElementAtIndex(functionCallCount - 1);
                SerializedProperty lastFunctionCallTargetProp = lastFunctionCallProp.FindPropertyRelative("targetGameObject");
                if (lastFunctionCallTargetProp.objectReferenceValue != null) {
                    FunctionCall_Editor.AppendNewFunctionCall(functionCallsProp);
                }
            }
        }

        protected int selectedFunctionCallIx = -1;

        protected virtual void ScriptInspector() {
            SerializedProperty functionCallsProp = serializedObject.FindProperty("functionCalls");

            int functionCallCount = functionCallsProp.arraySize;
            //if (functionCallCount == 0)
            //    FunctionCall_Editor.AppendNewFunctionCall(functionCallsProp);
            //else {
            //    SerializedProperty lastFunctionCallProp = functionCallsProp.GetArrayElementAtIndex(functionCallCount - 1);
            //    SerializedProperty lastFunctionCallTargetProp = lastFunctionCallProp.FindPropertyRelative("target");
            //    if (lastFunctionCallTargetProp.objectReferenceValue != null) {
            //        FunctionCall_Editor.AppendNewFunctionCall(functionCallsProp);
            //        return;
            //    }
            //}

            for (int i = 0; i < functionCallCount; i++) {
                SerializedProperty functionCallProp = functionCallsProp.GetArrayElementAtIndex(i);
                if (selectedFunctionCallIx == i) {
                    FunctionCall_Editor.FunctionCallInspector(functionCallProp);
                }
                else {
                    string label = FunctionCall_Editor.GetFunctionCallLabel(functionCallProp);
                    if (GUILayout.Button(label))
                        selectedFunctionCallIx = i;
                }
            }
            EditorGUILayout.Space();
        }

        #endregion

        #region Cleanup

        protected virtual void CleanupStuff() {
            Script script = (Script)target;
            script.conditions.RemoveAll(condition => condition.targetGameObject == null);
            //script.functionCalls.RemoveAll(functionCall => functionCall.target == null);
            script.functionCalls.RemoveAll(functionCall => functionCall.targetGameObject == null);
        }

        #endregion
    }

}