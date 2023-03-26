using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Passer.Tracking {

    public class UnityXR : TrackerComponent {

        #region Manage

        public static void RemoveCamera(Transform targetTransform) {
            Camera camera = targetTransform.GetComponentInChildren<Camera>();
            if (camera != null) {
                if (Application.isPlaying)
                    Object.Destroy(camera.gameObject);
                else
                    Object.DestroyImmediate(camera.gameObject);
            }
        }

        /// <summary>Create a new Unity Tracker</summary>
        public static UnityXR Get(Transform realWorld) {
            Transform trackerTransform = realWorld.Find("UnityXR");
            if (trackerTransform == null) {
                GameObject trackerObject = new GameObject("UnityXR");
                trackerTransform = trackerObject.transform;

                trackerTransform.parent = realWorld;
                trackerTransform.localPosition = Vector3.zero;
                trackerTransform.localRotation = Quaternion.identity;
            }

            UnityXR tracker = trackerTransform.GetComponent<UnityXR>();
            if (tracker == null) {
                tracker = trackerTransform.gameObject.AddComponent<UnityXR>();
                tracker.realWorld = realWorld;
            }

            return tracker;
        }

        #region Hmd

        protected UnityXRHmd _hmd;
        public UnityXRHmd hmd {
            get {
                if (_hmd != null)
                    return _hmd;

                _hmd = this.GetComponentInChildren<UnityXRHmd>();
                if (_hmd != null)
                    return _hmd;

                _hmd = FindObjectOfType<UnityXRHmd>();
                return _hmd;
            }
        }

        public UnityXRHmd GetHmd(Vector3 position, Quaternion rotation) {
            if (hmd == null) {
                GameObject sensorObj = new GameObject("Hmd");
                Transform sensorTransform = sensorObj.transform;

                sensorTransform.parent = this.transform;
                sensorTransform.position = position;
                sensorTransform.rotation = rotation;

                _hmd = sensorTransform.gameObject.AddComponent<UnityXRHmd>();
                _hmd.tracker = this;

                _hmd.unityCamera = _hmd.GetComponent<Camera>();
                if (_hmd.unityCamera == null) {
                    _hmd.unityCamera = _hmd.gameObject.AddComponent<Camera>();
                    _hmd.unityCamera.nearClipPlane = 0.1F;

                    _hmd.gameObject.AddComponent<AudioListener>();
                    _hmd.gameObject.tag = "MainCamera";
                }
                AddScreenFader(_hmd.unityCamera);
            }

            return _hmd;
        }

        private static void AddScreenFader(Camera camera) {
            if (camera == null)
                return;

            Transform planeTransform = camera.transform.Find("Fader");
            if (planeTransform != null)
                return;

            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.transform.name = "Fader";
            plane.transform.parent = camera.transform;
            plane.transform.localEulerAngles = new Vector3(-90, 0, 0);
            plane.transform.localPosition = new Vector3(0, 0, camera.nearClipPlane + 0.01F);

            Renderer renderer = plane.GetComponent<Renderer>();
            if (renderer != null) {
                Shader fadeShader = Shader.Find("Standard");
                Material fadeMaterial = new Material(fadeShader);
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
                renderer.enabled = false;
            }

            Collider c = plane.GetComponent<Collider>();
            Object.DestroyImmediate(c);
        }

        private static void RemoveScreenFader(Transform cameraTransform) {
            if (cameraTransform == null)
                return;

            Transform plane = cameraTransform.Find("Fader");
            if (plane == null)
                return;

            Object.DestroyImmediate(plane.gameObject);
        }

        #endregion

        #region Controller
#if pUNITYXR
        protected UnityXRController _leftController;
        protected UnityXRController _rightController;

        public UnityXRController leftController {
            get {
                if (_leftController != null)
                    return _leftController;

                _leftController = FindController(true);
                return _leftController;
            }
        }
        public UnityXRController rightController {
            get {
                if (_rightController != null)
                    return _rightController;

                _rightController = FindController(false);
                return _rightController;
            }
        }

        public UnityXRController FindController(bool isLeft) {
            UnityXRController[] unityControllers = this.GetComponentsInChildren<UnityXRController>();
            foreach (UnityXRController unityController in unityControllers) {
                if (unityController.isLeft == isLeft) {
                    return unityController;
                }
            }

            unityControllers = FindObjectsOfType<UnityXRController>();
            foreach (UnityXRController hydraController in unityControllers) {
                if (hydraController.isLeft == isLeft) {
                    return hydraController;
                }
            }
            return null;
        }

        public UnityXRController GetController(bool isLeft, Vector3 position, Quaternion rotation) {
            UnityXRController controller = FindController(isLeft);
            if (controller == null) {
                GameObject sensorObj = new GameObject(isLeft ? "Left Controller" : "Right Controler");
                Transform sensorTransform = sensorObj.transform;

                sensorTransform.parent = this.transform;
                sensorTransform.position = position;
                sensorTransform.rotation = rotation;

                controller = sensorTransform.gameObject.AddComponent<UnityXRController>();
                controller.tracker = this;
                controller.isLeft = isLeft;
            }

            if (isLeft)
                _leftController = controller;
            else
                _rightController = controller;

            return controller;
        }
#endif
        #endregion

        #endregion

        #region Start

        protected override void Start() {
            base.Start();

            Passer.Tracking.UnityVRDevice.Start();
#if pUNITYXR
            if (hmd != null)
                hmd.tracker = this;

            if (_leftController == null) {
                _leftController = FindController(true);
                //Debug.LogWarning("Left Controller created at origin");
                //_leftController = GetController(true, Vector3.zero, Quaternion.identity);
            }
            if (leftController != null)
                leftController.tracker = this;

            if (_rightController == null) {
                _rightController = FindController(false);
                //Debug.LogWarning("Right Controller created at origin");
                //_rightController = GetController(false, Vector3.zero, Quaternion.identity);
            }
            if (rightController != null)
                rightController.tracker = this;
#endif
        }

        //protected override void Start() {
        //    InputDevices.deviceConnected += DeviceConnected;
        //    InputDevices.deviceDisconnected += DeviceDisconnected;
        //}

        //protected virtual void DeviceConnected(InputDevice device) {
        //    /*
        //    if ((device.characteristics & InputDeviceCharacteristics.Camera) != 0) {
        //        // Does this make sense ? no.
        //        _hmd = hmd; // FindHmd(); // UnityXRHmd.Get(this, device);
        //    }
        //    else if ((device.characteristics & InputDeviceCharacteristics.Controller) != 0) {
        //        if ((device.characteristics & InputDeviceCharacteristics.Left) != 0)
        //            leftController = UnityXRController.Get(this, device);
        //        else
        //            rightController = UnityXRController.Get(this, device);
        //    }
        //    */
        //}

        //protected virtual void DeviceDisconnected(InputDevice device) {
        //}

        #endregion

        #region Update

        protected override void Update() {
            base.Update();

            if (hmd != null)
                status = hmd.status;
        }

        #endregion
    }
}