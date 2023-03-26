using UnityEditor;

namespace Passer.Pawn {

    public class PawnNetworking_Editor : Editor {

        protected virtual void SendRateInspector() {
            SerializedProperty sendRateProp = serializedObject.FindProperty("_sendRate");
            sendRateProp.floatValue = EditorGUILayout.FloatField("Send Rate", sendRateProp.floatValue);
        }

        protected virtual void DebugLevelInspector() {
            SerializedProperty debugLevelProp = serializedObject.FindProperty("_debug");
            debugLevelProp.intValue = (int)(PawnNetworking.DebugLevel)EditorGUILayout.EnumPopup("Debug Level", (PawnNetworking.DebugLevel)debugLevelProp.intValue);
        }

        protected virtual void SmoothingInspector() {
            SerializedProperty smoothingProp = serializedObject.FindProperty("_smoothing");
            smoothingProp.intValue = (int)(PawnNetworking.Smoothing)EditorGUILayout.EnumPopup("Smoothing", (PawnNetworking.Smoothing)smoothingProp.intValue);
        }

        protected virtual void CreateLocalRemotesInspector() {
            SerializedProperty createLocalRemotesProp = serializedObject.FindProperty("_createLocalRemotes");
            createLocalRemotesProp.boolValue = EditorGUILayout.Toggle("Create Local Remotes", createLocalRemotesProp.boolValue);
        }

    }

}
