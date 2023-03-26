using System.Collections;
using UnityEngine;

namespace Passer {

    /// <summary>Sockets can hold a Handles.</summary>
    /// When a handle is within the range of the socket, it will be attached to the socket.
    /// The position and rotation of the Transform with the Handle will be changed
    /// such that the handle fits the position and rotation of the socket.
    public class Socket : MonoBehaviour {

        protected static void DebugLog(string s) {
            //Debug.Log(s);
        }

        /// <summary>
        /// Does the socket currently have a handle attached?
        /// </summary>
        public bool isOccupied {
            get {
                return this.attachedTransform != null;
            }
        }

        /// <summary>
        /// A prefab which is used to attach to the socket at startup.
        /// </summary>
        public GameObject attachedPrefab;
        /// <summary>The Transform attached to this socket</summary>
        /// If the socket holds a Handle, this will contain the Transform of the Handle.
        /// It will be null otherwise
        public Transform attachedTransform;
        protected Transform releasingTransform;

        public Handle attachedHandle;

        /// <summary>The parent of the attached transform before it was attached</summary>
        /// This is used to restore the parent when the transform is released again.
        protected Transform attachedTransformParent;

        /// <summary>A tag for limiting which handles can be held by the socket</summary>
        /// If set (not null or empty) only Handles with the given tag will fit in the socket.
        public string socketTag;

        public bool destroyOnLoad = false;

        public virtual Vector3 worldPosition {
            get {
                return transform.position;
            }
        }

        public enum AttachMethod {
            Unknown,
            Parenting,
            SocketParenting,
            ColliderDuplication
        }
        public AttachMethod attachMethod = AttachMethod.Unknown;

        #region Alignment

        protected virtual void MoveHandleToSocket(Transform socketTransform, Rigidbody handleRigidbody, Handle handle) {
            DebugLog("MoveHandleToSocket");

            Transform handleTransform = handle.GetComponent<Transform>();
            if (handleRigidbody != null)
                handleTransform = handleRigidbody.transform;

            if (handle.grabType == Handle.GrabType.RailGrab) {
                MoveRailToSocket(socketTransform, handleTransform, handle);
            }
            else {
                handleTransform.rotation = handle.RotationTo(socketTransform.rotation) * handleTransform.rotation;
                handleTransform.position += handle.TranslationTo(socketTransform.position);
            }
        }

        protected virtual void MoveRailToSocket(Transform socketTransform, Transform railTransform, Handle rail) {
            Vector3 handleYaxis = rail.transform.up;
            Vector3 socketYaxis = socketTransform.up;
            float angle = Vector3.Angle(handleYaxis, socketYaxis);
            if (angle > 90)
                socketYaxis = -socketTransform.up;

            Quaternion socketToHandleRotation = Quaternion.FromToRotation(socketYaxis, handleYaxis);
            Quaternion targetRotation = Quaternion.Inverse(socketToHandleRotation) * railTransform.rotation;

            railTransform.rotation = rail.RotationTo(targetRotation) * railTransform.rotation;

            // Socket along rail
            Vector3 localSocketPosition = socketTransform.position - rail.transform.position;
            Vector3 positionOnRail = Vector3.Project(localSocketPosition, rail.transform.up);
            Vector3 railToSocket = localSocketPosition - positionOnRail;

            // Socket within rail length
            float maxDistance = rail.transform.lossyScale.y / 2;
            float distance = positionOnRail.magnitude - maxDistance;
            if (distance > 0) {
                float scale = distance / positionOnRail.magnitude;
                railToSocket += Vector3.Scale(positionOnRail, Vector3.one * scale);
            }

            railTransform.position += railToSocket;
        }

