namespace Passer.Humanoid {
    using UnityEngine;
#if UNITY_2017_2_OR_NEWER
    using UnityEngine.XR;
#else
    using UnityEngine.VR;
#endif

    [System.Serializable]
    public class UnityVRTracker : HumanoidTracker {
#if !UNITY_2020_1_OR_NEWER
        public override void StartTracker(HumanoidControl _humanoid) {
            humanoid = _humanoid;
#if hLEGACYXR
            trackerTransform = humanoid.unity.trackerTransform;
#endif
            Passer.Tracking.UnityVRDevice.Start();
        }

        public override void UpdateTracker() {
            base.UpdateTracker();
        }

        public override void Calibrate() {
#if UNITY_ANDROID
            //InputTracking.Recenter();
            // This leads to a double calibration. Unclear how this function actually works
            // with positional tracking, so it is disabled for now
#endif
        }

        public override void AdjustTracking(Vector3 v, Quaternion q) {
            base.AdjustTracking(v, q);
        }
#endif
    }

}