using UnityEngine;

namespace Passer.Pawn {

    public partial class PawnHand {
#if UNITY_EDITOR
        public static bool debug = true;
#endif
        protected static void DebugLog(string s) {
#if UNITY_EDITOR
            if (debug)
                Debug.Log(s);
#endif
        }


        public static float maxGrabbingMass = 10; // max mass you can grab

        #region Grab


        /// <summary>
        /// Try to grab the object we touch
        /// </summary>
        public void GrabTouchedObject() {
            if (touchedObject != null)
                Grab(touchedObject);
        }

        /// <summary>
        /// Try to grab an object near to the hand
        /// </summary>
        public void GrabNearObject() {
            GameObject grabObject = DetermineGrabObject();
            if (grabObject == null)
                return;

            Grab(grabObject);
        }

        /// <summary>Grab the object</summary>
        public void Grab(GameObject obj) {
            Grab(obj, false);
        }

        public void Grab(GameObject obj, bool rangeCheck) {
            DebugLog(this + " grabs " + obj);

            if (AlreadyGrabbedWithOtherController(this, obj))
                Grab2(this, obj);

            else {
                Rigidbody objRigidbody = obj.GetComponentInParent<Rigidbody>();
                //                Handle handle = obj.GetComponentInChildren<Handle>();
                Handle handle = Handle.GetClosestHandle(obj.transform, this.transform.position);
                if (handle != null) {
                    GrabHandle(this, objRigidbody, handle, rangeCheck);
                }
                else if (objRigidbody != null) {
                    GrabRigidbodyWithoutHandle(this, objRigidbody);
                }
            }
            if (grabbedObject != null) {
                if (!_pawn.isRemote && _pawn.networking != null)
                    _pawn.networking.Grab(this, obj, false);

                if (physics) {
                    ControllerPhysics controllerPhysics = this.GetComponent<ControllerPhysics>();
                    if (controllerPhysics != null) {
                        controllerPhysics.SetCollidersToTrigger();
                        //controllerPhysics.DeterminePhysicsMode();
                    }
                }
                if (Application.isPlaying) {
                    SendMessage("OnGrabbing", this.grabbedObject, SendMessageOptions.DontRequireReceiver);

                    IHandGrabEvents objectInteraction = this.grabbedObject.GetComponent<IHandGrabEvents>();
                    if (objectInteraction != null)
                        objectInteraction.OnHandGrabbed(this);
                }
            }
        }

        protected GameObject DetermineGrabObject() {
            Collider[] colliders = Physics.OverlapSphere(grabSocket.transform.position, 0.1F);

            GameObject objectToGrab = null;
            bool isHandle = false;
            bool isRigidbody = false;
            foreach (Collider collider in colliders) {

                GameObject obj;
                Rigidbody objRigidbody = collider.attachedRigidbody;
                if (objRigidbody != null)
                    obj = objRigidbody.gameObject;
                else
                    obj = collider.gameObject;
                //if (!CanBeGrabbed(handTarget, obj))
                //    continue;
                // don't grab your own hands
                if (obj == controllerRigidbody.gameObject || obj == otherController.controllerRigidbody.gameObject)
                    continue;

                if (objRigidbody != null) {
                    objectToGrab = obj;
                    isRigidbody = true;
                }
                else if (!isRigidbody) {
                    Handle handle = obj.GetComponentInChildren<Handle>();
                    if (handle != null && !isRigidbody) {
                        objectToGrab = obj;
                        isHandle = true;
                    }
                    else if (!isHandle) {
                        objectToGrab = obj;
                    }
                }
                if (objectToGrab.name.Contains("ArmPalm"))
                    Debug.Log("corrected to arm!!!");
            }
            return objectToGrab;
        }

        protected bool CanBeGrabbed(GameObject obj) {
            if (obj == null || obj == pawn.gameObject ||
                obj == pawn.headTarget.gameObject
                )
                return false;

            // We cannot grab 2D objects like UI
            RectTransform rectTransform = obj.GetComponent<RectTransform>();
            if (rectTransform != null)
                return false;

            return true;

        }

        private static bool AlreadyGrabbedWithOtherController(PawnHand controllerTarget, GameObject obj) {
            if (controllerTarget.otherController == null)
                return false;

            if (obj.transform == controllerTarget.otherController.transform)
                return true;

            Rigidbody objRigidbody = obj.GetComponent<Rigidbody>();
            if (objRigidbody != null && objRigidbody.isKinematic) {
                Transform parent = objRigidbody.transform.parent;
                if (parent == null)
                    return false;

                Rigidbody parentRigidbody = parent.GetComponentInParent<Rigidbody>();
                if (parentRigidbody == null)
                    return false;

                return AlreadyGrabbedWithOtherController(controllerTarget, parentRigidbody.gameObject);
            }

            return false;
        }

