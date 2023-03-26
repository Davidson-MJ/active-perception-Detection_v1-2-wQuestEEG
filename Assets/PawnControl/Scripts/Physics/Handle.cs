using UnityEngine;
#if hNW_UNET
using UnityEngine.Networking;
#endif

namespace Passer {
    using Pawn;
#if pHUMANOID
    using Humanoid;
#endif

    /// <summary>Component to specify behaviour when grabbing the GameObject</summary>
    [HelpURL("https://passervr.com/documentation/humanoid-control/grabbing-objects/handle/")]
    public class Handle : MonoBehaviour
    //        , Pawn.IHandInteraction
    //#if pHUMANOID
    //        , Humanoid.IHandInteraction
    //#endif
    {

        /// <summary>The position of the handle in world coordinates</summary>
        public Vector3 worldPosition {
            get {
                return transform.position; // TransformPoint(position);
            }
        }
        /// <summary>The rotation of the handle in world coordinates</summary>
        public Quaternion worldRotation {
            get {
                return transform.rotation; // * Quaternion.Euler(rotation);
            }
        }

        /// <summary>The way in which the hand can grab the handle</summary>
        public enum GrabType {
            DefaultGrab,    ///< Same as BarGrab
            BarGrab,        ///< The hand will grab the handle in the specified position and rotation
            BallGrab,       ///< The hand will grab the handle in the specified position, the rotation is free
            RailGrab,       ///< The hand will grab the handle along the the specified position, the rotation around the rail is free
            AnyGrab,        ///< The hand will grab the handle in any position or rotation
            NoGrab          ///< The hand cannot grab the handle or the gameObject
        }
        /// <summary>Select how the hand will grab the handle</summary>
        public GrabType grabType;

        /// <summary>
        /// Sticky handles will not release unless release sticky is used
        /// </summary>
        public bool sticky = false;

        /// <summary>The range within the handle will work. Outside this range normal grabbing is used.</summary>
        public float range = 0.2f;

        /// <summary>The Controller input which will be active while the Handle is grabbed.</summary>
        /// \version v3
        public ControllerEventHandlers[] inputEvents = {
            new ControllerEventHandlers() { label = "Vertical", id = 0 },
            new ControllerEventHandlers() { label = "Horizontal", id = 1 },
            new ControllerEventHandlers() { label = "Stick Button", id = 2 },
            new ControllerEventHandlers() { label = "Vertical", id = 3 },
            new ControllerEventHandlers() { label = "Horizontal", id = 4 },
            new ControllerEventHandlers() { label = "Stick Button", id = 5 },
            new ControllerEventHandlers() { label = "Button 1", id = 6 },
            new ControllerEventHandlers() { label = "Button 2", id = 7 },
            new ControllerEventHandlers() { label = "Button 3", id = 8 },
            new ControllerEventHandlers() { label = "Button 4", id = 9 },
            new ControllerEventHandlers() { label = "Trigger 1", id = 10 },
            new ControllerEventHandlers() { label = "Trigger 2", id = 11 },
            new ControllerEventHandlers() { label = "Option", id = 12 },
        };

        /// <summary>The socket holding the handle</summary>
        /// This parameter contains the socket holding the handle
        /// when it is held by a socket.
        public Socket socket;
#if pHUMANOID
        /// <summary>Which hand can pick up this handle?</summary>
        public enum Hand {
            Both, ///< The handle can be picked up by any hand 
            Left, ///< The handle can only be grabbed by the left hand
            Right ///< The handle can only be grabbed by the right hand
        }
        /// <summary>Selects which hand can pick up this handle</summary>
        public Hand hand;

        /// <summary>The Hand Pose which will be active while the Handle is grabbed.</summary>
        public Pose pose;

#if hNEARHANDLE
        public bool useNearPose;
        public int nearPose;
#endif
        /// <summary>The hand target which grabbed the handle.</summary>
        /// This is null when the handle is not grabbed.
        public Humanoid.HandTarget handTarget;
#endif
        /// <summary>
        /// The Handle is held by a socket
        /// </summary>
        public bool isHeld;

        public Vector3 TranslationTo(Vector3 position) {
            Vector3 handlePosition = worldPosition;
            Vector3 translation = position - handlePosition;
            return translation;
        }

        public Quaternion RotationTo(Quaternion orientation) {
            Quaternion handleOrientation = worldRotation;
            Quaternion rotation = orientation * Quaternion.Inverse(handleOrientation);
            return rotation;
        }

        public static void Create(GameObject gameObject, Pawn.PawnHand controllerTarget) {
            GameObject handleObject = new GameObject("Handle");
            handleObject.transform.parent = gameObject.transform;
            handleObject.transform.localRotation = controllerTarget.transform.rotation * gameObject.transform.rotation;
            handleObject.transform.localPosition = gameObject.transform.InverseTransformPoint(controllerTarget.transform.position);

            Handle handle = gameObject.AddComponent<Handle>();
            handle.grabType = GrabType.BarGrab;
        }
#if pHUMANOID
        public static void Create(GameObject gameObject, Humanoid.HandTarget handTarget) {
            GameObject handleObject = new GameObject("Handle");
            handleObject.transform.parent = gameObject.transform;
            handleObject.transform.localRotation = Quaternion.Inverse(Quaternion.Inverse(handTarget.handPalm.rotation * gameObject.transform.rotation));
            handleObject.transform.localPosition = gameObject.transform.InverseTransformPoint(handTarget.handPalm.position);

            Handle handle = gameObject.AddComponent<Handle>();
            handle.grabType = GrabType.BarGrab;
            handle.handTarget = handTarget;
        }
#endif

#if pHUMANOID
        /// <summary>Finds the handle on the transform closest to the given position</summary>
        public static Handle GetClosestHandle(Transform transform, Vector3 position, Hand hand, float range = float.PositiveInfinity) {
            Handle[] handles = transform.GetComponentsInChildren<Handle>();

            Handle closestHandle = null;
            float closestDistance = float.PositiveInfinity;
            foreach (Handle handle in handles) {
                bool correctHand = CheckHand(handle, hand);
                if (!correctHand)
                    continue;

                if (handle.grabType == GrabType.RailGrab) {
                    // Determine the closest position on the rail in local space
                    Vector3 localSocketPosition = handle.transform.InverseTransformPoint(position);
                    Vector3 projectedOnRail = Vector3.Project(localSocketPosition, Vector3.up);
                    Vector3 clampedOnRail = projectedOnRail;
                    float railLength = handle.transform.lossyScale.y;
                    if (projectedOnRail.magnitude > railLength / 2)
                        clampedOnRail = projectedOnRail.normalized * (railLength / 2);

                    // Now convert it back to world space
                    Vector3 targetPosition = handle.transform.TransformPoint(clampedOnRail);
                    // And determine the distance to of the grab position to the closest point on the rail
                    float distance = Vector3.Distance(position, targetPosition);
                    if (distance < closestDistance && distance < range) {
                        closestHandle = handle;
                        closestDistance = distance;
                    }
                }
                else {
                    float distance = Vector3.Distance(handle.worldPosition, position);
                    if (distance < closestDistance && distance < range) {
                        closestHandle = handle;
                        closestDistance = distance;
                    }
                }
            }

            return closestHandle;
        }
#endif
        /// <summary>Finds the handle on the transform closest to the given position</summary>
        public static Handle GetClosestHandle(Transform transform, Vector3 position, float range = float.PositiveInfinity) {
            Handle[] handles = transform.GetComponentsInChildren<Handle>();

            Handle closestHandle = null;
            float closestDistance = float.PositiveInfinity;
            foreach (Handle handle in handles) {
                if (handle.grabType == GrabType.RailGrab) {
                    // Determine the closest position on the rail in local space
                    Vector3 localSocketPosition = handle.transform.InverseTransformPoint(position);
                    Vector3 projectedOnRail = Vector3.Project(localSocketPosition, Vector3.up);
                    Vector3 clampedOnRail = projectedOnRail;
                    float railLength = handle.transform.lossyScale.y;
                    if (projectedOnRail.magnitude > railLength / 2)
                        clampedOnRail = projectedOnRail.normalized * (railLength / 2);

                    // Now convert it back to world space
                    Vector3 targetPosition = handle.transform.TransformPoint(clampedOnRail);
                    // And determine the distance to of the grab position to the closest point on the rail
                    float distance = Vector3.Distance(position, targetPosition);
                    if (distance < closestDistance && distance < range) {
                        closestHandle = handle;
                        closestDistance = distance;
                    }
                }
                else {
                    float distance = Vector3.Distance(handle.worldPosition, position);
                    if (distance < closestDistance && distance < range) {
                        closestHandle = handle;
                        closestDistance = distance;
                    }
                }
            }

            return closestHandle;
        }

#if pHUMANOID
        /// <summary>Finds the handle on the transform closest to the given position</summary>
        /// Handles not in socket have lower priority
        public static Handle GetClosestHandle(Transform transform, Vector3 position, Hand hand, bool rangeCheck = true) {
            Handle[] handles = transform.GetComponentsInChildren<Handle>();

            Handle closestHandle = GetClosestHandle(handles, position, hand, rangeCheck);
            if (closestHandle != null)
                return closestHandle;

            handles = transform.GetComponentsInParent<Handle>();
            closestHandle = GetClosestHandle(handles, position, hand, rangeCheck);
            return closestHandle;
        }

        protected static Handle GetClosestHandle(Handle[] handles, Vector3 position, Hand hand, bool rangeCheck = true) {
            bool closestInSocket = true;
            Handle closestHandle = null;
            float closestDistance = float.PositiveInfinity;
            foreach (Handle handle in handles) {
                bool correctHand = CheckHand(handle, hand);
                if (!correctHand)
                    continue;

                float distance = Vector3.Distance(handle.worldPosition, position);
                if ((rangeCheck == false || distance < handle.range) &&
                    (closestInSocket || (distance < closestDistance && handle.socket == null))) {
                    closestHandle = handle;
                    closestDistance = distance;
                    closestInSocket = handle.socket != null;
                }
                //Debug.Log("Closest: " + closestHandle + " " + closestInSocket);
            }

            return closestHandle;
        }

        protected static bool CheckHand(Handle handle, Hand hand) {
            if (handle.hand == Hand.Both)
                return true;
            else
                return (handle.hand == hand);
        }

#endif

        /// <summary>Releases this handle from the socket</summary>
        public void ReleaseFromSocket() {
            if (socket == null)
                return;

            socket.Release();
        }

#if pHUMANOID
        #region Hand Target Utilities

        public bool rigidbodyIsKinematic {
            get {
                Rigidbody rb = null;
                if (handTarget != null)
                    rb = handTarget.handRigidbody;
                if (rb == null)
                    rb = GetComponentInParent<Rigidbody>();
                if (rb != null)
                    return rb.isKinematic;
                else
                    return false;
            }
            set {
                Rigidbody rb = null;
                if (handTarget != null)
                    rb = handTarget.handRigidbody;
                if (rb == null)
                    rb = GetComponentInParent<Rigidbody>();
                if (rb != null)
                    rb.isKinematic = value;
            }
        }

        #endregion
#endif

        #region Update

        protected virtual void Update() {
            UpdateGrabbed();
        }

        #endregion

        #region Events

        public GameObjectEventHandlers grabbedEvent = new GameObjectEventHandlers() {
            label = "Grab Event",
            tooltip =
                "Call functions using the grabbing status\n" +
                "Parameter: the grabbed object",
            eventTypeLabels = new string[] {
                "Nothing",
                "On Grab Start",
                "On Let Go",
                "While Holding",
                "While Not Holding",
                "On Grab Change",
                "Always"
            },
            //fromEventLabelBool = "Handle.isHeld",
            fromEventLabel = "socket.gameObject"
        };

        public virtual void UpdateGrabbed() {
            isHeld = socket != null;
            if (isHeld)
                grabbedEvent.value = socket.gameObject;
            else
                grabbedEvent.value = null;
        }

        /*
        public virtual void OnGrabbed(PawnHand handTarget) {
            // ControllerInput is now handled in ControllerSocket.Attach
        }

        public virtual void OnLetGo(PawnHand handTarget) {
            // ControllerInput is now handled in ControllerSocket.Release
        }

#if pHUMANOID
        public virtual void OnGrabbed(HandTarget handTarget) {
            this.handTarget = handTarget;
            ControllerInput globalInput = handTarget.humanoid.GetComponent<ControllerInput>();
            if (globalInput == null)
                return;

            for (int i = 0; i < inputEvents.Length; i++) {
                if (inputEvents[i].events != null && inputEvents[i].events.Count > 0 &&
                    inputEvents[i].events[0].eventType != EventHandler.Type.Never) {
                    if (handTarget.isLeft)
                        globalInput.leftInputEvents[i].events.Insert(0, inputEvents[i].events[0]);
                    else
                        globalInput.rightInputEvents[i].events.Insert(0, inputEvents[i].events[0]);
                }
            }
        }

        public virtual void OnLetGo(HandTarget handTarget) {
            this.handTarget = null;
            ControllerInput globalInput = handTarget.humanoid.GetComponent<ControllerInput>();
            if (globalInput == null)
                return;

            for (int i = 0; i < inputEvents.Length; i++) {
                if (inputEvents[i].events != null && inputEvents[i].events.Count > 0 &&
                        inputEvents[i].events[0].eventType != EventHandler.Type.Never) {
                    if (handTarget.isLeft)
                        globalInput.leftInputEvents[i].events.RemoveAll(x => x == inputEvents[i].events[0]);
                    else
                        globalInput.rightInputEvents[i].events.RemoveAll(x => x == inputEvents[i].events[0]);
                }
            }
        }
   

#endif
        */
        #endregion

#if pHUMANOID
#if hNEARHANDLE
        private BasicHandPhysics nearHand;

        public void OnTriggerEnter(Collider other) {
            Rigidbody rigidbody = other.attachedRigidbody;
            if (rigidbody == null)
                return;

            nearHand = rigidbody.GetComponent<BasicHandPhysics>();
        }

        private void Update() {
            if (nearHand != null) {
                Vector3 handlePosition = transform.TransformPoint(position);
                float distance = Vector3.Distance(nearHand.target.handPalm.position, handlePosition) * 2;
                float f = Mathf.Clamp01((distance + 0.25F) / range);
                f = f * f * f;
                nearHand.target.SetHandPose(nearPose, 1 - f);
                if (1 - f <= 0) {
                    nearHand.target.SetHandPose1(1);
                    nearHand = null;
                }
            }
        }
#endif
#endif

        #region Gizmos

        protected Mesh gizmoMesh;

        void OnDrawGizmosSelected() {
            if (gizmoMesh == null)
                gizmoMesh = Socket.GenerateGizmoMesh();

            if (enabled) {
                Matrix4x4 m = Matrix4x4.identity;
                Vector3 p = transform.position; // TransformPoint(position);
                Quaternion q = Quaternion.identity; // Quaternion.Euler(rotation);
                m.SetTRS(p, transform.rotation * q, transform.localScale);//Vector3.one);
                Gizmos.color = Color.yellow;
                Gizmos.matrix = m;

                switch (grabType) {
                    case GrabType.DefaultGrab:
                    case GrabType.BarGrab:
                        Gizmos.DrawMesh(gizmoMesh);
                        break;
                    case GrabType.BallGrab:
                        Gizmos.DrawSphere(Vector3.zero, 0.04f);
                        break;
                    case GrabType.RailGrab:
                        Gizmos.DrawCube(Vector3.zero, new Vector3(0.03F, transform.lossyScale.y, 0.03F));
                        break;
                }
                //if (grabType != GrabType.NoGrab)
                //    Gizmos.DrawWireSphere(Vector3.zero, range);
            }
        }

        #endregion

    }
}
