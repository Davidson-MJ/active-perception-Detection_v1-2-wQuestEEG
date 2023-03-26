using System.Collections.Generic;
using UnityEngine;

namespace Passer.Humanoid {

    /// <summary>
    /// A Socket attached to a hand
    /// </summary>
    public class HandSocket : Socket {

        /// <summary>
        /// The handTarget of the socket
        /// </summary>
        /// This is the HandTarget of the hand to which the socket is attached
        public HandTarget handTarget;

        protected override void MoveHandleToSocket(Transform socketTransform, Handle handle) {
            DebugLog("MoveHandleToHand");

            Transform handleTransform = handle.GetComponent<Transform>();
            Rigidbody handleRigidbody = handle.GetComponentInParent<Rigidbody>();
            if (handleRigidbody != null)
                handleTransform = handleRigidbody.transform;

            handleTransform.rotation = handle.RotationTo(socketTransform.rotation) * handleTransform.rotation;
            handleTransform.position += handle.TranslationTo(socketTransform.position);
        }

        protected override void MoveSocketToHandle(Transform socketTransform, Handle handle) {
            DebugLog("MoveHandToHandle");

            if (handle.grabType == Handle.GrabType.RailGrab) {

                // Project Socket rotation on rail

                Vector3 handleYaxis = handle.transform.up;
                Vector3 socketYaxis = handTarget.grabSocket.transform.up;
                float angle = Vector3.Angle(handleYaxis, socketYaxis);
                if (angle > 90)
                    socketYaxis = -handTarget.grabSocket.transform.up;

                Quaternion socketToHandleRotation = Quaternion.FromToRotation(socketYaxis, handleYaxis);
                Quaternion targetRotation = socketToHandleRotation * handTarget.grabSocket.transform.rotation;

                //Debug.DrawRay(handle.transform.position, handle.transform.rotation * Vector3.up, Color.blue);
                //Debug.DrawRay(handTarget.grabSocket.transform.position, handTarget.grabSocket.transform.rotation * Vector3.up, Color.green);
                //Debug.DrawRay(handTarget.grabSocket.transform.position, targetRotation * Vector3.up);
                //Debug.Break();

                Quaternion socket2handRotation = Quaternion.Inverse(handTarget.grabSocket.transform.localRotation);
                handTarget.hand.bone.transform.rotation = targetRotation * socket2handRotation;

                // Project Socket on Rail

                // Socket along rail
                Vector3 localSocketPosition = handTarget.grabSocket.transform.position - handle.transform.position;
                Vector3 targetPosition = Vector3.Project(localSocketPosition, handle.transform.up);
                //Debug.DrawRay(handle.transform.position, handle.transform.up, Color.green);
                //Debug.DrawRay(handle.transform.position, targetPosition, Color.magenta);

                // Socket within rail length
                float maxDistance = handle.transform.lossyScale.y / 2;
                float distance = Mathf.Clamp(targetPosition.magnitude, -maxDistance, maxDistance);
                float scale = distance / targetPosition.magnitude;
                targetPosition = Vector3.Scale(targetPosition, Vector3.one * scale);
                //Debug.DrawRay(handle.transform.position, targetPosition, Color.cyan);

                targetPosition = handle.transform.position + targetPosition;
                //Debug.DrawLine(handTarget.grabSocket.transform.position, targetPosition);

                Vector3 socket2HandPosition = handTarget.hand.bone.transform.position - handTarget.grabSocket.transform.position;
                handTarget.hand.bone.transform.position = targetPosition + socket2HandPosition;
            }
            else {
                Quaternion socket2handRotation = Quaternion.Inverse(handTarget.grabSocket.transform.localRotation);
                handTarget.hand.bone.transform.rotation = handle.worldRotation * socket2handRotation;

                Vector3 socket2HandPosition = handTarget.hand.bone.transform.position - handTarget.grabSocket.transform.position;
                handTarget.hand.bone.transform.position = handle.worldPosition + socket2HandPosition;
            }
        }

        protected override void MassRedistribution(Rigidbody socketRigidbody, Rigidbody objRigidbody) {
            originalMass = socketRigidbody.mass;
            socketRigidbody.mass = objRigidbody.mass;
        }

