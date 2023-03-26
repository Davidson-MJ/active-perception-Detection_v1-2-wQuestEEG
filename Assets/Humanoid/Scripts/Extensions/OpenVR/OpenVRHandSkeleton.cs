using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Passer.Humanoid {
    using Passer.Tracking;
    using Passer.Humanoid.Tracking;

    /// <summary>
    /// An OpenVR hand skeleton
    /// </summary>
    public class OpenVRHandSkeleton : HandSkeleton {

#if hOPENVR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
        [System.NonSerialized]
        ulong actionHandle;
        [System.NonSerialized]
        InputSkeletalActionData_t tempSkeletonActionData = new InputSkeletalActionData_t();
        [System.NonSerialized]
        uint skeletonActionData_size;

        private const int numBones = 26;
        [System.NonSerialized]
        public VRBoneTransform_t[] tempBoneTransforms = new VRBoneTransform_t[31];

        public static OpenVRHandSkeleton Find(Transform openVRTransform, bool isLeft) {
            OpenVRHandSkeleton[] handSkeletons = openVRTransform.GetComponentsInChildren<OpenVRHandSkeleton>();
            foreach (OpenVRHandSkeleton handSkeleton in handSkeletons) {
                if (handSkeleton.isLeft == isLeft)
                    return handSkeleton;
            }
            return null;
        }

        public static OpenVRHandSkeleton Get(Transform openVRTransform, bool isLeft, bool showRealObjects) {
            OpenVRHandSkeleton skeleton = Find(openVRTransform, isLeft);
            if (skeleton == null) {
                GameObject skeletonObj = new GameObject(isLeft ? "Left Hand Skeleton" : "Right Hand Skeleton");
                skeletonObj.transform.parent = openVRTransform;
                skeletonObj.transform.localPosition = Vector3.zero;
                skeletonObj.transform.localRotation = Quaternion.identity;

                skeleton = skeletonObj.AddComponent<OpenVRHandSkeleton>();
                skeleton.isLeft = isLeft;
                skeleton.show = showRealObjects;
            }
            return skeleton;
        }

        #region Start

        protected override void Start() {
            base.Start();

            string actionName = isLeft ? "/actions/default/in/SkeletonLeftHand" : "/actions/default/in/SkeletonRightHand";
            if (Passer.OpenVR.Input == null) {
                return;
            }

            EVRInputError err = Passer.OpenVR.Input.GetActionHandle(actionName, ref actionHandle);
            if (err != EVRInputError.None) {
                Debug.LogError("OpenVR.Input.GetActionHandle error: " + err.ToString());
            }

            skeletonActionData_size = (uint)Marshal.SizeOf(tempSkeletonActionData);
        }

        protected override void InitializeSkeleton() {
            //boneWhite = new Material(Shader.Find("Standard")) {
            //    color = new Color(1, 1, 1)
            //};

            bones = new List<TrackedBone>(new TrackedBone[numBones]);

            bones[0] = new TrackedBone() {
                transform = this.transform,
            };

            Transform parent = bones[0].transform;
            for (int j = (int)FingerBone.Metacarpal; j <= (int)FingerBone.Tip; j++) {
                int boneId = 1 + j - 1;
                bones[boneId] = TrackedBone.Create(boneId.ToString(), parent);
                parent = bones[boneId].transform;
            }

            for (int i = 1; i < (int)Finger.Count; i++) {
                parent = bones[0].transform;
                for (int j = (int)FingerBone.Metacarpal; j <= (int)FingerBone.Tip; j++) {
                    int boneId = i * 5 + j;
                    bones[boneId] = TrackedBone.Create(boneId.ToString(), parent);
                    parent = bones[boneId].transform;
                }
            }
        }

        #endregion

        #region Update

        InputPoseActionData_t poseActionData;

        public override void UpdateComponent() {
            base.UpdateComponent();

            if (bones == null)
                InitializeSkeleton();
            if (bones == null) {
                status = Tracker.Status.Unavailable;
                return;
            }

            if (Passer.OpenVR.Input == null)
                return;

            ulong restrictToDevice = Passer.OpenVR.k_ulInvalidInputValueHandle;

            uint poseActionData_size = (uint)Marshal.SizeOf(poseActionData);
            Passer.OpenVR.Input.GetPoseActionData(actionHandle, ETrackingUniverseOrigin.TrackingUniverseStanding, 0, ref poseActionData, poseActionData_size, restrictToDevice);
            if (poseActionData.pose.eTrackingResult != ETrackingResult.Running_OK) {
                status = Tracker.Status.Present;
                return;
            }
            UpdateTransform(poseActionData);

            EVRInputError err = Passer.OpenVR.Input.GetSkeletalActionData(actionHandle, ref tempSkeletonActionData, skeletonActionData_size, restrictToDevice);
            if (err != EVRInputError.None) {
                Debug.LogError("GetSkeletalActionData error: " + err.ToString() + ", handle: " + actionHandle.ToString());
                return;
            }
            if (tempSkeletonActionData.bActive) {
                err = Passer.OpenVR.Input.GetSkeletalBoneData(actionHandle, EVRSkeletalTransformSpace.Parent, EVRSkeletalMotionRange.WithoutController, tempBoneTransforms, restrictToDevice);
                if (err != EVRInputError.None)
                    Debug.LogError("GetSkeletalBoneData error: " + err.ToString() + " handle: " + actionHandle.ToString());
            }

            status = Tracker.Status.Tracking;
            UpdateHand();
            //this.transform.localRotation = Quaternion.AngleAxis(Mathf.PI * Mathf.Rad2Deg, Vector3.up) * this.transform.localRotation;
            UpdateSkeletonRender();
        }

        protected void UpdateTransform(InputPoseActionData_t poseActionData) {
            HmdMatrix34_t pose = poseActionData.pose.mDeviceToAbsoluteTracking;
            Matrix4x4 m = new Matrix4x4();

            m.m00 = pose.m0;
            m.m01 = pose.m1;
            m.m02 = -pose.m2;
            m.m03 = pose.m3;

            m.m10 = pose.m4;
            m.m11 = pose.m5;
            m.m12 = -pose.m6;
            m.m13 = pose.m7;

            m.m20 = -pose.m8;
            m.m21 = -pose.m9;
            m.m22 = pose.m10;
            m.m23 = -pose.m11;

            m.m30 = 0;
            m.m31 = 0;
            m.m32 = 0;
            m.m33 = 0;

            this.transform.localPosition = GetPosition(m);
            this.transform.localRotation = GetRotation(m); // * Quaternion.AngleAxis(180, Vector3.right);
        }

        private static Quaternion GetRotation(Matrix4x4 matrix) {
            Quaternion q = Quaternion.identity; // new Rotation();
            q.w = (float)Math.Sqrt(Math.Max(0, 1 + matrix.m00 + matrix.m11 + matrix.m22)) / 2;
            q.x = (float)Math.Sqrt(Math.Max(0, 1 + matrix.m00 - matrix.m11 - matrix.m22)) / 2;
            q.y = (float)Math.Sqrt(Math.Max(0, 1 - matrix.m00 + matrix.m11 - matrix.m22)) / 2;
            q.z = (float)Math.Sqrt(Math.Max(0, 1 - matrix.m00 - matrix.m11 + matrix.m22)) / 2;
            q.x = _copysign(q.x, matrix.m21 - matrix.m12);
            q.y = _copysign(q.y, matrix.m02 - matrix.m20);
            q.z = _copysign(q.z, matrix.m10 - matrix.m01);
            return q;
        }

        private static float _copysign(float sizeval, float signval) {
            if (float.IsNaN(signval))
                return Math.Abs(sizeval);
            else
                return Math.Sign(signval) == 1 ? Math.Abs(sizeval) : -Math.Abs(sizeval);
        }

        private static Vector3 GetPosition(Matrix4x4 matrix) {
            var x = matrix.m03;
            var y = matrix.m13;
            var z = matrix.m23;

            return new Vector3(x, y, z);
        }

        bool first = true;
        protected void UpdateHand() {
            //UpdateBoneTransform(0, 0);
            if (first)
                UpdateBoneTransform0(1, 0);
            for (int i = 2; i < numBones; i++)
                UpdateBoneTransform(i, i - 1);
            //bones[1].localRotation = Quaternion.AngleAxis(Mathf.PI * Mathf.Rad2Deg, Vector3.up) * bones[1].localRotation;
            first = false;
        }

        protected void UpdateBoneTransform0(int boneIx, int boneIx2) {
            VRBoneTransform_t boneTransform = tempBoneTransforms[boneIx];

            Vector3 p = new Vector3(
                boneTransform.position.v0,
                boneTransform.position.v1,
                -boneTransform.position.v2
                );
            //Quaternion q = new Quaternion(
            //    boneTransform.orientation.x,
            //    -boneTransform.orientation.y,
            //    -boneTransform.orientation.z,
            //    boneTransform.orientation.w
            //    );
            Quaternion q2 = new Quaternion(
                boneTransform.orientation.z,
                boneTransform.orientation.w,
                -boneTransform.orientation.x,
                -boneTransform.orientation.y
                );

            // This needs a transformation!
            bones[boneIx2].transform.localRotation = q2;
            bones[boneIx2].transform.localPosition = p;

        }

        protected void UpdateBoneTransform(int boneIx, int boneIx2) {
            VRBoneTransform_t boneTransform = tempBoneTransforms[boneIx];

            Vector3 p = new Vector3(
                -boneTransform.position.v0,
                boneTransform.position.v1,
                boneTransform.position.v2
                );
            Quaternion q = new Quaternion(
                boneTransform.orientation.x,
                -boneTransform.orientation.y,
                -boneTransform.orientation.z,
                boneTransform.orientation.w
                );

            bones[boneIx2].transform.localRotation = q;
            bones[boneIx2].transform.localPosition = p;
        }

        #endregion

        public override Transform GetWristBone() {
            return bones[0].transform;
        }

        public override Transform GetBone(Finger finger, FingerBone fingerBone) {
            if (bones == null)
                return null;

            int boneId = GetBoneId(finger, fingerBone);
            if (boneId == -1)
                return null;

            return bones[boneId].transform;
        }

        public override int GetBoneId(Finger finger, FingerBone fingerBone) {
            if (finger == Finger.Thumb)
                return (int)fingerBone;
            else
                return (int)finger * 5 + (int)fingerBone;
        }
#endif 
    }
}