        protected virtual void MoveHandleToSocket(Transform socketTransform, Handle handle) {
            DebugLog("MoveHandleToSocket");

            Transform handleTransform = handle.GetComponent<Transform>();
            Rigidbody handleRigidbody = handle.GetComponentInParent<Rigidbody>();
            if (handleRigidbody != null) {
                handleTransform = handleRigidbody.transform;

                KinematicLimitations handleLimitations = handle.GetComponent<KinematicLimitations>();
                if (handleRigidbody.isKinematic && handleLimitations != null && handleLimitations.enabled) {
                    //handleTransform.position += handle.TranslationTo(socketTransform.position);
                    // No rotations for kinematic Limitations
                    return;
                }
            }

            if (handle.grabType == Handle.GrabType.RailGrab) {
                MoveRailToSocket(socketTransform, handleTransform, handle);
            }
            else {
                handleTransform.rotation = handle.RotationTo(socketTransform.rotation) * handleTransform.rotation;
                handleTransform.position += handle.TranslationTo(socketTransform.position);
            }
        }

        protected virtual void MoveSocketToHandle(Transform socketTransform, Handle handle) {
            DebugLog("MoveSocketToHandle");

            socketTransform.rotation *= Quaternion.Inverse(handle.RotationTo(socketTransform.rotation));
            socketTransform.position -= handle.TranslationTo(socketTransform.position);
        }

        protected virtual void MoveSocketToHandle(Transform socketTransform, Rigidbody socketRigidbody, Handle handle) {
            DebugLog("MoveSocketToHandle");

            //Quaternion rotation = Quaternion.Inverse(handle.RotationTo(socketTransform.rotation));
            //socketRigidbody.transform.rotation *= rotation;

            //Vector3 translation = handle.TranslationTo(socketTransform.position);
            //socketRigidbody.transform.position -= translation;

            // code based on HandSocket, potentially better, but rotation is somehow wrong
            Quaternion socket2rigidbodyRotation = Quaternion.Inverse(socketTransform.localRotation);
            socketRigidbody.transform.rotation = handle.worldRotation * socket2rigidbodyRotation;

            Vector3 socket2RigidbodyPosition = socketRigidbody.transform.position - socketTransform.position;
            socketRigidbody.transform.position = handle.worldPosition + socket2RigidbodyPosition;

        }

        #endregion Alignment

        #region Attach

        public void Attach(GameObject objectToAttach) {
            Attach(objectToAttach, false);
        }
        public bool Attach(GameObject objectToAttach, bool rangeCheck) {
            bool success = Attach(objectToAttach.transform, rangeCheck);
            return success;
        }

        /// <summary>Tries to attach the given Transform to this socket</summary>
        /// If the Transform has a Handle with the right Socket Tag it will be attached to the socket.
        /// Static and Kinematic Rigidbodies will be attached by parenting.
        /// Non-Kinematic Rigidbodies will be attached using a joint.
        /// <param name="transformToAttach">The Transform to attach to this socket</param>
        /// <returns>Boolean indicating whether attachment succeeded</returns>
        public virtual bool Attach(Transform transformToAttach, bool rangeCheck = true) {
            DebugLog("Attach transform " + transformToAttach);

            Handle handle;

            Rigidbody rigidbodyToAttach = transformToAttach.GetComponentInParent<Rigidbody>();
            if (rigidbodyToAttach != null)
                handle = rigidbodyToAttach.GetComponentInChildren<Handle>();
            else
                handle = transformToAttach.GetComponentInChildren<Handle>();

            if (handle == null) {
                // Transform does not have a handle
                DebugLog(gameObject.name + ": Attach failed. Object " + transformToAttach.name + " does not have a handle");
                return false;
            }

            if (handle.socket != null) {
                // Handle is already in a socket
                return false;
            }

            if (socketTag != null && socketTag != "" && !transformToAttach.gameObject.CompareTag(socketTag)) {
                // Object did not have the right tag
                Debug.Log(gameObject.name + ": Attach failed. Object " + transformToAttach + " does not have the right tag"); ;
                return false;
            }

            Rigidbody handleRigidbody = handle.GetComponentInParent<Rigidbody>();
            if (handleRigidbody != rigidbodyToAttach) {
                // Object does not have a handle,
                // found handle is of child rigidbody
                //Debug.Log(gameObject.name + ": Attach failed. Object does not have a handle. Handle is on child Rigidbody.");
                return false;
            }

            bool success = Attach(handle, rangeCheck);
            return success;
        }

