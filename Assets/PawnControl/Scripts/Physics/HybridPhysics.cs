#define DEBUG_TORQUE

using System.Collections.Generic;
using UnityEngine;

namespace Passer {
    using Pawn;

    public class HybridPhysics : MonoBehaviour {

        public Transform target;

        protected Rigidbody thisRigidbody;
        protected List<Collider> colliders;


        public enum PhysicsMode {
            Kinematic,
            NonKinematic,
            HybridKinematic,
            //ForceLess,
        }
        public PhysicsMode mode = PhysicsMode.HybridKinematic;
        public static float kinematicMass = 1; // masses < kinematicMass will move kinematic when not colliding

        public float strength = 100;

        protected bool colliding;
        protected bool hasCollided = false;

        protected Vector3 force;
        protected Vector3 torque;

        #region Start

        virtual protected void Awake() {
            thisRigidbody = GetComponent<Rigidbody>();
            if (thisRigidbody == null)
                return;

            if (thisRigidbody.useGravity || mode == PhysicsMode.NonKinematic)
                SetNonKinematic();
            else if (mode == PhysicsMode.HybridKinematic)
                SetHybridKinematic();
            else
                SetKinematic();
        }

        #endregion

        #region Update

        virtual protected void FixedUpdate() {
            if (thisRigidbody == null)
                UpdateWithoutRigidbody();
            else
                UpdateRigidbody();

            colliding = false;
        }

        virtual protected void UpdateWithoutRigidbody() {
            thisRigidbody = GetComponent<Rigidbody>();
            if (thisRigidbody != null)
                return;

            Rigidbody grabbedRigidbody = GetComponentInParent<Rigidbody>();
            if (grabbedRigidbody == null)
                return;

            KinematicLimitations grabbedKinematicLimitations = grabbedRigidbody.GetComponent<KinematicLimitations>();
            if (grabbedKinematicLimitations == null)
                return;

            Vector3 locationDifference = target.transform.position - this.transform.position;
            grabbedKinematicLimitations.transform.position += locationDifference;

            Vector3 correctionVector = grabbedKinematicLimitations.GetCorrectionVector();
            grabbedKinematicLimitations.transform.position += correctionVector;
        }

        virtual protected void UpdateRigidbody() {
            if (target == null)
                return;

            if (thisRigidbody.isKinematic)
                UpdateKinematicRigidbody();
            else
                UpdateNonKinematicRigidbody();
        }

        virtual protected void UpdateKinematicRigidbody() {
            if (mode == PhysicsMode.NonKinematic || 
                thisRigidbody.mass > kinematicMass ||
                thisRigidbody.GetComponent<Joint>() != null
                ) {

                SetNonKinematic();
                return;
            }

            force = Vector3.zero;
            torque = Vector3.zero;

            thisRigidbody.MovePosition(target.position);
            thisRigidbody.MoveRotation(target.rotation);
        }

        virtual protected void UpdateNonKinematicRigidbody() {
            if (mode == PhysicsMode.Kinematic) {
                SetKinematic();
                return;
            }

            torque = CalculateTorque();
            ApplyTorqueAtPosition(torque, transform.position);

            //Vector3 wristTorque = CalculateWristTorque();
            //ApplyTorqueAtPosition(wristTorque, transform.position);

            force = CalculateForce();
            ApplyForceAtPosition(force, transform.position);

            if (!hasCollided &&
                !thisRigidbody.useGravity &&
                thisRigidbody.mass <= kinematicMass &&
                mode != PhysicsMode.NonKinematic &&
                thisRigidbody.GetComponent<Joint>() == null
                ) {

                SetHybridKinematic();
            }
        }

        #endregion

        #region Events

        virtual public void OnTriggerEnter(Collider collider) {
            bool otherIsPawn = false;

            Rigidbody otherRigidbody = collider.attachedRigidbody;
            if (otherRigidbody != null) {
                PawnControl pawn = otherRigidbody.GetComponent<PawnControl>();
                otherIsPawn = (pawn != null);
            }

            if (thisRigidbody != null &&
                thisRigidbody.isKinematic &&
                !collider.isTrigger &&
                !otherIsPawn) {

                colliding = true;
                hasCollided = true;
                //Debug.Log("Collided with " + collider);

                if (otherRigidbody != null)
                    SetNonKinematic();
                else
                    SetNonKinematic();
            }
        }

        virtual public void OnCollisionEnter(Collision collision) {
            colliding = true;
        }

