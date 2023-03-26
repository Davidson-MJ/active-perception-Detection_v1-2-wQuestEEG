using System.Collections.Generic;
using UnityEngine;
#if hNW_UNET
using UnityEngine.Networking;
#endif

namespace Passer.Humanoid {

    public partial class HandTarget {

        public bool grabWithHandpose = true;

        protected static void DebugLog(string s) {
            //Debug.Log(s);
        }

        public enum GrabType {
            HandGrab,
            Pinch
        }

        #region Init

        public virtual void StartInteraction() {
            // Remote humanoids should not interact
            if (humanoid.isRemote)
                return;

            // Gun Interaction pointer creates an Event System
            // First solve that before enabling this warning
            // because the warning will appear in standard Grocery Store demo scene

            inputModule = humanoid.GetComponent<InteractionModule>();
            if (inputModule == null) {
                inputModule = Object.FindObjectOfType<InteractionModule>();
                if (inputModule == null) {
                    inputModule = humanoid.gameObject.AddComponent<InteractionModule>();
                }
            }

            // This condition is neede because the InteractionModule does not have EnableTouchInput compiled in
            // when pHUMANOID has not been defined yet
#if pHUMANOID
            inputModule.EnableTouchInput(humanoid, isLeft, 0);
#endif
        }

        public virtual HandSocket CreateGrabSocket() {
            if (hand.bone.transform == null)
                return null;

            HandSocket socket = hand.bone.transform.GetComponentInChildren<HandSocket>();
            if (socket != null)
                return socket;

            GameObject socketObj = new GameObject(isLeft ? "Left Grab Socket" : "Right Grab Socket");
            Transform socketTransform = socketObj.transform;
            socketTransform.parent = hand.bone.transform;
            MoveToPalm(socketTransform);

            socket = socketObj.AddComponent<HandSocket>();
            socket.handTarget = this;
            return socket;
        }

        public virtual Socket CreatePinchSocket() {
            if (hand.bone.transform == null)
                return null;

            Socket[] sockets = hand.bone.transform.GetComponentsInChildren<Socket>();
            foreach (Socket socket in sockets) {
                if (socket.gameObject.name.Contains("Pinch Socket"))
                    return socket;
            }

            GameObject socketObj = new GameObject(isLeft ? "Left Pinch Socket" : "Right Pinch Socket");
            Transform socketTransform = socketObj.transform;
            socketTransform.parent = hand.bone.transform;
            socketTransform.rotation = hand.bone.targetRotation * Quaternion.Euler(355, 190, 155);
            socketTransform.position = hand.target.transform.TransformPoint(isLeft ? -0.1F : 0.1F, -0.035F, 0.03F);

            Socket pinchSocket = socketObj.AddComponent<Socket>();
            return pinchSocket;
        }

        protected void MoveToPalm(Transform t) {
            if (hand.bone.transform == null)
                return;

            Transform indexFingerBone = fingers.index.proximal.bone.transform;
            Transform middleFingerBone = fingers.middle.proximal.bone.transform;

            // Determine position
            Vector3 palmOffset;
            if (indexFingerBone)
                palmOffset = (indexFingerBone.position - hand.bone.transform.position) * 0.9F;
            else if (middleFingerBone)
                palmOffset = (middleFingerBone.position - hand.bone.transform.position) * 0.9F;
            else
                palmOffset = new Vector3(0.1F, 0, 0);

            t.position = hand.bone.transform.position + palmOffset;

            Vector3 handUp = hand.bone.targetRotation * Vector3.up;

            // Determine rotation
            if (indexFingerBone)
                t.LookAt(indexFingerBone, handUp);
            else if (middleFingerBone)
                t.LookAt(middleFingerBone, handUp);
            else if (isLeft)
                t.LookAt(t.position - humanoid.avatarRig.transform.right, handUp);
            else
                t.LookAt(t.position + humanoid.avatarRig.transform.right, handUp);

            // Now get it in the palm
            if (isLeft) {
                t.rotation *= Quaternion.Euler(0, -45, -90);
                t.position += t.rotation * new Vector3(0.02F, -0.02F, 0);
            }
            else {
                t.rotation *= Quaternion.Euler(0, 45, 90);
                t.position += t.rotation * new Vector3(-0.02F, -0.02F, 0);
            }
        }

