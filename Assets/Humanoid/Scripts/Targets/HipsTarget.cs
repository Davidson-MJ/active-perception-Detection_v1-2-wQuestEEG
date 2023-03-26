using UnityEngine;

namespace Passer.Humanoid {
    using Humanoid.Tracking;

    /// <summary>
    /// Humanoid Control options for torso related stuff
    /// </summary>
    /// More information on the Humanoid Control Hips Target Component click
    /// <a href="https://passervr.com/documentation/humanoid-control/hips-target/">here</a>
    [HelpURLAttribute("https://passervr.com/documentation/humanoid-control/hips-target/")]
    public partial class HipsTarget : HumanoidTarget {

        public HipsTarget() {
            chest = new TargetedChestBone(this);
            spine = new TargetedSpineBone(this);
            hips = new TargetedHipsBone(this);
        }

        public bool newSpineIK = false;
        public TorsoMovements torsoMovements = new TorsoMovements();

        #region Limitations
        public const float maxSpineAngle = 20;
        public const float maxChestAngle = 20;
        #endregion

        #region Sensors
        public TorsoAnimator torsoAnimator = new TorsoAnimator();
        public override Passer.Sensor animator { get { return torsoAnimator;  } }

#if hOPENVR && hVIVETRACKER && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
        public ViveTrackerTorso viveTracker = new ViveTrackerTorso();
#endif
#if hNEURON
        public PerceptionNeuronTorso neuron = new PerceptionNeuronTorso();
#endif
#if hKINECT1
        public Kinect1Torso kinect1 = new Kinect1Torso();
#endif
#if hKINECT2
        public Kinect2Torso kinect2 = new Kinect2Torso();
#endif
#if hKINECT4
        public Kinect4Torso kinect4 = new Kinect4Torso();
#endif
#if hORBBEC
        public AstraTorso astra = new AstraTorso();
#endif
#if hOPTITRACK
        public OptitrackTorso optitrack = new OptitrackTorso();
#endif

        private TorsoSensor[] sensors;


        public override void InitSensors() {
            if (sensors == null) {
                sensors = new TorsoSensor[] {
                    torsoAnimator,
#if hOPENVR && hVIVETRACKER && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
                    viveTracker,
#endif
#if hKINECT1
                    kinect1,
#endif
#if hKINECT2
                    kinect2,
#endif
#if hKINECT4
                    kinect4,
#endif
#if hORBBEC
                    astra,
#endif
#if hNEURON
                    neuron,
#endif
#if hOPTITRACK
                    optitrack,
#endif
                };
            }
        }

        public override void StartSensors() {
            torsoAnimator.Start(humanoid, transform);

            for (int i = 0; i < sensors.Length; i++)
                sensors[i].Start(humanoid, this.transform);
        }

        protected override void UpdateSensors() {
            for (int i = 0; i < sensors.Length; i++)
                sensors[i].Update();
        }
        #endregion

        #region SubTargets

        public override TargetedBone main {
            get { return hips; }
        }

        #region Chest

        public TargetedChestBone chest = null;

        [System.Serializable]
        public class TargetedChestBone : TargetedBone {
            private HipsTarget hipsTarget;

            public TargetedChestBone(HipsTarget hipsTarget) {
                this.hipsTarget = hipsTarget;
                boneId = Bone.Chest;
            }

            // Disabled because it gives movement issues with UMA avatars
            //public override void RetrieveBones(HumanoidControl humanoid) {
            //    if (humanoid.targetsRig != null)
            //        GetDefaultTargetBone(humanoid.targetsRig, ref target.transform, boneId);
            //    if (humanoid.avatarRig != null)
            //        GetDefaultBone(humanoid.avatarRig, ref bone.transform, boneId, "Spine2");
            //}

            public override void Init() {
                parent = hipsTarget.spine;
                nextBone = (hipsTarget.humanoid.headTarget.neck.bone.transform != null) ?
                    (TargetedBone)hipsTarget.humanoid.headTarget.neck :
                    (TargetedBone)hipsTarget.humanoid.headTarget.head;
                boneId = Bone.Chest;
            }

