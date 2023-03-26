using UnityEngine;
using UnityEditor;

namespace Passer.Humanoid {
    using Passer.Tracking;

    [CustomEditor(typeof(RealWorldConfiguration))]
    public class RealWorldConfiguration_Editor : Editor {

        public override void OnInspectorGUI() {
            serializedObject.Update();
            SerializedProperty trackerList = serializedObject.FindProperty("trackers");
            for (int i = 0; i < trackerList.arraySize; i++) {
                SerializedProperty trackerProp = trackerList.GetArrayElementAtIndex(i);

                TrackingSpaceInspector(trackerProp);
            }
            serializedObject.ApplyModifiedProperties();
        }

        bool foldout = true;
        protected virtual void TrackingSpaceInspector(SerializedProperty trackerProp) {
            SerializedProperty trackerIdProp = trackerProp.FindPropertyRelative("trackerId");
            TrackerId trackerId = (TrackerId) trackerIdProp.intValue;
            foldout = EditorGUILayout.Foldout(foldout, trackerId.ToString());
            if (foldout) {
                EditorGUI.indentLevel++;
                SerializedProperty positionProp = trackerProp.FindPropertyRelative("position");
                positionProp.vector3Value = EditorGUILayout.Vector3Field("Position", positionProp.vector3Value);

                SerializedProperty rotationProp = trackerProp.FindPropertyRelative("rotation");
                Vector3 angles = rotationProp.quaternionValue.eulerAngles;
                angles = EditorGUILayout.Vector3Field("Rotation", angles);
                rotationProp.quaternionValue = Quaternion.Euler(angles);
                EditorGUI.indentLevel--;
            }
        }
    }
}