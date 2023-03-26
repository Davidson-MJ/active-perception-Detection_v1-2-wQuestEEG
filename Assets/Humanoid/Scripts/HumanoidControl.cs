using UnityEngine;

namespace Passer.Humanoid {
    using Pawn;
    using Tracking;

    /// \author PasserVR
    /// \version 3.3.0
    /// \mainpage Humanoid Control for Unity
    /// 
    /// Note: this documentation is still work in progress...
    /// 
    /// Quick Access
    /// ------------
    /// * HumanoidControl
    /// * HeadTarget
    /// * HandTarget
    /// * HipsTarget
    /// * FootTarget
    /// Input
    /// -----
    /// * ControllerInput
    /// * InteractionPointer
    /// Generic tools
    /// -------------
    /// * TeleportTarget
    /// * Socket
    /// * Handle
    /// Networking
    /// ----------
    /// * NetworkingStarter

    [System.Serializable]
    public enum NetworkingSystems {
        None
#if !UNITY_2019_1_OR_NEWER
        , UnityNetworking
#endif
#if hPHOTON1 || hPHOTON2
        , PhotonNetworking
#endif
#if hBOLT
        , PhotonBolt
#endif
#if hMIRROR
        , MirrorNetworking
#endif
    }

    /// <summary>Control avatars using tracking and animation options</summary>
    /// More inforation on the Humanoid Control Unity Component click
    /// <a href="https://passervr.com/documentation/humanoid-control/humanoid-control-script/">here</a>
    [HelpURL("https://passervr.com/documentation/humanoid-control/humanoid-control-script/")]
    public class HumanoidControl : PawnControl {
        /// <summary>The path at which the HumanoidControl script is found</summary>
        public string path;

        public new HeadTarget headTarget;
        /// <summary>The target script to control the left arm bones</summary>
        public new HandTarget leftHandTarget;
        /// <summary>The target script to control the right arm bones</summary>
        public new HandTarget rightHandTarget;
        /// <summary>The target script to control the torso bones</summary>
        public HipsTarget hipsTarget;
        /// <summary>The target script to control the left leg bones</summary>
        public FootTarget leftFootTarget;
        /// <summary>The target script to control the right leg bones</summary>
        public FootTarget rightFootTarget;

        public enum PrimaryTarget {
            Head,
            Hips
        };
        public PrimaryTarget primaryTarget;

        public new enum TargetId {
            Hips,
            Head,
            LeftHand,
            RightHand,
            LeftFoot,
            RightFoot,
            Face
        }

        /// <summary>The target bones rig</summary>
        /// The target bones rig contain the target pose of the avatar
        /// The humanoid movements will try to move the avatar such that the target pose is reached
        /// as closely as possible
        public Animator targetsRig;
        /// <summary>
        /// Draws the target rig in the scene view
        /// </summary>
        public bool showTargetRig = false;
        /// <summary>The neck height of the target rig</summary>
        /// When head tracking is used, this can be used to estimate the height of the player.
        public float trackingNeckHeight {
            get {
                if (headTarget == null || headTarget.neck.target.transform == null)
                    return 0;

                return headTarget.neck.target.transform.position.y - transform.position.y;
            }
        }

        /// <summary>The avatar Rig</summary>
        /// This is the rig of the avatar we want to control.
        public Animator avatarRig;
        /// <summary>
        /// Draws the avatar rig in the scene view
        /// </summary>
        public bool showAvatarRig = true;

        /// <summary>
        /// The neck height of the avatar
        /// </summary>
        public float avatarNeckHeight;

        /// <summary>
        /// Draws the tension at the joints of the avatar
        /// </summary>
        public bool showMuscleTension = false;

        /// <summary>Calculate the avatar pose</summary>
        /// When this option is enabled, the pose of the avatar is updated from the target rig using the 
        /// Humanoid movements.
        /// If you are only interested in the tracking result, you can disable this option and use
        /// the target rig to access the tracked pose.
        public bool calculateBodyPose = true;

        public static void SetControllerID(HumanoidControl humanoid, int controllerID) {
            if (humanoid.traditionalInput != null) {
                humanoid.controller = humanoid.traditionalInput.SetControllerID(controllerID);
                humanoid.gameControllerIndex = controllerID;
            }
        }

        /// <summary>
        /// Enables the animator for this humanoid
        /// </summary>
        public bool animatorEnabled = true;
        /// <summary>
        /// The Animator for this humanoid
        /// </summary>
        public RuntimeAnimatorController animatorController = null;

        /// <summary>
        /// The pose of this humanoid
        /// </summary>
        public Pose pose;
        /// <summary>
        /// Is true when the pose is currently being edited
        /// </summary>
        public bool editPose;

        #region Networking

        /// <summary>The networking interface</summary>
        public IHumanoidNetworking humanoidNetworking;
        /// <summary>The remote avatar prefab for this humanoid</summary>
        public GameObject remoteAvatar;
        /// <summary>Is true when this is a remote avatar</summary>
        /// Remote avatars are not controlled locally, but are controlled from another computer.
        /// These are copies of the avatar on that other computer and are updated via messages
        /// exchanges on a network.
        //public bool isRemote = false;
        /// <summary>The Id of this humanoid across the network</summary>
        //public ulong nwId;
        /// <summary>The Player Type of the humanoid</summary>
        public int playerType;
        public bool syncRootTransform = true;

        // Experimental
        public string remoteTrackerIpAddress;

        #endregion

        /// <summary>
        /// The local Id of this humanoid
        /// </summary>
        public int humanoidId = -1;

        #region Settings

        [SerializeField]
        private bool _showSkeletons;
        /// <summary>
        /// If enabled, tracking skeletons will be rendered
        /// </summary>
        public bool showSkeletons {
            get { return _showSkeletons; }
            set {
                _showSkeletons = value;
                foreach (HumanoidTracker tracker in trackers) {
                    tracker.ShowSkeleton(_showSkeletons);
                }
            }
        }

        /// <summary>
        /// If true, it wil generate colliders for the avatar where necessary
        /// </summary>
        public bool generateColliders = true;

        /// <summary>
        /// Will use haptic feedback on supported devices when the hands are colliding or touching objects
        /// </summary>
        public bool haptics = false;


        /// <summary>Types of Scaling which can be used to scale the tracking input to the size of the avatar</summary>
        /// SetHeightToAvatar adjusts the vertical tracking to match the avatar size.
        /// MoveHeightToAvatar does the same but also resets the tracking origin to the location of the avatar.
        /// ScaleTrackingToAvatar scales the tracking space to match the avatar size.
        /// ScaleAvatarToTracking resizes the avatar to match the player size.
        public enum ScalingType {
            None,
            SetHeightToAvatar,
            //MoveHeightToAvatar,
            ScaleTrackingToAvatar,
            ScaleAvatarToTracking
        }
        /// <summary>Scale Tracking to Avatar scales the tracking input to match the size of the avatar</summary>
        [SerializeField]
        protected ScalingType scaling = ScalingType.SetHeightToAvatar;

        // Prefab only
        //public bool disconnectInstances = false;

        #endregion

        #region Init

        protected override void Awake() {
            //Application.targetFrameRate = 2;

            if (dontDestroyOnLoad)
                DontDestroyOnLoad(this.transform.root);

            generateColliders = true;

            AddHumanoid();
            CheckTargetRig(this);
            avatarRig = GetAvatar(this.gameObject);

            // Move the animator controller to the targets rig for proper animation support
            // This is disabled (legacy) use animationController field to set animator;
            if (avatarRig != null && avatarRig.runtimeAnimatorController != null) {
                // && targetsRig.runtimeAnimatorController == null) {
                //    targetsRig.runtimeAnimatorController = avatarRig.runtimeAnimatorController;
                avatarRig.runtimeAnimatorController = null;
            }

            DetermineTargets();
            InitTargets();
            NewTargetComponents();
            RetrieveBones();
            InitAvatar();


            avatarNeckHeight = GetAvatarNeckHeight();
            MatchTargetsToAvatar();

            AddCharacterColliders();

            StartTargets();

            InitTrackers();
            StartTrackers();

            StartSensors();
        }

        #endregion

        #region Avatar

        public event OnChangeAvatar OnChangeAvatarEvent;
        public delegate void OnChangeAvatar();

        private float GetAvatarNeckHeight() {
            if (avatarRig == null)
                return headTarget.transform.localPosition.y;

            Transform avatarNeck = headTarget.neck.bone.transform; // avatarRig.GetBoneTransform(HumanBodyBones.Neck);
            if (avatarNeck != null) {
                float neckHeight = avatarNeck.position.y - avatarRig.transform.position.y;
                return neckHeight;
            }
            else
                return headTarget.transform.localPosition.y;
        }

        public void ChangeAvatar(GameObject fpAvatarPrefab) {
            ChangeAvatar(fpAvatarPrefab, fpAvatarPrefab);
        }

        public void ChangeAvatar(GameObject fpAvatarPrefab, GameObject tpAvatarPrefab) {
            remoteAvatar = tpAvatarPrefab;
            LocalChangeAvatar(fpAvatarPrefab);

            if (humanoidNetworking != null) {
                if (remoteAvatar != null)
                    humanoidNetworking.ChangeAvatar(this, remoteAvatar.name);
                else
                    humanoidNetworking.ChangeAvatar(this, fpAvatarPrefab.name);
            }

            if (OnChangeAvatarEvent != null)
                OnChangeAvatarEvent.Invoke();
        }