        /// <summary>Tries to attach the given Transform to this socket</summary>
        /// If the Handle has the right Socket Tag it will be attached to the socket.
        /// Static and Kinematic Rigidbodies will be attached by parenting.
        /// Non-Kinematic Rigidbodies will be attached using a joint.
        /// <param name="handle">The Handle to attach to this socket</param>
        /// <returns>Boolean indicating whether attachment succeeded</returns>
        public virtual bool Attach(Handle handle, bool rangeCheck = true) {
            DebugLog(this.gameObject.name + " Attach handle " + handle);
            if (handle == null) {
                // Transform does not have a handle
                DebugLog(gameObject.name + ": Attach failed. Transform does not have a handle");

                return false;
            }

            if (handle.socket != null) {
                // Handle is already in a socket
                return false;
            }

            if (socketTag != null && socketTag != "" && !handle.gameObject.CompareTag(socketTag)) {
                // Object did not have the right tag
                Debug.Log(gameObject.name + ": Attach failed. Handle " + handle + " does not have the right tag"); ;
                return false;
            }

            Rigidbody rigidbodyToAttach = handle.GetComponentInParent<Rigidbody>();
            if (rigidbodyToAttach == null)
                return AttachStaticObject(handle.transform, handle);
            else {
                RigidbodyDisabled disabledRigidbody = RigidbodyDisabled.Get(rigidbodyToAttach.transform);
                if (disabledRigidbody != null && rigidbodyToAttach.transform.parent != null)
                    rigidbodyToAttach = rigidbodyToAttach.transform.parent.GetComponentInParent<Rigidbody>();
                if (rigidbodyToAttach == null)
                    return AttachStaticObject(handle.transform, handle);
                else
                    return AttachRigidbody(rigidbodyToAttach, handle, rangeCheck);
            }
        }

        #region Rigidbody

        protected virtual bool AttachRigidbody(Rigidbody objRigidbody, Handle handle, bool rangeCheck = true) {
            DebugLog("AttachRigidbody " + objRigidbody);

            float grabDistance = Vector3.Distance(this.transform.position, handle.worldPosition);
            if (rangeCheck && handle.range > 0 && grabDistance > handle.range) {
                //Debug.Log("Socket is outside range of handle");
                return false;
            }

            Transform objTransform = objRigidbody.transform;

            Rigidbody thisRigidbody = this.GetComponentInParent<Rigidbody>();
            Joint joint = objRigidbody.GetComponent<Joint>();
            // See if these joints are being destroyed
            DestroyedJoints destroyedJoints = objRigidbody.GetComponent<DestroyedJoints>();

            if (objRigidbody.isKinematic) {
                if (thisRigidbody == null)
                    AttachRigidbodyParenting(objRigidbody, handle);
                else if (thisRigidbody.isKinematic)
                    AttachTransformParenting(objRigidbody.transform, handle);
                else {
                    AttachSocketParenting(objRigidbody, handle, thisRigidbody);
                }
            }
            else if (thisRigidbody == null) {
                AttachRigidbodyReverseJoint(objRigidbody, handle);
            }
            else if (
                (joint != null && destroyedJoints == null) ||
                objRigidbody.constraints != RigidbodyConstraints.None
                ) {
                //|| otherHandPhysics != null) {

                AttachRigidbodyJoint(objRigidbody, handle);
            }
            else {
                AttachRigidbodyParenting(objRigidbody, handle);
            }

            releasingTransform = null;
            attachedTransform = objTransform;
            handle.socket = this;
            return true;
        }

        protected void AttachTransformParenting(Transform objTransform, Handle handle) {
            Debug.Log("AttachTransformParenting: " + objTransform);

            attachedTransformParent = objTransform.parent;

            Rigidbody handleRigidbody = handle.GetComponentInParent<Rigidbody>();
            KinematicLimitations handleLimitations = handleRigidbody.GetComponent<KinematicLimitations>();
            if (handleRigidbody.isKinematic && handleLimitations != null && handleLimitations.enabled) {
                MoveSocketToHandle(this.transform, handle);
            }
            else
                MoveHandleToSocket(this.transform, handle);

            objTransform.SetParent(this.transform);

            attachedTransform = objTransform;
            attachedHandle = handle;
            handle.socket = this;
        }

