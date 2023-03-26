using System.Collections.Generic;
using UnityEngine;

namespace Passer {

    /// <summary>
    /// Target side
    /// </summary>
    public enum Side {
        AnySide,
        Left,
        Right,
    }

    /// <summary>
    /// A main tracking transform
    /// </summary>
    public abstract class Target : MonoBehaviour {

        [SerializeField]
        protected bool _showRealObjects = true;
        /// <summary>
        /// show the target meshes
        /// </summary>
        public virtual bool showRealObjects {
            get { return _showRealObjects; }
            set { _showRealObjects = value; }
        }

        // These values will not be changed when a animator is used
        public Vector3 savedPosition { get; private set; }
        public Quaternion savedRotation { get; private set; }
        public void SaveTransform() {
            savedPosition = transform.position;
            savedRotation = transform.rotation;
        }

        public virtual void InitComponent() { }

        public abstract void StartTarget();
        public abstract void InitSensors();
        public virtual void StartSensors() { }
        protected virtual void UpdateSensors() { }
        public virtual void StopSensors() { }
        public abstract void UpdateTarget();

        public static List<Collider> SetColliderToTrigger(GameObject obj) {
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
                return SetColliderToTrigger(rb);
            else {
                List<Collider> changedColliders = new List<Collider>();

                Collider[] colliders = obj.GetComponentsInChildren<Collider>();
                for (int j = 0; j < colliders.Length; j++) {
                    if (!colliders[j].isTrigger) {
                        colliders[j].isTrigger = true;
                        changedColliders.Add(colliders[j]);
                    }
                }

                return changedColliders;
            }
        }

        public static List<Collider> SetColliderToTrigger(Rigidbody rb) {
            List<Collider> changedColliders = new List<Collider>();

            Collider[] colliders = rb.GetComponentsInChildren<Collider>();
            for (int j = 0; j < colliders.Length; j++) {
                Rigidbody colliderRigidbody = colliders[j].attachedRigidbody;
                if (colliderRigidbody == null || colliderRigidbody == rb) {
                    if (!colliders[j].isTrigger) {
                        colliders[j].isTrigger = true;
                        changedColliders.Add(colliders[j]);
                    }
                }
            }
            return changedColliders;
        }

        public static void UnsetColliderToTrigger(List<Collider> colliders) {
            if (colliders == null)
                return;

            foreach (Collider c in colliders) {
                if (c != null)
                    c.isTrigger = false;
            }
        }

        public static void UnsetColliderToTrigger(List<Collider> colliders, Collider collider) {
            if (colliders == null || collider == null)
                return;

            if (colliders.Contains(collider))
                collider.isTrigger = false;
        }
    }
}