        private void DeterminePalmPosition() {
            if (hand.bone.transform == null)
                return;

            if (handPalm == null) {
                handPalm = hand.bone.transform.Find("Hand Palm");
                if (handPalm == null)
                    handPalm = hand.bone.transform.Find(isLeft ? "Left Hand Palm" : "Right Hand Palm");
                if (handPalm == null) {
                    GameObject handPalmObj = new GameObject(isLeft ? "Left Hand Palm" : "Right Hand Palm");
                    handPalm = handPalmObj.transform;
                    handPalm.parent = hand.bone.transform;
                }
            }

            Transform indexFingerBone = fingers.index.proximal.bone.transform; // handTarget.fingers.indexFinger.bones[(int)FingerBones.Proximal];
            Transform middleFingerBone = fingers.middle.proximal.bone.transform; //.middleFinger.bones[(int)FingerBones.Proximal];

            // Determine position
            Vector3 palmOffset;
            if (indexFingerBone)
                palmOffset = (indexFingerBone.position - hand.bone.transform.position) * 0.9F;
            else if (middleFingerBone)
                palmOffset = (middleFingerBone.position - hand.bone.transform.position) * 0.9F;
            else
                palmOffset = new Vector3(0.1F, 0, 0);

            handPalm.position = hand.bone.transform.position + palmOffset;

            Vector3 handUp = hand.bone.targetRotation * Vector3.up;

            // Determine rotation
            if (indexFingerBone)
                handPalm.LookAt(indexFingerBone, handUp);
            else if (middleFingerBone)
                handPalm.LookAt(middleFingerBone, handUp);
            else if (isLeft)
                handPalm.LookAt(handPalm.position - humanoid.avatarRig.transform.right, handUp);
            else
                handPalm.LookAt(handPalm.position + humanoid.avatarRig.transform.right, handUp);

            // Now get it in the palm
            if (isLeft) {
                handPalm.rotation *= Quaternion.Euler(0, -45, -90);
                handPalm.position += handPalm.rotation * new Vector3(0.02F, -0.02F, 0);
            }
            else {
                handPalm.rotation *= Quaternion.Euler(0, 45, 90);
                handPalm.position += handPalm.rotation * new Vector3(-0.02F, -0.02F, 0);
            }
        }

        #endregion Init

        #region Nearing

        public void OnNearing(GameObject obj) {
        }

        #endregion Nearing

        #region Touching

        public virtual void OnTouchStart(GameObject obj, Vector3 contactPoint) {
            GrabCheck(obj);
            if (inputModule != null)
                inputModule.OnFingerTouchStart(isLeft, obj);

            if (obj != null) {
                IHandTouchEvents touchEvents = obj.GetComponent<IHandTouchEvents>();
                if (touchEvents != null)
                    touchEvents.OnHandTouchStart(this);

                if (handRigidbody != null) {
                    IHandCollisionEvents collisionEvents = handRigidbody.GetComponentInChildren<IHandCollisionEvents>();
                    if (collisionEvents != null)
                        collisionEvents.OnHandCollisionStart(obj, contactPoint);
                }
            }
        }

        public virtual void OnTouchEnd(GameObject obj) {
            if (inputModule != null && obj == touchedObject)
                inputModule.OnFingerTouchEnd(isLeft);

            if (obj != null) {
                IHandTouchEvents touchEvents = obj.GetComponent<IHandTouchEvents>();
                if (touchEvents != null)
                    touchEvents.OnHandTouchEnd(this);

                if (handRigidbody != null) {
                    IHandCollisionEvents collisionEvents = handRigidbody.GetComponentInChildren<IHandCollisionEvents>();
                    if (collisionEvents != null)
                        collisionEvents.OnHandCollisionEnd(obj);
                }
            }
        }

        #endregion Touching

        #region Grabbing

        /// <summary>
        /// The maximum mass of object you can grab
        /// </summary>
        public static float maxGrabbingMass = 10;


        /// <summary>
        /// Try to grab the object we touch
        /// </summary>
        public void GrabTouchedObject() {
            if (touchedObject != null)
                Grab(touchedObject);
        }

        protected bool grabChecking = false;

        protected GameObject lastNearObject = null;

        /// <summary>
        /// Check the hand for grabbing near objects
        /// </summary>
        /// This function will grab a near object if the hand is grabbing
        public void NearCheck() {
            GameObject nearObject = DetermineGrabObject();
            if (nearObject != lastNearObject) {
                int interactionID = isLeft ? (int)InputDeviceIDs.LeftHand : (int)InputDeviceIDs.RightHand;
                UnityEngine.EventSystems.PointerEventData data = null;
                if (touchInteraction)
                    data = inputModule.pointers[interactionID].data;

                if (lastNearObject != null) {
                    IHandNearEvents touchEvents = lastNearObject.GetComponent<IHandNearEvents>();
                    if (touchEvents != null)
                        touchEvents.OnHandNearEnd(this);

                    if (touchInteraction) {
                        UnityEngine.EventSystems.ExecuteEvents.ExecuteHierarchy(lastNearObject, data, UnityEngine.EventSystems.ExecuteEvents.pointerExitHandler);
                        UnityEngine.EventSystems.ExecuteEvents.ExecuteHierarchy(lastNearObject, data, UnityEngine.EventSystems.ExecuteEvents.deselectHandler);
                    }
                }
                if (nearObject != null) {
                    IHandNearEvents touchEvents = nearObject.GetComponent<IHandNearEvents>();
                    if (touchEvents != null)
                        touchEvents.OnHandNearStart(this);

                    if (touchInteraction) {
                        UnityEngine.EventSystems.ExecuteEvents.ExecuteHierarchy(nearObject, data, UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler);
                        UnityEngine.EventSystems.ExecuteEvents.ExecuteHierarchy(nearObject, data, UnityEngine.EventSystems.ExecuteEvents.selectHandler);
                    }
                }
                lastNearObject = nearObject;
            }

            // handRigidbody needs to be there to do proper grabbing
            if (grabbingTechnique == GrabbingTechnique.NearGrabbing && handRigidbody != null && !otherHand.grabbedChanged) {
                if (grabWithHandpose == false)
                    return;

                if (grabbingTechnique != GrabbingTechnique.NearGrabbing
                    || grabChecking
                    || grabbedObject != null
                    || humanoid.isRemote
                    ) {

                    return;
                }

                grabChecking = true;

                float handCurl = HandCurl();
                if (handCurl > 3) {
                    if (nearObject != null)
                        Grab(nearObject);
                }
                grabChecking = false;
            }
        }

