using UnityEditor;
using UnityEngine;

namespace Passer {
    using Pawn;
    using Humanoid;

    [CustomEditor(typeof(ControllerInput))]
    public class ControllerInput_Editor : Editor {
        protected ControllerInput controllerInput;

#if pHUMANOID
        protected HumanoidControl humanoid;
#endif
        //protected PawnControl pawn;

        #region Enable

        protected virtual void OnEnable() {
            controllerInput = (ControllerInput)target;

            for (int i = 0; i < controllerInput.leftInputEvents.Length; i++) {
                if (controllerInput.leftInputEvents[i].events == null || controllerInput.leftInputEvents[i].events.Count == 0) {
                    controllerInput.leftInputEvents[i].events.Add(new ControllerEventHandler(controllerInput.gameObject, EventHandler.Type.Never));
                }
            }
            for (int i = 0; i < controllerInput.rightInputEvents.Length; i++) {
                if (controllerInput.rightInputEvents[i].events == null || controllerInput.rightInputEvents[i].events.Count == 0) {
                    controllerInput.rightInputEvents[i].events.Add(new ControllerEventHandler(controllerInput.gameObject, EventHandler.Type.Never));
                }
            }

#if pHUMANOID
            serializedObject.Update();
            humanoid = controllerInput.GetComponent<HumanoidControl>();
            SerializedProperty humanoidProp = serializedObject.FindProperty("humanoid");
            humanoidProp.objectReferenceValue = humanoid;
            serializedObject.ApplyModifiedProperties();
#endif

            InitController();
            CheckHandlerLabels();
        }

        protected virtual void CheckHandlerLabels() {
            string[] leftLabels = defaultLeft;
            string[] rightLabels = defaultRight;
            if (controllerInput.leftInputEvents.Length < 13) {
                for (int i = 0; i < 3; i++) {
                    leftLabels[i] = defaultLeft[i];
                    rightLabels[i] = defaultRight[i];
                }
                for (int i = 3; i < 10; i++) {
                    leftLabels[i] = defaultLeft[i + 3];
                    rightLabels[i] = defaultRight[i + 3];
                }
            }
#if pHUMANOID
            if (humanoid != null) {
#if hOCULUS && (UNITY_STANDALONE_WIN || UNITY_ANDROID)
                if (humanoid.oculus.enabled) {
                    if (controllerInput.leftInputEvents.Length < 13) {
                        for (int i = 0; i < 3; i++) {
                            leftLabels[i] = oculusLeft[i];
                            rightLabels[i] = oculusRight[i];
                        }
                        for (int i = 3; i < 10; i++) {
                            leftLabels[i] = oculusLeft[i + 3];
                            rightLabels[i] = oculusRight[i + 3];
                        }
                    }
                    else {
                        leftLabels = oculusLeft;
                        rightLabels = oculusRight;
                    }
                }
                else
#endif
#if hOPENVR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
                if (humanoid.openVR.enabled) {
                    if (controllerInput.leftInputEvents.Length < 13) {
                        for (int i = 0; i < 3; i++) {
                            leftLabels[i] = openVRLeft[i];
                            rightLabels[i] = openVRRight[i];
                        }
                        for (int i = 3; i < 10; i++) {
                            leftLabels[i] = openVRLeft[i + 3];
                            rightLabels[i] = openVRRight[i + 3];
                        }
                    }
                    else {
                        leftLabels = openVRLeft;
                        rightLabels = openVRRight;
                    }
                }
                else
#endif
#if hWINDOWSMR && UNITY_2017_2_OR_NEWER && !UNITY_2020_1_OR_NEWER && UNITY_WSA_10_0
                if (humanoid.mixedReality.enabled) {
                    if (controllerInput.leftInputEvents.Length < 13) {
                        for (int i = 0; i < 3; i++) {
                            leftLabels[i] = windowsMRLeft[i];
                            rightLabels[i] = windowsMRRight[i];
                        }
                        for (int i = 3; i < 10; i++) {
                            leftLabels[i] = windowsMRLeft[i + 3];
                            rightLabels[i] = windowsMRRight[i + 3];
                        }
                    }
                    else { 
                        leftLabels = windowsMRLeft;
                        rightLabels = windowsMRRight;
                    }
            }
            else
#endif
                {
                    ;
                }
            }
            else
#endif
                switch (controllerInput.gameController) {
                    case GameControllers.Xbox:
                        if (controllerInput.leftInputEvents.Length < 13) {
                            for (int i = 0; i < 3; i++) {
                                leftLabels[i] = xboxLeft[i];
                                rightLabels[i] = xboxRight[i];
                            }
                            for (int i = 3; i < 10; i++) {
                                leftLabels[i] = xboxLeft[i + 3];
                                rightLabels[i] = xboxRight[i + 3];
                            }
                        }
                        else {
                            leftLabels = xboxLeft;
                            rightLabels = xboxRight;
                        }
                        break;
                }

            for (int i = 0; i < controllerInput.leftInputEvents.Length; i++)
                controllerInput.leftInputEvents[i].label = leftLabels[i];
            for (int i = 0; i < controllerInput.leftInputEvents.Length; i++)
                controllerInput.rightInputEvents[i].label = rightLabels[i];
        }