        public void LocalChangeAvatar(GameObject avatarPrefab) {
            Animator animator = avatarPrefab.GetComponent<Animator>();
            if (animator == null || animator.avatar == null || !animator.avatar.isValid) {
                Debug.LogWarning("Could not detect suitable avatar");
                return;
            }

            // bones of previous avatar are no longer valid
            HeadTarget.ClearBones(headTarget);
            HandTarget.ClearBones(leftHandTarget);
            HandTarget.ClearBones(rightHandTarget);
            leftFootTarget.ClearBones();
            rightFootTarget.ClearBones();

            if (avatarRig != null) {
                if (avatarRig.transform != this.transform) {
                    DestroyImmediate(avatarRig.gameObject, true);
                }
                else {
                    // Delete all humanoid related gameObjects
                    // This may be destroying too much...
                    int maxChildren = 0;
                    while (this.transform.childCount > maxChildren) {
                        GameObject objToDelete = this.transform.GetChild(maxChildren).gameObject;
                        if (objToDelete == targetsRig.gameObject ||
                            objToDelete == realWorld.gameObject) {
                            maxChildren++;
                            continue;
                        }
                        DestroyImmediate(objToDelete);
                    }
                    DestroyImmediate(avatarRig);
                }
            }


            GameObject avatarObj = (GameObject)Instantiate(avatarPrefab, this.transform.position, this.transform.rotation);
            avatarObj.transform.SetParent(this.transform);
            avatarObj.transform.localPosition = Vector3.zero;

            // Remove camera from avatar
            Transform t = avatarObj.transform.FindDeepChild("First Person Camera");
            if (t != null)
                Destroy(t.gameObject);

#if hLEGACYXR
            if (headTarget.unity.cameraTransform == null)
                UnityVRHead.CheckCamera(headTarget);
#endif

            CheckTargetRig(this);
            InitializeAvatar();
            AddCharacterColliders();
            avatarNeckHeight = GetAvatarNeckHeight();

            switch (scaling) {
                case ScalingType.SetHeightToAvatar:
                    SetTrackingHeightToAvatar();
                    break;
                case ScalingType.ScaleAvatarToTracking:
                    ScaleAvatarToTracking();
                    break;
                case ScalingType.ScaleTrackingToAvatar:
                    ScaleTrackingToAvatar();
                    break;
                default:
                    break;
            }
        }

        public void InitializeAvatar() {
            avatarRig = GetAvatar(this.gameObject);

            // Move the animator controller to the targets rig for proper animation support
            if (avatarRig.runtimeAnimatorController != null && targetsRig.runtimeAnimatorController == null) {
                targetsRig.runtimeAnimatorController = avatarRig.runtimeAnimatorController;
                avatarRig.runtimeAnimatorController = null;
                avatarRig.gameObject.SetActive(false);
            }

            RetrieveBones();
            InitAvatar();
            MatchTargetsToAvatar();

            //avatarNeckHeight = GetAvatarNeckHeight();
            // This will change the target rotations wrongly when changing avatars
            //MatchTargetsToAvatar();

            //AddCharacterColliders();

            leftHandTarget.StartTarget();
            rightHandTarget.StartTarget();
        }

        private void InitializeAvatar2(GameObject avatarRoot) {
            avatarRig = GetAvatar(avatarRoot);

            // Move the animator controller to the targets rig for proper animation support
            if (avatarRig.runtimeAnimatorController != null && targetsRig.runtimeAnimatorController == null) {
                targetsRig.runtimeAnimatorController = avatarRig.runtimeAnimatorController;
                avatarRig.runtimeAnimatorController = null;
                avatarRig.gameObject.SetActive(false);
            }

            RetrieveBones();
            InitAvatar();
            //MatchTargetsToAvatar();

            //avatarNeckHeight = GetAvatarNeckHeight();
            // This will change the target rotations wrongly when changing avatars
            //MatchTargetsToAvatar();

            //AddCharacterColliders();

            leftHandTarget.StartTarget();
            rightHandTarget.StartTarget();
        }

        /// <summary>
        /// Analyses the avatar's properties requires for the movements
        /// </summary>
        public void InitAvatar() {
            hipsTarget.InitAvatar();
            headTarget.InitAvatar();
            leftHandTarget.InitAvatar();
            rightHandTarget.InitAvatar();
            leftFootTarget.InitAvatar();
            rightFootTarget.InitAvatar();
        }


        public void ScaleAvatarToTracking() {
#if hLEGACYXR
            Vector3 localNeckPosition;
            if (Passer.Tracking.UnityVRDevice.xrDevice == Passer.Tracking.UnityVRDevice.XRDeviceType.None || headTarget.unity.cameraTransform == null)
                localNeckPosition = headTarget.neck.target.transform.position - transform.position;
            else
                localNeckPosition = HeadMovements.CalculateNeckPositionFromEyes(headTarget.unity.cameraTransform.position, headTarget.unity.cameraTransform.rotation, -headTarget.neck2eyes) - transform.position;

            Debug.Log(localNeckPosition.y + " / " + avatarNeckHeight + " " + headTarget.unity.cameraTransform.position.y + " " + headTarget.neck2eyes);

            ScaleAvatar(localNeckPosition.y / avatarNeckHeight);
#endif
        }

        private void ScaleAvatarToHeight(float height) {
            if (height <= 0)
                return;

            float neckHeight = 0.875F * height;
            ScaleAvatar(neckHeight / avatarNeckHeight);
        }

        private void ScaleAvatar(float scaleFactor) {
            avatarRig.transform.localScale = Vector3.one * scaleFactor;

            // The scaling will result in wrong length for the forearm.
            // This is because the hands are detached and the position of the hands is not scaled with the avatar.
            // The solution is to reattach the hands temporarily when changing avatar
            // This may be in general a good thing to do, but the impact is non-trivial.

            Quaternion leftForearmRotation = leftHandTarget.forearm.bone.transform.rotation * leftHandTarget.forearm.bone.toTargetRotation;
            leftHandTarget.hand.bone.transform.position = leftHandTarget.forearm.bone.transform.position + leftForearmRotation * leftHandTarget.outward * (leftHandTarget.forearm.bone.length * scaleFactor);

            Quaternion rightForearmRotation = rightHandTarget.forearm.bone.transform.rotation * rightHandTarget.forearm.bone.toTargetRotation;
            rightHandTarget.hand.bone.transform.position = rightHandTarget.forearm.bone.transform.position + rightForearmRotation * rightHandTarget.outward * (rightHandTarget.forearm.bone.length * scaleFactor);

            leftHandTarget.hand.bone.transform.localScale = Vector3.one * scaleFactor;
            rightHandTarget.hand.bone.transform.localScale = Vector3.one * scaleFactor;

            CheckTargetRig(this);
            InitializeAvatar();
        }

        /// <summary>Match the target rig transform to the humanoid transform</summary>
        public static void CheckTargetRig(HumanoidControl humanoid) {
            if (humanoid.targetsRig == null) {
                Object targetsRigPrefab = Resources.Load("HumanoidTargetsRig");
                GameObject targetsRigObject = (GameObject)Instantiate(targetsRigPrefab);
                targetsRigObject.name = "Target Rig";
                humanoid.targetsRig = targetsRigObject.GetComponent<Animator>();

                targetsRigObject.transform.position = humanoid.transform.position;
                targetsRigObject.transform.rotation = humanoid.transform.rotation;
                targetsRigObject.transform.SetParent(humanoid.transform);
            }

            humanoid.targetsRig.runtimeAnimatorController = humanoid.animatorController;
        }

        /// <summary>Retrieve the avatar rig for this humanoid</summary>
        public Animator GetAvatar(GameObject avatarRoot) {
            if (avatarRig != null && avatarRig != targetsRig && avatarRig.enabled &&
                avatarRig.gameObject != null && avatarRig.gameObject.activeInHierarchy) {
                // We already have a good avatarRig
                return avatarRig;
            }

            // We don't have an avatar, make sure that the detached hands are deleted then
            if (!Application.isPlaying) {
                if (leftHandTarget != null && leftHandTarget.handRigidbody != null)
                    DestroyImmediate(leftHandTarget.handRigidbody.gameObject, true);
                if (rightHandTarget != null && rightHandTarget.handRigidbody != null)
                    DestroyImmediate(rightHandTarget.handRigidbody.gameObject, true);
            }

            Avatar avatar = null;
            Animator animator = avatarRoot.GetComponent<Animator>();
            if (animator != null) {
                avatar = animator.avatar;
                if (avatar != null && avatar.isValid/* && avatar.isHuman*/ && animator != targetsRig) {
                    return animator;
                }
            }

            Animator[] animators = avatarRoot.GetComponentsInChildren<Animator>();
            for (int i = 0; i < animators.Length; i++) {
                avatar = animators[i].avatar;
                if (avatar != null && avatar.isValid /*&& avatar.isHuman*/ && animators[i] != targetsRig) {
                    return animators[i];
                }
            }
            return null;
        }

        //private void ScaleAvatar2Tracking() {
        //    Animator characterAnimator = avatarRig.GetComponent<Animator>();

        //    for (int i = 0; i < (int)HumanBodyBones.LastBone; i++) {
        //        Transform sourceBone = targetsRig.GetBoneTransform((HumanBodyBones)i);
        //        Transform destBone = characterAnimator.GetBoneTransform((HumanBodyBones)i);

        //        if (sourceBone != null && destBone != null) {
        //            float sourceBoneLength = GetBoneLength(sourceBone);
        //            float destBoneLength = GetBoneLength(destBone);

        //            if (sourceBoneLength > 0 && destBoneLength > 0) {
        //                float startScaling = (destBone.localScale.x + destBone.localScale.y + destBone.localScale.z) / 3;
        //                float scaling = (sourceBoneLength / destBoneLength);
        //                float resultScaling = startScaling * scaling;
        //                destBone.localScale = new Vector3(resultScaling, resultScaling, resultScaling);
        //            }
        //        }
        //    }
        //}

        private static float GetBoneLength(Transform bone) {
            if (bone.childCount == 1) {
                Transform childBone = bone.GetChild(0);

                float length = Vector3.Distance(bone.position, childBone.position);
                return length;
            }
            else
                return 0;
        }

        #endregion

        #region Targets
        protected override void NewTargetComponents() {
            hipsTarget.NewComponent(this);
            hipsTarget.InitComponent();

            headTarget.NewComponent(this);
            headTarget.InitComponent();

            leftHandTarget.NewComponent(this);
            leftHandTarget.InitComponent();

            rightHandTarget.NewComponent(this);
            rightHandTarget.InitComponent();

            leftFootTarget.NewComponent(this);
            leftFootTarget.InitComponent();

            rightFootTarget.NewComponent(this);
            rightFootTarget.InitComponent();
        }

        /// <summary>Initialize the targets for this humanoid</summary>
        public override void InitTargets() {
            SetBones();
        }

        /// <summary>Start the targets for this humanoid</summary>
        protected override void StartTargets() {
            hipsTarget.StartTarget();
            headTarget.StartTarget();
            leftHandTarget.StartTarget();
            rightHandTarget.StartTarget();
            leftFootTarget.StartTarget();
            rightFootTarget.StartTarget();
        }