        #region Attach

        public override bool Attach(Handle handle, bool rangeCheck = true) {
            DebugLog("Attach " + handle);

            bool success = base.Attach(handle, rangeCheck);
            if (!success)
                return success;

            ControllerInput globalInput = handTarget.humanoid.GetComponent<ControllerInput>();
            if (globalInput == null)
                return success;

            if (handTarget.isLeft) {
                if (globalInput != null) {
                    if ((handle.inputEvents.Length < 13 && globalInput.leftInputEvents.Length < 13) ||
                        (handle.inputEvents.Length == 13 && globalInput.leftInputEvents.Length == 13)) {

                        for (int i = 0; i < handle.inputEvents.Length; i++)
                            CopyEventHandler(handle.inputEvents[i], globalInput.leftInputEvents[i]);

                    }
                    else if (handle.inputEvents.Length < 13) {
                        for (int i = 0; i < 3; i++)
                            CopyEventHandler(handle.inputEvents[i], globalInput.leftInputEvents[i]);
                        for (int i = 3; i < handle.inputEvents.Length; i++)
                            CopyEventHandler(handle.inputEvents[i], globalInput.leftInputEvents[i + 3]);
                    }
                    else {
                        for (int i = 0; i < 3; i++)
                            CopyEventHandler(handle.inputEvents[i], globalInput.leftInputEvents[i]);
                        for (int i = 3; i < handle.inputEvents.Length - 3; i++)
                            CopyEventHandler(handle.inputEvents[i + 3], globalInput.rightInputEvents[i]);
                    }
                }
            }
            else {
                if (globalInput != null) {
                    if ((handle.inputEvents.Length < 13 && globalInput.rightInputEvents.Length < 13) ||
                        (handle.inputEvents.Length == 13 && globalInput.rightInputEvents.Length == 13)) {

                        for (int i = 0; i < handle.inputEvents.Length; i++)
                            CopyEventHandler(handle.inputEvents[i], globalInput.rightInputEvents[i]);
                    }
                    else if (handle.inputEvents.Length < 13) {
                        for (int i = 0; i < 3; i++)
                            CopyEventHandler(handle.inputEvents[i], globalInput.rightInputEvents[i]);
                        for (int i = 3; i < handle.inputEvents.Length; i++)
                            CopyEventHandler(handle.inputEvents[i], globalInput.rightInputEvents[i + 3]);
                    }
                    else {
                        for (int i = 0; i < 3; i++)
                            CopyEventHandler(handle.inputEvents[i], globalInput.rightInputEvents[i]);
                        for (int i = 3; i < handle.inputEvents.Length - 3; i++)
                            CopyEventHandler(handle.inputEvents[i + 3], globalInput.rightInputEvents[i]);
                    }
                }
            }


            return success;
        }

        private void CopyEventHandler(ControllerEventHandlers source, ControllerEventHandlers destination) {
            if (source == null || source.events == null ||
                destination == null || destination.events == null)
                return;

            for (int i = 0; i < source.events.Count; i++) {
                if (source.events[i].eventType != EventHandler.Type.Never)
                    destination.events.Insert(i, source.events[i]);
            }
        }

        #region Rigidbody

