using UnityEditor;
using UnityEngine;

namespace Passer.Humanoid {
    using Passer.Tracking;

    public class UnityVR_Editor : Editor {
#if pUNITYXR
        public static void CleanupFirstPersonCamera(HumanoidControl humanoid) {
            RemoveCamera(humanoid.headTarget.transform);
        }

        public static void RemoveCamera(Transform targetTransform) {
            Camera camera = targetTransform.GetComponentInChildren<Camera>();
            if (camera != null) {
                if (Application.isPlaying)
                    Destroy(camera.gameObject);
                else
                    DestroyImmediate(camera.gameObject);
            }
        }

#endif
#if hLEGACYXR
        public static void AddTracker(HumanoidControl humanoid) {
            // you cannot find a tracker in a disabled gameObject
            if (!humanoid.gameObject.activeInHierarchy)
                return;

            GameObject realWorld = HumanoidControl.GetRealWorld(humanoid.transform);

            humanoid.unity.trackerTransform = realWorld.transform.Find(UnityVRDevice.trackerName);
            if (humanoid.unity.trackerTransform == null) {
                UnityVRDevice.trackerObject = new GameObject {
                    name = UnityVRDevice.trackerName
                };
                UnityVRDevice.trackerObject.transform.parent = realWorld.transform;
                UnityVRDevice.trackerObject.transform.localPosition = Vector3.zero;
            }
            else
                UnityVRDevice.trackerObject = humanoid.unity.trackerTransform.gameObject;
        }

        private static void RemoveTracker() {
            DestroyImmediate(UnityVRDevice.trackerObject, true);
        }

        public static void ShowTracker(bool show) {
            if (UnityVRDevice.trackerObject == null)
                return;

            if (show && !UnityVRDevice.trackerObject.activeSelf && UnityVRDevice.present)
                HumanoidControl_Editor.ShowTracker(UnityVRDevice.trackerObject, true);

            else if (!show && UnityVRDevice.trackerObject.activeSelf)
                HumanoidControl_Editor.ShowTracker(UnityVRDevice.trackerObject, false);
        }

        public static void Inspector(HumanoidControl humanoid) {
            if (humanoid.headTarget == null)
                return;

            FirstPersonCameraInspector(humanoid.headTarget);
#if (UNITY_STANDALONE_WIN || UNITY_ANDROID) && !UNITY_2020_1_OR_NEWER
            if (PlayerSettings.virtualRealitySupported)
                AddTracker(humanoid);
            else
                RemoveTracker();

            ShowTracker(humanoid.showRealObjects);
#endif
        }

        private static void FirstPersonCameraInspector(HeadTarget headTarget) {
            if (headTarget.unity == null || headTarget.humanoid == null)
                return;

#if hOPENVR && hVIVETRACKER && UNITY_STANDALONE_WIN
            EditorGUI.BeginDisabledGroup(headTarget.humanoid.openVR.enabled && headTarget.viveTracker.enabled);
#endif
            bool wasEnabled = headTarget.unity.enabled;

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
#if hOPENVR && hVIVETRACKER && UNITY_STANDALONE_WIN
            if (headTarget.humanoid.openVR.enabled && headTarget.viveTracker.enabled)
                headTarget.unity.enabled = false;
#endif
            GUIContent text = new GUIContent(
                "First Person Camera",
                "Enables a first person camera. Disabling and enabling again reset the camera position"
                );
            bool enabled = EditorGUILayout.ToggleLeft(text, headTarget.unity.enabled, GUILayout.Width(200));

            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(headTarget, enabled ? "Enabled " : "Disabled " + headTarget.unity.name);
                headTarget.unity.enabled = enabled;
            }
            EditorGUILayout.EndHorizontal();

            if (!Application.isPlaying && !HumanoidControl_Editor.IsPrefab(headTarget.humanoid)) {
                UnityVRHead.CheckCamera(headTarget);
                if (!wasEnabled && headTarget.unity.enabled) {
                    UnityVRHead.AddCamera(headTarget);
                }
                else if (wasEnabled && !headTarget.unity.enabled) {
                    UnityVRHead.RemoveCamera(headTarget);
                }
            }
#if hOPENVR && hVIVETRACKER && UNITY_STANDALONE_WIN
            EditorGUI.EndDisabledGroup();
#endif

        }
#endif
    }
}