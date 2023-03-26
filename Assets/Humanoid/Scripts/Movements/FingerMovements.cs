using UnityEngine;

namespace Passer.Humanoid {

    public class FingerMovements {

        public static void Update(HandTarget handTarget) {
            if (handTarget == null || handTarget.hand.bone.transform == null)
                return;

            Quaternion handRotation = handTarget.hand.bone.targetRotation;

            UpdateFinger(handRotation, handTarget.fingers.thumb);
            UpdateFinger(handRotation, handTarget.fingers.index);
            UpdateFinger(handRotation, handTarget.fingers.middle);
            UpdateFinger(handRotation, handTarget.fingers.ring);
            UpdateFinger(handRotation, handTarget.fingers.little);

            handTarget.fingers.DetermineFingerCurl();
        }

        private static void UpdateFinger(Quaternion handRotation, FingersTarget.TargetedFinger finger) {
            Quaternion proximalRotation = CalculatePhalanxRotation(finger.proximal, handRotation);
            finger.proximal.SetBoneRotation(proximalRotation);

            Quaternion intermediateRotation = CalculatePhalanxRotation(finger.intermediate, proximalRotation);
            finger.intermediate.SetBoneRotation(intermediateRotation);

            Quaternion distalRotation = CalculatePhalanxRotation(finger.distal, intermediateRotation);
            finger.distal.SetBoneRotation(distalRotation);
        }

        private static Quaternion CalculatePhalanxRotation(FingersTarget.TargetedPhalanges phalanx, Quaternion parentRotation) {
            if (phalanx.target.transform == null)
                return Quaternion.identity;

            Quaternion phalanxRotationOnParent = parentRotation * phalanx.target.transform.localRotation;
            return phalanxRotationOnParent;
        }
    }
}