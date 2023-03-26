using System.Collections.Generic;
using UnityEngine;

namespace Passer.Tracking {

    public class BodySkeleton : TrackerComponent {

        public bool show = false;

        protected List<TrackedBone> bones;
        //protected static Material boneWhite;

        #region Start

        protected virtual void InitializeSkeleton() {

        }

//        protected TrackedBone AddBone(string name, Transform parent) {
//            GameObject boneGO = new GameObject(name);
//            boneGO.transform.SetParent(parent, false);

//            AddBoneRenderer(boneGO);

//            TrackedBone bone = new TrackedBone() {
//                transform = boneGO.transform
//            };
//            return bone;
//        }

//        protected void AddBoneRenderer(GameObject boneGO) {
//            LineRenderer boneRenderer = boneGO.AddComponent<LineRenderer>();
//            boneRenderer.startWidth = 0.01F;
//            boneRenderer.endWidth = 0.01F;
//            boneRenderer.useWorldSpace = false;
//            boneRenderer.SetPosition(0, Vector3.zero);
//            boneRenderer.SetPosition(1, Vector3.zero);
//#if UNITY_2017_1_OR_NEWER
//            boneRenderer.generateLightingData = true;
//#endif
//            boneRenderer.material = boneWhite;
//        }

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
                    //boneRenderer.enabled = show;
                }
            }
        }

        protected bool rendered;
        protected void EnableRenderer() {
            if (rendered || !show)
                return;

            Renderer[] renderers = this.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers) {
                if (!(renderer is LineRenderer))
                    renderer.enabled = true;
            }

            rendered = true;
        }

        protected void DisableRenderer() {
            if (!rendered)
                return;

            Renderer[] renderers = this.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers) {
                if (!(renderer is LineRenderer))
                    renderer.enabled = false;
            }

            rendered = false;
        }

        #endregion

    }
}
