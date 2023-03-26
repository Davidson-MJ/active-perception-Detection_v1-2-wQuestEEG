using UnityEngine;

namespace Passer.Pawn {
    /// <summary>
    /// Pawn Control options for hand related things
    /// </summary>
    public partial class PawnHand : Target {

        protected PawnControl _pawn;
        /// <summary>
        /// The Pawn for this hand
        /// </summary>
        public PawnControl pawn {
            get {
#if UNITY_EDITOR
                if (_pawn == null)
                    _pawn = GetPawn();
#endif
                return _pawn;
            }
        }

        #region Manage

        private PawnControl GetPawn() {
            PawnControl[] pawns = FindObjectsOfType<PawnControl>();
            PawnControl foundPawn = null;

            for (int i = 0; i < pawns.Length; i++)
                if (isLeft && pawns[i].leftHandTarget == this)
                    foundPawn = pawns[i];
                else if (!isLeft && pawns[i].rightHandTarget == this)
                    foundPawn = pawns[i];

            return foundPawn;
        }

        public virtual void CheckSensors() {
#if pUNITYXR
            if (pawn == null || pawn.unity == null)
                return;

            if (enabled && pawn.unity.enabled) {
                if (unityController == null) {
                    Vector3 position = transform.TransformPoint(isLeft ? -0.1F : 0.1F, -0.05F, 0.04F);
                    Quaternion localRotation = isLeft ? Quaternion.Euler(180, 90, 90) : Quaternion.Euler(180, -90, -90);
                    Quaternion rotation = transform.rotation * localRotation;
                    unityController = pawn.unity.GetController(isLeft, position, rotation);
                }
                //if (!Application.isPlaying)
                //    SetSensor2Target();
            }
            else {
#if UNITY_EDITOR
                if (!Application.isPlaying) {
                    if (unityController != null)
                        Object.DestroyImmediate(unityController.gameObject, true);
                }
#endif
                unityController = null;
            }
#endif
        }

        #endregion Manage

        /// <summary>
        /// The rigidbody of the controller
        /// </summary>
        public Rigidbody controllerRigidbody;

        protected ControllerPhysics controllerPhysics;

        /// <summary>
        /// The Side of the controller
        /// </summary>
        public Side side;
        /// <summary>
        /// Is this the left hand controller?
        /// </summary>
        public bool isLeft {
            get { return side == Side.Left; }
        }
        /// <summary>
        /// Is this the right hand controller?
        /// </summary>
        public bool isRight {
            get { return side == Side.Right; }
        }
        /// <summary>
        /// Get the other controller
        /// </summary>
        public PawnHand otherController {
            get {
                return isLeft ? _pawn.rightHandTarget : _pawn.leftHandTarget;
            }
        }

        /// <summary>
        /// Is the controller holding an object together with the other controller?
        /// </summary>
        public bool twoHandedGrab = false;

        /// <summary>
        /// The outward direction of the controller. 
        /// </summary>
        public Vector3 outward;

        /// <summary>
        /// Create a new controller
        /// </summary>
        /// <param name="pawn">The pawn for which the controller should be created</param>
        /// <param name="side">The controller Side which should be created</param>
        /// <returns></returns>
        public static PawnHand Create(PawnControl pawn, Side side) {
            GameObject targetObject = new GameObject();
            if (side == Side.Left)
                targetObject.name = "Left Hand";
            else
                targetObject.name = "Right Hand";
            Transform targetTransform = targetObject.transform;

            targetTransform.parent = pawn.transform;

            CharacterController characterController = pawn.GetComponent<CharacterController>();
            if (characterController != null)
                targetTransform.localPosition = pawn.transform.position + new Vector3(side == Side.Left ? -characterController.radius : characterController.radius, characterController.height * 0.5F, 0);
            else
                targetTransform.localPosition = new Vector3(side == Side.Left ? -0.3F : 0.3F, 1, 0);
            targetTransform.localRotation = Quaternion.identity;

            PawnHand controllerTarget = targetTransform.gameObject.AddComponent<PawnHand>();
            controllerTarget._pawn = pawn;

            controllerTarget.side = side;
            if (side == Side.Left) {
                controllerTarget.outward = Vector3.left;
                //pawn.leftHandTarget = controllerTarget;
            }
            else {
                controllerTarget.outward = Vector3.right;
                //pawn.rightHandTarget = controllerTarget;
            }

            return controllerTarget;
        }

