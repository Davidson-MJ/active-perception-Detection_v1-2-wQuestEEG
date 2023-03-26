using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Passer.Humanoid.Tracking {

    public static class OculusDevice {
        public const string name = "Oculus";

        #region Versions

        private static System.Version _version;
        public static System.Version version {
            get {
                string pluginVersion = ovrp_GetVersion();

                if (pluginVersion != null) {
                    // Truncate unsupported trailing version info for System.Version. Original string is returned if not present.
                    pluginVersion = pluginVersion.Split('-')[0];
                    _version = new System.Version(pluginVersion);
                }
                else
                    _version = new System.Version(0, 0, 0);
                return _version;
            }

        }

        public static readonly System.Version version_1_16_0 = new System.Version(1, 16, 0);

        #endregion

        public static Passer.Tracker.Status status;

        private static Sensor.State[] sensorStates;

        public static void Start() {
            status = Passer.Tracker.Status.Unavailable;

            string pluginVersion = ovrp_GetVersion();

            if (pluginVersion != null) {
                // Truncate unsupported trailing version info for System.Version. Original string is returned if not present.
                pluginVersion = pluginVersion.Split('-')[0];
                _version = new System.Version(pluginVersion);
            }
            else
                _version = new System.Version(0, 0, 0);
            UnityEngine.Debug.Log("oculus version: " + _version);

            sensorStates = new Sensor.State[(int)Sensor.ID.Count];
            sensorStates[(int)Sensor.ID.Head].sensorID = (int)Node.Head;
            sensorStates[(int)Sensor.ID.LeftHand].sensorID = (int)Node.HandLeft;
            sensorStates[(int)Sensor.ID.RightHand].sensorID = (int)Node.HandRight;
            sensorStates[(int)Sensor.ID.Hips].sensorID = -1;
            sensorStates[(int)Sensor.ID.LeftFoot].sensorID = -1;
            sensorStates[(int)Sensor.ID.RightFoot].sensorID = -1;
            sensorStates[(int)Sensor.ID.Tracker1].sensorID = (int)Node.TrackerZero;
            sensorStates[(int)Sensor.ID.Tracker2].sensorID = (int)Node.TrackerOne;
            sensorStates[(int)Sensor.ID.Tracker3].sensorID = (int)Node.TrackerTwo;
            sensorStates[(int)Sensor.ID.Tracker4].sensorID = (int)Node.TrackerThree;

            // Initial filling of values
            Update();
        }

        public static void Update() {
            if (sensorStates == null)
                return;

            for (int i = 0; i < sensorStates.Length; i++) {
                if (sensorStates[i].sensorID < 0)
                    continue;

                sensorStates[i].present = (ovrp_GetNodePresent(sensorStates[i].sensorID) == Bool.True);
                sensorStates[i].confidence = (ovrp_GetNodeOrientationTracked(sensorStates[i].sensorID) == Bool.True) ? 1 : 0;

                if (sensorStates[i].confidence > 0)
                    status = Passer.Tracker.Status.Tracking;

                Pose pose = ovrp_GetNodePoseState(Step.Render, sensorStates[i].sensorID).Pose;
                sensorStates[i].position = new Vector(pose.Position.x, pose.Position.y + eyeHeight, -pose.Position.z);
                sensorStates[i].rotation = new Rotation(-pose.Orientation.x, -pose.Orientation.y, pose.Orientation.z, pose.Orientation.w);
            }
        }

        public static bool userPresent {
            get {
                return ovrp_GetUserPresent() == Bool.True;
            }
        }

        public static bool positionalTracking {
            set {
                ovrp_SetTrackingPositionEnabled(value ? Bool.True : Bool.False);
            }
        }
        public static Pose GetPose(int sensorID) {
            return ovrp_GetNodePoseState(Step.Render, sensorID).Pose;
        }

        public static Vector GetPosition(Sensor.ID sensorID) {
            if (sensorStates == null)
                return Vector.zero;

            return sensorStates[(int)sensorID].position;
        }

        public static Rotation GetRotation(Sensor.ID sensorID) {
            if (sensorStates == null)
                return Rotation.identity;

            return sensorStates[(int)sensorID].rotation;
        }

        public static float GetPositionalConfidence(Sensor.ID sensorID) {
            if (sensorStates == null)
                return 0;

            return (ovrp_GetNodePositionTracked(sensorStates[(int)sensorID].sensorID) == Bool.True) ? 0.99F : 0;
        }

        public static float GetRotationalConfidence(Sensor.ID sensorID) {
            if (sensorStates == null)
                return 0;

            if (ovrp_GetNodePositionTracked(sensorStates[(int)sensorID].sensorID) == Bool.True)
                return sensorStates[(int)sensorID].confidence;
            else
                return sensorStates[(int)sensorID].confidence * 0.9F; // without positional tracking, there is no drift correction
        }

        public static bool IsPresent(Sensor.ID sensorID) {
            if (sensorStates == null)
                return false;

            return sensorStates[(int)sensorID].present;
        }

        public static float GetConfidence(int sensorID) {
            return (ovrp_GetNodeOrientationTracked(sensorID) == Bool.True) ? 1 : 0;
        }

        public static void GetControllerInput(Sensor.ID sensorID, ref ControllerButtons input) {
            Controller controllerMask;
            switch (sensorID) {
                case Sensor.ID.LeftHand:
#if UNITY_ANDROID
                    controllerMask = Controller.LTrackedRemote;
#else
                    controllerMask = Controller.LTouch;
#endif
                    break;
                case Sensor.ID.RightHand:
#if UNITY_ANDROID
                    controllerMask = Controller.RTrackedRemote;
#else
                    controllerMask = Controller.RTouch;
#endif
                    break;
                default:
                    return;
            }
            bool isLeft = (sensorID == Sensor.ID.LeftHand);

            ControllerState4 controllerState = GetControllerState(controllerMask);

            input.stickHorizontal = GetHorizontalStick(controllerState, isLeft);
            input.stickVertical = GetVerticalStick(controllerState, isLeft);
            input.stickPress = GetStickPress(controllerState);
            input.stickTouch = GetStickTouch(controllerState);

            input.buttons[0] = GetButton1Press(controllerState);
            input.buttons[1] = GetButton2Press(controllerState);

            input.trigger1 = GetTrigger1(controllerState, isLeft);
            input.trigger2 = GetTrigger2(controllerState, isLeft);

            input.up = (input.stickVertical > 0.3F);
            input.down = (input.stickVertical < -0.3F);
            input.left = (input.stickHorizontal < -0.3F);
            input.right = (input.stickHorizontal > 0.3F);
        }

        public static ControllerState4 GetControllerState(OculusDevice.Controller controllerMask) {
            ControllerState4 controllerState;
            if (_version < version_1_16_0) {
                ControllerState2 controllerState2 = ovrp_GetControllerState2((uint)controllerMask);
                controllerState = new ControllerState4(controllerState2);
            }
            else {
                controllerState = new ControllerState4();
                ovrp_GetControllerState4((uint)controllerMask, ref controllerState);
            }
            return controllerState;
        }

        public static float GetHorizontalStick(ControllerState4 controllerState, bool isLeft) {
            float stickHorizontalValue = isLeft ? controllerState.LThumbstick.x : controllerState.RThumbstick.x;
            return stickHorizontalValue;
        }

        public static float GetVerticalStick(ControllerState4 controllerState, bool isLeft) {
            float stickVerticalValue = isLeft ? controllerState.LThumbstick.y : controllerState.RThumbstick.y;
            return stickVerticalValue;
        }

        public static float GetHorizontalTouch(ControllerState4 controllerState, bool isLeft) {
            float stickHorizontalValue = isLeft ? controllerState.LTouchpad.x : controllerState.RTouchpad.x;
            return stickHorizontalValue;
        }

        public static float GetVerticalTouch(ControllerState4 controllerState, bool isLeft) {
            float stickVerticalValue = isLeft ? controllerState.LTouchpad.y : controllerState.RTouchpad.y;
            return stickVerticalValue;
        }
        public static bool GetStickPress(ControllerState4 controllerState) {
            RawButton stickButton = RawButton.LThumbstick | RawButton.RThumbstick;
            bool stickButtonValue = (controllerState.Buttons & (uint)stickButton) != 0;
            return stickButtonValue;
        }

        public static bool GetStickTouch(ControllerState4 controllerState) {
            RawTouch stickTouch = RawTouch.LThumbstick | RawTouch.RThumbstick;
            bool stickTouchValue = (controllerState.Touches & (uint)stickTouch) != 0;
            return stickTouchValue;
        }

        public static bool GetButton1Press(ControllerState4 controllerState) {
            uint button = (uint)RawButton.X | (uint)RawButton.A;
            bool buttonValue = (controllerState.Buttons & button) != 0;
            return buttonValue;
        }

        public static bool GetButton1Touch(ControllerState4 controllerState) {
            uint button = (uint)RawTouch.X | (uint)RawTouch.A;
            bool buttonTouchValue = (controllerState.Touches & button) != 0;
            return buttonTouchValue;
        }

        public static bool GetButton2Press(ControllerState4 controllerState) {
            uint button = (uint)RawButton.Y | (uint)RawButton.B;
            bool buttonValue = (controllerState.Buttons & button) != 0;
            return buttonValue;
        }

        public static bool GetButton2Touch(ControllerState4 controllerState) {
            uint button = (uint)RawTouch.Y | (uint)RawTouch.B;
            bool buttonTouchValue = (controllerState.Touches & button) != 0;
            return buttonTouchValue;
        }

        // always give true... Maybe because I'm using an engineering sample?
        public static bool GetButton2Near(ControllerState4 controllerState) {
            uint mask = (uint)RawNearTouch.LThumbButtons | (uint)RawNearTouch.RThumbButtons;
            bool isNear = (controllerState.NearTouches & mask) != 0;
            return isNear;
        }

        public static bool GetButtonOptionPress(ControllerState4 controllerState) {
            uint button = (uint)RawButton.Back | (uint)RawButton.Start;
            bool buttonValue = (controllerState.Buttons & button) != 0;
            return buttonValue;
        }

        public static float GetTrigger1(ControllerState4 controllerState, bool isLeft) {
            float trigger1Value = isLeft ? controllerState.LIndexTrigger : controllerState.RIndexTrigger;

            // always give true... Maybe because I'm using an engineering sample?
            //uint nearId = (uint)RawNearTouch.LIndexTrigger | (uint)RawNearTouch.RIndexTrigger;
            //bool trigger1Near = (controllerState.NearTouches & nearId) != 0;

            uint touchId = (uint)RawTouch.LIndexTrigger | (uint)RawTouch.RIndexTrigger;
            bool trigger1Touch = (controllerState.Touches & touchId) != 0;
            if (!trigger1Touch)
                trigger1Value = -1F;

            return trigger1Value;
        }

        public static float GetTrigger2(ControllerState4 controllerState, bool isLeft) {
            float trigger2Value = isLeft ? controllerState.LHandTrigger : controllerState.RHandTrigger;
            return trigger2Value;
        }

        public static bool GetThumbRest(ControllerState4 controllerState) {
            RawTouch touchMask = RawTouch.LThumbRest | RawTouch.RThumbRest;
            bool touch = (controllerState.Touches & (uint)touchMask) != 0;
            return touch;
        }

        public static bool GetSkeleton(bool isLeft, out Skeleton skeleton) {
            return GetSkeleton(isLeft ? SkeletonType.HandLeft : SkeletonType.HandRight, out skeleton);
        }
        public static bool GetSkeleton(SkeletonType skeletonType, out Skeleton skeleton) {
#if OVRPLUGIN_UNSUPPORTED_PLATFORM
		skeleton = default(Skeleton);
		return false;
#else
            if (_version >= OVRP_1_44_0.version) {
                return OVRP_1_44_0.ovrp_GetSkeleton(skeletonType, out skeleton) == Result.Success;
            }
            else {
                skeleton = default(Skeleton);
                return false;
            }
#endif
        }

        public static UnityEngine.Quaternion GetSkeletonBoneRotation(Bone bone) {
            return new UnityEngine.Quaternion(-bone.Pose.Orientation.x, -bone.Pose.Orientation.y, bone.Pose.Orientation.z, bone.Pose.Orientation.w);
        }

        public static UnityEngine.Vector3 GetSkeletonBonePosition(Bone bone) {
            return new UnityEngine.Vector3(bone.Pose.Position.x, bone.Pose.Position.y, -bone.Pose.Position.z);
        }

        private static HandStateInternal cachedHandState = new HandStateInternal();
        public static bool GetHandState(Step stepId, Hand hand, ref HandState handState) {
#if OVRPLUGIN_UNSUPPORTED_PLATFORM
		return false;
#else
            if (_version >= OVRP_1_44_0.version) {
                Result res = OVRP_1_44_0.ovrp_GetHandState(stepId, hand, out cachedHandState);
                if (res == Result.Success) {
                    // attempt to avoid allocations if client provides appropriately pre-initialized HandState
                    if (handState.BoneRotations == null || handState.BoneRotations.Length != ((int)BoneId.Hand_End - (int)BoneId.Hand_Start)) {
                        handState.BoneRotations = new Quatf[(int)BoneId.Hand_End - (int)BoneId.Hand_Start];
                    }
                    if (handState.PinchStrength == null || handState.PinchStrength.Length != (int)HandFinger.Max) {
                        handState.PinchStrength = new float[(int)HandFinger.Max];
                    }
                    if (handState.FingerConfidences == null || handState.FingerConfidences.Length != (int)HandFinger.Max) {
                        handState.FingerConfidences = new TrackingConfidence[(int)HandFinger.Max];
                    }

                    // unrolling the arrays is necessary to avoid per-frame allocations during marshaling
                    handState.Status = cachedHandState.Status;
                    handState.RootPose = cachedHandState.RootPose;
                    handState.BoneRotations[0] = cachedHandState.BoneRotations_0;
                    handState.BoneRotations[1] = cachedHandState.BoneRotations_1;
                    handState.BoneRotations[2] = cachedHandState.BoneRotations_2;
                    handState.BoneRotations[3] = cachedHandState.BoneRotations_3;
                    handState.BoneRotations[4] = cachedHandState.BoneRotations_4;
                    handState.BoneRotations[5] = cachedHandState.BoneRotations_5;
                    handState.BoneRotations[6] = cachedHandState.BoneRotations_6;
                    handState.BoneRotations[7] = cachedHandState.BoneRotations_7;
                    handState.BoneRotations[8] = cachedHandState.BoneRotations_8;
                    handState.BoneRotations[9] = cachedHandState.BoneRotations_9;
                    handState.BoneRotations[10] = cachedHandState.BoneRotations_10;
                    handState.BoneRotations[11] = cachedHandState.BoneRotations_11;
                    handState.BoneRotations[12] = cachedHandState.BoneRotations_12;
                    handState.BoneRotations[13] = cachedHandState.BoneRotations_13;
                    handState.BoneRotations[14] = cachedHandState.BoneRotations_14;
                    handState.BoneRotations[15] = cachedHandState.BoneRotations_15;
                    handState.BoneRotations[16] = cachedHandState.BoneRotations_16;
                    handState.BoneRotations[17] = cachedHandState.BoneRotations_17;
                    handState.BoneRotations[18] = cachedHandState.BoneRotations_18;
                    handState.BoneRotations[19] = cachedHandState.BoneRotations_19;
                    handState.BoneRotations[20] = cachedHandState.BoneRotations_20;
                    handState.BoneRotations[21] = cachedHandState.BoneRotations_21;
                    handState.BoneRotations[22] = cachedHandState.BoneRotations_22;
                    handState.BoneRotations[23] = cachedHandState.BoneRotations_23;
                    handState.Pinches = cachedHandState.Pinches;
                    handState.PinchStrength[0] = cachedHandState.PinchStrength_0;
                    handState.PinchStrength[1] = cachedHandState.PinchStrength_1;
                    handState.PinchStrength[2] = cachedHandState.PinchStrength_2;
                    handState.PinchStrength[3] = cachedHandState.PinchStrength_3;
                    handState.PinchStrength[4] = cachedHandState.PinchStrength_4;
                    handState.PointerPose = cachedHandState.PointerPose;
                    handState.HandScale = cachedHandState.HandScale;
                    handState.HandConfidence = cachedHandState.HandConfidence;
                    handState.FingerConfidences[0] = cachedHandState.FingerConfidences_0;
                    handState.FingerConfidences[1] = cachedHandState.FingerConfidences_1;
                    handState.FingerConfidences[2] = cachedHandState.FingerConfidences_2;
                    handState.FingerConfidences[3] = cachedHandState.FingerConfidences_3;
                    handState.FingerConfidences[4] = cachedHandState.FingerConfidences_4;
                    handState.RequestedTimeStamp = cachedHandState.RequestedTimeStamp;
                    handState.SampleTimeStamp = cachedHandState.SampleTimeStamp;

                    return true;
                }
                else {
                    return false;
                }
            }
            else {
                return false;
            }
#endif
        }

        public static void Vibrate(Sensor.ID sensorID, float length, float strength) {
            Controller controllerMask;
            switch (sensorID) {
                case Sensor.ID.LeftHand:
                    controllerMask = Controller.LTouch;
                    break;
                case Sensor.ID.RightHand:
                    controllerMask = Controller.RTouch;
                    break;
                default:
                    return;
            }
            ovrp_SetControllerVibration((uint)controllerMask, 0.5F, strength);
        }

        public static float eyeHeight {
            get { return ovrp_GetUserEyeHeight(); }
        }

        #region DataTypes
        public enum RawNearTouch {
            None = 0,          ///< Maps to Physical NearTouch: [Gamepad, Touch, LTouch, RTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            LIndexTrigger = 0x00000001, ///< Maps to Physical NearTouch: [Touch, LTouch: Implies finger is in close proximity to LIndexTrigger.], [Gamepad, RTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            LThumbButtons = 0x00000002, ///< Maps to Physical NearTouch: [Touch, LTouch: Implies thumb is in close proximity to LThumbstick OR X/Y buttons.], [Gamepad, RTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            RIndexTrigger = 0x00000004, ///< Maps to Physical NearTouch: [Touch, RTouch: Implies finger is in close proximity to RIndexTrigger.], [Gamepad, LTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            RThumbButtons = 0x00000008, ///< Maps to Physical NearTouch: [Touch, RTouch: Implies thumb is in close proximity to RThumbstick OR A/B buttons.], [Gamepad, LTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            Any = ~None,      ///< Maps to Physical NearTouch: [Touch, LTouch, RTouch: Any], [Gamepad, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
        }

        public enum RawTouch {
            None = 0,                            ///< Maps to Physical Touch: [Gamepad, Touch, LTouch, RTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            A = RawButton.A,                  ///< Maps to Physical Touch: [Touch, RTouch: A], [Gamepad, LTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            B = RawButton.B,                  ///< Maps to Physical Touch: [Touch, RTouch: B], [Gamepad, LTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            X = RawButton.X,                  ///< Maps to Physical Touch: [Touch, LTouch: X], [Gamepad, RTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            Y = RawButton.Y,                  ///< Maps to Physical Touch: [Touch, LTouch: Y], [Gamepad, RTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            LIndexTrigger = 0x00001000,                   ///< Maps to Physical Touch: [Touch, LTouch: LIndexTrigger], [Gamepad, RTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            LThumbstick = RawButton.LThumbstick,        ///< Maps to Physical Touch: [Touch, LTouch: LThumbstick], [Gamepad, RTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            LThumbRest = 0x00000800,                   ///< Maps to Physical Touch: [Touch, LTouch: LThumbRest], [Gamepad, RTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            LTouchpad = RawButton.LTouchpad,          ///< Maps to Physical Touch: [LTrackedRemote, Touchpad: LTouchpad], [Gamepad, Touch, LTouch, RTouch, RTrackedRemote, Remote: None]
            RIndexTrigger = 0x00000010,                   ///< Maps to Physical Touch: [Touch, RTouch: RIndexTrigger], [Gamepad, LTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            RThumbstick = RawButton.RThumbstick,        ///< Maps to Physical Touch: [Touch, RTouch: RThumbstick], [Gamepad, LTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            RThumbRest = 0x00000008,                   ///< Maps to Physical Touch: [Touch, RTouch: RThumbRest], [Gamepad, LTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            RTouchpad = RawButton.RTouchpad,          ///< Maps to Physical Touch: [RTrackedRemote: RTouchpad], [Gamepad, Touch, LTouch, RTouch, LTrackedRemote, Touchpad, Remote: None]
            Any = ~None,                        ///< Maps to Physical Touch: [Touch, LTouch, RTouch, LTrackedRemote, RTrackedRemote, Touchpad: Any], [Gamepad, Remote: None]
        }

        public enum RawButton {
            None = 0,          ///< Maps to Physical Button: [Gamepad, Touch, LTouch, RTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            A = 0x00000001, ///< Maps to Physical Button: [Gamepad, Touch, RTouch: A], [LTrackedRemote: LIndexTrigger], [RTrackedRemote: RIndexTrigger], [LTouch, Touchpad, Remote: None]
            B = 0x00000002, ///< Maps to Physical Button: [Gamepad, Touch, RTouch: B], [LTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            X = 0x00000100, ///< Maps to Physical Button: [Gamepad, Touch, LTouch: X], [RTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            Y = 0x00000200, ///< Maps to Physical Button: [Gamepad, Touch, LTouch: Y], [RTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            Start = 0x00100000, ///< Maps to Physical Button: [Gamepad, Touch, LTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: Start], [RTouch: None]
            Back = 0x00200000, ///< Maps to Physical Button: [Gamepad, LTrackedRemote, RTrackedRemote, Touchpad, Remote: Back], [Touch, LTouch, RTouch: None]
            LShoulder = 0x00000800, ///< Maps to Physical Button: [Gamepad: LShoulder], [Touch, LTouch, RTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            LIndexTrigger = 0x10000000, ///< Maps to Physical Button: [Gamepad, Touch, LTouch, LTrackedRemote: LIndexTrigger], [RTouch, RTrackedRemote, Touchpad, Remote: None]
            LHandTrigger = 0x20000000, ///< Maps to Physical Button: [Touch, LTouch: LHandTrigger], [Gamepad, RTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            LThumbstick = 0x00000400, ///< Maps to Physical Button: [Gamepad, Touch, LTouch: LThumbstick], [RTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            LThumbstickUp = 0x00000010, ///< Maps to Physical Button: [Gamepad, Touch, LTouch: LThumbstickUp], [RTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            LThumbstickDown = 0x00000020, ///< Maps to Physical Button: [Gamepad, Touch, LTouch: LThumbstickDown], [RTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            LThumbstickLeft = 0x00000040, ///< Maps to Physical Button: [Gamepad, Touch, LTouch: LThumbstickLeft], [RTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            LThumbstickRight = 0x00000080, ///< Maps to Physical Button: [Gamepad, Touch, LTouch: LThumbstickRight], [RTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            LTouchpad = 0x40000000, ///< Maps to Physical Button: [LTrackedRemote: LTouchpad], [Gamepad, Touch, LTouch, RTouch, RTrackedRemote, Touchpad, Remote: None]
            RShoulder = 0x00000008, ///< Maps to Physical Button: [Gamepad: RShoulder], [Touch, LTouch, RTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            RIndexTrigger = 0x04000000, ///< Maps to Physical Button: [Gamepad, Touch, RTouch, RTrackedRemote: RIndexTrigger], [LTouch, LTrackedRemote, Touchpad, Remote: None]
            RHandTrigger = 0x08000000, ///< Maps to Physical Button: [Touch, RTouch: RHandTrigger], [Gamepad, LTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            RThumbstick = 0x00000004, ///< Maps to Physical Button: [Gamepad, Touch, RTouch: RThumbstick], [LTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            RThumbstickUp = 0x00001000, ///< Maps to Physical Button: [Gamepad, Touch, RTouch: RThumbstickUp], [LTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            RThumbstickDown = 0x00002000, ///< Maps to Physical Button: [Gamepad, Touch, RTouch: RThumbstickDown], [LTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            RThumbstickLeft = 0x00004000, ///< Maps to Physical Button: [Gamepad, Touch, RTouch: RThumbstickLeft], [LTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            RThumbstickRight = 0x00008000, ///< Maps to Physical Button: [Gamepad, Touch, RTouch: RThumbstickRight], [LTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            RTouchpad = unchecked((int)0x80000000),///< Maps to Physical Button: [Gamepad, Touch, LTouch, RTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: None]
            DpadUp = 0x00010000, ///< Maps to Physical Button: [Gamepad, LTrackedRemote, RTrackedRemote, Touchpad, Remote: DpadUp], [Touch, LTouch, RTouch: None]
            DpadDown = 0x00020000, ///< Maps to Physical Button: [Gamepad, LTrackedRemote, RTrackedRemote, Touchpad, Remote: DpadDown], [Touch, LTouch, RTouch: None]
            DpadLeft = 0x00040000, ///< Maps to Physical Button: [Gamepad, LTrackedRemote, RTrackedRemote, Touchpad, Remote: DpadLeft], [Touch, LTouch, RTouch: None]
            DpadRight = 0x00080000, ///< Maps to Physical Button: [Gamepad, LTrackedRemote, RTrackedRemote, Touchpad, Remote: DpadRight], [Touch, LTouch, RTouch: None]
            Any = ~None,      ///< Maps to Physical Button: [Gamepad, Touch, LTouch, RTouch, LTrackedRemote, RTrackedRemote, Touchpad, Remote: Any]
        }

        public enum Bool {
            False = 0,
            True
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Vector2f {
            public float x;
            public float y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Vector3f {
            public float x;
            public float y;
            public float z;
            public static readonly Vector3f zero = new Vector3f { x = 0.0f, y = 0.0f, z = 0.0f };
            public override string ToString() {
                return string.Format("{0}, {1}, {2}", x, y, z);
            }
            public UnityEngine.Vector3 ToVector3() {
                return new UnityEngine.Vector3(x, y, -z);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Quatf {
            public float x;
            public float y;
            public float z;
            public float w;
            public static readonly Quatf identity = new Quatf { x = 0.0f, y = 0.0f, z = 0.0f, w = 1.0f };
            public override string ToString() {
                return string.Format("{0}, {1}, {2}, {3}", x, y, z, w);
            }

            public UnityEngine.Quaternion ToQuaternion() {
                return new UnityEngine.Quaternion(-x, -y, z, w);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Pose {
            public Quatf Orientation;
            public Vector3f Position;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PoseStatef {
            public Pose Pose;
            public Vector3f Velocity;
            public Vector3f Acceleration;
            public Vector3f AngularVelocity;
            public Vector3f AngularAcceleration;
            double Time;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ControllerState2 {
            public uint ConnectedControllers;
            public uint Buttons;
            public uint Touches;
            public uint NearTouches;
            public float LIndexTrigger;
            public float RIndexTrigger;
            public float LHandTrigger;
            public float RHandTrigger;
            public Vector2f LThumbstick;
            public Vector2f RThumbstick;
            public Vector2f LTouchpad;
            public Vector2f RTouchpad;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ControllerState4 {
            public uint ConnectedControllers;
            public uint Buttons;
            public uint Touches;
            public uint NearTouches;
            public float LIndexTrigger;
            public float RIndexTrigger;
            public float LHandTrigger;
            public float RHandTrigger;
            public Vector2f LThumbstick;
            public Vector2f RThumbstick;
            public Vector2f LTouchpad;
            public Vector2f RTouchpad;
            public byte LBatteryPercentRemaining;
            public byte RBatteryPercentRemaining;
            public byte LRecenterCount;
            public byte RRecenterCount;
            public byte Reserved_27;
            public byte Reserved_26;
            public byte Reserved_25;
            public byte Reserved_24;
            public byte Reserved_23;
            public byte Reserved_22;
            public byte Reserved_21;
            public byte Reserved_20;
            public byte Reserved_19;
            public byte Reserved_18;
            public byte Reserved_17;
            public byte Reserved_16;
            public byte Reserved_15;
            public byte Reserved_14;
            public byte Reserved_13;
            public byte Reserved_12;
            public byte Reserved_11;
            public byte Reserved_10;
            public byte Reserved_09;
            public byte Reserved_08;
            public byte Reserved_07;
            public byte Reserved_06;
            public byte Reserved_05;
            public byte Reserved_04;
            public byte Reserved_03;
            public byte Reserved_02;
            public byte Reserved_01;
            public byte Reserved_00;

            public ControllerState4(ControllerState2 cs) {
                ConnectedControllers = cs.ConnectedControllers;
                Buttons = cs.Buttons;
                Touches = cs.Touches;
                NearTouches = cs.NearTouches;
                LIndexTrigger = cs.LIndexTrigger;
                RIndexTrigger = cs.RIndexTrigger;
                LHandTrigger = cs.LHandTrigger;
                RHandTrigger = cs.RHandTrigger;
                LThumbstick = cs.LThumbstick;
                RThumbstick = cs.RThumbstick;
                LTouchpad = cs.LTouchpad;
                RTouchpad = cs.RTouchpad;
                LBatteryPercentRemaining = 0;
                RBatteryPercentRemaining = 0;
                LRecenterCount = 0;
                RRecenterCount = 0;
                Reserved_27 = 0;
                Reserved_26 = 0;
                Reserved_25 = 0;
                Reserved_24 = 0;
                Reserved_23 = 0;
                Reserved_22 = 0;
                Reserved_21 = 0;
                Reserved_20 = 0;
                Reserved_19 = 0;
                Reserved_18 = 0;
                Reserved_17 = 0;
                Reserved_16 = 0;
                Reserved_15 = 0;
                Reserved_14 = 0;
                Reserved_13 = 0;
                Reserved_12 = 0;
                Reserved_11 = 0;
                Reserved_10 = 0;
                Reserved_09 = 0;
                Reserved_08 = 0;
                Reserved_07 = 0;
                Reserved_06 = 0;
                Reserved_05 = 0;
                Reserved_04 = 0;
                Reserved_03 = 0;
                Reserved_02 = 0;
                Reserved_01 = 0;
                Reserved_00 = 0;
            }
        }

        public enum Tracker {
            None = -1,
            Zero = 0,
            One = 1,
            Two = 2,
            Three = 3,
            Count,
        }

        public enum Node {
            None = -1,
            EyeLeft = 0,
            EyeRight = 1,
            EyeCenter = 2,
            HandLeft = 3,
            HandRight = 4,
            TrackerZero = 5,
            TrackerOne = 6,
            TrackerTwo = 7,
            TrackerThree = 8,
            Head = 9,
            Count,
        }

        public enum Controller {
            None = 0,
            LTouch = 0x00000001,
            RTouch = 0x00000002,
            Touch = LTouch | RTouch,
            Remote = 0x00000004,
            Gamepad = 0x00000010,
            Touchpad = 0x08000000,
            LTrackedRemote = 0x01000000,
            RTrackedRemote = 0x02000000,
            Active = unchecked((int)0x80000000),
            All = ~None,
        }

        public enum Step {
            Render = -1,
            Physics = 0,
        }

        public enum TrackingConfidence {
            Low = 0,
            High = 0x3f800000,
        }

        public enum Hand {
            None = -1,
            HandLeft = 0,
            HandRight = 1,
        }

        //[Flags]
        public enum HandStatus {
            HandTracked = (1 << 0), // if this is set the hand pose and bone rotations data is usable
            InputStateValid = (1 << 1), // if this is set the pointer pose and pinch data is usable
            SystemGestureInProgress = (1 << 6), // if this is set the hand is currently processing a system gesture
        }

        public enum BoneId {
            Invalid = -1,

            Hand_Start = 0,
            Hand_WristRoot = Hand_Start + 0, // root frame of the hand, where the wrist is located
            Hand_ForearmStub = Hand_Start + 1, // frame for user's forearm
            Hand_Thumb0 = Hand_Start + 2, // thumb trapezium bone
            Hand_Thumb1 = Hand_Start + 3, // thumb metacarpal bone
            Hand_Thumb2 = Hand_Start + 4, // thumb proximal phalange bone
            Hand_Thumb3 = Hand_Start + 5, // thumb distal phalange bone
            Hand_Index1 = Hand_Start + 6, // index proximal phalange bone
            Hand_Index2 = Hand_Start + 7, // index intermediate phalange bone
            Hand_Index3 = Hand_Start + 8, // index distal phalange bone
            Hand_Middle1 = Hand_Start + 9, // middle proximal phalange bone
            Hand_Middle2 = Hand_Start + 10, // middle intermediate phalange bone
            Hand_Middle3 = Hand_Start + 11, // middle distal phalange bone
            Hand_Ring1 = Hand_Start + 12, // ring proximal phalange bone
            Hand_Ring2 = Hand_Start + 13, // ring intermediate phalange bone
            Hand_Ring3 = Hand_Start + 14, // ring distal phalange bone
            Hand_Pinky0 = Hand_Start + 15, // pinky metacarpal bone
            Hand_Pinky1 = Hand_Start + 16, // pinky proximal phalange bone
            Hand_Pinky2 = Hand_Start + 17, // pinky intermediate phalange bone
            Hand_Pinky3 = Hand_Start + 18, // pinky distal phalange bone
            Hand_MaxSkinnable = Hand_Start + 19,
            // Bone tips are position only. They are not used for skinning but are useful for hit-testing.
            // NOTE: Hand_ThumbTip == Hand_MaxSkinnable since the extended tips need to be contiguous
            Hand_ThumbTip = Hand_Start + Hand_MaxSkinnable + 0, // tip of the thumb
            Hand_IndexTip = Hand_Start + Hand_MaxSkinnable + 1, // tip of the index finger
            Hand_MiddleTip = Hand_Start + Hand_MaxSkinnable + 2, // tip of the middle finger
            Hand_RingTip = Hand_Start + Hand_MaxSkinnable + 3, // tip of the ring finger
            Hand_PinkyTip = Hand_Start + Hand_MaxSkinnable + 4, // tip of the pinky
            Hand_End = Hand_Start + Hand_MaxSkinnable + 5,

            // add new bones here

            Max = Hand_End + 0,
        }

        private readonly static BoneId[] thumbBones = {
            BoneId.Hand_Thumb0, // metacarpal
            BoneId.Hand_Thumb1, // proximal,
            BoneId.Hand_Thumb2, // intermediate
            BoneId.Hand_Thumb3, // distal
            BoneId.Hand_ThumbTip, // tip
            };

        private readonly static BoneId[] indexBones = {
            BoneId.Invalid, // metacarpal
            BoneId.Hand_Index1, // proximal,
            BoneId.Hand_Index2, // intermediate
            BoneId.Hand_Index3, // distal
            BoneId.Hand_IndexTip, // tip
            };

        private readonly static BoneId[] middleBones = {
            BoneId.Invalid, // metacarpal
            BoneId.Hand_Middle1, // proximal,
            BoneId.Hand_Middle2, // intermediate
            BoneId.Hand_Middle3, // distal
            BoneId.Hand_MiddleTip, // tip
            };

        private readonly static BoneId[] ringBones = {
            BoneId.Invalid, // metacarpal
            BoneId.Hand_Ring1, // proximal,
            BoneId.Hand_Ring2, // intermediate
            BoneId.Hand_Ring3, // distal
            BoneId.Hand_RingTip, // tip
            };

        private readonly static BoneId[] littleBones = {
            BoneId.Invalid, // metacarpal
            BoneId.Hand_Pinky1, // proximal,
            BoneId.Hand_Pinky2, // intermediate
            BoneId.Hand_Pinky3, // distal
            BoneId.Hand_PinkyTip, // tip
            };

        public static BoneId GetBoneId(Finger finger, FingerBone fingerBone) {
            switch (finger) {
                case Finger.Thumb:
                    return thumbBones[(int)fingerBone];
                case Finger.Index:
                    return indexBones[(int)fingerBone];
                case Finger.Middle:
                    return middleBones[(int)fingerBone];
                case Finger.Ring:
                    return ringBones[(int)fingerBone];
                case Finger.Little:
                    return littleBones[(int)fingerBone];
                default:
                    return BoneId.Invalid;
            }
        }

        public enum HandFinger {
            Thumb = 0,
            Index = 1,
            Middle = 2,
            Ring = 3,
            Pinky = 4,
            Max = 5,
        }

        //[Flags]
        public enum HandFingerPinch {
            Thumb = (1 << HandFinger.Thumb),
            Index = (1 << HandFinger.Index),
            Middle = (1 << HandFinger.Middle),
            Ring = (1 << HandFinger.Ring),
            Pinky = (1 << HandFinger.Pinky),
        }

        public static UnityEngine.Transform GetHandBoneTarget(HandTarget handTarget, BoneId boneId) {
            switch (boneId) {
                case BoneId.Hand_WristRoot:
                    return handTarget.hand.target.transform;
                case BoneId.Hand_Thumb0:
                    return handTarget.fingers.thumb.proximal.target.transform;
                case BoneId.Hand_Thumb2:
                    return handTarget.fingers.thumb.intermediate.target.transform;
                case BoneId.Hand_Thumb3:
                    return handTarget.fingers.thumb.distal.target.transform;
                case BoneId.Hand_Index1:
                    return handTarget.fingers.index.proximal.target.transform;
                case BoneId.Hand_Index2:
                    return handTarget.fingers.index.intermediate.target.transform;
                case BoneId.Hand_Index3:
                    return handTarget.fingers.index.distal.target.transform;
                case BoneId.Hand_Middle1:
                    return handTarget.fingers.middle.proximal.target.transform;
                case BoneId.Hand_Middle2:
                    return handTarget.fingers.middle.intermediate.target.transform;
                case BoneId.Hand_Middle3:
                    return handTarget.fingers.middle.distal.target.transform;
                case BoneId.Hand_Ring1:
                    return handTarget.fingers.ring.proximal.target.transform;
                case BoneId.Hand_Ring2:
                    return handTarget.fingers.ring.intermediate.target.transform;
                case BoneId.Hand_Ring3:
                    return handTarget.fingers.ring.distal.target.transform;
                case BoneId.Hand_Pinky1:
                    return handTarget.fingers.little.proximal.target.transform;
                case BoneId.Hand_Pinky2:
                    return handTarget.fingers.little.intermediate.target.transform;
                case BoneId.Hand_Pinky3:
                    return handTarget.fingers.little.distal.target.transform;
            }
            return null;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Posef {
            public Quatf Orientation;
            public Vector3f Position;
            public static readonly Posef identity = new Posef { Orientation = Quatf.identity, Position = Vector3f.zero };
            public override string ToString() {
                return string.Format("Position ({0}), Orientation({1})", Position, Orientation);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BoneCapsule {
            public short BoneIndex;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public Vector3f[] Points;
            public float Radius;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Bone {
            public BoneId Id;
            public short ParentBoneIndex;
            public Posef Pose;
        }

        public enum SkeletonConstants {
            MaxBones = BoneId.Max,
            MaxBoneCapsules = 19,
        }

        public enum SkeletonType {
            None = -1,
            HandLeft = 0,
            HandRight = 1,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Skeleton {
            public SkeletonType Type;
            public uint NumBones;
            public uint NumBoneCapsules;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)SkeletonConstants.MaxBones)]
            public Bone[] Bones;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)SkeletonConstants.MaxBoneCapsules)]
            public BoneCapsule[] BoneCapsules;
        }

        public class SkeletonBone {
            public OculusDevice.BoneId Id { get; private set; }
            public short ParentBoneIndex { get; private set; }
            public UnityEngine.Transform Transform { get; private set; }

            public SkeletonBone(OculusDevice.BoneId id, short parentBoneIndex, UnityEngine.Transform trans) {
                Id = id;
                ParentBoneIndex = parentBoneIndex;
                Transform = trans;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HandState {
            public HandStatus Status;
            public Posef RootPose;
            public Quatf[] BoneRotations;
            public HandFingerPinch Pinches;
            public float[] PinchStrength;
            public Posef PointerPose;
            public float HandScale;
            public TrackingConfidence HandConfidence;
            public TrackingConfidence[] FingerConfidences;
            public double RequestedTimeStamp;
            public double SampleTimeStamp;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HandStateInternal {
            public HandStatus Status;
            public Posef RootPose;
            public Quatf BoneRotations_0;
            public Quatf BoneRotations_1;
            public Quatf BoneRotations_2;
            public Quatf BoneRotations_3;
            public Quatf BoneRotations_4;
            public Quatf BoneRotations_5;
            public Quatf BoneRotations_6;
            public Quatf BoneRotations_7;
            public Quatf BoneRotations_8;
            public Quatf BoneRotations_9;
            public Quatf BoneRotations_10;
            public Quatf BoneRotations_11;
            public Quatf BoneRotations_12;
            public Quatf BoneRotations_13;
            public Quatf BoneRotations_14;
            public Quatf BoneRotations_15;
            public Quatf BoneRotations_16;
            public Quatf BoneRotations_17;
            public Quatf BoneRotations_18;
            public Quatf BoneRotations_19;
            public Quatf BoneRotations_20;
            public Quatf BoneRotations_21;
            public Quatf BoneRotations_22;
            public Quatf BoneRotations_23;
            public HandFingerPinch Pinches;
            public float PinchStrength_0;
            public float PinchStrength_1;
            public float PinchStrength_2;
            public float PinchStrength_3;
            public float PinchStrength_4;
            public Posef PointerPose;
            public float HandScale;
            public TrackingConfidence HandConfidence;
            public TrackingConfidence FingerConfidences_0;
            public TrackingConfidence FingerConfidences_1;
            public TrackingConfidence FingerConfidences_2;
            public TrackingConfidence FingerConfidences_3;
            public TrackingConfidence FingerConfidences_4;
            public double RequestedTimeStamp;
            public double SampleTimeStamp;
        }

        #endregion

        #region Native Interface

        private const string pluginName = "OVRPlugin";

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_GetVersion")]
        private static extern System.IntPtr _ovrp_GetVersion();
        public static string ovrp_GetVersion() { return Marshal.PtrToStringAnsi(_ovrp_GetVersion()); }

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_GetUserPresent();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_SetTrackingPositionEnabled(Bool value);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_GetNodeOrientationTracked(int nodeId);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_GetNodePositionTracked(int nodeId);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern PoseStatef ovrp_GetNodePoseState(Step stepId, int nodeId);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern ControllerState2 ovrp_GetControllerState2(uint controllerMask);

        public enum Result {
            /// Success
            Success = 0,

            /// Failure
            Failure = -1000,
            Failure_InvalidParameter = -1001,
            Failure_NotInitialized = -1002,
            Failure_InvalidOperation = -1003,
            Failure_Unsupported = -1004,
            Failure_NotYetImplemented = -1005,
            Failure_OperationFailed = -1006,
            Failure_InsufficientSize = -1007,
        }

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetControllerState4(uint controllerMask, ref ControllerState4 controllerState);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_SetControllerVibration(uint controllerMask, float frequency, float amplitude);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_GetNodePresent(int nodeId);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern float ovrp_GetUserEyeHeight();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_GetAppShouldRecenter();

        public enum RecenterFlags {
            Default = 0,
            Controllers = 0x40000000,
            IgnoreAll = unchecked((int)0x80000000),
            Count,
        }

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_RecenterTrackingOrigin(uint flags);

        private static class OVRP_1_44_0 {
            public static readonly System.Version version = new System.Version(1, 44, 0);

            //[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            //public static extern Result ovrp_GetHandTrackingEnabled(ref Bool handTrackingEnabled);

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result ovrp_GetHandState(Step stepId, Hand hand, out HandStateInternal handState);

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result ovrp_GetSkeleton(SkeletonType skeletonType, out Skeleton skeleton);

            //[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            //public static extern Result ovrp_GetMesh(MeshType meshType, out Mesh mesh);

            //[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            //public static extern Result ovrp_OverrideExternalCameraFov(int cameraId, Bool useOverriddenFov, ref Fovf fov);

            //[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            //public static extern Result ovrp_GetUseOverriddenExternalCameraFov(int cameraId, out Bool useOverriddenFov);

            //[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            //public static extern Result ovrp_OverrideExternalCameraStaticPose(int cameraId, Bool useOverriddenPose, ref Posef pose);

            //[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            //public static extern Result ovrp_GetUseOverriddenExternalCameraStaticPose(int cameraId, out Bool useOverriddenStaticPose);

            //[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            //public static extern Result ovrp_ResetDefaultExternalCamera();

            //[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            //public static extern Result ovrp_SetDefaultExternalCamera(string cameraName, ref CameraIntrinsics cameraIntrinsics, ref CameraExtrinsics cameraExtrinsics);

        }

        #endregion
    }
}