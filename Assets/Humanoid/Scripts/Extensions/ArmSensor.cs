using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Passer.Humanoid {
    using Passer.Tracking;
    using Passer.Humanoid.Tracking;

    public class ArmSensor : HumanoidSensor {
        protected HandTarget handTarget {
            get { return (HandTarget)target; }
        }
        protected new Humanoid.Tracking.ArmSensor sensor;

        public HandSkeleton handSkeleton;

        #region Manage

        protected virtual HandSkeleton FindHandSkeleton(bool isLeft) {
            return null;
        }

        protected virtual HandSkeleton CreateHandSkeleton(bool isLeft, bool showRealObjects) {
            return null;
        }

        public virtual void CheckSensor(HandTarget _handTarget) {
            if (handTarget == null)
                target = _handTarget;
        }

        public virtual void CheckSensor(HandTarget handTarget, HumanoidTracker tracker) {
            if (this.handTarget == null)
                this.target = handTarget;
            if (this.handTarget == null)
                return;

            if (this.tracker == null)
                this.tracker = tracker;

            if (enabled && tracker.enabled) {
                if (handSkeleton == null) {
                    handSkeleton = FindHandSkeleton(handTarget.isLeft);
                    if (handSkeleton == null)
                        handSkeleton = CreateHandSkeleton(handTarget.isLeft, handTarget.showRealObjects);
                }
            }
            else {
#if UNITY_EDITOR
                if (!Application.isPlaying) {
                    if (handSkeleton != null)
                        Object.DestroyImmediate(handSkeleton.gameObject, true);
                }
#endif
                handSkeleton = null;
            }
        }

        #endregion

        #region Start

        public virtual void Init(HandTarget handTarget) {
            target = handTarget;
        }

        public override void Start(HumanoidControl _humanoid, Transform targetTransform) {
            base.Start(_humanoid, targetTransform);
            target = targetTransform.GetComponent<HandTarget>();
        }

#if UNITY_EDITOR
        public void InitController(SerializedProperty sensorProp, HandTarget handTarget) {
            if (sensorProp == null)
                return;

            Init(handTarget);

            SerializedProperty sensorTransformProp = sensorProp.FindPropertyRelative("sensorTransform");
            sensorTransformProp.objectReferenceValue = sensorTransform;

            SerializedProperty targetProp = sensorProp.FindPropertyRelative("target");
            targetProp.objectReferenceValue = target;

            if (tracker == null || !tracker.enabled || !enabled)
                return;

            CheckSensorTransform();
            sensorTransformProp.objectReferenceValue = sensorTransform;

            ShowSensor(handTarget.humanoid.showRealObjects && handTarget.showRealObjects);

            SerializedProperty sensor2TargetPositionProp = sensorProp.FindPropertyRelative("sensor2TargetPosition");
            sensor2TargetPositionProp.vector3Value = sensor2TargetPosition;
            SerializedProperty sensor2TargetRotationProp = sensorProp.FindPropertyRelative("sensor2TargetRotation");
            sensor2TargetRotationProp.quaternionValue = sensor2TargetRotation;
        }

        public void RemoveController(SerializedProperty sensorProp) {
            if (sensorProp == null)
                return;

            SerializedProperty sensorTransformProp = sensorProp.FindPropertyRelative("sensorTransform");
            sensorTransformProp.objectReferenceValue = null;
        }
#endif

        public void CheckSensorTransform(Transform targetTransform, bool isLeft) {
            if (enabled && sensorTransform == null)
                CreateSensorTransform(targetTransform, isLeft);
            else if (!enabled && sensorTransform != null)
                RemoveSensorTransform();
        }

        public virtual void CreateSensorTransform(Transform targetTransform, bool isLeft) { }

        protected virtual void CreateSensorTransform(string resourceName, Vector3 sensor2TargetPosition, Quaternion sensor2TargetRotation) {
            CreateSensorTransform(handTarget.hand.target.transform, resourceName, sensor2TargetPosition, sensor2TargetRotation);
        }

        public override void SetSensor2Target() {
            if (sensorTransform == null || target == null)
                return;

            sensor2TargetRotation = Quaternion.Inverse(sensorTransform.rotation) * target.transform.rotation;
            sensor2TargetPosition = -target.transform.InverseTransformPoint(sensorTransform.position);
        }

        public virtual void SetSensor2Target(Vector3 targetPosition, Quaternion targetRotation) {
            if (sensorTransform == null)
                return;

            sensor2TargetRotation = Quaternion.Inverse(sensorTransform.rotation) * targetRotation;
            //sensor2TargetPosition = -targetTransform.InverseTransformPoint(sensorTransform.position);

            var worldToLocalMatrix = Matrix4x4.TRS(targetPosition, targetRotation, Vector3.one).inverse;
            sensor2TargetPosition = -worldToLocalMatrix.MultiplyPoint3x4(sensorTransform.position);
        }

        #endregion

        #region Update
        protected void UpdateArm(Humanoid.Tracking.ArmSensor armSensor) {
            float armConfidence = ArmConfidence(armSensor);
            if (handTarget.hand.target.confidence.position > armConfidence)
                UpdateArmIK(armSensor);
            else
                UpdateArmDirect(armSensor);
        }

        private void UpdateArmDirect(Humanoid.Tracking.ArmSensor armSensor) {
            UpdateShoulder(armSensor);
            UpdateUpperArm(armSensor);
            UpdateForearm(armSensor);
            UpdateHand(armSensor);
        }

        private void UpdateArmIK(Humanoid.Tracking.ArmSensor armSensor) {
            Vector3 handTargetPosition = handTarget.hand.target.transform.position;
            Quaternion handTargetRotation = handTarget.hand.target.transform.rotation;

            Vector3 forearmUpAxis = HumanoidTarget.ToQuaternion(armSensor.upperArm.rotation) * Vector3.up;
            if (handTarget.upperArm.target.confidence.rotation < 0.9F) {
                handTarget.upperArm.target.transform.rotation = ArmMovements.UpperArmRotationIK(handTarget.upperArm.target.transform.position, handTargetPosition, forearmUpAxis, handTarget.upperArm.target.length, handTarget.forearm.target.length, handTarget.isLeft);
                handTarget.upperArm.target.confidence = armSensor.upperArm.confidence;
            }

            if (handTarget.forearm.target.confidence.rotation < 0.9F) {
                handTarget.forearm.target.transform.rotation = ArmMovements.ForearmRotationIK(handTarget.forearm.target.transform.position, handTargetPosition, forearmUpAxis, handTarget.isLeft);
                handTarget.forearm.target.confidence = armSensor.forearm.confidence;
            }

            handTarget.hand.target.transform.rotation = handTargetRotation;
            handTarget.hand.target.confidence.rotation = armSensor.hand.confidence.rotation;
        }

        protected void UpdateShoulder(Humanoid.Tracking.ArmSensor armSensor) {
            if (handTarget.shoulder.target.transform == null)
                return;

            if (armSensor.shoulder.confidence.position > 0)
                handTarget.shoulder.target.transform.position = HumanoidTarget.ToVector3(armSensor.shoulder.position);
            if (armSensor.shoulder.confidence.rotation > 0)
                handTarget.shoulder.target.transform.rotation = HumanoidTarget.ToQuaternion(armSensor.shoulder.rotation);
            handTarget.shoulder.target.confidence = armSensor.upperArm.confidence;
        }

        protected virtual void UpdateUpperArm(Humanoid.Tracking.ArmSensor armSensor) {
            if (handTarget.upperArm.target.transform != null) {
                if (armSensor.upperArm.confidence.position > 0)
                    handTarget.upperArm.target.transform.position = HumanoidTarget.ToVector3(armSensor.upperArm.position);
                else
                    handTarget.upperArm.target.transform.position = handTarget.shoulder.target.transform.position + handTarget.shoulder.target.transform.rotation * handTarget.outward * handTarget.shoulder.bone.length;

                if (armSensor.upperArm.confidence.rotation > 0)
                    handTarget.upperArm.target.transform.rotation = HumanoidTarget.ToQuaternion(armSensor.upperArm.rotation);

                handTarget.upperArm.target.confidence = armSensor.upperArm.confidence;
            }
        }

        protected virtual void UpdateForearm(Humanoid.Tracking.ArmSensor armSensor) {
            if (handTarget.forearm.target.transform != null) {
                if (armSensor.forearm.confidence.position > 0)
                    handTarget.forearm.target.transform.position = HumanoidTarget.ToVector3(armSensor.forearm.position);
                else
                    handTarget.forearm.target.transform.position = handTarget.upperArm.target.transform.position + handTarget.upperArm.target.transform.rotation * handTarget.outward * handTarget.upperArm.bone.length;

                if (armSensor.forearm.confidence.rotation > 0)
                    handTarget.forearm.target.transform.rotation = HumanoidTarget.ToQuaternion(armSensor.forearm.rotation);

                handTarget.forearm.target.confidence = armSensor.forearm.confidence;
            }
        }

        protected virtual void UpdateHand(Humanoid.Tracking.ArmSensor armSensor) {
            if (handTarget.hand.target.transform != null) {
                if (armSensor.hand.confidence.position > 0 && armSensor.hand.confidence.position >= handTarget.hand.target.confidence.position) {
                    handTarget.hand.target.transform.position = HumanoidTarget.ToVector3(armSensor.hand.position);
                    handTarget.hand.target.confidence.position = armSensor.hand.confidence.position;
                }
                else if (handTarget.hand.target.confidence.position == 0) // Hmm. I could insert the arm model here when confidence.rotation > 0.5F for example!
                    handTarget.hand.target.transform.position = handTarget.forearm.target.transform.position + handTarget.forearm.target.transform.rotation * handTarget.outward * handTarget.forearm.bone.length;

                if (armSensor.hand.confidence.rotation > 0 && armSensor.hand.confidence.rotation >= handTarget.hand.target.confidence.rotation) {
                    handTarget.hand.target.transform.rotation = HumanoidTarget.ToQuaternion(armSensor.hand.rotation);
                    handTarget.hand.target.confidence.rotation = armSensor.hand.confidence.rotation;
                }
            }
        }
        protected virtual void UpdateHandTargetTransform(Humanoid.Tracking.ArmSensor armSensor) {
            if (handTarget.hand.target.transform != null) {
                if (armSensor.hand.confidence.rotation > 0 && armSensor.hand.confidence.rotation >= handTarget.hand.target.confidence.rotation) {
                    handTarget.hand.target.transform.rotation = sensorTransform.rotation * sensor2TargetRotation;
                    handTarget.hand.target.confidence.rotation = armSensor.hand.confidence.rotation;
                }
                if (armSensor.hand.confidence.position > 0 && armSensor.hand.confidence.position >= handTarget.hand.target.confidence.position) {
                    handTarget.hand.target.transform.position = sensorTransform.position + handTarget.hand.target.transform.rotation * sensor2TargetPosition;
                    handTarget.hand.target.confidence.position = armSensor.hand.confidence.position;
                }
                else if (handTarget.hand.target.confidence.position == 0) // Hmm. I could insert the arm model here when confidence.rotation > 0.5F for example!
                    handTarget.hand.target.transform.position = handTarget.forearm.target.transform.position + handTarget.forearm.target.transform.rotation * handTarget.outward * handTarget.forearm.bone.length;

            }
        }

        protected virtual void UpdateFingers(Humanoid.Tracking.ArmSensor armSensor) {
            for (int i = 0; i < (int)Humanoid.Tracking.Finger.Count; i++) {
                UpdateFinger(armSensor.fingers[i], i);
            }
        }

        private void UpdateFinger(Humanoid.Tracking.ArmSensor.Finger fingerSensor, int i) {
            Transform proximalTarget = handTarget.fingers.allFingers[i].proximal.target.transform;
            proximalTarget.rotation = proximalTarget.parent.rotation * HumanoidTarget.ToQuaternion(fingerSensor.proximal.rotation);

            Transform intermediateTarget = handTarget.fingers.allFingers[i].intermediate.target.transform;
            intermediateTarget.rotation = intermediateTarget.parent.rotation * HumanoidTarget.ToQuaternion(fingerSensor.intermediate.rotation);

            Transform distalTarget = handTarget.fingers.allFingers[i].distal.target.transform;
            distalTarget.rotation = distalTarget.parent.rotation * HumanoidTarget.ToQuaternion(fingerSensor.distal.rotation);

            handTarget.DetermineFingerCurl((Humanoid.Tracking.Finger)i);
        }

        #region Skeleton

        protected virtual void UpdateHandFromSkeleton() {
            Transform wristBone = handSkeleton.GetWristBone();
            handTarget.hand.target.transform.position = wristBone.transform.position;
            if (handTarget.isLeft)
                handTarget.hand.target.transform.rotation = wristBone.transform.rotation; // * Quaternion.Euler(-90, 0, 90);
            else
                handTarget.hand.target.transform.rotation = wristBone.transform.rotation; // * Quaternion.Euler(-90, 0, -90);

            UpdateThumbFromSkeleton();
            UpdateIndexFingerFromSkeleton();
            UpdateMiddleFingerFromSkeleton();
            UpdateRingFingerFromSkeleton();
            UpdateLittleFingerFromSkeleton();
        }

        protected virtual void UpdateThumbFromSkeleton() {
            FingersTarget.TargetedFinger finger = handTarget.fingers.thumb;
            UpdateFingerBoneFromSkeleton(finger.proximal.target.transform, Finger.Thumb, FingerBone.Proximal);
            UpdateFingerBoneFromSkeleton(finger.intermediate.target.transform, Finger.Thumb, FingerBone.Intermediate);
            UpdateFingerBoneFromSkeleton(finger.distal.target.transform, Finger.Thumb, FingerBone.Distal);
        }

        protected void UpdateIndexFingerFromSkeleton() {
            FingersTarget.TargetedFinger finger = handTarget.fingers.index;
            UpdateFingerBoneFromSkeleton(finger.proximal.target.transform, Finger.Index, FingerBone.Proximal);
            UpdateFingerBoneFromSkeleton(finger.intermediate.target.transform, Finger.Index, FingerBone.Intermediate);
            UpdateFingerBoneFromSkeleton(finger.distal.target.transform, Finger.Index, FingerBone.Distal);
        }

        protected void UpdateMiddleFingerFromSkeleton() {
            FingersTarget.TargetedFinger finger = handTarget.fingers.middle;
            UpdateFingerBoneFromSkeleton(finger.proximal.target.transform, Finger.Middle, FingerBone.Proximal);
            UpdateFingerBoneFromSkeleton(finger.intermediate.target.transform, Finger.Middle, FingerBone.Intermediate);
            UpdateFingerBoneFromSkeleton(finger.distal.target.transform, Finger.Middle, FingerBone.Distal);
        }

        protected void UpdateRingFingerFromSkeleton() {
            FingersTarget.TargetedFinger finger = handTarget.fingers.ring;
            UpdateFingerBoneFromSkeleton(finger.proximal.target.transform, Finger.Ring, FingerBone.Proximal);
            UpdateFingerBoneFromSkeleton(finger.intermediate.target.transform, Finger.Ring, FingerBone.Intermediate);
            UpdateFingerBoneFromSkeleton(finger.distal.target.transform, Finger.Ring, FingerBone.Distal);
        }

        protected void UpdateLittleFingerFromSkeleton() {
            FingersTarget.TargetedFinger finger = handTarget.fingers.little;
            UpdateFingerBoneFromSkeleton(finger.proximal.target.transform, Finger.Little, FingerBone.Proximal);
            UpdateFingerBoneFromSkeleton(finger.intermediate.target.transform, Finger.Little, FingerBone.Intermediate);
            UpdateFingerBoneFromSkeleton(finger.distal.target.transform, Finger.Little, FingerBone.Distal);
        }

        //protected void UpdateFingerBoneFromSkeleton(Transform targetTransform, Finger finger, FingerBone fingerBone) {
        //    if (handSkeleton == null)
        //        return;

        //    Transform thisBoneTransform = handSkeleton.GetBone(finger, fingerBone);
        //    Transform nextBoneTransform = handSkeleton.GetBone(finger, fingerBone + 1);
        //    if (thisBoneTransform == null || nextBoneTransform == null)
        //        return;

        //    Vector3 direction = nextBoneTransform.position - thisBoneTransform.position;
        //    if (handTarget.isLeft)
        //        targetTransform.rotation = Quaternion.LookRotation(direction, handTarget.hand.target.transform.forward) * Quaternion.Euler(-90, 0, 90);
        //    else
        //        targetTransform.rotation = Quaternion.LookRotation(direction, handTarget.hand.target.transform.forward) * Quaternion.Euler(-90, 0, -90);
        //}

        protected virtual void UpdateFingerBoneFromSkeleton(Transform targetTransform, Finger finger, FingerBone fingerBone) {
            if (handSkeleton == null)
                return;

            Transform wristBone = handSkeleton.GetWristBone();

            Transform thisBoneTransform = handSkeleton.GetBone(finger, fingerBone);
            Transform nextBoneTransform = handSkeleton.GetBone(finger, fingerBone + 1);
            if (thisBoneTransform == null || nextBoneTransform == null)
                return;

            Vector3 direction = nextBoneTransform.position - thisBoneTransform.position;
            if (handTarget.isLeft) {
                Quaternion rotation = Quaternion.LookRotation(direction, wristBone.transform.forward) * Quaternion.Euler(-90, 0, 90);
                targetTransform.rotation = handTarget.hand.target.transform.rotation * Quaternion.Inverse(wristBone.transform.rotation) * rotation;
            }
            else {
                Quaternion rotation = Quaternion.LookRotation(direction, wristBone.transform.forward) * Quaternion.Euler(-90, 0, -90);
                targetTransform.rotation = handTarget.hand.target.transform.rotation * Quaternion.Inverse(wristBone.transform.rotation) * rotation;
            }
        }


        #endregion

        #endregion

        public float ArmConfidence(Humanoid.Tracking.ArmSensor armSensor) {
            float armOrientationsConfidence =
                //armSensor.shoulder.confidence.rotation *
                armSensor.upperArm.confidence.rotation *
                armSensor.forearm.confidence.rotation;
            return armOrientationsConfidence;
        }


        public virtual void Vibrate(float length, float strength) {
        }
    }
}

