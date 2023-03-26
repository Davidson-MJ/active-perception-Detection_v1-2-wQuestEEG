using System.Collections.Generic;
using UnityEngine;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#endif

namespace Passer {
    using Pawn;

    [System.Serializable]
    public class UnityCamera : Sensor {
        public override string name {
            get { return "First Person Camera"; }
        }

        new public UnityTracker tracker;

        #region Manage

        public void Detach(Transform rootTransform) {
            if (sensorTransform == null)
                return;

            Vector3 localPosition = sensorTransform.localPosition;
            sensorTransform.localPosition = Vector3.zero;
            sensorTransform.parent = tracker.trackerTransform;
        }

        #endregion

        #region Start

        public override void Start(Transform targetTransform) {
            PawnHead headTarget = targetTransform.GetComponent<PawnHead>();
            target = headTarget;
#if hLEGACYXR
            tracker = headTarget.pawn.unityTracker;
#endif

            if (!enabled)
                return;

            if (UnityTracker.xrDevice != UnityTracker.XRDeviceType.None)
                Detach(headTarget.pawn.transform);
        }

        #region Camera

        //public static Camera GetCamera(HeadTarget cameraTarget) {
        //    if (cameraTarget.unityCamera.camera != null)
        //        return cameraTarget.unityCamera.camera;

        //    Camera camera = cameraTarget.GetComponentInChildren<Camera>();
        //    if (cameraTarget.unityCamera.enabled) {
        //        camera = CheckCamera(cameraTarget);

        //        if (camera == null)
        //            camera = cameraTarget.GetComponentInChildren<Camera>();

        //    }
        //    return camera;
        //}

        public static Camera CheckCamera(PawnHead cameraTarget) {
#if hLEGACYXR
            if (cameraTarget.unityCamera.enabled) {
                return StaticAddCamera(cameraTarget);
            }
            else
#endif
            {
                RemoveCamera(cameraTarget);
                return null;
            }
        }

        public void CheckCameraLocation() {
            //sensorTransform.SetParent(tracker.trackerTransform, true);
            //sensorTransform.localPosition = Vector3.zero;
            //sensorTransform.localRotation = Quaternion.identity;
        }

        public static Camera StaticAddCamera(PawnHead headTarget) {
            Camera camera = headTarget.transform.GetComponentInChildren<Camera>();
            if (camera == null) {
                GameObject cameraObj = new GameObject("First Person Camera");
                camera = cameraObj.AddComponent<Camera>();
                camera.tag = "MainCamera";

                camera.nearClipPlane = 0.1F;

#if pUNITYXR
                //headTarget.unityCamera.transform = camera.transform;
#endif
#if hLEGACYXR
                headTarget.unityCamera.sensorTransform = camera.transform;
#endif
                camera.transform.SetParent(headTarget.transform, false);
                //camera.transform.position = hmdTarget.player.transform.position + Vector3.up * 1.6F;
                //camera.transform.rotation = Quaternion.Euler(0, headTarget.pawn.transform.eulerAngles.y, 0);

                camera.transform.localPosition = new Vector3(0, 0, headTarget.pawn.radius);
                camera.transform.localRotation = Quaternion.identity;

                cameraObj.AddComponent<AudioListener>();
            }

            //if (headTarget.collisionFader)
            //    AddScreenFader(camera);
            //else
            //    RemoveScreenFader(camera.transform);

            return camera;
        }

        public Transform AddCamera() {
            Camera camera = UnityCamera.StaticAddCamera((PawnHead)target);
            return camera.transform;
        }


        public static Camera AddCamera(Transform targetTransform) {
            Camera camera = targetTransform.GetComponentInChildren<Camera>();
            if (camera == null) {
                GameObject cameraObj = new GameObject("First Person Camera");
                camera = cameraObj.AddComponent<Camera>();
                camera.tag = "MainCamera";

                camera.nearClipPlane = 0.1F;

                camera.transform.SetParent(targetTransform, false);
                camera.transform.localPosition = Vector3.zero;
                camera.transform.rotation = Quaternion.Euler(0, targetTransform.eulerAngles.y, 0);

                cameraObj.AddComponent<AudioListener>();
            }
            //AddScreenFader(camera);

            return camera;
        }