        /// <summary>Checks the humanoid for presence of Targets and adds them if they are not found </summary>
        public void DetermineTargets() {
            HeadTarget.DetermineTarget(this);
            HandTarget.DetermineTarget(this, true);
            HandTarget.DetermineTarget(this, false);
            HipsTarget.DetermineTarget(this);
            FootTarget.DetermineTarget(this, true);
            FootTarget.DetermineTarget(this, false);
        }

        /// <summary>Changes the target rig transforms to match the avatar rig</summary>
        public void MatchTargetsToAvatar() {
            hipsTarget.MatchTargetsToAvatar();
            headTarget.MatchTargetsToAvatar();
            leftHandTarget.MatchTargetsToAvatar();
            rightHandTarget.MatchTargetsToAvatar();
            leftFootTarget.MatchTargetsToAvatar();
            rightFootTarget.MatchTargetsToAvatar();
        }

        private void UpdateTargetsAndMovements() {
            CopyTargetsToRig();

            UpdateTargets();
            UpdateMovements();

            CopyRigToTargets();
        }

        protected override void UpdateTargets() {
            hipsTarget.UpdateTarget();
            headTarget.UpdateTarget();
            leftHandTarget.UpdateTarget();
            rightHandTarget.UpdateTarget();
            leftFootTarget.UpdateTarget();
            rightFootTarget.UpdateTarget();
        }

        /// <summary>Updates the avatar pose based on the targets rig</summary>
        public void UpdateMovements() {
            headTarget.UpdateMovements(this);
            hipsTarget.UpdateMovements(this);
            leftHandTarget.UpdateMovements(this);
            rightHandTarget.UpdateMovements(this);
            leftFootTarget.UpdateMovements(this);
            rightFootTarget.UpdateMovements(this);
        }

        /// <summary>Copies the pose of the target rig to the avatar</summary>
        private void CopyTargetsToRig() {
            hipsTarget.CopyTargetToRig();
            headTarget.CopyTargetToRig();
            leftHandTarget.CopyTargetToRig();
            rightHandTarget.CopyTargetToRig();
            leftFootTarget.CopyTargetToRig();
            rightFootTarget.CopyTargetToRig();
        }

        /// <summary>Copies the pose of the avatar to the target rig</summary>
        public void CopyRigToTargets() {
            hipsTarget.CopyRigToTarget();
            headTarget.CopyRigToTarget();
            leftHandTarget.CopyRigToTarget();
            rightHandTarget.CopyRigToTarget();
            leftFootTarget.CopyRigToTarget();
            rightFootTarget.CopyRigToTarget();
        }

        /// <summary>Updated the sensor transform from the target transforms</summary>
        public void UpdateSensorsFromTargets() {
#if hLEAP
            // temporary solution? Leap may need a Head Sensor Component for the camera tracker?
            leapTracker.UpdateTrackerFromTarget(leapTracker.isHeadMounted);
#endif
            hipsTarget.UpdateSensorsFromTarget();
            headTarget.UpdateSensorsFromTarget();
            leftHandTarget.UpdateSensorsFromTarget();
            rightHandTarget.UpdateSensorsFromTarget();
            leftFootTarget.UpdateSensorsFromTarget();
            rightFootTarget.UpdateSensorsFromTarget();
        }

        private HumanoidTarget.TargetedBone[] _bones = null;
        /// <summary>Get the Humanoid Bone</summary>
        /// <param name="boneId">The identification of the requested bone</param>
        public HumanoidTarget.TargetedBone GetBone(Bone boneId) {
            if (_bones == null)
                SetBones();
            if (_bones == null || (int)boneId > _bones.Length)
                return null;
            return _bones[(int)boneId];
        }
        /// <summary>Get the Humanoid Bone on the incated side of the humanoid</summary>
        /// <param name="side">The requested side of the humanoid</param>
        /// <param name="sideBoneId">The identification of the requested bone</param>
        public HumanoidTarget.TargetedBone GetBone(Side side, SideBone sideBoneId) {
            if (_bones == null)
                SetBones();
            int boneIx = (int)BoneReference.HumanoidBone(side, sideBoneId);
            return _bones[boneIx];
        }
        private void SetBones() {
            _bones = new HumanoidTarget.TargetedBone[(int)Bone.Count] {
                null,
                hipsTarget.hips,
                hipsTarget.spine,
                null,
                null,
                hipsTarget.chest,

                headTarget.neck,
                headTarget.head,

                leftHandTarget.shoulder,
                leftHandTarget.upperArm,
                leftHandTarget.forearm,
                null,
                leftHandTarget.hand,

                leftHandTarget.fingers.thumb.proximal,
                leftHandTarget.fingers.thumb.intermediate,
                leftHandTarget.fingers.thumb.distal,

                null,
                leftHandTarget.fingers.index.proximal,
                leftHandTarget.fingers.index.intermediate,
                leftHandTarget.fingers.index.distal,

                null,
                leftHandTarget.fingers.middle.proximal,
                leftHandTarget.fingers.middle.intermediate,
                leftHandTarget.fingers.middle.distal,

                null,
                leftHandTarget.fingers.ring.proximal,
                leftHandTarget.fingers.ring.intermediate,
                leftHandTarget.fingers.ring.distal,

                null,
                leftHandTarget.fingers.little.proximal,
                leftHandTarget.fingers.little.intermediate,
                leftHandTarget.fingers.little.distal,

                leftFootTarget.upperLeg,
                leftFootTarget.lowerLeg,
                leftFootTarget.foot,
                leftFootTarget.toes,

                rightHandTarget.shoulder,
                rightHandTarget.upperArm,
                rightHandTarget.forearm,
                null,
                rightHandTarget.hand,

                rightHandTarget.fingers.thumb.proximal,
                rightHandTarget.fingers.thumb.intermediate,
                rightHandTarget.fingers.thumb.distal,

                null,
                rightHandTarget.fingers.index.proximal,
                rightHandTarget.fingers.index.intermediate,
                rightHandTarget.fingers.index.distal,

                null,
                rightHandTarget.fingers.middle.proximal,
                rightHandTarget.fingers.middle.intermediate,
                rightHandTarget.fingers.middle.distal,

                null,
                rightHandTarget.fingers.ring.proximal,
                rightHandTarget.fingers.ring.intermediate,
                rightHandTarget.fingers.ring.distal,

                null,
                rightHandTarget.fingers.little.proximal,
                rightHandTarget.fingers.little.intermediate,
                rightHandTarget.fingers.little.distal,

                rightFootTarget.upperLeg,
                rightFootTarget.lowerLeg,
                rightFootTarget.foot,
                rightFootTarget.toes,

#if hFACE
                headTarget.face.leftEye.upperLid,
                headTarget.face.leftEye,
                headTarget.face.leftEye.lowerLid,
                headTarget.face.rightEye.upperLid,
                headTarget.face.rightEye,
                headTarget.face.rightEye.lowerLid,

                headTarget.face.leftBrow.outer,
                headTarget.face.leftBrow.center,
                headTarget.face.leftBrow.inner,
                headTarget.face.rightBrow.inner,
                headTarget.face.rightBrow.center,
                headTarget.face.rightBrow.outer,

                headTarget.face.leftEar,
                headTarget.face.rightEar,

                headTarget.face.leftCheek,
                headTarget.face.rightCheek,

                headTarget.face.nose.top,
                headTarget.face.nose.tip,
                headTarget.face.nose.bottomLeft,
                headTarget.face.nose.bottom,
                headTarget.face.nose.bottomRight,

                headTarget.face.mouth.upperLipLeft,
                headTarget.face.mouth.upperLip,
                headTarget.face.mouth.upperLipRight,
                headTarget.face.mouth.lipLeft,
                headTarget.face.mouth.lipRight,
                headTarget.face.mouth.lowerLipLeft,
                headTarget.face.mouth.lowerLip,
                headTarget.face.mouth.lowerLipRight,

                headTarget.face.jaw,
#else
                null,
                null,
                null,
                null,
                null,
                null,

                null,
                null,
                null,
                null,
                null,
                null,

                null,
                null,

                null,
                null,

                null,
                null,
                null,
                null,
                null,

                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,

                null,
#endif
                null,
            };
        }

        #endregion

        #region Trackers

        /// <summary>Use game controller input</summary>
        public bool gameControllerEnabled = true;
        /// <summary>The game controller for this pawn</summary>
        public Passer.Controller controller;
        /// <summary>The index of the game controller</summary>
        public int gameControllerIndex;


        /// <summary>The Unity XR tracker</summary>\
#if pUNITYXR
        public UnityXRTracker unityXR = new UnityXRTracker();
#endif
#if hLEGACYXR
        public UnityVRTracker unity = new UnityVRTracker();
#endif
#if hOPENVR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
        public OpenVRHumanoidTracker openVR = new OpenVRHumanoidTracker();
#endif
#if hOCULUS && (UNITY_STANDALONE_WIN || UNITY_ANDROID)
        /// <summary>The Oculus tracker</summary>
        public OculusHumanoidTracker oculus = new OculusHumanoidTracker();
#endif
        /// <summary>The Windows Mixed Reality tracker</summary>
#if hWINDOWSMR && UNITY_2017_2_OR_NEWER && !UNITY_2020_1_OR_NEWER && UNITY_WSA_10_0
        public WindowsMRTracker mixedReality = new WindowsMRTracker();
#endif
        /// <summary>The Wave VR tracker</summary>
#if hWAVEVR
        public WaveVRTracker waveVR = new WaveVRTracker();
#endif
        /// <summary>The VRTK tracker</summary>
#if hVRTK
        public VrtkTracker vrtk = new VrtkTracker();
#endif
        /// <summary>The Perception Neuron tracker</summary>
#if hNEURON
        public NeuronTracker neuronTracker = new NeuronTracker();
#endif
        /// <summary>The Leap Motion tracker</summary>
#if hLEAP
        public LeapTracker leapTracker = new LeapTracker();
#endif
        /// <summary>The Intel RealSense tracker</summary>
#if hREALSENSE
        public RealsenseTracker realsense = new RealsenseTracker();
#endif
        /// <summary>The Razer Hydra tracker</summary>
#if hHYDRA
        public HydraTracker hydra = new HydraTracker();
#endif
        /// <summary>The Microsoft Kinect 360/Kinect for Windows tracker</summary>
#if hKINECT1
        public Kinect1Tracker kinect1 = new Kinect1Tracker();
#endif
        /// <summary>The Microsoft Kinect 2 tracker</summary>
#if hKINECT2
        public Kinect2Tracker kinect2 = new Kinect2Tracker();
#endif
#if hKINECT4
        /// <summary>
        /// Azure Kinect tracker
        /// </summary>
        public Kinect4Tracker kinect4 = new Kinect4Tracker();
#endif
        /// <summary>The Orbbec Astra tracker</summary>
#if hORBBEC && (UNITY_STANDALONE_WIN || UNITY_ANDROID || UNITY_WSA_10_0)
        public AstraTracker astra = new AstraTracker();
#endif
        /// <summary>The OptiTrack tracker</summary>
#if hOPTITRACK
        public OptiTracker optitrack = new OptiTracker();
#endif
        /// <summary>The Tobii tracker</summary>
#if hTOBII
        public TobiiTracker tobiiTracker = new TobiiTracker();
#endif
#if hARKIT && hFACE && UNITY_IOS && UNITY_2019_1_OR_NEWER
        public ArKit arkit = new ArKit();
#endif
        /// <summary>The Pupil Labs tracker</summary>
#if hPUPIL
        public Tracking.Pupil.Tracker pupil = new Tracking.Pupil.Tracker();
#endif
        /// <summary>The Dlib tracker</summary>
#if hDLIB
        public DlibTracker dlib = new DlibTracker();
#endif
#if hANTILATENCY
        public AntilatencyTracker antilatency = new AntilatencyTracker();
#endif
#if hHI5
        public Hi5Tracker hi5 = new Hi5Tracker();
#endif

