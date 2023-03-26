using UnityEngine;
using Passer.Humanoid;

namespace Passer.Tracking {

    /// <summary>
    /// The Oculus Device
    /// </summary>
    public class OculusTracker : TrackerComponent {
#if hOCULUS
        public bool persistentTracking;
        public RealWorldConfiguration realWorldConfiguration;


        #region Manage
        /*        
                /// <summary>
                /// Find an Oculus Tracker
                /// </summary>
                /// <param name="parentTransform"></param>
                /// <returns></returns>
                public static OculusTracker Find(Transform parentTransform) {
                    OculusTracker oculus = parentTransform.GetComponentInChildren<OculusTracker>();
                    if (oculus != null)
                        return oculus;

                    oculus = FindObjectOfType<OculusTracker>();
                    return oculus;
                }

                /// <summary>
                /// Find or create a new Oculus Tracker
                /// </summary>
                /// <param name="parentTransform"></param>
                /// <param name="position"></param>
                /// <param name="rotation"></param>
                /// <returns></returns>
                public static OculusTracker Get(Transform parentTransform, Vector3 position, Quaternion rotation) {
                    OculusTracker oculus = Find(parentTransform);
                    if (oculus != null)
                        return oculus;

                    if (Application.isPlaying) {
                        Debug.LogError("Oculus is missing");
                        return null;
                    }
        #if UNITY_EDITOR
                    GameObject trackerObj = new GameObject("Oculus");
                    Transform trackerTransform = trackerObj.transform;

                    trackerTransform.parent = parentTransform;
                    trackerTransform.position = position;
                    trackerTransform.rotation = rotation;

                    oculus = trackerTransform.gameObject.AddComponent<OculusTracker>();
                    oculus.realWorld = parentTransform;

        #endif
                    return oculus;
                }
    */
#if hOCHAND
        public HandSkeleton FindHandTrackingSkeleton(bool isLeft) {
            HandSkeleton[] handSkeletons = GetComponentsInChildren<OculusHandSkeleton>();
            foreach (HandSkeleton handSkeleton in handSkeletons) {
                if (handSkeleton.isLeft == isLeft)
                    return handSkeleton;
            }
            return null;
        }


        public HandSkeleton leftHandSkeleton {
            get { return FindHandTrackingSkeleton(true); }
        }
        public HandSkeleton rightHandSkeleton {
            get { return FindHandTrackingSkeleton(false); }
        }
#endif
        public override void ShowSkeleton(bool shown) {
#if hOCHAND
            if (leftHandSkeleton != null)
                leftHandSkeleton.show = shown;
            if (rightHandSkeleton != null)
                rightHandSkeleton.show = shown;
#endif
        }


        #endregion

        #region Start
        /*
                protected override void Start() {
                    if (!persistentTracking)
                        transform.localPosition = new Vector3(0, Humanoid.Tracking.OculusDevice.eyeHeight, 0);
                }

                protected virtual void OnEnable() {
                    if (!persistentTracking)
                        return;

                    if (realWorldConfiguration == null) {
                        Debug.LogError("Could not find Real World Configuration");
                        return;
                    }

                    RealWorldConfiguration.TrackingSpace trackingSpace =
                        realWorldConfiguration.trackers.Find(space => space.trackerId == TrackerId.Oculus);

                    if (trackingSpace == null)
                        return;

                    transform.position = trackingSpace.position;
                    transform.rotation = trackingSpace.rotation;
                }

                protected virtual void OnDestroy() {
                    if (!persistentTracking)
                        return;

                    if (realWorldConfiguration == null) {
                        Debug.LogError("Could not find Real World Configuration");
                        return;
                    }

                    RealWorldConfiguration.TrackingSpace trackingSpace =
                        realWorldConfiguration.trackers.Find(space => space.trackerId == TrackerId.Oculus);

                    if (trackingSpace == null) {
                        trackingSpace = new RealWorldConfiguration.TrackingSpace();
                        realWorldConfiguration.trackers.Add(trackingSpace);
                    }
                    trackingSpace.position = transform.position;
                    trackingSpace.rotation = transform.rotation;
                }
                */

        #endregion

#endif
    }

}