        public static void RemoveCamera(PawnHead hmdTarget) {
            RemoveCamera(hmdTarget.transform);
        }
        public static void RemoveCamera(Transform targetTransform) {
            Camera camera = targetTransform.GetComponentInChildren<Camera>();
            if (camera != null) {
                if (Application.isPlaying)
                    Object.Destroy(camera.gameObject);
                else
                    Object.DestroyImmediate(camera.gameObject);
            }
        }

        #endregion

        #endregion

        #region Update

        bool calibrated = false;


        //protected Vector3 _position;
        //public Vector3 position {
        //    get {
        //        return _position;
        //    }
        //}
        //protected Quaternion _rotation;
        //public Quaternion rotation {
        //    get {
        //        return _rotation;
        //    }
        //}

        public override void Update() {
            if (!enabled)
                return;

            UpdateTargetTransform();

            if (status != Tracker.Status.Tracking)
                return;

            if (!calibrated && tracker.pawn.calibrateAtStart) {
                tracker.pawn.Calibrate();
                calibrated = true;
            }
        }

        protected void UpdateTargetTransform() {
            if (UnityTracker.xrDevice != UnityTracker.XRDeviceType.None)
                UpdateStatus();
        }

        private List<XRNodeState> nodeStates = new List<XRNodeState>();
        protected virtual void UpdateStatus() {
            status = Tracker.Status.Unavailable;
            InputTracking.GetNodeStates(nodeStates);
            foreach (XRNodeState nodeState in nodeStates) {
                if (nodeState.nodeType == XRNode.Head) {
                    if (nodeState.tracked)
                        status = Tracker.Status.Tracking;
                    else
                        status = Tracker.Status.Present;
                }
            }
        }

        #endregion

        #region Fader

        protected Material fadeMaterial;

        protected void InitFader() {
            Transform planeTransform = sensorTransform.Find("Fader");
            if (planeTransform != null)
                return;

            Camera camera = sensorTransform.GetComponent<Camera>();

            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.transform.name = "Fader";
            plane.transform.parent = sensorTransform;
            plane.transform.localEulerAngles = new Vector3(-90, 0, 0);
            plane.transform.localPosition = new Vector3(0, 0, camera.nearClipPlane + 0.01F);

            Renderer renderer = plane.GetComponent<Renderer>();
            if (renderer != null) {
                Shader fadeShader = Shader.Find("Standard");
                fadeMaterial = new Material(fadeShader);
                fadeMaterial.name = "FadeMaterial";
                fadeMaterial.SetFloat("_Mode", 2);
                fadeMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                fadeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                fadeMaterial.SetInt("_ZWrite", 0);
                fadeMaterial.DisableKeyword("_ALPHATEST_ON");
                fadeMaterial.EnableKeyword("_ALPHABLEND_ON");
                fadeMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                fadeMaterial.renderQueue = 3000;
                Color color = Color.black;
                color.a = 0.0F;
                fadeMaterial.SetColor("_Color", new Color(0, 0, 0, 0));
                renderer.material = fadeMaterial;
                renderer.enabled = true;
            }

            Collider c = plane.GetComponent<Collider>();
            Object.DestroyImmediate(c);
        }

        public void Fader(float f) {
            //float elapsedTime = 0.0f;
            //Color color = Color.black;
            //color.a = 0.0f;
            //fadeMaterial.color = color;
            //while (elapsedTime < fadeTime) {
            //    yield return new WaitForEndOfFrame();
            //    elapsedTime += Time.deltaTime;
            Color color = Color.black;
            color.a = Mathf.Clamp01(f);
            fadeMaterial.color = color;
            //}

        }

        #endregion
    }

}