        protected virtual void AttachRigidbodyParenting(Rigidbody objRigidbody, Handle handle) {
            DebugLog("AttachRigidbodyParenting");

            MoveHandleToSocket(this.transform, objRigidbody, handle);

            attachedTransform = objRigidbody.transform;

            Rigidbody thisRigidbody = this.GetComponentInParent<Rigidbody>();
            if (thisRigidbody != null)
                MassRedistribution(thisRigidbody, objRigidbody);

            RigidbodyDisabled.ParentRigidbody(this.transform, objRigidbody);

#if pHUMANOID
            Humanoid.HumanoidNetworking.DisableNetworkSync(attachedTransform.gameObject);
#endif

            attachedHandle = handle;
            handle.socket = this;
        }

        protected virtual void AttachRigidbodyJoint(Rigidbody objRigidbody, Handle handle) {
            DebugLog("AttachRigidbodyJoint " + objRigidbody);

            //MassRedistribution(thisRididbody, objRigidbody);

            MoveHandleToSocket(this.transform, handle);

            ConfigurableJoint joint = this.gameObject.AddComponent<ConfigurableJoint>();
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;

            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;

            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionDistance = 0.01F;
            joint.projectionAngle = 1;

            Collider c = objRigidbody.transform.GetComponentInChildren<Collider>();
            joint.connectedBody = c.attachedRigidbody;

            attachedTransform = objRigidbody.transform;
            attachedHandle = handle;
            handle.socket = this;
        }

        /// <summary>Attach handle to socket using a static joint on the handle</summary>
        protected virtual void AttachRigidbodyReverseJoint(Rigidbody objRigidbody, Handle handle) {
            DebugLog("AttachRigidbodyReverseJoint " + objRigidbody);

            MoveHandleToSocket(this.transform, handle);

            ConfigurableJoint joint = objRigidbody.gameObject.AddComponent<ConfigurableJoint>();
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;

            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;

            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionDistance = 0.01F;
            joint.projectionAngle = 1;

            joint.breakForce = float.PositiveInfinity;
            joint.breakTorque = float.PositiveInfinity;

            attachedTransform = objRigidbody.transform;
            attachedHandle = handle;
            handle.socket = this;
        }

        protected virtual void AttachSocketParenting(Rigidbody objRigidbody, Handle handle, Rigidbody socketRigidbody) {
            DebugLog("AttachSocketParenting");

            RigidbodyDisabled.ParentRigidbody(objRigidbody, socketRigidbody);

            MoveSocketToHandle(this.transform, handle);

            attachedTransform = objRigidbody.transform;
            attachedHandle = handle;
            handle.socket = this;
            attachMethod = AttachMethod.SocketParenting;
        }


        protected float originalMass = 1;
        protected bool originalUseGravity = false;
        protected virtual void MassRedistribution(Rigidbody socketRigidbody, Rigidbody objRigidbody) {
            originalMass = socketRigidbody.mass;
            socketRigidbody.mass = HybridPhysics.CalculateTotalMass(objRigidbody);

            originalUseGravity = socketRigidbody.useGravity;
            // This gives an issue when attaching things to a socket which is already in the hand
            // It will enable gravity on the hand in that case (not on the object in the hand)
            // Which gives less stable hand tracking.
            // Update: added additional check such that gravity is only enabled when
            // holding heavy objects
            if (objRigidbody.useGravity && objRigidbody.mass > HybridPhysics.kinematicMass)
                socketRigidbody.useGravity = true;
        }

        #endregion Rigidbody

        #region Static Object

        virtual protected bool AttachStaticObject(Transform objTransform, Handle handle) {
            DebugLog("AttachStaticObject " + objTransform);

            Rigidbody thisRigidbody = this.GetComponentInParent<Rigidbody>();
            if (thisRigidbody == null) {
                Debug.LogWarning("Cannot attach static handle to static socket");
                return false;
            }

            // To do: rangecheck

            if (thisRigidbody.isKinematic) {
                AttachSocketParenting(objTransform, handle, thisRigidbody);
            }
            else {
                MoveSocketToHandle(this.transform, handle);
                if (handle.grabType == Handle.GrabType.RailGrab)
                    AttachStaticJointRotY(objTransform);
                else
                    AttachStaticJoint(objTransform);
            }

            releasingTransform = null;
            attachedTransform = objTransform;
            attachedHandle = handle;
            handle.socket = this;
            return true;
        }

