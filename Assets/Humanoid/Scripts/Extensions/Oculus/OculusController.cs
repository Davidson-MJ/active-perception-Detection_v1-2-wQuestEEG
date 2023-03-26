using UnityEngine;

namespace Passer.Tracking {
    using Passer.Humanoid;
    using Passer.Humanoid.Tracking;

    /// <summary>
    /// An Oculus controller
    /// </summary>
    // Base this on UnityXRController in the future?
    public class OculusController : SensorComponent {
#if hOCULUS
        public bool isLeft;

        public Vector3 joystick;
        public float indexTrigger;
        public float handTrigger;
        public float buttonAX;
        public float buttonBY;
        public float option;
        public float thumbrest;

        public bool positionalTracking = true;

        public override void UpdateComponent() {
            status = Tracker.Status.Tracking;
            if (OculusDevice.status == Tracker.Status.Unavailable)
                status = Tracker.Status.Unavailable;

            if (trackerTransform == null)
                trackerTransform = this.trackerTransform.parent;

            Sensor.ID sensorID = isLeft ? Sensor.ID.LeftHand : Sensor.ID.RightHand;

            if (OculusDevice.GetRotationalConfidence(sensorID) == 0)
                status = Tracker.Status.Present;

            if (status == Tracker.Status.Present || status == Tracker.Status.Unavailable) {
                positionConfidence = 0;
                rotationConfidence = 0;
                renderController = false;
                return;
            }

            Vector3 localSensorPosition = HumanoidTarget.ToVector3(OculusDevice.GetPosition(sensorID));
            transform.position = trackerTransform.TransformPoint(localSensorPosition);

            Quaternion localSensorRotation = HumanoidTarget.ToQuaternion(OculusDevice.GetRotation(sensorID));
            transform.rotation = trackerTransform.rotation * localSensorRotation;

            positionConfidence = OculusDevice.GetPositionalConfidence(sensorID);
            rotationConfidence = OculusDevice.GetRotationalConfidence(sensorID);
            renderController = show;

            UpdateInput(sensorID);
        }

        private void UpdateInput(Sensor.ID sensorID) {
            switch (sensorID) {
                case Sensor.ID.LeftHand:
                    UpdateLeftInput();
                    return;
                case Sensor.ID.RightHand:
                    UpdateRightInput();
                    return;
                default:
                    return;
            }
        }

        private void UpdateLeftInput() {
            OculusDevice.Controller controllerMask;

#if !UNITY_EDITOR
            if (!positionalTracking)
                controllerMask = OculusDevice.Controller.LTrackedRemote;
            else
#endif
            controllerMask = OculusDevice.Controller.LTouch;

            OculusDevice.ControllerState4 controllerState = OculusDevice.GetControllerState(controllerMask);

            float stickButton =
                OculusDevice.GetStickPress(controllerState) ? 1 : (
                OculusDevice.GetStickTouch(controllerState) ? 0 : -1);
            joystick = new Vector3(
                OculusDevice.GetHorizontalStick(controllerState, true),
                OculusDevice.GetVerticalStick(controllerState, true),
                stickButton);

            indexTrigger = OculusDevice.GetTrigger1(controllerState, true);
            handTrigger = OculusDevice.GetTrigger2(controllerState, true);

            buttonAX =
                OculusDevice.GetButton1Press(controllerState) ? 1 : (
                OculusDevice.GetButton1Touch(controllerState) ? 0 : -1);
            buttonBY =
                OculusDevice.GetButton2Press(controllerState) ? 1 : (
                OculusDevice.GetButton2Touch(controllerState) ? 0 : -1);
            thumbrest =
                OculusDevice.GetThumbRest(controllerState) ? 0 : -1;
            option =
                OculusDevice.GetButtonOptionPress(controllerState) ? 1 : 0;
        }

        private void UpdateRightInput() {
            OculusDevice.Controller controllerMask;
#if !UNITY_EDITOR
            if (!positionalTracking)
                controllerMask = OculusDevice.Controller.RTrackedRemote;
            else
#endif
            controllerMask = OculusDevice.Controller.RTouch;

            OculusDevice.ControllerState4 controllerState = OculusDevice.GetControllerState(controllerMask);

            float stickButton =
                OculusDevice.GetStickPress(controllerState) ? 1 : (
                OculusDevice.GetStickTouch(controllerState) ? 0 : -1);
            joystick = new Vector3(
                OculusDevice.GetHorizontalStick(controllerState, false),
                OculusDevice.GetVerticalStick(controllerState, false),
                stickButton);

            indexTrigger = OculusDevice.GetTrigger1(controllerState, false);
            handTrigger = OculusDevice.GetTrigger2(controllerState, false);

            buttonAX =
                OculusDevice.GetButton1Press(controllerState) ? 1 : (
                OculusDevice.GetButton1Touch(controllerState) ? 0 : -1);
            buttonBY =
                OculusDevice.GetButton2Press(controllerState) ? 1 : (
                OculusDevice.GetButton2Touch(controllerState) ? 0 : -1);
            option =
                0;
            thumbrest =
                OculusDevice.GetThumbRest(controllerState) ? 0 : -1;
        }

        public void Vibrate(float length, float strength) {
            Sensor.ID sensorID = isLeft ? Sensor.ID.LeftHand : Sensor.ID.RightHand;
            OculusDevice.Vibrate(sensorID, length, strength);
        }
#endif
    }
}