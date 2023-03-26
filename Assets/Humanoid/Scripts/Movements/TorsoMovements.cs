using UnityEngine;

namespace Passer.Humanoid {

    public partial class HipsTarget {
        public float hipsBaseHeight;
    }

    public class TorsoMovements {
        private HipsTarget hipsTarget;

        private Breathing breathing = new Breathing();

        #region Start
        public void Start(HumanoidControl _humanoid, HipsTarget _target) {
            hipsTarget = _target;

            if (hipsTarget.hips.bone.transform == null)
                return;
        }
        #endregion

        #region Update

        public static void Update(HipsTarget hipsTarget) {
            if (hipsTarget.hips.bone.transform == null || !hipsTarget.humanoid.calculateBodyPose)
                return;

            if (Mathf.Abs(hipsTarget.hipsBaseHeight) < 0.00001F)
                hipsTarget.hipsBaseHeight = hipsTarget.hips.bone.transform.position.y - hipsTarget.hips.target.transform.position.y;

            HeadTarget headTarget = hipsTarget.humanoid.headTarget;
            HumanoidTarget.BoneTransform neckBone = (headTarget.neck.bone.transform != null) ? headTarget.neck.bone : headTarget.head.bone;
            Vector3 neckPosition = neckBone.transform.position;
            Quaternion neckRotation = neckBone.transform.rotation;
            Quaternion neckTargetRotation = neckRotation * neckBone.toTargetRotation * Quaternion.Inverse(headTarget.neck.bone.baseRotation);

            if (Application.isPlaying &&
                hipsTarget.humanoid.targetsRig.runtimeAnimatorController != null &&
                hipsTarget.hips.target.confidence.position == 0 &&
                hipsTarget.humanoid.headTarget.head.target.confidence.position == 0) {

                hipsTarget.torsoMovements.CharacterNoIK(hipsTarget, neckPosition);
            }
            else {
                if (hipsTarget.hips.target.confidence.rotation > 0.5F && hipsTarget.spine.target.confidence.rotation > 0.5F && hipsTarget.chest.target.confidence.rotation > 0.5F)
                    hipsTarget.torsoMovements.CharacterNoIK(hipsTarget, neckPosition);
                else if (!hipsTarget.newSpineIK)
                    hipsTarget.torsoMovements.NewIK(hipsTarget, neckPosition, neckTargetRotation);
                else
                    hipsTarget.torsoMovements.SimpleIK(hipsTarget);

                if (Application.isPlaying)
                    hipsTarget.torsoMovements.breathing.Update();

                neckBone.transform.position = neckPosition;
                neckBone.transform.rotation = neckRotation;
            }
        }

        public void CharacterNoIK(HipsTarget hipsTarget, Vector3 neckBonePosition) {
            hipsTarget.hips.SetBoneRotation(hipsTarget.hips.target.transform.rotation);
            hipsTarget.spine.SetBoneRotation(hipsTarget.spine.target.transform.rotation);
            hipsTarget.chest.SetBoneRotation(hipsTarget.chest.target.transform.rotation);

            if (hipsTarget.humanoid.headTarget.head.target.confidence.position > 0) {
                if (hipsTarget.chest.bone.transform != null) {
                    Vector3 chestTopPosition = hipsTarget.chest.bone.transform.position + hipsTarget.chest.bone.targetRotation * Vector3.up * hipsTarget.chest.bone.length;
                    Vector3 spineVector = chestTopPosition - hipsTarget.hips.bone.transform.position;
                    hipsTarget.hips.SetBonePosition(neckBonePosition - spineVector);
                }
                else if (hipsTarget.spine.bone.transform != null) {
                    Vector3 spineTopPosition = hipsTarget.spine.bone.transform.position + hipsTarget.spine.bone.targetRotation * Vector3.up * hipsTarget.spine.bone.length;
                    Vector3 spineVector = spineTopPosition - hipsTarget.hips.bone.transform.position;
                    hipsTarget.hips.SetBonePosition(neckBonePosition - spineVector);
                }
            }
            else {
                Vector3 hipsPosition = hipsTarget.hips.target.transform.position + hipsTarget.hipsBaseHeight * Vector3.up;
                hipsTarget.hips.SetBonePosition(hipsPosition);
            }
        }

        private void SimpleIK(HipsTarget hipsTarget) {
            // This implementation ignores the spine and chest bones
            // Which results in a stiff spine
            Vector3 neckTargetPosition = hipsTarget.humanoid.headTarget.neck.target.transform.position;
            Quaternion neckRotation = hipsTarget.humanoid.headTarget.neck.bone.transform.rotation;

            Vector3 hipsTargetPosition = hipsTarget.hips.target.transform.position;

            Quaternion hipsTargetRotation = hipsTarget.hips.target.transform.rotation;

            Vector3 hipsBack = hipsTargetRotation * Vector3.back;
            Vector3 hipsUp = neckTargetPosition - hipsTargetPosition;

            if (hipsUp.sqrMagnitude <= 0.0001F)
                hipsUp = hipsTarget.humanoid.up;

            Quaternion spineRotation = Quaternion.LookRotation(hipsUp, hipsBack) * Quaternion.Euler(90, 0, 0);
            Quaternion hipsRotation = spineRotation * hipsTarget.spine2HipsRotation;
            hipsTarget.hips.SetBoneRotation(hipsRotation);

            Vector3 spineVector = spineRotation * Vector3.up * hipsTarget.torsoLength;
            hipsTarget.hips.SetBonePosition(hipsTarget.humanoid.headTarget.neck.target.transform.position - spineVector);

            hipsTarget.humanoid.headTarget.neck.bone.transform.rotation = neckRotation;
        }

