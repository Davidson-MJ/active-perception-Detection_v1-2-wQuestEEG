using System.Collections;
using UnityEngine;

namespace Passer.Humanoid {
    using Humanoid.Tracking;

    public class HeadMovements : Movements {
        protected HeadTarget headTarget;

        #region Start
        public override void Start(HumanoidControl _humanoid, HumanoidTarget _target) {
            base.Start(_humanoid, _target);
            headTarget = (HeadTarget)_target;

            if (headTarget.neck.bone.transform != null)
                headTarget.neck.bone.transform.rotation = headTarget.neck.target.transform.rotation * headTarget.neck.target.toBoneRotation;
            if (headTarget.head.bone.transform != null)
                headTarget.head.bone.transform.rotation = headTarget.head.target.transform.rotation * headTarget.head.target.toBoneRotation;
        }
        #endregion

        #region Update

        public static void Update(HeadTarget headTarget) {
            if (headTarget.head.bone.transform == null || !headTarget.humanoid.calculateBodyPose)
                return;

#if hFACE
            if (headTarget.neck.target.confidence.rotation < 0.2F && headTarget.head.target.confidence.rotation < 0.2F &&
                headTarget.face.leftEye.target.confidence.rotation > 0.2F) {

                UpdateHeadBonesFromGazeDirection(headTarget);
            }
            else {
#endif
                if (Application.isPlaying && headTarget.humanoid.primaryTarget == HumanoidControl.PrimaryTarget.Hips) {
                    UpdateNeckRotation(headTarget);
                    UpdateNeckPositionFromHips(headTarget);
                    UpdateHeadPositionFromNeck(headTarget);
                }
                else {
                    UpdateHead(headTarget);
                    UpdateNeck(headTarget);
                }
#if hFACE
            }
            headTarget.face.UpdateMovements();
#endif
        }

        private static void UpdateHead(HeadTarget headTarget) {
            if (headTarget.head.target.transform == null)
                return;

            headTarget.head.SetBoneRotation(headTarget.head.target.transform.rotation);

            // Commented out the requirement for position confidence > 0 because of Oculus Go/GearVR positional issue
            if (!Application.isPlaying ||
                (headTarget.humanoid.targetsRig.runtimeAnimatorController == null
                || headTarget.head.target.confidence.position > 0)
                )
                headTarget.head.SetBonePosition(headTarget.head.target.transform.position);
        }

        private static void UpdateHeadPositionFromNeck(HeadTarget headTarget) {
            headTarget.head.SetBoneRotation(headTarget.head.target.transform.rotation);
            Vector3 neckPosition = headTarget.neck.bone.transform.position;
            Vector3 headPosition = neckPosition + headTarget.head.target.transform.rotation * Vector3.up * headTarget.neck.bone.length;
            headTarget.head.SetBonePosition(headPosition);
        }


        private static void UpdateNeck(HeadTarget headTarget) {
            if (headTarget.neck.bone.transform == null)
                return;

            Vector3 headPosition = headTarget.head.bone.transform.position;
            Quaternion headRotation = headTarget.head.bone.transform.rotation;

            if (headTarget.neck.target.confidence.rotation > headTarget.head.target.confidence.rotation) {
                Quaternion neckRotation = headTarget.neck.target.transform.rotation;
                headTarget.neck.SetBoneRotation(neckRotation);

                if (headTarget.humanoid.targetsRig.runtimeAnimatorController == null ||
                    headTarget.head.target.confidence.position > 0) {

                    Vector3 neckPosition = headPosition - neckRotation * Vector3.up * headTarget.neck.bone.length;
                    headTarget.neck.SetBonePosition(neckPosition);
                }
            }
            else {
                Quaternion hipsTargetRotation = headTarget.humanoid.hipsTarget.hips.bone.targetRotation * Quaternion.Inverse(headTarget.humanoid.hipsTarget.hips.bone.baseRotation);
                Quaternion neckRotation =
                    Quaternion.Slerp(headTarget.head.bone.targetRotation, hipsTargetRotation, 0.3F) * headTarget.neck.bone.baseRotation;

                headTarget.neck.SetBoneRotation(neckRotation);

                // Commented out the requirement for position confidence > 0 because of Oculus Go/GearVR positional issue
                if (!Application.isPlaying || (
                    headTarget.humanoid.targetsRig.runtimeAnimatorController == null
                    || headTarget.head.target.confidence.position > 0
                    )) {
                    Vector3 neckPosition = headPosition - neckRotation * Vector3.up * headTarget.neck.bone.length;
                    headTarget.neck.SetBonePosition(neckPosition);
                }
            }

            headTarget.head.bone.transform.position = headPosition;
            headTarget.head.bone.transform.rotation = headRotation;
        }

