using System.Collections;
using UnityEngine;

namespace Passer.Pawn {

    /// <summary>
    /// Controls players using controller input
    /// </summary>
    public class PawnControl : MonoBehaviour {

        #region Settings

        /// <summary>If true, real world objects like controllers and cameras are shown in the scene</summary>
        public bool showRealObjects = true;

        /// <summary>Enables controller physics and collisions during walking.</summary>
        public bool physics = true;
        /// <summary>If there is not static object below the feet of the avatar the avatar will fall down until it reaches solid ground</summary>
        public bool useGravity = true;
        /// <summary>The maximum height of objects of the ground which do not stop the humanoid</summary>
        public float stepOffset = 0.3F;
        /// <summary>Will move the pawn position when grabbing handles on static objects</summary>
        public bool bodyPull = false;
        public void set_bodyPull(bool value) {
            bodyPull = value;
        }

        /// <summary>Reduces the walking speed of the humanoid when in the neighbourhood of objects to reduce motion sickness.</summary>
        public bool proximitySpeed = false;
        /// <summary>The amount of influence of the proximity speed. 1=No influence, 0 = Maximum</summary>
        public float proximitySpeedRate = 0.8f;

        /// <summary>Types of startposition for the Pawn</summary>
        public enum StartPosition {
            AvatarPosition,
            PlayerPosition
        }
        /// <summary>The start position of the humanoid</summary>
        public StartPosition startPosition = StartPosition.AvatarPosition;

        /// <summary>
        /// Options for matching the player to the game character
        /// </summary>
        public enum MatchingType {
            None,
            SetHeightToCharacter,
        }
        /// <summary>Scale Tracking to Avatar scales the tracking input to match the size of the avatar</summary>
        [SerializeField]
        protected MatchingType avatarMatching = MatchingType.SetHeightToCharacter;
        /// <summary>Perform a calibration when the scene starts</summary>
        public bool calibrateAtStart = false;
        /// <summary>Sets the Don't Destoy On Load such that the pawn survives scene changes</summary>
        public bool dontDestroyOnLoad = false;

        // Prefab only
        public bool disconnectInstances = false;

        #endregion

        protected TraditionalDevice traditionalInput = new TraditionalDevice();
        public GameControllers gameController;

        #region Init

        protected virtual void Awake() {
            if (dontDestroyOnLoad)
                DontDestroyOnLoad(this.transform.root);

            characterController = GetCharacter();

            InitTargets();
            NewTargetComponents();

            DetermineNeckHeight();

            InitTrackers();
            StartTrackers();
            StartTargets();

            StartSensors();

            traditionalInput.SetControllerID(0);
        }

        #endregion

        #region Character

        protected CharacterController characterController;

        public virtual CharacterController GetCharacter() {
            if (characterController != null && characterController.enabled && characterController.gameObject.activeInHierarchy) {
                // We already have a good characterController
                return characterController;
            }

            CharacterController[] characterControllers = GetComponentsInChildren<CharacterController>();
            if (characterControllers != null && characterControllers.Length > 0)
                return characterControllers[0];

            return null;
        }

        private float _neckHeight;
        private void DetermineNeckHeight() {
            if (_headTarget != null)
                _neckHeight = _headTarget.transform.localPosition.y;
            else
                _neckHeight = height * 0.75F;
        }
        public virtual float neckHeight {
            get {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DetermineNeckHeight();
#endif
                return _neckHeight;
            }
        }

        public virtual float height {
            get {
                if (characterController != null) {
                    return characterController.height;
                }
                else if (_headTarget != null)
                    return _headTarget.transform.localPosition.y * 1.333F;
                else
                    return 2;
            }
        }

        public virtual float radius {
            get {
                characterController = GetComponent<CharacterController>();
                if (characterController != null)
                    return characterController.radius;
                else
                    return 0.2F;
            }
        }

        protected virtual float GetCharacterSoleThickness() {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, 1, Physics.DefaultRaycastLayers))
                return hit.distance;

