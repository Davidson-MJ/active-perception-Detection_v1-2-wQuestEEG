using UnityEngine;

namespace Passer.Humanoid {
    using Humanoid.Tracking;

    [System.Serializable]
    public class TorsoAnimator : TorsoSensor {

        public enum BodyRotation {
            HeadRotation = 1,
            HeadAndHandRotation = 2,
            NoRotation = 3,
        }
        public BodyRotation bodyRotation;

        private float torsoLength;

        private Quaternion torsoUprightOrientation;

        #region Start

        public override void Start(HumanoidControl humanoid, Transform targetTransform) {
            base.Start(humanoid, targetTransform);

            if (humanoid.avatarRig == null || hipsTarget.hips.bone.transform == null)
                return;

            Vector3 torsoDirection = humanoid.headTarget.neck.bone.transform.position - hipsTarget.hips.bone.transform.position;
            torsoLength = torsoDirection.magnitude;

            torsoUprightOrientation = Quaternion.FromToRotation(humanoid.up, torsoDirection);
        }

        #endregion

        #region Update

        public override void Update() {
            if (!hipsTarget.humanoid.animatorEnabled || !enabled || hipsTarget.humanoid.targetsRig == null)
                return;
            if (hipsTarget.humanoid.avatarRig == null || hipsTarget.hips.bone.transform == null)
                return;

            Animator targetAnimator = hipsTarget.humanoid.targetsRig;
            // Don't use procedural if the animator controller plays an animation clip
            if (targetAnimator.runtimeAnimatorController != null &&
                hipsTarget.humanoid.animatorParameterForward == null &&
                targetAnimator.GetCurrentAnimatorClipInfoCount(0) > 0)
                return;

            status = Tracker.Status.Tracking;

            StoreTargets();
            UpdateHipsPosition();
            UpdateHipsRotation();
            RestoreTargets();
        }

        #region Target Preserve

        private Quaternion neckRotation;
        private Vector3 neckPosition;
        private Quaternion leftHandRotation;
        private Vector3 leftHandPosition;
        private Quaternion rightHandRotation;
        private Vector3 rightHandPosition;
        private Quaternion leftFootRotation;
        private Vector3 leftFootPosition;
        private Quaternion rightFootRotation;
        private Vector3 rightFootPosition;
        private void StoreTargets() {
            neckRotation = hipsTarget.humanoid.headTarget.neck.target.transform.rotation;
            neckPosition = hipsTarget.humanoid.headTarget.neck.target.transform.position;
            leftHandRotation = hipsTarget.humanoid.leftHandTarget.hand.target.transform.rotation;
            leftHandPosition = hipsTarget.humanoid.leftHandTarget.hand.target.transform.position;
            rightHandRotation = hipsTarget.humanoid.rightHandTarget.hand.target.transform.rotation;
            rightHandPosition = hipsTarget.humanoid.rightHandTarget.hand.target.transform.position;
            leftFootRotation = hipsTarget.humanoid.leftFootTarget.foot.target.transform.rotation;
            leftFootPosition = hipsTarget.humanoid.leftFootTarget.foot.target.transform.localPosition;
            rightFootRotation = hipsTarget.humanoid.rightFootTarget.foot.target.transform.rotation;
            rightFootPosition = hipsTarget.humanoid.rightFootTarget.foot.target.transform.localPosition;
        }

        private void RestoreTargets() {
            hipsTarget.humanoid.headTarget.neck.target.transform.rotation = neckRotation;
            hipsTarget.humanoid.headTarget.neck.target.transform.position = neckPosition;
            hipsTarget.humanoid.leftHandTarget.hand.target.transform.rotation = leftHandRotation;
            hipsTarget.humanoid.leftHandTarget.hand.target.transform.position = leftHandPosition;
            hipsTarget.humanoid.rightHandTarget.hand.target.transform.rotation = rightHandRotation;
            hipsTarget.humanoid.rightHandTarget.hand.target.transform.position = rightHandPosition;
            hipsTarget.humanoid.leftFootTarget.foot.target.transform.rotation = leftFootRotation;
            hipsTarget.humanoid.leftFootTarget.foot.target.transform.localPosition = leftFootPosition;
            hipsTarget.humanoid.rightFootTarget.foot.target.transform.rotation = rightFootRotation;
            hipsTarget.humanoid.rightFootTarget.foot.target.transform.localPosition = rightFootPosition;
        }

        #endregion Target Preserve

