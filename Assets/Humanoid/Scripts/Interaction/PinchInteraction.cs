using UnityEngine;

namespace Passer.Humanoid {

    public class PinchInteraction : MonoBehaviour {
        protected HandTarget handTarget;
        protected Socket pinchSocket;
        public bool pinching;
        public static float pinchDistance = 0.04F;

        // TODO: add phalanges.tip to the list of bones
        private static Vector3 GetTipPosition(HumanoidTarget.BoneTransform bone, Vector3 outward) {
            Vector3 tip = bone.transform.position + bone.targetRotation * outward * 0.02F; // bone.length?
            return tip;
        }

        #region Start
        protected virtual void Start() {
            if (handTarget == null)
                handTarget = GetComponent<HandTarget>();
            if (handTarget == null)
                return;

            if (handTarget.humanoid.isRemote)
                // Remote avatars cannot interact
                return;

            SocketCheck();
        }

        protected void SocketCheck() {
            HumanoidTarget.BoneTransform handBone = handTarget.hand.bone;
            // How to we distinguish between pinch and palm socket later????? by name?
            pinchSocket = handBone.transform.GetComponentInChildren<Socket>();
            if (pinchSocket != null)
                // Pinch Socket is already present
                return;

            GameObject socketObj = new GameObject(handTarget.isLeft ? "Left Pinch Socket" : "Right Pinch Socket");
            Transform socketTransform = socketObj.transform;
            socketTransform.parent = handTarget.hand.bone.transform;
            socketTransform.position = handTarget.hand.target.transform.TransformPoint(0.1F, -0.035F, 0.03F);
            socketTransform.rotation = handTarget.hand.bone.targetRotation * Quaternion.Euler(355, 190, 155);

            pinchSocket = socketObj.AddComponent<Socket>();
        }
        #endregion

        #region Update
        // sentinel.
        // TODO: implement thread-safe sentinel
        protected bool pinchChecking = false;

        protected virtual void FixedUpdate() {
            pinching = IsPinching(handTarget);

            if (handTarget.grabbedObject != null && pinchSocket.attachedTransform != null)
                // We are pinching an object
                PinchDropCheck(handTarget, pinchSocket);

            else {
                // We are not pinching an object
                if (handTarget.touchedObject == null || handTarget.grabbedObject != null)
                    return;

                if (!pinchChecking) {
                    pinchChecking = true;
                    PinchCheck(handTarget, pinchSocket, handTarget.touchedObject);
                    pinchChecking = false;
                }
            }
        }
        #endregion

        #region Pinch Grab
        public void PinchCheck(HandTarget handTarget, Socket pinchSocket, GameObject obj) {
            if (IsPinching(handTarget) && handTarget.CanBeGrabbed(obj))
                Pinch(handTarget, obj);
        }

        public static bool IsPinching(HandTarget handTarget) {
            // other fingers need to be open
            float handCurl = handTarget.HandCurl();
            if (handCurl > 1)
                return false;

            float distance = GetPinchDistance(handTarget);
            return distance < pinchDistance;
        }

        public static bool IsNotPinching(HandTarget handTarget) {
            float distance = GetPinchDistance(handTarget);
            return distance > pinchDistance + 0.02F;
        }

        protected static float GetPinchDistance(HandTarget handTarget) {
            HumanoidTarget.BoneTransform thumbDistalBone = handTarget.fingers.thumb.distal.bone;
            HumanoidTarget.BoneTransform indexDistalBone = handTarget.fingers.index.distal.bone;
            // We need finger tips for pinch to work
            if (thumbDistalBone.transform == null || indexDistalBone.transform == null)
                return float.PositiveInfinity;

            Vector3 thumbTip = GetTipPosition(thumbDistalBone, handTarget.outward);
            Vector3 indexTip = GetTipPosition(indexDistalBone, handTarget.outward);

            //Debug.DrawLine(thumbTip, indexTip, Color.yellow);

            float distance = Vector3.Distance(thumbTip, indexTip);
            return distance;
        }

        public static void Pinch(HandTarget handTarget, GameObject obj) {
            Handle handle = obj.GetComponentInChildren<Handle>();
            if (handle != null) {
                if (handle.socket != null) {
                    Debug.Log("Grab from socket");
                    handle.socket.Release();
                }
            }
            handTarget.pinchSocket.Attach(obj.transform);
            handTarget.grabbedObject = obj;
        }

        #endregion

        #region Pinch Drop
        public static void PinchDropCheck(HandTarget handTarget, Socket pinchSocket) {
            bool isDropping = IsPinchDropping(handTarget);
            bool pulledLoose = PulledLoose(handTarget, pinchSocket);
            if (isDropping || pulledLoose)
                PinchDrop(handTarget, pinchSocket);
        }

        protected static bool IsPinchDropping(HandTarget handTarget) {
            float pinchDistance = GetPinchDistance(handTarget);
            return pinchDistance > 0.06F;
        }

        protected static bool PulledLoose(HandTarget handTarget, Socket pinchSocket) {
            float forearmStretch = Vector3.Distance(handTarget.hand.bone.transform.position, handTarget.forearm.bone.transform.position) - handTarget.forearm.bone.length;
            if (forearmStretch > 0.15F)
                return true;

            if (pinchSocket.attachedTransform == null)
                return true;

            return false;
        }

        protected static void PinchDrop(HandTarget handTarget, Socket pinchSocket) {
            pinchSocket.Release();
            handTarget.grabbedObject = null;
        }
        #endregion
    }
}