            return 0;
        }

        protected virtual void UpdateCharacter() {
            if (characterController == null)
                return;

            characterController.detectCollisions = false;
            //float cameraYAngle = cameraTarget.unity.sensorTransform.eulerAngles.y;
            //characterController.transform.rotation = Quaternion.AngleAxis(cameraYAngle, Vector3.up);
        }

        #endregion

        #region Targets

        /// <summary>
        /// Target type
        /// </summary>
        public enum TargetId {
            HeadTarget,
            LeftHandTarget,
            RightHandTarget,
        }

        /// <summary>
        /// The camera of the pawn.
        /// When using VR this is the HMD
        /// </summary>
        public PawnHead headTarget {
            get {
                return _headTarget;
            }
        }
        [SerializeField]
        protected PawnHead _headTarget;

        /// <summary>
        /// The left (side side of) controller
        /// </summary>
        /// /// For game controllers this includes the buttons and controls operated by the left hand.
        public PawnHand leftHandTarget {
            get { return _leftHandTarget; }
        }
        [SerializeField]
        protected PawnHand _leftHandTarget;

        /// <summary>
        /// The right (side of the) controller
        /// </summary>
        /// For game controllers this includes the buttons and controls operated by the right hand.
        public PawnHand rightHandTarget {
            get { return _rightHandTarget; }
        }
        [SerializeField]
        protected PawnHand _rightHandTarget;

        protected virtual void NewTargetComponents() {
            if (_headTarget != null)
                _headTarget.InitComponent();
            if (_leftHandTarget != null)
                _leftHandTarget.InitComponent();
            if (_rightHandTarget != null)
                _rightHandTarget.InitComponent();
        }

        public virtual void InitTargets() {
            if (headTarget != null)
                headTarget.Init(this);
            if (leftHandTarget != null)
                leftHandTarget.Init(this);
            if (rightHandTarget != null)
                rightHandTarget.Init(this);
        }

        protected virtual void StartTargets() {
            if (_headTarget != null)
                _headTarget.StartTarget();
            if (_leftHandTarget != null)
                _leftHandTarget.StartTarget();
            if (_rightHandTarget != null)
                _rightHandTarget.StartTarget();
        }

        protected virtual void UpdateTargets() {
            if (_headTarget != null)
                _headTarget.UpdateTarget();
            if (_leftHandTarget != null)
                _leftHandTarget.UpdateTarget();
            if (_rightHandTarget != null)
                _rightHandTarget.UpdateTarget();
        }

        #endregion

        #region Trackers

        protected Transform _realWorld;
        /// <summary>The transform containing all real-world objects</summary>
        public Transform realWorld {
            get {
                if (_realWorld == null)
                    _realWorld = GetRealWorld();
                return _realWorld;
            }
        }

        private Transform GetRealWorld() {
            Transform realWorldTransform = this.transform.Find("Real World");
            if (realWorldTransform != null)
                return realWorldTransform;

            GameObject realWorld = new GameObject("Real World");
            realWorldTransform = realWorld.transform;

            realWorldTransform.parent = this.transform;
            realWorldTransform.position = this.transform.position;
            realWorldTransform.rotation = this.transform.rotation;
            return realWorldTransform;
        }

        protected Tracker[] _trackers;
        /// <summary>All available trackers for this pawn</summary>
        public Tracker[] trackers {
            get {
                if (_trackers == null)
                    InitTrackers();
                return _trackers;
            }
        }

#if pUNITYXR
        public Tracking.UnityXR unity;
#endif
#if hLEGACYXR
        public UnityTracker unityTracker = new UnityTracker();
