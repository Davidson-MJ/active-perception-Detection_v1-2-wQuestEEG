using UnityEngine;

namespace Passer.Humanoid {

    [System.Serializable]
    public class LegMovements : Movements {

        #region Update
        public static void Update(FootTarget footTarget) {
            if (footTarget == null || footTarget.foot.bone.transform == null || !footTarget.humanoid.calculateBodyPose)
                return;

            footTarget.CheckGrounded();

            if (Application.isPlaying &&
                footTarget.humanoid.targetsRig.runtimeAnimatorController != null
                && footTarget.foot.target.confidence.position == 0) {

                footTarget.legMovements.FullForwardKinematics(footTarget);
            }
            else {
                if (footTarget.foot.target.confidence.rotation >= footTarget.lowerLeg.target.confidence.rotation &&
                    (footTarget.foot.target.confidence.position >= footTarget.lowerLeg.target.confidence.rotation ||
                    footTarget.ground != null))

                    footTarget.legMovements.FullInverseKinematics(footTarget);
                else if (footTarget.lowerLeg.target.confidence.rotation > footTarget.foot.target.confidence.rotation)
                    footTarget.legMovements.LowerLegForwardKinematics(footTarget);
                else
                    footTarget.legMovements.FullForwardKinematics(footTarget);
            }
        }

        private void FullInverseKinematics(FootTarget footTarget) {
            //Debug.Log("FullInverseKinematics");

            if (footTarget.foot.target.transform == null)
                return;

            Vector3 footPosition = NaturalFootPosition(footTarget);

            Quaternion upperLegRotation = NaturalUpperLegOrientation(footTarget, footPosition);
            footTarget.upperLeg.SetBoneRotation(upperLegRotation);

            Quaternion lowerLegRotation = NaturalLowerLegOrientation(footTarget, upperLegRotation, footPosition);
            footTarget.lowerLeg.SetBoneRotation(lowerLegRotation);

            Quaternion footOrientation = NaturalFootOrientation(footTarget);
            footTarget.foot.SetBoneRotation(footOrientation);

            Quaternion toesOrientation = NaturalToesOrientation(footTarget);
            footTarget.toes.SetBoneRotation(toesOrientation);

            if (!Application.isPlaying)
                PlaceFootOnLowerLeg(footTarget, lowerLegRotation);
        }

        private void FullForwardKinematics(FootTarget footTarget) {
            //Debug.Log("FullForwardKinematics");
            footTarget.upperLeg.SetBoneRotation(footTarget.upperLeg.target.transform.rotation);
            footTarget.lowerLeg.SetBoneRotation(footTarget.lowerLeg.target.transform.rotation);
            footTarget.foot.SetBoneRotation(footTarget.foot.target.transform.rotation);
            footTarget.toes.SetBoneRotation(footTarget.toes.target.transform.rotation);
        }

        private void LowerLegForwardKinematics(FootTarget footTarget) {
            //Debug.Log("LowerLegForwardKinematics");

            Vector3 upperLegPosition = footTarget.upperLeg.bone.transform.position;
            Vector3 lowerLegPosition = footTarget.lowerLeg.target.transform.position;
            Quaternion lowerLegRotation = footTarget.lowerLeg.target.transform.rotation;

            Quaternion upperLegRotation = CalculateUpperLegRotation(footTarget, upperLegPosition, lowerLegPosition, lowerLegRotation);
            footTarget.upperLeg.SetBoneRotation(upperLegRotation);

            footTarget.lowerLeg.SetBoneRotation(lowerLegRotation);
            footTarget.foot.SetBoneRotation(footTarget.foot.target.transform.rotation);
            footTarget.toes.SetBoneRotation(footTarget.toes.target.transform.rotation);
        }

        #endregion

        #region Upper Leg

        private Quaternion NaturalUpperLegOrientation(FootTarget footTarget, Vector3 footPosition) {
            // Update leg bone length for UMA. Leg bones lengths can change in length in UMA. 
            // This is not the best place but it works for now
            footTarget.upperLeg.bone.length = Vector3.Distance(footTarget.upperLeg.bone.transform.position, footTarget.lowerLeg.bone.transform.position);
            footTarget.lowerLeg.bone.length = Vector3.Distance(footTarget.lowerLeg.bone.transform.position, footTarget.foot.bone.transform.position);

            Quaternion oldUpperLegRotation = footTarget.upperLeg.bone.transform.rotation * footTarget.upperLeg.bone.toTargetRotation;
            Quaternion upperLegRotation = CalculateUpperLegRotation(footTarget, footTarget.upperLeg.bone.transform.position, footPosition, footTarget.foot.target.transform.rotation, footTarget.upperLeg.bone.length, footTarget.lowerLeg.bone.length);
            //upperLegRotation = LimitRotationSpeed(oldUpperLegRotation, upperLegRotation);
            if (footTarget.isLeft)
                upperLegRotation = MeasureRotationSpeed(oldUpperLegRotation, upperLegRotation);
            return upperLegRotation;
        }