        protected virtual void AttachSocketParenting(Transform objTransform, Handle handle, Rigidbody thisRigidbody) {
            DebugLog("AttachSocketParenting");

            RigidbodyDisabled.ParentRigidbody(objTransform, thisRigidbody);

            MoveSocketToHandle(this.transform, handle);
        }

        public virtual void AttachStaticJoint(Transform objTransform) {
            DebugLog("AttachStaticJoint");

            FixedJoint joint = this.transform.gameObject.AddComponent<FixedJoint>();

            Collider c = objTransform.GetComponentInChildren<Collider>();
            if (c == null)
                c = objTransform.GetComponentInParent<Collider>();
            joint.connectedBody = c.attachedRigidbody;
        }

        virtual protected void AttachStaticJointRotY(Transform objTransform) {
            DebugLog("AttachStaticJointRotY is still fixed");

            FixedJoint joint = this.transform.gameObject.AddComponent<FixedJoint>();

            Collider c = objTransform.GetComponentInChildren<Collider>();
            if (c == null)
                c = objTransform.GetComponentInParent<Collider>();
            joint.connectedBody = c.attachedRigidbody;
        }

        #endregion Static Object

        #endregion Attach

        #region Release

        /// <summary>Releases a Transform from the socket</summary>
        /// Note that if the Transform is not taken out of the range of the socket
        /// or held by another Socket, it will automatically snap back to the Socket.
        public virtual void Release(bool releaseSticky = false) {
            DebugLog("Release " + attachedTransform);

            if (attachedTransform == null) {
                attachedHandle = null;
                return;
            }

            if (attachedHandle != null && attachedHandle.sticky && !releaseSticky)
                return;

            Rigidbody thisRigidbody = this.GetComponentInParent<Rigidbody>();
            Rigidbody objRigidbody = attachedTransform.GetComponentInParent<Rigidbody>();
            RigidbodyDisabled objRigidbodyDisabled = RigidbodyDisabled.Get(attachedTransform);
            // RigidbodyDisabled.GetInParent does not work well here.
            // When a socket is held in another socket, it can find the disabledRigidbody of the other socket
            RigidbodyDisabled thisRigidbodyDisabled = RigidbodyDisabled.Get(this.transform);

            if (attachMethod == AttachMethod.SocketParenting || thisRigidbodyDisabled != null)
                ReleaseSocketParenting(objRigidbody, this.transform);
            else if (objRigidbodyDisabled != null)
                ReleaseRigidbodyParenting();            
            else if (objRigidbody == null)
                ReleaseStaticObject();
            else if (thisRigidbody != null && thisRigidbody.isKinematic)
                ReleaseTransformParenting();
            else if (!objRigidbody.isKinematic && thisRigidbody == null)
                ReleaseRigidbodyReverseJoint();
            else
                ReleaseRigidbodyJoint();

            Handle handle = attachedHandle; // attachedTransform.GetComponentInChildren<Handle>();
            if (handle != null) {
                handle.socket = null;
            }

            this.releasingTransform = this.attachedTransform;
            this.attachedTransform = null;
            this.attachedHandle = null;
            this.attachMethod = AttachMethod.Unknown;

            StartCoroutine(ClearReleasingTransform());
        }

        // Released objects should be gone within 1 second or they will be reattached
        protected IEnumerator ClearReleasingTransform() {
            yield return new WaitForSeconds(1);
            this.releasingTransform = null;
        }

        #region Rigidbody

