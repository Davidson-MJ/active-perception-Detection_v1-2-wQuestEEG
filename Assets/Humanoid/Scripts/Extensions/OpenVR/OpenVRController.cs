using System.Runtime.InteropServices;
using UnityEngine;
using Passer.Humanoid;
using Passer.Humanoid.Tracking;

namespace Passer.Tracking {

    /// <summary>
    /// An OpenVR controller
    /// </summary>
    public class OpenVRController : SensorComponent {

#if hOPENVR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
        protected const string resourceName = "Vive Controller";
        public int trackerId = -1;

        public bool isLeft;

        public enum ControllerType {
            SteamVRController,
            ValveIndex,
            OculusTouch,
            MixedReality
        }
        public ControllerType controllerType;

        public Vector3 joystick;
        public Vector3 touchpad;
        public float trigger;
        public float grip;
        public float aButton;
        public float bButton;

        [System.NonSerialized]
        ulong skeletonActionHandle;
        [System.NonSerialized]
        ulong hapticsActionHandle;
        [System.NonSerialized]
        InputSkeletalActionData_t tempSkeletonActionData = new InputSkeletalActionData_t();
        [System.NonSerialized]
        uint skeletonActionData_size;

        public const int numBones = 31;
        [System.NonSerialized]
        public VRBoneTransform_t[] tempBoneTransforms = new Passer.VRBoneTransform_t[numBones];

        public override void StartComponent(Transform trackerTransform) {
            base.StartComponent(trackerTransform);

            if (Passer.OpenVR.Input != null) {
                string actionName = isLeft ? "/actions/default/in/SkeletonLeftHand" : "/actions/default/in/SkeletonRightHand";
                EVRInputError err = Passer.OpenVR.Input.GetActionHandle(actionName, ref skeletonActionHandle);
                if (err != EVRInputError.None)
                    Debug.LogError("OpenVR.Input.GetActionHandle error: " + err.ToString());

                if (isLeft)
                    actionName = "/actions/default/out/lefthaptic";
                else
                    actionName = "/actions/default/out/righthaptic";
                err = Passer.OpenVR.Input.GetActionHandle(actionName, ref hapticsActionHandle);
                if (err != EVRInputError.None)
                    Debug.LogError("OpenVR.Input.GetActionHandle error: " + err.ToString());

                skeletonActionData_size = (uint)Marshal.SizeOf(tempSkeletonActionData);

                GetInputActionHandles();
            }
        }

        //string renderModelName;
        //string componentName = "RenderModel";
        public Passer.RenderModel_ControllerMode_State_t controllerModeState;

        public static OpenVRController NewController(HumanoidControl humanoid, int trackerId = -1) {
            GameObject trackerPrefab = Resources.Load(resourceName) as GameObject;
            GameObject trackerObject = (trackerPrefab == null) ? new GameObject(resourceName) : Instantiate(trackerPrefab);

            trackerObject.name = resourceName;

            OpenVRController trackerComponent = trackerObject.GetComponent<OpenVRController>();
            if (trackerComponent == null)
                trackerComponent = trackerObject.AddComponent<OpenVRController>();

            if (trackerId != -1)
                trackerComponent.trackerId = trackerId;
            trackerObject.transform.parent = humanoid.openVR.trackerTransform;

            trackerComponent.StartComponent(humanoid.openVR.trackerTransform);

            return trackerComponent;
        }

        public static bool IsValidController(uint sensorId) {
            Passer.ETrackedControllerRole role = Passer.OpenVR.System.GetControllerRoleForTrackedDeviceIndex(sensorId);
            return (role != Passer.ETrackedControllerRole.Invalid);
        }

        public static bool IsLeftController(uint sensorId) {
            Passer.ETrackedControllerRole role = Passer.OpenVR.System.GetControllerRoleForTrackedDeviceIndex(sensorId);
            return (role == Passer.ETrackedControllerRole.LeftHand);
        }

        public static bool IsRightController(uint sensorId) {
            Passer.ETrackedControllerRole role = Passer.OpenVR.System.GetControllerRoleForTrackedDeviceIndex(sensorId);
            return (role == Passer.ETrackedControllerRole.RightHand);
        }

