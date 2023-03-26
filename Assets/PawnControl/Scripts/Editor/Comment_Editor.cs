using UnityEditor;

namespace Passer {

    [CustomEditor(typeof(Comment))]
    public class Comment_Editor : Editor {

        public override void OnInspectorGUI() {
            serializedObject.Update();

            EditorStyles.textField.wordWrap = true;

            SerializedProperty textProp = serializedObject.FindProperty("text");
            textProp.stringValue = EditorGUILayout.TextArea(textProp.stringValue);

            serializedObject.ApplyModifiedProperties();
        }

    }

}