namespace Passer.Humanoid { 
    public class ArmController : Humanoid.ArmSensor {
        protected Humanoid.Tracking.Sensor.ID sensorID;
        protected Controller controllerInput;
        public Humanoid.Tracking.ArmController controller;

        public override Tracker.Status status {
            get {
                if (controller == null)
                    return Tracker.Status.Unavailable;
                else
                    return controller.status;
            }
            set {
                if (controller != null)
                    controller.status = value;
            }
        }

        #region Start
        public override void Start(HumanoidControl _humanoid, Transform targetTransform) {
            base.Start(_humanoid, targetTransform);
            sensorID = handTarget.isLeft ? Humanoid.Tracking.Sensor.ID.LeftHand : Humanoid.Tracking.Sensor.ID.RightHand;
            controllerInput = Controllers.GetController(0);
        }
        #endregion

        #region Update
        public override void Update() {
            if (tracker == null || !tracker.enabled || !enabled)
                return;

            controller.Update();
            if (controller.status != Tracker.Status.Tracking)
                return;

            UpdateSensorTransform(controller);
            UpdateTargetTransform();
        }

        protected void UpdateInput(Controller controller, Humanoid.Tracking.ArmController armController) {
            if (handTarget.isLeft)
                SetControllerInput(controller.left, armController);
            else
                SetControllerInput(controller.right, armController);
        }

        protected void SetControllerInput(ControllerSide controllerSide, Humanoid.Tracking.ArmController armController) {
            controllerSide.stickHorizontal += armController.input.stickHorizontal;
            controllerSide.stickVertical += armController.input.stickVertical;
            controllerSide.stickButton |= armController.input.stickPress;

            //controllerSide.up |= armController.input.up;
            //controllerSide.down |= armController.input.down;
            //controllerSide.left |= armController.input.left;
            //controllerSide.right |= armController.input.right;

            controllerSide.buttons[0] |= armController.input.buttons[0];
            controllerSide.buttons[1] |= armController.input.buttons[1];
            controllerSide.buttons[2] |= armController.input.buttons[2];
            controllerSide.buttons[3] |= armController.input.buttons[3];

            controllerSide.trigger1 += armController.input.trigger1;
            controllerSide.trigger2 += armController.input.trigger2;

            controllerSide.option |= armController.input.option;
        }
        #endregion
    }
}