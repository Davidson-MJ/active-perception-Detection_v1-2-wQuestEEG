using UnityEngine;

namespace Passer.Tracking {
    using Humanoid.Tracking;

    /// <summary>
    /// The Oculus Head Mounted Device
    /// </summary>
    public class OculusHmd : SensorComponent {
#if hOCULUS
        private Humanoid.Tracking.Sensor.ID sensorId = Humanoid.Tracking.Sensor.ID.Head;

        private bool wasTracking = true;
        private Vector3 lastTrackingPosition;
        public bool resetTrackingWhenMounting = false;

        #region Start

        /// <summary>
        /// Find the Oculus Hmd
        /// </summary>
        /// <param name="oculusTransform"></param>
        /// <returns></returns>
        public static OculusHmd FindHmd(Transform oculusTransform) {
            OculusHmd[] tags = oculusTransform.GetComponentsInChildren<OculusHmd>();
            if (tags.Length > 0)
                return tags[0];

            return null;
        }

        /// <summary>Find or Create a new Antilatency Sensor</summary>
        public static OculusHmd Get(Transform oculusTransform, Vector3 position, Quaternion rotation) {
            if (oculusTransform == null)
                return null;

            OculusHmd hmd = FindHmd(oculusTransform);
            if (hmd == null) {
                GameObject sensorObject = new GameObject("Oculus HMD");
                Transform sensorTransform = sensorObject.transform;

                sensorTransform.parent = oculusTransform.transform;
                sensorTransform.position = position;
                sensorTransform.rotation = rotation;

                hmd = sensorTransform.gameObject.AddComponent<OculusHmd>();
                //hmd.oculus = oculus;
            }
            return hmd;
        }

        #endregion

        #region Update

        public override void UpdateComponent() {
            if (OculusDevice.GetRotationalConfidence(sensorId) == 0) {
                status = OculusDevice.IsPresent(0) ? Tracker.Status.Present : Tracker.Status.Unavailable;
                positionConfidence = 0;
                rotationConfidence = 0;
                gameObject.SetActive(false);
                OculusDevice.positionalTracking = false;
                wasTracking = false;
                return;
            }

            if (!OculusDevice.userPresent) {
                status = Tracker.Status.Present;
                positionConfidence = 0;
                rotationConfidence = 0;
                gameObject.SetActive(false);
                OculusDevice.positionalTracking = false;
                wasTracking = false;
                return;
            }

            OculusDevice.positionalTracking = true;
            if (resetTrackingWhenMounting && !wasTracking)
                ResetTrackingPosition();

            status = Tracker.Status.Tracking;

            Vector3 localPosition = Humanoid.HumanoidTarget.ToVector3(OculusDevice.GetPosition(sensorId));
            Quaternion localRotation = Humanoid.HumanoidTarget.ToQuaternion(OculusDevice.GetRotation(sensorId));
            transform.position = trackerTransform.TransformPoint(localPosition);
            transform.rotation = trackerTransform.rotation * localRotation;

            positionConfidence = OculusDevice.GetPositionalConfidence(sensorId);
            rotationConfidence = OculusDevice.GetRotationalConfidence(sensorId);
            gameObject.SetActive(true);

            FuseWithUnityCamera();

            wasTracking = true;
            lastTrackingPosition = transform.position;
        }

        protected virtual void FuseWithUnityCamera() {
            if (Camera.main == null)
                return;

            Vector3 deltaPos = Camera.main.transform.position - transform.position;
            if (deltaPos.sqrMagnitude > 0.00001) {
                Camera.main.transform.parent.position -= deltaPos;
            }
        }

        // Reset tracking position to the last position when it was tracking
        // This is needed to prevent the camera/avatar move while the headset is off
        // But then the tracking origin changes while you probably want to have it fixed
        // (that is: repeatable). 
        protected virtual void ResetTrackingPosition() {
            if (lastTrackingPosition.sqrMagnitude == 0)
                return;

            Vector3 deltaPos = Camera.main.transform.position - lastTrackingPosition;
            trackerTransform.position -= deltaPos;
        }

        #endregion
#endif
    }
}