        public override void UpdateComponent() {
            if (OpenVRDevice.status == Tracker.Status.Unavailable)
                status = Tracker.Status.Unavailable;

            if (Passer.OpenVR.System == null) {
                status = Tracker.Status.Unavailable;
                trackerId = -1;
            }

            if (trackerId > 0 && status != Tracker.Status.Unavailable) {
                Passer.ETrackedControllerRole role = Passer.OpenVR.System.GetControllerRoleForTrackedDeviceIndex((uint)trackerId);
                if ((isLeft && role != ETrackedControllerRole.LeftHand) ||
                    (!isLeft && role != ETrackedControllerRole.RightHand)
                    ) {

                    trackerId = -1;
                }
            }

            if (trackerId < 0)
                FindOutermostController(isLeft);

            if (OpenVRDevice.GetConfidence(trackerId) == 0) {
                status = OpenVRDevice.IsPresent(trackerId) ? Tracker.Status.Present : Tracker.Status.Unavailable;
                positionConfidence = 0;
                rotationConfidence = 0;
                renderController = false;
                return;
            }

            status = Tracker.Status.Tracking;
            Vector3 localSensorPosition = HumanoidTarget.ToVector3(OpenVRDevice.GetPosition(trackerId));
            Quaternion localSensorRotation = HumanoidTarget.ToQuaternion(OpenVRDevice.GetRotation(trackerId));
            transform.position = trackerTransform.TransformPoint(localSensorPosition);
            transform.rotation = trackerTransform.rotation * localSensorRotation;

            positionConfidence = OpenVRDevice.GetConfidence(trackerId);
            rotationConfidence = OpenVRDevice.GetConfidence(trackerId);
            renderController = show;

            Passer.VRControllerState_t controllerState = new Passer.VRControllerState_t();
            var system = Passer.OpenVR.System;
            uint controllerStateSize = (uint)Marshal.SizeOf(typeof(Passer.VRControllerState_t));
            bool newControllerState = system.GetControllerState((uint)trackerId, ref controllerState, controllerStateSize);
            if (system != null && newControllerState)
                UpdateInput(controllerState);

            Skeletal();

            UpdateVibration();
        }

        protected ushort vibrationStrength = 0;
        protected float vibrationStopTime = 0;


        private void UpdateVibration() {
            if (vibrationStopTime < Time.time)
                return;
            //Debug.Log("brrr  " + trackerId + " " + vibrationStrength);
            //Passer.OpenVR.System.TriggerHapticPulse((uint)trackerId, 0, vibrationStrength);
        }

        public void Vibrate(float length, float strength) {
            Passer.OpenVR.Input.TriggerHapticVibrationAction(hapticsActionHandle, 0, length, 1 / length, strength, 0);
            vibrationStrength = (ushort)(strength * 3999);
            vibrationStopTime = Time.time + length;

        }

        private void FindOutermostController(bool isLeft) {
            if (Passer.OpenVR.System == null)
                return;

            Vector outermostLocalPos = new Vector(isLeft ? -0.1F : 0.1F, 0, 0);

            for (int i = 0; i < Passer.OpenVR.k_unMaxTrackedDeviceCount; i++) {
                if (OpenVRDevice.GetDeviceClass(i) != Passer.ETrackedDeviceClass.Controller)
                    continue;

                Passer.ETrackedControllerRole role = Passer.OpenVR.System.GetControllerRoleForTrackedDeviceIndex((uint)i);
                if ((isLeft && role == Passer.ETrackedControllerRole.LeftHand) ||
                    (!isLeft && role == Passer.ETrackedControllerRole.RightHand)) {

                    trackerId = i;
                    return;
                }

                Vector sensorLocalPos = Rotation.Inverse(OpenVRDevice.GetRotation(0)) * (OpenVRDevice.GetPosition(i) - OpenVRDevice.GetPosition(0)); // 0 = HMD

                if ((isLeft && sensorLocalPos.x < outermostLocalPos.x && role != Passer.ETrackedControllerRole.RightHand) ||
                    (!isLeft && sensorLocalPos.x > outermostLocalPos.x) && role != Passer.ETrackedControllerRole.LeftHand) {

                    trackerId = i;
                    outermostLocalPos = sensorLocalPos;
                }
            }
        }

        // <summary>The order of the joints that OpenVR Skeleton Input is expecting.</summary>
        public class OpenVR_Skeleton_JointIndexes {
            public const int root = 0;
            public const int wrist = 1;
            public const int thumbMetacarpal = 2;
            public const int thumbProximal = 2;
            public const int thumbMiddle = 3;
            public const int thumbDistal = 4;
            public const int thumbTip = 5;
            public const int indexMetacarpal = 6;
            public const int indexProximal = 7;
            public const int indexMiddle = 8;
            public const int indexDistal = 9;
            public const int indexTip = 10;
            public const int middleMetacarpal = 11;
            public const int middleProximal = 12;
            public const int middleMiddle = 13;
            public const int middleDistal = 14;
            public const int middleTip = 15;
            public const int ringMetacarpal = 16;
            public const int ringProximal = 17;
            public const int ringMiddle = 18;
            public const int ringDistal = 19;
            public const int ringTip = 20;
            public const int pinkyMetacarpal = 21;
            public const int pinkyProximal = 22;
            public const int pinkyMiddle = 23;
            public const int pinkyDistal = 24;
            public const int pinkyTip = 25;
            public const int thumbAux = 26;
            public const int indexAux = 27;
            public const int middleAux = 28;
            public const int ringAux = 29;
            public const int pinkyAux = 30;
        }

