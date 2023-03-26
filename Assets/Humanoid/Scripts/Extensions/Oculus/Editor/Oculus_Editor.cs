using System.IO;
using UnityEditor;
using UnityEngine;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#else
using UnityEngine.VR;
#endif
using Passer.Tracking;

namespace Passer {
    using Humanoid;

    public class Oculus_Editor : Tracker_Editor {

#if hOCULUS && (UNITY_STANDALONE_WIN || UNITY_ANDROID)

#if UNITY_ANDROID && hOCHAND
        private readonly static System.Version minHandTrackingVersion = new System.Version(1, 44, 0);
#endif

        #region Tracker

        public class TrackerProps : HumanoidControl_Editor.HumanoidTrackerProps {

#if UNITY_ANDROID
            SerializedProperty deviceTypeProp;
            SerializedProperty handTrackingProp;
#endif

            public TrackerProps(SerializedObject serializedObject, HumanoidControl_Editor.HumanoidTargetObjs targetObjs, OculusHumanoidTracker _oculus)
                : base(serializedObject, targetObjs, _oculus, "oculus") {
                tracker = _oculus;

                headSensorProp = targetObjs.headTargetObj.FindProperty("oculus");
                leftHandSensorProp = targetObjs.leftHandTargetObj.FindProperty("oculus");
                rightHandSensorProp = targetObjs.rightHandTargetObj.FindProperty("oculus");

#if UNITY_ANDROID
                handTrackingProp = serializedObject.FindProperty("oculus.handTracking");
                deviceTypeProp = serializedObject.FindProperty("oculus.androidDeviceType");

                OculusHumanoidTracker.AndroidDeviceType androidDeviceType = (OculusHumanoidTracker.AndroidDeviceType)deviceTypeProp.intValue;
                if (androidDeviceType == OculusHumanoidTracker.AndroidDeviceType.OculusQuest)
                    CheckQuestManifest();
#endif
            }

            public override void Inspector(HumanoidControl humanoid) {
#if !UNITY_2020_1_OR_NEWER
                bool oculusSupported = OculusSupported();
                if (oculusSupported) {
#if hLEGACYXR
                    if (humanoid.headTarget.unity.enabled)
                        humanoid.oculus.enabled = true;

                    EditorGUI.BeginDisabledGroup(humanoid.headTarget.unity.enabled);
#else
                    humanoid.oculus.CheckTracker(humanoid);
                    humanoid.headTarget.oculus.CheckSensor(humanoid.headTarget);
                    humanoid.leftHandTarget.oculus.CheckSensor(humanoid.leftHandTarget);
                    humanoid.rightHandTarget.oculus.CheckSensor(humanoid.rightHandTarget);
#endif
                    Inspector(humanoid, "Oculus");
#if hLEGACYXR
                    EditorGUI.EndDisabledGroup();
#endif
#if UNITY_ANDROID && hOCHAND
                    EditorGUI.indentLevel++;
                    deviceTypeProp.intValue = (int)(OculusHumanoidTracker.AndroidDeviceType)EditorGUILayout.EnumPopup("Device Type", (OculusHumanoidTracker.AndroidDeviceType)deviceTypeProp.intValue);
                    handTrackingProp.boolValue = EditorGUILayout.Toggle("Hand Tracking", handTrackingProp.boolValue);
#if UNITY_EDITOR_OSX
                    if (handTrackingProp.boolValue) {
                        EditorGUILayout.HelpBox(
                            "Hand tracking required at least OVR Plugin version 1.44 or higher\n" +
                            "Install the latetest version using the Oculus Integration package",
                            MessageType.Warning);
                    }
#else
                    if (handTrackingProp.boolValue && !(Humanoid.Tracking.OculusDevice.version >= minHandTrackingVersion)) {
                        EditorGUILayout.HelpBox(
                            "Hand tracking required at least OVR Plugin version 1.44 or higher\n" +
                            "Install the latetest version using the Oculus Integration package",
                            MessageType.Error);
                    }
#endif
                    EditorGUI.indentLevel--;
#endif
                }
                else
                    enabledProp.boolValue = false;
#else
#if UNITY_ANDROID && hOCHAND
                if (humanoid.unityXR.enabled)
                    handTrackingProp.boolValue = EditorGUILayout.ToggleLeft("Oculus Hand Tracking", handTrackingProp.boolValue);
#endif
#endif
            }

