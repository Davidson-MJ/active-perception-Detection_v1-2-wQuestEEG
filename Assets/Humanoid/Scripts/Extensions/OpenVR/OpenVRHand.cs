#if hOPENVR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
using UnityEngine;
using Passer.Tracking;

namespace Passer.Humanoid {
    using Tracking;

    [System.Serializable]
    public class OpenVRHand : ArmController {
        public override string name {
            get { return "OpenVR Controller"; }
        }

        protected OpenVRHumanoidTracker openVRTracker;
        public OpenVRController openVRController;

        public override Tracker.Status status {
            get {
                if (openVRController == null)
                    return Tracker.Status.Unavailable;
                return openVRController.status;
            }
            set { openVRController.status = value; }
        }

        public OpenVRController.ControllerType controllerType;
        public bool useSkeletalInput = true;

        #region Manage

        public override void CheckSensor(HandTarget _handTarget) {
            base.CheckSensor(_handTarget);
            if (handTarget == null)
                return;

            if (tracker == null)
                tracker = handTarget.humanoid.openVR;

            CheckController();
            CheckSkeleton();
        }

        protected void CheckController() {
#if !UNITY_2020_1_OR_NEWER
            if (enabled && tracker != null && tracker.enabled) {
                if (openVRController == null) {
                    Vector3 position = handTarget.transform.TransformPoint(handTarget.isLeft ? -0.14F : 0.14F, -0.04F, 0.08F);
                    Quaternion localRotation = handTarget.isLeft ? Quaternion.Euler(0, -30, -90) : Quaternion.Euler(0, 30, 90);
                    Quaternion rotation = handTarget.transform.rotation * localRotation;
                    openVRController = handTarget.humanoid.openVR.tracker.GetController(position, rotation, handTarget.isLeft);
                }
                if (openVRController != null)
                    sensorTransform = openVRController.transform;

                if (!Application.isPlaying)
                    SetSensor2Target();

            }
            else {
#if UNITY_EDITOR
                if (!Application.isPlaying) {
                    if (openVRController != null)
                        Object.DestroyImmediate(openVRController.gameObject, true);
                }
#endif
                openVRController = null;
                sensorTransform = null;
            }
#endif
        }

        protected void CheckSkeleton() {
            if (enabled && tracker != null && tracker.enabled) {
#if hVIVEHAND
                if (handTarget.humanoid.openVR.handTracking) {
                    CheckHandTrackingSkeleton();
                    return;
                }
                else if (useSkeletalInput)
#endif
                {
                    CheckOpenVRSkeleton();
                    return;
                }
            }
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroySkeleton();
#endif
        }

        private void CheckOpenVRSkeleton() {
#if !UNITY_2020_1_OR_NEWER
#if UNITY_EDITOR && hVIVEHAND
            if (!Application.isPlaying) {
                if (handSkeleton != null && handSkeleton.GetType() == typeof(ViveHandSkeleton)) {
                    // Delete Vive Hand Tracking skeleton component
                    DestroySkeleton();
                }
            }
#endif
            if (handSkeleton == null) {
                Vector3 position = handTarget.transform.position;
                Quaternion rotation = handTarget.transform.rotation;
                handSkeleton = handTarget.humanoid.openVR.tracker.GetSkeleton(position, rotation, handTarget.isLeft);
            }
#endif
        }

#if hVIVEHAND
        private void CheckHandTrackingSkeleton() {
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                if (handSkeleton != null && handSkeleton.GetType() == typeof(OpenVRHandSkeleton)) {
                    // Delete OpenVR skeleton component
                    DestroySkeleton();
                }
            }
#endif
            if (handSkeleton == null) {
                Vector3 position = handTarget.transform.position;
                Quaternion rotation = handTarget.transform.rotation;
                handSkeleton = Passer.Tracking.OpenVR.GetHandTrackingSkeleton(handTarget.humanoid.openVR.tracker, position, rotation, handTarget.isLeft);

                //OpenVR openVR = handTarget.humanoid.openVR.tracker as OpenVR;
                //if (openVR != null)
                //    handSkeleton = openVR.tracker.GetHandTrackingSkeleton(position, rotation, handTarget.isLeft);
            }
#if UNITY_2020_1_OR_NEWER
            UnityXR unityXR = handTarget.humanoid.unity;
            if (unityXR != null)
                ((ViveHandSkeleton)handSkeleton).hmd = unityXR.hmd;
#else
            Transform hmdTransform = handTarget.humanoid.headTarget.openVR.sensorTransform;
            if (hmdTransform != null)
                ((ViveHandSkeleton)handSkeleton).hmd = hmdTransform.GetComponent<OpenVRHmd>();
#endif
        }
#endif