        private void Skeletal() {
            ulong restrictToDevice = 0; // Any Device for now
            Passer.EVRInputError err = Passer.OpenVR.Input.GetSkeletalActionData(skeletonActionHandle, ref tempSkeletonActionData, skeletonActionData_size, restrictToDevice);
            if (err != Passer.EVRInputError.None) {
                Debug.LogError("GetSkeletalActionData error: " + err.ToString() + ", handle: " + skeletonActionHandle.ToString());
                return;
            }
            if (tempSkeletonActionData.bActive) {
                err = Passer.OpenVR.Input.GetSkeletalBoneData(skeletonActionHandle, Passer.EVRSkeletalTransformSpace.Parent, Passer.EVRSkeletalMotionRange.WithoutController, tempBoneTransforms, restrictToDevice);
                if (err != Passer.EVRInputError.None)
                    Debug.LogError("GetSkeletalBoneData error: " + err.ToString() + " handle: " + skeletonActionHandle.ToString());
            }
        }

        [System.NonSerialized]
        protected ulong actionHandleJoystick;
        [System.NonSerialized]
        protected ulong actionHandleJoystickTouch;
        [System.NonSerialized]
        protected ulong actionHandleJoystickPress;

        [System.NonSerialized]
        protected ulong actionHandleTouchpad;
        [System.NonSerialized]
        protected ulong actionHandleTouchpadTouch;
        [System.NonSerialized]
        protected ulong actionHandleTouchpadPress;
        [System.NonSerialized]
        protected ulong actionHandleTouchpadClick;

        [System.NonSerialized]
        protected ulong actionHandleTrigger;
        [System.NonSerialized]
        protected ulong actionHandleTriggerTouch;


        [System.NonSerialized]
        protected ulong actionHandleGrip;
        [System.NonSerialized]
        protected ulong actionHandleGripTouch;
        [System.NonSerialized]
        protected ulong actionHandleGripClick;

        [System.NonSerialized]
        protected ulong actionHandleButtonA;
        [System.NonSerialized]
        protected ulong actionHandleButtonATouch;

        [System.NonSerialized]
        protected ulong actionHandleButtonB;
        [System.NonSerialized]
        protected ulong actionHandleButtonBTouch;

        protected void GetInputActionHandles() {
            GetInputActionHandle(isLeft ? "LeftJoystick" : "RightJoystick", ref actionHandleJoystick);
            GetInputActionHandle(isLeft ? "LeftJoystickTouch" : "RightJoystickTouch", ref actionHandleJoystickTouch);
            GetInputActionHandle(isLeft ? "LeftJoystickPress" : "RightJoystickPress", ref actionHandleJoystickPress);

            GetInputActionHandle(isLeft ? "LeftTouchpad" : "RightTouchpad", ref actionHandleTouchpad);
            GetInputActionHandle(isLeft ? "LeftTouchpadTouch" : "RightTouchpadTouch", ref actionHandleTouchpadTouch);
            GetInputActionHandle(isLeft ? "LeftTouchpadPress" : "RightTouchpadPress", ref actionHandleTouchpadPress);
            GetInputActionHandle(isLeft ? "LeftTouchpadClick" : "RightTouchpadClick", ref actionHandleTouchpadClick);

            GetInputActionHandle(isLeft ? "LeftTrigger" : "RightTrigger", ref actionHandleTrigger);
            GetInputActionHandle(isLeft ? "LeftTriggerTouch" : "RightTriggerTouch", ref actionHandleTriggerTouch);

            GetInputActionHandle(isLeft ? "LeftGrip" : "RightGrip", ref actionHandleGrip);
            GetInputActionHandle(isLeft ? "LeftGripTouch" : "RightGripTouch", ref actionHandleGripTouch);
            GetInputActionHandle(isLeft ? "LeftGripClick" : "RightGripClick", ref actionHandleGripClick);

            GetInputActionHandle(isLeft ? "LeftButtonA" : "RightButtonA", ref actionHandleButtonA);
            GetInputActionHandle(isLeft ? "LeftButtonATouch" : "RightButtonATouch", ref actionHandleButtonATouch);

            GetInputActionHandle(isLeft ? "LeftButtonB" : "RightButtonB", ref actionHandleButtonB);
            GetInputActionHandle(isLeft ? "LeftButtonBTouch" : "RightButtonBTouch", ref actionHandleButtonBTouch);
        }