        protected virtual void ReleaseRigidbodyParenting() {
            DebugLog("Release Rigidbody from Parenting");

            Rigidbody thisRigidbody = this.GetComponentInParent<Rigidbody>();
            Rigidbody objRigidbody;
            if (thisRigidbody == null)
                objRigidbody = RigidbodyDisabled.UnparentRigidbody(this.transform, attachedTransform);
            else
                objRigidbody = RigidbodyDisabled.UnparentRigidbody(thisRigidbody, attachedTransform);

            MassRestoration(thisRigidbody, objRigidbody);

#if pHUMANOID
            Humanoid.HumanoidNetworking.ReenableNetworkSync(objRigidbody.gameObject);
#endif

            if (thisRigidbody != null) {
                objRigidbody.velocity = thisRigidbody.velocity;
                objRigidbody.angularVelocity = thisRigidbody.angularVelocity;
            }

#if pHUMANOID
            // check if the object is released from an hybrid physics rigidbody (just hands for now)
            Humanoid.AdvancedHandPhysics handPhysics = this.transform.GetComponentInParent<Humanoid.AdvancedHandPhysics>();
            if (handPhysics != null) {
                Collider[] objColliders = objRigidbody.GetComponentsInChildren<Collider>();
                foreach (Collider objCollider in objColliders)
                    Target.UnsetColliderToTrigger(handPhysics.handTarget.colliders, objCollider);
            }
#endif
        }

        protected virtual void ReleaseRigidbodyJoint() {
            DebugLog("Release from Joint");

            Joint[] joints = this.gameObject.GetComponents<Joint>();
            foreach (Joint joint in joints) {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(joint, true);
                else
#endif
                    Destroy(joint);
            }
            //MassRestoration(..., ...);

            // Trick: when released and immediately attached to anther socket (e.g. grabbing)
            // the joints are not yet destroyed, because Destroy is executed with a delay.
            // Adding the DestroyedJoints component indicates that the joints which may
            // still be there are to be destroyed.
            attachedTransform.gameObject.AddComponent<DestroyedJoints>();
        }

        protected void ReleaseRigidbodyReverseJoint() {
            DebugLog("Release from reverse Joint");

            Joint[] joints = attachedTransform.GetComponents<Joint>();
            foreach (Joint joint in joints) {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(joint, true);
                else
#endif
                    Destroy(joint);
            }
            //MassRestoration(..., ...);

            // Trick: when released and immediately attached to anther socket (e.g. grabbing)
            // the joints are not yet destroyed, because Destroy is executed with a delay.
            // Adding the DestroyedJoints component indicates that the joints which may
            // still be there are to be destroyed.
            attachedTransform.gameObject.AddComponent<DestroyedJoints>();
        }
        protected virtual void ReleaseSocketParenting(Rigidbody objRigidbody, Transform socketTransform) {
            DebugLog("ReleaseSocketParenting");

            RigidbodyDisabled.UnparentRigidbody(objRigidbody, socketTransform);
        }

        protected virtual void MassRestoration(Rigidbody socketRigidbody, Rigidbody objRigidbody) {
            if (socketRigidbody != null) {
                socketRigidbody.mass = originalMass;
                socketRigidbody.useGravity = originalUseGravity;
            }
        }

        #endregion Rigidbody

        #region Static Object

        protected virtual void ReleaseStaticObject() {
            DebugLog("ReleaseStaticObject");

            Rigidbody thisRigidbody = this.GetComponent<Rigidbody>();
            RigidbodyDisabled thisDisabledRigidbody = RigidbodyDisabled.Get(this.transform);

            if (thisRigidbody != null)
                ReleaseStaticJoint();
            else if (thisDisabledRigidbody != null)
                ReleaseSocketParenting(attachedTransform);
            else
                ReleaseTransformParenting();
        }

        public virtual void ReleaseStaticJoint() {
            DebugLog("ReleaseStaticJoint");

            Joint[] joints = this.GetComponents<Joint>();
            foreach (Joint joint in joints) {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(joint, true);
                else
#endif
                    Destroy(joint);
            }
        }

        protected void ReleaseTransformParenting() {
            DebugLog("Release Transform from Parenting");

            attachedTransform.SetParent(attachedTransformParent);
        }

        protected void ReleaseSocketParenting(Transform objTransform) {
            DebugLog("ReleaseSocketParenting");

            RigidbodyDisabled.UnparentRigidbody(objTransform, this.transform);
        }