        virtual public void OnCollisionStay(Collision collision) {
            // Make sure the collision is not with kinematic child rigidbodies
            if (collision.rigidbody != null) {
                Rigidbody parentRigidbody = collision.rigidbody.transform.parent.GetComponentInParent<Rigidbody>();
                if (parentRigidbody == thisRigidbody) {
                    // we are colliding with a kinematic child rigidbody
                    colliding = false;
                    return;
                }
            }
            colliding = true;
        }

        virtual public void OnCollisionExit(Collision collision) {
            if (thisRigidbody != null) {
                // The sweeptests fail quite often...
                //RaycastHit hit;
                //if (!thisRigidbody.SweepTest(target.transform.position - thisRigidbody.position, out hit))
                    hasCollided = false;

            }
        }

        #endregion

        #region Force

        virtual protected Vector3 CalculateForce() {
            if (target == null)
                return Vector3.zero;

            Vector3 locationDifference = target.position - thisRigidbody.position;
            Debug.DrawRay(thisRigidbody.position, locationDifference);
            Vector3 force = locationDifference * strength;

            //force += CalculateForceDamper();
            return force;
        }

        public static Vector3 CalculateForce(Rigidbody thisRigidbody, Vector3 sollPosition, float strength, float damping = 1500) {
            Vector3 locationDifference = sollPosition - thisRigidbody.position;
            Vector3 force = locationDifference;

            //force += CalculateForceDamper();
            //Vector3 damper = -thisRigidbody.velocity * Time.deltaTime * damping;
            //force += damper;
            return force * strength;
        }

        private const float damping = 30;
        private float lastDistanceTime;
        private Vector3 lastDistanceToTarget;
        private Vector3 CalculateForceDamper() {
            Vector3 distanceToTarget = thisRigidbody.position - target.transform.position;

            float deltaTime = Time.fixedTime - lastDistanceTime;

            Vector3 damper = Vector3.zero;
            if (deltaTime < 0.1F) {
                Vector3 velocityTowardsTarget = (distanceToTarget - lastDistanceToTarget) / deltaTime;

                damper = -velocityTowardsTarget * damping;

                //Compensate for absolute rigidbody speed (specifically when on a moving platform)
                Vector3 residualVelocity = thisRigidbody.velocity - velocityTowardsTarget;
                damper += residualVelocity * 10;
            }
            lastDistanceToTarget = distanceToTarget;
            lastDistanceTime = Time.fixedTime;

            return damper;
        }

        virtual protected void ApplyForceAtPosition(Vector3 force, Vector3 position) {
            if (float.IsNaN(force.magnitude) || float.IsInfinity(force.magnitude))
                return;

            thisRigidbody.AddForceAtPosition(force, position);
#if DEBUG_FORCE
            Debug.DrawRay(position, force / 10, Color.yellow);
#endif
        }

        #endregion

        #region Torque

        virtual protected void ControlNonKinematicRotation() {
            thisRigidbody.angularVelocity = Vector3.zero;
        }

        virtual protected Vector3 CalculateTorque() {
            Quaternion sollRotation = target.transform.rotation;
            Quaternion istRotation = thisRigidbody.rotation;
            Quaternion dRot = sollRotation * Quaternion.Inverse(istRotation);

            float angle;
            Vector3 axis;
            dRot.ToAngleAxis(out angle, out axis);
            angle = UnityAngles.Normalize(angle);
            angle += CalculateTorqueDamper(angle);

            Vector3 angleDifference = axis.normalized * (angle * Mathf.Deg2Rad);
            Vector3 torque = angleDifference * strength * 0.1F;

            return torque;
        }

        protected float lastAngleTime;
        protected float lastAngle;
        private float CalculateTorqueDamper(float angle) {

            float deltaAngle = angle - lastAngle;
            float damper = deltaAngle * damping;

            //Compensate for absolute rigidbody speed (specifically when on a moving platform)
            //Vector3 residualVelocity = Vector3.Scale(Quaternion.Inverse(velocityTowardsTarget).eulerAngles, thisRigidbody.angularVelocity);
            //damper += residualVelocity * 10;

            lastAngle = angle;
            lastAngleTime = Time.fixedTime;

            return damper;
        }

        virtual protected void ApplyTorqueAtPosition(Vector3 torque, Vector3 posToApply) {
            if (float.IsNaN(torque.magnitude))
                return;

            Vector3 torqueAxis = torque.normalized;
            Vector3 ortho = new Vector3(1, 0, 0);

            // prevent torqueAxis and ortho from pointing in the same direction
            if (((torqueAxis - ortho).sqrMagnitude < Mathf.Epsilon) || ((torqueAxis + ortho).sqrMagnitude < Mathf.Epsilon)) {
                ortho = new Vector3(0, 1, 0);
            }

            ortho = Vector3OrthoNormalize(torqueAxis, ortho);
            // calculate force 
            Vector3 force = Vector3.Cross(0.5f * torque, ortho);

            thisRigidbody.AddForceAtPosition(force, posToApply + ortho);
            thisRigidbody.AddForceAtPosition(-force, posToApply - ortho);

#if DEBUG_TORQUE
            Debug.DrawRay(posToApply + ortho / 20, force / 10, Color.yellow);
            Debug.DrawLine(posToApply + ortho / 20, posToApply - ortho / 20, Color.yellow);
            Debug.DrawRay(posToApply - ortho / 20, -force / 10, Color.yellow);
#endif
        }