        private Quaternion lastLocalUpperLegRotation = Quaternion.identity;

        public Quaternion CalculateUpperLegRotation(FootTarget footTarget, Vector3 upperLegPosition, Vector3 footPosition, Quaternion footRotation, float upperLegLength, float lowerLegLength) {
            Vector3 upperLegForward = footPosition - upperLegPosition;
            Vector3 upperLegRight = footTarget.humanoid.hipsTarget.hips.target.transform.right;
            Vector3 upperLegUp = Vector3.Cross(upperLegForward, upperLegRight);

            float hip2FootDistance = upperLegForward.magnitude;
            float hipAngle = Mathf.Acos((Square(hip2FootDistance) + Square(upperLegLength) - Square(lowerLegLength)) / (2 * upperLegLength * hip2FootDistance)) * Mathf.Rad2Deg;
            // NaN happens when the distance to the footTarget is longer than the length of the leg
            // We will stretch the leg full then (angle = 0)
            if (float.IsNaN(hipAngle))
                hipAngle = 0;

            Quaternion upperLegRotation = Quaternion.LookRotation(upperLegForward, upperLegUp);
            upperLegRotation = Quaternion.AngleAxis(hipAngle, upperLegRotation * Vector3.left) * upperLegRotation;

            upperLegRotation *= Quaternion.Euler(270, 0, 0);
            if (footTarget.upperLeg.bone.jointLimitations)
                upperLegRotation = LimitAngle(footTarget.upperLeg, ref lastLocalUpperLegRotation, upperLegRotation);

            return upperLegRotation;
        }

        public Quaternion CalculateUpperLegRotation(FootTarget footTarget, Vector3 upperLegPosition, Vector3 lowerLegPosition, Quaternion lowerLegRotation) {
            Vector3 upperLegForward = lowerLegPosition - upperLegPosition;
            Vector3 upperLegRight = lowerLegRotation * Vector3.right;
            Vector3 upperLegUp = Vector3.Cross(upperLegForward, upperLegRight);

            Quaternion upperLegRotation = Quaternion.LookRotation(upperLegForward, upperLegUp) * Quaternion.AngleAxis(270, Vector3.right);
            if (footTarget.upperLeg.bone.jointLimitations)
                upperLegRotation = LimitAngle(footTarget.upperLeg, ref lastLocalUpperLegRotation, upperLegRotation);

            return upperLegRotation;
        }

        #endregion

        #region Lower Leg
        private Quaternion NaturalLowerLegOrientation(FootTarget footTarget, Quaternion upperLegRotation, Vector3 footPosition) {
            float lowerLegAngle = CalculateKneeAngle(footTarget.upperLeg.bone.transform.position, footPosition, footTarget.upperLeg.bone.length, footTarget.lowerLeg.bone.length);

            if (footTarget.lowerLeg.bone.jointLimitations)
                lowerLegAngle = LimitKneeAngle(footTarget.lowerLeg, lowerLegAngle);

            Quaternion localLowerLegRotation = Quaternion.AngleAxis(lowerLegAngle, Vector3.right);
            Quaternion lowerLegRotation = upperLegRotation * localLowerLegRotation;

            //lowerLegRotation = LimitRotationSpeed(oldLowerLegRotation, lowerLegRotation);
            return lowerLegRotation;
        }

        public static float LimitKneeAngle(FootTarget.TargetedLowerLegBone lowerLeg, float angle) {
            return UnityAngles.Clamp(angle, 0, lowerLeg.bone.maxAngle);
        }

        public static Quaternion CalculateLowerLegRotation(Vector3 lowerLegPosition, Vector3 footPosition, Quaternion hipsRotation) {
            Vector3 lowerLegUp = hipsRotation * Vector3.forward;
            Quaternion lowerLegRotation = Quaternion.LookRotation(footPosition - lowerLegPosition, lowerLegUp);

            lowerLegRotation *= Quaternion.Euler(270, 0, 0);
            return lowerLegRotation;
        }

