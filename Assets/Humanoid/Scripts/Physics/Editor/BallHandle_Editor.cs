using UnityEditor;

namespace Passer.Humanoid {

    [CustomEditor(typeof(BallHandle))]
    public class BallHandle_Editor : Handle_Editor {

        public override void OnInspectorGUI() {
            EditorGUILayout.HelpBox("BallHandle is depreciated, use Handle with Grab Type=Ball Grab instead.", MessageType.Warning);

            base.OnInspectorGUI();
        }
    }
}