        private static void UpdateNeckRotation(HeadTarget headTarget) {
            Quaternion headRotation = headTarget.head.bone.transform.rotation;

            if (headTarget.neck.target.confidence.rotation > headTarget.head.target.confidence.rotation) {
                Quaternion neckRotation = headTarget.neck.target.transform.rotation;
                headTarget.neck.SetBoneRotation(neckRotation);
            }
            else {
                Quaternion hipsTargetRotation = headTarget.humanoid.hipsTarget.hips.bone.targetRotation * Quaternion.Inverse(headTarget.humanoid.hipsTarget.hips.bone.baseRotation);
                Quaternion neckRotation =
                    Quaternion.Slerp(headTarget.head.bone.targetRotation, hipsTargetRotation, 0.3F) * headTarget.neck.bone.baseRotation;

                headTarget.neck.SetBoneRotation(neckRotation);
            }

            headTarget.head.bone.transform.rotation = headRotation;
        }

        private static void UpdateNeckPositionFromHips(HeadTarget headTarget) {
            HipsTarget hipsTarget = headTarget.humanoid.hipsTarget;
            Vector3 hipsPosition = hipsTarget.hips.bone.transform.position;

            if (hipsTarget.chest.bone.transform != null) {
                Vector3 chestTopPosition = hipsTarget.chest.bone.transform.position + hipsTarget.chest.bone.targetRotation * Vector3.up * hipsTarget.chest.bone.length;
                //Vector3 spineVector = chestTopPosition - hipsPosition;
                headTarget.neck.SetBonePosition(chestTopPosition);
            }
            else if (hipsTarget.spine.bone.transform != null) {
                Vector3 spineTopPosition = hipsTarget.spine.bone.transform.position + hipsTarget.spine.bone.targetRotation * Vector3.up * hipsTarget.spine.bone.length;
                //Vector3 spineVector = spineTopPosition - hipsPosition;
                headTarget.neck.SetBonePosition(spineTopPosition);
            }
        }

        public static Quaternion CalculateNeckRotation(Quaternion hipRotation, Quaternion headRotation) {
            Vector3 headAnglesCharacterSpace = (Quaternion.Inverse(hipRotation) * headRotation).eulerAngles;
            float neckYRotation = UnityAngles.Normalize(headAnglesCharacterSpace.y) * 0.6F;
            Quaternion neckRotation = hipRotation * Quaternion.Euler(headAnglesCharacterSpace.x, neckYRotation, headAnglesCharacterSpace.z);

            return neckRotation;
        }

        public static Vector3 CalculateNeckPositionFromEyes(Vector3 eyePosition, Quaternion eyeRotation, Vector3 eye2neck) {
            Vector3 neckPosition = eyePosition + eyeRotation * eye2neck;
            return neckPosition;
        }

        public static Vector3 CalculateNeckPositionFromHead(Vector3 headPosition, Quaternion headRotation, float neckBoneLength) {
            Vector3 neckPosition = headPosition - headRotation * Vector3.up * neckBoneLength;
            return neckPosition;
        }

