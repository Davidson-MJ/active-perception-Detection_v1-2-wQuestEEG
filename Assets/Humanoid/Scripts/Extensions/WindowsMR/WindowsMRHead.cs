#if hWINDOWSMR && UNITY_2017_2_OR_NEWER && !UNITY_2020_1_OR_NEWER && UNITY_WSA_10_0
using UnityEngine;

namespace Passer.Humanoid {
    [System.Serializable]
    public class WindowsMRHead : HeadSensor {
        public override string name {
            get { return "Windows MR HMD"; }
        }

        private WindowsMRHmdComponent mixedRealityHmd;

        public override Tracker.Status status {
            get {
                if (mixedRealityHmd == null)
                    return Tracker.Status.Unavailable;
                return mixedRealityHmd.status;
            }
            set { mixedRealityHmd.status = value; }
        }

#region Start

        public override void Init(HeadTarget headTarget) {
            base.Init(headTarget);
            if (headTarget.humanoid != null)
                tracker = headTarget.humanoid.mixedReality;
        }

        public override void Start(HumanoidControl _humanoid, Transform targetTransform) {
            base.Start(_humanoid, targetTransform);
            tracker = headTarget.humanoid.mixedReality;

            if (tracker == null || !tracker.enabled || !enabled)
                return;

            SetSensor2Target();
            CheckSensorTransform();
            sensor2TargetPosition = -headTarget.head2eyes;

            if (sensorTransform != null) {
                mixedRealityHmd = sensorTransform.GetComponent<WindowsMRHmdComponent>();
                if (mixedRealityHmd != null)
                    mixedRealityHmd.StartComponent(tracker.trackerTransform);
            }
        }

        protected override void CreateSensorTransform() {
            CreateSensorTransform("Mixed Reality HMD", headTarget.head2eyes, Quaternion.identity);
            WindowsMRHmdComponent mixedRealityHmd = sensorTransform.GetComponent<WindowsMRHmdComponent>();
            if (mixedRealityHmd == null)
                sensorTransform.gameObject.AddComponent<WindowsMRHmdComponent>();
        }

#endregion

#region Update

        bool calibrated = false;

        public override void Update() {
            if (tracker == null || !tracker.enabled || !enabled)
                return;

            if (mixedRealityHmd == null) {
                UpdateTarget(headTarget.head.target, sensorTransform);
            }

            mixedRealityHmd.UpdateComponent();
            headTarget.tracking = mixedRealityHmd.status == Tracker.Status.Tracking;
            if (mixedRealityHmd.status != Tracker.Status.Tracking)
                return;

            UpdateTarget(headTarget.head.target, mixedRealityHmd);
            UpdateNeckTargetFromHead();

            if (!calibrated && tracker.humanoid.calibrateAtStart) {
                tracker.humanoid.Calibrate();
                calibrated = true;
            }
        }

#endregion
    }
}
#endif