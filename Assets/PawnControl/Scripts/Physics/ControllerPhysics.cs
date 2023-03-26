using UnityEngine;

namespace Passer.Pawn {

    public class ControllerPhysics : HybridPhysics {

        protected PawnHand controllerTarget;

        #region Start

        override protected void Awake() {
            controllerTarget = GetComponent<PawnHand>();
            thisRigidbody = controllerTarget.controllerRigidbody;
#if pUNITYXR
            target = controllerTarget.unityController.transform;
#elif hLEGACYXR
            target = controllerTarget.unityController.sensorTransform;
#endif

            base.Awake();
        }

        #endregion

        #region Update

        override protected void FixedUpdate() {
            if (hasCollided && !colliding)
                controllerTarget.touchedObject = null;

            if (controllerTarget.touchedObject == null) // Object may be destroyed
                hasCollided = false;

            if (thisRigidbody == null)
                UpdateWithoutRigidbody();
            else
                UpdateRigidbody();

            colliding = false;
        }

        protected override void UpdateKinematicRigidbody() {
            if (mode == PhysicsMode.NonKinematic || thisRigidbody.mass > kinematicMass) {
                SetNonKinematic();
                return;
            }

            force = Vector3.zero;
            torque = Vector3.zero;

            thisRigidbody.MovePosition(target.position);

            if (controllerTarget.twoHandedGrab) {
                // this is the primary grabbing controller
                // otherController is the secondary grabbing controller

                // This assumes the socket is a child of the controllertarget
                Vector3 otherSocketLocalPosition = controllerTarget.otherController.grabSocket.transform.localPosition;
#if pUNITYXR
                // Calculate socket position from unity tracker
                Vector3 otherSocketPosition = controllerTarget.otherController.unityController.transform.TransformPoint(otherSocketLocalPosition);
#elif hLEGACYXR
                // Calculate socket position from unity tracker
                Vector3 otherSocketPosition = controllerTarget.otherController.unityController.sensorTransform.TransformPoint(otherSocketLocalPosition);
#else
                Vector3 otherSocketPosition = Vector3.zero;
#endif

                Vector3 handlePosition = controllerTarget.otherController.grabSocket.attachedHandle.worldPosition;
                Vector3 toHandlePosition = handlePosition - transform.position;
                Quaternion rotateToHandlePosition = Quaternion.FromToRotation(toHandlePosition, transform.forward);

                transform.LookAt(otherSocketPosition, target.up);
                transform.rotation *= rotateToHandlePosition;
                thisRigidbody.MoveRotation(transform.rotation);
            }
            else
                thisRigidbody.MoveRotation(target.rotation);
        }

        #endregion

        #region Events

        override public void OnTriggerEnter(Collider collider) {
            PawnControl pawn = collider.GetComponent<PawnControl>();
            bool otherIsPawn = (pawn != null);

            if (thisRigidbody != null &&
                thisRigidbody.isKinematic &&
                !collider.isTrigger &&
                !otherIsPawn) {

                colliding = true;
                hasCollided = true;

                Rigidbody otherRigidbody = collider.attachedRigidbody;
                if (otherRigidbody != null) {
                    controllerTarget.touchedObject = otherRigidbody.gameObject;
                    SetNonKinematic();
                }
                else {
                    controllerTarget.touchedObject = collider.gameObject;
                    SetNonKinematic();
                }
            }
        }

        override public void OnCollisionStay(Collision collision) {
            // Make sure the collision is not with kinematic child rigidbodies
            if (collision.rigidbody != null) {
                Rigidbody parentRigidbody = collision.rigidbody.transform.parent.GetComponentInParent<Rigidbody>();
                if (parentRigidbody == thisRigidbody) {
                    // we are colliding with a kinematic child rigidbody
                    colliding = false;
                    return;
                }
            }
            colliding = true;
        }

        override public void OnCollisionExit(Collision collision) {
            if (thisRigidbody != null) {
                RaycastHit hit;
                if (!thisRigidbody.SweepTest(target.transform.position - thisRigidbody.position, out hit)) {
                    //ControllerInteraction.OnTouchEnd(handTarget, handTarget.touchedObject);
                    hasCollided = false;
                    controllerTarget.touchedObject = null;
                }
            }
        }

        #endregion

    }

}