        protected Vector3 Vector3OrthoNormalize(Vector3 a, Vector3 b) {
            Vector3 tmp = Vector3.Cross(a, b).normalized;
            return tmp;
        }
        #endregion

        #region Utilities

        public void DeterminePhysicsMode() {
            if (thisRigidbody == null)
                mode = PhysicsMode.Kinematic;

            if (thisRigidbody.useGravity)
                mode = PhysicsMode.NonKinematic;
            else {
                float mass = CalculateTotalMass(thisRigidbody);
                if (mass > kinematicMass + 1) // HACK: we don't count the mass of the original rigidbody...
                    mode = PhysicsMode.NonKinematic;
                else
                    mode = PhysicsMode.HybridKinematic;
            }
        }

        public static float CalculateTotalMass(Rigidbody thisRigidbody) {
            if (thisRigidbody == null)
                return 0;

            float mass = thisRigidbody.gameObject.isStatic ? Mathf.Infinity : thisRigidbody.mass;
            Joint[] joints = thisRigidbody.GetComponents<Joint>();
            for (int i = 0; i < joints.Length; i++) {
                // Seems to result in cycle in spine in some cases
                //if (joints[i].connectedBody != null)
                //    mass += CalculateTotalMass(joints[i].connectedBody);
                //else
                mass = Mathf.Infinity;
            }
            return mass;
        }

        #region Kinematics mode

        /// <summary>
        /// Switches this Rigidbody to Non-Kinematic Mode
        /// </summary>
        public void SetNonKinematic() {
            if (thisRigidbody == null)
                return;

            thisRigidbody.isKinematic = false;
            UnsetCollidersToTrigger();
        }

        /// <summary>
        /// Switches the Rigidbody to Non-Kinematic Mode
        /// </summary>
        /// The colliders will be set to normal colliders using UnsetColliderToTrigger
        /// <param name="rigidbody">The Rigidbody which should become non-kinematic</param>
        /// <param name="colliders">The colliders which should become normal colliders</param>
        public static void SetNonKinematic(Rigidbody rigidbody, Collider[] colliders) {
            if (rigidbody == null)
                return;

            rigidbody.isKinematic = false;
            UnsetCollidersToTrigger(colliders);
        }

        /// <summary>
        /// Switches this Rigidbody to Hybrid Kinematic Mode
        /// </summary>
        /// The Rigidbody will switch to Hybrid Non-Kinematic Mode if it collides
        public void SetHybridKinematic() {
            if (thisRigidbody == null) {
                colliders = null;
                return;
            }

            thisRigidbody.isKinematic = true;
            SetCollidersToTrigger();
        }

        /// <summary>
        /// Switches this Rigidbody to pure Kinematic Mode
        /// </summary>
        /// The Rigidbody will behave like normal kinematic rigidbodies
        public void SetKinematic() {
            if (thisRigidbody == null) {
                colliders = null;
                return;
            }

            thisRigidbody.isKinematic = true;
            //SetCollidersToTrigger();
        }

        /// <summary>
        /// Switches the Rigidbody to Kinematic Mode
        /// </summary>
        /// The colliders will be switched to Trigger colliders using SetCollidersToTrigger.
        /// <param name="rigidbody">The Rigidbody which should become kinematic</param>
        /// <param name="colliders">The colliders which may need switching</param>
        /// <returns>A list of changed colliders</returns>
        public static List<Collider> SetKinematic(Rigidbody rigidbody, Collider[] colliders) {
            if (rigidbody == null)
                return null;

            rigidbody.isKinematic = true;
            List<Collider> changedColliders = SetCollidersToTrigger(rigidbody, colliders);
            return changedColliders;
        }

        #endregion

        #region Colliders

