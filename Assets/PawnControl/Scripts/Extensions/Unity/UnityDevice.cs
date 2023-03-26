using UnityEngine;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#else
    using UnityEngine.VR;
#endif

namespace Passer.Tracking {

    public static class UnityVRDevice {
        public static bool started;
        public static bool present;

        public static GameObject trackerObject;
        public static string trackerName = "UnityVR root";

        public static void Start() {
            xrDevice = DetermineLoadedDevice();
#if !pUNITYXR && !UNITY_2020_1_OR_NEWER
#if UNITY_2017_2_OR_NEWER
            present = XRDevice.isPresent;
#else
            present = VRDevice.isPresent;
#endif
#endif
            trackerObject = GameObject.Find(trackerName);
            started = true;
        }

        public enum XRDeviceType {
            None,
            Oculus,
            OpenVR,
            WindowsMR,
            Cardboard,
        };
        public static XRDeviceType xrDevice = XRDeviceType.None;

        private static XRDeviceType DetermineLoadedDevice() {
#if UNITY_2017_2_OR_NEWER
            if (XRSettings.enabled) {
                switch (XRSettings.loadedDeviceName) {

#else
            if (VRSettings.enabled) {
                switch (VRSettings.loadedDeviceName) {
#endif
                    case "OpenVR":
                    case "OpenVR Display":
                        return XRDeviceType.OpenVR;
                    case "Oculus":
                    case "Oculus Display":
                    case "oculus display":
                        return XRDeviceType.Oculus;
                    case "WindowsMR":
                        return XRDeviceType.WindowsMR;
                    case "cardboard":
                        return XRDeviceType.Cardboard;
                }
            }
            return XRDeviceType.None;
        }
    }
}