        private void DestroySkeleton() {
            if (handSkeleton != null)
                Object.DestroyImmediate(handSkeleton.gameObject, true);
            handSkeleton = null;
        }

        #endregion

        #region Start

        public override void Init(HandTarget handTarget) {
            base.Init(handTarget);
            openVRTracker = handTarget.humanoid.openVR;
            tracker = openVRTracker;
        }

        public override void Start(HumanoidControl _humanoid, Transform targetTransform) {
            base.Start(_humanoid, targetTransform);
            openVRTracker = handTarget.humanoid.openVR;
            tracker = openVRTracker;

            if (tracker == null || !tracker.enabled || !enabled)
                return;

            SetSensor2Target();
            CheckSensorTransform();
            ShowSensor(handTarget.humanoid.showRealObjects && target.showRealObjects);
            if (handSkeleton != null)
                handSkeleton.show = handTarget.humanoid.showSkeletons && target.showRealObjects;

            if (sensorTransform != null) {
                openVRController = sensorTransform.GetComponent<OpenVRController>();
                if (openVRController != null)
                    openVRController.StartComponent(tracker.trackerTransform);
            }
#if hVIVEHAND
            if (handTarget.humanoid.openVR.handTracking)
                CheckHandTrackingSkeleton();
#endif
        }

        #region Controller

        public virtual void FindSensor() {
            OpenVRController[] controllers = tracker.trackerTransform.GetComponentsInChildren<OpenVRController>();
            foreach (OpenVRController controller in controllers) {
                if (controller.isLeft == handTarget.isLeft) {
                    sensorTransform = controller.transform;
                }
            }
        }

        protected override void CreateSensorTransform() {
            //string prefabLeftName = "Vive Controller";
            //string prefabRightName = "Vive Controller";
            //switch (controllerType) {
            //    case OpenVRController.ControllerType.ValveIndex:
            //        break;
            //    case OpenVRController.ControllerType.MixedReality:
            //        prefabLeftName = "Windows MR Controller Left";
            //        prefabRightName = "Windows MR Controller Right";
            //        break;
            //    case OpenVRController.ControllerType.OculusTouch:
            //        prefabLeftName = "Left Touch Controller";
            //        prefabRightName = "Right Touch Controller";
            //        break;
            //    case OpenVRController.ControllerType.SteamVRController:
            //    default:
            //        break;
            //}

            //if (handTarget.isLeft)
            //    CreateSensorTransform(prefabLeftName, new Vector3(-0.14F, -0.04F, 0.08F), Quaternion.Euler(0, -30, -90));
            //else
            //    CreateSensorTransform(prefabRightName, new Vector3(0.14F, -0.04F, 0.08F), Quaternion.Euler(0, 30, 90));

            //openVRController = sensorTransform.GetComponent<OpenVRController>();
            //if (openVRController == null)
            //    openVRController = sensorTransform.gameObject.AddComponent<OpenVRController>();
            //openVRController.isLeft = handTarget.isLeft;
        }

        #endregion

        #region Skeleton