            public override Quaternion DetermineRotation() {
                if (nextBone.bone.transform == null)
                    return Quaternion.identity;

                Vector3 chestUpDirection = Vector3.up;
                if (nextBone != null && nextBone.bone.transform != null)
                    chestUpDirection = (nextBone.bone.transform.position - bone.transform.position).normalized;

                Vector3 humanoidForward = hipsTarget.hips.bone.targetRotation * Vector3.forward; // GetForward();
                //Vector3 humanoidForward = hipsTarget.hips.bone.targetRotation * Vector3.forward;
                Quaternion chestRotation = Quaternion.LookRotation(chestUpDirection, -humanoidForward) * Quaternion.AngleAxis(90, Vector3.right); ;
                bone.baseRotation = Quaternion.Inverse(hipsTarget.humanoid.transform.rotation) * chestRotation;

                return chestRotation;
            }

            public override float GetTension() {
                Quaternion restRotation = hipsTarget.spine.bone.targetRotation;
                float tension = GetTension(restRotation, this);
                return tension;
            }
        }

        #endregion

        #region Spine

        public TargetedSpineBone spine = null;

        [System.Serializable]
        public class TargetedSpineBone : TargetedBone {
            private HipsTarget hipsTarget;

            public TargetedSpineBone(HipsTarget hipsTarget) {
                this.hipsTarget = hipsTarget;
                boneId = Bone.Spine;
            }

            public override void RetrieveBones(HumanoidControl humanoid) {
                if (humanoid.targetsRig != null)
                    GetDefaultTargetBone(humanoid.targetsRig, ref target.transform, boneId);
                if (humanoid.avatarRig != null)
                    GetDefaultBone(humanoid.avatarRig, ref bone.transform, boneId, "Spine");
            }

            public override void Init() {
                parent = hipsTarget.hips;
                if (hipsTarget.chest.bone.transform != null)
                    nextBone = hipsTarget.chest;
                else
                    nextBone = hipsTarget.humanoid.headTarget.neck;
                boneId = Bone.Spine;
            }

            public override Quaternion DetermineRotation() {
                Vector3 spineUpDirection = hipsTarget.humanoid.up;
                if (nextBone != null && nextBone.bone.transform != null)
                    spineUpDirection = nextBone.bone.transform.position - bone.transform.position;

                Vector3 humanoidForward = hipsTarget.hips.bone.targetRotation * Vector3.forward;
                Quaternion spineRotation = Quaternion.LookRotation(spineUpDirection, -humanoidForward) * Quaternion.AngleAxis(90, Vector3.right);

                bone.baseRotation = Quaternion.Inverse(hipsTarget.humanoid.transform.rotation) * spineRotation;

                return spineRotation;
            }

            public override float GetTension() {
                Quaternion restRotation = hipsTarget.hips.bone.targetRotation;
                float tension = GetTension(restRotation, this);
                return tension;
            }
        }
        #endregion

        #region Hips

        public TargetedHipsBone hips = null;

        [System.Serializable]
        public class TargetedHipsBone : TargetedBone {
            public HipsTarget hipsTarget;

            public TargetedHipsBone(HipsTarget hipsTarget) {
                this.hipsTarget = hipsTarget;
                boneId = Bone.Hips;
            }

            public override void RetrieveBones(HumanoidControl humanoid) {
                if (humanoid.targetsRig != null)
                    GetDefaultTargetBone(humanoid.targetsRig, ref target.transform, boneId);
                if (humanoid.avatarRig != null)
                    GetDefaultBone(humanoid.avatarRig, ref bone.transform, boneId, "Hips");
            }

            public override void Init() {
                parent = null;
                if (hipsTarget.spine.bone.transform != null)
                    nextBone = hipsTarget.spine;
                else if (hipsTarget.chest.bone.transform != null)
                    nextBone = hipsTarget.chest;
                else
                    nextBone = hipsTarget.humanoid.headTarget.neck;
                boneId = Bone.Hips;
            }

            public Vector3 GetForward() {
                HumanoidControl humanoid = hipsTarget.humanoid;
                if (humanoid.rightFootTarget.upperLeg.bone.transform == null || humanoid.leftFootTarget.upperLeg.bone.transform == null)
                    return humanoid.transform.forward;

                Vector3 humanoidRight = humanoid.rightFootTarget.upperLeg.bone.transform.position - humanoid.leftFootTarget.upperLeg.bone.transform.position;
                Vector3 humanoidForward = Vector3.Cross(humanoidRight, humanoid.up);
                return humanoidForward;
            }

            public override Quaternion DetermineRotation() {
                Vector3 hipsUp = nextBone.bone.transform.position - hipsTarget.hips.bone.transform.position;

                Vector3 humanoidForward = GetForward();

                Quaternion hipsRotation = Quaternion.LookRotation(hipsUp, -humanoidForward) * Quaternion.AngleAxis(90, Vector3.right);

                bone.baseRotation = Quaternion.Inverse(hipsTarget.humanoid.transform.rotation) * hipsRotation;

                return hipsRotation;
            }

