
using UnityEngine;
using Passer.Humanoid.Tracking;

namespace Passer.Tracking {

    /// <summary>
    /// The OpenVR tracking device
    /// </summary>
    public class OpenVR : TrackerComponent {

#if hOPENVR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)

        #region Manage

        /// <summary>
        /// Find an OpenVR tracker
        /// </summary>
        /// <param name="parentTransform">The parent transform of the tracker</param>
        /// <returns>The tracker</returns>
        public static OpenVR Find(Transform parentTransform) {
            OpenVR openVR = parentTransform.GetComponentInChildren<OpenVR>();
            if (openVR != null)
                return openVR;

            return null;
        }

        /// <summary>
        /// Find or create a new OpenVR tracker
        /// </summary>
        /// <param name="parentTransform">The parent transform for the tracker</param>
        /// <param name="position">The world position of the tracker</param>
        /// <param name="rotation">The world rotation of the tracker</param>
        /// <returns>The tracker</returns>
        public static OpenVR Get(Transform parentTransform, Vector3 position, Quaternion rotation) {
            OpenVR openVR = Find(parentTransform);
            if (openVR != null)
                return openVR;

            if (Application.isPlaying) {
                Debug.LogError("OpenVR is missing");
                return null;
            }
#if UNITY_EDITOR
            GameObject trackerObj = new GameObject("OpenVR");
            Transform trackerTransform = trackerObj.transform;

            trackerTransform.parent = parentTransform;
            trackerTransform.position = position;
            trackerTransform.rotation = rotation;

            openVR = trackerObj.AddComponent<OpenVR>();
            openVR.realWorld = parentTransform;
#endif
            return openVR;
        }

        #region Hmd

        /// <summary>
        /// Find an OpenVR Hmd
        /// </summary>
        /// <returns>The hmd of null when it has not been found</returns>
        public OpenVRHmd FindHmd() {
            OpenVRHmd hmd = GetComponentInChildren<OpenVRHmd>();
            return hmd;
        }

        /// <summary>
        /// Find or Create a new OpenVR Hmd
        /// </summary>
        /// <param name="position">The world position of the hmd</param>
        /// <param name="rotation">The world rotation of the hmd</param>
        /// <returns>The hmd</returns>
        public OpenVRHmd GetHmd(Vector3 position, Quaternion rotation) {
            OpenVRHmd hmd = FindHmd();
            if (hmd != null)
                return hmd;

            if (Application.isPlaying) {
                Debug.LogError("OpenVR HMD is missing");
                return null;
            }

#if UNITY_EDITOR
            GameObject hmdObject = new GameObject("OpenVR Hmd");
            Transform hmdTransform = hmdObject.transform;

            hmdTransform.parent = this.transform;
            hmdTransform.position = position;
            hmdTransform.rotation = rotation;

            hmd = hmdObject.AddComponent<OpenVRHmd>();
            //hmd.openVR = this;
#endif
            return hmd;
        }

        #endregion Hmd

        #region Controller

        /// <summary>
        /// Find an OpenVR Controller
        /// </summary>
        /// <param name="isLeft">Looking for the left-handed controller?</param>
        /// <returns>The controller or null when it has not been found</returns>
        public OpenVRController FindController(bool isLeft) {
            OpenVRController[] controllers = GetComponentsInChildren<OpenVRController>();
            foreach (OpenVRController controller in controllers) {
                if (controller.isLeft == isLeft)
                    return controller;
            }
            return null;
        }

