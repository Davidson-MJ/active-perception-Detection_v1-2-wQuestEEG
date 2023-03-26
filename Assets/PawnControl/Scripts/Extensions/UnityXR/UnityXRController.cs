using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Passer.Tracking {

    public class UnityXRController : SensorComponent {
#if pUNITYXR
        public TrackerComponent tracker;
#if pUNITYXR
        public Transform sensorTransform {
            get { return this.transform; }
        }
#endif

        public bool isLeft;

        protected InputDevice device;
        public Vector3 primaryAxis;
        public Vector3 secondaryAxis;
        public float trigger;
        public float grip;
        public float primaryButton;
        public float secondaryButton;
        public float menu;
        public float battery;

        protected XRNode xrNode;

        public GameObject model;

#if hOPENVR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
        protected OpenVRController openVRController;
#endif
        #region Start

        protected override void Start() {
            base.Start();

            xrNode = isLeft ? XRNode.LeftHand : XRNode.RightHand;
            device = InputDevices.GetDeviceAtXRNode(xrNode);
            ShowModel(device.name);
            CheckRenderers();

            InputDevices.deviceConnected += OnDeviceConnected;
            InputDevices.deviceDisconnected += OnDeviceDisconnected;

#if hOPENVR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
            if (UnityVRDevice.xrDevice == UnityVRDevice.XRDeviceType.OpenVR)
                StartOpenVR();
#endif
        }

        /// <summary>
        /// Controller has connected
        /// </summary>
        /// <param name="device">The InputDevice of the controller</param>
        protected virtual void OnDeviceConnected(InputDevice device) {
            bool isLeft = (device.characteristics & InputDeviceCharacteristics.Left) != 0;
            bool isController = (device.characteristics & InputDeviceCharacteristics.Controller) != 0;
            if (isController && isLeft == this.isLeft) {
                if (this.device.name != device.name)
                    ShowModel(device.name);
                this.device = device;
                Show(true);
            }
        }

        /// <summary>
        /// Controller has disconnected
        /// </summary>
        /// This also happens when the device is no longer tracked.
        /// <param name="device">The InputDevice of the controller</param>
        protected virtual void OnDeviceDisconnected(InputDevice device) {
            bool isLeft = (device.characteristics & InputDeviceCharacteristics.Left) != 0;
            bool isController = (device.characteristics & InputDeviceCharacteristics.Controller) != 0;
            if (isController && isLeft == this.isLeft) {
                this.device = device;
                Show(false);
            }
        }

        /// <summary>
        /// Find a Unity XR Controller
        /// </summary>
        /// <param name="unityXR"></param>
        /// <param name="isLeft"></param>
        /// <returns></returns>
        public static UnityXRController FindController(UnityXR unityXR, bool isLeft) {
            UnityXRController[] controllers = unityXR.GetComponentsInChildren<UnityXRController>();
            foreach (UnityXRController controller in controllers) {
                if (controller.isLeft == isLeft)
                    return controller;
            }
            return null;
        }

        /// <summary>
        /// Find or Create a Unity XR Controller
        /// </summary>
        /// <param name="unityXR"></param>
        /// <param name="isLeft"></param>
        /// <returns></returns>
        public static UnityXRController Get(UnityXR unityXR, bool isLeft, Vector3 position, Quaternion rotation) {
            string name = isLeft ? "Left Controller" : "Right Controller";

            if (unityXR == null || unityXR.transform == null)
                return null;

            //Transform controllerTransform = tracker.transform.Find(name);
            UnityXRController unityController = FindController(unityXR, isLeft);
            if (unityController == null) {
                GameObject trackerObject = new GameObject(name);
                Transform controllerTransform = trackerObject.transform;

                controllerTransform.parent = unityXR.transform;
                controllerTransform.position = position;
                controllerTransform.rotation = rotation;

                unityController = controllerTransform.gameObject.AddComponent<UnityXRController>();
                unityController.tracker = unityXR;
                unityController.isLeft = isLeft;
            }

            return unityController;
        }

        protected Dictionary<string, string> modelNames = new Dictionary<string, string>() {
            { "Oculus Touch Controller - Left", "Left Touch Controller" },
            { "Oculus Touch Controller - Right", "Right Touch Controller" },
            { "Spatial Controller - Left", "Windows MR Controller Left" },
            { "Spatial Controller - Right", "Windows MR Controller Right" },
        };

        protected virtual void ShowModel(string deviceName) {
            if (model != null)
                Destroy(model);

            if (deviceName == null)
                return;

            string modelName = deviceName;
            if (modelNames.ContainsKey(modelName))
                modelName = modelNames[deviceName];


            CreateModel(modelName);
        }

        protected void CreateModel() {
#if hLEGACY
            switch (UnityTracker.DetermineLoadedDevice()) {
                case UnityTracker.XRDeviceType.Oculus:
                    CreateModel(isLeft ? "Left Touch Controller" : "Right Touch Controller");
                    break;
                case UnityTracker.XRDeviceType.OpenVR:
                    CreateModel("Vive Controller");
                    break;
                case UnityTracker.XRDeviceType.None:
                    CreateModel("Generic Controller");
                    break;
            }
#endif
        }

        protected void CreateModel(string resourceName) {
            GameObject sensorObject;
            if (resourceName == null) {
                sensorObject = new GameObject("Model");
            }
            else {
                Object controllerPrefab = Resources.Load(resourceName);
                if (controllerPrefab == null)
                    sensorObject = new GameObject("Model");
                else
                    sensorObject = (GameObject)Instantiate(controllerPrefab);

                sensorObject.name = resourceName;
            }

            model = sensorObject;
            model.transform.parent = this.transform;
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
        }

        #endregion

        #region Update

        public override void UpdateComponent() {
            base.UpdateComponent();

            status = Tracker.Status.Unavailable;
            positionConfidence = 0;
            rotationConfidence = 0;

            if (device == null)
                return;

            status = Tracker.Status.Present;

#if hOPENVR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
            if (UnityVRDevice.xrDevice == UnityVRDevice.XRDeviceType.OpenVR) {
                UpdateOpenVR();
            }
            else
#endif
            {

                Vector3 position;
                if (device.TryGetFeatureValue(CommonUsages.devicePosition, out position)) {
                    transform.position = tracker.transform.TransformPoint(position);
                    positionConfidence = 1;
                    status = Tracker.Status.Tracking;
                }

                Quaternion rotation;
                if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation)) {
                    //rotation *= Quaternion.AngleAxis(45, Vector3.right);
                    transform.rotation = tracker.transform.rotation * rotation;
                    rotationConfidence = 1;
                    status = Tracker.Status.Tracking;
                }

                UpdateInput();
            }

        }

        protected virtual void UpdateInput() {
            device.TryGetFeatureValue(CommonUsages.trigger, out trigger);
            device.TryGetFeatureValue(CommonUsages.grip, out grip);

            bool buttonPress;
            bool buttonTouch;

            device.TryGetFeatureValue(CommonUsages.primaryButton, out buttonPress);
            device.TryGetFeatureValue(CommonUsages.primaryTouch, out buttonTouch);
            primaryButton = buttonPress ? 1 : buttonTouch ? 0 : -1;

            device.TryGetFeatureValue(CommonUsages.secondaryButton, out buttonPress);
            device.TryGetFeatureValue(CommonUsages.secondaryTouch, out buttonTouch);
            secondaryButton = buttonPress ? 1 : buttonTouch ? 0 : -1;

            device.TryGetFeatureValue(CommonUsages.menuButton, out buttonPress);
            menu = buttonPress ? 1 : 0;

            Vector2 axis;
            float axisButton;

            device.TryGetFeatureValue(CommonUsages.primary2DAxis, out axis);
            device.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out buttonPress);
            device.TryGetFeatureValue(CommonUsages.primary2DAxisTouch, out buttonTouch);
            axisButton = buttonPress ? 1 : buttonTouch ? 0 : -1;
            primaryAxis = new Vector3(axis.x, axis.y, axisButton);

            device.TryGetFeatureValue(CommonUsages.secondary2DAxis, out axis);
            device.TryGetFeatureValue(CommonUsages.secondary2DAxisClick, out buttonPress);
            device.TryGetFeatureValue(CommonUsages.secondary2DAxisTouch, out buttonTouch);
            axisButton = buttonPress ? 1 : buttonTouch ? 0 : -1;
            secondaryAxis = new Vector3(axis.x, axis.y, axisButton);

            device.TryGetFeatureValue(CommonUsages.batteryLevel, out battery);
        }

        #region OpenVR

#if hOPENVR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)

        protected void StartOpenVR() {
            openVRController = this.gameObject.GetComponent<OpenVRController>();
            if (openVRController == null)
                openVRController = this.gameObject.AddComponent<OpenVRController>();
            //    StartOpenVR();

            openVRController.isLeft = isLeft;
            openVRController.StartComponent(tracker.transform);
        }

        protected void UpdateOpenVR() {
            openVRController.UpdateComponent();

            status = openVRController.status;
            positionConfidence = openVRController.positionConfidence;
            rotationConfidence = openVRController.rotationConfidence;

            primaryAxis = openVRController.joystick;
            secondaryAxis = openVRController.touchpad;
            primaryButton = openVRController.aButton;
            secondaryButton = openVRController.bButton;
            trigger = openVRController.trigger;
            grip = openVRController.grip;
        }
#endif

        #endregion OpenVR

        public void Show(bool showModel) {
            if (model == null)
                return;

            if (!Application.isPlaying)
                model.SetActive(showModel);

            CheckRenderers();
        }

        #endregion
#endif
    }

}