            protected override void DetermineBasePosition() {
                if (target.basePosition.sqrMagnitude != 0)
                    // Base Position is already determined
                    return;

                Transform basePositionReference = GetBasePositionReference();
                target.basePosition = basePositionReference.InverseTransformPoint(target.transform.position);
            }

            public override Vector3 TargetBasePosition() {
                Transform basePositionReference = GetBasePositionReference();
                return basePositionReference.TransformPoint(target.basePosition);
            }

            private Transform GetBasePositionReference() {
                return hipsTarget.humanoid.transform;
            }

            protected static Quaternion quaternionZero = new Quaternion(0, 0, 0, 0);

            public override void MatchTargetToAvatar() {
                // Don't know why this was here 
                // but with this, the targets will never be matched when changing avatars 
                //if (Application.isPlaying)
                //    return;

                if (bone.transform == null || target.transform == null)
                    return;

                if (!Application.isPlaying) {
                    float targetDistance = Vector3.Distance(bone.transform.position, target.transform.position);
                    if (targetDistance > 0.001F)
                        target.transform.position = bone.transform.position;

                    float targetAngle = Quaternion.Angle(bone.targetRotation, target.transform.rotation);
                    if (targetAngle > 0.1F)
                        target.transform.rotation = bone.targetRotation;
                }
                else {
                    target.transform.position = bone.transform.position;

                    if (bone.toTargetRotation != quaternionZero)
                        target.transform.rotation = bone.targetRotation;
                    else
                        target.transform.rotation = hipsTarget.humanoid.transform.rotation * bone.baseRotation;
                }

                DetermineBasePosition();
                DetermineBaseRotation();
            }
        }

        #endregion

        private void InitSubTargets() {
            hips.hipsTarget = this;

            hips.Init();
            spine.Init();
            chest.Init();
        }

        private void SetTargetPositionsToAvatar() {
            hips.SetTargetPositionToAvatar();
            spine.SetTargetPositionToAvatar();
            chest.SetTargetPositionToAvatar();

            // We need to set neck target here too, because HeadTarget.InitComponent is called later and the chest direction depends on the neck.target.position...
            humanoid.headTarget.neck.SetTargetPositionToAvatar();
        }

        private void DoMeasurements() {
            hips.DoMeasurements();
            spine.DoMeasurements();
            chest.DoMeasurements();
        }
        #endregion

        #region Configuration

        public override Transform GetDefaultTarget(HumanoidControl humanoid) {
            Transform targetTransform = null;
            GetDefaultBone(humanoid.targetsRig, ref targetTransform, HumanBodyBones.Hips);
            return targetTransform;
        }

        // Do not remove this, this is dynamically called from Target_Editor!
        public static HipsTarget CreateTarget(HumanoidTarget oldTarget) {
            GameObject targetObject = new GameObject("Hips Target");
            Transform targetTransform = targetObject.transform;
            HumanoidControl humanoid = oldTarget.humanoid;

            targetTransform.parent = oldTarget.humanoid.transform;
            targetTransform.position = oldTarget.transform.position;
            targetTransform.rotation = oldTarget.transform.rotation;

            HipsTarget hipsTarget = targetTransform.gameObject.AddComponent<HipsTarget>();
            hipsTarget.humanoid = humanoid;
            humanoid.hipsTarget = hipsTarget;

            hipsTarget.RetrieveBones();
            hipsTarget.InitAvatar();
            hipsTarget.MatchTargetsToAvatar();
            //hipsTarget.NewComponent(oldTarget.humanoid);
            //hipsTarget.InitComponent();

            return hipsTarget;
        }

        // Do not remove this, this is dynamically called from Target_Editor!
        // Changes the target transform used for this head target
        // Generates a new headtarget component, so parameters will be lost if transform is changed
        public static HipsTarget SetTarget(HumanoidControl humanoid, Transform targetTransform, bool isLeft) {
            HipsTarget currentHipsTarget = humanoid.hipsTarget;
            if (targetTransform == currentHipsTarget.transform)
                return currentHipsTarget;

            GetDefaultBone(humanoid.targetsRig, ref targetTransform, HumanBodyBones.Hips);
            if (targetTransform == null)
                return currentHipsTarget;

            HipsTarget hipsTarget = targetTransform.GetComponent<HipsTarget>();
            if (hipsTarget == null)
                hipsTarget = targetTransform.gameObject.AddComponent<HipsTarget>();

            hipsTarget.NewComponent(humanoid);
            hipsTarget.InitComponent();

            return hipsTarget;
        }

