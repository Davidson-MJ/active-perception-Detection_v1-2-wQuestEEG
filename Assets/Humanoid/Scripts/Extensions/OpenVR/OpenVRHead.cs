#if hOPENVR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
using UnityEngine;
using Passer.Tracking;

namespace Passer.Humanoid {
    using Tracking;

    [System.Serializable]
    public class OpenVRHead : HeadSensor {
        public override string name {
            get { return "OpenVR HMD"; }
        }

        public OpenVRHmd hmd;

        public override Tracker.Status status {
            get {
                if (hmd == null)
                    return Tracker.Status.Unavailable;
                return hmd.status;
            }
            set { hmd.status = value; }
        }

        #region Manage

        public override void CheckSensor(HeadTarget headTarget) {
            base.CheckSensor(headTarget);
#if !UNITY_2020_1_OR_NEWER
            if (this.headTarget == null)
                return;

            if (tracker == null)
                tracker = headTarget.humanoid.openVR;

            if (enabled && tracker != null && tracker.enabled) {
                if (hmd == null) {
                    Vector3 position = headTarget.transform.TransformPoint(headTarget.head2eyes);
                    Quaternion rotation = headTarget.transform.rotation;
                    hmd = headTarget.humanoid.openVR.tracker.GetHmd(position, rotation);
                }
                if (hmd != null)
                    sensorTransform = hmd.transform;

                if (!Application.isPlaying)
                    SetSensor2Target();
            }
            else {
#if UNITY_EDITOR
                if (!Application.isPlaying) {
                    if (hmd != null)
                        Object.DestroyImmediate(hmd.gameObject, true);
                }
#endif
                hmd = null;
                sensorTransform = null;
            }
#endif
        }

        #endregion Manage

        #region Start
        public override void Init(HeadTarget headTarget) {
            base.Init(headTarget);
            if (headTarget.humanoid != null)
                tracker = headTarget.humanoid.openVR;
        }

        public override void Start(HumanoidControl _humanoid, Transform targetTransform) {
            base.Start(_humanoid, targetTransform);
            tracker = headTarget.humanoid.openVR;

            if (tracker == null || !tracker.enabled || !enabled)
                return;

            SetSensor2Target();
            CheckSensorTransform();
            sensor2TargetPosition = -headTarget.head2eyes;

            if (sensorTransform != null) {
                hmd = sensorTransform.GetComponent<OpenVRHmd>();
                if (hmd != null)
                    hmd.StartComponent(tracker.trackerTransform);
            }
        }

        protected override void CreateSensorTransform() {
            return;

            //CreateSensorTransform("OpenVR HMD", headTarget.head2eyes, Quaternion.identity);
            //OpenVRHmd openVRHmd = sensorTransform.GetComponent<OpenVRHmd>();
            //if (openVRHmd == null)
            //    sensorTransform.gameObject.AddComponent<OpenVRHmd>();
        }
        #endregion

        #region Update
        protected bool calibrated = false;

        public override void Update() {
            if (tracker == null || !tracker.enabled || !enabled)
                return;

            if (hmd == null && sensorTransform != null) {
                hmd = sensorTransform.GetComponent<OpenVRHmd>();
                UpdateTarget(headTarget.head.target, sensorTransform);
            }
            if (hmd == null)
                return;

            hmd.UpdateComponent();

            headTarget.tracking = hmd.status == Tracker.Status.Tracking;
            if (hmd.status != Tracker.Status.Tracking)
                return;

            UpdateTarget(headTarget.head.target, hmd);
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