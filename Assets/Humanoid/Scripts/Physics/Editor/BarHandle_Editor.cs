using UnityEditor;

namespace Passer.Humanoid {

    [CustomEditor(typeof(BarHandle))]
    public class BarHandle_Editor : Handle_Editor {

        public override void OnInspectorGUI() {
            EditorGUILayout.HelpBox("BarHandle is depreciated, use Handle with Grab Type=Bar Grab instead.", MessageType.Warning);

            base.OnInspectorGUI();
        }
    }
}