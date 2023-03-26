using UnityEditor;
using UnityEngine;

namespace Passer.Pawn {

    [CanEditMultipleObjects]
    [CustomEditor(typeof(PawnHead))]
    public class PawnHead_Editor : Editor {
        private PawnHead headTarget;

        private TargetProps[] allProps;

        #region Enable

        public void OnEnable() {
            headTarget = (PawnHead)target;
            //headTarget._pawn = GetPawn(headTarget);

            InitEditors();

            headTarget.InitSensors();
            InitEvents();
        }

        private void InitEditors() {
            allProps = new TargetProps[] {
                //new UnityVR_Editor.HeadTargetProps(serializedObject, headTarget)
            };
        }

        #endregion

        #region Disable
        public void OnDisable() {
            if (headTarget.pawn == null) {
                // This target is not connected to a pawn, so we delete it
                DestroyImmediate(headTarget, true);
                return;
            }

            if (!Application.isPlaying) {
                SetSensor2Target();
            }
        }

        private void SetSensor2Target() {
            if (allProps != null)
                foreach (TargetProps props in allProps)
                    props.SetSensor2Target();
        }
        #endregion

        #region Inspector

        public static PawnHead Inspector(string name, PawnControl pawn) {
            if (pawn.headTarget != null)
                pawn.headTarget.CheckSensors();

            EditorGUILayout.BeginHorizontal();
            PawnHead cameraTarget = pawn.headTarget;
            if (cameraTarget == null || cameraTarget.transform == null) {
                if (!Application.isPlaying) {
                    EditorGUILayout.LabelField(name);
                    if (GUILayout.Button("Create", GUILayout.MinWidth(60))) {
                        cameraTarget = PawnHead.Create(pawn);
                        cameraTarget.InitSensors();
                        cameraTarget.show = pawn.showRealObjects;
                    }
                }
            }
            else {
                EditorGUI.BeginDisabledGroup(true);
                GUIContent text = new GUIContent(
                    name,
                    "The transform controlling the " + name
                    );
                EditorGUILayout.ObjectField(text, cameraTarget.transform, typeof(Transform), true);
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();
            return cameraTarget;
        }

        public override void OnInspectorGUI() {
            if (headTarget == null || headTarget.pawn == null)
                return;

            serializedObject.Update();

            SensorInspectors(headTarget);

            SettingsInspector(headTarget);

            EventsInspector();
            GazeInteractionButton(headTarget);

            serializedObject.ApplyModifiedProperties();
        }

        #region Sensors

        private void SensorInspectors(PawnHead cameraTarget) {
            FirstPersonCameraInspector(cameraTarget);

            foreach (TargetProps props in allProps)
                props.Inspector();
        }

        private void FirstPersonCameraInspector(PawnHead headTarget) {
#if hLEGACYXR
            SerializedProperty cameraTargetProp = serializedObject.FindProperty("unityCamera.target");
            cameraTargetProp.objectReferenceValue = headTarget;

            SerializedProperty cameraEnabledProp = serializedObject.FindProperty("unityCamera.enabled");
            bool wasEnabled = cameraEnabledProp.boolValue;
            cameraEnabledProp.boolValue = EditorGUILayout.ToggleLeft(headTarget.unityCamera.name, cameraEnabledProp.boolValue, GUILayout.MinWidth(80));

            if (cameraEnabledProp.boolValue)
                headTarget.pawn.unityTracker.enabled = true;

            if (!Application.isPlaying) {
                UnityCamera.CheckCamera(headTarget);
                if (!wasEnabled && cameraEnabledProp.boolValue) {
                    SerializedProperty sensorTransformProp = serializedObject.FindProperty("unityCamera.sensorTransform");
                    sensorTransformProp.objectReferenceValue = headTarget.unityCamera.AddCamera();
                }
                else if (wasEnabled && !cameraEnabledProp.boolValue) {
                    UnityCamera.RemoveCamera(headTarget);
                }
            }
#endif
        }

        #endregion

        #region Settings
        protected bool showSettings;
        protected virtual void SettingsInspector(PawnHead camearTarget) {
            //showSettings = EditorGUILayout.Foldout(showSettings, "Settings", true);
            //if (showSettings) {
            //    EditorGUI.indentLevel++;

            //    EditorGUI.indentLevel--;
            //}
        }
        #endregion

        #region Events
        //protected SerializedProperty audioEventProp;
        protected virtual void InitEvents() {
            //audioEventProp = serializedObject.FindProperty("audioEvent");
        }

        protected int selectedEvent;

        protected bool showEvents;
        protected virtual void EventsInspector() {
            //showEvents = EditorGUILayout.Foldout(showEvents, "Events", true);
            //if (showEvents) {
            //    EditorGUI.indentLevel++;

            //    EditorGUILayout.BeginHorizontal();

            //    // Labels
            //    EditorGUILayout.BeginVertical(GUILayout.MinWidth(110));

            //    GUILayout.Space(3);
            //    EditorGUILayout.LabelField("Audio", GUILayout.Width(110));
            //    EditorGUILayout.EndVertical();

            //    // Buttons
            //    string[] buttonTexts = new string[1];
            //    buttonTexts[0] = Event.GetInputButtonLabel(cameraTarget.audioEvent.floatEvent);

            //    int oldFontSize = GUI.skin.button.fontSize;
            //    GUI.skin.button.fontSize = 9;
            //    selectedEvent = GUILayout.SelectionGrid(selectedEvent, buttonTexts, 1);
            //    GUI.skin.button.fontSize = oldFontSize;

            //    EditorGUILayout.EndHorizontal();

            //    // Details
            //    GUIStyle style = new GUIStyle(GUI.skin.label) {
            //        fontStyle = FontStyle.Bold
            //    };
            //    EditorGUILayout.LabelField("Details", style, GUILayout.ExpandWidth(true));

            //    EditorGUI.indentLevel++;
            //    EventDetails(selectedEvent);
            //    EditorGUI.indentLevel--;

            //    EditorGUI.indentLevel--;
            //}
        }

        protected void EventDetails(int selectedEvent) {
            //switch (selectedEvent) {
            //    case 0:
            //        FloatEvent_Editor.DetailsInspector(audioEventProp, "Audio");
            //        break;
            //}
        }

        #endregion

        #region Buttons
        private void GazeInteractionButton(PawnHead cameraTarget) {
            InteractionPointer interactionPointer = cameraTarget.transform.GetComponentInChildren<InteractionPointer>();
            if (interactionPointer != null)
                return;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Interaction Pointer"))
                AddInteractionPointer();
            //if (GUILayout.Button("Add Teleporter"))
            //    AddTeleporter();
            GUILayout.EndHorizontal();
        }

        private void AddInteractionPointer() {
            InteractionPointer pointer = InteractionPointer.Add(headTarget.transform, InteractionPointer.PointerType.FocusPoint);
            //Camera fpCamera = UnityCamera.GetCamera(cameraTarget);
            //if (fpCamera != null) {
            pointer.transform.position = headTarget.transform.position;
            pointer.transform.rotation = headTarget.transform.rotation;
            //}
            pointer.focusPointObj.transform.localPosition = new Vector3(0, 0, 2);

            ControllerInput controllerInput = headTarget.pawn.GetComponent<ControllerInput>();
            if (controllerInput != null)
                controllerInput.SetEventHandler(false, ControllerInput.SideButton.Button1, pointer.Click);
            MouseInput mouseInput = headTarget.pawn.GetComponent<MouseInput>();
            if (mouseInput != null)
                mouseInput.SetEventHandler(MouseInput.Button.Left, EventHandler.Type.OnChange, pointer.Click);
        }

        //private void AddTeleporter() {
        //    Teleporter teleporter = Teleporter.Add(cameraTarget.transform, InteractionPointer.PointerType.FocusPoint);
        //    if (cameraTarget.unityVRHead.cameraTransform != null) {
        //        teleporter.transform.position = cameraTarget.unityVRHead.cameraTransform.position;
        //        teleporter.transform.rotation = cameraTarget.unityVRHead.cameraTransform.rotation;
        //    }
        //    teleporter.focusPointObj.transform.localPosition = new Vector3(0, 0, 2);

        //    ControllerInput controllerInput = cameraTarget.humanoid.GetComponent<ControllerInput>();
        //    if (controllerInput != null)
        //        ControllerEvent_Editor.SetBoolMethod(controllerInput.GetInputEvent(true, ControllerInput.SideButton.Button1), Event.Type.OnStart, teleporter.Click);

        //}
        #endregion

        #endregion

        public abstract class TargetProps {
            public SerializedProperty enabledProp;
            public SerializedProperty sensorTransformProp;
            public SerializedProperty sensor2TargetPositionProp;
            public SerializedProperty sensor2TargetRotationProp;

            public PawnHead cameraTarget;
            public CameraSensor sensor;

            public TargetProps(SerializedObject serializedObject, CameraSensor _sensor, PawnHead _cameraTarget, string unitySensorName) {
                enabledProp = serializedObject.FindProperty(unitySensorName + ".enabled");
                sensorTransformProp = serializedObject.FindProperty(unitySensorName + ".sensorTransform");
                sensor2TargetPositionProp = serializedObject.FindProperty(unitySensorName + ".sensor2TargetPosition");
                sensor2TargetRotationProp = serializedObject.FindProperty(unitySensorName + ".sensor2TargetRotation");

                cameraTarget = _cameraTarget;
                sensor = _sensor;

                sensor.target = cameraTarget;
            }

            public virtual void SetSensor2Target() {
                //if (sensor.sensorTransform == null)
                //    return;

                //sensor2TargetRotationProp.quaternionValue = Quaternion.Inverse(sensor.sensorTransform.rotation) * headTarget.head.target.transform.rotation;
                //sensor2TargetPositionProp.vector3Value = -headTarget.head.target.transform.InverseTransformPoint(sensor.sensorTransform.position);
            }

            public abstract void Inspector();
        }
    }
}