using UnityEngine;

namespace Passer {

    public class RigidbodyData {
        public float mass = 1;
        public float drag;
        public float angularDrag = 0.05F;
        public bool useGravity = true;
        public bool isKinematic;
        public RigidbodyInterpolation interpolation = RigidbodyInterpolation.None;
        public CollisionDetectionMode collisionDetectionMode = CollisionDetectionMode.Discrete;
        public RigidbodyConstraints constraints = RigidbodyConstraints.None;
        public Transform parent;

        public RigidbodyData(Rigidbody rb) {
            mass = rb.mass;
            drag = rb.drag;
            angularDrag = rb.angularDrag;
            useGravity = rb.useGravity;
            isKinematic = rb.isKinematic;
            interpolation = rb.interpolation;
            collisionDetectionMode = rb.collisionDetectionMode;
            constraints = rb.constraints;

            parent = rb.transform.parent;
        }

        public void CopyToRigidbody(Rigidbody rb) {
            rb.mass = mass;
            rb.drag = drag;
            rb.angularDrag = angularDrag;
            rb.useGravity = useGravity;
            rb.isKinematic = isKinematic;
            rb.interpolation = interpolation;
            rb.collisionDetectionMode = collisionDetectionMode;
            rb.constraints = constraints;

            rb.transform.parent = parent;
        }

        public static RigidbodyData ParentRigidbody(Transform parentTransform, Rigidbody childRigidbody) {
            RigidbodyData rigidbodyData = new RigidbodyData(childRigidbody);

            childRigidbody.transform.parent = parentTransform;

            if (Application.isPlaying)
                Object.Destroy(childRigidbody);
            else
                Object.DestroyImmediate(childRigidbody, true);

            return rigidbodyData;
        }

        public static RigidbodyData ParentRigidbody(Rigidbody parentRigidbody, Rigidbody childRigidbody) {
            RigidbodyData rigidbodyData = new RigidbodyData(childRigidbody);

            childRigidbody.transform.parent = parentRigidbody.transform;
            parentRigidbody.mass += childRigidbody.mass;

            if (Application.isPlaying)
                Object.Destroy(childRigidbody);
            else
                Object.DestroyImmediate(childRigidbody, true);

            return rigidbodyData;
        }

        public Rigidbody UnparentRigidbody(Transform parentTransform, Transform childRigidbodyTransform) {
            Rigidbody childRigidbody = childRigidbodyTransform.GetComponent<Rigidbody>();
            if (!childRigidbodyTransform.gameObject.isStatic && childRigidbody == null) {
                childRigidbody = childRigidbodyTransform.gameObject.AddComponent<Rigidbody>();
                CopyToRigidbody(childRigidbody);
            }
            childRigidbody.transform.parent = null;

            // To do: copy velocity of parent to child
            return childRigidbody;
        }

        public Rigidbody UnparentRigidbody(Rigidbody parentRigidbody, Transform childRigidbodyTransform) {
            Rigidbody childRigidbody = childRigidbodyTransform.GetComponent<Rigidbody>();
            if (!childRigidbodyTransform.gameObject.isStatic && childRigidbody == null) {
                childRigidbody = childRigidbodyTransform.gameObject.AddComponent<Rigidbody>();
                CopyToRigidbody(childRigidbody);
            }
            parentRigidbody.mass -= childRigidbody.mass;

            // To do: copy velocity of parent to child
            return childRigidbody;
        }
    }

}