        public static float CalculateKneeAngle(Vector3 upperLegPosition, Vector3 footPosition, float upperLegLength, float lowerLegLength) {
            float dHipTarget = Vector3.Distance(upperLegPosition, footPosition);

            float kneeAngle = Mathf.Acos((Square(upperLegLength) + Square(lowerLegLength) - dHipTarget * dHipTarget) / (2 * upperLegLength * lowerLegLength)) * Mathf.Rad2Deg;
            if (float.IsNaN(kneeAngle))
                kneeAngle = 180;
            return 180 - kneeAngle;
        }
        #endregion

        #region Foot

        // Calculate the foot position taking body limitations into account
        private Vector3 NaturalFootPosition(FootTarget footTarget) {
            if (footTarget.ground == null || !footTarget.physics)
                return footTarget.foot.target.transform.position;
            else {
                return footTarget.foot.target.transform.position + footTarget.groundDistance * footTarget.humanoid.up;
            }
        }

        // Calculate the foot orientation taking body limitation into account
        private Quaternion NaturalFootOrientation(FootTarget footTarget) {
            Quaternion footOrientation = DetermineFootOrientation(footTarget);
            if (footTarget.foot.bone.jointLimitations)
                footOrientation = LimitAngle(footTarget.foot, ref lastLocalFootRotation, footOrientation);
            //footOrientation = LimitFootSpeed(footOrientation);
            return footOrientation;
        }

        // Limit the rotation speed of the foot to natural speeds
        private Quaternion LimitFootSpeed(FootTarget footTarget, Quaternion newFootOrientation) {
            Quaternion oldFootRotation = footTarget.foot.bone.transform.rotation * footTarget.foot.bone.toTargetRotation;

            if (footTarget.rotationSpeedLimitation)
                newFootOrientation = LimitRotationSpeed(oldFootRotation, newFootOrientation);

            return newFootOrientation;
        }

        // Limit the foot orientation from the body limitations
        private Quaternion lastLocalFootRotation = Quaternion.identity;

        // Determine foot orientation without body limitations
        private Quaternion DetermineFootOrientation(FootTarget footTarget) {
            Vector3 footAngles;
            Quaternion footOrientation;
            if (footTarget.upperLeg.target.confidence.rotation > footTarget.foot.target.confidence.rotation) {
                Vector3 upperLegAngles = footTarget.upperLeg.target.transform.eulerAngles;
                footAngles = footTarget.foot.target.transform.eulerAngles;
                footOrientation = Quaternion.Euler(footAngles.x, upperLegAngles.y, footAngles.z);
            }
            else {
                if (footTarget.ground != null) {
                    Quaternion footRotation = footTarget.foot.target.transform.rotation;
                    footOrientation = Quaternion.LookRotation(footTarget.groundNormal, footRotation * Vector3.back) * Quaternion.Euler(90, 0, 0);
                }
                else {
                    footOrientation = footTarget.foot.target.transform.rotation;
                }
            }
            return footOrientation;
        }

        public static Vector3 CalculateFootPosition(Vector3 upperLegPosition, Quaternion upperLegOrientation, float upperLegLength, Quaternion lowerLegOrientation, float lowerLegLength) {
            Vector3 lowerLegPosition = upperLegPosition + upperLegOrientation * Vector3.up * upperLegLength;
            Vector3 footPosition = lowerLegPosition + lowerLegOrientation * Vector3.up * lowerLegLength;
            return footPosition;
        }

        private static void PlaceFootOnLowerLeg(FootTarget footTarget, Quaternion lowerLegRotation) {
            footTarget.foot.bone.transform.position = footTarget.lowerLeg.bone.transform.position + lowerLegRotation * -footTarget.humanoid.up * footTarget.lowerLeg.bone.length;
        }

        #endregion

        #region Toes
        private Quaternion NaturalToesOrientation(FootTarget footTarget) {
            Quaternion footRotation = footTarget.foot.bone.transform.rotation * footTarget.foot.bone.toTargetRotation;
            if (footTarget.ground != null) {
                return Quaternion.LookRotation(footTarget.groundNormal, footRotation * Vector3.back) * Quaternion.Euler(90, 0, 0);
            }
            else {
                return footRotation;
            }
        }
        #endregion

        private static float Square(float x) {
            return x * x;
        }
    }
}