            protected virtual void CheckQuestManifest() {
                string manifestPath = Application.dataPath + "/Plugins/Android/AndroidManifest.xml";
                FileInfo fileInfo = new FileInfo(manifestPath);
                fileInfo.Directory.Create();
                bool manifestAvailable = File.Exists(manifestPath);
                if (manifestAvailable)
                    return;

                string humanoidPath = Configuration_Editor.FindHumanoidFolder();
                string questManifestPath = Application.dataPath + humanoidPath + "Extensions/Oculus/QuestManifest.xml";
                File.Copy(questManifestPath, manifestPath);
            }
        }

        #endregion

        #region Head

        public class HeadTargetProps : HeadTarget_Editor.TargetProps {

#if pUNITYXR
            SerializedProperty cameraProp;
#endif
            SerializedProperty overrideOptitrackPositionProp;

            public HeadTargetProps(SerializedObject serializedObject, HeadTarget headTarget)
                : base(serializedObject, headTarget.oculus, headTarget, "oculus") {

#if pUNITYXR
                cameraProp = serializedObject.FindProperty("oculus.unityXRHmd");
#endif
                overrideOptitrackPositionProp = serializedObject.FindProperty("oculus.overrideOptitrackPosition");
            }

            public override void Inspector() {
                if (!headTarget.humanoid.oculus.enabled || !OculusSupported())
                    return;

                //CheckHmdComponent(headTarget);

                enabledProp.boolValue = HumanoidTarget_Editor.ControllerInspector(headTarget.oculus, headTarget);
                headTarget.oculus.enabled = enabledProp.boolValue;
                if (!Application.isPlaying) {
#if pUNITYXR
                    //headTarget.oculus.CheckCamera();
#else
                    //headTarget.oculus.CheckSensorTransform();
                    headTarget.oculus.CheckSensor();
#endif
                    headTarget.oculus.SetSensor2Target();
                    headTarget.oculus.ShowSensor(headTarget.humanoid.showRealObjects && headTarget.showRealObjects);
                }

                if (enabledProp.boolValue) {
                    EditorGUI.indentLevel++;
#if pUNITYXR
                    cameraProp.objectReferenceValue = (UnityXRHmd)EditorGUILayout.ObjectField("Camera", cameraProp.objectReferenceValue, typeof(UnityXRHmd), true);
#else
                    sensorTransformProp.objectReferenceValue = (Transform)EditorGUILayout.ObjectField("Tracker Transform", headTarget.oculus.sensorTransform, typeof(Transform), true);
#endif
#if hOPTITRACK
                    if (headTarget.optitrack.enabled)
                        overrideOptitrackPositionProp.boolValue = EditorGUILayout.Toggle("Override OptiTrack Position", overrideOptitrackPositionProp.boolValue);
                    else
#endif
                        overrideOptitrackPositionProp.boolValue = true;

                    EditorGUI.indentLevel--;
                }
            }

            protected static void CheckHmdComponent(HeadTarget headTarget) {
                if (headTarget.oculus.sensorTransform == null)
                    return;

#if !pUNITYXR
                OculusHmd sensorComponent = headTarget.oculus.sensorTransform.GetComponent<OculusHmd>();
                if (sensorComponent == null)
                    headTarget.oculus.sensorTransform.gameObject.AddComponent<OculusHmd>();
#endif
            }
        }

        #region HMD Component
        [CustomEditor(typeof(OculusHmd))]
        public class OculusHmdComponent_Editor : Editor {
            OculusHmd sensorComponent;

            private void OnEnable() {
                sensorComponent = (OculusHmd)target;
            }

            public override void OnInspectorGUI() {
                serializedObject.Update();

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.EnumPopup("Status", sensorComponent.status);
                EditorGUILayout.FloatField("Position Confidence", sensorComponent.positionConfidence);
                EditorGUILayout.FloatField("Rotation Confidence", sensorComponent.rotationConfidence);
                EditorGUI.EndDisabledGroup();
                ResetTrackingWhenMounting();

                serializedObject.ApplyModifiedProperties();
            }

            protected void ResetTrackingWhenMounting() {
                SerializedProperty resetTrackingWhenMounting = serializedObject.FindProperty("resetTrackingWhenMounting");
                resetTrackingWhenMounting.boolValue = EditorGUILayout.Toggle("Reset Tracking When Mounting", resetTrackingWhenMounting.boolValue);

            }
        }
        #endregion

