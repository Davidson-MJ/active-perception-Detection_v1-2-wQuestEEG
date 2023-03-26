using System.Collections.Generic;

using UnityEngine;
using UnityEngine.XR;

namespace Passer.Tracking {

    public class UnityXRCamera : SensorComponent {
#if pUNITYXR
        public TrackerComponent tracker;
        protected Camera unityCamera;

        protected InputDevice device;

        #region Start

        public static UnityXRCamera FindXRCamera(UnityXR unityXR) {
            UnityXRCamera xrCamera = unityXR.GetComponentInChildren<UnityXRCamera>();
            return xrCamera;
        }


        /// <summary>Find or Create a new Unity XR Camera</summary>
        public static UnityXRCamera Get(UnityXR unityXR, Vector3 position, Quaternion rotation) {
            if (unityXR == null)
                return null;

            //Transform cameraTransform = tracker.transform.Find("Camera");
            //if (cameraTransform == null) {
            UnityXRCamera unityCamera = FindXRCamera(unityXR);
            if (unityCamera == null) {
                GameObject trackerObject = new GameObject("Camera");
                Transform cameraTransform = trackerObject.transform;

                cameraTransform.parent = unityXR.transform;
                cameraTransform.position = position;
                cameraTransform.rotation = rotation;

                unityCamera = cameraTransform.gameObject.AddComponent<UnityXRCamera>();
                unityCamera.tracker = unityXR;


                unityCamera.unityCamera = cameraTransform.GetComponent<Camera>();
                if (unityCamera.unityCamera == null) {
                    unityCamera.unityCamera = cameraTransform.gameObject.AddComponent<Camera>();
                    unityCamera.unityCamera.nearClipPlane = 0.05F;

                    cameraTransform.gameObject.AddComponent<AudioListener>();
                }
            }

            return unityCamera;
        }

        public static UnityXRCamera Get(UnityXR unityXR, InputDevice device) {
            if (unityXR == null)
                return null;

            UnityXRCamera unityCamera = FindXRCamera(unityXR);
            if (unityCamera == null) {
                GameObject trackerObject = new GameObject("Camera");
                Transform cameraTransform = trackerObject.transform;

                cameraTransform.parent = unityXR.transform;
                cameraTransform.localPosition = Vector3.zero;
                cameraTransform.localRotation = Quaternion.identity;

                unityCamera = cameraTransform.gameObject.AddComponent<UnityXRCamera>();
                unityCamera.tracker = unityXR;


                unityCamera.unityCamera = cameraTransform.GetComponent<Camera>();
                if (unityCamera.unityCamera == null) {
                    unityCamera.unityCamera = cameraTransform.gameObject.AddComponent<Camera>();
                    unityCamera.unityCamera.nearClipPlane = 0.05F;

                    cameraTransform.gameObject.AddComponent<AudioListener>();
                }
            }
            unityCamera.device = device;

            return unityCamera;
        }

        public static Camera AddCamera(UnityXRCamera xrHmd) {
            xrHmd.unityCamera = xrHmd.GetComponent<Camera>();
            if (xrHmd.unityCamera == null) {
                xrHmd.unityCamera = xrHmd.gameObject.AddComponent<Camera>();
                xrHmd.unityCamera.nearClipPlane = 0.05F;

                xrHmd.gameObject.AddComponent<AudioListener>();
            }

            //if (collisionFader)
            //    AddScreenFader(xrHmd);
            //else
            //    RemoveScreenFader(xrHmd.transform);

            return xrHmd.unityCamera;
        }

        public static void RemoveCamera(UnityXRCamera xrHmd) {
            Camera camera = xrHmd.GetComponentInChildren<Camera>();
            if (camera != null) {
                if (Application.isPlaying)
                    Destroy(camera);
                else
                    DestroyImmediate(camera);
            }

            AudioListener listener = xrHmd.GetComponentInChildren<AudioListener>();
            if (listener != null) {
                if (Application.isPlaying)
                    Destroy(listener);
                else
                    DestroyImmediate(listener);
            }
        }

        #endregion

        #region Update

        private List<XRNodeState> nodeStates = new List<XRNodeState>();

        public override void UpdateComponent() {
            base.UpdateComponent();

            status = Tracker.Status.Present;
            positionConfidence = 0;
            rotationConfidence = 0;

            // This is still legacy!
            InputTracking.GetNodeStates(nodeStates);
            foreach (XRNodeState nodeState in nodeStates) {
                if (nodeState.nodeType == XRNode.CenterEye) {
                    Vector3 position;
                    if (nodeState.TryGetPosition(out position)) {
                        transform.position = tracker.transform.TransformPoint(position);
                        positionConfidence = 1;
                        status = Tracker.Status.Tracking;
                    }
                    Quaternion rotation;
                    if (nodeState.TryGetRotation(out rotation)) {
                        transform.rotation = tracker.transform.rotation * rotation;
                        rotationConfidence = 1;
                        status = Tracker.Status.Tracking;
                    }

                }
            }
        }

        public void Show(bool showModel) {
        }

        #endregion
#endif
    }

}