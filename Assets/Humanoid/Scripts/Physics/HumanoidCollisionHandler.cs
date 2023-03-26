using UnityEngine;

namespace Passer.Humanoid {

    public class HumanoidCollisionHandler : MonoBehaviour {
        public HumanoidControl humanoid;

        protected void FixedUpdate() {
            humanoid.collidedWith = null;
        }

        public void OnCollisionEnter(Collision collision) {
            OnTriggerStay(collision.collider);
        }

        public void OnTriggerEnter(Collider otherCollider) {
            OnTriggerStay(otherCollider);
        }

        public void OnTriggerStay(Collider otherCollider) {
            Rigidbody rigidbody = otherCollider.attachedRigidbody;

            // static colliders
            if (rigidbody == null) {
                humanoid.triggerEntered = true;
                humanoid.collidedWith = otherCollider.gameObject;
            }

            if (!otherCollider.isTrigger && !humanoid.IsMyRigidbody(rigidbody)) {
                //if (!humanoid.collided) {
                    Vector3 worldVelocity = humanoid.headTarget.neck.target.transform.TransformDirection(humanoid.velocity);
                    worldVelocity += humanoid.targetVelocity;
                    humanoid.hitNormal = DetermineHitNormal(worldVelocity);                   
                //}
                humanoid.triggerEntered = true;
                if (rigidbody != null)
                    humanoid.collidedWith = rigidbody.gameObject;
            }
        }

        public void OnTriggerExit() {
            humanoid.triggerEntered = false;
        }

        public void OnCollisionStay(Collision collision) {
            Rigidbody rigidbody = collision.rigidbody;

            // static colliders
            if (rigidbody == null) {
                humanoid.collidedWith = collision.gameObject;
            }
            else if (!humanoid.IsMyRigidbody(rigidbody)) { 
                humanoid.collidedWith = rigidbody.gameObject;
            }
        }

        public void OnCollisionExit(Collision collision) {
            humanoid.triggerEntered = false;
        }

        private Vector3 DetermineHitNormal(Vector3 velocity) {
            //Vector3 normalizedVelocity = new Vector3(velocity.x, 0, velocity.z).normalized;
            Vector3 normalizedVelocity = velocity.normalized;
            if (humanoid.hitNormal != Vector3.zero && Vector3.Angle(normalizedVelocity, humanoid.hitNormal) < 90)
                normalizedVelocity = -humanoid.hitNormal;

            CapsuleCollider cc = humanoid.bodyCapsule;
            Vector3 capsuleCenter = humanoid.hipsTarget.hips.bone.transform.position + cc.center;
            Vector3 capsuleOffset = ((cc.height - cc.radius) / 2) * humanoid.up;

            Vector3 backSweep = normalizedVelocity * (cc.radius + 0.2F);
            Vector3 top = capsuleCenter + capsuleOffset - backSweep;
            Vector3 bottom = capsuleCenter - capsuleOffset - backSweep;

            Vector3 hitNormal;
            if (CapsulecastAllNormal(top, bottom, cc.radius, normalizedVelocity, velocity.magnitude * Time.deltaTime + cc.radius + 0.3F, out hitNormal)) {
                return hitNormal;
            }

            if (Vector3.Angle(humanoid.hitNormal, normalizedVelocity) < 90) {
                return -normalizedVelocity;
            }
            else {
                return humanoid.hitNormal;
            }
        }

        private bool CapsulecastAllNormal(Vector3 top, Vector3 bottom, float radius, Vector3 direction, float maxDistance, out Vector3 hitNormal) {
            RaycastHit[] hits = Physics.CapsuleCastAll(top, bottom, radius, direction, maxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            hitNormal = Vector3.zero;
            for (int i = 0; i < hits.Length; i++) {
                if (!hits[i].collider.isTrigger && hits[i].point.sqrMagnitude > 0) {
                    hitNormal = hits[i].normal;
                    return true;
                }
            }
            return false;
        }

    }
}