        #region Labels

        readonly string[] defaultLeft = {
            "Stick Vertical",
            "Stick Horizontal",
            "Stick Button",
            "Touchpad Vertical",
            "Touchpad Horizontal",
            "Touchpad Button",
            "Button 1",
            "Button 2",
            "Button 3",
            "Button 4",
            "Trigger 1",
            "Trigger 2",
            "Option"
        };
        readonly string[] defaultRight = {
            "Stick Vertical",
            "Stick Horizontal",
            "Stick Button",
            "Touchpad Vertical",
            "Touchpad Horizontal",
            "Touchpad Button",
            "Button 1",
            "Button 2",
            "Button 3",
            "Button 4",
            "Trigger 1",
            "Trigger 2",
            "Option"
        };

        readonly string[] xboxLeft = {
            "Vertical",
            "Horizontal",
            "Stick Button",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "Bumper",
            "Trigger",
            "Back"
        };
        readonly string[] xboxRight = {
            "Vertical",
            "Horizontal",
            "Stick Button",
            "",
            "",
            "",
            "A",
            "B",
            "X",
            "Y",
            "Bumper",
            "Trigger",
            "Start"
        };

#if hOCULUS
        readonly string[] oculusLeft = {
            "Vertical",
            "Horizontal",
            "Stick Button",
            "",
            "",
            "",
            "X",
            "Y",
            "",
            "",
            "Trigger 1",
            "Trigger 2",
            "Menu"
        };
        readonly string[] oculusRight = {
            "Vertical",
            "Horizontal",
            "Stick Button",
            "",
            "",
            "",
            "A",
            "B",
            "",
            "",
            "Trigger 1",
            "Trigger 2",
            ""
        };
#endif

#if hOPENVR
        readonly string[] openVRLeft = {
            "Stick Vertical",
            "Stick Horizontal",
            "Stick Button",
            "Touchpad Vertical",
            "Touchpad Horizontal",
            "Touchpad Button",
            "A",
            "B",
            "",
            "",
            "Trigger",
            "Grip",
            "Menu"
        };
        readonly string[] openVRRight = {
            "Stick Vertical",
            "Stick Horizontal",
            "Stick Button",
            "Touchpad Vertical",
            "Touchpad Horizontal",
            "Touchpad Button",
            "A",
            "B",
            "",
            "",
            "Trigger",
            "Grip",
            "Menu"
        };
#endif

#if hWINDOWSMR && UNITY_WSA_10_0
        readonly string[] windowsMRLeft = {
            "Stick Vertical",
            "Stick Horizontal",
            "Stick Button",
            "Touchpad Vertical",
            "Touchpad Horizontal",
            "Touchpad Button",
            "",
            "",
            "",
            "",
            "Select",
            "Grab",
            "Menu"
        };
        readonly string[] windowsMRRight = {
            "Stick Vertical",
            "Stick Horizontal",
            "Stick Button",
            "Touchpad Vertical",
            "Touchpad Horizontal",
            "Touchpad Button",
            "",
            "",
            "",
            "",
            "Select",
            "Grab",
            "Menu"
        };
#endif

