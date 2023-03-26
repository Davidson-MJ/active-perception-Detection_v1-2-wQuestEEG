using System.Collections.Generic;
using UnityEngine;

namespace Passer.Tracking {
    using Passer.Humanoid.Tracking;

    /// <summary>
    /// Hand tracking skeleton
    /// </summary>
    public class HandSkeleton : SensorComponent {

        public bool isLeft;

        public new bool show = false;

        protected List<TrackedBone> bones;
        //protected static Material boneWhite;

        public enum BoneId {
            Invalid = -1,
            Hand = 0,
            ThumbProximal,
            ThumbIntermediate,
            ThumbDistal,
            ThumbTip,
            IndexMetaCarpal,
            IndexProximal,
            IndexIntermediate,
            IndexDistal,
            IndexTip,
            MiddleMetaCarpal,
            MiddleProximal,
            MiddleIntermediate,
            MiddleDistal,
            MiddleTip,
            RingMetaCarpal,
            RingProximal,
            RingIntermediate,
            RingDistal,
            RingTip,
            LittleMetacarpal,
            LittleProximal,
            LittleIntermediate,
            LittleDistal,
            LittleTip,

            Forearm,
            Count
        }

        #region Start

        protected virtual void InitializeSkeleton() {
            bones = new List<TrackedBone>(new TrackedBone[(int)BoneId.Count]);

            for (int i = 0; i < (int)Finger.Count; i++) {
                Transform parent = this.transform;
                for (int j = (int)FingerBone.Proximal; j <= (int)FingerBone.Tip; j++) {
                    BoneId boneId = BoneId.ThumbProximal + i * 5 + j - 1;
                    int boneIx = (int)boneId;

                    bones[boneIx] = TrackedBone.Create(boneId.ToString(), parent);
                    parent = bones[boneIx].transform;
                    if (boneId == BoneId.ThumbProximal) {
                        bones[boneIx].transform.localPosition = new Vector3(0, 0, 0.02F);
                        if (isLeft)
                            bones[boneIx].transform.localRotation = Quaternion.Euler(0, 20, 25);
                        else
                            bones[boneIx].transform.localRotation = Quaternion.Euler(0, -20, 25);
                    }
                    else if (j == (int)FingerBone.Proximal)
                        bones[boneIx].transform.localPosition = new Vector3(isLeft ? -0.06F : 0.06F, 0, 0.04F - i * 0.02F);
                    else if (i == (int)Finger.Little)
                        bones[boneIx].transform.localPosition = new Vector3(isLeft ? -0.015F : 0.015F, 0, 0);
                    else if (i == (int)Finger.Middle)
                        bones[boneIx].transform.localPosition = new Vector3(isLeft ? -0.023F : 0.023F, 0, 0);
                    else
                        bones[boneIx].transform.localPosition = new Vector3(isLeft ? -0.02F : 0.02F, 0, 0);
                }
            }
            int forearmIx = (int)BoneId.Forearm;
            bones[forearmIx] = TrackedBone.Create(BoneId.Forearm.ToString(), this.transform);
            bones[forearmIx].transform.localPosition = new Vector3(isLeft ? 0.2F : -0.2F, 0, 0);
        }

        public static HandSkeleton FindHandSkeleton(Transform trackerTransform, bool isLeft) {
            HandSkeleton[] handSkeletons = trackerTransform.GetComponentsInChildren<HandSkeleton>();
            foreach (HandSkeleton handSkeleton in handSkeletons) {
                if (handSkeleton.isLeft == isLeft)
                    return handSkeleton;
            }
            return null;
        }

        public virtual GameObject CreateHandSkeleton(Transform trackerTransform, bool isLeft, bool showRealObjects) {
            GameObject skeletonObj = new GameObject(isLeft ? "Left Hand Skeleton" : "Right Hand Skeleton");
            skeletonObj.transform.parent = trackerTransform;
            skeletonObj.transform.localPosition = Vector3.zero;
            skeletonObj.transform.localRotation = Quaternion.identity;
            return skeletonObj;
        }

        #endregion

        #region Update

        protected void UpdateSkeletonRender() {
            if (bones == null)
                return;

            // Render Skeleton
            foreach (TrackedBone bone in bones) {
                if (bone == null)
                    continue;
                LineRenderer boneRenderer = bone.transform.GetComponent<LineRenderer>();
                if (boneRenderer != null) {
                    Vector3 localParentPosition = bone.transform.InverseTransformPoint(bone.transform.parent.position);
                    boneRenderer.SetPosition(1, localParentPosition);
                    boneRenderer.enabled = show;
                }
            }
        }

        protected bool rendered;
        protected void EnableRenderer() {
            if (rendered || !show)
                return;

            Renderer[] renderers = this.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers) {
                if (renderer is LineRenderer)
                    renderer.enabled = true;
            }

            rendered = true;
        }

        protected void DisableRenderer() {
            if (!rendered)
                return;

            Renderer[] renderers = this.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers) {
                if (renderer is LineRenderer)
                    renderer.enabled = false;
            }

            rendered = false;
        }

        #endregion

        public virtual Transform GetForearmBone() {
            return bones[(int)BoneId.Forearm].transform;
        }

        public virtual Transform GetWristBone() {
            return this.transform;
        }

        public virtual Transform GetBone(Finger finger, FingerBone fingerBone) {
            if (bones == null)
                return null;

            int boneId = GetBoneId(finger, fingerBone);
            if (boneId == -1 || bones[boneId] == null)
                return null;

            return bones[boneId].transform;
        }

        public virtual int GetBoneId(Finger finger, FingerBone fingerBone) {
            return -1;
        }
    }

}