        #region Sensors

#if pUNITYXR
        public Tracking.UnityXRController unityController;
#elif hLEGACYXR
        public UnityController unityController = new UnityController() {
            enabled = true
        };
#endif

        public override void InitSensors() {
            if (_pawn == null)
                return;

#if pUNITYXR
            unityController = isLeft ? pawn.unity.leftController : pawn.unity.rightController;
            //if (!pawn.isRemote) {
            //    if (unity == null) {
            //        unity = Tracking.UnityXRController.Get(pawn.unity, isLeft);
            //        if (unity != null) {
            //            unity.transform.position = transform.position;
            //            unity.transform.rotation = transform.rotation;
            //        }
            //    }
            //    if (unity != null) {
            //        if (isLeft)
            //            pawn.unity.leftController = unity;
            //        else
            //            pawn.unity.rightController = unity;
            //    }
            //}
            
#else
            //if (!pawn.isRemote) {
            //if (unity == null) {
            //    unity = UnityControllerComponent.Get(pawn.unity, isLeft);
            //    unity.transform.position = transform.position;
            //    unity.transform.rotation = transform.rotation;
            //}
            //if (isLeft)
            //    pawn.unity.leftController = unity;
            //else
            //    pawn.unity.rightController = unity;
            //}
#endif
        }

        public override void StartSensors() {
#if hLEGACYXR
            unityController.Start(this.transform);
#endif
        }

        protected override void UpdateSensors() {
#if hLEGACYXR
            unityController.Update();
#endif
        }

        public override bool showRealObjects {
            set {
                base.showRealObjects = value;
#if hLEGACYXR
                if (unityController != null) { }// unityController.show = _showRealObjects;
#endif
            }
        }

        #endregion

        #region Settings

        public bool physics;

        #endregion

        #region Start

        public void Init(PawnControl pawn) {
            this._pawn = pawn;
        }

        public void InitTarget() {
            InitSensors();
            if (grabSocket == null)
                //grabSocket = HandInteraction.CreateGrabSocket(this);
                grabSocket = CreateGrabSocket(this);
        }

        public override void StartTarget() {
            side = isLeft ? Side.Left : Side.Right;

            InitSensors();
            showRealObjects = pawn.showRealObjects;
            //ShowSensors(pawn.showRealObjects);

            CheckRigidbody();
            CheckColliders();

            if (grabSocket == null)
                //grabSocket = HandInteraction.CreateGrabSocket(this);
                grabSocket = PawnHand.CreateGrabSocket(this);

            if (physics) {
                controllerPhysics = GetComponent<ControllerPhysics>();
                if (controllerPhysics == null)
                    gameObject.AddComponent<ControllerPhysics>();
            }
        }

        #endregion

        #region Update

        public override void UpdateTarget() {
#if UNITY_2017_2_OR_NEWER
            if (!UnityEngine.XR.XRSettings.enabled)
                return;
#elif !pUNITYXR
            if (!UnityEngine.VR.VRSettings.enabled)
                return;
#endif
            if (_pawn.isRemote)
                return;
#if hLEGACYXR
            if (unityController == null)
                return;
#endif
            //if (physics) {
            //    if (controllerRigidbody == null)
            //        controllerRigidbody = GetComponent<Rigidbody>();
            //    return;
            //}

            UpdateSensors();

#if pUNITYXR
            UpdateHandTargetFromController();    
#endif

            //if (grabbedObject != null) {

            //    // Kinematic Limitations
            //    Handle handle = grabSocket.attachedHandle;
            //    if (handle != null) {
            //        Rigidbody handleRigidbody = handle.GetComponentInParent<Rigidbody>();
            //        if (handleRigidbody != null) {
            //            KinematicLimitations kinematicLimitations = handleRigidbody.GetComponent<KinematicLimitations>();
            //            if (kinematicLimitations != null && kinematicLimitations.enabled) {
            //                Vector3 correctionVector = kinematicLimitations.GetCorrectionVector();
            //                transform.position += correctionVector;
            //                Quaternion correctionRotation = kinematicLimitations.GetCorrectionRotation();
            //                handleRigidbody.transform.rotation *= correctionRotation;
            //                return;
            //            }
            //        }
            //    }

            //    // two handed grab
            //    if (twoHandedGrab) {
            //        // this is the primary grabbing controller
            //        // otherController is the secondary grabbing controller

            //        // This assumes the socket is a child of the controllertarget
            //        Vector3 otherSocketLocalPosition = otherController.grabSocket.transform.localPosition;
            //        // Calculate socket position from unity tracker
            //        Vector3 otherSocketPosition = otherController.unityController.transform.TransformPoint(otherSocketLocalPosition);

            //        Vector3 handlePosition = otherController.grabSocket.attachedHandle.worldPosition;
            //        Vector3 toHandlePosition = handlePosition - transform.position;
            //        Quaternion rotateToHandlePosition = Quaternion.FromToRotation(toHandlePosition, transform.forward);

            //        transform.LookAt(otherSocketPosition, unityController.transform.up);
            //        transform.rotation *= rotateToHandlePosition;
            //    }
            //    else
            //        transform.rotation = unityController.transform.rotation;
            //}
            //else
            //    transform.rotation = unityController.transform.rotation;

        }