        #endregion Static Object

        #endregion Release

        #region Init

        protected virtual void Awake() {
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnload;
        }

        #endregion

        #region Update
        protected virtual void Update() {
            UpdateHolding();
        }
        #endregion

        #region Events

        protected static string[] attachEventTypeLabels = {
                "Never",
                "On Attach",
                "On Release",
                "While Attached",
                "While Released",
                "When Changed",
                "Always"
        };

        /// <summary>A GameObject Event for triggering changes in the Transform held by the Socket</summary>
        public GameObjectEventHandlers attachEvent = new GameObjectEventHandlers() {
            label = "Hold Event",
            tooltip =
                "Call functions using what the socket is holding\n" +
                "Parameter: the GameObject held by the socket",
            eventTypeLabels = attachEventTypeLabels
        };

        protected void UpdateHolding() {
            if (attachedTransform != null)
                attachEvent.value = attachedTransform.gameObject;
            else
                attachEvent.value = null;
        }

        protected virtual void OnSceneUnload(UnityEngine.SceneManagement.Scene _) {
            if (destroyOnLoad) {
                if (attachedTransform != null) {
                    Destroy(attachedTransform.gameObject);
                }
            }
        }


        #endregion

        #region Gizmos
        protected Mesh gizmoMesh;

        public void OnDrawGizmosSelected() {
            if (gizmoMesh == null)
                gizmoMesh = GenerateGizmoMesh1();
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireMesh(gizmoMesh, transform.position, transform.rotation);
        }

        public static Mesh GenerateGizmoMesh1() {
            Vector3 p0 = new Vector3(-0.01F, 0.03F, -0.01F);
            Vector3 p1 = new Vector3(-0.01F, -0.03F, -0.01F);
            Vector3 p2 = new Vector3(-0.01F, -0.03F, 0.01F);
            Vector3 p3 = new Vector3(-0.01F, 0.01F, 0.01F);
            Vector3 p4 = new Vector3(-0.01F, 0.01F, 0.03F);
            Vector3 p5 = new Vector3(-0.01F, 0.03F, 0.03F);

            Vector3 p6 = new Vector3(0.01F, 0.03F, -0.01F);
            Vector3 p7 = new Vector3(0.01F, -0.03F, -0.01F);
            Vector3 p8 = new Vector3(0.01F, -0.03F, 0.01F);
            Vector3 p9 = new Vector3(0.01F, 0.01F, 0.01F);
            Vector3 p10 = new Vector3(0.01F, 0.01F, 0.03F);
            Vector3 p11 = new Vector3(0.01F, 0.03F, 0.03F);

            Vector3[] vertices = {
                p0, p1, p6, p7,     // 0, 1, 2, 3
                p1, p2, p7, p8,     // 4, 5, 6, 7
                p2, p3, p8, p9,     // 8, 9, 10, 11
                p3, p4, p9, p10,    // 12, 13, 14, 15
                p4, p5, p10, p11,   // 16, 17, 18, 19
                p5, p0, p11, p6,    // 20, 21, 22, 23

                //p0, p1, p2, p3, p4, p5,     // 24, 25, 26, 27, 28, 29
                //p6, p7, p8, p9, p10, p11,   // 30, 31, 32, 33, 34, 35
            };
            int[] triangles = {
                0, 2, 2,     3, 1, 1,   0, 1, 1,    3, 2, 2,
                4, 6, 6,     7, 5, 5,   4, 5, 5,    7, 6, 6,
                8, 10, 10,   11, 9, 9,  8, 9, 9,    11, 10, 10,
                12, 14, 14,  15, 13, 13,    12, 13, 13,     15, 14, 14,
                16, 18, 18,  19, 17, 17,    16, 17, 17,     19, 18, 18,
                20, 22, 22,  23, 21, 21,    20, 21, 21,     23, 22, 22,

            };
            Vector3[] normals = {
                Vector3.back, Vector3.back, Vector3.back, Vector3.back, //back
                Vector3.down, Vector3.down, Vector3.down, Vector3.down, //down  
                Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward, //forward
                Vector3.down, Vector3.down, Vector3.down, Vector3.down, //down  
                Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward, //forward
                Vector3.up, Vector3.up, Vector3.up, Vector3.up, //up

                //Vector3.left, Vector3.left, Vector3.left, Vector3.left, Vector3.left, Vector3.left,
                //Vector3.right, Vector3.right, Vector3.right, Vector3.right, Vector3.right, Vector3.right,
            };
            Mesh gizmoMesh = new Mesh() {
                vertices = vertices,
                triangles = triangles,
                normals = normals
            };
            return gizmoMesh;
        }