        //float bendingFactor = 1;

        private void NewIK(HipsTarget hipsTarget, Vector3 neckBonePosition, Quaternion neckBoneTargetRotation) {
            Quaternion hipsTargetRotation;
            if (!Application.isPlaying || hipsTarget.humanoid.primaryTarget == HumanoidControl.PrimaryTarget.Head) {
                hipsTargetRotation = CalculateHipsRotation(hipsTarget, neckBonePosition);
                hipsTarget.hips.SetBoneRotation(hipsTargetRotation);

                hipsTargetRotation *= Quaternion.Inverse(hipsTarget.hips.bone.baseRotation);

                Quaternion spineTargetRotation = CalculateSpineRotation(hipsTarget, hipsTarget.spine.bone.baseRotation, hipsTargetRotation, neckBoneTargetRotation);
                hipsTarget.spine.SetBoneRotation(spineTargetRotation);

                Quaternion chestTargetRotation = CalculateChestRotation(hipsTarget, hipsTarget.chest.bone.baseRotation, hipsTargetRotation, neckBoneTargetRotation);
                hipsTarget.chest.SetBoneRotation(chestTargetRotation);

                CalculateHipsPositionFromHead(hipsTarget, neckBonePosition);
            }
            else {
                HeadTarget headTarget = hipsTarget.humanoid.headTarget;
                hipsTargetRotation = CalculateHipsRotation(hipsTarget, headTarget.head.target.transform.position);
                hipsTarget.hips.SetBoneRotation(hipsTargetRotation);

                Quaternion spineTargetRotation = CalculateSpineLookRotation(hipsTarget, hipsTarget.spine.bone.baseRotation, hipsTargetRotation);
                hipsTarget.spine.SetBoneRotation(spineTargetRotation);

                Quaternion chestTargetRotation = CalculateChestLookRotation(hipsTarget, hipsTarget.chest.bone.baseRotation, hipsTargetRotation);
                hipsTarget.chest.SetBoneRotation(chestTargetRotation);

                CalculateHipsPositionFromTarget(hipsTarget);
            }
        }

        private void CalculateHipsPositionFromHead(HipsTarget hipsTarget, Vector3 neckBonePosition) {
            if (hipsTarget.chest.bone.transform != null) {
                Vector3 chestTopPosition = hipsTarget.chest.bone.transform.position + hipsTarget.chest.bone.targetRotation * Vector3.up * hipsTarget.chest.bone.length;
                Vector3 spineVector = chestTopPosition - hipsTarget.hips.bone.transform.position;
                hipsTarget.hips.SetBonePosition(neckBonePosition - spineVector);
            }
            else if (hipsTarget.spine.bone.transform != null) {
                Vector3 spineTopPosition = hipsTarget.spine.bone.transform.position + hipsTarget.spine.bone.targetRotation * Vector3.up * hipsTarget.spine.bone.length;
                Vector3 spineVector = spineTopPosition - hipsTarget.hips.bone.transform.position;
                hipsTarget.hips.SetBonePosition(neckBonePosition - spineVector);
            }
        }

        private void CalculateHipsPositionFromTarget(HipsTarget hipsTarget) {
            hipsTarget.hips.SetBonePosition(hipsTarget.hips.target.transform.position);
        }

        #endregion

        #region Chest

        public Quaternion CalculateChestRotation(HipsTarget hipsTarget, Quaternion baseRotation, Quaternion hipsRotation, Quaternion neckRotation) {
            float factor = hipsTarget.bendingFactor * 0.3F;
            Quaternion chestRotation = Quaternion.Slerp(neckRotation, hipsRotation, factor) * baseRotation;
            return chestRotation;
        }
        public Quaternion CalculateChestLookRotation(HipsTarget hipsTarget, Quaternion baseRotation, Quaternion hipsRotation) {
            Quaternion spineRotation = hipsRotation;
            return spineRotation;

        }

        #endregion

        #region Spine

        public Quaternion CalculateSpineRotation(HipsTarget hipsTarget, Quaternion baseRotation, Quaternion hipsRotation, Quaternion neckRotation) {
            float factor = hipsTarget.chest.bone.transform != null ? 0.4F : 0.3F;
            factor *= hipsTarget.bendingFactor;
            Quaternion spineRotation = Quaternion.Slerp(neckRotation, hipsRotation, factor) * baseRotation;
            return spineRotation;
        }

        public Quaternion CalculateSpineLookRotation(HipsTarget hipsTarget, Quaternion baseRotation, Quaternion hipsRotation) {
            Quaternion spineRotation = hipsRotation;
            return spineRotation;
        }