        private void UpdateHipsRotation() {
            HeadTarget headTarget = hipsTarget.humanoid.headTarget;
            HandTarget leftHandTarget = hipsTarget.humanoid.leftHandTarget;
            HandTarget rightHandTarget = hipsTarget.humanoid.rightHandTarget;

            if (hipsTarget.hips.target.confidence.rotation < 0.5F) {
                Quaternion hipsTargetRotation = hipsTarget.hips.target.transform.rotation;
                Quaternion headTargetRotation = headTarget.head.target.transform.rotation;

                Vector3 neckTargetPosition = headTarget.neck.target.transform.position;

                if (bodyRotation == BodyRotation.NoRotation) {

                }
                else if (!(bodyRotation == BodyRotation.HeadRotation) &&
                     // still need to add foot based rotation
                     (leftHandTarget.hand.target.confidence.rotation > 0.5F && rightHandTarget.hand.target.confidence.rotation > 0.5F)) {

                    Quaternion newHipsRotation = TorsoMovements.CalculateHipsRotation(hipsTarget.hips.target.transform.position, hipsTargetRotation, leftHandTarget.transform.rotation, rightHandTarget.transform.rotation, hipsTarget.humanoid.leftFootTarget.transform, hipsTarget.humanoid.rightFootTarget.transform, headTargetRotation, neckTargetPosition);
                    hipsTarget.hips.target.transform.rotation = newHipsRotation;
                }
                else {
                    // Does not seem to be working reliably when the humanoid.up != Vector3.up

                    Vector3 hipsUpDirection = hipsTarget.humanoid.up;
                    // We need to rotate the head to the up direction to reliably determine the head forward direction
                    Quaternion headBoneRotation = headTarget.head.bone.targetRotation;
                    Quaternion rotationToUp = Quaternion.FromToRotation(headBoneRotation * Vector3.up, hipsTarget.humanoid.up);
                    Vector3 hipsBackDirection = -(rotationToUp * headBoneRotation * Vector3.forward);

                    Quaternion q = Quaternion.LookRotation(hipsUpDirection, hipsBackDirection);
                    Quaternion hipRotation = q * Quaternion.Euler(90, 0, 0);
                    hipsTarget.hips.target.transform.rotation = hipRotation;
                }
            }
            hipsTarget.hips.target.confidence.rotation = 0.2F;
        }

        protected Vector3 oldPosition; // may be neck, head of hips ATM

        private void UpdateHipsPosition() {
            if (hipsTarget.hips.target.confidence.rotation > 0.25F)
                return;

            HumanoidControl humanoid = hipsTarget.humanoid;
            HeadTarget headTarget = humanoid.headTarget;

            Vector3 oldHipPosition = hipsTarget.hips.target.transform.position;
            Vector3 neckPosition = headTarget.neck.target.transform.position;
            if (headTarget.neck.target.confidence.position == 0)
                neckPosition = headTarget.head.target.transform.position + headTarget.head.target.transform.rotation * (headTarget.neck2eyes - headTarget.head2eyes);

            // This is necessary here because the avatar can be changed or scaled.
            torsoLength = Vector3.Distance(headTarget.neck.bone.transform.position, hipsTarget.hips.bone.transform.position);

            //Quaternion headRotation = hipsTarget.humanoid.headTarget.head.target.transform.rotation;
            //Quaternion hipsNeckRotation = Quaternion.Slerp(headRotation, hipsTarget.hips.target.transform.rotation, 0.5F);
            //Debug.Log(neckRotation.eulerAngles + " " + hipsTarget.hips.target.transform.rotation.eulerAngles);
            Quaternion hipsNeckRotation = Quaternion.identity;

            Quaternion hipsHeightRotation = CalculateHipsHeightRotation(humanoid);

            Vector3 torsoVector = /*Vector3.down*/ -humanoid.up * torsoLength;
            Vector3 spine = hipsNeckRotation * hipsHeightRotation * torsoVector;

            Vector3 hipsPosition = neckPosition + spine;
            //hipsPosition = UprightCheck(humanoid, hipsPosition, neckPosition, hipsTarget.hips.target.transform.InverseTransformDirection(-spine), torsoVector);

            hipsTarget.hips.target.transform.position = hipsPosition;
            hipsTarget.hips.target.confidence.position = 0.2F;

            //Vector3 movementDirection = new Vector3(hipsPosition.x - oldHipPosition.x, 0, hipsPosition.z - oldHipPosition.z).normalized;
            //float angle = Vector3.Angle(movementDirection, humanoid.hitNormal);
            //if (humanoid.physics && (humanoid.collided && angle >= 90)) {
            //    hipsTarget.hips.target.transform.position = oldHipPosition;
            //}

            // Make sure the neck position has not changed at all
            headTarget.neck.target.transform.position = neckPosition;
            oldPosition = neckPosition;
        }

