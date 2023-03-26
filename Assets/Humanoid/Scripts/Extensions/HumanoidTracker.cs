using System.Collections.Generic;
using UnityEngine;

namespace Passer.Humanoid {
    using Tracking;

    /// <summary>A tracker</summary>
    public class HumanoidTracker : Tracker {
        public HumanoidControl humanoid;
        public System.IntPtr trackerDevice;

        #region Manage

        public virtual void CheckTracker(HumanoidControl humanoid) {
            if (this.humanoid == null)
                this.humanoid = humanoid;
        }

        #endregion Manage

        #region Device
        public virtual Vector3 GetBonePosition(uint actorId, Bone boneId) { return Vector3.zero; }
        public virtual Quaternion GetBoneRotation(uint actorId, Bone boneId) { return Quaternion.identity; }
        public virtual float GetBoneConfidence(uint actorId, Bone boneId) { return 0; }

#if hFACE
        public virtual Vector3 GetBonePosition(uint actorId, FacialBone boneId) { return Vector3.zero; }
        public virtual Quaternion GetBoneRotation(uint actorId, FacialBone boneId) { return Quaternion.identity; }
        public virtual float GetBoneConfidence(uint actorId, FacialBone boneId) { return 0; }
#endif
        #endregion

        public DeviceView deviceView = new DeviceView();

        public List<SubTracker> subTrackers = new List<SubTracker>();

        public virtual void Enable() {
            enabled = true;
        }

        public virtual HeadSensor headSensor {
            get { return null; }
        }
        public virtual ArmSensor leftHandSensor {
            get { return null; }
        }
        public virtual ArmSensor rightHandSensor {
            get { return null; }
        }
        public virtual TorsoSensor hipsSensor {
            get { return null; }
        }
        public virtual LegSensor leftFootSensor {
            get { return null; }
        }
        public virtual LegSensor rightFootSensor {
            get { return null; }
        }
        private UnitySensor[] _sensors = new UnitySensor[0];
        public virtual UnitySensor[] sensors {
            get { return _sensors; }
        }

        #region Manage

        public virtual bool AddTracker(HumanoidControl humanoid, string resourceName) {
            GameObject realWorld = HumanoidControl.GetRealWorld(humanoid.transform);

            trackerTransform = FindTrackerObject(realWorld, name);
            if (trackerTransform == null) {
                GameObject model = Resources.Load(resourceName) as GameObject;

                if (model != null) {
                    GameObject trackerObject = GameObject.Instantiate(model);
                    trackerObject.name = name;
                    trackerTransform = trackerObject.transform;
                }
                else {
                    GameObject trackerObject = new GameObject(name);
                    trackerTransform = trackerObject.transform;
                }
                trackerTransform.parent = realWorld.transform;
                trackerTransform.position = humanoid.transform.position;
                trackerTransform.rotation = humanoid.transform.rotation;
                return true;
            }
            return false;
        }

        public virtual bool AddTracker(GameObject realWorld, string resourceName) {
            trackerTransform = FindTrackerObject(realWorld, name);
            if (trackerTransform == null) {
                GameObject model = Resources.Load(resourceName) as GameObject;

                if (model != null) {
                    GameObject trackerObject = GameObject.Instantiate(model);
                    trackerObject.name = name;
                    trackerTransform = trackerObject.transform;
                }
                else {
                    GameObject trackerObject = new GameObject(name);
                    trackerTransform = trackerObject.transform;
                }
                trackerTransform.parent = realWorld.transform;
                trackerTransform.localPosition = Vector3.zero;
                trackerTransform.localRotation = Quaternion.identity;
                return true;
            }
            return false;
        }

        #endregion Manage

        public virtual void ShowTracker(bool shown) {
            if (trackerTransform != null)
                ShowTracker(trackerTransform.gameObject, shown);
        }

        public static void ShowTracker(GameObject trackerObject, bool enabled) {
            if (trackerObject == null)
                return;

            Renderer[] renderers = trackerObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers) {
                if (!(renderer is LineRenderer))
                    renderer.enabled = enabled;
            }
        }

        public virtual void ShowSkeleton(bool shown) {

        }

        #region Start
        /// <summary>Start the tracker</summary>
        public virtual void StartTracker(HumanoidControl _humanoid) {
            humanoid = _humanoid;

            // Real World should not be used as tracker transform anymore!
            //GameObject realWorld = HumanoidControl.GetRealWorld(humanoid.transform);
            //Init(realWorld.transform);
        }

        public void Init(Transform _trackerTransform) {
            trackerTransform = _trackerTransform;
        }

        public override void StartTracker(Transform trackerTransform) {
            // Real World should not be used as tracker transform anymore!
            //GameObject realWorld = HumanoidControl.GetRealWorld(trackerTransform);
            //Init(realWorld.transform);
        }
        #endregion

        public virtual void UpdateSubTracker(int i) {
            if (subTrackers[i] != null)
                subTrackers[i].UpdateTracker(humanoid.showRealObjects);
        }

        protected virtual Vector3 GetSubTrackerPosition(int i) {
            return Vector3.zero;
        }

        protected virtual Quaternion GetSubTrackerRotation(int i) {
            return Quaternion.identity;
        }

        public virtual void StopTracker() { }

        public Vector3 ToWorldPosition(Vector3 localPosition) {
            return trackerTransform.transform.position + trackerTransform.transform.rotation * localPosition;
        }

        public Quaternion ToWorldOrientation(Quaternion localRotation) {
            return trackerTransform.transform.rotation * localRotation;
        }
    }

    public abstract class SubTracker : MonoBehaviour {
        public HumanoidTracker tracker;
        public int subTrackerId = -1;

        public abstract bool IsPresent();
        public abstract void UpdateTracker(bool showRealObjects);
    }
}
