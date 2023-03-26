using UnityEngine;

namespace Passer.Humanoid {

    [System.Serializable]
    public class HeadAnimator : HeadSensor {
        public bool headAnimation = true;
        public bool faceAnimation = true;

        #region Update
        public override void Update() {
            if (!headTarget.humanoid.animatorEnabled || !enabled || headTarget.humanoid.targetsRig.runtimeAnimatorController != null)
                return;

            if (headAnimation)
                UpdateNeck();
        }

        private void UpdateNeck() {
            if (headTarget.neck.target.confidence.rotation > 0.25F)
                return;

            Vector3 headPosition = headTarget.head.target.transform.position;
            Quaternion headRotation = headTarget.head.target.transform.rotation;

            headTarget.neck.target.transform.rotation = headTarget.head.target.transform.rotation;

            headTarget.neck.target.transform.position = headPosition - headTarget.neck.target.transform.rotation * Vector3.up * headTarget.neck.bone.length;

            headTarget.head.target.transform.position = headPosition;
            headTarget.head.target.transform.rotation = headRotation;
        }
        #endregion

        public static void UpdateNeckTargetFromHead(HeadTarget headTarget) {
            Vector3 headPosition = headTarget.head.target.transform.position;
            Quaternion headRotation = headTarget.head.target.transform.rotation;

            headTarget.neck.target.transform.rotation = headTarget.head.target.transform.rotation;
            headTarget.neck.target.transform.position = headPosition - headTarget.neck.target.transform.rotation * Vector3.up * headTarget.neck.bone.length;

            headTarget.head.target.transform.position = headPosition;
            headTarget.head.target.transform.rotation = headRotation;
        }

    }
}