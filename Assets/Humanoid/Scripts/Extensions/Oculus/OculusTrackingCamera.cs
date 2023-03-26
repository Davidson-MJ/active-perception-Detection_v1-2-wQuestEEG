using UnityEngine;

namespace Passer.Humanoid {
    using Humanoid.Tracking;

    public class OculusTrackingCamera : SubTracker {
#if hOCULUS
        public static OculusTrackingCamera Create(HumanoidTracker tracker) {
            GameObject cameraObject;
            Object cameraPrefab = Resources.Load("Oculus Camera");
            if (cameraPrefab == null)
                cameraObject = new GameObject();            
            else
                cameraObject = (GameObject)Instantiate(cameraPrefab);
            cameraObject.name = "Oculus Camera";
            cameraObject.transform.parent = tracker.trackerTransform;

            OculusTrackingCamera cameraComponent = cameraObject.AddComponent<OculusTrackingCamera>();
            cameraComponent.tracker = tracker;

            return cameraComponent;
        }

        public static int GetCount() {
            int count = 0;

            for (int i = 0; i < (int)OculusDevice.Tracker.Count; ++i) {
                if (OculusDevice.IsPresent(Humanoid.Tracking.Sensor.ID.Tracker1 + i))
                    count++;
            }

            return count;
        }
#endif
        public override bool IsPresent() {
#if hOCULUS
            return OculusDevice.IsPresent(Humanoid.Tracking.Sensor.ID.Tracker1 + subTrackerId);
#else
            return false;
#endif
        }

        public override void UpdateTracker(bool showRealObjects) {
#if hOCULUS
            gameObject.SetActive(showRealObjects && IsPresent());

            Vector3 localPosition = HumanoidTarget.ToVector3(OculusDevice.GetPosition(Humanoid.Tracking.Sensor.ID.Tracker1 + subTrackerId));
            Quaternion localRotation = HumanoidTarget.ToQuaternion(OculusDevice.GetRotation(Humanoid.Tracking.Sensor.ID.Tracker1 + subTrackerId));
            transform.position = tracker.trackerTransform.TransformPoint(localPosition);
            transform.rotation = tracker.trackerTransform.rotation * localRotation;
#endif
        }
    }
}
