using System.Collections.Generic;
using UnityEngine;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#else
using UnityEngine.VR;
#endif

namespace Passer.Pawn {

    [System.Serializable]
    public class UnityController : ControllerSensor {

        new public UnityTracker tracker;

        #region Start

        public override void Start(Transform targetTransform) {
            PawnHand handTarget = targetTransform.GetComponent<PawnHand>();
            target = handTarget;
#if hLEGACYXR
            tracker = handTarget.pawn.unityTracker;
#endif
        }

        #endregion

        #region Update

        public override void Update() {
            if (!enabled)
                return;

            UpdateTargetTransform();
        }

        protected virtual void UpdateTargetTransform() {
            if (!XRSettings.enabled)
                return;

            XRNode xrNode = controllerTarget.isLeft ? XRNode.LeftHand : XRNode.RightHand;

#if !pUNITYXR && !UNITY_2020_1_OR_NEWER
            if (tracker == null || tracker.trackerTransform == null)
                return;

            List<XRNodeState> nodeStates = new List<XRNodeState>();
            InputTracking.GetNodeStates(nodeStates);

            foreach (XRNodeState nodeState in nodeStates) {
                if (nodeState.nodeType == xrNode) {
                    Vector3 position;
                    if (nodeState.TryGetPosition(out position)) {
                        target.transform.position = tracker.trackerTransform.TransformPoint(position);
                    }

                    Quaternion rotation;
                    if (nodeState.TryGetRotation(out rotation)) {
                        target.transform.rotation = tracker.trackerTransform.rotation * rotation * Quaternion.AngleAxis(90, Vector3.right);
                    }

                }
            }
            //Quaternion localControllerRotation = InputTracking.GetLocalRotation(xrNode);
            //target.transform.rotation = tracker.trackerTransform.rotation * localControllerRotation * Quaternion.AngleAxis(90, Vector3.right);

            //Vector3 localControllerPosition = InputTracking.GetLocalPosition(xrNode);
            //target.transform.position = tracker.trackerTransform.TransformPoint(localControllerPosition);
#endif
        }

        #endregion
    }
}