        /// <summary>
        /// Try to grab an object near to the hand
        /// </summary>
        public void GrabNearObject() {
            GameObject grabObject = DetermineGrabObject();
            if (grabObject != null)
                Grab(grabObject);
        }

        /// <summary>
        /// Try to take the object
        /// </summary>
        /// <param name="obj">The object to take</param>
        /// Depending on the hand pose the object may be grabbed, pinched or not
        public void GrabCheck(GameObject obj) {
            if (grabWithHandpose == false)
                return;

            if (grabbingTechnique != GrabbingTechnique.TouchGrabbing
                    || grabChecking
                    || grabbedObject != null
                    || humanoid.isRemote
                    ) {
                return;
            }

            grabChecking = true;

            if (CanBeGrabbed(obj)) {
                float handCurl = HandCurl();
                if (handCurl > 2) {
                    GameObject grabObject = DetermineGrabObject();
                    if (grabObject != null)
                        this.Grab(grabObject);
                    else
                        this.Grab(obj);
                }
                else {
                    bool pinching = PinchInteraction.IsPinching(this);
                    if (pinching) {
                        HandInteraction.NetworkedPinch(this, obj);
                    }
                    else {
                        LerpToGrab(obj);
                    }
                }
            }
            grabChecking = false;
        }

        protected void LerpToGrab(GameObject obj) {
#if pHUMANOID
            Handle handle = Handle.GetClosestHandle(obj.transform, transform.position, isLeft ? Handle.Hand.Left : Handle.Hand.Right, true);
#else
            Handle handle = Handle.GetClosestHandle(obj.transform, transform.position);
#endif
            if (handle == null)
                return;

            float handCurl = HandCurl();
            float f = handCurl / 2;

            Vector3 socket2HandPosition = hand.bone.transform.position - grabSocket.transform.position;
            Vector3 handOnSocketPosition = handle.worldPosition + socket2HandPosition;

            hand.bone.transform.position = Vector3.Lerp(hand.target.transform.position, handOnSocketPosition, f);
        }

        // Rigidbody > Static Object
        // Handles > No Handle
        // Handles not in socket > Handle in socket
        protected virtual GameObject DetermineGrabObject() {
            Collider[] colliders = Physics.OverlapSphere(grabSocket.transform.position, nearDistance);

            GameObject objectToGrab = null;
            bool grabRigidbody = false;
            bool grabHandle = false;
            bool grabHandleInSocket = false;
            foreach (Collider collider in colliders) {
                GameObject obj;
                Rigidbody objRigidbody = collider.attachedRigidbody;
                if (objRigidbody != null)
                    obj = objRigidbody.gameObject;
                else
                    obj = collider.gameObject;

                if (!CanBeGrabbed(obj))
                    continue;
                if (handRigidbody != null && obj == handRigidbody.gameObject)
                    continue;
                //if (obj == otherHand.handRigidbody.gameObject)
                //    continue;

                //Debug.Log("grab check  " +isLeft + " " + obj);

                if (objRigidbody != null) {
                    Handle handle = obj.GetComponentInChildren<Handle>();
                    if (handle != null) {
                        if (!grabRigidbody || !grabHandle || grabHandleInSocket) {
                            objectToGrab = obj;
                            grabHandle = true;
                            grabHandleInSocket = handle.socket != null;
                        }
                    }
                    else {
                        if (!grabRigidbody) {
                            objectToGrab = obj;
                            grabRigidbody = true;
                            grabHandle = false;
                            grabHandleInSocket = false;
                        }
                    }
                }
                else if (!grabRigidbody) {
                    Handle handle = obj.GetComponentInChildren<Handle>();
                    if (handle != null) {
                        if (!grabHandle || grabHandleInSocket) {
                            objectToGrab = obj;
                            grabHandle = true;
                            grabHandleInSocket = handle.socket != null;
                        }
                    }
                    else if (!grabHandle) {
                        objectToGrab = obj;
                    }
                }
                if (objectToGrab.name.Contains("ArmPalm"))
                    Debug.Log("corrected to arm!!!");
            }
            return objectToGrab;
        }

        public virtual bool CanBeGrabbed(GameObject obj) {
            if (obj == null || obj == humanoid.gameObject ||
                (humanoid.characterRigidbody != null && obj == humanoid.characterRigidbody.gameObject) ||
                obj == humanoid.headTarget.gameObject
                )
                return false;

            // We cannot grab 2D objects like UI
            RectTransform rectTransform = obj.GetComponent<RectTransform>();
            if (rectTransform != null)
                return false;

            return true;
        }

