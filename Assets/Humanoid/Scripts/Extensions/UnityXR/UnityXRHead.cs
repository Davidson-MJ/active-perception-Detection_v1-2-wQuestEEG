using UnityEngine;

namespace Passer.Humanoid {
    using Passer.Tracking;

    [System.Serializable]
    public class UnityXRHead : HeadSensor {
#if pUNITYXR
        public override string name {
            get { return "Unity XR"; }
        }

        [SerializeField]
        private UnityXRHmd _unityXrHmd;
        public UnityXRHmd unityXrHmd {
            get {
                if (_unityXrHmd == null) {
                    if (tracker == null && headTarget != null && headTarget.humanoid != null && headTarget.humanoid.unityXR != null)
                        tracker = headTarget.humanoid.unityXR.tracker;
                    if (tracker != null)
                    _unityXrHmd = tracker.hmd;
                }
                return _unityXrHmd;
            }
            set {
                _unityXrHmd = value;
            }
        }
        private new UnityXR tracker;

        #region Manage

        public override void CheckSensor(HeadTarget headTarget) {
#if pUNITYXR
            if (this.headTarget == null)
                this.target = headTarget;
            if (this.headTarget == null || this.headTarget.humanoid == null)
                return;

            if (headTarget.humanoid.unity == null) {
                headTarget.humanoid.unityXR.CheckTracker(headTarget.humanoid);
            }

            if (tracker == null)
                tracker = headTarget.humanoid.unity;

            if (enabled && tracker != null && tracker.enabled) {
                if (unityXrHmd == null) {
                    Vector3 position = headTarget.transform.TransformPoint(headTarget.head2eyes);
                    Quaternion rotation = headTarget.transform.rotation;
                    unityXrHmd = headTarget.humanoid.unity.GetHmd(position, rotation);
                }
                if (unityXrHmd != null)
                    sensorTransform = unityXrHmd.transform;

                if (!Application.isPlaying)
                    SetSensor2Target();
            }
            else {
#if UNITY_EDITOR
                if (!Application.isPlaying) {
                    if (unityXrHmd != null)
                        Object.DestroyImmediate(unityXrHmd.gameObject, true);
                }
#endif
                unityXrHmd = null;
                sensorTransform = null;
            }
#endif
        }

        #endregion

        #region Start

        public override void Start(HumanoidControl _humanoid, Transform targetTransform) {
            base.Start(_humanoid, targetTransform);
            tracker = headTarget.humanoid.unity;

            if (tracker == null || !tracker.enabled || !enabled)
                return;

            Vector3 position = headTarget.transform.TransformPoint(headTarget.head2eyes);
            Quaternion rotation = headTarget.transform.rotation;

            unityXrHmd = tracker.GetHmd(position, rotation);
            if (unityXrHmd != null)
                sensorTransform = unityXrHmd.transform;

            SetSensor2Target();
            CheckSensorTransform();
            sensor2TargetPosition = -headTarget.head2eyes;

            if (unityXrHmd != null)
                unityXrHmd.StartComponent(tracker.transform);
        }

        #endregion

        #region Update

        protected bool calibrated = false;

        public override void Update() {
            if (tracker == null || !tracker.enabled || !enabled)
                return;

            if (unityXrHmd == null)
                return;

            unityXrHmd.UpdateComponent();

            UpdateTarget(headTarget.head.target, unityXrHmd);
            UpdateNeckTargetFromHead();

            if (!calibrated && unityXrHmd.status == Tracker.Status.Tracking) {
                if (headTarget.humanoid.calibrateAtStart)
                    headTarget.humanoid.Calibrate();
                calibrated = true;
            }
        }

        #endregion
#endif
    }
}