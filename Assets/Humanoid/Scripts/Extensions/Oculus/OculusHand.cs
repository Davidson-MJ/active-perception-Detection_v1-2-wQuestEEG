using System.Collections.Generic;
#if hOCULUS && (UNITY_STANDALONE_WIN || UNITY_ANDROID)
using UnityEngine;
using Passer.Tracking;

namespace Passer.Humanoid {
    using Tracking;

    [System.Serializable]
    public class OculusHand : ArmController {
        public override string name {
#if pUNITYXR
            get { return "Unity XR"; }
#else
            get { return "Oculus"; }
#endif
        }

        protected OculusHumanoidTracker oculusTracker;
#if pUNITYXR
        public UnityXRController unityXRController;
#else
        protected OculusController oculusController;
#endif
#if hOCHAND
        //public OculusHandSkeleton handSkeleton;
#endif

        public override Tracker.Status status {
            get {
#if hOCHAND && UNITY_ANDROID
                if (oculusTracker.handTracking && handSkeleton != null)
                    return handSkeleton.status;
#if pUNITYXR
                else if (!oculusTracker.handTracking && unityXRController != null)
                    return unityXRController.status;
#else
                else if (!oculusTracker.handTracking && oculusController != null)
                    return oculusController.status;
#endif
                else
#endif
#if pUNITYXR
                if (unityXRController == null)
                    return Tracker.Status.Unavailable;
                return unityXRController.status;
            }
#else
                if (oculusController != null)
                    return oculusController.status;
                else
                    return Tracker.Status.Unavailable;
            }
            set { oculusController.status = value; }
#endif
        }

        #region Manage

        public override void CheckSensor(HandTarget handTarget) {
#if pUNITYXR
            if (this.handTarget == null)
                this.target = handTarget;
            if (this.handTarget == null)
                return;

            if (tracker == null)
                tracker = handTarget.humanoid.oculus;

            if (enabled && tracker != null && tracker.enabled) {
                if (unityXRController == null) {
                    Vector3 position = handTarget.transform.TransformPoint(handTarget.isLeft ? -0.1F : 0.1F, -0.05F, 0.04F);
                    Quaternion localRotation = handTarget.isLeft ? Quaternion.Euler(180, 90, 90) : Quaternion.Euler(180, -90, -90);
                    Quaternion rotation = handTarget.transform.rotation * localRotation;
                    //unityXRController = handTarget.humanoid.unity.GetController(handTarget.isLeft, position, rotation);
                    unityXRController = handTarget.humanoid.oculus.GetController(handTarget.isLeft, position, rotation);
                }
                if (unityXRController != null)
                    sensorTransform = unityXRController.transform;

                if (!Application.isPlaying)
                    SetSensor2Target();
            }
            else {
#if UNITY_EDITOR
                if (!Application.isPlaying) {
                    if (unityXRController != null)
                        Object.DestroyImmediate(unityXRController.gameObject, true);
                }
#endif
                unityXRController = null;
                sensorTransform = null;
            }
#endif
        }

        #endregion

        #region Start

        public override void Init(HandTarget handTarget) {
            base.Init(handTarget);
            oculusTracker = handTarget.humanoid.oculus;
            tracker = oculusTracker;
        }

        public override void Start(HumanoidControl _humanoid, Transform targetTransform) {
            base.Start(_humanoid, targetTransform);
            oculusTracker = handTarget.humanoid.oculus;
            tracker = oculusTracker;

            if (tracker == null || !tracker.enabled || !enabled)
                return;

            //#if !pUNITYXR
            SetSensor2Target();
            CheckSensorTransform();
            ShowSensor(handTarget.humanoid.showRealObjects && target.showRealObjects);
            if (handSkeleton != null)
                handSkeleton.show = handTarget.humanoid.showRealObjects && target.showRealObjects;

#if !pUNITYXR
            if (sensorTransform != null) {
                oculusController = sensorTransform.GetComponent<OculusController>();
                if (oculusController != null) {
#if UNITY_ANDROID
                    if (handTarget.humanoid.oculus.androidDeviceType == OculusHumanoidTracker.AndroidDeviceType.GearVR ||
                        handTarget.humanoid.oculus.androidDeviceType == OculusHumanoidTracker.AndroidDeviceType.OculusGo) {

                        oculusController.positionalTracking = false;
                    }
                    else
#endif
                    oculusController.positionalTracking = true;
                    oculusController.StartComponent(tracker.trackerTransform);
                }
            }
#endif
        }

        #region Controller

