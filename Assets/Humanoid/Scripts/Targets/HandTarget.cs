using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Passer.Humanoid {
    using Tracking;

    public enum ArmBones {
        Hand,
        Forearm,
        UpperArm,
        Shoulder
    }

    public enum HandBones {
        ThumbProximal = 0,
        ThumbIntermediate = 1,
        ThumbDistal = 2,
        IndexProximal = 3,
        IndexIntermediate = 4,
        IndexDistal = 5,
        MiddleProximal = 6,
        MiddleIntermediate = 7,
        MiddleDistal = 8,
        RingProximal = 9,
        RingIntermediate = 10,
        RingDistal = 11,
        LittleProximal = 12,
        LittleIntermediate = 13,
        LittleDistal = 14,
        LastHandBone = 15
    }

    /// <summary>
    /// Humanoid Control options for hand-related stuff
    /// </summary>
    /// More information on the Humanoid Control Hand Target Component click
    /// <a href="https://passervr.com/documentation/humanoid-control/hand-target/">here</a>
    [System.Serializable]
    [HelpURLAttribute("https://passervr.com/documentation/humanoid-control/hand-target/")]
    public partial class HandTarget : HumanoidTarget {
#if LIB
        [System.NonSerialized]
        private HumanoidMovements.Arm arm;
#endif
        /// <summary>
        /// Is this the left hand?
        /// </summary>
        public bool isLeft;
        /// <summary>
        /// Left or right hand side hand
        /// </summary>
        public Side side;
        /// <summary>
        /// Vector pointing toward the outward direction of the hand.
        /// In general this is the index finger pointing direction
        /// </summary>
        public Vector3 outward;
        /// <summary>
        /// Vector3 pointing toward the up direction of the hand.
        /// This is the opposite side of the palm of the hand
        /// </summary>
        public Vector3 up;

        public FingersTarget fingers = null;

        public HandTarget() {
            shoulder = new TargetedShoulderBone(this);
            upperArm = new TargetedUpperArmBone(this);
            forearm = new TargetedForearmBone(this);
            hand = new TargetedHandBone(this);
            subTargets = new TargetedBone[] {
                shoulder,
                upperArm,
                forearm,
                hand
            };

            fingers = new FingersTarget(this);
        }

        #region Limitations

        public bool rotationSpeedLimitation = false;

        public const float maxShoulderAngle = 30;
        public const float maxUpperArmAngle = 120;
        public const float maxForearmAngle = 130;
        public const float maxHandAngle = 100;

        // for future use
        public static readonly Vector3 minLeftShoulderAngles = new Vector3(0, 0, -45);
        public static readonly Vector3 maxLeftShoulderAngles = new Vector3(0, 45, 0);
        public static readonly Vector3 minRightShoulderAngles = new Vector3(0, -45, 0);
        public static readonly Vector3 maxRightShoulderAngles = new Vector3(0, 0, 45);

        public static readonly Vector3 minLeftUpperArmAngles = new Vector3(-180, -45, -180);
        public static readonly Vector3 maxLeftUpperArmAngles = new Vector3(60, 130, 45);
        public static readonly Vector3 minRightUpperArmAngles = new Vector3(-180, -130, -45);
        public static readonly Vector3 maxRightUpperArmAngles = new Vector3(60, 45, 180);

        public static readonly Vector3 minLeftForearmAngles = new Vector3(0, 0, 0);
        public static readonly Vector3 maxLeftForearmAngles = new Vector3(0, 150, 0);
        public static readonly Vector3 minRightForearmAngles = new Vector3(0, -150, 0);
        public static readonly Vector3 maxRightForearmAngles = new Vector3(0, 0, 0);

        public static readonly Vector3 minLeftHandAngles = new Vector3(-180, -50, -70);
        public static readonly Vector3 maxLeftHandAngles = new Vector3(90, 20, 90);
        public static readonly Vector3 minRightHandAngles = new Vector3(-90, -20, -70);
        public static readonly Vector3 maxRightHandAngles = new Vector3(45, 50, 70);
        #endregion

        #region Sensors

#if pUNITYXR
        public Passer.Tracking.UnityXRController unity;
#elif hLEGACYXR
        public UnityVRHand unity = new UnityVRHand();
#endif

        private ArmPredictor armPredictor = new ArmPredictor();
        public ArmAnimator armAnimator = new ArmAnimator();
        public override Passer.Sensor animator { get { return armAnimator; } }

#if pUNITYXR
        public UnityXRHand unityXR = new UnityXRHand();
#endif
#if hOPENVR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
        public OpenVRHand openVR = new OpenVRHand();
#if hVIVETRACKER
        public ViveTrackerArm viveTracker = new ViveTrackerArm();
#endif
#endif
#if hOCULUS && (UNITY_STANDALONE_WIN || UNITY_ANDROID)
        public OculusHand oculus = new OculusHand();
#endif
#if hWINDOWSMR && UNITY_2017_2_OR_NEWER && !UNITY_2020_1_OR_NEWER && UNITY_WSA_10_0
        public WindowsMRHand mixedReality = new WindowsMRHand();
#endif
#if hWAVEVR
        public WaveVRHand waveVR = new WaveVRHand();
#endif
#if hVRTK
        public VrtkHand vrtk = new VrtkHand();
#endif
#if hNEURON
        public NeuronHand neuron = new NeuronHand();
#endif
#if hLEAP
        public LeapMotionHand leap = new LeapMotionHand();
#endif
#if hKINECT1
        public Kinect1Arm kinect1 = new Kinect1Arm();
#endif
#if hKINECT2
        public Kinect2Arm kinect2 = new Kinect2Arm();
#endif
#if hKINECT4
        public Kinect4Arm kinect4 = new Kinect4Arm();
#endif
#if hORBBEC
        public AstraArm astra = new AstraArm();
#endif
#if hHYDRA
        public RazerHydraHand hydra = new RazerHydraHand();
#endif
#if hOPTITRACK
        public OptitrackArm optitrack = new OptitrackArm();
#endif
#if hANTILATENCY
        public AntilatencyHand antilatency = new AntilatencyHand();
#endif
#if hHI5
        public Hi5Hand hi5 = new Hi5Hand();
#endif

        protected ArmSensor[] sensors;
        protected List<SensorComponent> sensorComponents = new List<SensorComponent>();
        protected List<TrackedRigidbody> trackedRigidbodies = new List<TrackedRigidbody>();

        /*! \cond PRIVATE */

        public override void InitSensors() {
#if pUNITYXR
            /*
            if (!humanoid.isRemote) {
                if (unity == null) {
                    unity = Passer.Tracking.UnityXRController.Get(humanoid.unity, isLeft);
                    if (unity != null) {
                        unity.transform.position = transform.position;
                        unity.transform.rotation = transform.rotation;
                    }
                }
                if (unity != null) {
                    if (isLeft)
                        humanoid.unity.leftController = unity;
                    else
                        humanoid.unity.rightController = unity;
                }
            }
            */
#endif

            if (sensors == null) {
                sensors = new Humanoid.ArmSensor[] {
                    armPredictor,
                    armAnimator,
#if pUNITYXR
                    unityXR,
#endif
#if hLEGACYXR
                    unity,
#endif
#if hMINDSTORMS
                    mindstormsHand,
#endif
#if hHYDRA
                    hydra,
#endif
#if hLEAP
                    leap,
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
#if hOCULUS && (UNITY_STANDALONE_WIN || UNITY_ANDROID)
                    oculus,
#endif
#if hWINDOWSMR && UNITY_2017_2_OR_NEWER && !UNITY_2020_1_OR_NEWER && UNITY_WSA_10_0
                    mixedReality,
#endif
#if hWAVEVR
                    waveVR,
#endif
#if hOPENVR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
                    openVR,
#endif
#if hOPENVR && hVIVETRACKER && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
                    viveTracker,
#endif

#if hVRTK
                    vrtk,
#endif
#if hREALSENSE
                    //realsenseHand,
#endif
#if hNEURON
                    neuron,
#endif
#if hOPTITRACK
                    optitrack,
#endif
#if hANTILATENCY
                    antilatency,
#endif
#if hHI5
                    hi5,
#endif
                };
            }
        }

        public override void StartSensors() {
            for (int i = 0; i < sensors.Length; i++)
                sensors[i].Start(humanoid, this.transform);
        }

        protected override void UpdateSensors() {
            for (int i = 0; i < sensors.Length; i++)
                sensors[i].Update();
        }

        public override void StopSensors() {
            if (sensors == null)
                return;

            for (int i = 0; i < sensors.Length; i++)
                sensors[i].Stop();
        }

        public void ShowSensors(bool shown, bool refresh = false) {
            if (sensors == null)
                InitSensors();

            if (sensors == null)
                return;

            for (int i = 0; i < sensors.Length; i++) {
                if (refresh)
                    sensors[i].RefreshSensor();
                sensors[i].ShowSensor(shown);
            }
        }

        public void AddSensorComponent(SensorComponent sensorComponent) {
            sensorComponents.Add(sensorComponent);
        }

        protected void RemoveSensorComponent(SensorComponent sensorComponent) {
            sensorComponents.Remove(sensorComponent);
        }

        public void AddTrackedRigidbody(TrackedRigidbody trackedRigidbody) {
            trackedRigidbodies.Add(trackedRigidbody);
        }

        public void RemoveTrackedRigidbody(TrackedRigidbody trackedRigidbody) {
            trackedRigidbodies.Remove(trackedRigidbody);
        }

        /*! \endcond */

        #endregion

        #region SubTargets

        public override TargetedBone main {
            get { return hand; }
        }
        public Transform stretchlessTarget;
        private readonly TargetedBone[] subTargets;

        #region Shoulder

        public TargetedShoulderBone shoulder;

        [System.Serializable]
        public class TargetedShoulderBone : TargetedBone {
            private HandTarget handTarget;

            public TargetedShoulderBone(HandTarget handTarget) : base(handTarget.upperArm) {
                this.handTarget = handTarget;
                bone.jointLimitations = true;
            }

            public override void RetrieveBones(HumanoidControl humanoid) {
                if (humanoid.targetsRig != null)
                    GetDefaultTargetBone(humanoid.targetsRig, ref target.transform, boneId);
                if (humanoid.avatarRig != null) {
                    if (handTarget.isLeft)
                        GetDefaultBone(humanoid.avatarRig, ref bone.transform, boneId, "LeftShoulder");
                    else
                        GetDefaultBone(humanoid.avatarRig, ref bone.transform, boneId, "RightShoulder");
                }
            }

            public override void Init() {
                if (handTarget.humanoid == null || handTarget.humanoid.hipsTarget == null)
                    parent = null;
                else
                    parent = (handTarget.humanoid.hipsTarget.chest.bone.transform != null) ?
                        (TargetedBone)handTarget.humanoid.hipsTarget.chest :
                        (TargetedBone)handTarget.humanoid.hipsTarget.hips;

                nextBone = handTarget.upperArm;

                boneId = handTarget.isLeft ? Bone.LeftShoulder : Bone.RightShoulder;

                bone.maxAngle = maxShoulderAngle;
                if (handTarget.isLeft) {
                    bone.minAngles = minLeftShoulderAngles;
                    bone.maxAngles = maxLeftShoulderAngles;
                }
                else {
                    bone.minAngles = minRightShoulderAngles;
                    bone.maxAngles = maxRightShoulderAngles;
                }
            }

            public override Quaternion DetermineRotation() {
                Quaternion torsoRotation;
                if (handTarget.humanoid.hipsTarget.chest.bone.transform != null)
                    torsoRotation = Quaternion.LookRotation(handTarget.humanoid.hipsTarget.chest.bone.targetRotation * Vector3.forward, handTarget.humanoid.up);
                else
                    torsoRotation = Quaternion.LookRotation(handTarget.humanoid.hipsTarget.hips.bone.targetRotation * Vector3.forward, handTarget.humanoid.up);

                Vector3 shoulderOutwardDirection = handTarget.upperArm.bone.transform.position - bone.transform.position;

                Quaternion shoulderRotation = Quaternion.LookRotation(shoulderOutwardDirection, Vector3.up) * Quaternion.AngleAxis(handTarget.isLeft ? 90 : -90, Vector3.up);
                bone.baseRotation = Quaternion.Inverse(torsoRotation) * shoulderRotation;
                return shoulderRotation;
            }

            public override float GetTension() {
                if (parent == null)
                    return 0;

                Quaternion restRotation = parent.bone.targetRotation;
                float tension = GetTension(restRotation, this);
                return tension;
            }
        }

        #endregion

        #region UpperArm

        public TargetedUpperArmBone upperArm;

        [System.Serializable]
        public class TargetedUpperArmBone : TargetedBone {
            private HandTarget handTarget;

            public TargetedUpperArmBone(HandTarget handTarget) : base(handTarget.forearm) {
                this.handTarget = handTarget;
            }

            public override void RetrieveBones(HumanoidControl humanoid) {
                if (humanoid.targetsRig != null)
                    GetDefaultTargetBone(humanoid.targetsRig, ref target.transform, boneId);
                if (humanoid.avatarRig != null) {
                    if (handTarget.isLeft)
                        GetDefaultBone(humanoid.avatarRig, ref bone.transform, boneId, "LeftArm");
                    else
                        GetDefaultBone(humanoid.avatarRig, ref bone.transform, boneId, "RightArm");
                }
            }

            public override void Init() {
                parent = handTarget.shoulder;
                nextBone = handTarget.forearm;

                boneId = handTarget.isLeft ? Bone.LeftUpperArm : Bone.RightUpperArm;

                bone.maxAngle = maxUpperArmAngle;
                if (handTarget.isLeft) {
                    bone.minAngles = minLeftUpperArmAngles;
                    bone.maxAngles = maxLeftUpperArmAngles;
                }
                else {
                    bone.minAngles = minRightUpperArmAngles;
                    bone.maxAngles = maxRightUpperArmAngles;
                }
            }

            public override Quaternion DetermineRotation() {
                Vector3 upperArmBoneDirection = (handTarget.forearm.bone.transform.position - bone.transform.position).normalized;

                // Because we do upperarm before forearm, we need to ensure the forearm rotation is already calculated
                Quaternion forearmRotation = handTarget.forearm.DetermineRotation();
                Vector3 upperArmUp = ArmMovements.CalculateUpperArmUp(forearmRotation);

                Quaternion rotation = Quaternion.LookRotation(upperArmBoneDirection, upperArmUp);
                if (handTarget.isLeft)
                    return rotation * Quaternion.Euler(0, 90, 0);
                else
                    return rotation * Quaternion.Euler(0, -90, 0);
            }

            public override float GetTension() {
                Quaternion restRotation = handTarget.shoulder.bone.targetRotation * Quaternion.AngleAxis(handTarget.isLeft ? 45 : -45, Vector3.forward);
                float tension = GetTension(restRotation, this);
                return tension;
            }
        }

        #endregion

        #region Forearm
        public TargetedForearmBone forearm;

        [System.Serializable]
        public class TargetedForearmBone : TargetedBone {
            private HandTarget handTarget;

            public TargetedForearmBone(HandTarget handTarget) : base(handTarget.hand) {
                this.handTarget = handTarget;
            }

            public override void RetrieveBones(HumanoidControl humanoid) {
                if (humanoid.targetsRig != null)
                    GetDefaultTargetBone(humanoid.targetsRig, ref target.transform, boneId);
                if (humanoid.avatarRig != null) {
                    if (handTarget.isLeft)
                        GetDefaultBone(humanoid.avatarRig, ref bone.transform, boneId, "LeftForeArm");
                    else
                        GetDefaultBone(humanoid.avatarRig, ref bone.transform, boneId, "RightForeArm");
                }
            }

            public override void Init() {
                parent = handTarget.upperArm;
                nextBone = handTarget.hand;

                boneId = handTarget.isLeft ? Bone.LeftForearm : Bone.RightForearm;

                bone.maxAngle = maxForearmAngle;
                if (handTarget.isLeft) {
                    bone.minAngles = minLeftForearmAngles;
                    bone.maxAngles = maxLeftForearmAngles;
                }
                else {
                    bone.minAngles = minRightForearmAngles;
                    bone.maxAngles = maxRightForearmAngles;
                }
            }

            public override Quaternion DetermineRotation() {
                if (handTarget.hand.bone.transform == null)
                    return Quaternion.identity;

                Vector3 forearmBoneDirection = (handTarget.hand.bone.transform.position - bone.transform.position).normalized;
                Vector3 upperArmBoneDirection = (bone.transform.position - handTarget.upperArm.bone.transform.position).normalized;
                float angle = Vector3.Angle(upperArmBoneDirection, forearmBoneDirection);
                Vector3 rotationAxis = angle > 10 ? Vector3.Cross(upperArmBoneDirection, forearmBoneDirection) : handTarget.humanoid.up;
                if (angle > 10) {
                    if (handTarget.isLeft)
                        return Quaternion.LookRotation(forearmBoneDirection, rotationAxis) * Quaternion.Euler(0, 90, 0);
                    else
                        return Quaternion.LookRotation(forearmBoneDirection, -rotationAxis) * Quaternion.Euler(0, -90, 0);
                }
                else {
                    if (handTarget.isLeft)
                        return Quaternion.LookRotation(forearmBoneDirection, handTarget.humanoid.up) * Quaternion.Euler(0, 90, 0);
                    else
                        return Quaternion.LookRotation(forearmBoneDirection, handTarget.humanoid.up) * Quaternion.Euler(0, -90, 0);
                }
            }

            public override float GetTension() {
                if (handTarget.upperArm.bone.transform == null)
                    return 0;

                Quaternion restRotation = handTarget.upperArm.bone.targetRotation;
                float tension = GetTension(restRotation, this);
                return tension;
            }
        }
        #endregion

        #region Hand

        public TargetedHandBone hand;

        [System.Serializable]
        public class TargetedHandBone : TargetedBone {
            private HandTarget handTarget;

            public TargetedHandBone(HandTarget handTarget) {
                this.handTarget = handTarget;
            }

            public override void RetrieveBones(HumanoidControl humanoid) {
                if (humanoid.targetsRig != null)
                    GetDefaultTargetBone(humanoid.targetsRig, ref target.transform, boneId);
                if (humanoid.avatarRig != null) {
                    if (handTarget.isLeft)
                        GetDefaultBone(humanoid.avatarRig, ref bone.transform, boneId, "LeftHand");
                    else
                        GetDefaultBone(humanoid.avatarRig, ref bone.transform, boneId, "RightHand");
                }
            }

            public override void Init() {
                parent = handTarget.forearm;

                boneId = handTarget.isLeft ? Bone.LeftHand : Bone.RightHand;

                bone.maxAngle = maxHandAngle;
                if (handTarget.isLeft) {
                    bone.minAngles = minLeftHandAngles;
                    bone.maxAngles = maxLeftHandAngles;
                }
                else {
                    bone.minAngles = minRightHandAngles;
                    bone.maxAngles = maxRightHandAngles;
                }
            }

            public override Quaternion DetermineRotation() {
                Vector3 outward = handTarget.HandBoneOutwardAxis();
                Vector3 right = handTarget.HandBoneRightAxis();
                Vector3 up = Vector3.Cross(outward, right);

                if (handTarget.isLeft)
                    return Quaternion.LookRotation(outward, up) * Quaternion.Euler(0, 90, 0);
                else
                    return Quaternion.LookRotation(outward, up) * Quaternion.Euler(0, -90, 0);
            }

            public override float GetTension() {
                if (handTarget.forearm.bone.transform == null)
                    return 0;

                Quaternion restRotation = handTarget.forearm.bone.targetRotation;
                float tension = GetTension(restRotation, this);
                return tension;
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
                return handTarget.humanoid.headTarget.neck.target.transform;
            }

            public override void CalculateVelocity() {
                if (handTarget.grabSocket == null)
                    return;

                if (lastTime > 0 && lastRotation != Quaternion.identity && Time.time > lastTime) {
                    float deltaTime = Time.time - lastTime;

                    Vector3 newVelocity = (handTarget.grabSocket.transform.position - lastPosition) / deltaTime;
                    // velocity = newVelocity
                    // velocity smoothing
                    velocity = velocity * 0.5F + (newVelocity * 0.5F);

                    Quaternion deltaRotation = handTarget.grabSocket.transform.rotation * Quaternion.Inverse(lastRotation);
                    float angle;
                    Vector3 axis;
                    deltaRotation.ToAngleAxis(out angle, out axis);
                    angle *= Mathf.Deg2Rad;
                    angularVelocity = angle * axis / Time.deltaTime;

                }

                lastTime = Time.time;
                lastPosition = handTarget.grabSocket.transform.position;
                lastRotation = handTarget.grabSocket.transform.rotation;
            }
        }

        #endregion

        private void InitSubTargets() {
            //foreach (TargetedBone subTarget in subTargets)
            //    subTarget.Init();
            shoulder.Init();
            upperArm.Init();
            forearm.Init();
            hand.Init();
        }

        private void SetTargetPositionsToAvatar() {
            hand.SetTargetPositionToAvatar();
            forearm.SetTargetPositionToAvatar();
            upperArm.SetTargetPositionToAvatar();
            shoulder.SetTargetPositionToAvatar();
        }

        private void DoMeasurements() {
            hand.DoMeasurements();
            forearm.DoMeasurements();
            upperArm.DoMeasurements();
            shoulder.DoMeasurements();
        }

        public override Transform GetDefaultTarget(HumanoidControl humanoid) {
            Transform targetTransform = null;
            if (humanoid != null)
                GetDefaultBone(humanoid.targetsRig, ref targetTransform, isLeft ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            return targetTransform;
        }

        // Do not remove this, this is dynamically called from Target_Editor!
        public static HandTarget CreateTarget(HandTarget oldTarget) {
            HumanoidControl humanoid = oldTarget.humanoid;

            GameObject targetObject = new GameObject();
            if (oldTarget.isLeft)
                targetObject.name = "Left Hand Target";
            else
                targetObject.name = "Right Hand Target";
            Transform targetTransform = targetObject.transform;

            targetTransform.parent = humanoid.transform;
            targetTransform.position = oldTarget.transform.position;
            targetTransform.rotation = oldTarget.transform.rotation;

            HandTarget handTarget = Constructor(humanoid, oldTarget.isLeft, targetTransform);
            if (oldTarget.isLeft) {
                humanoid.leftHandTarget = handTarget;
                //handTarget.otherHand = humanoid.rightHandTarget;
            }
            else {
                humanoid.rightHandTarget = handTarget;
                //handTarget.otherHand = humanoid.leftHandTarget;
            }

            handTarget.RetrieveBones();
            handTarget.InitAvatar();
            handTarget.MatchTargetsToAvatar();

            return handTarget;
        }

        // Do not remove this, this is dynamically called from Target_Editor!
        // Changes the target transform used for this head target
        // Generates a new headtarget component, so parameters will be lost if transform is changed
        public static HandTarget SetTarget(HumanoidControl humanoid, Transform targetTransform, bool isLeft) {
            HandTarget currentHandTarget = isLeft ? humanoid.leftHandTarget : humanoid.rightHandTarget;
            if (targetTransform == currentHandTarget.transform)
                return currentHandTarget;

            GetDefaultBone(humanoid.targetsRig, ref targetTransform, isLeft ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            if (targetTransform == null)
                return currentHandTarget;

            HandTarget handTarget = targetTransform.GetComponent<HandTarget>();
            if (handTarget == null)
                handTarget = targetTransform.gameObject.AddComponent<HandTarget>();

            if (isLeft)
                humanoid.leftHandTarget = handTarget;
            else
                humanoid.rightHandTarget = handTarget;

            handTarget.NewComponent(humanoid);

            return handTarget;
        }

        public TargetedBone GetTargetBone(ArmBones boneID) {
            switch (boneID) {
                case ArmBones.Hand:
                    return hand;
                case ArmBones.Forearm:
                    return forearm;
                case ArmBones.UpperArm:
                    return upperArm;
                case ArmBones.Shoulder:
                    return shoulder;
                default:
                    return null;
            }
        }

        #endregion

        #region Configuration

        public static void ClearBones(HandTarget handTarget) {
            handTarget.handMovements.ReattachHand();
            handTarget.shoulder.bone.transform = null;
            handTarget.upperArm.bone.transform = null;
            handTarget.forearm.bone.transform = null;
            handTarget.hand.bone.transform = null;
            handTarget.ClearHandBones();
        }

        private void ClearHandBones() {
            fingers.thumb.proximal.bone.transform = null;
            fingers.index.proximal.bone.transform = null;
            fingers.middle.proximal.bone.transform = null;
            fingers.ring.proximal.bone.transform = null;
            fingers.little.proximal.bone.transform = null;
        }

        public void RetrieveBones() {
            foreach (TargetedBone subTarget in subTargets)
                subTarget.RetrieveBones(humanoid);

            fingers.RetrieveBones(this);
        }

        #endregion

        #region Settings
        //public bool jointLimitations = true;

        public enum PoseMethod {
            Position,
            Rotation
        }
        public PoseMethod poseMethod;

        //public override bool showRealObjects {
        //    get { return base.showRealObjects; }
        //    set {
        //        if (value != base.showRealObjects) {
        //            base.showRealObjects = value;
        //            ShowSensors(value);
        //        }
        //    }
        //}

        /// <summary>
        /// Enables hand physics
        /// </summary>
        public bool physics = true;
        public AdvancedHandPhysics.PhysicsMode physicsMode = AdvancedHandPhysics.PhysicsMode.HybridKinematic;
        /// <summary>
        /// The strength of the arm when holding physics objects
        /// </summary>
        public float strength = 100;
        /* Replaced by grabbingType
        /// <summary>
        /// Enables grabbing of objects
        /// </summary>
        public bool grabbing = true;
        */
        /// <summary>
        /// Grabbing techniques
        /// </summary>
        public enum GrabbingTechnique {
            TouchGrabbing,  ///< try to grab the object when you touch it and you close the hand
            NearGrabbing,   ///< try to grab an object near to your hand when you close the hand
            NoGrabbing,     ///< do not try to grab objects when you close the hand
        }
        /// <summary>
        /// The GrabbingType used to grab objects
        /// </summary>
        public GrabbingTechnique grabbingTechnique = GrabbingTechnique.NearGrabbing;
        /// <summary>
        /// The distance where the hand is near an objecty
        /// </summary>
        public float nearDistance = 0.1F;
        /// <summary>
        /// Enables touch interaction
        /// </summary>
        public bool touchInteraction = true;

        #endregion

        #region Poses

        public Pose grabPose;
        public Pose closedPose;

        public PoseMixer poseMixer = new PoseMixer();
        public void SetPose1(Pose pose) {
            poseMixer.SetPoseValue(pose, 1);
        }
        public void SetPose(Pose pose, float weight) {
            poseMixer.SetPoseValue(pose, weight);
        }

        #endregion

        #region Interaction

        /// <summary>
        /// The Socket for holding objects with the whole hand
        /// </summary>
        public HandSocket grabSocket;
        /// <summary>
        /// The Socket for holding objects between the thumb and index finger
        /// </summary>
        public Socket pinchSocket;

        [System.NonSerialized]
        public InteractionModule inputModule;

        [System.NonSerialized]
        public GameObject touchedObject = null;
        public GameObject grabbedPrefab;
        public GameObject grabbedObject;
        public Handle grabbedHandle = null;
        public Vector3 targetToHandle;

        public bool grabbedRigidbody;
        public bool grabbedKinematicRigidbody;
        public List<Collider> colliders;

        public bool twoHandedGrab = false;
        public Vector3 targetToSecondaryHandle;

        public bool GrabbedStaticObject() {
            if (grabbedObject != null && grabbedRigidbody && grabbedKinematicRigidbody)
                return true;

            return (grabbedObject != null && !grabbedRigidbody);
        }

        public static void TmpDisableCollisions(HandTarget handTarget, float duration) {
            handTarget.StartCoroutine(TmpDisableCollisions(handTarget.hand.bone.transform.gameObject, duration));
        }

        private static IEnumerator TmpDisableCollisions(GameObject handObj, float duration) {
            HandMovements.SetAllColliders(handObj, false);
            yield return new WaitForSeconds(duration);
            HandMovements.SetAllColliders(handObj, true);
        }

        #endregion

        #region Events

        public PoseEventList poseEvent = new PoseEventList() {
            id = 1,
            label = "Pose Event",
            tooltip =
                "Call functions based on recognized poses" +
                "Parameter: the recognized pose",
            eventTypeLabels = new string[] {
                "Never",
                "On Pose Recognized",
                "On No Pose Recongnized",
                "While Pose Recognized",
                "While No Pose Recognized",
                "While Pose Changes",
                "Always",
            },
            fromEventLabel = "poseMixer.detectedPose",
        };
        public GameObjectEventHandlers touchEvent = new GameObjectEventHandlers() {
            id = 2,
            label = "Touch Event",
            tooltip =
                "Call funtions based on touched objects" +
                "Parameter: the touched object",
            eventTypeLabels = new string[] {
                "Never",
                "On Touch Start",
                "On Touch End",
                "While Touching",
                "While not Touching",
                "On Touched Object Changes",
                "Always",
            },
            fromEventLabel = "touchedObject",
        };
        public GameObjectEventHandlers grabEvent = new GameObjectEventHandlers() {
            id = 3,
            label = "Grab Event",
            tooltip =
                "Call functions based on grabbed objects" +
                "Parameter: the grabbed object",
            eventTypeLabels = new string[] {
                "Never",
                "On Grab Start",
                "On Grab End",
                "While Holding Object",
                "While not Holding Object",
                "On Grabbed Object Changes",
                "Always",
            },
            fromEventLabel = "grabbedObject",
        };

        protected virtual void UpdateEvents() {
            poseEvent.value = poseMixer.detectedPose;
            touchEvent.value = touchedObject;
            grabEvent.value = grabbedObject;
        }

        #endregion

        public Transform handPalm;
        public Rigidbody handRigidbody;

        public AdvancedHandPhysics handPhysics;

        public HandMovements handMovements = new HandMovements();
        public ArmMovements armMovements = new ArmMovements();

        public HandTarget otherHand {
            get {
                return isLeft ? humanoid.rightHandTarget : humanoid.leftHandTarget;
            }
        }

        private Vector3 _localPalmPosition = Vector3.zero;
        public Vector3 localPalmPosition {
            get {
                if (_localPalmPosition == Vector3.zero)
                    CalculatePalm();
                return _localPalmPosition;
            }
        }
        private Quaternion localPalmRotation = Quaternion.identity;
        private void CalculatePalm() {
            Transform indexFingerBone = fingers.index.proximal.bone.transform;
            Transform middleFingerBone = fingers.middle.proximal.bone.transform;

            // Determine position
            if (indexFingerBone)
                _localPalmPosition = (indexFingerBone.position - hand.bone.transform.position) * 0.9F + new Vector3(0, 0, 0);
            else if (middleFingerBone)
                _localPalmPosition = (middleFingerBone.position - hand.bone.transform.position) * 0.9F + new Vector3(0, 0, 0);
            else
                _localPalmPosition = new Vector3(0.1F, 0, 0);

            Vector3 handPalmPosition = hand.bone.transform.position + _localPalmPosition;

            Vector3 handUp = hand.bone.targetRotation * Vector3.up;
            Vector3 handForward = Vector3.zero;

            if (indexFingerBone)
                handForward = indexFingerBone.position - handPalmPosition;
            else if (middleFingerBone)
                handForward = middleFingerBone.position - handPalmPosition;
            else if (isLeft)
                handForward = -humanoid.avatarRig.transform.right;
            else
                handForward = humanoid.avatarRig.transform.right;

            Quaternion worldPalmRotation = Quaternion.LookRotation(handForward, handUp);
            localPalmRotation = Quaternion.Inverse(hand.target.transform.rotation) * worldPalmRotation;
            _localPalmPosition = Quaternion.Inverse(hand.target.transform.rotation) * _localPalmPosition;

            // Now get it in the palm
            if (isLeft) {
                localPalmRotation *= Quaternion.Euler(0, -45, -90);
                _localPalmPosition += localPalmRotation * new Vector3(0.02F, -0.04F, 0);
            }
            else {
                localPalmRotation *= Quaternion.Euler(0, 45, 90);
                _localPalmPosition += localPalmRotation * new Vector3(-0.02F, -0.04F, 0);
            }
        }

        public Vector3 palmPosition {
            get {
                //if (localPalmPosition.sqrMagnitude == 0)
                //    CalculatePalm();

                Vector3 handPalmPosition = hand.bone.transform.position + hand.bone.targetRotation * localPalmPosition;
                return handPalmPosition;
            }
        }
        public Quaternion palmRotation {
            get {
                if (localPalmPosition.sqrMagnitude == 0)
                    CalculatePalm();

                Quaternion handPalmRotation = hand.bone.targetRotation * localPalmRotation;
                return handPalmRotation;
            }
        }


        // index<->little
        public Vector3 HandBoneRightAxis() {
            if (fingers.index.proximal.bone.transform == null || fingers.little.proximal.bone.transform == null)
                return isLeft ? Vector3.forward : Vector3.back;

            Transform indexFingerBone = fingers.index.proximal.bone.transform;
            Transform littleFingerBone = fingers.little.proximal.bone.transform;

            if (indexFingerBone == null || littleFingerBone == null)
                return Vector3.zero;

            Vector3 fingersDirection;
            if (isLeft)
                fingersDirection = (indexFingerBone.position - littleFingerBone.position).normalized;
            else
                fingersDirection = (littleFingerBone.position - indexFingerBone.position).normalized;

            return fingersDirection;//humanoid.transform.InverseTransformDirection(fingersDirection);
        }

        public Vector3 HandBoneOutwardAxis() {
            Transform fingerBone = null;
            //if (fingers.middleFinger != null && fingers.middleFinger.bones.Length > 0 && fingers.middleFinger.bones[0] != null)
            if (fingers.middle.proximal.bone.transform != null)
                fingerBone = fingers.middle.proximal.bone.transform; // middleFinger.bones[0];
            //else if (fingers.indexFinger != null && fingers.indexFinger.bones.Length > 0 && fingers.indexFinger.bones[0] != null)
            else if (fingers.index.proximal.bone.transform != null)
                fingerBone = fingers.index.proximal.bone.transform; // fingers.indexFinger.bones[0];

            if (fingerBone == null)
                return Vector3.forward;

            Vector3 outward = (fingerBone.position - hand.bone.transform.position).normalized;
            return outward;
        }

        #region Init

        public static bool IsInitialized(HumanoidControl humanoid) {
            if (humanoid.leftHandTarget == null || humanoid.leftHandTarget.humanoid == null)
                return false;
            if (humanoid.leftHandTarget.hand.target.transform == null || humanoid.rightHandTarget.hand.target.transform == null)
                return false;
            if (humanoid.rightHandTarget == null || humanoid.rightHandTarget.humanoid == null)
                return false;
            return true;
        }

        private void Reset() {
            humanoid = GetHumanoid();
            if (humanoid == null)
                return;

            //poses.poses = null;
            NewComponent(humanoid);

            shoulder.bone.maxAngle = maxShoulderAngle;
            upperArm.bone.maxAngle = maxUpperArmAngle;
            forearm.bone.maxAngle = maxForearmAngle;
            hand.bone.maxAngle = maxHandAngle;
        }

        private HumanoidControl GetHumanoid() {
            // This does not work for prefabs
            HumanoidControl[] humanoids = FindObjectsOfType<HumanoidControl>();

            for (int i = 0; i < humanoids.Length; i++) {
                if ((humanoids[i].leftHandTarget != null && humanoids[i].leftHandTarget.transform == this.transform) ||
                    (humanoids[i].rightHandTarget != null && humanoids[i].rightHandTarget.transform == this.transform)) {

                    return humanoids[i];
                }
            }

            return null;
        }

        public void InitTarget() {
            InitSensors();
            CheckRigidbody();
            if (grabSocket == null)
                grabSocket = CreateGrabSocket();
            if (pinchSocket == null)
                pinchSocket = CreatePinchSocket();
        }

        protected void CheckRigidbody() {
            if (handRigidbody == null) {
                if (hand.bone.transform == null)
                    return;

                handRigidbody = hand.bone.transform.GetComponent<Rigidbody>();
                if (handRigidbody == null)
                    handRigidbody = hand.bone.transform.gameObject.AddComponent<Rigidbody>();
            }
            handRigidbody.mass = 1;
            handRigidbody.drag = 0;
            handRigidbody.angularDrag = 10;
            handRigidbody.useGravity = false;
            handRigidbody.isKinematic = true;
            handRigidbody.interpolation = RigidbodyInterpolation.None;
#if UNITY_2018_3_OR_NEWER
            handRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
#else
            handRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
#endif
            if (handPalm != null)
                handRigidbody.centerOfMass = handPalm.position - hand.bone.transform.position;

        }

        public override void InitAvatar() {
            InitSubTargets();

            shoulder.DoMeasurements();
            upperArm.DoMeasurements();
            forearm.DoMeasurements();
            hand.DoMeasurements();

            fingers.InitAvatar();

            DeterminePalmPosition();
            if (Application.isPlaying) {
                HandMovements.DetachHand(this);
            }
        }

        // This function is called only when the humanoid is created
        private static HandTarget Constructor(HumanoidControl humanoid, bool isLeft, Transform handTargetTransform) {
            HandTarget handTarget = handTargetTransform.gameObject.AddComponent<HandTarget>();
            handTarget.humanoid = humanoid;
            handTarget.isLeft = isLeft;
            handTarget.side = isLeft ? Side.Left : Side.Right;
            handTarget.outward = handTarget.isLeft ? Vector3.left : Vector3.right;

            handTarget.InitSubTargets();
            return handTarget;
        }

        public override void NewComponent(HumanoidControl _humanoid) {
            humanoid = _humanoid;
            isLeft = (this == humanoid.leftHandTarget);
            if (isLeft)
                outward = Vector3.left;
            else
                outward = Vector3.right;

            fingers.NewComponent(this);

            if (hand == null)
                hand = new TargetedHandBone(this);
            if (forearm == null)
                forearm = new TargetedForearmBone(this);
            if (upperArm == null)
                upperArm = new TargetedUpperArmBone(this);

            //otherHand = isLeft ? humanoid.rightHandTarget : humanoid.leftHandTarget;

            InitComponent();
        }

        // This function is called every time the avatar is changed
        public override void InitComponent() {
            if (humanoid == null)
                return;

            //bones = new TargetedBone[] { hand, forearm, upperArm, shoulder };
            //bonesReverse = new TargetedBone[] { hand, forearm, upperArm, shoulder };

            InitSubTargets();
            //foreach (TargetedBone bone in bones)
            //    bone.Init(this);

            //RetrieveBones();
#if LIB
            arm = new HumanoidMovements.Arm();
            RetrieveBoneTransforms();            
            //arm.Init();
#endif
            DeterminePalmPosition();

            // We need to do this before the measurements
            //foreach (TargetedBone bone in bones)
            //    bone.SetTargetPositionToAvatar();
            SetTargetPositionsToAvatar();
            //foreach (TargetedBone bone in bones)
            //    bone.DoMeasurements();
            DoMeasurements();

            if (stretchlessTarget == null && hand.target.transform != null) {
                stretchlessTarget = hand.target.transform.Find("Stretchless Target");
                if (stretchlessTarget == null) {

                    GameObject stretchlessTargetObj = new GameObject("Stretchless Target");
                    stretchlessTarget = stretchlessTargetObj.transform;
                    stretchlessTarget.parent = hand.target.transform;
                    stretchlessTarget.localPosition = Vector3.zero;
                    stretchlessTarget.localRotation = Quaternion.identity;
                }
            }

            //poses.InitPoses(fingers);

        }

        public override void StartTarget() {
            side = isLeft ? Side.Left : Side.Right;

            InitSensors();

            if (grabSocket == null)
                grabSocket = CreateGrabSocket();
            if (pinchSocket == null)
                pinchSocket = CreatePinchSocket();

            //RetrieveBones();

            //#if LIB
            //            arm = new HumanoidMovements.Arm();
            //            RetrieveBoneTransforms();
            //            arm.Init();
            //#endif
            //fingers.CalculateFingerRetargeting();

            CheckColliders();
#if hVRTK
            // VRTK is not compatible with touch interaction
            // because it uses its own EventSystem
            // which will be destroyed by humanoid control
            if (touchInteraction && !(humanoid.vrtk.enabled && vrtk.enabled))
#else
            if (touchInteraction)
#endif
                StartInteraction();


            if (humanoid.avatarRig != null) {
                Vector3 handRightAxis = HandBoneRightAxis();
                Vector3 handOutwardAxis = HandBoneOutwardAxis();
                up = Vector3.Cross(handOutwardAxis, handRightAxis);
            }

            handMovements.Start(humanoid, this);

            if (humanoid.physics && physics && hand.bone.transform != null)
                handPhysics = hand.bone.transform.GetComponent<AdvancedHandPhysics>();
        }

        /// <summary>
        /// Checks whether the humanoid has an HandTarget
        /// and adds one if none has been found
        /// </summary>
        /// <param name="humanoid">The humanoid to check</param>
        /// <param name="isLeft">Is this the left hand?</param>
        public static void DetermineTarget(HumanoidControl humanoid, bool isLeft) {
            HandTarget handTarget = isLeft ? humanoid.leftHandTarget : humanoid.rightHandTarget;

            if (handTarget == null) {
                Transform handTargetTransform = humanoid.targetsRig.GetBoneTransform(isLeft ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
                if (handTargetTransform == null) {
                    Debug.LogError("Could not find hand bone in targets rig");
                    return;
                }

                handTarget = handTargetTransform.GetComponent<HandTarget>();
                if (handTarget == null)
                    handTarget = Constructor(humanoid, isLeft, handTargetTransform);
            }

            if (isLeft)
                humanoid.leftHandTarget = handTarget;
            else
                humanoid.rightHandTarget = handTarget;
        }

#if LIB
        private void RetrieveBoneTransforms() {
            arm.SetBonePosition(ArmBones.Shoulder, shoulder.bone.transform.position);
            arm.SetBoneRotation(ArmBones.Shoulder, shoulder.bone.transform.rotation);

            arm.SetBonePosition(ArmBones.UpperArm, upperArm.bone.transform.position);
            arm.SetBoneRotation(ArmBones.UpperArm, upperArm.bone.transform.rotation);

            arm.SetBonePosition(ArmBones.Forearm, forearm.bone.transform.position);
            arm.SetBoneRotation(ArmBones.Forearm, forearm.bone.transform.rotation);

            arm.SetBonePosition(ArmBones.Hand, hand.bone.transform.position);
            arm.SetBoneRotation(ArmBones.Hand, hand.bone.transform.rotation);
        }
#endif
        public override void MatchTargetsToAvatar() {
            if (shoulder != null)
                shoulder.MatchTargetToAvatar();
            if (upperArm != null)
                upperArm.MatchTargetToAvatar();
            if (forearm != null)
                forearm.MatchTargetToAvatar();
            MatchHandTargetToAvatar();
#if LIB
            if (shoulder != null)
                CopyBoneToTarget(ArmBones.Shoulder, shoulder.bone);
            arm.SetTargetRotation(ArmBones.Shoulder, shoulder.target.transform.rotation);
            if (upperArm != null)
                CopyBoneToTarget(ArmBones.UpperArm, upperArm.bone);
            if (forearm != null)
                CopyBoneToTarget(ArmBones.Forearm, forearm.bone);
            if (hand != null)
                CopyBoneToTarget(ArmBones.Hand, hand.bone);

            Debug.Log(hand.target.transform.eulerAngles + " " + arm.GetTargetRotation(ArmBones.Shoulder).eulerAngles);
            arm.Init();
#endif

            fingers.MatchTargetsToAvatar();
        }
#if LIB
        public void CopyBoneToTarget(ArmBones boneId, BoneTransform bone) {
            if (bone.transform != null) {
                arm.SetTargetPosition(boneId, bone.transform.position);
                Debug.Log(bone.targetRotation.eulerAngles);
                arm.SetTargetRotation(boneId, bone.targetRotation);
            }
        }
#endif

        //private void MatchHandTargetToAvatar() {
        //    if (hand == null)
        //        return;

        //    //hand.DoMeasurements();
        //    if (hand.bone.transform != null) {
        //        transform.position = hand.bone.transform.position;
        //        transform.rotation = hand.bone.targetRotation;
        //        if (hand.target.transform != null) {
        //            hand.target.transform.position = transform.position;
        //            hand.target.transform.rotation = transform.rotation;
        //        }
        //    }
        //}

        private void MatchHandTargetToAvatar() {
            if (hand == null)
                return;

            //hand.DoMeasurements();
            if (hand.bone.transform == null)
                return;

            if (!Application.isPlaying) {
                float targetDistance = Vector3.Distance(hand.bone.transform.position, hand.target.transform.position);
                if (targetDistance > 0.001F)
                    hand.target.transform.position = hand.bone.transform.position;

                float targetAngle = Quaternion.Angle(hand.bone.targetRotation, hand.target.transform.rotation);
                if (targetAngle > 0.1F)
                    hand.target.transform.rotation = hand.bone.targetRotation;
            }
            else {
                transform.position = hand.bone.transform.position;
                transform.rotation = hand.bone.targetRotation;
            }

            if (hand.target.transform != null) {
                if (!Application.isPlaying) {
                    float targetDistance = Vector3.Distance(transform.position, hand.target.transform.position);
                    if (targetDistance > 0.001F)
                        hand.target.transform.position = transform.position;
                }
                else
                    hand.target.transform.position = transform.position;
                hand.target.transform.rotation = transform.rotation;

            }

            //if (hand.target.transform != null) {
            //    if (!Application.isPlaying) {
            //        float targetDistance = Vector3.Distance(transform.position, hand.target.transform.position);
            //        if (targetDistance > 0.001F)
            //            hand.target.transform.position = transform.position;
            //    }
            //    else
            //        hand.target.transform.position = transform.position;
            //    hand.target.transform.rotation = transform.rotation;
            //}
        }

        private void MatchFingersToAvatar() {

        }
        #endregion

        #region Update

        public bool grabbedChanged;

        public override void UpdateTarget() {
            GameObject lastGrabbedObject = grabbedObject;

            // handRigidbody needs to be there to do proper grabbing
            if (grabbingTechnique == GrabbingTechnique.NearGrabbing && handRigidbody != null && !otherHand.grabbedChanged)
                NearCheck();

            grabbedChanged = (lastGrabbedObject != grabbedObject);

            hand.target.confidence.Degrade();
            forearm.target.confidence.Degrade();
            upperArm.target.confidence.Degrade();
            shoulder.target.confidence.Degrade();

            UpdateSensors();

            // Letting go leap does not work yet because curl values depend on
            // bone rotations/
            // Set curl values from LeapHand?
            // This gives problems with the remote humanoid thumb movements not being correct
            //FingersTarget.UpdateTargetCurlValues(this);

            if (!grabbedChanged && !otherHand.grabbedChanged)
                CheckLetGo();

            grabbedChanged |= (lastGrabbedObject != grabbedObject);

            // Override the hand pose when it is set
            poseMixer.ShowPose(humanoid, isLeft ? Side.Left : Side.Right);
#if LIB
            arm.SetTargets(this);
#endif
            hand.CalculateVelocity();

            UpdateEvents();
        }

        protected void UpdateUsingTrackedRigidbody(TrackedRigidbody trackedRigidbody) {
            Debug.Log("using trackedRigidbody");
            hand.target.transform.position = trackedRigidbody.transform.position;
            hand.target.transform.rotation = trackedRigidbody.transform.rotation;
        }

        protected void UpdateTwoHanded2() {
            if (!twoHandedGrab)
                return;

            Vector3 primaryHandlePosition = hand.target.transform.TransformPoint(targetToHandle);
            Vector3 secondaryHandlePosition = hand.target.transform.TransformPoint(targetToSecondaryHandle);

            Vector3 secondarySocketPosition = otherHand.hand.target.transform.TransformPoint(otherHand.targetToHandle);

            Vector3 toSecondaryHandle = secondaryHandlePosition - primaryHandlePosition;
            Vector3 toSecondarySocket = secondarySocketPosition - primaryHandlePosition;

            Quaternion rotation = Quaternion.FromToRotation(toSecondaryHandle, toSecondarySocket);
            hand.target.transform.rotation = rotation * hand.target.transform.rotation;

            // Check secondary arm stretching
            Quaternion otherForeArmRotation = otherHand.forearm.bone.targetRotation;
            Vector3 nostretchPosition = otherHand.forearm.bone.transform.position + otherForeArmRotation * otherHand.outward * otherHand.forearm.bone.length;
            Vector3 delta = nostretchPosition - otherHand.hand.bone.transform.position;

            hand.target.transform.position += delta;

            // This decreases the secondary arm stretch, but when the right hand is fully stretched too, it does not work
            // because the stretchtarget (which I want to get rig of anyway) does not take this delta into account.
        }


        protected Vector3 Orthagonal(Transform primaryTransform, Transform secondaryTransform) {
            Vector3 forwardDirection = secondaryTransform.position - primaryTransform.position;
            Vector3 upDirection = primaryTransform.up;
            float angle = Vector3.Angle(forwardDirection, upDirection);
            if (angle == 0)
                upDirection = primaryTransform.forward;
            Debug.DrawRay(primaryTransform.position, forwardDirection);
            Vector3 orthagonal = Vector3.Cross(upDirection, forwardDirection);
            return orthagonal;
        }

        public override void UpdateMovements(HumanoidControl humanoid) {
            if (humanoid == null || !humanoid.calculateBodyPose)
                return;

            UpdateTwoHanded2();

            ArmMovements.Update(this);
            HandMovements.Update(this);
            FingerMovements.Update(this);
#if LIB
                if (arm != null) {
                    arm.UpdateMovements();
                    arm.CopyToRig(this);
                }
#endif
        }

        [HideInInspector]
        public bool directFingerMovements = true;
        public override void CopyTargetToRig() {
            if (humanoid == null)
                return;

            if (Application.isPlaying &&
                humanoid.animatorEnabled && humanoid.targetsRig.runtimeAnimatorController != null)
                return;

            if (directFingerMovements)
                FingersTarget.CopyFingerTargetsToRig(this);

            if (hand.target.transform == null || transform == hand.target.transform)
                return;

            hand.target.transform.position = transform.position;
            hand.target.transform.rotation = transform.rotation;
#if LIB
            arm.SetTargets(this);
#endif
        }

        public override void CopyRigToTarget() {

            if (hand.target.transform == null || transform == hand.target.transform)
                return;

            if (!Application.isPlaying && hand.bone.transform != null) {
                float targetDistance = Vector3.Distance(hand.bone.transform.position, hand.target.transform.position);
                if (targetDistance < 0.001F)
                    return;
            }

            if (hand.target.transform != null && transform != hand.target.transform) {
                transform.position = hand.target.transform.position;
                transform.rotation = hand.target.transform.rotation;
            }

            //FingersTarget.CopyRigToFingerTargets(this);
            //pose = HandPoses.DetermineHandPose(fingers, out poseConfidence);

            // Wierd place for this, but it needs the finger subtargets to work
            if (humanoid != null && humanoid.avatarRig != null) {
                FingerMovements.Update(this);
            }
        }

        public void UpdateSensorsFromTarget() {
            if (sensors == null)
                return;

            for (int i = 0; i < sensors.Length; i++)
                sensors[i].UpdateSensorTransformFromTarget(this.transform);
        }

        public float GetFingerCurl(Finger fingerID) {
            return fingers.GetFingerCurl(fingerID);
        }
        public float GetFingerCurl(FingersTarget.TargetedFinger finger) {
            return finger.CalculateCurl();
        }

        public void AddFingerCurl(Finger fingerID, float curlValue) {
            fingers.AddFingerCurl(fingerID, curlValue);
        }

        public void SetFingerCurl(Finger fingerID, float curlValue) {
            fingers.SetFingerCurl(fingerID, curlValue);
        }

        public void SetFingerGroupCurl(FingersTarget.FingerGroup fingerGroupID, float curlValue) {
            fingers.SetFingerGroupCurl(fingerGroupID, curlValue);
        }

        public void DetermineFingerCurl(Finger fingerID) {
            fingers.DetermineFingerCurl(fingerID);
        }

        public float HandCurl() {

            float middleCurl = fingers.middle.CalculateCurl();
            float ringCurl = fingers.ring.CalculateCurl();
            float littleCurl = fingers.little.CalculateCurl();

            // Leave out index finger to prevent interference with pinching
            //float indexCurl = fingers.index.CalculateCurl();
            //return indexCurl + middleCurl + ringCurl + littleCurl;

            return (middleCurl + ringCurl + littleCurl) / 3 * 4;
        }

        #endregion

        #region DrawRigs

        protected override void DrawTargetRig(HumanoidControl humanoid) {
            if (this != humanoid.leftHandTarget && this != humanoid.rightHandTarget)
                return;

            if (shoulder != null)
                DrawTargetBone(shoulder, outward);
            if (upperArm != null)
                DrawTargetBone(upperArm, outward);
            if (forearm != null)
                DrawTargetBone(forearm, outward);
            if (hand != null)
                DrawTargetBone(hand, outward);

            fingers.DrawTargetRig(this);
        }

        protected override void DrawAvatarRig(HumanoidControl humanoid) {
            if (this != humanoid.leftHandTarget && this != humanoid.rightHandTarget)
                return;

            if (shoulder != null)
                DrawAvatarBone(shoulder, outward);
            if (upperArm != null)
                DrawAvatarBone(upperArm, outward);
            if (forearm != null)
                DrawAvatarBone(forearm, outward);
            if (hand != null)
                DrawAvatarBone(hand, outward);

            fingers.DrawAvatarRig(this);
        }

        #endregion

        #region Colliders

        private void CheckColliders() {
            if (hand.bone.transform == null)
                return;

            if (hand.bone.transform != null) {
                Collider c = hand.bone.transform.GetComponent<Collider>();
                // Does not work if the hand has grabbed an object with colliders...
                if (c == null)
                    GenerateColliders();
            }

        }

        // assumes hand scale is uniform!
        protected virtual void GenerateColliders() {
            float unscale = 1 / hand.bone.transform.lossyScale.x;

            if (fingers.middle.proximal.bone.transform == null)
                return;

            BoxCollider hc = hand.bone.transform.gameObject.AddComponent<BoxCollider>();
            hc.center = hand.bone.toTargetRotation * (isLeft ? new Vector3(-0.05F * unscale, 0, 0) : new Vector3(0.05F * unscale, 0, 0));
            Vector3 hcSize = hand.bone.toTargetRotation * new Vector3(0.1F * unscale, 0.03F * unscale, 0.05F * unscale);

            hc.size = new Vector3(Mathf.Abs(hcSize.x), Mathf.Abs(hcSize.y), Mathf.Abs(hcSize.z));


            // TO DO: thumb
            for (int i = 1; i < 5; i++) {
                FingersTarget.TargetedFinger finger = fingers.allFingers[i];

                Transform proximal = finger.proximal.bone.transform;
                if (proximal == null)
                    continue;

                Transform intermediate = finger.intermediate.bone.transform;
                Transform distal = finger.distal.bone.transform;

                if (intermediate != null) {
                    Vector3 localIntermediatePosition = proximal.InverseTransformPoint(intermediate.position);
                    //Quaternion fingerRotation = Quaternion.FromToRotation(localIntermediatePosition, Vector3.forward);
                    float proximalLength = Vector3.Distance(proximal.position, intermediate.position);

                    GameObject proximalColliderObj = new GameObject("Proximal Collider");
                    proximalColliderObj.tag = this.gameObject.tag;
                    proximalColliderObj.layer = this.gameObject.layer;
                    proximalColliderObj.transform.parent = proximal;
                    proximalColliderObj.transform.localPosition = localIntermediatePosition / 2;
                    proximalColliderObj.transform.localRotation = Quaternion.LookRotation(localIntermediatePosition);

                    CapsuleCollider cc = proximalColliderObj.AddComponent<CapsuleCollider>();
                    cc.height = proximalLength * unscale;
                    cc.radius = 0.01F * unscale;
                    cc.direction = 2; // Z-axis

                    if (distal != null) {
                        GameObject distalColliderObj = new GameObject("Distal Collider");
                        distalColliderObj.tag = this.gameObject.tag;
                        distalColliderObj.layer = this.gameObject.layer;
                        distalColliderObj.transform.parent = distal;
                        distalColliderObj.transform.localPosition = localIntermediatePosition.normalized * 0.01F;
                        distalColliderObj.transform.localRotation = Quaternion.identity;

                        SphereCollider sc = distalColliderObj.AddComponent<SphereCollider>();
                        //sc.center = fingerRotation * new Vector3(0, 0, -0.01F * unscale);
                        sc.radius = 0.01F * unscale;
                    }
                }
            }
        }

        #endregion

        public void Vibrate(float strength) {
            for (int i = 0; i < sensors.Length; i++)
                sensors[i].Vibrate(0.1F, strength);
        }
    }
}

#if LIB
namespace Passer.HumanoidMovements {
    [StructLayout(LayoutKind.Sequential)]
    public struct Vec3 {
        public float x;
        public float y;
        public float z;

        public Vec3(Vector3 v) {
            x = v.x;
            y = v.y;
            z = v.z;
        }
        public Vector3 Vector3 {
            get { return new Vector3(x, y, z); }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Quat {
        public float x;
        public float y;
        public float z;
        public float w;

        public Quat(Quaternion q) {
            x = q.x;
            y = q.y;
            z = q.z;
            w = q.w;
        }
        public Quaternion Quaternion {
            get { return new Quaternion(x, y, z, w); }
        }
    }

    public class Arm {
        private System.IntPtr pArm;

        public Arm() {
            pArm = Arm_Constructor();
        }
        [DllImport("HumanoidMovements")]
        private static extern System.IntPtr Arm_Constructor();

        ~Arm() {
            Arm_Destructor(pArm);
        }
        [DllImport("HumanoidMovements")]
        private static extern void Arm_Destructor(System.IntPtr pArm);

#region Parameters
        public bool isLeft {
            get { return Arm_GetIsLeft(pArm); }
            set { Arm_SetIsLeft(pArm, value); }
        }
        [DllImport("HumanoidMovements")]
        private static extern bool Arm_GetIsLeft(System.IntPtr pArm);
        [DllImport("HumanoidMovements")]
        private static extern void Arm_SetIsLeft(System.IntPtr pArm, bool isLeft);

#region Main Target
        public Vector3 targetPosition {
            get { return Arm_GetMainTargetPosition(pArm).Vector3; }
            set { Arm_SetMainTargetPosition(pArm, new Vec3(value)); }
        }
        [DllImport("HumanoidMovements")]
        private static extern Vec3 Arm_GetMainTargetPosition(System.IntPtr pArm);
        [DllImport("HumanoidMovements")]
        private static extern void Arm_SetMainTargetPosition(System.IntPtr pArm, Vec3 position);

        public Quaternion targetRotation {
            get { return Arm_GetMainTargetRotation(pArm).Quaternion; }
            set { Arm_SetMainTargetRotation(pArm, new Quat(value)); }
        }
        [DllImport("HumanoidMovements")]
        private static extern Quat Arm_GetMainTargetRotation(System.IntPtr pArm);
        [DllImport("HumanoidMovements")]
        private static extern void Arm_SetMainTargetRotation(System.IntPtr pArm, Quat rotation);
#endregion

#region Sub Target
        public void SetTargetPosition(ArmBones boneId, Vector3 position) {
            Arm_SetTargetPosition(pArm, (int)boneId, new Vec3(position));
        }
        [DllImport("HumanoidMovements")]
        private static extern void Arm_SetTargetPosition(System.IntPtr pArm, int boneId, Vec3 position);

        public Vector3 GetTargetPosition(ArmBones boneId) {
            return Arm_GetTargetPosition(pArm, (int)boneId).Vector3;
        }
        [DllImport("HumanoidMovements")]
        private static extern Vec3 Arm_GetTargetPosition(System.IntPtr pArm, int boneId);

        public void SetTargetRotation(ArmBones boneId, Quaternion rotation) {
            Arm_SetTargetRotation(pArm, (int)boneId, new Quat(rotation));
        }
        [DllImport("HumanoidMovements")]
        private static extern void Arm_SetTargetRotation(System.IntPtr pArm, int boneId, Quat rotation);

        public Quaternion GetTargetRotation(ArmBones boneId) {
            return Arm_GetTargetRotation(pArm, (int)boneId).Quaternion;
        }
        [DllImport("HumanoidMovements")]
        private static extern Quat Arm_GetTargetRotation(System.IntPtr pArm, int boneId);
#endregion

#region Bone
        // Set Bone Position
        public void SetBonePosition(ArmBones boneId, Vector3 position) {
            Arm_SetBonePosition(pArm, (int)boneId, new Vec3(position));
        }
        [DllImport("HumanoidMovements")]
        private static extern void Arm_SetBonePosition(System.IntPtr pArm, int boneId, Vec3 position);

        // Get Bone Position
        public Vector3 GetBonePosition(ArmBones boneId) {
            return Arm_GetBonePosition(pArm, (int)boneId).Vector3;
        }
        [DllImport("HumanoidMovements")]
        private static extern Vec3 Arm_GetBonePosition(System.IntPtr pArm, int boneId);

        // Set Bone Rotation
        public void SetBoneRotation(ArmBones boneId, Quaternion rotation) {
            Arm_SetBoneRotation(pArm, (int)boneId, new Quat(rotation));
        }
        [DllImport("HumanoidMovements")]
        private static extern void Arm_SetBoneRotation(System.IntPtr pArm, int boneId, Quat rotation);

        // Get Bone Rotation
        public Quaternion GetBoneRotation(ArmBones boneId) {
            return Arm_GetBoneRotation(pArm, (int)boneId).Quaternion;
        }
        [DllImport("HumanoidMovements")]
        private static extern Quat Arm_GetBoneRotation(System.IntPtr pArm, int boneId);
#endregion
#endregion

#region Init
        public void Init() {
            Arm_Init(pArm);
        }
        [DllImport("HumanoidMovements")]
        private static extern void Arm_Init(System.IntPtr pArm);
#endregion

#region Update
        public void Update() {
            Debug.Log("Update");
            Arm_Update(pArm);
        }
        [DllImport("HumanoidMovements")]
        private static extern void Arm_Update(System.IntPtr pArm);

        public void UpdateMovements() {
            Arm_UpdateMovements(pArm);
        }
        [DllImport("HumanoidMovements")]
        private static extern void Arm_UpdateMovements(System.IntPtr pArm);
#endregion

#region Tools
        public void SetTargets(HandTarget handTarget) {
            //SetTargetRotation(ArmBones.Shoulder, handTarget.shoulder.target.transform.rotation);
            //SetTargetRotation(ArmBones.UpperArm, handTarget.upperArm.target.transform.rotation);
            //SetTargetRotation(ArmBones.Forearm, handTarget.forearm.target.transform.rotation);
            //SetTargetRotation(ArmBones.Hand, handTarget.hand.target.transform.rotation);
            targetPosition = handTarget.transform.position;
            targetRotation = handTarget.transform.rotation;
        }
        public void CopyToRig(HandTarget handTarget) {
            //Debug.Log(handTarget.hand.bone.transform.rotation + " " + GetBoneRotation(ArmBones.Hand));
            handTarget.hand.bone.transform.rotation = GetBoneRotation(ArmBones.Hand);
            //handTarget.forearm.bone.transform.rotation = GetBoneRotation(ArmBones.Forearm);
            //handTarget.upperArm.bone.transform.rotation = GetBoneRotation(ArmBones.UpperArm);
            //handTarget.shoulder.bone.transform.rotation = GetBoneRotation(ArmBones.Shoulder);
        }
#endregion
    }
}
#endif