#endif
        //private TraditionalDevice traditionalInput = new TraditionalDevice();

        protected virtual void InitTrackers() {
            _trackers = new Tracker[] {
#if pUNITYXR
                //unity,
#endif
#if hLEGACYXR
                unityTracker,
#endif
            };
            CheckTrackers();
        }

        public virtual void CheckTrackers() {
#if pUNITYXR
            unity = Tracking.UnityXR.Get(realWorld);
#endif
        }

        protected virtual void StartTrackers() {
            CheckTrackers();
            for (int i = 0; i < _trackers.Length; i++)
                _trackers[i].StartTracker(this.transform);
        }

        protected virtual void UpdateTrackers() {
            for (int i = 0; i < _trackers.Length; i++)
                _trackers[i].UpdateTracker();
        }

        protected virtual void StartSensors() {
            if (_headTarget != null)
                _headTarget.StartSensors();
            if (_leftHandTarget != null)
                _leftHandTarget.StartSensors();
            if (_rightHandTarget != null)
                _rightHandTarget.StartSensors();
        }

        protected virtual void StopSensors() {
        }

        #endregion

        #region Update

        protected virtual void Update() {
            CheckGround();

            Controllers.Clear();
            traditionalInput.UpdateGameController(gameController);
            UpdateTrackers();
            UpdateTargets();
            UpdateCharacter();
            CalculateVelocity();
        }

        protected virtual void FixedUpdate() {
            //CheckGround();
            CalculateMovement();
        }

        protected virtual void LateUpdate() {
            Controllers.EndFrame();
        }

        #endregion

        #region Calibration

        /// <summary>Calibrates the tracking with the player</summary>
        public virtual void Calibrate() {
            Debug.Log("Calibrate");
            foreach (Tracker tracker in _trackers)
                tracker.Calibrate();

            if (startPosition == StartPosition.AvatarPosition)
                SetStartPosition();

            switch (avatarMatching) {
                case MatchingType.SetHeightToCharacter:
                    SetTrackingHeightToAvatar();
                    break;
                default:
                    break;
            }
        }

        public virtual void SetStartPosition() {
#if hLEGACYXR
            Vector3 delta = transform.position - _headTarget.unityCamera.sensorTransform.position;
            delta.y = 0;
            AdjustTracking(delta);
#endif
        }

        protected virtual void SetTrackingHeightToAvatar() {
#if hLEGACYXR
            Vector3 localNeckPosition;
            if (UnityTracker.xrDevice == UnityTracker.XRDeviceType.None || _headTarget.unityCamera.sensorTransform == null)
                localNeckPosition = _headTarget.transform.position - transform.position;
            else
                localNeckPosition = _headTarget.neckPosition - transform.position;

            float deltaHeight = (neckHeight - localNeckPosition.y);
            AdjustTracking(deltaHeight * Vector3.up);
#endif
        }

        /// <summary>Adjust the tracking origin of all trackers</summary>
        /// <param name="delta">The translation to apply to the tracking origin</param>
        public virtual void AdjustTracking(Vector3 delta) {
            foreach (Tracker tracker in trackers) {
                tracker.AdjustTracking(delta, Quaternion.identity);
            }
        }

        #endregion

        #region Movement

        /// <summary>
        /// maximum forward speed in units(meters)/second
        /// </summary>
        public float forwardSpeed = 1;
        /// <summary>
        /// maximum backward speed in units(meters)/second
        /// </summary>
        public float backwardSpeed = 0.6F;
        /// <summary>
        /// maximum sideways speed in units(meters)/second
        /// </summary>
        public float sidewardSpeed = 1;
        /// <summary>
        /// maximum acceleration in units(meters)/second/second
        /// value 0 = no maximum acceleration
        /// </summary>
        public float maxAcceleration = 1;
        /// <summary>
        /// maximum rotational speed in degrees/second
        /// </summary>
        public float rotationSpeed = 60;

        /// <summary>
        /// The current velocity of the Pawn
        /// </summary>
        public Vector3 velocity;

        [HideInInspector]
        protected Vector3 gravitationalVelocity;

        protected Vector3 inputMovement = Vector3.zero;

        /// <summary>
        /// Move the Pawn forward with the given speed
        /// </summary>
        /// <param name="z">The forward movement speed in units(meters) per second.
        /// Negative value result in backward movement
        /// </param>
        public virtual void MoveForward(float z) {
            if (z > 0)
                z *= forwardSpeed;
            else
                z *= backwardSpeed;

            inputMovement = new Vector3(inputMovement.x, inputMovement.y, z);
        }

        /// <summary>
        /// Move the Parn sideward (left/right) with the given speed
        /// </summary>
        /// <param name="x">The movement speed in units(meters) per second.
        /// Positive values move the pawn to the right (in local space).
        /// Negative value move the pawn to the left.
        /// </param>
        public virtual void MoveSideward(float x) {
            x = x * sidewardSpeed;

            inputMovement = new Vector3(x, inputMovement.y, inputMovement.z);
        }

        public virtual void MoveWorldVector(Vector3 v) {
            inputMovement = characterController.transform.InverseTransformDirection(v);
        }

        /// <summary>
        /// Stop any translational movement of the Pawn
        /// </summary>
        public virtual void Stop() {
            inputMovement = Vector3.zero;
        }

        /// <summary>Rotate the pawn</summary>
        /// Rotates the pawn along the Y axis
        /// <param name="angularSpeed">The speed in degrees per second</param>
        public virtual void Rotate(float angularSpeed) {
            angularSpeed *= Time.deltaTime * rotationSpeed;
            transform.Rotate(transform.up, angularSpeed);
        }

        /// <summary>
        /// Set the rotation angle along Y axis
        /// </summary>
        public virtual void Rotation(float yAngle) {
            Vector3 angles = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(angles.x, yAngle, angles.z);
        }

        /// <summary>Rotation animation to reach the given rotation</summary>
        /// The speed of the rotation is determined by the #rotationSpeed
        /// <param name="targetRotation">The target rotation</param>
        public IEnumerator RotateTo(Quaternion targetRotation) {
            float angle;
            float maxAngle;
            do {
                maxAngle = rotationSpeed * Time.deltaTime;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, maxAngle);
                angle = Quaternion.Angle(transform.rotation, targetRotation);
                yield return new WaitForSeconds(0.01F);
            } while (angle > maxAngle);
            yield return null;
        }

        /// <summary>
        /// Rotation animation to look at the given position
        /// </summary>
        /// The speed of the rotation is determined by the #rotationSpeed
        /// <param name="targetPosition">The position to look at</param>
        /// <returns></returns>
        public IEnumerator LookAt(Vector3 targetPosition) {
            Quaternion targetRotation = Quaternion.LookRotation(targetPosition - transform.position);
            yield return RotateTo(targetRotation);
        }

        /// <summary>
        /// Movement animation to reach the given position
        /// </summary>
        /// The pawn will walk to the given position. The speed of the movement is determined by #forwardSpeed.
        /// Any required rotation will be limited by #rotationSpeed
        /// <param name="targetPosition">The position to where the pawn should walk</param>
        /// <returns></returns>
        public IEnumerator WalkTo(Vector3 targetPosition) {
            yield return LookAt(targetPosition);

            float totalDistance = Vector3.Distance(transform.position, targetPosition);
            float distance = totalDistance;
            do {
                // Don't rotate when you're close to the target position
                if (distance > 0.2F)
                    transform.LookAt(targetPosition);
                transform.rotation = Quaternion.AngleAxis(transform.eulerAngles.y, Vector3.up);
                if (distance > 0.5F) // make dependent on maxAcceleration?
                    MoveForward(1);
                else {
                    float f = 1 - (distance / 0.5F);
                    Vector3 worldVector = (targetPosition - transform.position).normalized * (1 - (f * f * f));
                    //Debug.Log("M " + worldVector.magnitude);
                    //if (worldVector.magnitude < 0.15F)
                    //    worldVector = worldVector.normalized * 0.15F; // * Time.fixedDeltaTime ;
                    MoveWorldVector(worldVector);
                }
                Vector3 direction = targetPosition - transform.position;
                direction.y = 0;
                distance = direction.magnitude;
                //yield return new WaitForEndOfFrame();
                yield return new WaitForSeconds(0.01F);
            } while (distance > 0.01F);
            Stop();

            transform.position = targetPosition;
            yield return null;
        }

        /// <summary>
        /// Movement animation to reach the given position and rotation
        /// </summary>
        /// The pawn will walk to the given position and rotation. The speed of the movement is determined by #forwardSpeed.
        /// Any required rotation will be limited by #rotationSpeed
        /// <param name="targetPosition">The position to where the pawn should walk</param>
        /// <param name="targetRotation">The rotation the pawn should have at the end</param>
        /// <returns></returns>
        public IEnumerator WalkTo(Vector3 targetPosition, Quaternion targetRotation) {
            yield return WalkTo(targetPosition);
            yield return RotateTo(targetRotation);
        }

        /// <summary>
        /// Let the pawn jump
        /// </summary>
        /// <param name="takeoffVelocity>The takeoff velocity of the jump</param>
        public virtual void Jump(float takeoffVelocity) {
            if (ground == null)
                return;

            float verticalVelocity = velocity.y + takeoffVelocity;
            gravitationalVelocity -= new Vector3(0, -verticalVelocity, 0);
            float y = inputMovement.y + verticalVelocity;
            inputMovement = new Vector3(inputMovement.x, y, inputMovement.z);
        }

        protected virtual void CalculateMovement() {
            if (characterController != null && _headTarget != null) {
#if hLEGACYXR
                if (UnityTracker.xrDevice == UnityTracker.XRDeviceType.None ||
                    _headTarget.unityCamera.sensorTransform == null ||
                    _headTarget.unityCamera.status != Tracker.Status.Tracking) {

                    Vector3 cameraTranslation = Vector3.zero;

                    Vector3 moveDirection = characterController.transform.TransformDirection(inputMovement);

                    characterController.Move(cameraTranslation + moveDirection * Time.fixedDeltaTime);
                }
                else {
#endif
#if pUNITYXR
                Vector3 oldCameraPosition = _headTarget.unityCamera.transform.position;
#endif
#if hLEGACYXR
                    Vector3 oldCameraPosition = _headTarget.unityCamera.sensorTransform.position;
#endif

                    Vector3 moveDirection = characterController.transform.TransformDirection(inputMovement);

                    Vector3 localHmdPosition = new Vector3(0, 0, -characterController.radius);
#if pUNITYXR
                    Vector3 neckPosition = _headTarget.unityCamera.transform.TransformPoint(localHmdPosition) + moveDirection * Time.fixedDeltaTime;
#endif
#if hLEGACYXR
                    Vector3 neckPosition = _headTarget.unityCamera.sensorTransform.TransformPoint(localHmdPosition) + moveDirection * Time.fixedDeltaTime;
#endif
#if pUNITYXR || hLEGACYXR
                    Vector3 footPosition = new Vector3(neckPosition.x, transform.position.y, neckPosition.z);

                    Vector3 cameraTranslation = footPosition - transform.position;

                    float characterHeight = (neckPosition.y - transform.position.y) / 0.75F;

                    characterController.height = characterHeight;
                    characterController.center = new Vector3(0, characterHeight / 2, 0);

                    Vector3 ccPos = characterController.transform.position;

                    Vector3 characterControllerMove = cameraTranslation + moveDirection * Time.fixedDeltaTime;
                    characterControllerMove.y = 0;
                    characterController.Move(characterControllerMove);
#endif
#if pUNITYXR
                    unity.transform.Translate(-cameraTranslation);
#elif hLEGACYXR
                    unityTracker.trackerTransform.Translate(-cameraTranslation);
#endif

#if hLEGACYXR
                }
#endif
            }
            else if (characterController != null) {
                characterController.Move(inputMovement * Time.fixedDeltaTime);
            }
            else {
                transform.Translate(inputMovement * Time.fixedDeltaTime);
            }
            inputMovement = Vector3.zero;
        }

        /*
        protected virtual void CalculateMovement2() {
            if (characterController != null && _headTarget != null) {
                Vector3 localHmdPosition = new Vector3(0, 0, -characterController.radius);

#if pUNITYXR
                Vector3 cameraPositionOnCharacter = _headTarget.unityCamera.transform.TransformPoint(localHmdPosition);
#else
                Vector3 cameraPositionOnCharacter = _headTarget.unityCamera.sensorTransform.TransformPoint(localHmdPosition);
#endif
                Vector3 cameraTranslation = cameraPositionOnCharacter - characterController.transform.position;

                float characterBottom = characterController.transform.position.y; // - characterController.height / 2 - characterController.skinWidth;
                float cameraHeight = cameraPositionOnCharacter.y;

                float characterHeight = cameraHeight * (2 / 1.5F); // + characterController.height / 4;
                if (characterHeight > 0.5F)
                    characterController.height = characterHeight;
                float characterY = characterBottom; // + characterHeight / 2;
                float translationY = characterY - characterController.transform.position.y;
                translationY = 0;

                cameraTranslation = new Vector3(cameraTranslation.x, translationY, cameraTranslation.z);
                //Vector3 moveDirection = characterController.transform.TransformDirection(inputMovement);

                Vector3 ccPos = characterController.transform.position;
                characterController.Move(cameraTranslation);
                //Vector3 ccTranslation = characterController.transform.position - ccPos;


                // compensate charactercontroller translation in VR HMD
                //cameraTarget.unity.tracker.trackerTransform.Translate(-ccTranslation + moveDirection * Time.fixedDeltaTime);

                
                //unity.trackerTransform.Translate(-ccTranslation + moveDirection * Time.fixedDeltaTime);                

                // Better restore Camera.main.transform.position
                // by moving cameraTarget.unity.tracker.trackertransform based on camPos

                //float cameraDistance = cameraTranslation.magnitude - ccTranslation.magnitude;
                //if (cameraDistance > 0.01F)
                //    cameraTarget.unity.Fader(cameraDistance - 0.01F);
                //else
                //    cameraTarget.unity.Fader(0);
            }
            else {
                Vector3 moveDirection = transform.TransformDirection(inputMovement);
                transform.Translate(moveDirection * Time.fixedDeltaTime);
            }
            inputMovement = Vector3.zero;
        }
        */
        protected virtual void Fall() {
            gravitationalVelocity += Physics.gravity * Time.deltaTime;
            transform.Translate(gravitationalVelocity * Time.deltaTime);
        }

        #region Ground

        /// <summary>
        /// The ground Transform on which the pawn is standing
        /// </summary>
        /// When the pawn is not standing on the ground, the value is null
        public Transform ground;

        [HideInInspector]
        protected Transform lastGround;

        /// <summary>
        /// The velocity of the ground on which the pawn is standing
        /// </summary>
        [HideInInspector]
        public Vector3 groundVelocity;
        /// <summary>
        /// The angular velocity of the ground on which the pawn is standing
        /// </summary>
        /// The velocity is in degrees per second along the Y axis of the pawn
        [HideInInspector]
        public float groundAngularVelocity;

        [HideInInspector]
        protected Vector3 lastGroundPosition = Vector3.zero;
        [HideInInspector]
        protected float lastGroundAngle = 0;

        protected virtual void CheckGround() {
            CheckGrounded();
            CheckGroundMovement();
        }

        protected virtual void CheckGrounded() {
            //if (leftControllerTarget.GrabbedStaticObject() || rightControllerTarget.GrabbedStaticObject())
            //    return;

            Vector3 pawnBase = transform.position;
            Vector3 groundNormal;
            float distance = GetDistanceToGroundAt(pawnBase, stepOffset, out ground, out groundNormal);
            if (characterController != null)
                distance += characterController.skinWidth;

            if (ground != null && distance < 0.01F && gravitationalVelocity.y <= 0) {
                gravitationalVelocity = Vector3.zero;
                transform.Translate(0, distance, 0);
            }
            else {
                ground = null;
                if (useGravity)
                    Fall();
            }
        }

        protected virtual void CheckGroundMovement() {
            if (ground == null) {
                lastGround = null;
                lastGroundPosition = Vector3.zero;
                lastGroundAngle = 0;
                return;
            }

            if (ground == lastGround) {
                Vector3 groundTranslation = ground.position - lastGroundPosition;
                // Vertical movement is handled by CheckGrounded
                groundTranslation = new Vector3(groundTranslation.x, 0, groundTranslation.z);
                groundVelocity = groundTranslation / Time.deltaTime;

                float groundRotation = ground.eulerAngles.y - lastGroundAngle;
                groundAngularVelocity = groundRotation / Time.deltaTime;

                if (this.transform.root != ground.root) {
                    transform.Translate(groundTranslation, Space.World);
                    transform.RotateAround(ground.position, Vector3.up, groundRotation);
                }
            }
            lastGround = ground;
            lastGroundPosition = ground.position;
            lastGroundAngle = ground.eulerAngles.y;
        }

        public virtual float GetDistanceToGroundAt(Vector3 position, float maxDistance, out Transform ground, out Vector3 normal) {
            normal = Vector3.up;

            Vector3 rayStart = position + normal * maxDistance;
            Vector3 rayDirection = -normal;
            //Debug.DrawRay(rayStart, rayDirection * maxDistance * 2, Color.magenta);
            RaycastHit[] hits = Physics.RaycastAll(rayStart, rayDirection, maxDistance * 2, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            if (hits.Length == 0) {
                ground = null;
                return -maxDistance;
            }

            int closestHit = 0;
            bool foundClosest = false;
            for (int i = 0; i < hits.Length; i++) {
                if ((characterController == null || hits[i].transform != characterController.transform) &&
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

        #endregion

        [HideInInspector]
        protected float lastVelocityTime;
        [HideInInspector]
        protected Vector3 lastPosition;

        protected virtual void CalculateVelocity() {
            if (lastVelocityTime > 0) {
                float deltaTime = Time.time - lastVelocityTime;

                velocity = Vector3.zero; // -groundVelocity;
                //if (avatarRig != null) {
                //    Vector3 headTranslation = headTarget.neck.target.transform.position - lastHeadPosition;//preTrackingHeadPosition;
                //    if (headTranslation.magnitude == 0)
                //        // We assume we did not get an update - needs to be improved though
                //        // Especially with networking, position updates occur less frequent than frame updates
                //        return;
                //    Vector3 localHeadTranslation = headTarget.neck.target.transform.InverseTransformDirection(headTranslation);
                //    localVelocity += localHeadTranslation / deltaTime;
                //    //Debug.Log(gameObject.name + " " + localHeadTranslation.z);

                //    float headDirection = headTarget.neck.target.transform.eulerAngles.y - lastHeadDirection; //preTrackingHeadDirection;
                //    float localHeadDirection = Angle.Normalize(headDirection);
                //    turningVelocity = localHeadDirection / deltaTime;
                //}
                //else {
                Vector3 translation = transform.position - lastPosition;
                velocity += translation / deltaTime;
                //}
                //acceleration = (localVelocity - velocity) / deltaTime;
                // Acceleration is not correct like this. We get accels like -24.3, 22, 6.7, -34.4, 32.6, -5.0 for linear speed increase...
                // This code is not correct. 
                //if (acceleration.magnitude > 15) { // more than 15 is considered unhuman and will be ignored
                //    localVelocity = Vector3.zero;
                //    acceleration = Vector3.zero;
                //}
            }
            lastVelocityTime = Time.time;

            lastPosition = transform.position;
            //lastRotation = headTarget.neck.target.transform.rotation;
            //lastDirection = headTarget.neck.target.transform.eulerAngles.y;
        }

        #endregion

        #region Networking

        /// <summary>The networking interface</summary>
        public IPawnNetworking networking;
        /// <summary>Is true when this is a remote pawn</summary>
        /// Remote pawns are not controlled locally, but are controlled from another computer.
        /// These are copies of the pawn on that other computer and are updated via messages
        /// exchanges on a network.
        public bool isRemote = false;
        /// <summary>The Id of this pawn across the network</summary>
        public ulong nwId;

        /// <summary>The local Id of this humanoid</summary>
        public int id = -1;

        #endregion
    }
}