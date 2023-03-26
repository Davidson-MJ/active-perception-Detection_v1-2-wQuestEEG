using System.Collections.Generic;
using UnityEngine;

namespace Passer {
    public class Tracker {
        public enum Status {
            Unavailable,
            Present,
            Tracking
        }

        public const string _name = "";
        public virtual string name { get { return _name; } }

        public bool enabled;
        public Status status;

        protected GameObject realWorld;
        public Transform trackerTransform;

        #region Start
        public static GameObject GetRealWorld(Transform transform) {
            Transform realWorldTransform = transform.Find("Real World");
            if (realWorldTransform != null)
                return realWorldTransform.gameObject;

            GameObject realWorld = new GameObject("Real World");
            realWorld.transform.parent = transform;
            realWorld.transform.localPosition = Vector3.zero;
            realWorld.transform.localRotation = Quaternion.identity;
            return realWorld;
        }

        public static Transform FindTrackerObject(GameObject realWorld, string trackerName) {
            Transform[] ancestors = realWorld.GetComponentsInChildren<Transform>();
            for (int i = 0; i < ancestors.Length; i++) {
                if (ancestors[i].name == trackerName)
                    return ancestors[i].transform;
            }
            return null;
        }

        public virtual bool AddTracker(Transform rootTransform, string resourceName) {
            GameObject realWorld = GetRealWorld(rootTransform);

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
                trackerTransform.position = rootTransform.position;
                trackerTransform.rotation = rootTransform.rotation;
                return true;
            }
            return false;
        }

        public virtual void StartTracker(Transform rootTransform) {
            realWorld = GetRealWorld(rootTransform);
        }

        #endregion

        #region Update
        /// <summary>Update the tracker state</summary>
        public virtual void UpdateTracker() { }
        #endregion

        #region Calibration
        public virtual void Calibrate() { }

        public virtual void AdjustTracking(Vector3 v, Quaternion q) {
            if (trackerTransform != null) {
                trackerTransform.position += v;
                trackerTransform.rotation *= q;
            }
        }
        #endregion
    }
}