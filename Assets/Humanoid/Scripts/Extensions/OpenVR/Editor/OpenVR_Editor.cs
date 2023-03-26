using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#else
using UnityEngine.VR;
#endif
using Passer.Tracking;

namespace Passer.Humanoid {

    public class OpenVR_Editor : Tracker_Editor {

#if hOPENVR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)

        #region Tracker

        public class TrackerProps : HumanoidControl_Editor.HumanoidTrackerProps {

#if hVIVEHAND
            private SerializedProperty handTrackingProp;
#endif
#if hVIVETRACKER
            private ViveTracker_Editor.TrackerProps viveTrackerProps;
#endif

            public TrackerProps(SerializedObject serializedObject, HumanoidControl_Editor.HumanoidTargetObjs targetObjs, OpenVRHumanoidTracker _openVR)
                : base(serializedObject, targetObjs, _openVR, "openVR") {
                tracker = _openVR;

                headSensorProp = targetObjs.headTargetObj.FindProperty("openVR");
                leftHandSensorProp = targetObjs.leftHandTargetObj.FindProperty("openVR");
                rightHandSensorProp = targetObjs.rightHandTargetObj.FindProperty("openVR");
#if hVIVEHAND
                handTrackingProp = serializedObject.FindProperty("openVR.handTracking");
#endif
#if hVIVETRACKER
                viveTrackerProps = new ViveTracker_Editor.TrackerProps(serializedObject, targetObjs, _openVR);
#endif
                CheckStreamingAssets();
            }

            public override void Inspector(HumanoidControl humanoid) {
                bool openVRSupported = OpenVRSupported();
                if (openVRSupported) {

#if !UNITY_2020_1_OR_NEWER
                    humanoid.openVR.CheckTracker(humanoid);
                    humanoid.headTarget.openVR.CheckSensor(humanoid.headTarget);
                    humanoid.leftHandTarget.openVR.CheckSensor(humanoid.leftHandTarget);
                    humanoid.rightHandTarget.openVR.CheckSensor(humanoid.rightHandTarget);

#if pUNITYXR || hLEGACYXR
                    if (humanoid.headTarget.unity.enabled)
                        humanoid.openVR.enabled = true;
#endif
#if hLEGACYXR
                    EditorGUI.BeginDisabledGroup(humanoid.headTarget.unity.enabled);
                    Inspector(humanoid, "TrackerModels/Lighthouse");
                    EditorGUI.EndDisabledGroup();
                    humanoid.unity.enabled = humanoid.openVR.enabled;
#endif
#else
#if hVIVETRACKER
                    humanoid.openVR.enabled = humanoid.unityXR.enabled;
#else
                    humanoid.openVR.enabled = false;
#endif
#endif

#if hVIVEHAND
                    using (new EditorGUILayout.HorizontalScope()) {
                        handTrackingProp.boolValue = EditorGUILayout.ToggleLeft("Vive Hand Tracking", handTrackingProp.boolValue);
                        if (handTrackingProp.boolValue == true) {
                            humanoid.openVR.enabled = true;
                            humanoid.openVR.CheckTracker(humanoid);
                            ViveHandSkeleton.CheckGestureProvider(humanoid.openVR);
                            if (Application.isPlaying) {
                                EditorGUI.indentLevel--;
                                EditorGUILayout.EnumPopup(humanoid.leftHandTarget.openVR.handSkeleton.status);
                                EditorGUILayout.EnumPopup(humanoid.rightHandTarget.openVR.handSkeleton.status);
                                EditorGUI.indentLevel++;
                            }
                        }
                    }

#endif
#if hVIVETRACKER
                    viveTrackerProps.Inspector(humanoid);
#endif
                }
                else
                    enabledProp.boolValue = false;
            }

            public void CheckStreamingAssets() {
                string streamingAssetsPath = Application.streamingAssetsPath;

                if (!Directory.Exists(streamingAssetsPath))
                    AssetDatabase.CreateFolder("Assets", "StreamingAssets");

                string steamVrPath = streamingAssetsPath + "/SteamVR";
                if (!Directory.Exists(steamVrPath))
                    AssetDatabase.CreateFolder("Assets/StreamingAssets", "SteamVR");

                CopyFilesToPath(steamVrPath, false);
            }