        /// <summary>Grab and object with the hand (non-networked)</summary>
        /// <param name="handTarget">The hand to grab with</param>
        /// <param name="obj">The gameObject to grab</param>
        /// <param name="rangeCheck">check wither the hand is in range of the handle</param>
        public virtual void Grab(GameObject obj, bool rangeCheck = true) {
            // Extra protection for remote grabbing which bypasses GrabCheck
            if (grabbedObject != null)
                return;

            DebugLog(this + " grabs " + obj);

            bool grabbed = false;

            NoGrab noGrab = obj.GetComponent<NoGrab>();
            if (noGrab != null)
                return;

            Rigidbody objRigidbody = obj.GetComponent<Rigidbody>();

            BasicHandPhysics otherHandPhysics = null;
            if (objRigidbody != null)
                otherHandPhysics = objRigidbody.GetComponent<BasicHandPhysics>();

            if (otherHandPhysics != null || GrabbedWithOtherHand(obj))
                grabbed = SecondHandGrab(obj, rangeCheck);

            else {
#if pHUMANOID
                Handle handle = Handle.GetClosestHandle(obj.transform, grabSocket.transform.position, isLeft ? Handle.Hand.Left : Handle.Hand.Right, rangeCheck);
#else
                Handle handle = Handle.GetClosestHandle(obj.transform, transform.position);
#endif
                if (handle != null) {
                    grabbed = GrabHandle(objRigidbody, handle, rangeCheck);
                }
                else if (objRigidbody != null) {
                    HumanoidControl humanoidControl = objRigidbody.GetComponent<HumanoidControl>();
                    if (humanoidControl != null) {
                        // Can't grab another humanoid right now
                        grabbed = false;
                    }
                    else
                        grabbed = GrabRigidbodyWithoutHandle(objRigidbody);
                }
            }

            if (grabbed) {
                if (!humanoid.isRemote && humanoid.humanoidNetworking != null) {
                    if (otherHandPhysics != null) {
                        GameObject otherHandGrabbedObject = otherHandPhysics.handTarget.grabbedObject;
                        humanoid.humanoidNetworking.Grab(this, otherHandGrabbedObject, false, HandTarget.GrabType.HandGrab);
                    }
                    else
                        humanoid.humanoidNetworking.Grab(this, obj, rangeCheck, HandTarget.GrabType.HandGrab);
                }

                TrackedRigidbody trackedRigidbody = obj.GetComponent<TrackedRigidbody>();
                if (trackedRigidbody != null && trackedRigidbody.target != null) {
                    //Debug.Log("grabbed trackedRigidbody");
                    AddSensorComponent(trackedRigidbody.target.GetComponent<SensorComponent>());
                    AddTrackedRigidbody(trackedRigidbody);
                }

                if (humanoid.physics && physics && grabbedRigidbody)
                    AdvancedHandPhysics.SetNonKinematic(handRigidbody, colliders);

                // This does not work in the editor, so controller input cannot be set this way
                // When grabbing handles in the editor
                if (Application.isPlaying) {
                    SendMessage("OnGrabbing", grabbedObject, SendMessageOptions.DontRequireReceiver);
                    grabbedObject.SendMessage("OnGrabbed", this, SendMessageOptions.DontRequireReceiver);

                    IHandGrabEvents objectInteraction = grabbedObject.GetComponent<IHandGrabEvents>();
                    if (objectInteraction != null)
                        objectInteraction.OnHandGrabbed(this);
                }
            }
        }

        protected bool GrabbedWithOtherHand(GameObject obj) {
            if (otherHand == null || otherHand.hand.bone.transform == null)
                return false;

            Rigidbody objRigidbody = obj.GetComponentInParent<Rigidbody>();
            if (objRigidbody != null && objRigidbody.transform == otherHand.hand.bone.transform)
                /* two handed grip for rigidbodies only */
                return true;

            if (objRigidbody != null && objRigidbody.isKinematic) {
                Transform parent = objRigidbody.transform.parent;
                if (parent == null)
                    return false;

                Rigidbody parentRigidbody = parent.GetComponentInParent<Rigidbody>();
                if (parentRigidbody == null)
                    return false;

                return GrabbedWithOtherHand(parentRigidbody.gameObject);
            }

            return false;
        }

        protected bool SecondHandGrab(GameObject obj, bool rangeCheck) {
            DebugLog("SecondHandGrab " + obj);

#if pHUMANOID
            Handle handle = Handle.GetClosestHandle(obj.transform, transform.position, isLeft ? Handle.Hand.Left : Handle.Hand.Right, rangeCheck ? 0.2F : float.PositiveInfinity);
#else
            Handle handle = Handle.GetClosestHandle(obj.transform, transform.position);
#endif
            //if (handle == null)
            //    return false;

            Rigidbody objRigidbody = handle != null ? handle.GetComponentInParent<Rigidbody>() : obj.GetComponentInParent<Rigidbody>();
            if (objRigidbody == null)
                return false;

            if (handle != null)
                GrabHandle(objRigidbody, handle, false);
            else {
                GrabRigidbody(objRigidbody);
                targetToHandle = hand.target.transform.InverseTransformPoint(palmPosition);
            }

            if (objRigidbody != null && objRigidbody == otherHand.handRigidbody) {
                // We grabbed our own other hand
                otherHand.twoHandedGrab = true;
                if (handle != null)
                    otherHand.targetToSecondaryHandle = otherHand.hand.target.transform.InverseTransformPoint(handle.transform.position);
                else
                    otherHand.targetToSecondaryHandle = otherHand.hand.target.transform.InverseTransformPoint(palmPosition);
            }
            return true;
        }

