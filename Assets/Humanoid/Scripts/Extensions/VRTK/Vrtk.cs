using UnityEngine;
#if hVRTK
using VRTK;
#endif

namespace Passer.Humanoid {

    [System.Serializable]
    public class VrtkTracker : HumanoidTracker {
#if hVRTK
        public override string name {
            get { return "VRTK"; }
        }

        public override HeadSensor headSensor {
            get { return humanoid.headTarget.vrtk; }
        }
        public override ArmSensor leftHandSensor {
            get { return humanoid.leftHandTarget.vrtk; }
        }
        public override ArmSensor rightHandSensor {
            get { return humanoid.rightHandTarget.vrtk; }
        }

        public VRTK_SDKManager sdkManager;

#region Start

        public override void StartTracker(HumanoidControl _humanoid) {
            humanoid = _humanoid;
            if (!enabled || sdkManager == null)
                return;

            sdkManager.LoadedSetupChanged += LoadedSetupChanged;
        }

        private void LoadedSetupChanged(VRTK_SDKManager sender, VRTK_SDKManager.LoadedSetupChangeEventArgs e) {
            VRTK_SDKSetup loadedSetup = sdkManager.loadedSetup;
            if (loadedSetup == null)
                return;

            trackerTransform = loadedSetup.actualBoundaries.transform;
            headSensor.sensorTransform = loadedSetup.actualHeadset.transform;

            VrtkHand.ControllerType controllerType = VrtkHand.ControllerType.None;
            if (loadedSetup.controllerSDK.GetType() == typeof(SDK_SteamVRController))
                controllerType = VrtkHand.ControllerType.SteamVRController;
            else if (loadedSetup.controllerSDK.GetType() == typeof(SDK_OculusController))
                controllerType = VrtkHand.ControllerType.OculusTouch;

            humanoid.leftHandTarget.vrtk.SetSensorTransform(loadedSetup.actualLeftController.transform, controllerType);
            humanoid.rightHandTarget.vrtk.SetSensorTransform(loadedSetup.actualRightController.transform, controllerType);
        }

#endregion

#region Update

        public override void UpdateTracker() {
            base.UpdateTracker();

            if (trackerTransform != null) {
                humanoid.transform.position = trackerTransform.position;
                humanoid.transform.rotation = trackerTransform.rotation;
            }
        }

#endregion

        public override void AdjustTracking(Vector3 v, Quaternion q) {
            if (trackerTransform != null) {
                trackerTransform.position += v;
                trackerTransform.rotation *= q;
            }
        }
#endif
    }
}