        #endregion

        #endregion

        #region Disable

        protected virtual void OnDisable() {
            ControllerEventHandlers.Cleanup(controllerInput.leftInputEvents);
            ControllerEventHandlers.Cleanup(controllerInput.rightInputEvents);
        }

        #endregion

        #region Inspector

        protected int selectedLeft = -1;
        protected int selectedRight = -1;
        protected int selectedSub = -1;

        protected bool showLeft = true;
        protected bool showRight = true;

        public override void OnInspectorGUI() {
            serializedObject.Update();

#if pHUMANOID
            SerializedProperty humanoidProp = serializedObject.FindProperty("humanoid");
            if (humanoidProp.objectReferenceValue != null)
                FingerMovementsInspector();
#endif

            ControllerInspector();

            serializedObject.ApplyModifiedProperties();
        }

        protected void FingerMovementsInspector() {
            GUIContent text = new GUIContent(
                "Finger Movements",
                "Implements finger movements using controller input"
                );

            SerializedProperty fingerMovementsProp = serializedObject.FindProperty("fingerMovements");
            fingerMovementsProp.boolValue = EditorGUILayout.Toggle(text, fingerMovementsProp.boolValue);
        }

        #region Controller
#if pHUMANOID
        protected SerializedProperty fingerMovementsProp;
#endif
        protected SerializedProperty leftEventsProp;
        protected SerializedProperty rightEventsProp;

        protected virtual void InitController() {

            leftEventsProp = serializedObject.FindProperty("leftInputEvents");
            rightEventsProp = serializedObject.FindProperty("rightInputEvents");
        }

        protected virtual void ControllerInspector() {
            GameControllerInspector();

            showLeft = EditorGUILayout.Foldout(showLeft, "Left", true);
            if (showLeft) {
                for (int i = 0; i < controllerInput.leftInputEvents.Length; i++) {
                    ControllerEvent_Editor.EventInspector(
                        leftEventsProp.GetArrayElementAtIndex(i),
                        ref selectedLeft, ref selectedSub
                        );
                }
                if (selectedLeft >= 0)
                    selectedRight = -1;
            }

            showRight = EditorGUILayout.Foldout(showRight, "Right");
            if (showRight) {
                for (int i = 0; i < controllerInput.rightInputEvents.Length; i++) {
                    ControllerEvent_Editor.EventInspector(
                        rightEventsProp.GetArrayElementAtIndex(i),
                        ref selectedRight, ref selectedSub
                        );
                }
                if (selectedRight >= 0)
                    selectedLeft = -1;
            }
        }

        protected void GameControllerInspector() {
            SerializedProperty gameControllerProp = serializedObject.FindProperty("gameController");

#if hLEGACYXR
            UnityTracker.XRDeviceType device = UnityTracker.DetermineLoadedDevice();
            switch (device) {
                case UnityTracker.XRDeviceType.Oculus:
                    gameControllerProp.intValue = (int)GameControllers.Oculus;
                    break;
                case UnityTracker.XRDeviceType.OpenVR:
                    gameControllerProp.intValue = (int)GameControllers.OpenVR;
                    break;
            }
#endif
            GUIContent text = new GUIContent(
                "Game Controller",
                "The type of Game Controller used."
                );
            gameControllerProp.intValue = (int)(GameControllers)EditorGUILayout.EnumPopup(text, (GameControllers)gameControllerProp.intValue);
        }

        #endregion

        #endregion
    }

