using UnityEngine;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#else
using UnityEngine.VR;
#endif

namespace Passer.Tracking {

    /// <summary>
    /// A Unity Controller
    /// </summary>
    public class UnityControllerComponent : SensorComponent {

        public UnityTrackerComponent tracker;
        public GameObject model;

        public bool isLeft;

        public new bool show {
            set {
                if (value && model == null)
                    CreateModel();

                if (model == null)
                    return;

                if (!Application.isPlaying)
                    model.SetActive(value);

                Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
                for (int i = 0; i < renderers.Length; i++) {
                    if (!(renderers[i] is LineRenderer))
                        renderers[i].enabled = value;
                }
            }
        }
        //public void Show(bool showModel) {
        //    if (showModel && model == null)
        //        CreateModel();

        //    if (model == null)
        //        return;

        //    if (!Application.isPlaying)
        //        model.SetActive(showModel);

        //    Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        //    for (int i = 0; i < renderers.Length; i++)
        //        renderers[i].enabled = showModel;
        //}

        protected void CreateModel() {
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

        /// <summary>Create a new Unity Controller</summary>
        public static UnityControllerComponent Get(UnityTrackerComponent tracker, bool isLeft) {
            string name = isLeft ? "Left Controller" : "Right Controller";

            if (tracker == null || tracker.transform == null)
                return null;

            Transform controllerTransform = tracker.transform.Find(name);
            if (controllerTransform == null) {
                GameObject trackerObject = new GameObject(name);
                controllerTransform = trackerObject.transform;

                controllerTransform.parent = tracker.transform;
                controllerTransform.localPosition = Vector3.zero;
                controllerTransform.localRotation = Quaternion.identity;
            }

            UnityControllerComponent unityController = controllerTransform.GetComponent<UnityControllerComponent>();
            if (unityController == null) {
                unityController = controllerTransform.gameObject.AddComponent<UnityControllerComponent>();
                unityController.tracker = tracker;
                unityController.isLeft = isLeft;
            }

            return unityController;
        }

        override protected void Start() {
            if (model != null) {
                // Replace the editor-time model with the runtime model
                Destroy(model);
                CreateModel();
            }
        }

        public override void UpdateComponent() {
            base.UpdateComponent();
#if !pUNITYXR && !UNITY_2020_1_OR_NEWER
#if UNITY_2017_2_OR_NEWER
            if (!XRSettings.enabled)
                return;
            XRNode centerEye = XRNode.CenterEye;
            XRNode hand = isLeft ? XRNode.LeftHand : XRNode.RightHand;
#else
            if (!VRSettings.enabled)
                return;

            VRNode centerEye = VRNode.CenterEye;
            VRNode hand = isLeft ? VRNode.LeftHand : VRNode.RightHand;
#endif

            Quaternion localCameraRotation = InputTracking.GetLocalRotation(centerEye);
            Quaternion trackingSpaceRotation = tracker.unityCamera.transform.rotation * Quaternion.Inverse(localCameraRotation);

            Vector3 localCameraPosition = InputTracking.GetLocalPosition(centerEye);
            Vector3 trackingSpacePosition = tracker.unityCamera.transform.position - localCameraPosition;


            Quaternion localControllerRotation = InputTracking.GetLocalRotation(hand);
            Vector3 localControllerPosition = InputTracking.GetLocalPosition(hand);

            transform.rotation = trackingSpaceRotation * localControllerRotation;
            transform.position = trackingSpacePosition + trackingSpaceRotation * localControllerPosition;
#endif
        }
    }

}