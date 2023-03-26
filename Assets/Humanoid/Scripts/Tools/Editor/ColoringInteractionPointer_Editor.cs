using UnityEngine;
using UnityEditor;

namespace Passer.Humanoid {
    [CustomEditor(typeof(ColoringInteractionPointer), true)]
    public class ColoringInteractionPointer_Editor : InteractionPointer_Editor {

        #region Inspector

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            SerializedProperty validMaterialProp = serializedObject.FindProperty("validMaterial");
            SerializedProperty invalidMaterialProp = serializedObject.FindProperty("invalidMaterial");

            validMaterialProp.objectReferenceValue = EditorGUILayout.ObjectField("Valid Material", validMaterialProp.objectReferenceValue, typeof(Material), false);
            invalidMaterialProp.objectReferenceValue = EditorGUILayout.ObjectField("Invalid Material", invalidMaterialProp.objectReferenceValue, typeof(Material), false);

            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }
}