        private Vector3 CalculateHipsPosition() {
            HumanoidControl humanoid = hipsTarget.humanoid;
            HeadTarget headTarget = humanoid.headTarget;

            Vector3 oldHipPosition = hipsTarget.hips.target.transform.position;
            Vector3 neckPosition = headTarget.neck.target.transform.position;

            // This is necessary here because the avatar can be changed or scaled.
            torsoLength = Vector3.Distance(headTarget.neck.bone.transform.position, hipsTarget.hips.bone.transform.position);

            Quaternion hipsNeckRotation = Quaternion.identity;

            Quaternion hipsHeightRotation = CalculateHipsHeightRotation(humanoid);

            Vector3 torsoVector = -humanoid.up * torsoLength;
            Vector3 spine = hipsNeckRotation * hipsHeightRotation * torsoVector;

            Vector3 hipsPosition = neckPosition + spine;

            //hipsTarget.hips.target.transform.position = hipsPosition;

            // Make sure the neck position has not changed at all
            //headTarget.neck.target.transform.position = neckPosition;
            //oldPosition = neckPosition;
            return hipsPosition;
        }

        private Quaternion CalculateHipsHeightRotation(HumanoidControl humanoid) {
            //float heightFactor =
            //    (hipsTarget.hips.bone.transform.position.y - humanoid.transform.position.y - humanoid.leftFootTarget.soleThicknessFoot) /
            //    (humanoid.leftFootTarget.upperLeg.bone.length + humanoid.leftFootTarget.lowerLeg.bone.length);

            Vector3 localHipsPosition = humanoid.transform.InverseTransformPoint(hipsTarget.hips.bone.transform.position);
            float hipsHeight = Vector3.Project(localHipsPosition, Vector3.up).magnitude;
            float heightFactor =
                (hipsHeight - humanoid.leftFootTarget.soleThicknessFoot) /
                (humanoid.leftFootTarget.upperLeg.bone.length + humanoid.leftFootTarget.lowerLeg.bone.length);
            if (heightFactor > 1)
                heightFactor = 1;

            Quaternion hipsHeightRotation = Quaternion.AngleAxis(60 * (1 - heightFactor), hipsTarget.hips.target.transform.right);
            return hipsHeightRotation;
        }

        private Vector3 UprightCheck(HumanoidControl humanoid, Vector3 hipsPosition, Vector3 neckPosition, Vector3 spineDirection, Vector3 backVector) {
            float headPositionY = neckPosition.y - humanoid.transform.position.y;
            float hipsAngle = Vector3.Angle(humanoid.up, spineDirection);
            float verticalBodyStretch = headPositionY - humanoid.avatarNeckHeight;

            if (verticalBodyStretch >= -0.01F && (hipsAngle < 20 || Mathf.Abs(spineDirection.normalized.x) < 0.2F || Mathf.Abs(spineDirection.z) < 0.15F)) {
                // standing upright
                //Debug.Log("Upright " + verticalBodyStretch + " " + hipsAngle + " " + spineDirection.normalized);

                Vector3 uprightSpine = humanoid.transform.rotation * torsoUprightOrientation * backVector;
                Vector3 targetHipPosition = neckPosition + uprightSpine;
                Vector3 toTargetHipPosition = targetHipPosition - hipsPosition;

                if (hipsAngle < 30)
                    hipsPosition = hipsPosition + Vector3.ClampMagnitude(toTargetHipPosition, 0.02F);
                else
                    hipsPosition = targetHipPosition;
            }
            //else
            //    Debug.Log("Bent");
            return hipsPosition;
        }

        public static Quaternion CalculateChestRotation(Quaternion chestRotation, Quaternion hipRotation, Quaternion headRotation) {
            Vector3 chestAngles = chestRotation.eulerAngles;
            Vector3 headAnglesCharacterSpace = (Quaternion.Inverse(hipRotation) * headRotation).eulerAngles;
            float chestYRotation = UnityAngles.Normalize(headAnglesCharacterSpace.y) * 0.3F;
            Quaternion newChestRotation = hipRotation * Quaternion.Euler(chestAngles.x, chestYRotation, chestAngles.z);

            return newChestRotation;
        }
        #endregion
    }
}