            public override void InitControllers() {
                base.InitControllers();
#if hVIVETRACKER
                viveTrackerProps.InitControllers();
#endif
            }

            public override void RemoveControllers() {
                base.RemoveControllers();
#if hVIVETRACKER
                viveTrackerProps.RemoveControllers();
#endif
            }

            public override void SetSensors2Target() {
                base.SetSensors2Target();
#if hVIVETRACKER
                viveTrackerProps.SetSensors2Target();
#endif
            }
        }

        #endregion

        #region Head
        public class HeadTargetProps : HeadTarget_Editor.TargetProps {

#if hVIVEFACE
            protected readonly SerializedProperty viveFaceProp;
#endif

            public HeadTargetProps(SerializedObject serializedObject, HeadTarget headTarget)
                : base(serializedObject, headTarget.openVR, headTarget, "openVR") {

#if hVIVEFACE
                viveFaceProp = serializedObject.FindProperty("face.vive");
#endif
            }

            public override void Inspector() {
#if UNITY_2020_1_OR_NEWER
                return;
#else

                if (!headTarget.humanoid.openVR.enabled || !OpenVRSupported())
                    return;

                CheckHmdComponent(headTarget);

                enabledProp.boolValue = HumanoidTarget_Editor.ControllerInspector(headTarget.openVR, headTarget);
                headTarget.openVR.enabled = enabledProp.boolValue;
                headTarget.openVR.CheckSensorTransform();
                if (!Application.isPlaying) {
                    headTarget.openVR.SetSensor2Target();
                    headTarget.openVR.ShowSensor(headTarget.humanoid.showRealObjects && headTarget.showRealObjects);
                }

                if (enabledProp.boolValue) {
                    EditorGUI.indentLevel++;
                    sensorTransformProp.objectReferenceValue = (Transform)EditorGUILayout.ObjectField("Tracker Transform", headTarget.openVR.sensorTransform, typeof(Transform), true);
#if hFACE && hVIVEFACE
                    using (new EditorGUILayout.HorizontalScope()) {
                        SerializedProperty faceEnabledProp = viveFaceProp.FindPropertyRelative("faceTracking");
                        faceEnabledProp.boolValue = EditorGUILayout.ToggleLeft("Vive Face Tracking", faceEnabledProp.boolValue);
                        if (Application.isPlaying && faceEnabledProp.boolValue) {
                            EditorGUI.indentLevel--;
                            EditorGUILayout.EnumPopup(headTarget.face.vive.faceTrackingStatus);
                            EditorGUI.indentLevel++;
                        }
                    }
                    using (new EditorGUILayout.HorizontalScope()) {
                        SerializedProperty eyeEnabledProp = viveFaceProp.FindPropertyRelative("eyeTracking");
                        eyeEnabledProp.boolValue = EditorGUILayout.ToggleLeft("Vive Eye Tracking", eyeEnabledProp.boolValue);
                        if (Application.isPlaying && eyeEnabledProp.boolValue) {
                            EditorGUI.indentLevel--;
                            EditorGUILayout.EnumPopup(headTarget.face.vive.eyeTrackingStatus);
                            EditorGUI.indentLevel++;
                        }
                    }

#endif
                    EditorGUI.indentLevel--;
                }
#endif
            }

            protected static void CheckHmdComponent(HeadTarget headTarget) {
                if (headTarget.openVR.sensorTransform == null)
                    return;

                OpenVRHmd sensorComponent = headTarget.openVR.sensorTransform.GetComponent<OpenVRHmd>();
                if (sensorComponent == null)
                    headTarget.openVR.sensorTransform.gameObject.AddComponent<OpenVRHmd>();
            }
        }

        #region HMD Component
        [CustomEditor(typeof(OpenVRHmd))]
        public class OpenVRHmd_Editor : Editor {
            OpenVRHmd sensorComponent;

            private void OnEnable() {
                sensorComponent = (OpenVRHmd)target;
            }

