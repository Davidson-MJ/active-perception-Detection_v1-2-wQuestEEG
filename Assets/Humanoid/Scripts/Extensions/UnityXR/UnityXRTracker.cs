#if pUNITYXR

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Passer.Tracking;

namespace Passer.Humanoid {
    using Humanoid.Tracking;

    [System.Serializable]
    public class UnityXRTracker : HumanoidTracker {

        public bool handTracking = true;

        public UnityXRTracker() {
            deviceView = new DeviceView();
        }

        public override string name {
            get { return "Unity XR"; }
        }

        public override HeadSensor headSensor {
            get { return humanoid.headTarget.unityXR; }
        }
        public override ArmSensor leftHandSensor {
            get { return humanoid.leftHandTarget.unityXR; }
        }
        public override ArmSensor rightHandSensor {
            get { return humanoid.rightHandTarget.unityXR; }
        }

        public UnityXR tracker;

        [System.NonSerialized]
        private UnitySensor[] _sensors;
        public override UnitySensor[] sensors {
            get {
                if (_sensors == null)
                    _sensors = new UnitySensor[] {
                        headSensor,
                        leftHandSensor,
                        rightHandSensor
                    };

                return _sensors;
            }
        }

#region Manage

        public override void CheckTracker(HumanoidControl humanoid) {
            if (this.humanoid == null)
                this.humanoid = humanoid;
            if (this.humanoid == null)
                return;

            if (enabled) {
                if (tracker == null) {
                    GameObject rwObject = HumanoidControl.GetRealWorld(humanoid.transform);
                    Transform realWorld = rwObject.transform;

                    humanoid.unity = UnityXR.Get(realWorld);
                    tracker = humanoid.unity;
                }
                if (tracker == null)
                    return;
                trackerTransform = tracker.transform;
            }
            else {
#if UNITY_EDITOR
                if (!Application.isPlaying) {
                    if (tracker != null)
                        Object.DestroyImmediate(tracker.gameObject, true);
                }
#endif
                tracker = null;
                trackerTransform = null;
            }
        }

        public UnityXRController GetController(bool isLeft, Vector3 position, Quaternion rotation) {
            if (humanoid.unity == null)
                return null;

            if (isLeft && humanoid.unity.leftController != null)
                return humanoid.unity.leftController;
            if (!isLeft && humanoid.unity.rightController != null)
                return humanoid.unity.rightController;

            if (Application.isPlaying) {
                Debug.LogError("UnityXR Controller is missing");
                return null;
            }
#if UNITY_EDITOR
            else {
                UnityXRController controller = humanoid.unity.GetController(isLeft, position, rotation);
                GameObject controllerPrefab = isLeft ?
                    (GameObject)AssetDatabase.LoadAssetAtPath("Assets/PawnControl/Prefabs/Resources/Left Touch Controller.prefab", typeof(GameObject)) :
                    (GameObject)AssetDatabase.LoadAssetAtPath("Assets/PawnControl/Prefabs/Resources/Right Touch Controller.prefab", typeof(GameObject));
                if (controllerPrefab != null) {
                    GameObject controllerObj = Object.Instantiate(controllerPrefab);
                    controllerObj.name = "Controller";
                    controllerObj.transform.parent = controller.transform;
                    controllerObj.transform.localPosition = Vector3.zero;
                    controllerObj.transform.localRotation = Quaternion.identity;
                }
                return controller;
            }
#else
            return null;
#endif
        }

        public override void ShowTracker(bool shown) {
            if (tracker != null)
                tracker.ShowSkeleton(shown);
        }

#endregion

#region Start

        public override void StartTracker(HumanoidControl humanoid) {
            this.humanoid = humanoid;

            // Hand Tracking is not yet supported with Unity XR
            handTracking = false;

            CheckTracker(humanoid);
#if hOPENVR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
            humanoid.openVR.enabled = this.enabled;
            humanoid.openVR.StartTracker(humanoid);
#endif
#if hOCULUS && hOCHAND && UNITY_ANDROID
            humanoid.oculus.enabled = this.enabled;
            humanoid.oculus.StartTracker(humanoid);
#endif
        }

        protected virtual void CheckTrackerComponent() {
            if (tracker != null)
                return;

            tracker = trackerTransform.GetComponent<Passer.Tracking.UnityXR>();
            if (tracker == null)
                trackerTransform.gameObject.AddComponent<Passer.Tracking.UnityXR>();
        }

        public override bool AddTracker(HumanoidControl humanoid, string resourceName) {
            return false;
        }

#endregion

#region Update

        public override void UpdateTracker() {
            if (!enabled || trackerTransform == null)
                return;


            status = tracker.status;

            deviceView.position = HumanoidTarget.ToVector(trackerTransform.position);
            deviceView.orientation = HumanoidTarget.ToRotation(trackerTransform.rotation);

            foreach (SubTracker subTracker in subTrackers) {
                if (subTracker != null)
                    subTracker.UpdateTracker(humanoid.showRealObjects);
            }

#if hOPENVR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
            humanoid.openVR.UpdateTracker();
#endif
#if hOCULUS && hOCHAND && UNITY_ANDROID
            humanoid.oculus.UpdateTracker();
#endif
        }

#endregion

        public override void Calibrate() {
            if (!enabled || trackerTransform == null)
                return;

            base.Calibrate();

            humanoid.unity.transform.position = tracker.transform.position;
        }

        public override void AdjustTracking(Vector3 v, Quaternion q) {
            if (trackerTransform != null) {
                trackerTransform.position += v;
                trackerTransform.rotation *= q;
            }
        }
    }
}
#endif