        protected static void GetInputActionHandle(string name, ref ulong actionHandle) {
            Passer.EVRInputError err;
            string path = "/actions/default/in/" + name;
            err = Passer.OpenVR.Input.GetActionHandle(path, ref actionHandle);
            if (err != Passer.EVRInputError.None) {
                Debug.LogError("OpenVR.Input.GetActionHandle error: " + err.ToString());
            }
        }

        public void UpdateInput(Passer.VRControllerState_t controllerState) {
            Vector2 joystickPosition = GetVector2(actionHandleJoystick);
            float joystickButton =
                GetBoolean(actionHandleJoystickPress) ? 1 :
                GetBoolean(actionHandleJoystickTouch) ? 0 : -1;
            joystick = new Vector3(joystickPosition.x, joystickPosition.y, joystickButton);

            Vector2 touchPadPosition = GetVector2(actionHandleTouchpad);
            float touchPadButton =
                GetBoolean(actionHandleTouchpadTouch) ?
                (GetBoolean(actionHandleTouchpadClick) ? 1 :
                GetFloat(actionHandleTouchpadPress)) : -1;
            touchpad = new Vector3(touchPadPosition.x, touchPadPosition.y, touchPadButton);

            float triggerPress = GetFloat(actionHandleTrigger);
            trigger =
                triggerPress == 0 && !GetBoolean(actionHandleTriggerTouch) ?
                -1 : triggerPress;

            float gripPress = GetBoolean(actionHandleGripClick) ? 1 : GetFloat(actionHandleGrip);
            grip =
                gripPress == 0 && !GetBoolean(actionHandleGripTouch) ?
                -1 : gripPress;
            //(GetBoolean(actionHandleGripClick) ? 1 : GetFloat(actionHandleGrip))
            // : -1;

            aButton =
                GetBoolean(actionHandleButtonA) ? 1 :
                GetBoolean(actionHandleButtonATouch) ? 0 : -1;
            bButton =
                GetBoolean(actionHandleButtonB) ? 1 :
                GetBoolean(actionHandleButtonBTouch) ? 0 : -1;
        }

        [System.NonSerialized]
        protected Passer.InputDigitalActionData_t digitalActionData = new Passer.InputDigitalActionData_t();
        [System.NonSerialized]
        protected readonly uint digitalActionDataSize = (uint)Marshal.SizeOf(typeof(Passer.InputDigitalActionData_t));

        protected bool GetBoolean(ulong actionHandle) {
            Passer.EVRInputError err;

            err = Passer.OpenVR.Input.GetDigitalActionData(actionHandle, ref digitalActionData, digitalActionDataSize, 0);
            if (err != Passer.EVRInputError.None)
                return false;

            return digitalActionData.bState;
        }

        [System.NonSerialized]
        protected Passer.InputAnalogActionData_t analogActionData = new Passer.InputAnalogActionData_t();
        [System.NonSerialized]
        protected readonly uint analogActionDataSize = (uint)Marshal.SizeOf(typeof(Passer.InputAnalogActionData_t));

        protected float GetFloat(ulong actionHandle) {
            Passer.EVRInputError err;

            err = Passer.OpenVR.Input.GetAnalogActionData(actionHandle, ref analogActionData, analogActionDataSize, 0);
            if (err != Passer.EVRInputError.None)
                return 0;

            return analogActionData.x;
        }

        protected Vector2 GetVector2(ulong actionHandle) {
            Passer.EVRInputError err;

            err = Passer.OpenVR.Input.GetAnalogActionData(actionHandle, ref analogActionData, analogActionDataSize, 0);
            if (err != Passer.EVRInputError.None)
                return Vector2.zero;

            Vector2 v = new Vector2(analogActionData.x, analogActionData.y);
            return v;
        }

        protected Vector3 GetVector3(ulong actionHandle) {
            Passer.EVRInputError err;

            err = Passer.OpenVR.Input.GetAnalogActionData(actionHandle, ref analogActionData, analogActionDataSize, 0);
            if (err != Passer.EVRInputError.None)
                return Vector3.zero;

            Vector3 v = new Vector3(analogActionData.x, analogActionData.y, analogActionData.z);
            return v;
        }
#endif
    }
}