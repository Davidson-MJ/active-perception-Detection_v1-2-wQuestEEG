using UnityEditor;
using UnityEngine;

namespace Passer.Pawn {

    [CanEditMultipleObjects]
    [CustomEditor(typeof(PawnHand))]
    public class PawnHand_Editor : Editor {
        protected PawnHand handTarget;

        private TargetProps[] allProps;

        #region Enable
        public void OnEnable() {
            handTarget = (PawnHand)target;

            //handTarget.pawn = GetPawn(handTarget);
            //if (handTarget.pawn == null)
            //    return;

            InitEditors();

            handTarget.InitTarget();

            InitOther();
            //InitEvents();

            if (!Application.isPlaying)
                SetSensor2Target();
        }

        protected virtual void InitEditors() {
            allProps = new TargetProps[] {
            };
        }
        #endregion

        #region Disable
        public void OnDisable() {
            if (handTarget.pawn == null) {
                // This target is not connected to a pawn, so we delete it
                DestroyImmediate(handTarget, true);
                return;
            }

            if (!Application.isPlaying) {
                SetSensor2Target();
            }
        }

        private void SetSensor2Target() {
            foreach (TargetProps props in allProps)
                props.SetSensor2Target();
        }
        #endregion

        #region Inspector

        public override void OnInspectorGUI() {
            if (handTarget == null || handTarget.pawn == null)
                return;

            serializedObject.Update();

            SensorInspectors(handTarget);

            TouchedObjectInspector(handTarget);
            GrabbedObjectInspector(handTarget);

            SettingsInspector(handTarget);

            //EventsInspector();

            InteractionPointerButton(handTarget);

            serializedObject.ApplyModifiedProperties();
        }