        protected override void CreateSensorTransform() {
#if !pUNITYXR
            if (handTarget.isLeft) {
                string controllerName = "Left Touch Controller";
#if UNITY_ANDROID
                switch (handTarget.humanoid.oculus.androidDeviceType) {
                    case OculusHumanoidTracker.AndroidDeviceType.GearVR:
                        controllerName = "GearVR Controller";
                        break;
                    case OculusHumanoidTracker.AndroidDeviceType.OculusGo:
                        controllerName = "OculusGo Controller";
                        break;
                    case OculusHumanoidTracker.AndroidDeviceType.OculusQuest:
                        controllerName = "Left Quest Controller";
                        break;
                    default:
                        break;
                }
#endif
                CreateSensorTransform(controllerName, new Vector3(-0.1F, -0.05F, 0.04F), Quaternion.Euler(180, 90, 90));
            }
            else {
                string controllerName = "Right Touch Controller";
#if UNITY_ANDROID
                switch (handTarget.humanoid.oculus.androidDeviceType) {
                    case OculusHumanoidTracker.AndroidDeviceType.GearVR:
                        controllerName = "GearVR Controller";
                        break;
                    case OculusHumanoidTracker.AndroidDeviceType.OculusGo:
                        controllerName = "OculusGo Controller";
                        break;
                    case OculusHumanoidTracker.AndroidDeviceType.OculusQuest:
                        controllerName = "Right Quest Controller";
                        break;
                    default:
                        break;
                }
#endif
                CreateSensorTransform(controllerName, new Vector3(0.1F, -0.05F, 0.04F), Quaternion.Euler(180, -90, -90));
            }

            OculusController oculusController = sensorTransform.GetComponent<OculusController>();
            if (oculusController == null)
                oculusController = sensorTransform.gameObject.AddComponent<OculusController>();
            oculusController.isLeft = handTarget.isLeft;
#endif
        }

#if pUNITYXR

        protected readonly Vector3 defaultLeftPosition = new Vector3(-0.1F, -0.05F, 0.04F);
        protected readonly Quaternion defaultLeftRotation = Quaternion.Euler(180, 90, 90);
        protected readonly Vector3 defaultRightPosition = new Vector3(0.1F, -0.05F, 0.04F);
        protected readonly Quaternion defaultRightRotation = Quaternion.Euler(180, -90, -90);
        //public virtual void CheckController() {
        //    Vector3 position = handTarget.transform.TransformPoint(handTarget.isLeft ? defaultLeftPosition : defaultRightPosition);
        //    Quaternion rotation = handTarget.transform.rotation * (handTarget.isLeft ? defaultLeftRotation : defaultRightRotation);
        //    unityXRController = UnityXRController.Get(handTarget.humanoid.unity, handTarget.isLeft, position, rotation);
        //    if (unityXRController != null)
        //        sensorTransform = unityXRController.transform;
        //}
#endif

        #endregion

        #region Skeleton

#if hOCHAND
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

            OculusHandSkeleton handSkeleton = skeletonObj.AddComponent<OculusHandSkeleton>();
            handSkeleton.isLeft = isLeft;
            handSkeleton.show = handTarget.humanoid.showRealObjects && showRealObjects;
            return handSkeleton;
        }