        /// <summary>
        /// Switches the colliders to Trigger Colliders
        /// </summary>
        public void SetCollidersToTrigger() {
            //List<Collider> changedColliders = colliders ?? new List<Collider>();

            //Collider[] thisColliders = thisRigidbody.GetComponentsInChildren<Collider>();
            //for (int j = 0; j < thisColliders.Length; j++) {
            //    Rigidbody colliderRigidbody = thisColliders[j].attachedRigidbody;
            //    if (colliderRigidbody == null || colliderRigidbody == thisRigidbody) {
            //        if (!thisColliders[j].isTrigger) {
            //            thisColliders[j].isTrigger = true;
            //            if (!changedColliders.Contains(thisColliders[j]))
            //                changedColliders.Add(thisColliders[j]);
            //        }
            //    }
            //}
            //colliders = changedColliders;
            colliders = SetCollidersToTrigger(thisRigidbody, colliders);
        }

        /// <summary>
        /// Switches the colliders to Trigger Colliders
        /// </summary>
        /// Normal Colliders will become Trigger Colliders to detect collisions with environment
        /// Only Normal Colliders will be changed, Trigger colliders will not be changed.
        /// Only Colliders of the given Rigidbody will be changeds.
        /// Colliders of child Rigidbodies will be unaffected.
        /// The return value will contain the list of colliders which are affected by this change.
        /// <param name="rigidbody">The Rigidbody for which the colliders should be switched</param>
        /// <param name="colliders">The colliders which may need switching</param>
        /// <returns>A list of updated colliders</returns>
        public static List<Collider> SetCollidersToTrigger(Rigidbody rigidbody, Collider[] colliders) {
            List<Collider> changedColliders = new List<Collider>();

            for (int j = 0; j < colliders.Length; j++) {

                Rigidbody colliderRigidbody = colliders[j].attachedRigidbody;
                if (colliderRigidbody != null && colliderRigidbody != rigidbody)
                    // Don't change colliders of sub-rigidbodies
                    continue;

                if (colliders[j].isTrigger)
                    // Don't change trigger colliders
                    continue;

                colliders[j].isTrigger = true;
                if (!changedColliders.Contains(colliders[j]))
                    changedColliders.Add(colliders[j]);
            }

            return changedColliders;
        }

        /// <summary>
        /// Switches the colliders to Trigger Colliders
        /// </summary>
        /// Normal Colliders will become Trigger Colliders to detect collisions with environment
        /// Only Normal Colliders will be changed, Trigger colliders will not be changed.
        /// Only Colliders of the given Rigidbody will be changeds.
        /// Colliders of child Rigidbodies will be unaffected.
        /// The return value will contain the list of colliders which are affected by this change.
        /// <param name="rigidbody">The Rigidbody for which the colliders should be switched</param>
        /// <param name="colliders">The colliders which may need switching</param>
        /// <returns>A list of updated colliders</returns>
        public static List<Collider> SetCollidersToTrigger(Rigidbody rigidbody, List<Collider> colliders) {
            List<Collider> changedColliders = new List<Collider>();

            for (int j = 0; j < colliders.Count; j++) {

                Rigidbody colliderRigidbody = colliders[j].attachedRigidbody;
                if (colliderRigidbody != null && colliderRigidbody != rigidbody)
                    // Don't change colliders of sub-rigidbodies
                    continue;

                if (colliders[j].isTrigger)
                    // Don't change trigger colliders
                    continue;

                colliders[j].isTrigger = true;
                if (!changedColliders.Contains(colliders[j]))
                    changedColliders.Add(colliders[j]);
            }

            return changedColliders;
        }

        /// <summary>
        /// Switches this Rigidbody to Non-Kinematic mode.
        /// </summary>
        public virtual void UnsetCollidersToTrigger() {
            UnsetCollidersToTrigger(colliders);
        }

        /// <summary>
        /// Switches the Rigidbody to Non-Kinematic mode.
        /// </summary>
        /// The colliders in the parameter will all be set to non-trigger Colliders.
        /// To restore the state of the Rigidbody as it was before SetCollidersToTrigger
        /// the colliders paramter of this function should be set to the changed colliders as
        /// returned by the SetCollidersToTrigger function.
        /// <param name="colliders">The colliders which will become normal colliders</param>
        public static void UnsetCollidersToTrigger(Collider[] colliders) {
            if (colliders == null)
                return;

            foreach (Collider c in colliders)
                c.isTrigger = false;
        }

        /// <summary>
        /// Switches the Rigidbody to Non-Kinematic mode.
        /// </summary>
        /// The colliders in the parameter will all be set to non-trigger Colliders.
        /// To restore the state of the Rigidbody as it was before SetCollidersToTrigger
        /// the colliders paramter of this function should be set to the changed colliders as
        /// returned by the SetCollidersToTrigger function.
        /// <param name="colliders">The colliders which will become normal colliders</param>
        public static void UnsetCollidersToTrigger(List<Collider> colliders) {
            if (colliders == null)
                return;

            foreach (Collider c in colliders)
                c.isTrigger = false;
        }

        #endregion

        #endregion
    }

}