#if hOPENVR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
using UnityEngine;

namespace Passer.Humanoid {
    using Tracking;

    /// <summary>
    /// An OpenVR subtracker
    /// </summary>
    public class OpenVRBaseStation : SubTracker {
        protected const string resourceName = "Lighthouse";

        public static OpenVRBaseStation Create(HumanoidTracker tracker) {
            Object lighthousePrefab = Resources.Load(resourceName);
            GameObject lighthouseObject = (lighthousePrefab == null) ? new GameObject(resourceName) : (GameObject)Instantiate(lighthousePrefab);

            lighthouseObject.name = "Lighthouse";
            lighthouseObject.transform.parent = tracker.trackerTransform;

            lighthouseObject.SetActive(false);

            OpenVRBaseStation subTracker = lighthouseObject.AddComponent<OpenVRBaseStation>();
            subTracker.tracker = tracker;

            return subTracker;
        }

        public override bool IsPresent() {
            bool isPresent = OpenVRDevice.IsPresent(subTrackerId);
            return isPresent;
        }

        public override void UpdateTracker(bool showRealObjects) {
            if (subTrackerId == -1)
                return;

            bool isPresent = IsPresent();
            if (!isPresent)
                return;

            if (this.gameObject == null)
                return;

            gameObject.SetActive(showRealObjects);

            transform.localPosition = HumanoidTarget.ToVector3(OpenVRDevice.GetPosition(subTrackerId));
            transform.localRotation = HumanoidTarget.ToQuaternion(OpenVRDevice.GetRotation(subTrackerId));
        }
    }
}
#endif