        public void GrabHandle(Handle handle, bool rangeCheck = false) {
            // Extra protection for remote grabbing which bypasses GrabCheck
            if (grabbedObject != null)
                return;

            Debug.Log(this + " grabs handle " + handle);
            bool grabbed = false;

            if (handle == null)
                return;

            // A bit silly, a handle with NoGrab, but I leave it in to be sure
            NoGrab noGrab = handle.GetComponent<NoGrab>();
            if (noGrab != null)
                return;

            //GrabHandle does not yet support two-handed grabbing!!!!!
            //if (HandInteraction.AlreadyGrabbedWithOtherHand(this, obj))
            //    grabbed = HandInteraction.Grab2(this, obj);

            //else {

            Rigidbody objRigidbody = handle.GetComponentInParent<Rigidbody>();
            grabbed = GrabHandle(objRigidbody, handle, rangeCheck);

            if (grabbed) {
                if (!humanoid.isRemote && humanoid.humanoidNetworking != null)
                    humanoid.humanoidNetworking.Grab(this, objRigidbody.gameObject, rangeCheck, HandTarget.GrabType.HandGrab);

                TrackedRigidbody trackedRigidbody = objRigidbody.gameObject.GetComponent<TrackedRigidbody>();
                if (trackedRigidbody != null && trackedRigidbody.target != null) {
                    Debug.Log("grabbed trackedRigidbody");
                    AddSensorComponent(trackedRigidbody.target.GetComponent<SensorComponent>());
                    AddTrackedRigidbody(trackedRigidbody);
                }

                if (humanoid.physics && grabbedRigidbody) {
                    AdvancedHandPhysics.SetNonKinematic(handRigidbody, colliders);
                }

                if (Application.isPlaying) {
                    SendMessage("OnGrabbing", grabbedObject, SendMessageOptions.DontRequireReceiver);
                    grabbedObject.SendMessage("OnGrabbed", this, SendMessageOptions.DontRequireReceiver);
                    if (grabbedHandle != null)
                        grabbedHandle.SendMessage("OnGrabbed", this, SendMessageOptions.DontRequireReceiver);
                }
            }

        }

        protected virtual bool GrabHandle(Rigidbody objRigidbody, Handle handle, bool rangeCheck) {
            DebugLog(gameObject + " Grabs Handle " + handle);

            if (handle.grabType == Handle.GrabType.NoGrab)
                return false;

            GameObject obj = (objRigidbody != null) ? objRigidbody.gameObject : handle.gameObject;

            if (objRigidbody != null && objRigidbody.isKinematic) {
                //Debug.Log("Grab Kinematic Rigidbody Handle");
                // When grabbing a kinematic rigidbody, the hand should change to a non-kinematic rigidbody first
                AdvancedHandPhysics.SetNonKinematic(handRigidbody, colliders);
            }

            //if (handle.socket != null) {
            //    Debug.Log("Grab from socket");
            //    handle.socket.Release();
            //}

            targetToHandle = hand.target.transform.InverseTransformPoint(grabSocket.transform.position);

            if (grabSocket.Attach(handle, rangeCheck)) {
                grabbedHandle = handle;

                grabbedObject = obj;
                grabbedRigidbody = (objRigidbody != null);
                if (grabbedRigidbody)
                    grabbedKinematicRigidbody = objRigidbody.isKinematic;

                return true;
            }
            else
                return false;
        }

        protected virtual bool GrabRigidbody(Rigidbody objRigidbody, bool rangeCheck = true) {
            DebugLog("GrabRigidbody " + objRigidbody);

            if (objRigidbody.mass > maxGrabbingMass)
                return false;

            if (objRigidbody.isKinematic) {
                // When grabbing a kinematic rigidbody, the hand should change to a non-kinematic rigidbody first
                AdvancedHandPhysics.SetNonKinematic(handRigidbody, colliders);
            }

            Handle[] handles = objRigidbody.GetComponentsInChildren<Handle>();
            for (int i = 0; i < handles.Length; i++) {

#if pHUMANOID
                if ((isLeft && handles[i].hand == Handle.Hand.Right) ||
                    (!isLeft && handles[i].hand == Handle.Hand.Left))
                    continue;

                // Don't grab it from the other hand
                if (handles[i].socket == otherHand.grabSocket)
                    continue;
#endif

                return GrabRigidbodyHandle(objRigidbody, handles[i], rangeCheck);
            }

            GrabRigidbodyWithoutHandle(objRigidbody);

            grabbedObject = objRigidbody.gameObject;
            grabbedRigidbody = true;
            grabbedKinematicRigidbody = objRigidbody.isKinematic;
            return true;
        }

        protected virtual bool GrabRigidbodyHandle(Rigidbody objRigidbody, Handle handle, bool rangeCheck) {
            DebugLog("GrabRigidbodyHandle " + objRigidbody);

            Transform objTransform = objRigidbody.transform;

            if (handle.socket != null) {
                //Debug.Log("Grab from socket");
                handle.socket.Release();
            }
            grabSocket.Attach(handle.transform, rangeCheck);

            grabbedObject = handle.gameObject;
            grabbedRigidbody = true;
            grabbedKinematicRigidbody = objRigidbody.isKinematic;
#if pHUMANOID
            handle.handTarget = this;
            if (handle.pose != null) {
                poseMixer.SetPoseValue(handle.pose, 1);
            }
#endif

            grabbedRigidbody = true;
            grabbedKinematicRigidbody = objRigidbody.isKinematic;
            return true;
        }