        public static PawnHand Inspector(string name, PawnControl pawn, Side side) {
            EditorGUILayout.BeginHorizontal();
            PawnHand controllerTarget = side == Side.Left ? pawn.leftHandTarget : pawn.rightHandTarget;
            if (controllerTarget == null || controllerTarget.transform == null) {
                if (!Application.isPlaying) {
                    EditorGUILayout.LabelField(name);
                    if (GUILayout.Button("Create", GUILayout.MinWidth(60))) {
                        controllerTarget = PawnHand.Create(pawn, side);
                        controllerTarget.InitSensors();
                        //controllerTarget.ShowSensors(pawn.showRealObjects);]
                        controllerTarget.showRealObjects = pawn.showRealObjects;
                    }
                }
            }
            else {
                EditorGUI.BeginDisabledGroup(true);
                GUIContent text = new GUIContent(
                    name,
                    "The transform controlling the " + name
                    );
                EditorGUILayout.ObjectField(text, controllerTarget.transform, typeof(Transform), true);
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();
            return controllerTarget;
        }

        //public static PawnControl GetPawn(HandTarget target) {
        //    PawnControl[] pawns = FindObjectsOfType<PawnControl>();
        //    PawnControl foundPawn = null;

        //    for (int i = 0; i < pawns.Length; i++)
        //        if ((pawns[i].leftHandTarget != null && pawns[i].leftHandTarget.transform == target.transform) ||
        //            (pawns[i].rightHandTarget != null && pawns[i].rightHandTarget.transform == target.transform))
        //            foundPawn = pawns[i];

        //    return foundPawn;
        //}

        #region Sensors

        private void SensorInspectors(PawnHand controllerTarget) {
            //    foreach (TargetProps props in allProps)
            //        props.Inspector();
        }

        #endregion

        #region Settings

        public bool showSettings;
        private void SettingsInspector(PawnHand controllerTarget) {
            showSettings = EditorGUILayout.Foldout(showSettings, "Settings", true);
            if (showSettings) {
                EditorGUI.indentLevel++;

                //ShowRealObjectsInspector();

                //touchInteractionProp.boolValue = EditorGUILayout.Toggle("Touch Interaction", touchInteractionProp.boolValue);

                PhysicsInspector();

                EditorGUI.indentLevel--;
            }
        }
        
        protected void ShowRealObjectsInspector() {
            SerializedProperty showRealObjectsProp = serializedObject.FindProperty("showRealObjects");
            GUIContent text = new GUIContent(
                "Show Real Objects",
                "Shows real physical controllers at their actual location"
                );

            bool showRealObjects = EditorGUILayout.Toggle(text, showRealObjectsProp.boolValue);
            if (showRealObjects != handTarget.showRealObjects) {
                showRealObjectsProp.boolValue = showRealObjects;
                //controllerTarget.ShowSensors(showRealObjects);
                handTarget.showRealObjects = showRealObjects;
            }
        }

        protected void PhysicsInspector() {
            SerializedProperty physicsProp = serializedObject.FindProperty("physics");
            GUIContent text = new GUIContent(
                "Physics",
                "Enables collisions and physics on grabbed objects"
                );
            physicsProp.boolValue = EditorGUILayout.Toggle(text, physicsProp.boolValue);
        }

        #endregion

        #region Other
        //private SerializedProperty grabbedObjectProp;
        private void InitOther() {
            //grabbedObjectProp = serializedObject.FindProperty("grabbedObject");
        }

        private void TouchedObjectInspector(PawnHand handTarget) {
            handTarget.touchedObject = (GameObject)EditorGUILayout.ObjectField("Touched Object", handTarget.touchedObject, typeof(GameObject), true);
        }

        private void GrabbedObjectInspector(PawnHand handTarget) {
            SerializedProperty grabbedObjectProp = serializedObject.FindProperty("grabbedObject");
            if (Application.isPlaying) {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField("Grabbed Object", grabbedObjectProp.objectReferenceValue, typeof(GameObject), false);
                EditorGUI.EndDisabledGroup();
            }
            else {
                SerializedProperty grabbedPrefabProp = serializedObject.FindProperty("grabbedPrefab");
                GameObject grabbedPrefab = (GameObject)EditorGUILayout.ObjectField("Grabbed Prefab", grabbedPrefabProp.objectReferenceValue, typeof(GameObject), false);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField("Grabbed Object", grabbedObjectProp.objectReferenceValue, typeof(GameObject), false);
                EditorGUI.EndDisabledGroup();

                if (grabbedPrefab != grabbedPrefabProp.objectReferenceValue) {
                    if (grabbedPrefab != null)
                        GrabPrefab(handTarget, grabbedObjectProp, grabbedPrefab);
                    else
                        LetGoObject(handTarget, grabbedObjectProp);                    
                }
                serializedObject.Update();
                grabbedPrefabProp.objectReferenceValue = grabbedPrefab;

            }
        }

        private void GrabPrefab(PawnHand handTarget, SerializedProperty grabbedObjectProp, GameObject prefab) {
            if (grabbedObjectProp.objectReferenceValue != null)
                LetGoObject(handTarget, grabbedObjectProp);

            GameObject obj = Instantiate(prefab, handTarget.transform.position, handTarget.transform.rotation);

            handTarget.Grab(obj, false);
            if (handTarget.grabbedObject == null)
                Debug.LogWarning("Could not grab object");
            else {
                grabbedObjectProp.objectReferenceValue = obj;
                Handle handle = handTarget.grabbedHandle;
                if (handle == null) {
#if pHUMANOID
                    Handle.Create(obj, handTarget);
#endif
                }
            }
        }

        private void LetGoObject(PawnHand handTarget, SerializedProperty grabbedObjectProp) {
            GameObject grabbedObject = (GameObject)grabbedObjectProp.objectReferenceValue;
            //HandTarget.NetworkedLetGo(handTarget);
            handTarget.LetGo();
            DestroyImmediate(grabbedObject, true);
            grabbedObjectProp.objectReferenceValue = null;
        }

        #endregion

        #region Events
        /*
        protected SerializedProperty touchEventProp;
        protected SerializedProperty grabEventProp;
        protected SerializedProperty poseEventProp;

        protected virtual void InitEvents() {
            touchEventProp = serializedObject.FindProperty("touchEvent");
            grabEventProp = serializedObject.FindProperty("grabEvent");
            poseEventProp = serializedObject.FindProperty("poseEvent");
        }

        protected int selectedEvent;

        protected bool showEvents;
        protected virtual void EventsInspector() {
            showEvents = EditorGUILayout.Foldout(showEvents, "Events", true);
            if (showEvents) {
                EditorGUI.indentLevel++;

                EditorGUILayout.BeginHorizontal();

                //Labels
                EditorGUILayout.BeginVertical(GUILayout.MinWidth(110));

                GUILayout.Space(3);
                EditorGUILayout.LabelField("Touch", GUILayout.Width(110));
                GUILayout.Space(1);
                EditorGUILayout.LabelField("Grab", GUILayout.Width(110));
                GUILayout.Space(1);
                EditorGUILayout.LabelField("Pose", GUILayout.Width(110));

                EditorGUILayout.EndVertical();

                // Buttons
                string[] buttonTexts = new string[3];
                buttonTexts[0] = Event.GetInputButtonLabel(controllerTarget.touchEvent.gameObjectEvent);
                buttonTexts[1] = Event.GetInputButtonLabel(controllerTarget.grabEvent.gameObjectEvent);
                buttonTexts[2] = Event.GetInputButtonLabel(controllerTarget.poseEvent.poseEvent);

                int oldFontSize = GUI.skin.button.fontSize;
                GUI.skin.button.fontSize = 9;
                selectedEvent = GUILayout.SelectionGrid(selectedEvent, buttonTexts, 1);
                GUI.skin.button.fontSize = oldFontSize;

                EditorGUILayout.EndHorizontal();

                // Details
                GUIStyle style = new GUIStyle(GUI.skin.label) {
                    fontStyle = FontStyle.Bold
                };
                EditorGUILayout.LabelField("Details", style, GUILayout.ExpandWidth(true));

                EditorGUI.indentLevel++;
                EventDetails(selectedEvent);
                EditorGUI.indentLevel--;

                EditorGUI.indentLevel--;
            }
        }

        protected void EventDetails(int selectedEvent) {
            switch (selectedEvent) {
                case 0:
                    GameObjectEvent_Editor.DetailsInspector(touchEventProp, "Touch");
                    break;
                case 1:
                    GameObjectEvent_Editor.DetailsInspector(grabEventProp, "Grab");
                    break;
                case 2:
                    PoseEvent_Editor.DetailsInspector(poseEventProp, "Pose");
                    break;
            }
        }
        */
        #endregion

        #region Buttons
        private void InteractionPointerButton(PawnHand handTarget) {
            InteractionPointer interactionPointer = handTarget.transform.GetComponentInChildren<InteractionPointer>();
            if (interactionPointer != null)
                return;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Interaction Pointer"))
                AddInteractionPointer();
            if (GUILayout.Button("Add Teleporter"))
                AddTeleporter();
            GUILayout.EndHorizontal();
        }

        private void AddInteractionPointer() {
            InteractionPointer pointer = InteractionPointer.Add(handTarget.transform, InteractionPointer.PointerType.Ray);
            pointer.transform.localPosition = Vector3.zero;
            pointer.transform.localRotation = Quaternion.identity;
            pointer.active = false;

            ControllerInput controllerInput = handTarget.pawn.GetComponent<ControllerInput>();
            if (controllerInput != null) {
                //ControllerEventHandlers button1Input = controllerInput.GetInputEvent(controllerTarget.isLeft, ControllerInput.SideButton.Button1);
                //ControllerEvent_Editor.SetBoolMethod(controllerInput.gameObject, button1Input, EventHandler.Type.OnChange, pointer.Activation);
                controllerInput.SetEventHandler(handTarget.isLeft, ControllerInput.SideButton.Button1, pointer.Activation);

                //ControllerEventHandlers trigger1Input = controllerInput.GetInputEvent(controllerTarget.isLeft, ControllerInput.SideButton.Trigger1);
                //ControllerEvent_Editor.SetBoolMethod(controllerInput.gameObject, trigger1Input, EventHandler.Type.OnChange, pointer.Click);
                controllerInput.SetEventHandler(handTarget.isLeft, ControllerInput.SideButton.Trigger1, pointer.Click);
            }
        }

        private void AddTeleporter() {
            Teleporter teleporter = Teleporter.Add(handTarget.transform);
            teleporter.transform.localPosition = Vector3.zero;
            teleporter.transform.localRotation = Quaternion.identity;
            teleporter.active = false;

            ControllerInput controllerInput = handTarget.pawn.GetComponent<ControllerInput>();
            if (controllerInput != null) {
                //ControllerEventHandlers button1Input = controllerInput.GetInputEvent(controllerTarget.isLeft, ControllerInput.SideButton.Button1);
                //ControllerEvent_Editor.SetBoolMethod(controllerInput.gameObject, button1Input, EventHandler.Type.OnChange, teleporter.Activation);
                controllerInput.SetEventHandler(handTarget.isLeft, ControllerInput.SideButton.Button1, teleporter.Activation);

                //ControllerEventHandlers trigger1Input = controllerInput.GetInputEvent(controllerTarget.isLeft, ControllerInput.SideButton.Trigger1);
                //ControllerEvent_Editor.SetBoolMethod(controllerInput.gameObject, trigger1Input, EventHandler.Type.OnChange, teleporter.Click);
                controllerInput.SetEventHandler(handTarget.isLeft, ControllerInput.SideButton.Trigger1, teleporter.Click);
            }
        }
        #endregion

        #endregion

        public abstract class TargetProps {
            public SerializedProperty enabledProp;
            public SerializedProperty sensorTransformProp;
            public SerializedProperty sensor2TargetPositionProp;
            public SerializedProperty sensor2TargetRotationProp;

            public PawnHand controllerTarget;
            public ControllerSensor sensor;

            public TargetProps(SerializedObject serializedObject, ControllerSensor _sensor, PawnHand _controllerTarget, string unitySensorName) {
                enabledProp = serializedObject.FindProperty(unitySensorName + ".enabled");
                sensorTransformProp = serializedObject.FindProperty(unitySensorName + ".sensorTransform");
                sensor2TargetPositionProp = serializedObject.FindProperty(unitySensorName + ".sensor2TargetPosition");
                sensor2TargetRotationProp = serializedObject.FindProperty(unitySensorName + ".sensor2TargetRotation");

                controllerTarget = _controllerTarget;
                sensor = _sensor;

                sensor.target = controllerTarget;
            }

            public virtual void SetSensor2Target() {
                //sensor.SetSensor2Target();
            }

            public abstract void Inspector();
        }
    }
}
