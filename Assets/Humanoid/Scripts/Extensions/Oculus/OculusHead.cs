#if hOCULUS && (UNITY_STANDALONE_WIN || UNITY_ANDROID)
using UnityEngine;
using Passer.Tracking;

namespace Passer.Humanoid {
    using Humanoid.Tracking;

    [System.Serializable]
    public class OculusHead : HeadSensor {
        public override string name {
#if pUNITYXR
            get { return "Unity XR"; }
#else
            get { return "Oculus HMD"; }
#endif
        }

#if pUNITYXR
        public UnityXRHmd unityXRHmd;
#else
        private OculusHmd oculusHmd;
#endif

        public override Tracker.Status status {
#if pUNITYXR
            get {
                if (unityXRHmd == null)
                    return Tracker.Status.Unavailable;
                return unityXRHmd.status;
            }
#else
            get {
                if (oculusHmd == null)
                    return Tracker.Status.Unavailable;
                return oculusHmd.status;
            }
            set {
                if (oculusHmd != null)
                    oculusHmd.status = value;
            }
#endif
        }

        public bool overrideOptitrackPosition = true;

        #region Manage

        public override void CheckSensor(HeadTarget headTarget) {
#if pUNITYXR
            if (this.headTarget == null)
                this.target = headTarget;
            if (this.headTarget == null || this.headTarget.humanoid == null || headTarget.humanoid.unity == null)
                return;

            if (tracker == null)
                tracker = headTarget.humanoid.oculus;

            if (enabled && tracker != null && tracker.enabled) {
                if (unityXRHmd == null) {
                    Vector3 position = headTarget.transform.TransformPoint(headTarget.head2eyes);
                    Quaternion rotation = headTarget.transform.rotation;
                    unityXRHmd = headTarget.humanoid.unity.GetHmd(position, rotation);
                }
                if (unityXRHmd != null)
                    sensorTransform = unityXRHmd.transform;

                if (!Application.isPlaying)
                    SetSensor2Target();
            }
            else {
#if UNITY_EDITOR
                if (!Application.isPlaying) {
                    if (unityXRHmd != null)
                        Object.DestroyImmediate(unityXRHmd.gameObject, true);
                }
#endif
                unityXRHmd = null;
                sensorTransform = null;
            }
#endif
        }

        #endregion

        #region Start

        public override void Init(HeadTarget headTarget) {
            base.Init(headTarget);
            if (headTarget.humanoid != null)
                tracker = headTarget.humanoid.oculus;
        }

        public override void Start(HumanoidControl _humanoid, Transform targetTransform) {
            base.Start(_humanoid, targetTransform);
            tracker = headTarget.humanoid.oculus;

            if (tracker == null || !tracker.enabled || !enabled)
                return;


#if pUNITYXR
            if (headTarget.humanoid.unity != null)
                unityXRHmd = headTarget.humanoid.unity.hmd;
#else
            SetSensor2Target();
            CheckSensorTransform();
            sensor2TargetPosition = -headTarget.head2eyes;

            if (sensorTransform != null) {
                oculusHmd = sensorTransform.GetComponent<OculusHmd>();
                if (oculusHmd != null)
                    oculusHmd.StartComponent(tracker.trackerTransform);
            }
#endif
        }

        protected override void CreateSensorTransform() {
#if !pUNITYXR
            CreateSensorTransform("Oculus HMD", headTarget.head2eyes, Quaternion.identity);

            OculusHmd oculusHmd = sensorTransform.GetComponent<OculusHmd>();
            if (oculusHmd == null)
                sensorTransform.gameObject.AddComponent<OculusHmd>();
#endif
        }

        public virtual void CheckSensor() {
#if !pUNITYXR
            Vector3 position = headTarget.transform.TransformPoint(headTarget.head2eyes);
            Quaternion rotation = headTarget.transform.rotation;
            oculusHmd = OculusHmd.Get(headTarget.humanoid.oculus.trackerTransform, position, rotation);
            if (oculusHmd != null)
                sensorTransform = oculusHmd.transform;
#endif
        }

        #region Hmd

        //#if pUNITYXR
        //        public virtual void CheckCamera() {
        //            Vector3 position = headTarget.transform.TransformPoint(headTarget.head2eyes);
        //            Quaternion rotation = headTarget.transform.rotation;
        //            unityXRCamera = UnityXRCamera.Get(headTarget.humanoid.unity, position, rotation);
        //            if (unityXRCamera != null)
        //                sensorTransform = unityXRCamera.transform;
        //        }
        //#endif

        #endregion

        #endregion

        #region Update

        protected bool calibrated = false;
        // first frame can track but without proper values
        // so we need to wait one frame
        protected bool calibratedWait = true;

        public override void Update() {
            if (tracker == null || !tracker.enabled || !enabled)
                return;

#if pUNITYXR
            if (unityXRHmd == null || unityXRHmd.status != Tracker.Status.Tracking)
                return;
            UpdateTarget(headTarget.head.target, unityXRHmd);
#else
            if (oculusHmd == null) {
                //UpdateHeadTarget(headTarget.head.target, sensorTransform);
                return;
            }

            oculusHmd.UpdateComponent();

            headTarget.tracking = oculusHmd.status == Tracker.Status.Tracking;
            if (oculusHmd.status != Tracker.Status.Tracking)
                return;

            //if (oculusHmd.rotationConfidence < headTarget.head.target.confidence.rotation)
            //    FuseRotation();

            //if (oculusHmd.positionConfidence < headTarget.head.target.confidence.position && !overrideOptitrackPosition)
            //    FusePosition();

            UpdateTarget(headTarget.head.target, oculusHmd);
#endif
            UpdateNeckTargetFromHead();
            //UpdateCamera();


            if (!calibrated) {
#if !pUNITYXR
                if (calibratedWait)
                    calibratedWait = false;
                else
#endif
                {
                    if (tracker.humanoid.calibrateAtStart)
                        tracker.humanoid.Calibrate();
                    calibrated = true;
                }
            }
        }

        private Transform cameraTransform;

        private void UpdateCamera() {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
            if (cameraTransform == null || cameraTransform.parent == null)
                return;

            //if (headTarget.head.target.confidence.rotation > 0)
            cameraTransform.parent.rotation = headTarget.head.target.transform.rotation;
            //if (headTarget.head.target.confidence.position > 0)
            //cameraTransform.parent.position = headTarget.head.target.transform.position; // + headTarget.head.target.transform.rotation * headTarget.head2eyes;
        }

        /*
        // Oculus has no positional tracking and drift correction
        private void FuseRotation() {
        Quaternion oculusHeadRotation = GetTargetRotation(oculusHmd.transform);
            Quaternion rotation = Quaternion.FromToRotation(oculusHeadRotation * Vector3.forward, headTarget.head.target.transform.forward);
            float rotY = Angle.Normalize(rotation.eulerAngles.y);
            if (rotY > 10 || rotY < -10)
                // we do snap rotation for large differences
                tracker.trackerTransform.RotateAround(headTarget.head.target.transform.position, headTarget.humanoid.up, rotY);
            else
                tracker.trackerTransform.RotateAround(headTarget.head.target.transform.position, headTarget.humanoid.up, rotY * 0.001F);
        }

        // Oculus has no positional tracking and drift correction
        private void FusePosition() {
            Vector3 oculusHeadPosition = GetTargetPosition(oculusHmd.transform);
            Vector3 delta = headTarget.head.target.transform.position - oculusHeadPosition;
            tracker.trackerTransform.transform.position += delta; // (delta * 0.01F);
        }
        */

        #endregion
    }
}
#endif