        public OpenVRController GetController(Vector3 position, Quaternion rotation, bool isLeft) {
            OpenVRController controller = FindController(isLeft);
            if (controller != null)
                return controller;

            if (Application.isPlaying) {
                Debug.LogError("OpenVR Controller is missing");
                return null;
            }

#if UNITY_EDITOR
            GameObject controllerObject = new GameObject("OpenVR Controller");
            Transform controllerTransform = controllerObject.transform;

            controllerTransform.parent = this.transform;
            controllerTransform.position = position;
            controllerTransform.rotation = rotation;

            controller = controllerObject.AddComponent<OpenVRController>();
            controller.isLeft = isLeft;
            //controller.openVR = this;

            // To do: add controller model
            string prefabLeftName = "Vive Controller";
            string prefabRightName = "Vive Controller";
            string resourceName = isLeft ? prefabLeftName : prefabRightName;
            //switch (controllerType) {
            //    case OpenVRController.ControllerType.ValveIndex:
            //        break;
            //    case OpenVRController.ControllerType.MixedReality:
            //        prefabLeftName = "Windows MR Controller Left";
            //        prefabRightName = "Windows MR Controller Right";
            //        break;
            //    case OpenVRController.ControllerType.OculusTouch:
            //        prefabLeftName = "Left Touch Controller";
            //        prefabRightName = "Right Touch Controller";
            //        break;
            //    case OpenVRController.ControllerType.SteamVRController:
            //    default:
            //        break;
            //}
            Object controllerPrefab = Resources.Load(resourceName);
            GameObject sensorObject;
            if (controllerPrefab == null)
                sensorObject = new GameObject("Sensor");
            else
                sensorObject = (GameObject)Object.Instantiate(controllerPrefab);

            sensorObject.transform.parent = controllerTransform;
            sensorObject.transform.localPosition = Vector3.zero;
            sensorObject.transform.localRotation = Quaternion.identity;

#endif
            return controller;
        }

        #endregion Controller

        #region Skeleton

        /// <summary>
        /// Find an Hand Skeleton
        /// </summary>
        /// <param name="isLeft">Looking for the left-handed skeleton?</param>
        /// <returns>The hand skeleton or null when it has not been found</returns>
        public HandSkeleton FindSkeleton(bool isLeft) {
            HandSkeleton[] handSkeletons = GetComponentsInChildren<HandSkeleton>();
            foreach (HandSkeleton handSkeleton in handSkeletons) {
                if (handSkeleton.isLeft == isLeft)
                    return handSkeleton;
            }
            return null;
        }

        public HandSkeleton GetSkeleton(Vector3 position, Quaternion rotation, bool isLeft) {
            HandSkeleton skeleton = FindSkeleton(isLeft);
            if (skeleton != null)
                return skeleton;

            if (Application.isPlaying) {
                Debug.LogError("Hand Skeleton is missing");
                return null;
            }
#if UNITY_EDITOR
            GameObject skeletonObj = new GameObject(isLeft ? "Left Hand Skeleton" : "Right Hand Skeleton");
            skeletonObj.transform.parent = this.transform;
            skeletonObj.transform.localPosition = position;
            skeletonObj.transform.localRotation = rotation;

            skeleton = skeletonObj.AddComponent<Humanoid.OpenVRHandSkeleton>();
            skeleton.isLeft = isLeft;
#endif
            return skeleton;
        }

#if hVIVEHAND
        public static HandSkeleton FindHandTrackingSkeleton(TrackerComponent tracker, bool isLeft) {
            HandSkeleton[] handSkeletons = tracker.transform.GetComponentsInChildren<ViveHandSkeleton>();
            foreach (HandSkeleton handSkeleton in handSkeletons) {
                if (handSkeleton.isLeft == isLeft)
                    return handSkeleton;
            }
            return null;
        }

        public static HandSkeleton GetHandTrackingSkeleton(TrackerComponent tracker, Vector3 position, Quaternion rotation, bool isLeft) {
            HandSkeleton skeleton = FindHandTrackingSkeleton(tracker, isLeft);
            if (skeleton != null)
                return skeleton;

#if UNITY_EDITOR
            GameObject skeletonObj = new GameObject(isLeft ? "Left Hand Tracking Skeleton" : "Right Hand Tracking Skeleton");
            skeletonObj.transform.parent = tracker.transform.transform;
            skeletonObj.transform.localPosition = position;
            skeletonObj.transform.localRotation = rotation;

            skeleton = skeletonObj.AddComponent<ViveHandSkeleton>();
            skeleton.isLeft = isLeft;
#endif
            return skeleton;
        }
#endif
        public HandSkeleton leftHandSkeleton {
            get { return FindSkeleton(true); }
        }
        public HandSkeleton rightHandSkeleton {
            get { return FindSkeleton(false); }
        }

        public override void ShowSkeleton(bool shown) {
            if (leftHandSkeleton != null)
                leftHandSkeleton.show = shown;
            if (rightHandSkeleton != null)
                rightHandSkeleton.show = shown;
        }

        #endregion

        #endregion Manage

#endif
    }
}