        protected virtual bool GrabRigidbodyWithoutHandle(Rigidbody objRigidbody) {
            if (objRigidbody.mass > maxGrabbingMass) {
                DebugLog("Object is too heavy, mass > " + maxGrabbingMass);
                return false;
            }

            if (objRigidbody.isKinematic)
                //GrabStaticWithoutHandle(objRigidbody.gameObject);
                GrabRigidbodyHandParenting(objRigidbody);
            else
                GrabRigidbodyParenting(objRigidbody);

            grabbedRigidbody = true;
            grabbedKinematicRigidbody = objRigidbody.isKinematic;
            return true;
        }

        protected virtual void GrabRigidbodyHandParenting(Rigidbody objRigidbody) {
            DebugLog("GrabRigidbodyHandParenting");
            GameObject obj = objRigidbody.gameObject;
            RigidbodyDisabled.ParentRigidbody(objRigidbody, handRigidbody);
            handRigidbody = null;
            grabbedObject = obj;
        }

        protected virtual bool GrabRigidbodyParenting(Rigidbody objRigidbody) {
            DebugLog("GrabRigidbodyParenting");
            GameObject obj = objRigidbody.gameObject;
            if (handRigidbody == null) {
                Debug.LogError("Hand no longer has a rigidbody...");
            }

            RigidbodyDisabled.ParentRigidbody(handRigidbody, objRigidbody);

            HumanoidNetworking.DisableNetworkSync(objRigidbody.gameObject);
            if (!humanoid.isRemote)
                HumanoidNetworking.TakeOwnership(objRigidbody.gameObject);

            if (Application.isPlaying)
                Object.Destroy(objRigidbody);
            else
                Object.DestroyImmediate(objRigidbody, true);
            grabbedObject = obj;

            return true;
        }

        protected virtual bool GrabStaticWithoutHandle(GameObject obj) {
            DebugLog("Grab Static Without Handle");

            if (handRigidbody == null)
                return false;

            grabSocket.AttachStaticJoint(obj.transform);

            grabbedObject = obj;
            grabbedRigidbody = false;

            return true;
        }

        #endregion Grabbbing

        #region Letting go

        [System.NonSerialized]
        protected bool letGoChecking = false;
        [System.NonSerialized]
        protected float letGoCheckStart;

        protected virtual void CheckLetGo() {
            if (grabWithHandpose == false)
                return;

            // timeout for letgochecking
            if (letGoChecking && (Time.time - letGoCheckStart) > 1) {
                Debug.Log("LetGo check timeout reached");
                letGoChecking = false;
            }

            if (letGoChecking || grabbedObject == null)
                return;

            letGoChecking = true;
            letGoCheckStart = Time.time;
            bool pulledLoose = PulledLoose();

            if (pinchSocket.attachedTransform != null) {
                bool notPinching = PinchInteraction.IsNotPinching(this);
                if (notPinching || pulledLoose)
                    LetGoPinch();
            }
            else if (grabSocket.attachedTransform != null || grabbedObject != null) {
                float handCurl = HandCurl();
                bool fingersGrabbing = (handCurl >= 1.5F);
                if (!humanoid.isRemote && (!fingersGrabbing || pulledLoose)) {
                    LetGo();
                    if (pulledLoose)
                        colliders = AdvancedHandPhysics.SetKinematic(handRigidbody);
                }
            }
            letGoChecking = false;
        }

        protected virtual bool PulledLoose() {
            // Remote humanoids will only let go objects when the local humanoid has done so.
            if (humanoid.isRemote)
                return false;

            float forearmStretch = Vector3.Distance(hand.bone.transform.position, forearm.bone.transform.position) - forearm.bone.length;
            if (forearmStretch > 0.15F) {
                return true;
            }

            return false;
        }

        ///<summary>Let go the object the hand is holding (if any)</summary>
        public virtual void LetGo() {
            DebugLog("LetGo " + grabbedObject);

            if (hand.bone.transform == null || grabbedObject == null)
                return;

            if (!humanoid.isRemote && humanoid.humanoidNetworking != null)
                humanoid.humanoidNetworking.LetGo(this);

            if (grabSocket.attachedHandle != null)
                grabSocket.Release();
            else if (grabbedRigidbody && !grabbedKinematicRigidbody)
                LetGoRigidbodyWithoutHandle();
            else if (grabbedRigidbody)
                LetGoRigidbodyWithoutHandle();
            else
                LetGoStaticWithoutHandle();

            if (grabbedRigidbody) {
                Rigidbody grabbedRigidbody = grabbedObject.GetComponent<Rigidbody>();
                LetGoRigidbody(grabbedRigidbody);
            }

            if (humanoid.dontDestroyOnLoad) {
                // Prevent this object inherites the dontDestroyOnLoad from the humanoid
                if (grabbedObject.transform.parent == null)
                    Object.DontDestroyOnLoad(grabbedObject);
            }

            if (humanoid.physics && physics)
                AdvancedHandPhysics.SetNonKinematic(handRigidbody, colliders);

            if (grabbedRigidbody)
                TmpDisableCollisions(this, 0.2F);

#if hNW_UNET
#pragma warning disable 0618
            NetworkTransform nwTransform = handTarget.grabbedObject.GetComponent<NetworkTransform>();
            if (nwTransform != null)
                nwTransform.sendInterval = 1;
#pragma warning restore 0618
#endif

            if (Application.isPlaying) {
                SendMessage("OnLettingGo", null, SendMessageOptions.DontRequireReceiver);
                grabbedObject.SendMessage("OnLetGo", this, SendMessageOptions.DontRequireReceiver);
                IHandGrabEvents objectInteraction = grabbedObject.GetComponent<IHandGrabEvents>();
                if (objectInteraction != null)
                    objectInteraction.OnHandLetGo(this);
            }

            grabbedObject = null;
            grabbedHandle = null;
            grabbedKinematicRigidbody = false;
            twoHandedGrab = false;
            otherHand.twoHandedGrab = false;

            touchedObject = null;

        }