        protected override HandSkeleton FindHandSkeleton(bool isLeft) {
            HandSkeleton[] handSkeletons = tracker.trackerTransform.GetComponentsInChildren<HandSkeleton>();
            foreach (HandSkeleton handSkeleton in handSkeletons) {
                if (handSkeleton.isLeft == isLeft)
                    return handSkeleton;
            }
            return null;
        }

        protected override HandSkeleton CreateHandSkeleton(bool isLeft, bool showRealObjects) {
            GameObject skeletonObj = new GameObject(isLeft ? "Left Hand Skeleton" : "Right Hand Skeleton");
            skeletonObj.transform.parent = tracker.trackerTransform;
            skeletonObj.transform.localPosition = Vector3.zero;
            skeletonObj.transform.localRotation = Quaternion.identity;

            HandSkeleton handSkeleton = skeletonObj.AddComponent<HandSkeleton>();
            handSkeleton.isLeft = isLeft;
            handSkeleton.show = showRealObjects;
            return handSkeleton;
        }

        #endregion

        #endregion

        #region Update

        public override void Update() {
            if (tracker == null || !tracker.enabled || !enabled)
                return;

#if hVIVEHAND
            if (openVRTracker.handTracking) {
                if (handSkeleton == null)
                    handSkeleton = ViveHandSkeleton.Get(handTarget.humanoid.openVR.tracker.transform, handTarget.isLeft, handTarget.showRealObjects);
                useSkeletalInput = true;
            }
#endif
            if (useSkeletalInput) {
                if (handSkeleton == null)
                    handSkeleton = OpenVRHandSkeleton.Get(handTarget.humanoid.openVR.tracker.transform, handTarget.isLeft, handTarget.showRealObjects);

                if (handSkeleton != null && handSkeleton.status == Tracker.Status.Tracking) {
                    UpdateHandFromSkeleton(handSkeleton);
                    handTarget.hand.target.confidence.position = 0.9F;
                    handTarget.hand.target.confidence.rotation = 0.9F;
                    //openVRController.show = false;
                }
                else {
                    handTarget.hand.target.confidence.position = 0.0F;
                    handTarget.hand.target.confidence.rotation = 0.0F;
                }
                if (openVRController != null)
                    UpdateInput();
                return;
            }

            if (openVRController == null) {
                UpdateTarget(handTarget.hand.target, sensorTransform);
                return;
            }

            if (openVRController.isLeft != handTarget.isLeft) {
                // Reassign controller when the left and right have swapped
                openVRController.trackerId = -1;
            }

            openVRController.UpdateComponent();
            if (openVRController.status != Tracker.Status.Tracking)
                return;
            //#endif
            UpdateTarget(handTarget.hand.target, openVRController);
            UpdateInput();
            UpdateHand();
        }

        #region Controller

        protected void UpdateInput() {
            if (handTarget.isLeft)
                SetControllerInput(controllerInput.left);
            else
                SetControllerInput(controllerInput.right);
        }

        protected void SetControllerInput(ControllerSide controllerSide) {
            controllerSide.stickHorizontal += openVRController.joystick.x;
            controllerSide.stickVertical += openVRController.joystick.y;
            controllerSide.stickButton |= (openVRController.joystick.z > 0.5F);
            controllerSide.stickTouch |= (openVRController.joystick.z > -0.5F);

            controllerSide.touchpadHorizontal += openVRController.touchpad.x;
            controllerSide.touchpadVertical += openVRController.touchpad.y;
            controllerSide.touchpadPress |= (openVRController.touchpad.z > 0.5F);
            controllerSide.touchpadTouch |= (openVRController.touchpad.z > -0.5F);

            controllerSide.buttons[0] |= openVRController.aButton > 0.5F;
            controllerSide.buttons[1] |= openVRController.bButton > 0.5F;

            controllerSide.trigger1 += openVRController.trigger;
            controllerSide.trigger2 += openVRController.grip;

            controllerSide.option |= openVRController.aButton > 0.5F;
        }