        #endregion

        #region Hand

        public class HandTargetProps : HandTarget_Editor.TargetProps {

#if UNITY_ANDROID && hOCHAND
            SerializedProperty handTrackingProp;
            SerializedProperty skeletonProp;
#endif
#if pUNITYXR
            SerializedProperty controllerProp;
#endif

            public HandTargetProps(SerializedObject serializedObject, HandTarget handTarget)
                : base(serializedObject, handTarget.oculus, handTarget, "oculus") {

#if UNITY_ANDROID && hOCHAND
                handTrackingProp = serializedObject.FindProperty("oculus.handTracking");
                skeletonProp = serializedObject.FindProperty("oculus.handSkeleton");
#endif
#if pUNITYXR
                controllerProp = serializedObject.FindProperty("oculus.unityXRController");
#endif
            }

            public override void Inspector() {
                if (!handTarget.humanoid.oculus.enabled || !OculusSupported())
                    return;

                CheckControllerComponent(handTarget);
#if UNITY_ANDROID && hOCHAND
                handTarget.oculus.CheckSensor(handTarget, handTarget.humanoid.oculus);
#endif

                enabledProp.boolValue = HumanoidTarget_Editor.ControllerInspector(handTarget.oculus, handTarget);
                handTarget.oculus.enabled = enabledProp.boolValue;
#if !pUNITYXR
                handTarget.oculus.CheckSensorTransform();
#endif
                if (!Application.isPlaying) {
                    handTarget.oculus.SetSensor2Target();
                    handTarget.oculus.ShowSensor(handTarget.humanoid.showRealObjects && handTarget.showRealObjects);
                }

                if (enabledProp.boolValue) {
                    EditorGUI.indentLevel++;
#if pUNITYXR
                    controllerProp.objectReferenceValue = (UnityXRController)EditorGUILayout.ObjectField("Controller", controllerProp.objectReferenceValue, typeof(UnityXRController), true);
#else
                    sensorTransformProp.objectReferenceValue = (Transform)EditorGUILayout.ObjectField("Tracker Transform", handTarget.oculus.sensorTransform, typeof(Transform), true);
#endif
#if UNITY_ANDROID && hOCHAND
                    if (handTarget.humanoid.oculus.handTracking) {
                        skeletonProp.objectReferenceValue = (OculusHandSkeleton)EditorGUILayout.ObjectField("Skeleton", skeletonProp.objectReferenceValue, typeof(OculusHandSkeleton), true);
                    }
#endif
                    EditorGUI.indentLevel--;
                }
            }

            protected static void CheckControllerComponent(HandTarget handTarget) {
                if (handTarget.oculus.sensorTransform == null)
                    return;

#if pUNITYXR
                //                UnityXRController controller = handTarget.oculus.sensorTransform.GetComponent<UnityXRController>();
                //                if (controller == null)
                //                    controller = handTarget.oculus.sensorTransform.gameObject.AddComponent<UnityXRController>();
                //                controller.isLeft = handTarget.isLeft;
                //#else
                OculusController sensorComponent = handTarget.oculus.sensorTransform.GetComponent<OculusController>();
                if (sensorComponent == null)
                    sensorComponent = handTarget.oculus.sensorTransform.gameObject.AddComponent<OculusController>();
                sensorComponent.isLeft = handTarget.isLeft;
#endif
            }

            //#if hOCHAND
            //            protected static void CheckSkeletonComponent(HandTarget handTarget) {
            //                if (handTarget.oculus.handSkeleton == null) {
            //                    handTarget.oculus.handSkeleton = handTarget.oculus.FindHandSkeleton(handTarget.isLeft);
            //                    if (handTarget.oculus.handSkeleton == null)
            //                        handTarget.oculus.handSkeleton = handTarget.oculus.CreateHandSkeleton(handTarget.isLeft, handTarget.showRealObjects);
            //                }

            //            }
            //#endif
        }

        #region Controller Component
        [CustomEditor(typeof(OculusController))]
        public class OculusControllerComponent_Editor : Editor {
            OculusController controllerComponent;

            private void OnEnable() {
                controllerComponent = (OculusController)target;
            }