        protected void LetGoRigidbodyWithoutHandle() {
            if (handRigidbody == null)
                LetGoHandParenting();
            
            else
                LetGoRigidbodyParenting();            
        }

        protected void LetGoHandParenting() { // Without handle
            Rigidbody objRigidbody = grabbedObject.GetComponentInParent<Rigidbody>();
            Rigidbody rigidbody = RigidbodyDisabled.UnparentRigidbody(objRigidbody, hand.bone.transform);
            handRigidbody = rigidbody;
        }

        protected void LetGoRigidbodyParenting() { // Without handle
            DebugLog("Let go Rigidbody from Parenting " + grabbedObject);

            bool wasTwoHandedGrab = false;

            BasicHandPhysics[] grabbedPhysicsHands = null;
            if (handRigidbody != null) {
                grabbedPhysicsHands = GetComponentsInChildrenOnly(handRigidbody.transform);
                if (grabbedPhysicsHands.Length > 0) {
                    // Multiple hand grabbing: releasing the primary hand
                    // We should first release all secondary hands
                    // and have the secondary hands regrab after primary hand has released the grab
                    // There can be more than one secondary hand,
                    // which one will becom the new primary is chosen is not determined
                    DebugLog("+++++ letting go two-handed primary grab hand");
                    DebugLog("Release other hands first " + grabbedPhysicsHands.Length);
                    foreach (BasicHandPhysics grabbedPhysicsHand in grabbedPhysicsHands) {
                        HandTarget otherHand = grabbedPhysicsHand.handTarget;
                        otherHand.LetGo();
                    }
                    wasTwoHandedGrab = true;
                }
            }
            else
                Debug.LogWarning(this + ": No Hand Rigidbody when letting go");

            DebugLog("Let go from primary hand");
            Rigidbody thisRigidbody = this.GetComponentInParent<Rigidbody>();
            Rigidbody objRigidbody;
            if (thisRigidbody == null)
                objRigidbody = RigidbodyDisabled.UnparentRigidbody(this.transform, grabbedObject.transform);
            else
                objRigidbody = RigidbodyDisabled.UnparentRigidbody(thisRigidbody, grabbedObject.transform);

            //MassRestoration(thisRigidbody, objRigidbody);

#if pHUMANOID
            HumanoidNetworking.ReenableNetworkSync(objRigidbody.gameObject);
#endif

            SetLetGoRigibodyVelocity(objRigidbody);

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

        protected void LetGoStaticWithoutHandle() {
            grabSocket.ReleaseStaticJoint();
        }

        protected void LetGoRigidbody(Rigidbody grabbedRigidbody) {
            DebugLog("LetGoRigidbody");

            if (grabbedRigidbody != null) {
                //if (handTarget.handRigidbody != null)
                //    GrabMassRestoration(handTarget.handRigidbody, grabbedRigidbody);

                Joint[] joints = grabbedObject.GetComponents<Joint>();
                for (int i = 0; i < joints.Length; i++) {
                    if (joints[i].connectedBody == handRigidbody)
                        Object.Destroy(joints[i]);
                }
                //grabbedRigidbody.centerOfMass = handTarget.storedCOM;

                SetLetGoRigibodyVelocity(grabbedRigidbody);

                if (grabbedHandle != null)
                    LetGoHandle(grabbedHandle);
            }
            this.grabbedRigidbody = false;
        }

        protected void SetLetGoRigibodyVelocity(Rigidbody objRigidbody) {
            if (handRigidbody == null)
                return;

            if (handRigidbody.isKinematic) {
                objRigidbody.velocity = hand.velocity;
                objRigidbody.angularVelocity = hand.angularVelocity;
            }
            else {
                objRigidbody.velocity = handRigidbody.velocity;
                objRigidbody.angularVelocity = handRigidbody.angularVelocity;
            }
        }

        protected void LetGoHandle(Handle handle) {
            DebugLog("LetGoHandle " + handle);
#if pHUMANOID
            if (Application.isPlaying) {
                if (grabbedHandle != null) {
                    grabbedHandle.SendMessage("OnLetGo", this, SendMessageOptions.DontRequireReceiver);
                    IHandGrabEvents objectInteraction = grabbedHandle.GetComponent<IHandGrabEvents>();
                    if (objectInteraction != null)
                        objectInteraction.OnHandLetGo(this);
                }
            }

            handle.handTarget = null;
            grabbedHandle = null;

            if (handle.pose != null)
                poseMixer.Remove(handle.pose);
#endif
        }

        #region Pinch

        protected void LetGoPinch() {

            // No Networking yet!

            pinchSocket.Release();

            if (humanoid.physics)
                UnsetColliderToTrigger(colliders);

            if (Application.isPlaying) {
                SendMessage("OnLettingGo", null, SendMessageOptions.DontRequireReceiver);
                grabbedObject.SendMessage("OnLetGo", this, SendMessageOptions.DontRequireReceiver);
                IHandGrabEvents objectInteraction = grabbedObject.GetComponent<IHandGrabEvents>();
                if (objectInteraction != null)
                    objectInteraction.OnHandLetGo(this);
            }

            grabbedObject = null;
        }

        #endregion Pinch

        #endregion Letting go

        public void GrabOrLetGo(GameObject obj, bool rangeCheck = true) {
            if (grabbedObject != null)
                LetGo();
            else {
                Grab(obj, rangeCheck);
            }
        }
    }