            public override void OnInspectorGUI() {
                serializedObject.Update();

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.EnumPopup("Status", sensorComponent.status);
                EditorGUILayout.FloatField("Position Confidence", sensorComponent.positionConfidence);
                EditorGUILayout.FloatField("Rotation Confidence", sensorComponent.rotationConfidence);
                EditorGUILayout.Space();
                EditorGUILayout.IntField("Tracker Id", sensorComponent.trackerId);
                EditorGUI.EndDisabledGroup();

                serializedObject.ApplyModifiedProperties();
            }
        }
        #endregion

        #endregion

        #region Hand

        public class HandTargetProps : HandTarget_Editor.TargetProps {

            protected SerializedProperty useSkeletalInputProp;
#if hVIVEHAND
            //SerializedProperty handTrackingProp;
            protected readonly SerializedProperty skeletonProp;
#endif

            public HandTargetProps(SerializedObject serializedObject, HandTarget handTarget)
                : base(serializedObject, handTarget.openVR, handTarget, "openVR") {

                useSkeletalInputProp = serializedObject.FindProperty("openVR.useSkeletalInput");
#if hVIVEHAND
                //handTrackingProp = serializedObject.FindProperty("openVR.handTracking");
                skeletonProp = serializedObject.FindProperty("openVR.handSkeleton");
#endif
            }

            public override void Inspector() {
#if UNITY_2020_1_OR_NEWER
                return;
#else
                if (!handTarget.humanoid.openVR.enabled || !OpenVRSupported())
                    return;

                CheckControllerComponent(handTarget);
                handTarget.openVR.CheckSensor(handTarget);

                enabledProp.boolValue = HumanoidTarget_Editor.ControllerInspector(handTarget.openVR, handTarget);
                handTarget.openVR.enabled = enabledProp.boolValue;
                handTarget.openVR.CheckSensorTransform();
                if (!Application.isPlaying) {
                    handTarget.openVR.SetSensor2Target();
                    handTarget.openVR.ShowSensor(handTarget.humanoid.showRealObjects && handTarget.showRealObjects);
                }

                if (enabledProp.boolValue) {
                    EditorGUI.indentLevel++;
                    // For this, the controller meshes need to have the same origin which is currently not the case
                    //controllerTypeProp.intValue = (int)(OpenVRController.ControllerType)EditorGUILayout.EnumPopup("Controller Type", handTarget.openVR.controllerType);
                    sensorTransformProp.objectReferenceValue = (Transform)EditorGUILayout.ObjectField("Tracker Transform", handTarget.openVR.sensorTransform, typeof(Transform), true);
#if hVIVEHAND
                    if (handTarget.humanoid.openVR.handTracking)
                        skeletonProp.objectReferenceValue = (HandSkeleton)EditorGUILayout.ObjectField("Skeleton", skeletonProp.objectReferenceValue, typeof(HandSkeleton), true);
                    else
#endif
                        useSkeletalInputProp.boolValue = EditorGUILayout.Toggle("Use Skeletal Input", useSkeletalInputProp.boolValue);
                    EditorGUI.indentLevel--;
                }
#endif
            }

            protected static void CheckControllerComponent(HandTarget handTarget) {
                if (handTarget.openVR.sensorTransform == null)
                    return;

                OpenVRController sensorComponent = handTarget.openVR.sensorTransform.GetComponent<OpenVRController>();
                if (sensorComponent == null)
                    sensorComponent = handTarget.openVR.sensorTransform.gameObject.AddComponent<OpenVRController>();
                sensorComponent.isLeft = handTarget.isLeft;
            }

#if hVIVEHAND
            //protected static void CheckSkeletonComponent(HandTarget handTarget) {
            //    if (handTarget.openVR.handSkeleton == null) {
            //        handTarget.openVR.handSkeleton = handTarget.openVR.FindHandSkeleton(handTarget.isLeft);
            //        if (handTarget.openVR.handSkeleton == null)
            //            handTarget.openVR.handSkeleton = handTarget.openVR.CreateHandSkeleton(handTarget.isLeft, handTarget.showRealObjects);
            //    }
            //}
#endif
        }