        private new HumanoidTracker[] _trackers;
        /// <summary>All available trackers for this humanoid</summary>
        public new HumanoidTracker[] trackers {
            get {
                if (_trackers == null)
                    InitTrackers();
                return _trackers;
            }
        }

        private new TraditionalDevice traditionalInput;

        protected override void InitTrackers() {
            _trackers = new HumanoidTracker[] {
#if pUNITYXR
                unityXR,
#endif
#if hLEGACYXR
                unity,
#endif
#if hOPENVR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
                openVR,
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
#if hVRTK
                vrtk,
#endif
#if hNEURON
                neuronTracker,
#endif
#if hLEAP
                leapTracker,
#endif
#if hREALSENSE
                realsense,
#endif
#if hHYDRA
                hydra,
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
#if hORBBEC && (UNITY_STANDALONE_WIN || UNITY_ANDROID)
                astra,
#endif
#if hOPTITRACK
                optitrack,
#endif
#if hTOBII
                tobiiTracker,
#endif
#if hARKIT && hFACE && UNITY_IOS && UNITY_2019_1_OR_NEWER
                arkit,
#endif
#if hPUPIL
                pupil,
#endif
#if hDLIB
                dlib,
#endif
#if hANTILATENCY
                antilatency,
#endif
#if hHI5
                hi5,
#endif
            };
        }

        private void EnableTrackers(bool trackerEnabled) {
            foreach (HumanoidTracker tracker in _trackers)
                tracker.enabled = trackerEnabled;
        }

        protected override void StartTrackers() {
            if (traditionalInput == null)
                traditionalInput = new TraditionalDevice();
            traditionalInput.SetControllerID(0);

            for (int i = 0; i < _trackers.Length; i++) {
                if (_trackers[i].enabled)
                    _trackers[i].StartTracker(this);
            }

            // Experimental
#if hNW_MIRROR || hNW_UNET
            if (remoteTrackerIpAddress != null && remoteTrackerIpAddress != "") {
#if hNW_UNET
                UnetStarter.StartClient(remoteTrackerIpAddress);
#endif
            }
#endif
        }

        protected override void UpdateTrackers() {
            //if (gameControllerEnabled && traditionalInput != null)
            //    traditionalInput.UpdateGameController(gameController);

            for (int i = 0; i < trackers.Length; i++) {
                if (trackers[i].enabled)
                    trackers[i].UpdateTracker();
            }
        }

        protected override void StartSensors() {
            hipsTarget.StartSensors();
            headTarget.StartSensors();
            leftHandTarget.StartSensors();
            rightHandTarget.StartSensors();
            leftFootTarget.StartSensors();
            rightFootTarget.StartSensors();
        }

        protected override void StopSensors() {
            hipsTarget.StopSensors();
            headTarget.StopSensors();
            leftHandTarget.StopSensors();
            rightHandTarget.StopSensors();
            leftFootTarget.StopSensors();
            rightFootTarget.StopSensors();
        }

        public void ScaleTrackingToAvatar() {
            GameObject realWorld = GetRealWorld(transform);
            float neckHeight = headTarget.transform.position.y - transform.position.y;
            neckHeight = neckHeight / realWorld.transform.lossyScale.y;
            ScaleTracking(avatarNeckHeight / neckHeight);
        }

        private void ScaleTracking(float scaleFactor) {
            GameObject realWorld = HumanoidControl.GetRealWorld(transform);
            Vector3 newScale = scaleFactor * Vector3.one; // * realWorld.transform.localScale;

            targetsRig.transform.localScale = newScale;
            realWorld.transform.localScale = newScale;
        }

        /// <summary>Adjust Y position to match the tracking with the avatar</summary>
        /// This function will adjust the vertical position of the tracking origin such that the tracking
        /// matches the avatar. This function should preferably be executed when the player is in a base
        /// position: either standing upright or sitting upright, depending on the playing pose.
        /// This will prevent the avatar being in the air or in a crouching position when the player is
        /// taller or smaller than the avatar itself.
        /// It retains 1:1 tracking and the X/Z position of the player are not affected.
        protected override void SetTrackingHeightToAvatar() {
            //#if !pUNITYXR
            /*
            float localNeckHeight;
            if (headTarget.unity.cameraTransform == null ||
                UnityVRDevice.xrDevice == UnityVRDevice.XRDeviceType.None ||
                headTarget.head.target.confidence.position <= 0
                ) {

                localNeckHeight = headTarget.neck.target.transform.position.y - transform.position.y;
            }
            else {
                //Vector3 neckPosition = HeadMovements.CalculateNeckPosition(headTarget.unityVRHead.cameraTransform.position, headTarget.unityVRHead.cameraTransform.rotation, -headTarget.neck2eyes);
                //localNeckHeight = neckPosition.y - transform.position.y;
                localNeckHeight = headTarget.neck.target.transform.position.y - transform.position.y;
            }
            */
            Vector3 neckPosition;
            if (headTarget.neck.target.confidence.position > 0.2F)
                neckPosition = headTarget.neck.target.transform.position;
            else {
                Transform headTargetTransform = headTarget.head.target.transform;
                neckPosition = HeadMovements.CalculateNeckPositionFromHead(headTargetTransform.position, headTargetTransform.rotation, headTarget.neck.bone.length);
            }

            float playersNeckHeight = neckPosition.y - transform.position.y;

            float deltaY = avatarNeckHeight - playersNeckHeight;
            AdjustTrackingHeight(deltaY);
            //#endif
        }

        //public void MoveTrackingHeightToAvatar() {
        //    Vector3 localNeckPosition;
        //    if (UnityVRDevice.xrDevice == UnityVRDevice.XRDeviceType.None || headTarget.unity.cameraTransform == null)
        //        localNeckPosition = headTarget.neck.target.transform.position - transform.position;
        //    else
        //        localNeckPosition = HeadMovements.CalculateNeckPosition(headTarget.unity.cameraTransform.position, headTarget.unity.cameraTransform.rotation, -headTarget.neck2eyes) - transform.position;
        //    Vector3 delta = new Vector3(-localNeckPosition.x, avatarNeckHeight - localNeckPosition.y, -localNeckPosition.z);
        //    AdjustTracking(delta);
        //}

        private void AdjustTrackingHeight(float deltaY) {
            AdjustTracking(new Vector3(0, deltaY, 0));
        }

        /// <summary>Adjust the tracking origin of all trackers</summary>
        /// <param name="translation">The translation to apply to the tracking origin</param>
        public override void AdjustTracking(Vector3 translation) {
            foreach (HumanoidTracker tracker in trackers)
                tracker.AdjustTracking(translation, Quaternion.identity);
        }

        /// <summary>Adjust the tracking origin of all trackers</summary>
        /// <param name="translation">The translation to apply to the tracking origin</param>
        /// <param name="rotation">The rotation to apply to the tracking origin</param>
        public void AdjustTracking(Vector3 translation, Quaternion rotation) {
            foreach (HumanoidTracker tracker in trackers)
                tracker.AdjustTracking(translation, rotation);
        }

        #endregion

        #region Configuration
        /// <summary>
        /// Scans the humanoid to retrieve all bones
        /// </summary>
        public void RetrieveBones() {
            hipsTarget.RetrieveBones();
            headTarget.RetrieveBones();

            leftHandTarget.RetrieveBones();
            rightHandTarget.RetrieveBones();
            leftFootTarget.RetrieveBones();
            rightFootTarget.RetrieveBones();
        }
        #endregion

        #region Update
        protected override void FixedUpdate() {
            DetermineCollision();
            CalculateMovement();
            CheckBodyPull();

            CheckGround();

            if (leftHandTarget.handMovements != null)
                leftHandTarget.handMovements.FixedUpdate();
            if (rightHandTarget.handMovements != null)
                rightHandTarget.handMovements.FixedUpdate();

        }

        protected override void Update() {
            Controllers.Clear();
            traditionalInput.UpdateGameController(gameController);
            UpdatePose();
            UpdateTrackers();
            UpdateTargetsAndMovements();
            CalculateVelocityAcceleration();

            UpdateAnimation();
            UpdatePoseEvent();
            PreAnimation();
        }

        protected override void LateUpdate() {
            PostAnimation();

            CheckUpright();
            Controllers.EndFrame();
        }

        #endregion

        #region Stop
        public void OnApplicationQuit() {
#if hLEAP
            leapTracker.StopTracker();
#endif
#if hNEURON
            neuronTracker.StopTracker();
#endif
#if hKINECT1
            kinect1.StopTracker();
#endif
#if hKINECT2
            kinect2.StopTracker();
#endif
#if hORBBEC
            astra.StopTracker();
#endif
#if hREALSENSE
            realsense.StopTracker();
#endif
#if hOPTITRACK
            optitrack.StopTracker();
#endif
        }
        #endregion

