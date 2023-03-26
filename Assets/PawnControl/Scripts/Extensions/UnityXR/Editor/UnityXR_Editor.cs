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

    public class UnityXR_Editor : Editor {

#if pUNITYXR

        #region Tracker

        public class TrackerProps : HumanoidControl_Editor.HumanoidTrackerProps {

            private SerializedProperty trackerProp;

            public TrackerProps(SerializedObject serializedObject, HumanoidControl_Editor.HumanoidTargetObjs targetObjs, UnityXRTracker _unityXR)
                : base(serializedObject, targetObjs, _unityXR, "unityXR") {
                tracker = _unityXR;

                headSensorProp = targetObjs.headTargetObj.FindProperty("oculus");
                leftHandSensorProp = targetObjs.leftHandTargetObj.FindProperty("oculus");
                rightHandSensorProp = targetObjs.rightHandTargetObj.FindProperty("oculus");

                trackerProp = serializedObject.FindProperty("unityXR.tracker");
            }

            public override void Inspector(HumanoidControl humanoid) {
                //humanoid.unityXR.CheckTracker(humanoid);
                //humanoid.headTarget.unityXR.CheckSensor(humanoid.headTarget);
                //humanoid.leftHandTarget.unityXR.CheckSensor(humanoid.leftHandTarget);
                //humanoid.rightHandTarget.unityXR.CheckSensor(humanoid.rightHandTarget);

                Inspector(humanoid, "Unity XR");
                if (enabledProp.boolValue) {
                    EditorGUI.indentLevel++;
                    if (trackerProp.objectReferenceValue == null) {
                        // Tracker does not exit
                        using (new EditorGUILayout.HorizontalScope()) {
                            EditorGUILayout.LabelField("Tracker", GUILayout.Width(120));
                            if (GUILayout.Button("Show")) {
                                humanoid.unityXR.CheckTracker(humanoid);
                            }
                        }
                    }
                    else
                        trackerProp.objectReferenceValue = (UnityXR)EditorGUILayout.ObjectField("Tracker", trackerProp.objectReferenceValue, typeof(UnityXR), true);
                    EditorGUI.indentLevel--;
                }
            }
        }

        #endregion

        #region Head

        public class HeadTargetProps : HeadTarget_Editor.TargetProps {

            SerializedProperty hmdProp;
#if hVIVEFACE
            protected readonly SerializedProperty viveFaceProp;
#endif

            public HeadTargetProps(SerializedObject serializedObject, HeadTarget headTarget)
                : base(serializedObject, headTarget.unityXR, headTarget, "unityXR") {

                hmdProp = serializedObject.FindProperty("unityXR._unityXrHmd");
#if hVIVEFACE
                viveFaceProp = serializedObject.FindProperty("face.vive");
#endif
            }

            public override void Inspector() {
                if (!headTarget.humanoid.unityXR.enabled)
                    return;

                enabledProp.boolValue = HumanoidTarget_Editor.ControllerInspector(headTarget.unityXR, headTarget);
                headTarget.unityXR.enabled = enabledProp.boolValue;

                if (enabledProp.boolValue) {
                    EditorGUI.indentLevel++;
                    if (headTarget.unityXR.unityXrHmd == null) {
                        // Hmd does not exist
                        using (new EditorGUILayout.HorizontalScope()) {
                            EditorGUILayout.LabelField("Hmd", GUILayout.Width(120));
                            if (GUILayout.Button("Show")) {
                                headTarget.unityXR.CheckSensor(headTarget);
                            }
                        }
                    }
                    else
                        hmdProp.objectReferenceValue = (UnityXRHmd)EditorGUILayout.ObjectField("Hmd", headTarget.unityXR.unityXrHmd, typeof(UnityXRHmd), true);
                    EditorGUI.indentLevel--;

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

                }
            }
        }

        #endregion

        #region Hand

        public class HandTargetProps : HandTarget_Editor.TargetProps {

            SerializedProperty controllerProp;

            public HandTargetProps(SerializedObject serializedObject, HandTarget handTarget)
                : base(serializedObject, handTarget.unityXR, handTarget, "unityXR") {

                controllerProp = serializedObject.FindProperty("unityXR._unityXrController");
            }

            public override void Inspector() {
                if (!handTarget.humanoid.unityXR.enabled)
                    return;

                enabledProp.boolValue = HumanoidTarget_Editor.ControllerInspector(handTarget.unityXR, handTarget);
                handTarget.unityXR.enabled = enabledProp.boolValue;

                if (enabledProp.boolValue) {
                    EditorGUI.indentLevel++;
                    if (handTarget.unityXR.unityXrController == null) {
                        // Hmd does not exist
                        using (new EditorGUILayout.HorizontalScope()) {
                            EditorGUILayout.LabelField("Controller", GUILayout.Width(120));
                            if (GUILayout.Button("Show")) {
                                handTarget.unityXR.CheckSensor(handTarget);
                            }
                        }
                    }
                    else
                        controllerProp.objectReferenceValue = (UnityXRController)EditorGUILayout.ObjectField("Controller", controllerProp.objectReferenceValue, typeof(UnityXRController), true);
                    EditorGUI.indentLevel--;
                }

                // TODO: _only_ when the unityXR hand is going from disabled to enabled, a controller should be created
            }

        }

        #endregion

#endif
    }

}