        #region Controller Component
        [CustomEditor(typeof(OpenVRController))]
        public class OpenVRController_Editor : Editor {
            OpenVRController controllerComponent;

            private void OnEnable() {
                controllerComponent = (OpenVRController)target;
            }

            public override void OnInspectorGUI() {
                serializedObject.Update();

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.EnumPopup("Status", controllerComponent.status);
                EditorGUILayout.FloatField("Position Confidence", controllerComponent.positionConfidence);
                EditorGUILayout.FloatField("Rotation Confidence", controllerComponent.rotationConfidence);
                EditorGUILayout.IntField("Tracker Id", controllerComponent.trackerId);
                EditorGUILayout.Space();
                EditorGUILayout.Toggle("Is Left", controllerComponent.isLeft);
                EditorGUILayout.Vector3Field("Joystick", controllerComponent.joystick);
                EditorGUILayout.Vector3Field("Touchpad", controllerComponent.touchpad);
                EditorGUILayout.Slider("Trigger", controllerComponent.trigger, -1, 1);
                EditorGUILayout.Slider("Grip", controllerComponent.grip, -1, 1);
                EditorGUILayout.Slider("Button A", controllerComponent.aButton, -1, 1);
                EditorGUILayout.Slider("Button B", controllerComponent.bButton, -1, 1);
                EditorGUI.EndDisabledGroup();
                // For this, the controller meshes need to have the same origin which is currently not the case
                //controllerTypeProp.intValue = (int)(OPenVRController.ControllerType)EditorGUILayout.EnumPopup("View Controller Type", controllerComponent.controllerType);

                serializedObject.ApplyModifiedProperties();

                controllerComponent.show = EditorGUILayout.Toggle("Show", controllerComponent.show);
            }
        }
        #endregion

        #endregion

#endif

        public static bool OpenVRSupported() {
#if UNITY_2017_2_OR_NEWER
            string[] supportedDevices = XRSettings.supportedDevices;
#else
            string[] supportedDevices = VRSettings.supportedDevices;
#endif
            foreach (string supportedDevice in supportedDevices) {
                if (supportedDevice == "OpenVR" || supportedDevice == "OpenVR Display")
                    return true;
            }
            return false;
        }

        /*
        [PostProcessBuildAttribute(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject) {
            FileInfo fileInfo = new FileInfo(pathToBuiltProject);
            string buildPath = fileInfo.Directory.FullName;


            CopyFilesToPath(buildPath, true);
        }

        private static void CopyManifestsToBuild(string buildPath) {

        }
        */
        public static void CopyFilesToPath(string toPath, bool overwrite) {
            string humanoidPath = Configuration_Editor.FindHumanoidFolder();
            string[] files = GetFilesToCopy();

            foreach (string file in files) {
                string fullFile = Application.dataPath + humanoidPath + "Extensions/OpenVR/" + file;
                FileInfo bindingInfo = new FileInfo(fullFile);
                string newFilePath = Path.Combine(toPath, bindingInfo.Name);

                bool exists = false;
                if (File.Exists(newFilePath))
                    exists = true;

                if (exists) {
                    if (overwrite) {
                        FileInfo existingFile = new FileInfo(newFilePath) {
                            IsReadOnly = false
                        };
                        existingFile.Delete();

                        File.Copy(fullFile, newFilePath);

                        //Debug.Log("Copied (overwrote) manifest to build: " + newFilePath);
                    }
                    //else
                    //    Debug.Log("Skipped writing existing manifest in build: " + newFilePath);
                }
                else {
                    File.Copy(fullFile, newFilePath);

                    //Debug.Log("Copied manifest to buld: " + newFilePath);
                }

            }
        }

        private static string[] GetFilesToCopy() {
            string[] files = {
                "actions.json",
                "binding_vive.json",
                "binding_vive_pro.json",
                "bindings_holographic_controller.json",
                "bindings_knuckles.json",
                "bindings_oculus_touch.json",
                "bindings_vive_controller.json"
            };
            return files;
        }

    }
}