        #region Destroy

        #endregion

        public Vector3 up {
            get {
                return useGravity ? Vector3.up : transform.up;
            }
        }

        [HideInInspector]
        private Vector3 lastHumanoidPos;
        [HideInInspector]
        private float lastNeckHeight;
        [HideInInspector]
        private Vector3 lastHeadPosition;
        [HideInInspector]
        private Quaternion lastHeadRotation;
        [HideInInspector]
        private float lastHeadDirection;

        public event OnNewNeckHeight OnNewNeckHeightEvent;
        public delegate void OnNewNeckHeight(float neckHeight);

        private void CheckUpright() {
            if (OnNewNeckHeightEvent == null)
                return;

            GameObject realWorld = HumanoidControl.GetRealWorld(transform);

            // need to unscale the velocity, use localPosition ?
            float headVelocity = (headTarget.neck.target.transform.position - lastHeadPosition).magnitude / Time.deltaTime;
            float angularHeadVelocity = Quaternion.Angle(lastHeadRotation, headTarget.neck.target.transform.rotation) / Time.deltaTime;

            float deviation = Vector3.Angle(up, headTarget.transform.up);

            if (deviation < 4 && headVelocity < 0.02 && angularHeadVelocity < 3 && headVelocity + angularHeadVelocity > 0) {

                float neckHeight = (headTarget.transform.position.y - transform.position.y) / realWorld.transform.localScale.y;
                if (Mathf.Abs(neckHeight - lastNeckHeight) > 0.01F) {
                    lastNeckHeight = neckHeight;
                    if (lastNeckHeight > 0)
                        OnNewNeckHeightEvent(lastNeckHeight);
                }
            }
        }

        #region Calibration

        public override void SetStartPosition() {
#if hLEGACYXR
            Vector3 localNeckPosition;
            if (Passer.Tracking.UnityVRDevice.xrDevice == Passer.Tracking.UnityVRDevice.XRDeviceType.None || headTarget.unity.cameraTransform == null)
                localNeckPosition = headTarget.neck.target.transform.position - transform.position;
            else
                localNeckPosition = HeadMovements.CalculateNeckPositionFromEyes(headTarget.unity.cameraTransform.position, headTarget.unity.cameraTransform.rotation, -headTarget.neck2eyes) - transform.position;
            Vector3 delta = new Vector3(-localNeckPosition.x, 0, -localNeckPosition.z);
            AdjustTracking(delta);
#endif
        }