#endif

        #endregion

        #endregion

        #region Update

        public override void Update() {
            if (tracker == null || !tracker.enabled || !enabled)
                return;

#if hOCHAND && UNITY_ANDROID
            if (oculusTracker.handTracking) {
                if (handSkeleton == null)
                    handSkeleton = CreateHandSkeleton(handTarget.isLeft, handTarget.showRealObjects);

                if (handSkeleton != null && handSkeleton.status == Tracker.Status.Tracking) {
                    UpdateHandFromSkeleton();
                    handTarget.hand.target.confidence.position = 0.9F;
                    handTarget.hand.target.confidence.rotation = 0.9F;

#if pUNITYXR
                    //unityXRController.show = false;
#else
                    oculusController.show = false;
#endif
                    return;
                }
                else {
#if pUNITYXR
                    handTarget.hand.target.confidence.position = 0.0F;
                    handTarget.hand.target.confidence.rotation = 0.0F;
#else
                    oculusController.UpdateComponent();
                    if (oculusController.status != Tracker.Status.Tracking)
                        return;

                    UpdateTarget(handTarget.hand.target, oculusController);

                    UpdateInput();
#endif
                    return;
                }
            }
#endif

#if pUNITYXR
            if (unityXRController == null || unityXRController.status != Tracker.Status.Tracking)
                return;

            UpdateTarget(handTarget.hand.target, unityXRController);
#else
            if (oculusController == null) {
                UpdateTarget(handTarget.hand.target, sensorTransform);
                return;
            }

            oculusController.UpdateComponent();
            if (oculusController.status != Tracker.Status.Tracking)
                return;

            UpdateTarget(handTarget.hand.target, oculusController);
#endif

#if UNITY_ANDROID
            if (handTarget.humanoid.oculus.androidDeviceType == OculusHumanoidTracker.AndroidDeviceType.GearVR ||
                handTarget.humanoid.oculus.androidDeviceType == OculusHumanoidTracker.AndroidDeviceType.OculusGo) {

                CalculateHandPosition();
            }
#endif

            UpdateInput();
        }

        #region Controller

        // arm model for 3DOF tracking: position is calculated from rotation
        protected void CalculateHandPosition() {
            Quaternion hipsYRotation = Quaternion.AngleAxis(handTarget.humanoid.hipsTarget.transform.eulerAngles.y, handTarget.humanoid.up);

            Vector3 pivotPoint = handTarget.humanoid.hipsTarget.transform.position + hipsYRotation * (handTarget.isLeft ? new Vector3(-0.25F, 0.15F, -0.05F) : new Vector3(0.25F, 0.15F, -0.05F));
            Quaternion forearmRotation = handTarget.hand.target.transform.rotation * (handTarget.isLeft ? Quaternion.Euler(0, -90, 0) : Quaternion.Euler(0, 90, 0));

            Vector3 localForearmDirection = handTarget.humanoid.hipsTarget.transform.InverseTransformDirection(forearmRotation * Vector3.forward);

            if (localForearmDirection.x < 0 || localForearmDirection.y > 0) {
                pivotPoint += hipsYRotation * Vector3.forward * Mathf.Lerp(0, 0.15F, -localForearmDirection.x * 3 + localForearmDirection.y);
            }
            if (localForearmDirection.y > 0) {
                pivotPoint += hipsYRotation * Vector3.up * Mathf.Lerp(0, 0.2F, localForearmDirection.y);
            }

            if (localForearmDirection.z < 0.2F) {
                localForearmDirection = new Vector3(localForearmDirection.x, localForearmDirection.y, 0.2F);
                forearmRotation = Quaternion.LookRotation(handTarget.humanoid.hipsTarget.transform.TransformDirection(localForearmDirection), forearmRotation * Vector3.up);
            }

            handTarget.hand.target.transform.position = pivotPoint + forearmRotation * Vector3.forward * handTarget.forearm.bone.length;

            sensorTransform.position = handTarget.hand.target.transform.TransformPoint(-sensor2TargetPosition);
        }

        private void UpdateInput() {
            if (controllerInput == null)
                return;

            if (handTarget.isLeft)
                UpdateInputSide(controllerInput.left);
            else
                UpdateInputSide(controllerInput.right);
        }

        private void UpdateInputSide(ControllerSide controllerInputSide) {
#if pUNITYXR
            controllerInputSide.stickHorizontal = unityXRController.primaryAxis.x;
            controllerInputSide.stickVertical = unityXRController.primaryAxis.y;
            controllerInputSide.stickButton |= (unityXRController.primaryAxis.z > 0.5F);
            controllerInputSide.stickTouch |= (unityXRController.primaryAxis.z > -0.5F);

            controllerInputSide.buttons[0] |= (unityXRController.primaryButton > 0.5F);
            controllerInputSide.buttons[1] |= (unityXRController.secondaryButton > 0.5F);

            controllerInputSide.trigger1 = unityXRController.trigger;
            controllerInputSide.trigger2 = unityXRController.grip;
            controllerInputSide.option = unityXRController.menu > 0;
#else
            controllerInputSide.stickHorizontal = oculusController.joystick.x;
            controllerInputSide.stickVertical = oculusController.joystick.y;
            controllerInputSide.stickButton |= (oculusController.joystick.z > 0.5F);
            controllerInputSide.stickTouch |= (oculusController.joystick.z > -0.5F);

            controllerInputSide.buttons[0] |= (oculusController.buttonAX > 0.5F);
            controllerInputSide.buttons[1] |= (oculusController.buttonBY > 0.5F);

            controllerInputSide.trigger1 = oculusController.indexTrigger;
            controllerInputSide.trigger2 = oculusController.handTrigger;
            controllerInputSide.option = oculusController.option > 0;
#endif
        }

        public override void Vibrate(float length, float strength) {
#if pUNITYXR
#else
            if (oculusController != null)
                oculusController.Vibrate(length, strength);
#endif
        }

        #endregion

#if hOCHAND
        #region Skeleton

        protected override void UpdateHandFromSkeleton() {
            Transform wristBone = handSkeleton.GetWristBone();
            handTarget.hand.target.transform.position = wristBone.transform.position;
            if (handTarget.isLeft)
                handTarget.hand.target.transform.rotation = wristBone.transform.rotation * Quaternion.Euler(180, 180, 0);
            else
                handTarget.hand.target.transform.rotation = wristBone.transform.rotation * Quaternion.Euler(0, 180, 0);

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
            if (thisBoneTransform == null) {
                Debug.Log(finger + " " + fingerBone + " " + thisBoneTransform);
                return;
            }
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
#endif
        #endregion
    }

}
#endif