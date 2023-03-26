using System.Collections.Generic;
using UnityEngine;

namespace Passer.Humanoid {

/*
    [System.Serializable]
    public class HandPoseMixer : PoseMixer {
        protected HandTarget handTarget;

        public HandPoseMixer(HandTarget handTarget) {
            this.handTarget = handTarget;
        }

        public override MixedPose Add() {
            HandPose newHandPose = ScriptableObject.CreateInstance<HandPose>();
            newHandPose.Init(handTarget);

            MixedPose newMixedPose = new MixedPose() {
                pose = newHandPose
            };
            mixedPoses.Add(newMixedPose);
            return newMixedPose;
        }

        /// <summary>Return the best matching Pose for the current hand pose</summary>
        public Pose DeterminePose(FingersTarget fingersTarget, bool isLeft) {
            int bestHandPose = -1;
            float bestHandPoseConfidence = 0;
            int i = 0;
            foreach (MixedPose mixedPose in mixedPoses) {
                float confidence = mixedPose.pose.GetConfidence();           
                if (confidence > bestHandPoseConfidence) {
                    bestHandPose = i;
                    bestHandPoseConfidence = confidence;
                }
                i++;
            }

            if (bestHandPoseConfidence > 0.4F) {
                return mixedPoses[bestHandPose].pose;
            }
            else {
                return null;
            }
        }
    }

    [System.Serializable]
    [CreateAssetMenu(menuName = "Humanoid/HandPose", fileName = "HumanoidHandPose", order = 104)]
    public class HandPose : Pose {
        protected HandTarget handTarget;

        public float thumbCurl;
        //protected float thumbSwing;
        public float indexCurl;
        public float middleCurl;
        public float ringCurl;
        public float littleCurl;
        //bool withOrientation
        //Quaternion orientation;

        public void Init(HandTarget handTarget) {
            this.handTarget = handTarget;
        }

        public override void UpdatePose(HumanoidControl humanoid) {
            thumbCurl = handTarget.fingers.thumb.GetCurl(handTarget.isLeft);
            indexCurl = handTarget.fingers.index.GetCurl(handTarget.isLeft);
            middleCurl = handTarget.fingers.middle.GetCurl(handTarget.isLeft);
            ringCurl = handTarget.fingers.ring.GetCurl(handTarget.isLeft);
            littleCurl = handTarget.fingers.little.GetCurl(handTarget.isLeft);
        }

        public override float GetConfidence() {
            float thumbCurlScore = GetFingerScore(handTarget.fingers.thumb.GetCurl(handTarget.isLeft), thumbCurl);
            float indexCurlScore = GetFingerScore(handTarget.fingers.index.GetCurl(handTarget.isLeft), indexCurl);
            float middleCurlScore = GetFingerScore(handTarget.fingers.middle.GetCurl(handTarget.isLeft), middleCurl);
            float ringCurlScore = GetFingerScore(handTarget.fingers.ring.GetCurl(handTarget.isLeft), ringCurl);
            float littleCurlScore = GetFingerScore(handTarget.fingers.little.GetCurl(handTarget.isLeft), littleCurl);

            return 1 - (thumbCurlScore + indexCurlScore + middleCurlScore + ringCurlScore + littleCurlScore);
        }

        private static float GetFingerScore(float curl, float targetCurl) {
            float score = Mathf.Abs(curl - targetCurl);
            score = score * score;
            return score;
        }
    }
*/
}