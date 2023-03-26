using UnityEditor;
using UnityEngine;

namespace Passer {
    using Pawn;
    using Tracking;

    [CustomEditor(typeof(PawnControl), true)]
    public class PawnControl_Editor : Editor {
        protected PawnControl pawn;

        #region Enable

        public virtual void OnEnable() {
            pawn = (PawnControl)target;

            if (pawn.disconnectInstances) {
#if UNITY_2018_3_OR_NEWER
                PrefabInstanceStatus prefabInstanceStatus = PrefabUtility.GetPrefabInstanceStatus(pawn.gameObject);
                if (prefabInstanceStatus == PrefabInstanceStatus.Connected) {
                    Debug.Log("Unpacking Prefab Instance for Pawn");
                    PrefabUtility.UnpackPrefabInstance(pawn.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
                }
#else
                if (PrefabUtility.GetPrefabObject(pawn.gameObject) != null) {
                    PrefabType prefabType = PrefabUtility.GetPrefabType(pawn);
                    if (prefabType != PrefabType.Prefab) {
                        // Only when it is not a prefab
                        Debug.Log("Breaking Prefab Instance for Pawn");
                        PrefabUtility.DisconnectPrefabInstance(pawn.gameObject);
                    }
                }
#endif
            }

            // Maybe switch his to an OnInitialize???
            if (!pawn.isRemote) {
                //if (pawn.realWorld == null)
                //    pawn.realWorld = PawnControl.GetRealWorld(pawn.transform);
#if pUNITYXR
                if (pawn.unity == null) {
                    pawn.unity = UnityXR.Get(pawn.realWorld);
                }
#else
                //if (pawn.unityTracker == null) {
                //    pawn.unityTracker = UnityTrackerComponent.Get(pawn.realWorld);
                //}
#endif
            }

            //CheckCameraTarget();
            if (pawn.leftHandTarget != null)
                pawn.leftHandTarget.InitSensors();
            if (pawn.rightHandTarget != null)
                pawn.rightHandTarget.InitSensors();

            InitMovement();
            InitSettings();
        }

        #endregion

        #region Disable

        public virtual void OnDisable() {
        }

        #endregion

        #region Inspector

        public override void OnInspectorGUI() {
            serializedObject.Update();

            TargetsInspector();
            TrackerInspector();
            MovementInspector();
            SettingsInspector();
            Buttons();

            serializedObject.ApplyModifiedProperties();
        }

        #region Targets

        protected bool showTargets;

        protected void TargetsInspector() {
            SerializedProperty headTargetProp = serializedObject.FindProperty("_headTarget");
            headTargetProp.objectReferenceValue = PawnHead_Editor.Inspector("Head Target", pawn);

#if !pUNITYXR && !UNITY_2020_1_OR_NEWER
            if (PlayerSettings.virtualRealitySupported) {
                SerializedProperty leftHandProp = serializedObject.FindProperty("_leftHandTarget");
                leftHandProp.objectReferenceValue = PawnHand_Editor.Inspector("Left Hand Target", pawn, Side.Left);
                SerializedProperty rightHandProp = serializedObject.FindProperty("_rightHandTarget");
                rightHandProp.objectReferenceValue = PawnHand_Editor.Inspector("Right Hand Target", pawn, Side.Right);
            }
#endif
        }

        #endregion

        #region Trackers

        protected void TrackerInspector() {
#if pUNITYXR && hLEGACYXR
            UnityVR_Editor.CleanupFirstPersonCamera(pawn);
#endif

            pawn.CheckTrackers();
            if (pawn.headTarget != null)
                pawn.headTarget.CheckSensors();
            if (pawn.leftHandTarget != null)
                pawn.leftHandTarget.CheckSensors();
            if (pawn.rightHandTarget != null)
                pawn.rightHandTarget.CheckSensors();
        }

        #endregion Trakcers

        #region Movement

        protected virtual void InitMovement() {
        }

        protected bool showMovement = false;
        protected virtual void MovementInspector() {
            GUIContent text = new GUIContent(
                "Movement",
                "Settings related to moving the pawn around"
                );
            showMovement = EditorGUILayout.Foldout(showMovement, text, true);
            if (showMovement) {
                EditorGUI.indentLevel++;
                ForwardSpeedInspector();
                BackwardSpeedInspector();
                SidewardSpeedInspector();
                MaxAccelerationInspector();
                RotationSpeedInspector();
                ProximitySpeedInspector();
                EditorGUI.indentLevel--;
            }
        }

        protected virtual void ForwardSpeedInspector() {
            SerializedProperty forwardSpeedProp = serializedObject.FindProperty("forwardSpeed");
            GUIContent text = new GUIContent(
                "Forward Speed",
                "Maximum forward speed in units(meters)/second"
                );
            forwardSpeedProp.floatValue = EditorGUILayout.FloatField(text, forwardSpeedProp.floatValue);
        }

        protected virtual void BackwardSpeedInspector() {
            SerializedProperty backwardSpeedProp = serializedObject.FindProperty("backwardSpeed");
            GUIContent text = new GUIContent(
                "Backward Speed",
                "Maximum backward speed in units(meters)/second"
                );
            backwardSpeedProp.floatValue = EditorGUILayout.FloatField(text, backwardSpeedProp.floatValue);
        }

        protected virtual void SidewardSpeedInspector() {
            SerializedProperty sidewardSpeedPop = serializedObject.FindProperty("sidewardSpeed");
            GUIContent text = new GUIContent(
                "Sideward Speed",
                "Maximum sideward speed in units(meters)/second"
                );
            sidewardSpeedPop.floatValue = EditorGUILayout.FloatField(text, sidewardSpeedPop.floatValue);
        }

        protected virtual void MaxAccelerationInspector() {
            SerializedProperty maxAccelerationProp = serializedObject.FindProperty("maxAcceleration");
            GUIContent text = new GUIContent(
                "Maximum Acceleration",
                "Maximum acceleration in units(meters)/second/second, 0 = no maximum acceleration"
                );
            maxAccelerationProp.floatValue = EditorGUILayout.FloatField(text, maxAccelerationProp.floatValue);
        }

        protected virtual void RotationSpeedInspector() {
            SerializedProperty rotationSpeedProp = serializedObject.FindProperty("rotationSpeed");
            GUIContent text = new GUIContent(
                "Rotation Speed",
                "Maximum rotational speed in degrees/second"
                );
            rotationSpeedProp.floatValue = EditorGUILayout.FloatField(text, rotationSpeedProp.floatValue);
        }

        protected virtual void ProximitySpeedInspector() {
            SerializedProperty proximitySpeedProp = serializedObject.FindProperty("proximitySpeed");
            GUIContent text = new GUIContent(
                "Proximity Speed",
                "Decreases movement speed when the pawn is close to static objects to reduce motion sickness"
                );
            proximitySpeedProp.boolValue = EditorGUILayout.Toggle(text, proximitySpeedProp.boolValue);
        }



        #endregion

        #region Settings

        protected SerializedProperty showRealObjectsProp;

        protected virtual void InitSettings() {
            showRealObjectsProp = serializedObject.FindProperty("showRealObjects");

            ShowRealWorldObjects();
        }

        protected bool showSettings = false;
        protected virtual void SettingsInspector() {
            GUIContent text = new GUIContent(
                "Settings",
                "To contract various aspects of the script"
                );
            showSettings = EditorGUILayout.Foldout(showSettings, text, true);

            if (showSettings) {
                EditorGUI.indentLevel++;

                if (!pawn.isRemote) {
                    RealWorldObjects(pawn);

                    PhysicsSetting();
                    UseGravitySetting();
                    BodyPullSetting();
                    //HapticsSetting();
                    //ProximitySpeedSetting();
                    CalibrateAtStartSetting();
                    StartPositionSetting();
                    ScalingSetting();
                }
                DontDestroySetting();
                //IsRemoteSetting();
                if (IsPrefab(pawn))
                    DisconnectInstancesSetting();

                EditorGUI.indentLevel--;
            }
        }

        protected void RealWorldObjects(PawnControl pawn) {
            SerializedProperty showRealObjectsProp = serializedObject.FindProperty("showRealObjects");
            bool lastShowRealObjects = showRealObjectsProp.boolValue;

            GUIContent text = new GUIContent(
                "Show Real Objects",
                "Shows real physical objects like trackers, controllers and camera's at their actual location"
                );
            showRealObjectsProp.boolValue = EditorGUILayout.Toggle(text, lastShowRealObjects);

            if (!lastShowRealObjects && showRealObjectsProp.boolValue) { // we turned real world objects on
                ShowRealWorldObjects();
            }
        }

        protected virtual void ShowRealWorldObjects() {
            if (pawn.leftHandTarget != null)
                //pawn.leftControllerTarget.ShowSensors(pawn.showRealObjects);
                pawn.leftHandTarget.showRealObjects = pawn.showRealObjects;
            if (pawn.rightHandTarget != null)
                //pawn.rightControllerTarget.ShowSensors(pawn.showRealObjects);
                pawn.rightHandTarget.showRealObjects = pawn.showRealObjects;
        }

        protected virtual void UseGravitySetting() {
            SerializedProperty useGravityProp = serializedObject.FindProperty("useGravity");
            GUIContent text = new GUIContent(
                "Use Gravity",
                "Implements downward motion when the character is not on solid ground"
                );
            useGravityProp.boolValue = EditorGUILayout.Toggle(text, useGravityProp.boolValue);
        }

        protected virtual void PhysicsSetting() {
            // Physics cannot bet changes during runtime
            EditorGUI.BeginDisabledGroup(Application.isPlaying);

            SerializedProperty physicsProp = serializedObject.FindProperty("physics");
            GUIContent text = new GUIContent(
                "Physics",
                "Enables collisions of the pawn with the environment using the physics engine"
                );
            physicsProp.boolValue = EditorGUILayout.Toggle(text, physicsProp.boolValue);

#if !pUNITYXR && !UNITY_2020_1_OR_NEWER
            if (physicsProp.boolValue && PlayerSettings.virtualRealitySupported && Time.fixedDeltaTime > 0.01F) {
                EditorGUILayout.HelpBox("Project Settings->Time->Fixed Timestep is too high.\nPlease set to 0.01 or smaller for stable physics in VR.", MessageType.Warning);
            }
#endif

            EditorGUI.EndDisabledGroup();
        }

        protected virtual void BodyPullSetting() {
            SerializedProperty bodyPullProp = serializedObject.FindProperty("bodyPull");
            GUIContent text = new GUIContent(
                "Body Pull",
                "Moves the body when grabbing static objects"
                );
            bodyPullProp.boolValue = EditorGUILayout.Toggle(text, bodyPullProp.boolValue);
        }

        protected void HapticsSetting() {
            SerializedProperty hapticsProp = serializedObject.FindProperty("haptics");
            GUIContent text = new GUIContent(
                "Haptics",
                "Uses haptic feedback when colliding with objects"
                );
            hapticsProp.boolValue = EditorGUILayout.Toggle(text, hapticsProp.boolValue);
        }

        protected virtual void CalibrateAtStartSetting() {
            SerializedProperty calibrateAtStartProp = serializedObject.FindProperty("calibrateAtStart");
            GUIContent text = new GUIContent(
                "Calibrate at Start",
                "Will calibrate the pawn when the tracking starts."
                );
            calibrateAtStartProp.boolValue = EditorGUILayout.Toggle(text, calibrateAtStartProp.boolValue);
        }

        protected virtual void ScalingSetting() {
            SerializedProperty scalingProp = serializedObject.FindProperty("avatarMatching");
            GUIContent text = new GUIContent(
                "Scaling",
                "Determines how differences between the player size and the character are resolved"
                );
            scalingProp.intValue = (int)(PawnControl.MatchingType)EditorGUILayout.EnumPopup(text, (PawnControl.MatchingType)scalingProp.intValue);
        }

        protected virtual void StartPositionSetting() {
            SerializedProperty startPositionProp = serializedObject.FindProperty("startPosition");
            GUIContent text = new GUIContent(
                "Start Position",
                "Determines the start position of the player"
                );
            startPositionProp.intValue = (int)(PawnControl.StartPosition)EditorGUILayout.EnumPopup(text, (PawnControl.StartPosition)startPositionProp.intValue);
        }

        protected void DontDestroySetting() {
            SerializedProperty dontDestroyProp = serializedObject.FindProperty("dontDestroyOnLoad");
            GUIContent text = new GUIContent(
                "Don't Destroy on Load",
                "Ensures that the pawn survives a scene change"
                );
            dontDestroyProp.boolValue = EditorGUILayout.Toggle(text, dontDestroyProp.boolValue);
        }

        protected void DisconnectInstancesSetting() {
            SerializedProperty disconnectInstancesProp = serializedObject.FindProperty("disconnectInstances");
            GUIContent text = new GUIContent(
                "Disconnect Instances",
                "Disconnects instances from prefab when instantiating"
                );
            disconnectInstancesProp.boolValue = EditorGUILayout.Toggle(text, disconnectInstancesProp.boolValue);
        }

        protected virtual void IsRemoteSetting() {
            SerializedProperty isRemoteProp = serializedObject.FindProperty("isRemote");
            GUIContent text = new GUIContent(
                "Is Remote",
                "This pawn is used for remote clients"
                );
            isRemoteProp.boolValue = EditorGUILayout.Toggle(text, isRemoteProp.boolValue);
        }

        #endregion

        #region Buttons
        private void Buttons() {
            GUILayout.BeginHorizontal();
            if (Application.isPlaying && GUILayout.Button("Calibrate"))
                pawn.Calibrate();
            GUILayout.EndHorizontal();
        }
        #endregion

        public static bool IsPrefab(PawnControl pawn) {
#if UNITY_2018_3_OR_NEWER
            if (PrefabUtility.IsPartOfAnyPrefab(pawn.gameObject))
                return false;
            else
                return true;
#else
            PrefabType prefabType = PrefabUtility.GetPrefabType(pawn);
            if (prefabType == PrefabType.Prefab)
                return true;
            else
                return false;
#endif
        }


        #endregion

    }
}