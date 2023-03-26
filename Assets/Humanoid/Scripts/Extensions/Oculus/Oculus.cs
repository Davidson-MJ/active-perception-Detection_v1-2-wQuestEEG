#if hOCULUS && (UNITY_STANDALONE_WIN || UNITY_ANDROID)

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Passer.Tracking;

namespace Passer.Humanoid {
    using Humanoid.Tracking;

    [System.Serializable]
    public class OculusHumanoidTracker : HumanoidTracker {

        public bool handTracking = true;

        public OculusHumanoidTracker() {
            deviceView = new DeviceView();
        }

        public override string name {
#if pUNITYXR
            get { return "Unity XR"; }
#else
            get { return OculusDevice.name; }
#endif
        }

        public override HeadSensor headSensor {
            get { return humanoid.headTarget.oculus; }
        }
        public override ArmSensor leftHandSensor {
            get { return humanoid.leftHandTarget.oculus; }
        }
        public override ArmSensor rightHandSensor {
            get { return humanoid.rightHandTarget.oculus; }
        }

        public TrackerComponent tracker;

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

#if UNITY_ANDROID
        public enum AndroidDeviceType {
            GearVR,
            OculusGo,
            OculusQuest,
        }
        public AndroidDeviceType androidDeviceType = AndroidDeviceType.OculusQuest;
#endif

        #region Manage

        public override void CheckTracker(HumanoidControl humanoid) {
            if (this.humanoid == null)
                this.humanoid = humanoid;
            if (this.humanoid == null)
                return;

            if (enabled) {
                if (tracker == null) {
                    GameObject rwObject = HumanoidControl.GetRealWorld(humanoid.transform);//humanoid.GetRealWorld();
                    Transform realWorld = rwObject.transform;

                    //Vector3 position = realWorld.position;
                    //Quaternion rotation = realWorld.rotation;
                    //tracker = Passer.Tracking.OculusTracker.Get(realWorld, position, rotation);
#if pUNITYXR
                    humanoid.unity = UnityXR.Get(realWorld);
                    tracker = humanoid.unity;
#endif
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

#if pUNITYXR
        public UnityXRController GetController(bool isLeft, Vector3 position, Quaternion rotation) {
            if (humanoid.unity == null)
                return null;

            if (isLeft && humanoid.unity.leftController != null)
                return humanoid.unity.leftController;
            if (!isLeft && humanoid.unity.rightController != null)
                return humanoid.unity.rightController;

            if (Application.isPlaying) {
                Debug.LogError("Oculus Controller is missing");
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
#endif
        public override void ShowTracker(bool shown) {
            if (tracker != null)
                tracker.ShowSkeleton(shown);
        }

        #endregion

        #region Start

        public override void StartTracker(HumanoidControl _humanoid) {
            humanoid = _humanoid;

            UnityVRDevice.Start();

#if hLEGACYXR
            if (humanoid.headTarget.unity.enabled && UnityVRDevice.xrDevice == UnityVRDevice.XRDeviceType.Oculus)
                enabled = true;
#endif
#if pUNITYXR
            if (tracker == null)
                tracker = humanoid.unityXR.tracker;

            Passer.Tracking.UnityVRDevice.Start();
#endif
            if (!enabled || UnityVRDevice.xrDevice != UnityVRDevice.XRDeviceType.Oculus)
                return;

            OculusDevice.Start();

            AddTracker(humanoid, "Oculus");
            CheckTrackerComponent();
            StartCameras(trackerTransform);

#if pUNITYXR && hOCHAND
            this.trackerTransform = tracker.transform;
#endif
        }

        private void StartCameras(Transform trackerTransform) {
            //subTrackers = new OculusCameraComponent[(int)OculusDevice.Tracker.Count];
            for (int i = 0; i < OculusTrackingCamera.GetCount(); i++) {
                OculusTrackingCamera oculusCamera = OculusTrackingCamera.Create(this);
                oculusCamera.subTrackerId = i;
                subTrackers.Add(oculusCamera);
                //subTrackers[i] = OculusCameraComponent.Create(this);
                //subTrackers[i].subTrackerId = i;
            }
        }

        protected virtual void CheckTrackerComponent() {
            if (tracker != null)
                return;

            tracker = trackerTransform.GetComponent<Passer.Tracking.OculusTracker>();
            if (tracker == null)
                tracker = trackerTransform.gameObject.AddComponent<Passer.Tracking.OculusTracker>();
        }

        public override bool AddTracker(HumanoidControl humanoid, string resourceName) {
#if pUNITYXR
            return false;
#else
            return base.AddTracker(humanoid, resourceName);
#endif
        }
        #endregion

        #region Update

        public override void UpdateTracker() {
            if (!enabled || trackerTransform == null || tracker == null)
                return;

            if (UnityVRDevice.xrDevice != UnityVRDevice.XRDeviceType.Oculus) {
#if pUNITYXR
                status = tracker.status;
#else
                status = Status.Unavailable;
#endif
                return;
            }

            status = OculusDevice.status;

            deviceView.position = HumanoidTarget.ToVector(trackerTransform.position);
            deviceView.orientation = HumanoidTarget.ToRotation(trackerTransform.rotation);

            OculusDevice.Update();

            foreach (SubTracker subTracker in subTrackers) {
                if (subTracker != null)
                    subTracker.UpdateTracker(humanoid.showRealObjects);
            }

            if (OculusDevice.ovrp_GetAppShouldRecenter() == OculusDevice.Bool.True) {
                humanoid.Calibrate();
            }
        }

        #endregion

        public override void Calibrate() {
            if (!enabled || trackerTransform == null)
                return;

            base.Calibrate();

            if (enabled && UnityVRDevice.xrDevice == UnityVRDevice.XRDeviceType.Oculus)
                OculusDevice.ovrp_RecenterTrackingOrigin(unchecked((uint)OculusDevice.RecenterFlags.IgnoreAll));
#if pUNITYXR
            humanoid.unity.transform.position = tracker.transform.position;
#endif
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