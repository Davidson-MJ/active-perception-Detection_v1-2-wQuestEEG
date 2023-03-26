using UnityEditor;

namespace Passer.Humanoid {
    using Pawn;

    public class HumanoidNetworking_Editor : PawnNetworking_Editor {

        protected virtual void SyncFingerSwingInspector() {
            SerializedProperty syncFingerSwingProp = serializedObject.FindProperty("_syncFingerSwing");
            syncFingerSwingProp.boolValue = EditorGUILayout.Toggle("Sync Finger Swing", syncFingerSwingProp.boolValue);
        }

        protected virtual void SyncTrackingInspector() {
            SerializedProperty syncTrackingProp = serializedObject.FindProperty("_syncTracking");
            syncTrackingProp.boolValue = EditorGUILayout.Toggle("Sync Tracking Space", syncTrackingProp.boolValue);
        }

    }

}