        protected override bool AttachRigidbody(Rigidbody objRigidbody, Handle handle, bool rangeCheck = true) {
            DebugLog("AttachRigidbody " + objRigidbody);

            if (handle.grabType == Handle.GrabType.RailGrab) {
                Vector3 localSocketPosition = this.transform.position - handle.worldPosition;
                Vector3 localTargetPosition = Vector3.Project(localSocketPosition, handle.transform.up);
                float grabDistance = localTargetPosition.magnitude;
                if (rangeCheck && handle.range > 0 && grabDistance > handle.range) {
                    Debug.Log("Socket is outside range of handle");
                    return false;
                }
            }
            else {
                float grabDistance = Vector3.Distance(this.transform.position, handle.worldPosition);
                if (rangeCheck && handle.range > 0 && grabDistance > handle.range) {
                    Debug.Log("Socket is outside range of handle");
                    return false;
                }
            }

            Transform objTransform = objRigidbody.transform;

            Rigidbody thisRigidbody = this.GetComponentInParent<Rigidbody>();
            Joint joint = objRigidbody.GetComponent<Joint>();
            // See if these joints are being destroyed
            DestroyedJoints destroyedJoints = objRigidbody.GetComponent<DestroyedJoints>();

            // Check if we are grabbing a hand
            BasicHandPhysics handPhysics = objRigidbody.GetComponent<BasicHandPhysics>();
            if (handPhysics != null) { // We are grabbing a hand
                if (thisRigidbody == null) {
                    DebugLog("Cannot attach to hand because this handRigidbody is not present");
                    return false;
                }
                AttachSocketParenting(objRigidbody, handle, thisRigidbody);
            }
            else
            if (objRigidbody.isKinematic) {
                if (thisRigidbody == null)
                    AttachSocketParenting(objRigidbody, handle, thisRigidbody);
                else if (thisRigidbody == null)
                    AttachRigidbodyParenting(objRigidbody, handle);
                else if (thisRigidbody.isKinematic)
                    AttachTransformParenting(objRigidbody.transform, handle);
                else
                    AttachSocketParenting(objRigidbody, handle, thisRigidbody);
            }
            else if (thisRigidbody == null) {
                AttachRigidbodyReverseJoint(objRigidbody, handle);
            }
            else if (
                (joint != null && destroyedJoints == null) ||
                objRigidbody.constraints != RigidbodyConstraints.None
                ) {

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

        protected override void AttachRigidbodyParenting(Rigidbody objRigidbody, Handle handle) {
            DebugLog("AttachRigidbodyParenting");

            if (objRigidbody.mass > HybridPhysics.kinematicMass)
                MoveSocketToHandle(this.transform, handle);
            else
                MoveHandleToSocket(this.transform, objRigidbody, handle);

            attachedTransform = objRigidbody.transform;

            Rigidbody thisRigidbody = this.GetComponentInParent<Rigidbody>();
            if (thisRigidbody != null)
                MassRedistribution(thisRigidbody, objRigidbody);

            RigidbodyDisabled.ParentRigidbody(this.transform, objRigidbody);

#if pHUMANOID
            HumanoidNetworking.DisableNetworkSync(attachedTransform.gameObject);
            if (!handTarget.humanoid.isRemote) {
                //Debug.Log("Take Ownership");
                HumanoidNetworking.TakeOwnership(attachedTransform.gameObject);
            }
#endif

            attachedHandle = handle;
            handle.socket = this;
        }

        protected override void AttachRigidbodyJoint(Rigidbody objRigidbody, Handle handle) {
            DebugLog("AttachRigidbodyJoint " + objRigidbody);

            //MassRedistribution(thisRididbody, objRigidbody);

            MoveHandleToSocket(this.transform, handle);

            ConfigurableJoint joint = handTarget.handRigidbody.gameObject.AddComponent<ConfigurableJoint>();
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

        protected override void AttachSocketParenting(Rigidbody objRigidbody, Handle handle, Rigidbody socketRigidbody) {
            DebugLog("AttachSocketParenting");

            RigidbodyDisabled.ParentRigidbody(objRigidbody, socketRigidbody);
            handTarget.handRigidbody = null;

            MoveSocketToHandle(this.transform, handle);

            attachedTransform = objRigidbody.transform;
            attachedHandle = handle;
            handle.socket = this;

#if pHUMANOID
            HumanoidNetworking.DisableNetworkSync(attachedTransform.gameObject);
            if (!handTarget.humanoid.isRemote)
                HumanoidNetworking.TakeOwnership(attachedTransform.gameObject);
#endif
            attachMethod = AttachMethod.SocketParenting;
        }

        #endregion Rigidbody

        #region Static

        public override void AttachStaticJoint(Transform objTransform) {
            DebugLog("AttachStaticJoint");

            // Joint is no longer necessary, because the constraint makes sure the hand cannot move
            // Constraints are more stable than fixed joints
            // The constraint does not work, because it is relative to its parent.
            // The socket may therefore not stay at the same world coodinate....
            // So we are back to using a joint again.
            // In general this is true, but detached hands have not parent so we can use constraints

            FixedJoint joint = handTarget.handRigidbody.gameObject.AddComponent<FixedJoint>();

            Collider c = objTransform.GetComponentInChildren<Collider>();
            if (c == null)
                c = objTransform.GetComponentInParent<Collider>();
            joint.connectedBody = c.attachedRigidbody;

            //handTarget.handRigidbody.constraints = RigidbodyConstraints.FreezeAll;
            handTarget.handRigidbody.isKinematic = false;
        }

        #endregion

        #endregion

        #region Release

        public override void Release(bool releaseSticky = false) {
            DebugLog("Release");

            if (attachedHandle != null) {
                if (attachedHandle.sticky && !releaseSticky)
                    return;

                ControllerInput globalInput = handTarget.humanoid.GetComponent<ControllerInput>();
                if (globalInput != null) {

                    for (int i = 0; i < attachedHandle.inputEvents.Length; i++) {
                        if (attachedHandle.inputEvents[i].events == null || attachedHandle.inputEvents[i].events.Count == 0)
                            continue;

                        if (handTarget.isLeft)
                            globalInput.leftInputEvents[i].events.RemoveAll(x => x == attachedHandle.inputEvents[i].events[0]);
                        else
                            globalInput.rightInputEvents[i].events.RemoveAll(x => x == attachedHandle.inputEvents[i].events[0]);
                    }
                }
            }

            base.Release();
        }

        #region Rigidbody

        protected override void ReleaseRigidbodyParenting() {
            DebugLog("Release Rigidbody from Parenting " + attachedTransform);

            bool wasTwoHandedGrab = false;

            BasicHandPhysics[] grabbedPhysicsHands = null;
            if (handTarget.handRigidbody != null) {
                grabbedPhysicsHands = GetComponentsInChildrenOnly(handTarget.handRigidbody.transform);
                if (grabbedPhysicsHands.Length > 0) {
                    // Multiple hand grabbing: releasing the primary hand
                    // We should first release all secondary hands
                    // and have the secondary hands regrab after primary hand has released the grab
                    // There can be more than one secondary hand,
                    // which one will becom the new primary is chosen is not determined
                    DebugLog("+++++ Releasing two-handed primary grab hand");
                    DebugLog("Release other hands first " + grabbedPhysicsHands.Length);
                    foreach (BasicHandPhysics grabbedPhysicsHand in grabbedPhysicsHands) {
                        HandTarget otherHand = grabbedPhysicsHand.handTarget;
                        otherHand.LetGo();
                    }
                    wasTwoHandedGrab = true;
                }
            }
            else
                Debug.LogWarning(handTarget + ": No Hand Rigidbody when letting go");

            DebugLog("Release from primary hand");
            Rigidbody thisRigidbody = this.GetComponentInParent<Rigidbody>();
            Rigidbody objRigidbody;
            if (thisRigidbody == null)
                objRigidbody = RigidbodyDisabled.UnparentRigidbody(this.transform, attachedTransform);
            else
                objRigidbody = RigidbodyDisabled.UnparentRigidbody(thisRigidbody, attachedTransform);

            MassRestoration(thisRigidbody, objRigidbody);

#if pHUMANOID
            HumanoidNetworking.ReenableNetworkSync(objRigidbody.gameObject);
#endif

            if (thisRigidbody != null) {
                objRigidbody.velocity = thisRigidbody.velocity;
                objRigidbody.angularVelocity = thisRigidbody.angularVelocity;
            }

#if pHUMANOID
            // check if the object is released from an hybrid physics rigidbody (just hands for now)
            AdvancedHandPhysics handPhysics = this.transform.GetComponentInParent<AdvancedHandPhysics>();
            if (handPhysics != null) {
                Collider[] objColliders = objRigidbody.GetComponentsInChildren<Collider>();
                foreach (Collider objCollider in objColliders)
                    Target.UnsetColliderToTrigger(handPhysics.handTarget.colliders, objCollider);
            }
#endif
            if (wasTwoHandedGrab) {
                //Debug.Log("Now attach it to the other hands again ");

                // First one will become the primary grabbing hand
                HandTarget primaryHand = grabbedPhysicsHands[0].handTarget;
                if (primaryHand.grabbedHandle != null)
                    primaryHand.GrabHandle(primaryHand.grabbedHandle);
                else
                    primaryHand.Grab(objRigidbody.gameObject, false);

                // The other hands (if any) will be secondary grabbing
                // ans should therefore grab the primary hand
                for (int i = 1; i < grabbedPhysicsHands.Length; i++) {
                    HandTarget otherHand = grabbedPhysicsHands[i].handTarget;
                    otherHand.Grab(primaryHand.handRigidbody.gameObject, false);
                }
            }
        }

        protected BasicHandPhysics[] GetComponentsInChildrenOnly(Transform transform) {
            List<BasicHandPhysics> childHandPhysicsList = new List<BasicHandPhysics>();
            for (int i = 0; i < transform.childCount; i++) {
                BasicHandPhysics handPhysics = transform.GetChild(i).GetComponent<BasicHandPhysics>();
                if (handPhysics != null) {
                    childHandPhysicsList.Add(handPhysics);
                }
            }
            BasicHandPhysics[] childHandPhysics = childHandPhysicsList.ToArray();
            return childHandPhysics;
        }

        protected override void ReleaseRigidbodyJoint() {
            DebugLog("Release from Joint");

            Joint[] joints = handTarget.handRigidbody.GetComponents<Joint>();
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

        protected override void ReleaseSocketParenting(Rigidbody objRigidbody, Transform socketTransform) {
            DebugLog("ReleaseSocketParenting");

            Rigidbody handRigidbody = RigidbodyDisabled.UnparentRigidbody(objRigidbody, handTarget.hand.bone.transform);
            handTarget.handRigidbody = handRigidbody;
        }

        #endregion

        #region Static

        protected override void ReleaseStaticObject() {
            DebugLog("ReleaseStaticObject");

            Rigidbody thisRigidbody = handTarget.handRigidbody;
            RigidbodyDisabled thisDisabledRigidbody = this.GetComponent<RigidbodyDisabled>();

            if (thisRigidbody != null)
                ReleaseStaticJoint();
            else if (thisDisabledRigidbody != null)
                ReleaseSocketParenting(attachedTransform);
            else
                ReleaseTransformParenting();
        }

        public override void ReleaseStaticJoint() {
            DebugLog("ReleaseStaticJoint");

            // Joint is no longer necessary, because the constraint makes sure the hand cannot move
            // The constraint does not work, because it is relative to its parent.
            // The socket may therefore not stay at the same world coodinate....
            // So we are back to using a joint again.

            Joint[] joints = handTarget.handRigidbody.GetComponents<Joint>();
            foreach (Joint joint in joints) {
#if UNITY_EDITOR
                DestroyImmediate(joint, true);
#else
                            Destroy(joint);
#endif
            }
            //handTarget.handRigidbody.constraints = RigidbodyConstraints.None;
        }

        #endregion

        #endregion

        protected override void MassRestoration(Rigidbody socketRigidbody, Rigidbody objRigidbody) {
            if (socketRigidbody != null)
                socketRigidbody.mass = originalMass;
        }

        public override Vector3 worldPosition {
            get {
                Vector3 handPosition = handTarget.hand.target.transform.position;
                Vector3 hand2Socket = handTarget.grabSocket.transform.position - handTarget.hand.bone.transform.position;
                Vector3 socketPosition = handPosition + hand2Socket;
                return socketPosition;
            }
        }

        public virtual Quaternion worldRotation {
            get {
                Quaternion handRotation = handTarget.hand.target.transform.rotation;
                Quaternion hand2Socket = Quaternion.Inverse(handTarget.hand.bone.targetRotation) * handTarget.grabSocket.transform.rotation;
                Quaternion socketRotation = handRotation * hand2Socket;
                return socketRotation;
            }
        }
    }
}