        protected static void Grab2(PawnHand controllerTarget, GameObject obj) {
            DebugLog("Grab2 " + obj);

            Handle handle = Handle.GetClosestHandle(obj.transform, controllerTarget.transform.position);// obj.GetComponentInChildren<Handle>();
            if (handle == null)
                return;

            Rigidbody objRigidbody = handle.GetComponentInParent<Rigidbody>();
            if (objRigidbody == null)
                return;

            if (handle != null)
                GrabHandle(controllerTarget, objRigidbody, handle, false);
            else
                GrabRigidbodyParenting(controllerTarget, objRigidbody);

            controllerTarget.otherController.twoHandedGrab = true;
            return;
        }

        private static void GrabHandle(PawnHand handTarget, Rigidbody objRigidbody, Handle handle, bool rangeCheck) {
            DebugLog("GrabHandle " + handle);

            GameObject obj = (objRigidbody != null) ? objRigidbody.gameObject : handle.gameObject;

            if (objRigidbody != null && objRigidbody.isKinematic) {
                DebugLog("Grab Kinematic Rigidbody Handle");
                // When grabbing a kinematic rigidbody, the controller should change
                // to a non-kinematic rigidbody first
                ControllerPhysics controllerPhysics = handTarget.GetComponent<ControllerPhysics>();
                if (controllerPhysics != null)
                    controllerPhysics.SetNonKinematic();
            }

            if (handle.socket != null) {
                DebugLog("Grab from socket");
                handle.socket.Release();
            }
            handTarget.grabSocket.Attach(handle.transform, rangeCheck);
            handTarget.grabbedHandle = handle;

            handTarget.grabbedObject = obj;
        }

        private static void GrabRigidbodyWithoutHandle(PawnHand controllerTarget, Rigidbody objRigidbody) {

            if (objRigidbody.mass > maxGrabbingMass) {
                Debug.Log("Object is too heavy, mass > " + maxGrabbingMass);
                return;
            }

            GrabRigidbodyParenting(controllerTarget, objRigidbody);
            controllerTarget.grabbedRigidbody = true;
        }

        private static void GrabRigidbodyParenting(PawnHand controllerTarget, Rigidbody objRigidbody) {
            DebugLog("GrabRigidbodyParenting " + objRigidbody);

            GameObject obj = objRigidbody.gameObject;

            Rigidbody controllerRigidbody = controllerTarget.GetComponent<Rigidbody>();
            RigidbodyDisabled.ParentRigidbody(controllerRigidbody, objRigidbody);

            controllerTarget.grabbedObject = obj;
        }

        #endregion

        #region Let Go

        public void LetGo() {
            LetGo(this);
        }

        public static void LetGo(PawnHand handTarget) {
            if (handTarget.grabbedObject == null)
                return;

            DebugLog("LetGo");

            if (!handTarget._pawn.isRemote && handTarget._pawn.networking != null)
                handTarget._pawn.networking.LetGo(handTarget);

            if (handTarget.grabSocket.attachedHandle != null)
                handTarget.grabSocket.Release();
            else
                LetGoWithoutHandle(handTarget);

            if (handTarget.grabbedRigidbody)
                LetGoRigidbody(handTarget);

            if (handTarget.physics) {
                ControllerPhysics controllerPhysics = handTarget.GetComponent<ControllerPhysics>();
                if (controllerPhysics != null) {
                    controllerPhysics.UnsetCollidersToTrigger();
                    controllerPhysics.SetHybridKinematic();
                }
            }

            if (Application.isPlaying) {
                handTarget.SendMessage("OnLettingGo", null, SendMessageOptions.DontRequireReceiver);
                handTarget.grabbedObject.SendMessage("OnLetGo", handTarget, SendMessageOptions.DontRequireReceiver);
            }

            handTarget.grabbedObject = null;
            handTarget.grabbedHandle = null;
            handTarget.twoHandedGrab = false;
            if (handTarget.otherController != null)
                handTarget.otherController.twoHandedGrab = false;
        }

        protected static void LetGoWithoutHandle(PawnHand controllerTarget) {
            Rigidbody controllerRigidbody = controllerTarget.GetComponent<Rigidbody>();
            Rigidbody objRigidbody = RigidbodyDisabled.UnparentRigidbody(controllerRigidbody, controllerTarget.grabbedObject.transform);

            if (controllerRigidbody != null) {
                objRigidbody.velocity = controllerRigidbody.velocity;
                objRigidbody.angularVelocity = controllerRigidbody.angularVelocity;
            }
        }

        private static void LetGoRigidbody(PawnHand controllerTarget) {
            DebugLog("LetGoRigidbody");

            controllerTarget.grabbedRigidbody = false;
        }

        #endregion Let Go

    }
}