        public void RetrieveBones() {
            hips.RetrieveBones(humanoid);
            spine.RetrieveBones(humanoid);
            chest.RetrieveBones(humanoid);
        }

        #endregion

        #region Settings

        public float bendingFactor = 1;

        #endregion

        #region Init
        public static bool IsInitialized(HumanoidControl humanoid) {
            if (humanoid.hipsTarget == null || humanoid.hipsTarget.humanoid == null || humanoid.hipsTarget.hips.hipsTarget == null)
                return false;
            if (humanoid.hipsTarget.hips.target.transform == null)
                return false;
            if (humanoid.hipsTarget.hips.bone.transform == null && humanoid.hipsTarget.spine.bone.transform == null && humanoid.hipsTarget.chest.bone.transform == null)
                return false;
            return true;
        }

        private void Reset() {
            humanoid = GetHumanoid();
            if (humanoid == null)
                return;

            NewComponent(humanoid);

            spine.bone.maxAngle = maxSpineAngle;
            chest.bone.maxAngle = maxChestAngle;
        }

        private HumanoidControl GetHumanoid() {
            // This does not work for prefabs
            HumanoidControl[] humanoids = FindObjectsOfType<HumanoidControl>();

            for (int i = 0; i < humanoids.Length; i++) {
                if (humanoids[i].hipsTarget != null && humanoids[i].hipsTarget.transform == this.transform)
                    return humanoids[i];
            }

            return null;
        }

        public override void InitAvatar() {
            InitSubTargets();
            DoMeasurements();

            torsoLength = DetermineTorsoLength();
            spine2HipsRotation = DetermineSpine2HipsRotation();
        }

        public float torsoLength;
        // This is the hipsRotation when the neck is exactly above the hips.
        // This depends on the default curvature of the spine.
        //When the spine is straight, deltaHipRotation = 0
        public Quaternion spine2HipsRotation;

        public override void InitComponent() {
            //bones = new TargetedBone[] { hips, spine, chest };
            //bonesReverse = new TargetedBone[] { chest, spine, hips };

            //foreach (TargetedBone bone in bones)
            //    bone.Init(this);
            InitSubTargets();

            RetrieveBones();

            // We need to do this before the measurements
            //foreach (TargetedBone bone in bones)
            //    bone.SetTargetPositionToAvatar();
            SetTargetPositionsToAvatar();

            // We need the neck.bone to measure the chest length. This can be null when the avatar is changed
            if (humanoid.headTarget.neck.bone.transform == null)
                humanoid.headTarget.neck.RetrieveBones(humanoid);
            //HeadTarget.GetDefaultNeck(humanoid.avatarRig, ref humanoid.headTarget.neck.bone.transform);
            humanoid.headTarget.neck.SetTargetPositionToAvatar();

            //foreach (TargetedBone bone in bones)
            //    bone.DoMeasurements();
            DoMeasurements();

            if (humanoid.headTarget.neck.bone.transform != null && hips.bone.transform != null)
                torsoLength = Vector3.Distance(humanoid.headTarget.neck.bone.transform.position, hips.bone.transform.position);
            else if (humanoid.headTarget.neck.target.transform != null)
                torsoLength = Vector3.Distance(humanoid.headTarget.neck.target.transform.position, hips.target.transform.position);
            else
                return;

            spine2HipsRotation = DetermineSpine2HipsRotation();
        }

        private float DetermineTorsoLength() {
            if (humanoid.headTarget.neck.bone.transform != null && hips.bone.transform != null)
                return Vector3.Distance(humanoid.headTarget.neck.bone.transform.position, hips.bone.transform.position);
            else if (humanoid.headTarget.neck.target.transform != null)
                return Vector3.Distance(humanoid.headTarget.neck.target.transform.position, hips.target.transform.position);
            else
                return 0.5F;
        }

        protected Quaternion DetermineSpine2HipsRotation() {
            Vector3 torsoTop;
            if (humanoid.headTarget.neck.bone.transform != null)
                torsoTop = humanoid.headTarget.neck.bone.transform.position;
            else
                torsoTop = humanoid.headTarget.neck.target.transform.position;

            if (hips.bone.transform != null) {
                Vector3 torsoUp = torsoTop - hips.bone.transform.position;
                Vector3 hipsForward = hips.bone.targetRotation * Vector3.forward; //hips.target.transform.forward; //humanoid.transform.forward;
                Quaternion torsoRotation = Quaternion.LookRotation(torsoUp, -hipsForward) * Quaternion.AngleAxis(90, Vector3.right);
                return Quaternion.Inverse(torsoRotation) * hips.bone.targetRotation;
            }
            else
                return spine2HipsRotation;
        }

