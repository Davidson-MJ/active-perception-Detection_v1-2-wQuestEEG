using System.Collections.Generic;

using UnityEngine;
using UnityEngine.XR;

namespace Passer.Tracking {

    public class UnityXRHmd : SensorComponent {

        public TrackerComponent tracker;
        public Camera unityCamera;

#if pUNITYXR
        public bool positionalTracking = true;

        public Transform sensorTransform {
            get { return this.transform; }
        }

        protected InputDevice device;

        #region Start

        protected override void Start() {
            base.Start();

            device = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);

            InputDevices.deviceConnected += OnDeviceConnected;
            InputDevices.deviceDisconnected += OnDeviceDisconnected;

            if (this.gameObject.GetComponent<UnityEngine.SpatialTracking.TrackedPoseDriver>() == null) {
                this.gameObject.AddComponent<UnityEngine.SpatialTracking.TrackedPoseDriver>();
            }
        }

        /// <summary>
        /// Hmd has connected
        /// </summary>
        /// <param name="device">The InputDevice of the hmd</param>
        protected virtual void OnDeviceConnected(InputDevice device) {
            bool isHmd = (device.characteristics & InputDeviceCharacteristics.HeadMounted) != 0;
            if (isHmd) {
                this.device = device;
                Show(true);
            }
        }

        /// <summary>
        /// Hmd has disconnected
        /// </summary>
        /// This also happens when the device is no longer tracked.
        /// <param name="device">The InputDevice of the hmd</param>
        protected virtual void OnDeviceDisconnected(InputDevice device) {
            bool isHmd = (device.characteristics & InputDeviceCharacteristics.HeadMounted) != 0;
            if (isHmd) {
                this.device = device;
                Show(false);
            }
        }

        #endregion
#endif

        #region Update

        public override void UpdateComponent() {
            base.UpdateComponent();

            status = Tracker.Status.Unavailable;
            positionConfidence = 0;
            rotationConfidence = 0;
#if pUNITYXR
            if (device == null)
                return;

            status = Tracker.Status.Present;

            Vector3 position;
            if (positionalTracking == false) {
                positionConfidence = 0;
            }
            else if (device.TryGetFeatureValue(CommonUsages.centerEyePosition, out position)) {
                // We may get 0,0,0 as first measurement
                if (position != Vector3.zero) {
                    //transform.position = tracker.transform.TransformPoint(position);
                    positionConfidence = 1;
                    status = Tracker.Status.Tracking;
                }
            }

            Quaternion rotation;
            if (device.TryGetFeatureValue(CommonUsages.centerEyeRotation, out rotation)) {
                //transform.rotation = tracker.transform.rotation * rotation;
                rotationConfidence = 1;
                if (positionalTracking == false)
                    status = Tracker.Status.Tracking;
            }

            bool userPresent = false;
            if (device.TryGetFeatureValue(CommonUsages.userPresence, out userPresent)) {
                // tracking is only true when the positional tracking is working
                // but when the user is not present, the tracking status is removed again
                if (!userPresent)
                    status = Tracker.Status.Present;
            }
#endif
        }

        public void Show(bool _) {
        }

        #endregion
    }

}