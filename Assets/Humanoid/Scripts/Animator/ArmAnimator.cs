using UnityEngine;

namespace Passer.Humanoid {
    using Humanoid.Tracking;

    [System.Serializable]
    public class ArmPredictor : Humanoid.ArmSensor {

        public override Tracker.Status status {
            get { return Tracker.Status.Tracking; }
            set { }
        }
    }

    [System.Serializable]
    public class ArmAnimator : Humanoid.ArmSensor {
        private Quaternion handBaseRotation;

        public override Tracker.Status status {
            get { return Tracker.Status.Tracking; }
            set { }
        }

        public bool armSwing;
        [HideInInspector]
        protected Vector3 foot2hand;

        public override void Start(HumanoidControl humanoid, Transform targetTransform) {
            base.Start(humanoid, targetTransform);
            target = targetTransform.GetComponent<HandTarget>();

            if (handTarget.isLeft)
                handBaseRotation = Quaternion.Euler(-25, 0, 90);
            else
                handBaseRotation = Quaternion.Euler(-25, 0, -90);

            foot2hand = CalculateFoot2Hand();

        }

        public override void Update() {
            if (!handTarget.humanoid.animatorEnabled || !enabled || handTarget.humanoid.targetsRig.runtimeAnimatorController != null)
                return;

            if (handTarget.hand.bone.transform == null || handTarget.upperArm.bone.transform == null)
                return;

            if (armSwing) {
                if (ArmSwingAnimation())
                    return;
            }

            Quaternion localAvatarRotation = Quaternion.Inverse(handTarget.humanoid.transform.rotation) * handTarget.humanoid.hipsTarget.hips.bone.targetRotation;
            float avatarYangle = localAvatarRotation.eulerAngles.y;
            Quaternion avatarRotation = handTarget.humanoid.transform.rotation * Quaternion.AngleAxis(avatarYangle, Vector3.up);

            Vector3 handPosition = CalulateIdleHandPosition(avatarRotation);

            Quaternion forearmRotation = handTarget.forearm.bone.targetRotation;

            Quaternion handRotation;
            if (handTarget.forearm.target.confidence.rotation > 0.2F)
                handRotation = avatarRotation * forearmRotation;
            else
                handRotation = avatarRotation * handBaseRotation;

            handTarget.hand.target.transform.position = handPosition;
            handTarget.hand.target.transform.rotation = handRotation;
            handTarget.hand.target.confidence.position = 0.2F;
            handTarget.hand.target.confidence.rotation = 0.2F;

            // This should not be here, it will disable finger input when a pose is present
            //handTarget.poseMixer.ResetAffectedBones(handTarget.humanoid, handTarget.side);
        }

        protected Vector3 CalulateIdleHandPosition(Quaternion avatarRotation) {
            HumanoidControl humanoid = handTarget.humanoid;

            float hipsWidth = 0.4F;
            if (humanoid.leftFootTarget.upperLeg.bone.transform != null && humanoid.rightFootTarget.upperLeg.bone.transform != null)
                hipsWidth = Vector3.Distance(humanoid.leftFootTarget.upperLeg.bone.transform.position, humanoid.rightFootTarget.upperLeg.bone.transform.position);
            float handDistanceFromHips = hipsWidth / 2 + 0.15F;

            Vector3 positionBetweenArms = humanoid.headTarget.neck.target.transform.position;
            if (handTarget.upperArm.bone.transform != null && handTarget.otherHand.upperArm.bone.transform != null)
                positionBetweenArms = (handTarget.upperArm.bone.transform.position + handTarget.otherHand.upperArm.bone.transform.position) / 2;

            Vector3 handPosition = positionBetweenArms;
            handPosition += avatarRotation *
                new Vector3(
                    (handTarget.isLeft ? -handDistanceFromHips : handDistanceFromHips),
                    -(handTarget.upperArm.bone.length + handTarget.forearm.bone.length - 0.05F),
                    0
                );

            return handPosition;
        }

        protected Vector3 CalculateFoot2Hand() {
            HumanoidControl humanoid = handTarget.humanoid;

            Quaternion avatarRotation = humanoid.transform.rotation;

            Vector3 handPosition = CalulateIdleHandPosition(avatarRotation);
            Vector3 footPosition = humanoid.transform.position;
            if (humanoid.leftFootTarget.foot.bone.transform != null && humanoid.rightFootTarget.foot.bone.transform != null)
                footPosition = handTarget.isLeft ? humanoid.rightFootTarget.foot.bone.transform.position : humanoid.leftFootTarget.foot.bone.transform.position;
            Vector3 foot2hand = Quaternion.Inverse(humanoid.transform.rotation) * (handPosition - footPosition);
            foot2hand += new Vector3(0, 0, 0.05F);
            return foot2hand;
        }


        /// <summary>Animates the arm swing based on foot movements</summary>
        /// <returns>boolean indicating is the arms are swinging</returns>
        protected bool ArmSwingAnimation() {
            HumanoidControl humanoid = handTarget.humanoid;
            HipsTarget hipsTarget = humanoid.hipsTarget;

            Vector3 newPosition;
            float localFootZ;
            if (handTarget.isLeft) {
                localFootZ = hipsTarget.transform.InverseTransformPoint(humanoid.rightFootTarget.transform.position).z;
                Vector3 localFootPosition = Quaternion.Inverse(hipsTarget.hips.bone.targetRotation) * (humanoid.rightFootTarget.transform.position - humanoid.transform.position);
                newPosition = localFootPosition + foot2hand;

                handTarget.hand.target.transform.rotation = handTarget.forearm.bone.targetRotation;
                handTarget.hand.target.confidence.rotation = 0.2F;
            }
            else {
                localFootZ = hipsTarget.transform.InverseTransformPoint(humanoid.leftFootTarget.transform.position).z;
                Vector3 localFootPosition = Quaternion.Inverse(hipsTarget.hips.bone.targetRotation) * (humanoid.leftFootTarget.transform.position - humanoid.transform.position);
                newPosition = localFootPosition + foot2hand;

                handTarget.hand.target.transform.rotation = handTarget.forearm.bone.targetRotation;
                handTarget.hand.target.confidence.rotation = 0.2F;
            }

            float newY = Mathf.Abs(localFootZ / 4 + 0.02f) - humanoid.transform.position.y;
            handTarget.hand.target.transform.position = hipsTarget.transform.TransformPoint(new Vector3(newPosition.x, newY, newPosition.z));
            handTarget.hand.target.confidence.position = 0.2F;
            return true;
        }
    }
}