    [InitializeOnLoad]
    class InputManager {
        static InputManager() {
#if !UNITY_2021_2_OR_NEWER
            EnforceInputManagerBindings();
#endif
        }

        private static void EnforceInputManagerBindings() {
            try {
                BindAxis(new Axis() { name = "Axis 3", axis = 2, });
                BindAxis(new Axis() { name = "Axis 4", axis = 3, });
                BindAxis(new Axis() { name = "Axis 5", axis = 4, });
                BindAxis(new Axis() { name = "Axis 6", axis = 5, });
                BindAxis(new Axis() { name = "Axis 7", axis = 6, });
                BindAxis(new Axis() { name = "Axis 8", axis = 7, });
                BindAxis(new Axis() { name = "Axis 9", axis = 8, });
                BindAxis(new Axis() { name = "Axis 10", axis = 9, });
                BindAxis(new Axis() { name = "Axis 11", axis = 10, });
                BindAxis(new Axis() { name = "Axis 12", axis = 11, });
                BindAxis(new Axis() { name = "Axis 13", axis = 12, });
            }
            catch {
                Debug.LogError("Failed to apply Humanoid input manager bindings.");
            }
        }

        private class Axis {
            public string name = System.String.Empty;
            public string descriptiveName = System.String.Empty;
            public string descriptiveNegativeName = System.String.Empty;
            public string negativeButton = System.String.Empty;
            public string positiveButton = System.String.Empty;
            public string altNegativeButton = System.String.Empty;
            public string altPositiveButton = System.String.Empty;
            public float gravity = 0.0f;
            public float dead = 0.001f;
            public float sensitivity = 1.0f;
            public bool snap = false;
            public bool invert = false;
            public int type = 2;
            public int axis = 0;
            public int joyNum = 0;
        }

        private static void BindAxis(Axis axis) {
            SerializedObject serializedObject = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0]);
            SerializedProperty axesProperty = serializedObject.FindProperty("m_Axes");

            SerializedProperty axisIter = axesProperty.Copy();
            axisIter.Next(true);
            axisIter.Next(true);
            while (axisIter.Next(false)) {
                if (axisIter.FindPropertyRelative("m_Name").stringValue == axis.name) {
                    // Axis already exists. Don't create binding.
                    return;
                }
            }

            axesProperty.arraySize++;
            serializedObject.ApplyModifiedProperties();

            SerializedProperty axisProperty = axesProperty.GetArrayElementAtIndex(axesProperty.arraySize - 1);
            axisProperty.FindPropertyRelative("m_Name").stringValue = axis.name;
            axisProperty.FindPropertyRelative("descriptiveName").stringValue = axis.descriptiveName;
            axisProperty.FindPropertyRelative("descriptiveNegativeName").stringValue = axis.descriptiveNegativeName;
            axisProperty.FindPropertyRelative("negativeButton").stringValue = axis.negativeButton;
            axisProperty.FindPropertyRelative("positiveButton").stringValue = axis.positiveButton;
            axisProperty.FindPropertyRelative("altNegativeButton").stringValue = axis.altNegativeButton;
            axisProperty.FindPropertyRelative("altPositiveButton").stringValue = axis.altPositiveButton;
            axisProperty.FindPropertyRelative("gravity").floatValue = axis.gravity;
            axisProperty.FindPropertyRelative("dead").floatValue = axis.dead;
            axisProperty.FindPropertyRelative("sensitivity").floatValue = axis.sensitivity;
            axisProperty.FindPropertyRelative("snap").boolValue = axis.snap;
            axisProperty.FindPropertyRelative("invert").boolValue = axis.invert;
            axisProperty.FindPropertyRelative("type").intValue = axis.type;
            axisProperty.FindPropertyRelative("axis").intValue = axis.axis;
            axisProperty.FindPropertyRelative("joyNum").intValue = axis.joyNum;
            serializedObject.ApplyModifiedProperties();
        }
    }

}