        static float lastTime;
        private static void UpdateHeadBonesFromGazeDirection(HeadTarget headTarget) {
            if (headTarget.humanoid.hipsTarget.hips.bone.transform == null)
                return;
#if hFACE
            Quaternion neckParentRotation = headTarget.humanoid.hipsTarget.hips.bone.transform.rotation * headTarget.humanoid.hipsTarget.hips.bone.toTargetRotation;

            Quaternion oldNeckRotation = headTarget.neck.bone.transform.rotation * headTarget.neck.bone.toTargetRotation;
            Vector3 localNeckAngles = (Quaternion.Inverse(neckParentRotation) * oldNeckRotation).eulerAngles;

            Quaternion predictedRotation = Quaternion.Slerp(Quaternion.identity, headTarget.neck.bone.rotationVelocity, Time.deltaTime);

            Quaternion eyeRotation = headTarget.face.leftEye.target.transform.rotation;
            Vector3 localEyeAngles = (Quaternion.Inverse(oldNeckRotation) * eyeRotation).eulerAngles;
            if (Quaternion.Angle(oldNeckRotation, eyeRotation) < 2) {
                lastTime = Time.time;
                return;
            }

            float headTensionY = CalculateTension(localNeckAngles, headTarget.neck.bone.maxAngles);
            float eyeTensionY = CalculateTension(localEyeAngles, headTarget.face.leftEye.bone.maxAngles);
            float f = Mathf.Clamp01(1 - (headTensionY / eyeTensionY));

            Quaternion targetRotation = Quaternion.LookRotation(headTarget.face.gazeDirection, Vector3.up); // this will overshoot,  because the lookdirection itself is affected...

            Quaternion neckRotation = Quaternion.RotateTowards(oldNeckRotation, targetRotation, f * 2);
            Quaternion desiredRotation = Quaternion.Inverse(oldNeckRotation) * neckRotation;

            // This limits the speed changes
            float deltaTime = Time.time - lastTime;
            Quaternion resultRotation = Quaternion.RotateTowards(predictedRotation, desiredRotation, deltaTime * 100);
            neckRotation = oldNeckRotation * resultRotation;

            headTarget.neck.bone.transform.rotation = neckRotation * headTarget.neck.target.toBoneRotation;
            lastTime = Time.time;
#endif
        }

        private void UpdateHeadBonesFromLookDirection() {
            Quaternion neckParentRotation = headTarget.humanoid.hipsTarget.hips.bone.transform.rotation * headTarget.humanoid.hipsTarget.hips.bone.toTargetRotation;

            Quaternion oldNeckRotation = headTarget.neck.bone.transform.rotation * headTarget.neck.bone.toTargetRotation;
            Vector3 localNeckAngles = (Quaternion.Inverse(neckParentRotation) * oldNeckRotation).eulerAngles;

            Quaternion predictedRotation = Quaternion.Slerp(Quaternion.identity, headTarget.neck.bone.rotationVelocity, Time.deltaTime);

            Quaternion targetRotation = Quaternion.LookRotation(headTarget.lookDirection, Vector3.up); // this will overshoot,  because the lookdirection itself is affected...

            Quaternion neckRotation = targetRotation;
            Quaternion desiredRotation = Quaternion.Inverse(oldNeckRotation) * neckRotation;

            // This limits the speed changes
            Quaternion resultRotation = Quaternion.RotateTowards(predictedRotation, desiredRotation, Time.deltaTime * 6);
            neckRotation = oldNeckRotation * resultRotation;

            headTarget.neck.bone.transform.rotation = neckRotation * headTarget.neck.target.toBoneRotation;
        }

        #endregion

        private static float CalculateTension(Vector3 angle, Vector3 maxAngle) {
            float dX = CalculateTension(angle.x, maxAngle.x);
            float dY = CalculateTension(angle.y, maxAngle.y);
            float dZ = CalculateTension(angle.z, maxAngle.z);
            return (dX + dY + dZ);
        }

        private static float CalculateTension(float angle, float maxAngle) {
            return (maxAngle != 0) ? Mathf.Abs(Angle.Normalize(angle) / maxAngle) : 0;
        }
    }

    public class HeadCollisionHandler : MonoBehaviour {