        /// <summary>Calibrates the tracking with the player</summary>
        public override void Calibrate() {
            Debug.Log("Calibrate");
            foreach (HumanoidTracker tracker in _trackers)
                tracker.Calibrate();

            if (startPosition == PawnControl.StartPosition.AvatarPosition)
                SetStartPosition();

            switch (scaling) {
                case ScalingType.SetHeightToAvatar:
                    SetTrackingHeightToAvatar();
                    break;
                case ScalingType.ScaleAvatarToTracking:
                    ScaleAvatarToTracking();
                    break;
                case ScalingType.ScaleTrackingToAvatar:
                    ScaleTrackingToAvatar();
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region Pose

        protected virtual void UpdatePose() {
            if (pose != null) {
                pose.Show(this);
                CopyRigToTargets();
            }
        }

        #region Pose Event

        public delegate void OnHumanoidPose(HumanoidPose pose);
        public event OnHumanoidPose onHumanoidPose;

        protected virtual void UpdatePoseEvent() {
            if (onHumanoidPose != null) {
                HumanoidPose pose = HumanoidPose.Retrieve(this, Time.time);
                onHumanoidPose.Invoke(pose);
            }
        }

        public class HumanoidPose {
            public ulong nwId;
            public int humanoidId;
            public float time;
            public Bone[] bones;
            public Blendshape[] blendshapes;

            public class Bone {
                public Tracking.Bone id;
                public Vector3 position;
                public byte positionConfidence;
                public Quaternion rotation;
                public byte rotationConfidence;
            }

            public class Blendshape {
                public string name;
                public int value;
            }

            public static HumanoidPose Retrieve(HumanoidControl humanoid, float poseTime) {
                HumanoidPose pose = new HumanoidPose() {
                    nwId = humanoid.nwId,
                    humanoidId = humanoid.humanoidId,
                    time = poseTime,
                };

                pose.bones = new Bone[3];
                pose.bones[0] = GetBonePose(humanoid.headTarget, Tracking.Bone.Head);
                pose.bones[1] = GetBonePose(humanoid.leftHandTarget, Tracking.Bone.LeftHand);
                pose.bones[2] = GetBonePose(humanoid.rightHandTarget, Tracking.Bone.RightHand);

                pose.blendshapes = null;

                return pose;
            }

            protected static Bone GetBonePose(HumanoidTarget target, Tracking.Bone boneId) {
                Bone poseBone = new Bone() {
                    id = boneId,
                    position = target.main.target.transform.position,
                    positionConfidence = (byte)(target.main.target.confidence.position * 255),
                    rotation = target.main.target.transform.rotation,
                    rotationConfidence = (byte)(target.main.target.confidence.rotation * 255),
                };
                return poseBone;
            }
        }

        #endregion

        #endregion

        #region Movement

        #region Input/API

        /// <summary>
        /// maximum forward speed in units(meters)/second
        /// </summary>
        //public float forwardSpeed = 1;
        /// <summary>
        /// maximum backward speed in units(meters)/second
        /// </summary>
        //public float backwardSpeed = 0.6F;
        /// <summary>
        /// maximum sideways speed in units(meters)/second
        /// </summary>
        //public float sidewardSpeed = 1;
        /// <summary>
        /// maximum acceleration in units(meters)/second/second
        /// value 0 = no maximum acceleration
        /// </summary>
        //public float maxAcceleration = 1;
        /// <summary>
        /// maximum rotational speed in degrees/second
        /// </summary>
        //public float rotationSpeed = 60;

        /// <summary>Moves the humanoid forward</summary>
        /// <param name="z">The distance in units(meters) to move forward.</param>
        public override void MoveForward(float z) {
            if (z > 0)
                z *= forwardSpeed;
            else
                z *= backwardSpeed;

            if (maxAcceleration > 0 && curProximitySpeed >= 1) {
                float accelerationStep = (z - targetVelocity.z);
                float maxAccelerationStep = maxAcceleration * Time.deltaTime;
                accelerationStep = Mathf.Clamp(accelerationStep, -maxAccelerationStep, maxAccelerationStep);
                z = targetVelocity.z + accelerationStep;
            }

            targetVelocity = new Vector3(targetVelocity.x, targetVelocity.y, z);
        }

        /// <summary>Moves the humanoid sideward</summary>
        /// <param name="x">The distance in units(meters) to move sideward.</param>
        public override void MoveSideward(float x) {
            x = x * sidewardSpeed;

            if (maxAcceleration > 0 && curProximitySpeed >= 1) {
                float accelerationStep = (x - targetVelocity.x);
                float maxAccelerationStep = maxAcceleration * Time.deltaTime;
                accelerationStep = Mathf.Clamp(accelerationStep, -maxAccelerationStep, maxAccelerationStep);
                x = targetVelocity.x + accelerationStep;
            }

            targetVelocity = new Vector3(x, targetVelocity.y, targetVelocity.z);
            //targetVelocity += Vector3.right * x;
        }


        /// <summary>Moves the humanoid</summary>
        public virtual void Move(Vector3 velocity) {
            targetVelocity = velocity;
        }

        public override void MoveWorldVector(Vector3 v) {
            targetVelocity = hipsTarget.transform.InverseTransformDirection(v);
        }

        public override void Stop() {
            targetVelocity = Vector3.zero;
        }

        /// <summary>Rotate the humanoid</summary>
        /// Rotates the humanoid along the Y axis
        /// <param name="angularSpeed">The speed in degrees per second</param>
        public override void Rotate(float angularSpeed) {
            angularSpeed *= Time.deltaTime * rotationSpeed;
            //transform.RotateAround(hipsTarget.transform.position, hipsTarget.transform.up, angularSpeed);
            transform.RotateAround(headTarget.transform.position, hipsTarget.transform.up, angularSpeed);
        }

        /// <summary>Set the rotation angle along the Y axis</summary>
        public override void Rotation(float yAngle) {
            Vector3 angles = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(angles.x, yAngle, angles.z);
        }

        /// <summary>Quickly moves this humanoid to the given position</summary>
        /// <param name="targetPosition">The position to move to</param>
        public void Dash(Vector3 targetPosition) {
            MoveTo(targetPosition, MovementType.Dash);
        }

        /// <summary>Teleports this humanoid to the given position</summary>
        /// <param name="targetPosition">The position to move to</param>
        public void Teleport(Vector3 targetPosition) {
            MoveTo(targetPosition, MovementType.Teleport);
        }

        /// <summary>Teleports the humanoid in the forward direction</summary>
        /// <param name="distance">The distance to teleport</param>
        /// The forward direction is determined by the hips target forward.
        public void TeleportForward(float distance = 1) {
            MoveTo(transform.position + hipsTarget.transform.forward * distance);
        }

        /// <summary>Moves the humanoid to the given position</summary>
        /// <param name="movementType">The type of movement to use</param>
        public void MoveTo(Vector3 position, MovementType movementType = MovementType.Teleport) {
            switch (movementType) {
                case MovementType.Teleport:
                    TransformMovements.Teleport(transform, position);
                    break;
                case MovementType.Dash:
                    StartCoroutine(TransformMovements.DashCoroutine(transform, position));
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Lets the Humanoid jump up with the given take off velocity
        /// </summary>
        /// <param name="takeoffVelocity">The vertical velocity to start the jump</param>
        public override void Jump(float takeoffVelocity) {
            if (ground == null)
                return;

            float verticalVelocity = velocity.y + takeoffVelocity;
            gravitationalVelocity -= new Vector3(0, -verticalVelocity, 0);
            float y = targetVelocity.y + verticalVelocity;
            targetVelocity = new Vector3(targetVelocity.x, y, targetVelocity.z);

        }

        #endregion

        #region Checks

        [HideInInspector]
        public Vector3 targetVelocity;
        //[HideInInspector]
        //public Vector3 velocity;
        [HideInInspector]
        public Vector3 acceleration;
        [HideInInspector]
        public float turningVelocity;

        protected override void CalculateMovement() {
            Vector3 translationVector = CheckMovement();
            transform.position += translationVector * Time.fixedDeltaTime;
            if (targetVelocity.y != 0) {
                if (ground == null)
                    targetVelocity.y = 0;
                transform.position += targetVelocity.y * Vector3.up * Time.fixedDeltaTime;
            }
        }

        private float curProximitySpeed = 1;

        public Vector3 CheckMovement() {
            Vector3 newVelocity = targetVelocity;

            if (proximitySpeed) {
                curProximitySpeed = CalculateProximitySpeed(bodyCapsule, curProximitySpeed);
                newVelocity *= curProximitySpeed;
            }

            Vector3 inputDirection = hipsTarget.transform.TransformDirection(newVelocity);

            if (physics && (collided || (!proximitySpeed && triggerEntered))) {
                float angle = Vector3.Angle(inputDirection, hitNormal);
                if (angle > 90) {
                    targetVelocity = Vector3.zero;
                    return Vector3.zero;
                }
            }

            return inputDirection;
        }

        private float CalculateProximitySpeed(CapsuleCollider cc, float curProximitySpeed) {
            if (triggerEntered) {
                if (cc.radius > 0.25f && targetVelocity.magnitude > 0)
                    curProximitySpeed = CheckDecreaseProximitySpeed(cc, curProximitySpeed);
            }
            else {
                if (curProximitySpeed < 1 && targetVelocity.magnitude > 0)
                    curProximitySpeed = CheckIncreaseProximitySpeed(cc, curProximitySpeed);
            }
            return curProximitySpeed;
        }

        private float CheckDecreaseProximitySpeed(CapsuleCollider cc, float curProximitySpeed) {
            RaycastHit[] hits = Physics.CapsuleCastAll(hipsTarget.transform.position + (cc.radius - 0.8f) * Vector3.up, hipsTarget.transform.position - (cc.radius - 1.2f) * Vector3.up, cc.radius - 0.05f, velocity, 0.04f);
            bool collision = false;
            for (int i = 0; i < hits.Length && collision == false; i++) {
                if (!IsMyRigidbody(hits[i].rigidbody)) {
                    collision = true;
                    cc.radius -= 0.05f / proximitySpeedRate;
                    cc.height += 0.05f / proximitySpeedRate;
                    curProximitySpeed = EaseIn(1, (-0.8f), 1 - cc.radius, 0.75f);
                }
            }
            return curProximitySpeed;
        }

        private float CheckIncreaseProximitySpeed(CapsuleCollider cc, float curProximitySpeed) {
            Vector3 capsuleCenter = hipsTarget.hips.bone.transform.position + cc.center;
            Vector3 offset = ((cc.height - cc.radius) / 2) * Vector3.up;
            Vector3 point1 = capsuleCenter + offset;
            Vector3 point2 = capsuleCenter - offset;
            Collider[] results = Physics.OverlapCapsule(point1, point2, cc.radius + 0.05F);

            /*
            RaycastHit[] hits = Physics.CapsuleCastAll(hipsTarget.transform.position + (cc.radius - 0.75f) * Vector3.up, hipsTarget.transform.position - (cc.radius - 1.15f) * Vector3.up, cc.radius, inputDirection, 0.04f);
            bool collision = false;
            for (int i = 0; i < hits.Length && collision == false; i++) {
                if (hits[i].rigidbody == null) {
                    collision = true;
                }
            }
            */

            bool collision = false;
            for (int i = 0; i < results.Length; i++) {
                if (!results[i].isTrigger && !IsMyRigidbody(results[i].attachedRigidbody)) {
                    //results[i].attachedRigidbody != humanoidRigidbody && results[i].attachedRigidbody != characterRigidbody &&
                    //results[i].attachedRigidbody != headTarget.headRigidbody &&
                    //results[i].attachedRigidbody != leftHandTarget.handRigidbody && results[i].attachedRigidbody != rightHandTarget.handRigidbody
                    //) {

                    collision = true;
                }
            }

            if (collision == false) {
                cc.radius += 0.05f / proximitySpeedRate;
                cc.height -= 0.05f / proximitySpeedRate;
                curProximitySpeed = EaseIn(1, (-0.8f), 1 - cc.radius, 0.75f);
            }
            return curProximitySpeed;
        }

        private static float EaseIn(float start, float distance, float elapsedTime, float duration) {
            // clamp elapsedTime so that it cannot be greater than duration
            elapsedTime = (elapsedTime > duration) ? 1.0f : elapsedTime / duration;
            return distance * elapsedTime * elapsedTime + start;
        }

        #endregion

        #region Collisions

        public bool triggerEntered;
        public bool collided;
        public GameObject collidedWith;
        public Vector3 hitNormal = Vector3.zero;

        [HideInInspector]
        public Rigidbody humanoidRigidbody;
        [HideInInspector]
        public Rigidbody characterRigidbody;
        [HideInInspector]
        public CapsuleCollider bodyCapsule;
        [HideInInspector]
        public CapsuleCollider bodyCollider;
        [HideInInspector]
        private readonly float colliderRadius = 0.15F;

        private void AddCharacterColliders() {
            if (avatarRig == null || hipsTarget.hips.bone.transform == null || isRemote || !physics)
                return;

            Transform collidersTransform = hipsTarget.hips.bone.transform.Find("Character Colliders");
            if (collidersTransform != null)
                return;

            GameObject collidersObject = hipsTarget.hips.bone.transform.gameObject;

            HumanoidCollisionHandler collisionHandler = collidersObject.AddComponent<HumanoidCollisionHandler>();
            collisionHandler.humanoid = this;

            characterRigidbody = collidersObject.GetComponent<Rigidbody>();
            if (characterRigidbody == null)
                characterRigidbody = collidersObject.AddComponent<Rigidbody>();
            if (characterRigidbody != null) {
                characterRigidbody.mass = 1;
                characterRigidbody.useGravity = false;
                characterRigidbody.isKinematic = true;
            }

            if (generateColliders) {
                float avatarHeight = avatarNeckHeight * 8 / 7;
                Vector3 colliderCenter = Vector3.up * (stepOffset / 2);

                CheckBodyCollider(collidersObject);

                GameObject bodyCapsuleObject;
                Transform bodyCapsuleTransform = transform.Find("Body Capsule");
                if (bodyCapsuleTransform != null)
                    bodyCapsuleObject = bodyCapsuleTransform.gameObject;
                else
                    bodyCapsuleObject = new GameObject("Body Capsule");

                bodyCapsuleObject.tag = this.gameObject.tag;
                bodyCapsuleObject.layer = this.gameObject.layer;
                bodyCapsuleObject.transform.parent = this.transform; //collidersObject.transform;
                bodyCapsuleObject.transform.position = hipsTarget.hips.bone.transform.position;
                float hipsYangle = hipsTarget.hips.bone.targetRotation.eulerAngles.y;
                bodyCapsuleObject.transform.rotation = Quaternion.AngleAxis(hipsYangle, up);
                bodyCapsule = bodyCapsuleObject.GetComponent<CapsuleCollider>();
                if (bodyCapsule == null)
                    bodyCapsule = bodyCapsuleObject.AddComponent<CapsuleCollider>();

                // We use this only for the capsulecast when colliding
                bodyCapsule.enabled = false;

                if (bodyCapsule != null) {
                    bodyCapsule.isTrigger = true;
                    if (proximitySpeed) {
                        bodyCapsule.height = 0.80F;
                        bodyCapsule.radius = 1F;
                    }
                    else {
                        bodyCapsule.height = avatarHeight - stepOffset;
                        bodyCapsule.radius = colliderRadius;
                    }
                    bodyCapsule.center = colliderCenter;
                }
            }

            humanoidRigidbody = gameObject.GetComponent<Rigidbody>();
            if (humanoidRigidbody == null)
                humanoidRigidbody = gameObject.AddComponent<Rigidbody>();
            if (humanoidRigidbody != null) {
                humanoidRigidbody.mass = 1;
                humanoidRigidbody.useGravity = false;
                humanoidRigidbody.isKinematic = true;
            }
        }

        private void CheckBodyCollider(GameObject collidersObject) {
            // Important explanation!
            // The humanoid colliders need to be trigger colliders
            // because they will detect both static colliders and rigibodies
            // Normal colliders on kinematic rigidbodies only detect
            // rigidbodies reliably, static colliders are not detected reliably.

            HumanoidTarget.TargetedBone spineBone = hipsTarget.spine;
            if (spineBone == null)
                spineBone = hipsTarget.hips;


            // Add gameobject with target rotation to ensure the direction of the capsule
            GameObject spineColliderObject = new GameObject("Spine Collider") {
                tag = this.gameObject.tag,
                layer = this.gameObject.layer
            };
            spineColliderObject.transform.parent = spineBone.bone.transform;
            spineColliderObject.transform.localPosition = Vector3.zero;
            float hipsYangle = hipsTarget.hips.bone.targetRotation.eulerAngles.y;
            spineColliderObject.transform.rotation = Quaternion.AngleAxis(hipsYangle, up);

            bodyCollider = spineColliderObject.AddComponent<CapsuleCollider>();
            bodyCollider.isTrigger = true;
            bodyCollider.height = avatarNeckHeight - (hipsTarget.hips.bone.transform.position.y - avatarRig.transform.position.y) + 0.1F;
            bodyCollider.radius = colliderRadius - 0.05F;
            bodyCollider.center = new Vector3(0, bodyCollider.height / 2, 0);

            HumanoidTarget.BoneTransform leftUpperLeg = leftFootTarget.upperLeg.bone;
            // Add gameobject with target rotation to ensure the direction of the capsule
            GameObject leftColliderObject = new GameObject("Left Leg Collider") {
                tag = this.gameObject.tag,
                layer = this.gameObject.layer
            };
            leftColliderObject.transform.parent = leftUpperLeg.transform;
            leftColliderObject.transform.localPosition = Vector3.zero;
            leftColliderObject.transform.rotation = leftFootTarget.upperLeg.bone.targetRotation;

            CapsuleCollider leftUpperLegCollider = leftColliderObject.AddComponent<CapsuleCollider>();
            leftUpperLegCollider.isTrigger = true;
            leftUpperLegCollider.height = leftUpperLeg.length;
            leftUpperLegCollider.radius = 0.08F;
            leftUpperLegCollider.center = new Vector3(0, -leftUpperLeg.length / 2, 0);

            HumanoidTarget.BoneTransform rightUpperLeg = rightFootTarget.upperLeg.bone;
            // Add gameobject with target rotation to ensure the direction of the capsule
            GameObject rightColliderObject = new GameObject("Right Leg Collider") {
                tag = this.gameObject.tag,
                layer = this.gameObject.layer
            };
            rightColliderObject.transform.parent = rightUpperLeg.transform;
            rightColliderObject.transform.localPosition = Vector3.zero;
            rightColliderObject.transform.rotation = rightFootTarget.upperLeg.bone.targetRotation;

            CapsuleCollider rightUpperLegCollider = rightColliderObject.AddComponent<CapsuleCollider>();
            rightUpperLegCollider.isTrigger = true;
            rightUpperLegCollider.height = rightUpperLeg.length;
            rightUpperLegCollider.radius = 0.08F;
            rightUpperLegCollider.center = new Vector3(0, -rightUpperLeg.length / 2, 0);
        }

        private void DetermineCollision() {
            if (proximitySpeed) {
                //float angle = Vector3.Angle(hitNormal, targetVelocity);
                collided = (triggerEntered && bodyCapsule.radius <= 0.25f);
            }
            else
                collided = triggerEntered;

            if (!collided)
                hitNormal = Vector3.zero;
        }

        public bool IsMyRigidbody(Rigidbody rigidbody) {
            return
                rigidbody != null && (
                rigidbody == humanoidRigidbody ||
                rigidbody == characterRigidbody ||
                rigidbody == headTarget.headRigidbody ||
                rigidbody == leftHandTarget.handRigidbody ||
                rigidbody == rightHandTarget.handRigidbody
                );
        }

        #endregion

        #region Ground

        protected override void CheckGround() {
            CheckGrounded();
            CheckGroundMovement();
        }

        // Leg Length Correction is used to get the feet on the ground when the target bone length of the legs
        // is bigger than the bone length of the avatar.
        // This normally does not happen, but when using animations, the target bones lengths are overridden.
        // So probably this setting should only be used when using animations.
        public bool useLegLengthCorrection = false;
        private float legLengthCorrection = 0;
        protected override void CheckGrounded() {
            Vector3 footBase = GetHumanoidPosition();
            //footBase += legLengthBias * Vector3.up;
            // This can lead to the humanoid fall through the ground in some cases.
            // Maybe this is caused by the feet moving forward animation, but I am not sure yet.
            // Workaround is to use
            // footBase = transform.position
            // But this will lead to floating avatars when the user's position is not at the origin

            Vector3 groundNormal;
            float distance = GetDistanceToGroundAt(footBase, stepOffset, out ground, out groundNormal);
            distance -= legLengthCorrection;
            if (distance > 0.01F) {
                gravitationalVelocity = Vector3.zero;
                if (!isRemote)
                    transform.Translate(0, distance, 0);
            }
            else if (distance < -0.02F) {
                ground = null;
                if (!leftHandTarget.GrabbedStaticObject() && !rightHandTarget.GrabbedStaticObject()) {
                    if (useGravity)
                        Fall();
                }
            }

            if (useLegLengthCorrection && ground != null) {
                Vector3 footBoneDistance = Vector3.zero;
                if (leftFootTarget.ground != null)
                    footBoneDistance = leftFootTarget.foot.bone.transform.position - leftFootTarget.foot.target.transform.position;
                else if (rightFootTarget.ground != null)
                    footBoneDistance = rightFootTarget.foot.bone.transform.position - rightFootTarget.foot.target.transform.position;

                legLengthCorrection = 0.99F * legLengthCorrection + 0.01F * footBoneDistance.y;
            }
        }

        public float GetDistanceToGroundAt(Vector3 position, float maxDistance) {
            Transform _ground;
            Vector3 _normal;
            return GetDistanceToGroundAt(position, maxDistance, out _ground, out _normal);
        }

        public override float GetDistanceToGroundAt(Vector3 position, float maxDistance, out Transform ground, out Vector3 normal) {
            normal = up;

            Vector3 rayStart = position + normal * maxDistance;
            Vector3 rayDirection = -normal;
            //Debug.DrawRay(rayStart, rayDirection * maxDistance * 2, Color.magenta);

            int layerMask = 0;
            for (int layer = 0; layer < 32; layer++) {
                if (!Physics.GetIgnoreLayerCollision(gameObject.layer, layer)) {
                    layerMask |= 1 << layer;
                }
            }
            RaycastHit[] hits = Physics.RaycastAll(rayStart, rayDirection, maxDistance * 2, layerMask, QueryTriggerInteraction.Ignore);

            if (hits.Length == 0) {
                ground = null;
                return -maxDistance;
            }

            int closestHit = 0;
            bool foundClosest = false;
            for (int i = 0; i < hits.Length; i++) {
                if ((hits[i].rigidbody == null || hits[i].rigidbody != characterRigidbody || isRemote) &&    // remote humanoids do not have a characterRigidbody
                    hits[i].transform != headTarget.transform &&
                    hits[i].distance <= hits[closestHit].distance) {
                    closestHit = i;
                    foundClosest = true;
                }
            }
            if (!foundClosest) {
                ground = null;
                return -maxDistance;
            }

            ground = hits[closestHit].transform;
            normal = hits[closestHit].normal;
            float distance = maxDistance - hits[closestHit].distance;
            return distance;
        }

        protected override void CheckGroundMovement() {
            if (ground == null) {
                lastGround = null;
                lastGroundPosition = Vector3.zero;
                lastGroundAngle = 0;
                return;
            }

            if (ground == lastGround) {
                Vector3 groundTranslation = ground.position - lastGroundPosition;
                groundVelocity = groundTranslation / Time.fixedDeltaTime;
                //Debug.Log(ground.position.x  + " " +  lastGroundPosition.x + " " + Time.fixedDeltaTime + " " + groundVelocity.x);

                float groundRotation = ground.eulerAngles.y - lastGroundAngle;
                groundAngularVelocity = groundRotation / Time.fixedDeltaTime;

                if (this.transform.root != ground.root) {
                    transform.Translate(groundTranslation, Space.World);
                    transform.RotateAround(ground.position, Vector3.up, groundRotation);
                }
            }

            lastGround = ground;
            lastGroundPosition = ground.position;
            lastGroundAngle = ground.eulerAngles.y;
        }

        #endregion

        #region Body Pull

        protected virtual void CheckBodyPull() {
            if (!bodyPull)
                return;

            bool leftGrabbedStatic = leftHandTarget.GrabbedStaticObject();
            bool rightGrabbedStatic = rightHandTarget.GrabbedStaticObject();

            if (leftGrabbedStatic && rightGrabbedStatic)
                KinematicBodyControlTwoHanded();
            else if (leftGrabbedStatic)
                KinematicBodyControlOneHanded(leftHandTarget);
            else if (rightGrabbedStatic)
                KinematicBodyControlOneHanded(rightHandTarget);
        }

        protected void KinematicBodyControlOneHanded(HandTarget handTarget) {
            // Translation
            Vector3 pullVector = handTarget.hand.bone.transform.position - handTarget.hand.target.transform.position;

            TranslateBody(this.transform, pullVector);
        }

        protected void KinematicBodyControlTwoHanded() {
            // Translation
            Vector3 leftPullVector = leftHandTarget.hand.bone.transform.position - leftHandTarget.hand.target.transform.position;
            Vector3 rightPullVector = rightHandTarget.hand.bone.transform.position - rightHandTarget.hand.target.transform.position;
            Vector3 pullVector = (leftPullVector + rightPullVector) / 2;

            TranslateBody(this.transform, pullVector);
        }

        private void TranslateBody(Transform transform, Vector3 translation) {
            var collisionAngle = Vector3.Angle(translation, hitNormal);
            if (!collided || (collided && collisionAngle <= 90f))
                transform.Translate(translation, Space.World);
        }

        #endregion

        [HideInInspector]
        private float lastTime;

        private void CalculateVelocityAcceleration() {
            if (lastTime > 0) {
                float deltaTime = Time.time - lastTime;

                Vector3 localVelocity = -groundVelocity;
                if (avatarRig != null) {
                    Vector3 headTranslation = headTarget.neck.target.transform.position - lastHeadPosition;
                    if (headTranslation.magnitude == 0)
                        // We assume we did not get an update - needs to be improved though
                        // Especially with networking, position updates occur less frequent than frame updates
                        return;
                    Vector3 localHeadTranslation = headTarget.neck.target.transform.InverseTransformDirection(headTranslation);
                    localVelocity += localHeadTranslation / deltaTime;

                    float headDirection = headTarget.neck.target.transform.eulerAngles.y - lastHeadDirection;
                    float localHeadDirection = Angle.Normalize(headDirection);
                    turningVelocity = (localHeadDirection / deltaTime) / 360;
                }

                // Remote humanoids will receive the velocity from the network
                if (!isRemote) {
                    Vector3 rootTranslation = transform.position - lastHumanoidPos;
                    Vector3 rootVelocity = rootTranslation / deltaTime;
                    velocity = velocity * 0.8F + (rootVelocity + localVelocity) * 0.2F;
                }
            }
            lastTime = Time.time;

            lastHumanoidPos = transform.position;
            lastHeadPosition = headTarget.neck.target.transform.position;
            lastHeadRotation = headTarget.neck.target.transform.rotation;
            lastHeadDirection = headTarget.neck.target.transform.eulerAngles.y;
        }

        #region Animation
        public string animatorParameterForward;
        public string animatorParameterSideward;
        public string animatorParameterRotation;
        public string animatorParameterHeight;

        // needed for the Editor
        public int animatorParameterForwardIndex;
        public int animatorParameterSidewardIndex;
        public int animatorParameterRotationIndex;
        public int animatorParameterHeightIndex;

        private void UpdateAnimation() {
            if (targetsRig.runtimeAnimatorController != null) {
                if (animatorParameterForward != null && animatorParameterForward != "") {
                    targetsRig.SetFloat(animatorParameterForward, velocity.z);
                }
                if (animatorParameterSideward != null && animatorParameterSideward != "") {
                    targetsRig.SetFloat(animatorParameterSideward, velocity.x);
                }
                if (animatorParameterRotation != null && animatorParameterRotation != "") {
                    targetsRig.SetFloat(animatorParameterRotation, turningVelocity);
                }

                if (animatorParameterHeight != null && animatorParameterHeight != "") {
                    float relativeHeadHeight = headTarget.neck.target.transform.position.y - avatarNeckHeight;
                    targetsRig.SetFloat(animatorParameterHeight, relativeHeadHeight);
                }
            }
        }

        private void PreAnimation() {
            if (!animatorEnabled || targetsRig.runtimeAnimatorController == null)
                return;

            headTarget.SaveTransform();
            leftHandTarget.SaveTransform();
            rightHandTarget.SaveTransform();
        }

        private void PostAnimation() {

            if (animatorEnabled && animatorController != null) {
                // copy animator root motion to the humanoid
                if (targetsRig.GetCurrentAnimatorClipInfoCount(0) > 0) {
                    this.transform.position = targetsRig.transform.position;
                    this.transform.rotation = targetsRig.transform.rotation;

                    // As targets rig is probably a child of this.transform,
                    // We need to restore the position/rotation of the targetsRig.
                    targetsRig.transform.position = this.transform.position;
                    targetsRig.transform.rotation = this.transform.rotation;
                }

                // tracking should override animation pose
                if (headTarget.head.target.confidence.position > 0.2F)
                    headTarget.head.target.transform.position = headTarget.savedPosition;
                if (headTarget.head.target.confidence.rotation > 0.2F)
                    headTarget.head.target.transform.rotation = headTarget.savedRotation;

                if (leftHandTarget.hand.target.confidence.position > 0.2F)
                    leftHandTarget.hand.target.transform.position = leftHandTarget.savedPosition;
                if (leftHandTarget.hand.target.confidence.rotation > 0.2F)
                    leftHandTarget.hand.target.transform.rotation = leftHandTarget.savedRotation;

                if (rightHandTarget.hand.target.confidence.position > 0.2F)
                    rightHandTarget.hand.target.transform.position = rightHandTarget.savedPosition;
                if (rightHandTarget.hand.target.confidence.rotation > 0.2F)
                    rightHandTarget.hand.target.transform.rotation = rightHandTarget.savedRotation;

            }
        }

        public void SetAnimationParameterBool(string parameterName, bool boolValue) {
            targetsRig.SetBool(parameterName, boolValue);
        }

        public void SetAnimationParameterFloat(string parameterName, float floatValue) {
            targetsRig.SetFloat(parameterName, floatValue);
        }

        public void SetAnimationParameterInt(string parameterName, int intValue) {
            targetsRig.SetInteger(parameterName, intValue);
        }

        public void SetAnimationParameterTrigger(string parameterName) {
            targetsRig.SetTrigger(parameterName);
        }

        #endregion

        [HideInInspector]
        private float lastLocalHipY;
        protected override void Fall() {
            gravitationalVelocity += Physics.gravity * Time.deltaTime;

            if (hipsTarget.hips.bone.transform == null)
                return;

            // Only fall when the avatar is not moving vertically
            // This to prevent physical falling interfering with virtual falling
            Vector3 hipsPosition = hipsTarget.hips.bone.transform != null ? hipsTarget.hips.bone.transform.position : hipsTarget.hips.target.transform.position;
            float localHipY = hipsPosition.y - transform.position.y;
            float hipsTranslationY = localHipY - lastLocalHipY;
            if (Mathf.Abs(hipsTranslationY) < 0.01F)
                transform.Translate(gravitationalVelocity * Time.deltaTime);

            lastLocalHipY = localHipY;
        }

        #endregion Movement

        /// <summary>Gets the Real World GameObject for this Humanoid</summary>
        /// <param name="transform">The root transform of the humanoid</param>
        public static GameObject GetRealWorld(Transform transform) {
            Transform realWorldTransform = transform.Find("Real World");
            if (realWorldTransform != null)
                return realWorldTransform.gameObject;

            GameObject realWorld = new GameObject("Real World");
            realWorld.transform.parent = transform;
            realWorld.transform.localPosition = Vector3.zero;
            realWorld.transform.localRotation = Quaternion.identity;
            return realWorld;
        }

        //public Transform GetRealWorld() {
        //    GameObject realWorld = GetRealWorld(this.transform);
        //    return realWorld.transform;
        //}

        /// <summary>Tries to find a tracker GameObject by name</summary>
        /// <param name="realWorld">The Real World GameOject in which the tracker should be</param>
        /// <param name="trackerName">The name of the tracker GameObject to find</param>
        public static GameObject FindTrackerObject(GameObject realWorld, string trackerName) {
            Transform rwTransform = realWorld.transform;

            for (int i = 0; i < rwTransform.childCount; i++) {
                if (rwTransform.GetChild(i).name == trackerName)
                    return rwTransform.GetChild(i).gameObject;
            }
            return null;
        }

        /// <summary>
        /// The humanoid can be on a differentlocation than the humanoid.transform
        /// because the tracking can move the humanoid around independently
        /// This function takes this into account
        /// </summary>
        /// <returns>The position of the humanoid</returns>
        public Vector3 GetHumanoidPosition() {
            Vector3 footPosition = (leftFootTarget.foot.target.transform.position + rightFootTarget.foot.target.transform.position) / 2;
            Vector3 footBase = new Vector3(footPosition.x, transform.position.y, footPosition.z);
            return footBase;
        }
        //public Vector3 GetHumanoidPosition2() {
        //    Vector3 footPosition = (leftFootTarget.foot.bone.transform.position + rightFootTarget.foot.bone.transform.position) / 2;
        //    float lowestFoot = Mathf.Min(leftFootTarget.foot.bone.transform.position.y, rightFootTarget.foot.bone.transform.position.y);
        //    Vector3 footBase = new Vector3(footPosition.x, lowestFoot - leftFootTarget.soleThicknessFoot, footPosition.z);
        //    return footBase;
        //}
        //public Vector3 GetHumanoidPosition3() {
        //    Vector3 hipsPosition = hipsTarget.hips.bone.transform.position;
        //    Vector3 footPosition = hipsPosition - up * (leftFootTarget.upperLeg.bone.length + leftFootTarget.lowerLeg.bone.length + leftFootTarget.soleThicknessFoot);
        //    return footPosition;
        //}
        //public Vector3 GetHumanoidPosition4() {
        //    Vector3 neckPosition = headTarget.neck.bone.transform.position;
        //    Vector3 footBase = neckPosition - up * avatarNeckHeight;
        //    return footBase;
        //}
        //public Vector3 GetHumanoidPosition5() {
        //    Vector3 footPosition = (leftFootTarget.foot.target.transform.position + rightFootTarget.foot.target.transform.position) / 2;
        //    float lowestFoot = Mathf.Min(leftFootTarget.foot.target.transform.position.y, rightFootTarget.foot.target.transform.position.y);
        //    Vector3 footBase = new Vector3(footPosition.x, lowestFoot - leftFootTarget.soleThicknessFoot, footPosition.z);
        //    return footBase;
        //}


        #region Humanoid store

        private static HumanoidControl[] _allHumanoids = new HumanoidControl[0];
        public static HumanoidControl[] allHumanoids {
            get { return _allHumanoids; }
        }

        public delegate void OnNewHumanoid(HumanoidControl humanoid);
        public static event OnNewHumanoid onNewHumanoid;

        public void AddHumanoid() {
            if (HumanoidExists(this))
                return;

            ExtendHumanoids(this);

            humanoidNetworking = HumanoidNetworking.GetLocalHumanoidNetworking();
            if (!isRemote && humanoidNetworking != null)
                humanoidNetworking.InstantiateHumanoid(this);

            if (onNewHumanoid != null)
                onNewHumanoid(this);
        }

        private static void ExtendHumanoids(HumanoidControl humanoid) {
            HumanoidControl[] newAllHumanoids = new HumanoidControl[_allHumanoids.Length + 1];
            for (int i = 0; i < _allHumanoids.Length; i++) {
                newAllHumanoids[i] = _allHumanoids[i];
            }
            _allHumanoids = newAllHumanoids;
            _allHumanoids[_allHumanoids.Length - 1] = humanoid;
            humanoid.humanoidId = _allHumanoids.Length - 1;
        }

        private void RemoveHumanoid() {
            if (!HumanoidExists(this))
                return;

            if (!isRemote && humanoidNetworking != null)
                humanoidNetworking.DestroyHumanoid(this);

            RemoveHumanoid(this);

        }

        private static void RemoveHumanoid(HumanoidControl humanoid) {
            HumanoidControl[] newAllHumanoids = new HumanoidControl[_allHumanoids.Length - 1];
            int j = 0;
            for (int i = 0; i < _allHumanoids.Length; i++) {
                if (_allHumanoids[i] != humanoid) {
                    newAllHumanoids[j] = _allHumanoids[i];
                    j++;
                }
            }
            _allHumanoids = newAllHumanoids;
        }

        private static bool HumanoidExists(HumanoidControl humanoid) {
            for (int i = 0; i < _allHumanoids.Length; i++) {
                if (humanoid == _allHumanoids[i])
                    return true;
            }
            return false;
        }

        public static HumanoidControl[] AllVisibleHumanoids(Camera camera) {
            HumanoidControl[] visibleHumanoids = new HumanoidControl[_allHumanoids.Length];

            int j = 0;
            for (int i = 0; i < _allHumanoids.Length; i++) {
                if (_allHumanoids[i].IsVisible(camera)) {
                    visibleHumanoids[j] = _allHumanoids[i];
                    j++;
                }
            }

            HumanoidControl[] allVisibleHumanoids = new HumanoidControl[j];
            for (int i = 0; i < j; i++) {
                allVisibleHumanoids[i] = visibleHumanoids[i];
            }
            return allVisibleHumanoids;
        }

        public bool IsVisible(Camera camera) {
            Vector3 screenPosition = camera.WorldToScreenPoint(headTarget.transform.position);
            return (screenPosition.x > 0 && screenPosition.x < camera.pixelWidth &&
                screenPosition.y > 0 && screenPosition.y < camera.pixelHeight);
        }

        #endregion
    }
}