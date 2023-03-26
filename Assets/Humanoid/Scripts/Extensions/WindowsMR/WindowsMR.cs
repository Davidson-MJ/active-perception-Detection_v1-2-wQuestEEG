#if hWINDOWSMR && UNITY_2017_2_OR_NEWER && !UNITY_2020_1_OR_NEWER && UNITY_WSA_10_0

using UnityEngine;

namespace Passer.Humanoid {
    using Humanoid.Tracking;

    [System.Serializable]
    public class WindowsMRTracker : HumanoidTracker {
        public WindowsMRTracker() {
            deviceView = new DeviceView();
        }

        public override string name {
            get { return WindowsMRDevice.name; }
        }

        public GameObject hmdObject;

        public override HeadSensor headSensor {
            get { return humanoid.headTarget.mixedReality; }
        }
        public override ArmSensor leftHandSensor {
            get { return humanoid.leftHandTarget.mixedReality; }
        }
        public override ArmSensor rightHandSensor {
            get { return humanoid.rightHandTarget.mixedReality; }
        }
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


#region Start
        public override void StartTracker(HumanoidControl _humanoid) {
            humanoid = _humanoid;

            if (humanoid.headTarget.unity.enabled && UnityVRDevice.xrDevice == UnityVRDevice.XRDeviceType.WindowsMR)
                enabled = true;

            if (!enabled || UnityVRDevice.xrDevice != UnityVRDevice.XRDeviceType.WindowsMR)
                return;

            WindowsMRDevice.Start();

            AddTracker(humanoid, null);
        }
#endregion

#region Update
        public override void UpdateTracker() {
            if (!enabled || trackerTransform == null)
                return;

            if (UnityVRDevice.xrDevice != UnityVRDevice.XRDeviceType.WindowsMR) {
#if pUNITYXR
                status = tracker.status;
#else
                status = Status.Unavailable;
#endif
                return;
            }

            status = Status.Present;

            WindowsMRDevice.Update();
            status = Status.Tracking;
        }
#endregion

        public override void AdjustTracking(Vector3 v, Quaternion q) {
            if (trackerTransform != null) {
                trackerTransform.position += v;
                trackerTransform.rotation *= q;
            }
        }

    }
}
#endif