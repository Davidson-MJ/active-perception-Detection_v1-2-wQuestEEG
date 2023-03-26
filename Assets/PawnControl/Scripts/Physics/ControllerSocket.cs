using UnityEngine;

namespace Passer.Pawn {

    /// <summary>
    /// A Socket attached to a hand
    /// </summary>
    public class ControllerSocket : Socket {

        public PawnHand controllerTarget;

        protected override void MoveSocketToHandle(Transform socketTransform, Handle handle) {
            Debug.Log("MoveControllerToHandle");

            Quaternion socket2handRotation = Quaternion.Inverse(controllerTarget.grabSocket.transform.localRotation);
            controllerTarget.transform.rotation = handle.worldRotation * socket2handRotation;

            Vector3 socket2HandPosisition = controllerTarget.transform.position - controllerTarget.grabSocket.transform.position;
            controllerTarget.transform.position = handle.worldPosition + socket2HandPosisition;
        }

        override public Vector3 worldPosition {
            get {
                Vector3 controllerPosition = controllerTarget.transform.position;
                Vector3 controller2Socket = controllerTarget.grabSocket.transform.position - controllerTarget.transform.position;
                Vector3 socketPosition = controllerPosition + controller2Socket;
                return socketPosition;
            }
        }

        #region Attach

        public override bool Attach(Handle handle, bool rangeCheck = true) {
            DebugLog("Attach " + handle);

            bool success = base.Attach(handle, rangeCheck);
            if (!success)
                return success;

            ControllerInput globalInput = controllerTarget.pawn.GetComponent<ControllerInput>();
            if (globalInput == null)
                return success;

            if (controllerTarget.isLeft) {
                if (globalInput != null) {
                    for (int i = 0; i < handle.inputEvents.Length; i++) {
                        if (handle.inputEvents[i].events != null && handle.inputEvents[i].events.Count > 0 &&
                            handle.inputEvents[i].events[0].eventType != EventHandler.Type.Never)
                            globalInput.leftInputEvents[i].events.Insert(0, handle.inputEvents[i].events[0]);
                    }
                }
            }
            else {
                if (globalInput != null) {
                    for (int i = 0; i < handle.inputEvents.Length; i++) {
                        if (handle.inputEvents[i].events != null && handle.inputEvents[i].events.Count > 0 &&
                            handle.inputEvents[i].events[0].eventType != EventHandler.Type.Never)
                            globalInput.rightInputEvents[i].events.Insert(0, handle.inputEvents[i].events[0]);
                    }
                }
            }
            return success;
        }

        #region Rigidbody

        protected override void AttachRigidbodyJoint(Rigidbody objRigidbody, Handle handle) {
            Debug.Log("(ControllerSocket) AttachRigidbodyJoint " + objRigidbody);
            //MoveHandleToSocket(this.transform, handle);

            ConfigurableJoint joint = controllerTarget.gameObject.AddComponent<ConfigurableJoint>();
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;

            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;

            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionDistance = 0.01F;
            joint.projectionAngle = 1;

            joint.breakForce = 100;
            joint.breakTorque = 100;

            Collider c = objRigidbody.transform.GetComponentInChildren<Collider>();
            joint.connectedBody = c.attachedRigidbody;

            attachedTransform = objRigidbody.transform;
            attachedHandle = handle;
            handle.socket = this;
        }

        #endregion

        #region Static

        public override void AttachStaticJoint(Transform objTransform) {
            FixedJoint joint = controllerTarget.gameObject.AddComponent<FixedJoint>();

            Debug.Log(objTransform);
            Collider c = objTransform.GetComponentInChildren<Collider>();
            if (c == null)
                c = objTransform.GetComponentInParent<Collider>();
            joint.connectedBody = c.attachedRigidbody;
        }

        #endregion

        #endregion

        #region Release

        public override void Release(bool releaseSticky = false) {
            DebugLog("Release");

            if (attachedHandle != null) {
                if (attachedHandle.sticky && !releaseSticky)
                    return;

                ControllerInput globalInput = controllerTarget.pawn.GetComponent<ControllerInput>();
                if (globalInput == null)
                    return;

                for (int i = 0; i < attachedHandle.inputEvents.Length; i++) {
                    if (attachedHandle.inputEvents[i].events == null || attachedHandle.inputEvents[i].events.Count == 0)
                        continue;

                    if (controllerTarget.isLeft)
                        globalInput.leftInputEvents[i].events.RemoveAll(x => x == attachedHandle.inputEvents[i].events[0]);
                    else
                        globalInput.rightInputEvents[i].events.RemoveAll(x => x == attachedHandle.inputEvents[i].events[0]);
                }

            }

            base.Release();
        }

        #region Static

        protected override void ReleaseStaticObject() {
            Debug.Log("ReleaseStaticObject");

            Rigidbody thisRigidbody = controllerTarget.controllerRigidbody;
            RigidbodyDisabled thisDisabledRigidbody = this.GetComponent<RigidbodyDisabled>();

            if (thisRigidbody != null)
                ReleaseStaticJoint();
            else if (thisDisabledRigidbody != null)
                ReleaseSocketParenting(attachedTransform);
            else
                ReleaseTransformParenting();
        }

        public override void ReleaseStaticJoint() {
            Debug.Log("ReleaseStaticJoint");
            Joint[] joints = controllerTarget.controllerRigidbody.GetComponents<Joint>();
            foreach (Joint joint in joints) {
#if UNITY_EDITOR
                DestroyImmediate(joint, true);
#else
                Destroy(joint);
#endif
            }
        }

        #endregion

        #endregion
    }
}