    [System.Serializable]
    public class HandInteraction {

        #region Grabbing/Pinching

        #region Grab

        public static void MoveAndGrabHandle(HandTarget handTarget, Handle handle) {
            if (handTarget == null || handle == null)
                return;

            MoveHandTargetToHandle(handTarget, handle);
            GrabHandle(handTarget, handle);
        }

        public static void MoveHandTargetToHandle(HandTarget handTarget, Handle handle) {
            // Should use GetGrabPosition
            Quaternion handleWorldRotation = handle.transform.rotation; // * Quaternion.Euler(handle.rotation);
            Quaternion palm2handRot = Quaternion.Inverse(Quaternion.Inverse(handTarget.hand.bone.targetRotation) * handTarget.palmRotation);
            handTarget.hand.target.transform.rotation = handleWorldRotation * palm2handRot;

            Vector3 handleWorldPosition = handle.transform.position; // TransformPoint(handle.position);
            handTarget.hand.target.transform.position = handleWorldPosition - handTarget.hand.target.transform.rotation * handTarget.localPalmPosition;
        }

        public static void GetGrabPosition(HandTarget handTarget, Handle handle, out Vector3 handPosition, out Quaternion handRotation) {
            Vector3 handleWPos = handle.transform.position; // TransformPoint(handle.position);
            Quaternion handleWRot = handle.transform.rotation; // * Quaternion.Euler(handle.rotation);

            GetGrabPosition(handTarget, handleWPos, handleWRot, out handPosition, out handRotation);
        }

        public static void GetGrabPosition(HandTarget handTarget, Vector3 targetPosition, Quaternion targetRotation, out Vector3 handPosition, out Quaternion handRotation) {
            Quaternion palm2handRot = Quaternion.Inverse(handTarget.handPalm.localRotation) * handTarget.hand.bone.toTargetRotation;
            handRotation = targetRotation * palm2handRot;

            Vector3 hand2palmPos = handTarget.handPalm.localPosition;
            Vector3 hand2palmWorld = handTarget.hand.bone.transform.TransformVector(hand2palmPos);
            Vector3 hand2palmTarget = handTarget.hand.target.transform.InverseTransformVector(hand2palmWorld); // + new Vector3(0, -0.03F, 0); // small offset to prevent fingers colliding with collider
            handPosition = targetPosition + handRotation * -hand2palmTarget;
            Debug.DrawLine(targetPosition, handPosition);
        }

        // This is not fully completed, no parenting of joints are created yet
        public static void GrabHandle(HandTarget handTarget, Handle handle) {
            handTarget.grabbedHandle = handle;
            handTarget.targetToHandle = handTarget.hand.target.transform.InverseTransformPoint(handle.transform.position);
            handTarget.grabbedObject = handle.gameObject;
#if pHUMANOID
            handle.handTarget = handTarget;

            if (handle.pose != null)
                handTarget.SetPose1(handle.pose);
#endif
        }

        #endregion

        #region Pinch
        public static void NetworkedPinch(HandTarget handTarget, GameObject obj, bool rangeCheck = true) {
            if (handTarget.grabbedObject != null)                   // We are already holding an object
                return;

            if (obj.GetComponent<NoGrab>() != null)                 // Don't pinch NoGrab Rigidbodies
                return;

            Rigidbody objRigidbody = obj.GetComponent<Rigidbody>();
            RigidbodyDisabled objDisabledRigidbody = obj.GetComponent<RigidbodyDisabled>();
            if (objRigidbody == null && objDisabledRigidbody == null)   // We can only pinch Rigidbodies
                return;

            if (objRigidbody != null && objRigidbody.mass > HandTarget.maxGrabbingMass) // Don't pinch too heavy Rigidbodies            
                return;

            if (objDisabledRigidbody != null && objDisabledRigidbody.mass > HandTarget.maxGrabbingMass)    // Don't pinch too heavy Rigidbodies
                return;

            if (handTarget.humanoid.humanoidNetworking != null)
                handTarget.humanoid.humanoidNetworking.Grab(handTarget, obj, rangeCheck, HandTarget.GrabType.Pinch);

            LocalPinch(handTarget, obj);

            //Collider[] handColliders = handTarget.hand.bone.transform.GetComponentsInChildren<Collider>();
            //foreach (Collider handCollider in handColliders)
            //    Physics.IgnoreCollision(c, handCollider);            
        }

        public static void LocalPinch(HandTarget handTarget, GameObject obj, bool rangeCheck = true) {
            PinchWithSocket(handTarget, obj);
        }

        private static bool PinchWithSocket(HandTarget handTarget, GameObject obj) {
            Handle handle = obj.GetComponentInChildren<Handle>();
            if (handle != null) {
                if (handle.socket != null) {
                    //Debug.Log("Grab from socket");
                    handle.socket.Release();
                }
            }
            bool grabbed = handTarget.pinchSocket.Attach(obj.transform);
            handTarget.grabbedObject = obj;

            return grabbed;
        }
        #endregion

        #endregion
    }
}