        public static Mesh GenerateGizmoMesh() {
            Vector3 p0 = new Vector3(-0.01F, 0.03F, -0.01F);
            Vector3 p1 = new Vector3(-0.01F, -0.03F, -0.01F);
            Vector3 p2 = new Vector3(-0.01F, -0.03F, 0.01F);
            Vector3 p3 = new Vector3(-0.01F, 0.01F, 0.01F);
            Vector3 p4 = new Vector3(-0.01F, 0.01F, 0.03F);
            Vector3 p5 = new Vector3(-0.01F, 0.03F, 0.03F);

            Vector3 p6 = new Vector3(0.01F, 0.03F, -0.01F);
            Vector3 p7 = new Vector3(0.01F, -0.03F, -0.01F);
            Vector3 p8 = new Vector3(0.01F, -0.03F, 0.01F);
            Vector3 p9 = new Vector3(0.01F, 0.01F, 0.01F);
            Vector3 p10 = new Vector3(0.01F, 0.01F, 0.03F);
            Vector3 p11 = new Vector3(0.01F, 0.03F, 0.03F);

            Vector3[] vertices = {
                p0, p1, p6, p7,     // 0, 1, 2, 3
                p1, p2, p7, p8,     // 4, 5, 6, 7
                p2, p3, p8, p9,     // 8, 9, 10, 11
                p3, p4, p9, p10,    // 12, 13, 14, 15
                p4, p5, p10, p11,   // 16, 17, 18, 19
                p5, p0, p11, p6,    // 20, 21, 22, 23

                p0, p1, p2, p3, p4, p5,     // 24, 25, 26, 27, 28, 29
                p6, p7, p8, p9, p10, p11,   // 30, 31, 32, 33, 34, 35
            };
            int[] triangles = {
                0, 2, 1,     3, 1, 2,
                4, 6, 5,     7, 5, 6,
                8, 10, 9,    11, 9, 10,
                12, 14, 13,  15, 13, 14,
                16, 18, 17,  19, 17, 18,
                20, 22, 21,  23, 21, 22,
                27,24,25,   27,25,26,   29,27,28,   27,29,24,
                30,33,31,   31,33,32,   33,35,34,   35,33,30,
            };
            Vector3[] normals = {
                Vector3.back, Vector3.back, Vector3.back, Vector3.back, //back
                Vector3.down, Vector3.down, Vector3.down, Vector3.down, //down  
                Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward, //forward
                Vector3.down, Vector3.down, Vector3.down, Vector3.down, //down  
                Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward, //forward
                Vector3.up, Vector3.up, Vector3.up, Vector3.up, //up

                Vector3.left, Vector3.left, Vector3.left, Vector3.left, Vector3.left, Vector3.left,
                Vector3.right, Vector3.right, Vector3.right, Vector3.right, Vector3.right, Vector3.right,
            };
            Mesh gizmoMesh = new Mesh() {
                vertices = vertices,
                triangles = triangles,
                normals = normals
            };
            return gizmoMesh;
        }
        #endregion
    }

    // <summary>Component to indicate that the joints are being destroyed</summary>
    // When a rigidbody is released and immediately attached to anther socket (e.g. grabbing)
    // the joints may not yet be destroyed when they are grabbed, because Destroy is executed with a delay.
    // Adding the DestroyedJoints component indicates that the joints which may
    // still be there are to be destroyed.
    public class DestroyedJoints : MonoBehaviour {
        private void LateUpdate() {
            Destroy(this);
        }
    }
}