        protected void UpdateHand() {
            for (int i = 0; i < (int)Finger.Count; i++)
                UpdateFinger(handTarget.fingers.allFingers[i], i);

            handTarget.fingers.DetermineFingerCurl();
        }

        private void UpdateFinger(FingersTarget.TargetedFinger finger, int fingerIx) {
            finger.proximal.target.transform.localRotation = GetFingerRotation(openVRController, fingerIx, 0);
            finger.intermediate.target.transform.localRotation = GetFingerRotation(openVRController, fingerIx, 1);
            finger.distal.target.transform.localRotation = GetFingerRotation(openVRController, fingerIx, 2);
        }

        protected Quaternion GetFingerRotation(OpenVRController openVRController, int fingerIx, int boneIx) {
            int ix = fingerIx * 5 + boneIx + 2;
            Passer.VRBoneTransform_t boneTransform = openVRController.tempBoneTransforms[ix];

            Quaternion q = new Quaternion(
                boneTransform.orientation.x,
                boneTransform.orientation.y,
                boneTransform.orientation.z,
                boneTransform.orientation.w
                );
            if (!handTarget.isLeft)
                q = new Quaternion(q.x, -q.y, -q.z, q.w);
            if (fingerIx == 0) {
                if (boneIx == 0) {
                    q = Rotations.Rotate(q, Quaternion.Euler(90, 0, 0));
                    if (handTarget.isLeft)
                        q = Quaternion.Euler(0, -180, -90) * q;
                    else
                        q = Quaternion.Euler(180, -180, -90) * q;
                }
                else
                    q = Rotations.Rotate(q, Quaternion.Euler(90, 0, 0));
            }
            return q;
        }

        //#endregion

        //length is how long the vibration should go for in seconds
        //strength is vibration strength from 0-1
        public override void Vibrate(float length, float strength) {
            if (tracker == null || !tracker.enabled || !enabled)
                return;

            for (float i = 0; i < length; i += Time.deltaTime)
                //openVRController.Vibrate(length, strength);
                openVRController.Vibrate(1, 1); // strength);
        }

        #endregion

        #region Skeleton

        protected void UpdateHandFromSkeleton(HandSkeleton handSkeleton) {
            Transform wristBone = handSkeleton.GetWristBone();
            handTarget.hand.target.transform.position = wristBone.transform.position;
            if (handTarget.isLeft)
                handTarget.hand.target.transform.rotation = wristBone.transform.rotation * Quaternion.Euler(-90, 0, 90);
            else
                handTarget.hand.target.transform.rotation = wristBone.transform.rotation * Quaternion.Euler(-90, 0, -90);

            UpdateThumbFromSkeleton();
            UpdateIndexFingerFromSkeleton();
            UpdateMiddleFingerFromSkeleton();
            UpdateRingFingerFromSkeleton();
            UpdateLittleFingerFromSkeleton();
        }

        protected override void UpdateFingerBoneFromSkeleton(Transform targetTransform, Finger finger, FingerBone fingerBone) {
            if (handSkeleton == null)
                return;

            Transform thisBoneTransform = handSkeleton.GetBone(finger, fingerBone);
            Transform nextBoneTransform = handSkeleton.GetBone(finger, fingerBone + 1);
            if (thisBoneTransform == null || nextBoneTransform == null)
                return;

            Vector3 direction = nextBoneTransform.position - thisBoneTransform.position;
            if (handTarget.isLeft)
                targetTransform.rotation = Quaternion.LookRotation(direction, handTarget.hand.target.transform.forward) * Quaternion.Euler(-90, 0, 90);
            else
                targetTransform.rotation = Quaternion.LookRotation(direction, handTarget.hand.target.transform.forward) * Quaternion.Euler(-90, 0, -90);
        }

        #endregion

        #endregion
    }
}

#endif