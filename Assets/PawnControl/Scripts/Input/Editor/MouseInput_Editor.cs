using UnityEditor;

namespace Passer {

    [CustomEditor(typeof(MouseInput))]
    public class MouseInput_Editor : Editor {
        protected MouseInput mouseInput;

        protected string[] mouseLabelList = new string[] {
                "Mouse Vertical",
                "Mouse Horizontal",
                "Mouse Scroll",
                "Left Button",
                "Middle button",
                "Right Button",
            };

        #region Enable

        protected void OnEnable() {
            mouseInput = (MouseInput)target;
        }

        #endregion

        #region Disable

        protected virtual void OnDisable() {
            ControllerEventHandlers.Cleanup(mouseInput.mouseInputEvents);
        }

        #endregion

        #region Inspector

        protected int selectedMouse = -1;
        protected int selectedSub = -1;

        public override void OnInspectorGUI() {
            serializedObject.Update();

            SerializedProperty mouseEventsProp = serializedObject.FindProperty("mouseInputEvents");
            for (int i = 0; i < mouseEventsProp.arraySize; i++) {
                ControllerEvent_Editor.EventInspector(mouseEventsProp.GetArrayElementAtIndex(i), ref selectedMouse, ref selectedSub);
            }

            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }
}