        public static Vector3 CalculateSpinePosition(Vector3 chestPosition, Quaternion spineRotation, float spineLength) {
            Vector3 spinePosition = chestPosition - spineRotation * Vector3.up * spineLength;
            return spinePosition;
        }

        #endregion

        #region Hips

        private Quaternion CalculateHipsRotation(HipsTarget hipsTarget, Vector3 neckBonePosition) {
            if (hipsTarget.hips.target.transform == null)
                return Quaternion.identity;

            Vector3 torsoUp = neckBonePosition - hipsTarget.hips.target.transform.position;
            Quaternion spine2HipsRotation = Rotations.Rotate(hipsTarget.spine2HipsRotation, hipsTarget.hips.target.transform.rotation);
            Vector3 hipsUp = spine2HipsRotation * torsoUp;

            Vector3 hipsForward = hipsTarget.hips.target.transform.forward;

            if (hipsUp == Vector3.zero)
                return Quaternion.identity;

            Quaternion hipsRotation = Quaternion.LookRotation(hipsUp, -hipsForward) * Quaternion.AngleAxis(90, Vector3.right);

            return hipsRotation;
        }

        #endregion


        public static Vector3 CalculateNeckPosition(Vector3 chestPosition, Quaternion chestRotation, Vector3 chest2neck) {
            Vector3 neckPosition = chestPosition + chestRotation * chest2neck;
            return neckPosition;
        }

        public static Quaternion CalculateHipsRotation(Vector3 hipsPosition, Quaternion hipsRotation, Quaternion leftHandRotation, Quaternion rightHandRotation, Transform leftFoot, Transform rightFoot, Quaternion neckRotation, Vector3 neckPosition) {
            float angleX = hipsRotation.eulerAngles.x;
            float angleY = hipsRotation.eulerAngles.y;
            float angleZ = hipsRotation.eulerAngles.z;

            float dOrientation = 0;

            Vector3 leftHandForward = leftHandRotation * Vector3.left;
            Vector3 rightHandForward = rightHandRotation * Vector3.right;
            Vector3 hipsForward = hipsRotation * Vector3.forward;

            Vector2 leftHandForward2 = new Vector2(leftHandForward.x, leftHandForward.z);
            Vector2 rightHandForward2 = new Vector2(rightHandForward.x, rightHandForward.z);
            Vector2 hipsRotation2 = new Vector2(hipsForward.x, hipsForward.z);

            // Check for hands not pointing up/down too much
            float dOrientationL = leftHandForward2.sqrMagnitude > 0.1F ? UnityAngles.SignedAngle(hipsRotation2, leftHandForward2) : 0;
            float dOrientationR = rightHandForward2.sqrMagnitude > 0.1F ? UnityAngles.SignedAngle(hipsRotation2, rightHandForward2) : 0;
            if (Mathf.Sign(dOrientationL) == Mathf.Sign(dOrientationR)) {
                if (Mathf.Abs(dOrientationL) < Mathf.Abs(dOrientationR))
                    dOrientation = dOrientationL;
                else
                    dOrientation = dOrientationR;
            }

            float neckOrientation = UnityAngles.Difference(neckRotation.eulerAngles.y, angleY + dOrientation);
            if (neckOrientation < 90 && neckOrientation > -90) { // head cannot turn more than 90 degrees
                angleY += dOrientation;
            }

            Quaternion newHipsRotation = Quaternion.Euler(angleX, angleY, angleZ);
            return newHipsRotation;
        }

        public static Vector3 CalculateHipsPosition(Vector3 spinePosition, Quaternion hipsRotation, float hipsLength, Vector3 leftFootPosition, Vector3 rightFootPosition) {
            Vector3 hipsPosition = spinePosition - hipsRotation * Vector3.up * hipsLength;

            // stationary
            /*
            float footHeightDifference = leftFootPosition.y - rightFootPosition.y;

            Vector3 centerFoot;
            if (footHeightDifference > maxFeetdifference) {
                //standing on right foot
                centerFoot = rightFootPosition;
            } else if (footHeightDifference < -maxFeetdifference) {
                //standing on left foot
                centerFoot = leftFootPosition;
            } else {
                //standing on both feet
                float fraction = footHeightDifference * (0.5F / maxFeetdifference) + 0.5F;
                centerFoot = leftFootPosition * (1 - fraction) + rightFootPosition * fraction;
            }
            hipsPosition = new Vector3(centerFoot.x, hipsPosition.y, centerFoot.z);
            */
            return hipsPosition;
        }

    }

    public class Breathing {
        public float speed = 4;
        public float intensity = 1;

        private float f;
        public float v;

        public void Update() {
            f = CalculateF();
            v = Mathf.Sin(f * (Mathf.PI + Mathf.PI)) * intensity;
        }

        private float lastBreathStart;
        private float CalculateF() {
            float f = (Time.realtimeSinceStartup - lastBreathStart) / speed;
            if (f > 1) {
                lastBreathStart = Time.realtimeSinceStartup;
                f = 0;
            }

            return f;
        }
    }

}