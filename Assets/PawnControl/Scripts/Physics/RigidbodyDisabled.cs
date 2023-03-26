using UnityEngine;
namespace Passer {

    public class RigidbodyDisabled : MonoBehaviour {
        public float mass = 1;
        public float drag;
        public float angularDrag = 0.05F;
        public bool useGravity = true;
        public bool isKinematic;
        public RigidbodyInterpolation interpolation = RigidbodyInterpolation.None;
        public CollisionDetectionMode collisionDetectionMode = CollisionDetectionMode.Discrete;
        public RigidbodyConstraints constraints = RigidbodyConstraints.None;
        public Transform parent;
        public Vector3 localScale;
        public bool markedForDestruction = false;

        public static RigidbodyDisabled Get(Transform transform) {
            RigidbodyDisabled rigidbodyDisabled = transform.GetComponent<RigidbodyDisabled>();
            if (rigidbodyDisabled != null && rigidbodyDisabled.markedForDestruction)
                return null;
            return rigidbodyDisabled;
        }

        public static RigidbodyDisabled GetInParent(Transform transform) {
            RigidbodyDisabled rigidbodyDisabled = transform.GetComponentInParent<RigidbodyDisabled>();
            if (rigidbodyDisabled != null && rigidbodyDisabled.markedForDestruction)
                return null;
            return rigidbodyDisabled;
        }

        public void CopyFromRigidbody(Rigidbody rb) {
            mass = rb.mass;
            drag = rb.drag;
            angularDrag = rb.angularDrag;
            useGravity = rb.useGravity;
            isKinematic = rb.isKinematic;
            interpolation = rb.interpolation;
            collisionDetectionMode = rb.collisionDetectionMode;
            constraints = rb.constraints;

            parent = rb.transform.parent;
            localScale = rb.transform.localScale;
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
            rb.transform.localScale = localScale;
        }

        public static RigidbodyDisabled DisableRigidbody(Rigidbody rigidbody) {
            //Debug.Log("Disable Rigidbody: " + rigidbody + " " + Time.time);

            RigidbodyDisabled disabledRigidbody = rigidbody.GetComponent<RigidbodyDisabled>();
            if (disabledRigidbody == null)
                disabledRigidbody = rigidbody.gameObject.AddComponent<RigidbodyDisabled>();
            //else
            //    Debug.Log(rigidbody + " DisableRigidbody already has DisabledRigidbody");
            disabledRigidbody.CopyFromRigidbody(rigidbody);

            if (Application.isPlaying) {
                // don't do this directly, because it could be enabled again in the same frame
                //Debug.Log(rigidbody + " = UNmarked for Destruction");
                disabledRigidbody.markedForDestruction = false;
            }
            else
                DestroyImmediate(rigidbody, true);

            return disabledRigidbody;
        }

        public static Rigidbody EnableRigidbody(Transform rigidbodyTransform) {
            //Debug.Log("Enable Rigidbody: " + rigidbodyTransform + " " + Time.time);
            Rigidbody rigidbody = rigidbodyTransform.GetComponent<Rigidbody>();

            RigidbodyDisabled disabledRigidbody = Get(rigidbodyTransform);
            if (disabledRigidbody != null) {
                if (rigidbodyTransform.gameObject.isStatic) {
                    Debug.LogError("rigidbody " + rigidbodyTransform + " is static");
                    return rigidbody;
                }
                else if (rigidbody == null) {
                    rigidbody = rigidbodyTransform.gameObject.AddComponent<Rigidbody>();
                }
                else
                    Debug.Log(rigidbody + " already exists");
                disabledRigidbody.CopyToRigidbody(rigidbody);
            }
            else if (rigidbody != null) {
                Debug.Log(rigidbody + " already exists");
                return rigidbody;
            }
            else {
                Debug.LogError("No Disabled Rigidbody");
            }

            if (Application.isPlaying) {
                // don't do this directly, because it could be enabled again in the same frame
                disabledRigidbody.markedForDestruction = true;
            }
            else
                DestroyImmediate(disabledRigidbody, true);

            return rigidbody;
        }


        public static void ParentRigidbody(Transform parentTransform, Rigidbody childRigidbody) {
            Transform childTransform = childRigidbody.transform;
            DisableRigidbody(childRigidbody);
            childTransform.parent = parentTransform;
        }

        /// <summary>
        /// Parents a Child Rigidbody to the Parent. The Child Rigidbody gets disabled
        /// </summary>
        /// <param name="parentRigidbody"></param>
        /// <param name="childRigidbody"></param>
        public static void ParentRigidbody(Rigidbody parentRigidbody, Rigidbody childRigidbody) {
            //parentRigidbody.mass += childRigidbody.mass;

            Transform childTransform = childRigidbody.transform;
            DisableRigidbody(childRigidbody);
            childTransform.parent = parentRigidbody.transform;
        }

        public static Rigidbody UnparentRigidbody(Transform parentTransform, Transform childRigidbodyTransform) {
            Rigidbody childRigidbody = EnableRigidbody(childRigidbodyTransform);

            // To do: copy velocity of parent to child
            return childRigidbody;
        }

        public static Rigidbody UnparentRigidbody(Rigidbody parentRigidbody, Transform childRigidbodyTransform) {
            Rigidbody childRigidbody = EnableRigidbody(childRigidbodyTransform);
            //parentRigidbody.mass -= childRigidbody.mass;

            // Adjust local scale to precise 1,1,1 to compensate for calculation errors
            float distance = Vector3.Distance(childRigidbodyTransform.localScale, Vector3.one);
            if (distance > 0) {
                if (distance > 0 && distance < 0.01F) {
                    childRigidbodyTransform.localScale = Vector3.one;
                }
            }

            // To do: copy velocity of parent to child
            return childRigidbody;
        }

        private void LateUpdate() {
            if (!markedForDestruction) {
                Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
                if (rigidbody == null)
                    return;

                //Debug.Log(this + " Rigidbody destruct in Update " + Time.time);
                Destroy(rigidbody);
            }
            else {
                //Debug.Log(this + " RigidbodyDisabled destruct in Update " + Time.time);
                Destroy(this);
            }
        }
    }
}