            public override void OnInspectorGUI() {
                serializedObject.Update();

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.EnumPopup("Status", controllerComponent.status);
                EditorGUILayout.FloatField("Position Confidence", controllerComponent.positionConfidence);
                EditorGUILayout.FloatField("Rotation Confidence", controllerComponent.rotationConfidence);
                EditorGUILayout.Space();
                EditorGUILayout.Toggle("Is Left", controllerComponent.isLeft);
                EditorGUILayout.Vector3Field("Joystick", controllerComponent.joystick);
                EditorGUILayout.Slider("Index Trigger", controllerComponent.indexTrigger, -1, 1);
                EditorGUILayout.Slider("Hand Trigger", controllerComponent.handTrigger, -1, 1);
                if (controllerComponent.isLeft) {
                    EditorGUILayout.Slider("Button X", controllerComponent.buttonAX, -1, 1);
                    EditorGUILayout.Slider("Button Y", controllerComponent.buttonBY, -1, 1);
                }
                else {
                    EditorGUILayout.Slider("Button A", controllerComponent.buttonAX, -1, 1);
                    EditorGUILayout.Slider("Button B", controllerComponent.buttonBY, -1, 1);
                }
                EditorGUILayout.Slider("Thumbrest", controllerComponent.thumbrest, -1, 1);
                EditorGUI.EndDisabledGroup();

                serializedObject.ApplyModifiedProperties();

                controllerComponent.show = EditorGUILayout.Toggle("Show", controllerComponent.show);
            }
        }
        #endregion

        #endregion

        #region Object Target
        /*
                private static SerializedProperty objectEnabledProp;
                private static SerializedProperty objectSensorTransformProp;
                private static SerializedProperty objectSensor2TargetPositionProp;
                private static SerializedProperty objectSensor2TargetRotationProp;

                public static void InitObject(SerializedObject serializedObject, ObjectTarget objectTarget) {
                    objectEnabledProp = serializedObject.FindProperty("oculusController.enabled");
                    objectSensorTransformProp = serializedObject.FindProperty("oculusController.sensorTransform");
                    objectSensor2TargetPositionProp = serializedObject.FindProperty("oculusController.sensor2TargetPosition");
                    objectSensor2TargetRotationProp = serializedObject.FindProperty("oculusController.sensor2TargetRotation");

                    objectTarget.oculus.Init(objectTarget);
                }

                private enum LeftRight {
                    Left,
                    Right
                }

                public static void ObjectInspector(OculusController controller) {
                    objectEnabledProp.boolValue = Target_Editor.ControllerInspector(controller);
                    controller.CheckSensorTransform();

                    if (objectEnabledProp.boolValue) {
                        EditorGUI.indentLevel++;
                        LeftRight leftRight = controller.isLeft ? LeftRight.Left : LeftRight.Right;
                        leftRight = (LeftRight)EditorGUILayout.EnumPopup("Tracker Id", leftRight);
                        controller.isLeft = leftRight == LeftRight.Left;
                        objectSensorTransformProp.objectReferenceValue = (Transform)EditorGUILayout.ObjectField("Tracker Transform", controller.sensorTransform, typeof(Transform), true);
                        EditorGUI.indentLevel--;
                    }
                }

                public static void SetSensor2Target(OculusController controller) {
                    controller.SetSensor2Target();
                    objectSensor2TargetRotationProp.quaternionValue = controller.sensor2TargetRotation;
                    objectSensor2TargetPositionProp.vector3Value = controller.sensor2TargetPosition;
                }
                */
        #endregion
#endif
        public static bool OculusSupported() {
#if pUNITYXR
            return true;
#else
#if UNITY_2017_2_OR_NEWER
            string[] supportedDevices = XRSettings.supportedDevices;
#else
            string[] supportedDevices = VRSettings.supportedDevices;
#endif
            foreach (string supportedDevice in supportedDevices) {
                if (supportedDevice == "Oculus")
                    return true;
            }
            return false;
#endif
        }

    }

#if !hOCULUS
    [InitializeOnLoad]
    public class OculusCleanup {

        // Cleanup Oculus devices when Oculus is disabled in the preferences
        static OculusCleanup() {
            OculusHmd[] hmds = Object.FindObjectsOfType<OculusHmd>();
            foreach (OculusHmd hmd in hmds)
                Object.DestroyImmediate(hmd.gameObject, true);

            OculusController[] controllers = Object.FindObjectsOfType<OculusController>();
            foreach (OculusController controller in controllers)
                Object.DestroyImmediate(controller.gameObject, true);
        }
    }
#endif
}
