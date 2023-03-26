using UnityEngine;

namespace Passer.Tracking {

    /// <summary>
    /// A Unity Tracking Device
    /// </summary>
    public class UnityTrackerComponent : TrackerComponent {

        //protected UnityCameraComponent unityCamera;
        protected UnityControllerComponent leftController;
        protected UnityControllerComponent rightController;

        /// <summary>Create a new Unity Tracker</summary>
        public static UnityTrackerComponent Get(Transform realWorld) {
            Transform trackerTransform = realWorld.Find("Unity");
            if (trackerTransform == null) {
                GameObject trackerObject = new GameObject("Unity");
                trackerTransform = trackerObject.transform;

                trackerTransform.parent = realWorld;
                trackerTransform.localPosition = Vector3.zero;
                trackerTransform.localRotation = Quaternion.identity;
            }

            UnityTrackerComponent tracker = trackerTransform.GetComponent<UnityTrackerComponent>();
            if (tracker == null) {
                tracker = trackerTransform.gameObject.AddComponent<UnityTrackerComponent>();
                tracker.realWorld = realWorld;
            }

            return tracker;
        }

        #region Camera

        protected UnityCameraComponent _unityCamera;
        public UnityCameraComponent unityCamera {
            get {
                if (_unityCamera != null)
                    return _unityCamera;

                _unityCamera = this.GetComponentInChildren<UnityCameraComponent>();
                if (_unityCamera != null)
                    return _unityCamera;

                _unityCamera = FindObjectOfType<UnityCameraComponent>();
                return _unityCamera;
            }
        }

        public UnityCameraComponent GetCamera(Vector3 position, Quaternion rotation) {
#if !pUNITYXR && !UNITY_2020_1_OR_NEWER
            if (unityCamera == null) {
                if (!Application.isPlaying) {
#if UNITY_EDITOR
                    GameObject sensorObj = new GameObject("Camera");
                    Transform sensorTransform = sensorObj.transform;

                    sensorTransform.parent = this.transform;
                    sensorTransform.position = position;
                    sensorTransform.rotation = rotation;

                    _unityCamera = sensorTransform.gameObject.AddComponent<UnityCameraComponent>();
                    _unityCamera.tracker = this;

                    _unityCamera.camera = _unityCamera.GetComponent<Camera>();
                    if (_unityCamera.GetComponent<Camera>() == null) {
                        _unityCamera.camera = _unityCamera.gameObject.AddComponent<Camera>();
                        _unityCamera.GetComponent<Camera>().nearClipPlane = 0.05F;

                        _unityCamera.gameObject.AddComponent<AudioListener>();
                    }
#endif
                }
            }

            return _unityCamera;
#else
            return null;
#endif
        }

        #endregion
    }

}