        static public Collider AddHeadCollider(GameObject headObject) {
            HeadTarget headTarget = headObject.GetComponent<HeadTarget>();
            if (headTarget.headRigidbody == null) {
                headTarget.headRigidbody = headObject.AddComponent<Rigidbody>();
                if (headTarget.headRigidbody != null) {
                    headTarget.headRigidbody.mass = 1;
                    headTarget.headRigidbody.useGravity = false;
                    headTarget.headRigidbody.isKinematic = true;
                }
            }

            Collider collider = headObject.GetComponent<Collider>();
            if (collider != null)
                return collider;

            SphereCollider sphereCollider = headObject.AddComponent<SphereCollider>();
            if (sphereCollider != null) {
                sphereCollider.isTrigger = true;
                sphereCollider.radius = 0.1F;
                sphereCollider.center = new Vector3(0, 0, 0.05F);
            }

            return sphereCollider;
        }

        private HumanoidControl humanoid;
        private Material fadeMaterial;

        public void Initialize(HumanoidControl _humanoid) {
            humanoid = _humanoid;
            if (humanoid.headTarget.collisionFader)
                FindFadeMaterial();
        }

        private void FindFadeMaterial() {
#if pUNITYXR
            if (humanoid.unity.hmd == null || humanoid.unity.hmd.unityCamera == null)
#endif
#if hLEGACYXR
            if (humanoid.headTarget.unity == null || humanoid.headTarget.unity.cameraTransform == null)
#endif
                return;

#if pUNITYXR
            Transform plane = humanoid.unity.hmd.unityCamera.transform.Find("Fader");
#endif
#if hLEGACYXR
            Transform plane = humanoid.headTarget.unity.cameraTransform.Find("Fader");
#endif
#if pUNITYXR || hLEGACYXR
            if (plane != null) {
                Renderer renderer = plane.GetComponent<Renderer>();
                if (renderer != null) {
                    renderer.enabled = true;
                    fadeMaterial = renderer.sharedMaterial;
                    Color color = fadeMaterial.color;
                    color.a = 0.0F;
                    fadeMaterial.color = color;
                }
            }
#endif
        }

        void OnTriggerEnter(Collider otherCollider) {
            if (fadeMaterial == null || otherCollider.isTrigger)
                return;

            if (otherCollider.attachedRigidbody == null) {
                DoFadeOut();
                humanoid.headTarget.isInsideCollider = true;
            }
        }

        void OnTriggerExit(Collider otherCollider) {

            if (fadeMaterial == null || otherCollider.isTrigger)
                return;

            if (otherCollider.attachedRigidbody == null) {
                DoFadeIn();
                humanoid.headTarget.isInsideCollider = false;
            }
        }

        private void DoFadeOut() {
            StartCoroutine(FadeOut(fadeMaterial));
        }
        private void DoFadeIn() {
            StartCoroutine(FadeIn(fadeMaterial));
        }

        private bool faded = false;
        public float fadeTime = 0.3F;

        public IEnumerator FadeOut(Material fadeMaterial) {
            if (!faded) {
                float elapsedTime = 0.0f;
                Color color = fadeMaterial.color;
                color.a = 0.0f;
                fadeMaterial.color = color;
                while (elapsedTime < fadeTime) {
                    yield return new WaitForEndOfFrame();
                    elapsedTime += Time.deltaTime;
                    color.a = Mathf.Clamp01(elapsedTime / fadeTime);
                    fadeMaterial.color = color;
                }
            }
            faded = true;
        }

        public IEnumerator FadeIn(Material fadeMaterial) {
            if (faded) {
                float elapsedTime = 0.0f;
                Color color = fadeMaterial.color;
                while (elapsedTime < fadeTime) {
                    yield return new WaitForEndOfFrame();
                    elapsedTime += Time.deltaTime;
                    color.a = 1.0f - Mathf.Clamp01(elapsedTime / fadeTime);
                    fadeMaterial.color = color;
                }
            }
            faded = false;
        }
    }
}