        public override void StartTarget() {
            InitSensors();

            torsoMovements.Start(humanoid, this);
        }

        /// <summary>
        /// Checks whether the humanoid has an HipsTarget
        /// and adds one if none has been found
        /// </summary>
        /// <param name="humanoid">The humanoid to check</param>
        public static void DetermineTarget(HumanoidControl humanoid) {
            HipsTarget hipsTarget = humanoid.hipsTarget;

            if (hipsTarget == null) {
                Transform hipsTargetTransform = humanoid.targetsRig.GetBoneTransform(HumanBodyBones.Hips);
                if (hipsTargetTransform == null) {
                    Debug.LogError("Could not find hips bone in targets rig");
                    return;
                }

                hipsTarget = hipsTargetTransform.GetComponent<HipsTarget>();
                if (hipsTarget == null) {
                    hipsTarget = hipsTargetTransform.gameObject.AddComponent<HipsTarget>();
                    hipsTarget.humanoid = humanoid;
                }
            }

            humanoid.hipsTarget = hipsTarget;
        }

        public override void MatchTargetsToAvatar() {
            //if (!Application.isPlaying)
            //    return;

            hips.MatchTargetToAvatar();
            if (main.bone.transform != null && main.target.transform != null && transform != null) {
                transform.position = main.target.transform.position;
                // This is disabled, because the hips rotation is more dependent on the head target
                // than the hips target. Enabling this will make the posing instable.
                transform.rotation = main.target.transform.rotation;
            }
            spine.MatchTargetToAvatar();
            chest.MatchTargetToAvatar();
        }


        #endregion

        #region Update

        public override void UpdateTarget() {
            hips.target.confidence.Degrade();
            spine.target.confidence = Confidence.none;
            chest.target.confidence = Confidence.none;

            UpdateSensors();
        }

        public override void UpdateMovements(HumanoidControl humanoid) {
            TorsoMovements.Update(this);
        }

        public override void CopyTargetToRig() {
            if (Application.isPlaying &&
                humanoid.animatorEnabled && humanoid.targetsRig.runtimeAnimatorController != null)
                return;

            if (hips.target.transform == null || transform == hips.target.transform)
                return;

            hips.target.transform.position = transform.position;
            hips.target.transform.rotation = transform.rotation;
        }

        public override void CopyRigToTarget() {
            if (hips.target.transform == null || transform == hips.target.transform)
                return;

            if (!Application.isPlaying && hips.bone.transform != null) {
                float targetDistance = Vector3.Distance(hips.bone.transform.position, hips.target.transform.position);
                if (targetDistance < 0.001F)
                    return;
                float angleDifference = Quaternion.Angle(hips.bone.targetRotation, hips.target.transform.rotation);
                if (angleDifference < 0.1F)
                    return;
            }

            transform.position = hips.target.transform.position;
            transform.rotation = hips.target.transform.rotation;
        }

        public void UpdateSensorsFromTarget() {
            if (sensors == null)
                return;

            for (int i = 0; i < sensors.Length; i++)
                sensors[i].UpdateSensorTransformFromTarget(this.transform);
        }

        #endregion

        #region DrawRigs

        protected override void DrawTargetRig(HumanoidControl humanoid) {
            if (this != humanoid.hipsTarget)
                return;

            DrawTarget(hips.target.confidence, hips.target.transform, Vector3.up, hips.target.length);
            DrawTarget(spine.target.confidence, spine.target.transform, Vector3.up, spine.target.length);
            DrawTarget(chest.target.confidence, chest.target.transform, Vector3.up, chest.target.length);
        }

        protected override void DrawAvatarRig(HumanoidControl humanoid) {
            if (this != humanoid.hipsTarget)
                return;

            if (chest.bone.transform != null)
                Debug.DrawRay(chest.bone.transform.position, chest.bone.targetRotation * Vector3.up * chest.bone.length, Color.cyan);
            if (spine.bone.transform != null)
                Debug.DrawRay(spine.bone.transform.position, spine.bone.targetRotation * Vector3.up * spine.bone.length, Color.cyan);
            if (hips.bone.transform != null)
                Debug.DrawRay(hips.bone.transform.position, hips.bone.targetRotation * Vector3.up * hips.bone.length, Color.cyan);
        }

        #endregion
    }
}