        protected void UpdateHandTargetFromController() {
#if hLEGACYXR
            transform.rotation = unityController.sensorTransform.rotation * Quaternion.AngleAxis(90, Vector3.right);
            transform.position = unityController.sensorTransform.position;
#endif
        }

        #endregion

        #region Interaction

        public Socket grabSocket;

        //[System.NonSerialized]
        public GameObject touchedObject = null;
        public GameObject grabbedPrefab;
        public GameObject grabbedObject;
        public Handle grabbedHandle = null;
        public bool grabbedRigidbody = false;
        //public RigidbodyData grabbedRigidbodyData;

        public static Socket CreateGrabSocket(PawnHand controllerTarget) {
            GameObject socketObj = new GameObject(controllerTarget.isLeft ? "Left Grab Socket" : "Right Grab Socket");
            Transform socketTransform = socketObj.transform;
            socketTransform.parent = controllerTarget.transform;
            socketTransform.localPosition = Vector3.zero;
            socketTransform.localRotation = Quaternion.identity;
            //socketTransform.localPosition = new Vector3(0, -0.04F, -0.04F);
            //socketTransform.localRotation = Quaternion.AngleAxis(45, Vector3.right);

            ControllerSocket grabSocket = socketObj.AddComponent<ControllerSocket>();
            grabSocket.controllerTarget = controllerTarget;
            return grabSocket;
        }

        /// <summary>Calculate socket position from tracking position</summary>
        protected Vector3 GetSocketPosition() {
            // Assums that the grabsocket is a child of the target transform
            Vector3 localSocketPosition = grabSocket.transform.localPosition;

#if pUNITYXR
            Vector3 socketPosition = unityController.transform.TransformPoint(localSocketPosition);
            return socketPosition;
#elif hLEGACYXR
            Vector3 socketPosition = unityController.sensorTransform.TransformPoint(localSocketPosition);
            return socketPosition;
#else
            return Vector3.zero;
#endif
        }


        #endregion

        #region Collisions

        protected void CheckRigidbody() {
            controllerRigidbody = GetComponent<Rigidbody>();
            if (controllerRigidbody == null)
                controllerRigidbody = gameObject.AddComponent<Rigidbody>();
            controllerRigidbody.isKinematic = true;
            controllerRigidbody.useGravity = false;
        }

        protected void CheckColliders() {
            Collider[] colliders = transform.GetComponentsInChildren<Collider>();
            // Does not work if the hand has grabbed an object with colliders...
            if (colliders.Length == 0)
                colliders = GenerateColliders();

            if (!physics) {
                foreach (Collider collider in colliders)
                    collider.isTrigger = true;
            }
        }

        protected virtual Collider[] GenerateColliders() {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.center = new Vector3(0, 0, 0);
            collider.radius = 0.1F;

            return new Collider[] { collider };
        }

        protected void OnTriggerEnter(Collider other) {
        }

        protected void OnTriggerStay(Collider collider) {
            if (physics)
                return;

            if (collider.isTrigger)
                // We cannot touch trigger colliders
                return;

            if (collider.attachedRigidbody != null)
                touchedObject = collider.attachedRigidbody.gameObject;
            else
                touchedObject = collider.gameObject;
        }

        protected void OnTriggerExit(Collider other) {
            if (physics)
                return;

            touchedObject = null;
        }

        #endregion Colliders


    }
}