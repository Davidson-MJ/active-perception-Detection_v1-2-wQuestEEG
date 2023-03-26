using UnityEngine;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#else
    using UnityEngine.VR;
#endif

namespace Passer {
    using Pawn;

    [System.Serializable]
    public class UnityTracker : Tracker {
        public enum XRDeviceType {
            None,
            Oculus,
            OpenVR,
            WindowsMR
        };
        static public XRDeviceType xrDevice = XRDeviceType.None;

        [System.NonSerialized]
        public PawnControl pawn;
        [System.NonSerialized]
        public UnityCamera headSensor;

        #region Manage

        public override bool AddTracker(Transform rootTransform, string resourceName) {
            GameObject realWorld = GetRealWorld(rootTransform);
            trackerTransform = realWorld.transform.Find(resourceName);
            if (trackerTransform == null) {
                CreateUnityRoot(realWorld.transform, resourceName);

                trackerTransform.parent = realWorld.transform;
                return true;
            }
            return false;
        }

        protected virtual void CreateUnityRoot(Transform realWorld, string resourceName) {
            GameObject unityRootObject = new GameObject(resourceName);
            trackerTransform = unityRootObject.transform;
            trackerTransform.parent = realWorld.transform;
        }

        #endregion

        #region Start

        public override void StartTracker(Transform targetTransform) {
            if (!enabled)
                return;

            pawn = targetTransform.GetComponent<PawnControl>();

            xrDevice = DetermineLoadedDevice();
            if (xrDevice != XRDeviceType.None) {
                if (trackerTransform == null)
                    AddTracker(targetTransform, "Unity");
            }

        }

        public static XRDeviceType DetermineLoadedDevice() {
#if UNITY_2017_2_OR_NEWER
            if (XRSettings.enabled) {
                switch (XRSettings.loadedDeviceName) {

#else
            if (VRSettings.enabled) {
                switch (VRSettings.loadedDeviceName) {
#endif
                    case "OpenVR":
                        return XRDeviceType.OpenVR;
                    case "Oculus":
                        return XRDeviceType.Oculus;
                    case "WindowsMR":
                        return XRDeviceType.WindowsMR;
                }
            }
            return XRDeviceType.None;
        }
        #endregion

        #region Calibration

        public override void AdjustTracking(Vector3 v, Quaternion q) {
            base.AdjustTracking(v, q);
        }

        #endregion
    }

}