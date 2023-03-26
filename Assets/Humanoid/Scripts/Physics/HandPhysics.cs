#define DEBUG_FORCE
//#define DEBUG_TORQUE
//#define IMPULSE

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Passer.Humanoid {

    public class BasicHandPhysics : MonoBehaviour {
        protected static void DebugLog(string s) {
#if UNITY_EDITOR
            //Debug.Log(s);
#endif
        }

        public HandTarget handTarget;

        public virtual void Start() {
            if (!handTarget.physics)
                AdvancedHandPhysics.SetKinematic(handTarget.handRigidbody);
        }

        [HideInInspector]
        protected Rigidbody handRigidbody;

        protected virtual void Initialize() {
            if (handTarget == null)
                return;

            handRigidbody = GetComponent<Rigidbody>();
        }

        #region Update

        private List<Collider> currentTriggers = new List<Collider>();
        private List<Collider> lastTriggers = new List<Collider>();

        public virtual void FixedUpdate() {
            lastTriggers = currentTriggers;
            currentTriggers = new List<Collider>();

        }

        protected virtual void Update() {
            foreach (Collider lastTrigger in lastTriggers) {
                if (!currentTriggers.Contains(lastTrigger))
                    SendHandTriggerExit(lastTrigger);
            }
        }

        public virtual void ManualFixedUpdate(HandTarget _handTarget) {
            handTarget = _handTarget;

            /*
            Collider[] colliders = Physics.OverlapSphere(handTarget.grabSocket.transform.position, 0.1F);
            if (colliders.Length == 0)
                return;

            Collider nearingCollider = null;
            for (int i = 0; i < colliders.Length; i++) {
                if (colliders[i].attachedRigidbody == null)
                    nearingCollider = colliders[i];
            }
            if (nearingCollider != null)
                HandInteraction.OnNearing(handTarget, nearingCollider.gameObject);
            */
            if (handRigidbody == null)
                Initialize();

            if (handRigidbody != null && !handRigidbody.isKinematic)
                UpdateNonKinematicRigidbody();
        }

        protected virtual void UpdateNonKinematicRigidbody() {
            Vector3 torque = CalculateTorque();
            ApplyTorqueAtPosition(torque, handTarget.handPalm.position);

            Vector3 wristTorque = CalculateWristTorque();
            ApplyTorqueAtPosition(wristTorque, handTarget.hand.bone.transform.position);

            Vector3 force = CalculateForce();
            ApplyForceAtPosition(force, handTarget.handPalm.position);

        }


        #endregion

        #region Events

        public virtual void OnCollisionEnter(Collision collision) {
            if (collision.contacts.Length == 0)
                return;

            Rigidbody objRigidbody = collision.rigidbody;
            if (objRigidbody != null)
                handTarget.OnTouchStart(objRigidbody.gameObject, collision.contacts[0].point);
            else
                handTarget.OnTouchStart(collision.gameObject, collision.contacts[0].point);
        }

        public virtual void OnCollisionExit(Collision collision) {
            Rigidbody objRigidbody = collision.rigidbody;
            if (objRigidbody != null)
                handTarget.OnTouchEnd(objRigidbody.gameObject);
            else
                handTarget.OnTouchEnd(collision.gameObject);
        }

        public virtual void OnTriggerEnter(Collider collider) {
            if (collider.isTrigger)
                // We cannot touch trigger colliders
                return;

            Rigidbody objRigidbody = collider.attachedRigidbody;
            if (objRigidbody != null)
                handTarget.touchedObject = objRigidbody.gameObject;
            else
                handTarget.touchedObject = collider.gameObject;

            handTarget.OnTouchStart(handTarget.touchedObject, collider.transform.position);
        }

        public virtual void OnTriggerStay(Collider collider) {
            if (!lastTriggers.Contains(collider))
                // This is a new trigger
                SendHandTriggerEnter(collider);

            currentTriggers.Add(collider);

            if (collider.isTrigger)
                // We cannot touch trigger colliders
                return;

            Rigidbody objRigidbody = collider.attachedRigidbody;
            if (objRigidbody != null)
                handTarget.touchedObject = objRigidbody.gameObject;
            else
                handTarget.touchedObject = collider.gameObject;

            handTarget.GrabCheck(handTarget.touchedObject);
        }

        public virtual void OnTriggerExit(Collider collider) {
            Rigidbody objRigidbody = collider.attachedRigidbody;
            if (objRigidbody != null)
                handTarget.OnTouchEnd(objRigidbody.gameObject);
            else
                handTarget.OnTouchEnd(collider.gameObject);

            handTarget.touchedObject = null;
        }

        protected void SendHandTriggerEnter(Collider collider) {
            Rigidbody objRigidbody = collider.attachedRigidbody;

            if (collider.gameObject != null) {
                IHandTriggerEvents triggerEvent;
                if (objRigidbody != null)
                    triggerEvent = objRigidbody.GetComponent<IHandTriggerEvents>();
                else
                    triggerEvent = collider.GetComponent<IHandTriggerEvents>();

                if (triggerEvent != null)
                    triggerEvent.OnHandTriggerEnter(handTarget, collider);
            }
        }

        protected void SendHandTriggerExit(Collider collider) {
            Rigidbody objRigidbody = collider.attachedRigidbody;

            if (collider.gameObject != null) {
                IHandTriggerEvents triggerEvent;
                if (objRigidbody != null)
                    triggerEvent = objRigidbody.GetComponent<IHandTriggerEvents>();
                else
                    triggerEvent = collider.GetComponent<IHandTriggerEvents>();

                if (triggerEvent != null)
                    triggerEvent.OnHandTriggerExit(handTarget, collider);
            }
        }
        #endregion

        #region Force

        protected Vector3 CalculateForce() {
            /*
            //Vector3 locationDifference = handTarget.stretchlessTarget.position - handTarget.handRigidbody.position;
            //Debug.DrawLine(handTarget.stretchlessTarget.position, handTarget.handRigidbody.position);
            Vector3 locationDifference = handTarget.transform.position - handTarget.handRigidbody.position;
            //Debug.DrawLine(handTarget.hand.target.transform.position, handTarget.handRigidbody.position);
            
            //if (locationDifference.magnitude < 0.01F)
            //    return Vector3.zero;

            Vector3 force = locationDifference * handTarget.strength;

            force += CalculateForceDamper();
            */
            Vector3 force = HybridPhysics.CalculateForce(handRigidbody, handTarget.stretchlessTarget.position, handTarget.strength);
            return force;
        }

        private const float damping = 12;
        private float lastDistanceTime;
        private Vector3 lastDistanceToTarget;
        private Vector3 CalculateForceDamper() {
            Vector3 distanceToTarget = handTarget.hand.bone.transform.position - handTarget.hand.target.transform.position;

            float deltaTime = Time.fixedTime - lastDistanceTime;

            Vector3 damper = Vector3.zero;
            if (deltaTime < 0.1F) {
                Vector3 velocityTowardsTarget = (distanceToTarget - lastDistanceToTarget) / deltaTime;

                damper = -velocityTowardsTarget * damping;

                //Compensate for absolute rigidbody speed (specifically when on a moving platform)
                if (handRigidbody != null) {
                    Vector3 residualVelocity = handRigidbody.velocity - velocityTowardsTarget;
                    damper += residualVelocity * 10;
                }
            }
            lastDistanceToTarget = distanceToTarget;
            lastDistanceTime = Time.fixedTime;

            return damper;
        }

        protected void ApplyForce(Vector3 force) {
            if (float.IsNaN(force.magnitude))
                return;

            /*
            if (contactPoint.sqrMagnitude > 0) {
                // The contact point is OK, but the force here is not OK, because this is the force from the hand
                // The force needs to be projected on the contactPoint !
                //handRigidbody.AddForceAtPosition(force, contactPoint);
                //#if DEBUG_FORCE
                Debug.DrawRay(contactPoint, force / 10, Color.yellow);
                //#endif
            }
            else {
                // The contact point is OK, but the force here is not OK, because this is the force from the hand
                // The force needs to be projected on the contactPoint !
                //handRigidbody.AddForceAtPosition(force, target.handPalm.position);
                handRigidbody.AddForce(force);
                //#if DEBUG_FORCE
                Debug.DrawRay(target.handPalm.position, force / 10, Color.yellow);
                //#endif
            }
            */
            handRigidbody.AddForce(force);
#if DEBUG_FORCE
            Debug.DrawRay(handRigidbody.position, force / 10, Color.yellow);
#endif
        }

        protected void ApplyForceAtPosition(Vector3 force, Vector3 position) {
            if (float.IsNaN(force.magnitude))
                return;

            handRigidbody.AddForceAtPosition(force, position);
#if DEBUG_FORCE
            Debug.DrawRay(position, force / 10, Color.yellow);
#endif
        }

        #endregion

        #region Torque

        protected Vector3 CalculateTorque() {
            Quaternion sollRotation = handTarget.hand.target.transform.rotation * handTarget.hand.target.toBoneRotation;
            Quaternion istRotation = handTarget.hand.bone.transform.rotation;
            Quaternion dRot = sollRotation * Quaternion.Inverse(istRotation);

            float angle;
            Vector3 axis;
            dRot.ToAngleAxis(out angle, out axis);
            angle = UnityAngles.Normalize(angle);

            Vector3 angleDifference = axis.normalized * (angle * Mathf.Deg2Rad);
            Vector3 torque = angleDifference * handTarget.strength * 0.1F;
            return torque;
        }

        protected Vector3 CalculateWristTorque() {
            //Vector3 wristTension = target.GetWristTension();

            // Not stable
            //Vector3 forces = new Vector3(-(wristTension.x * wristTension.x * 10), -(wristTension.y * wristTension.y * 10), -wristTension.z * wristTension.z * 10);
            //Vector3 forces = new Vector3(0, -(wristTension.y * wristTension.y * 10), -wristTension.z * wristTension.z * 10);

            Vector3 torque = Vector3.zero; // (0, 0, -wristTension.z * wristTension.z * 10);
            return torque;
        }

        private void ApplyTorque(Vector3 torque) {
            //AddTorqueAtPosition(torque, target.handPalm.position);
            ApplyTorqueAtPosition(torque, handTarget.hand.bone.transform.position);
        }

        protected void ApplyTorqueAtPosition(Vector3 torque, Vector3 posToApply) {
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

            if (handTarget.handRigidbody != null) {
                handTarget.handRigidbody.AddForceAtPosition(force, posToApply + ortho);
                handTarget.handRigidbody.AddForceAtPosition(-force, posToApply - ortho);
            }

#if DEBUG_TORQUE
            UnityEngine.Debug.DrawRay(posToApply + ortho / 20, force / 10, Color.yellow);
            UnityEngine.Debug.DrawLine(posToApply + ortho / 20, posToApply - ortho / 20, Color.yellow);
            UnityEngine.Debug.DrawRay(posToApply - ortho / 20, -force / 10, Color.yellow);
#endif
        }

        private Vector3 Vector3OrthoNormalize(Vector3 a, Vector3 b) {
            Vector3 tmp = Vector3.Cross(a.normalized, b).normalized;
            return tmp;
        }
        #endregion

    }

    public class AdvancedHandPhysics : BasicHandPhysics {
#if UNITY_EDITOR
        public static bool debug = false;
#endif
        public enum PhysicsMode {
            Kinematic,
            NonKinematic,
            HybridKinematic,
            NonKinematicVelocity,
            VelocityUpdate
        }
        public PhysicsMode mode = PhysicsMode.HybridKinematic;

        private bool colliding;
        public bool hasCollided = false;
        public Vector3 contactPoint;

        public Vector3 force;
        public Vector3 torque;

        protected override void Initialize() {
            if (handTarget == null)
                return;

            if (enabled) {
                handRigidbody = GetComponent<Rigidbody>();
                if (handRigidbody != null) {
                    if (handRigidbody != null) {
                        if (handRigidbody.useGravity || mode == PhysicsMode.NonKinematic)
                            SetNonKinematic(handRigidbody, handTarget.colliders);
                        else
                            handTarget.colliders = SetKinematic(handRigidbody);
                    }
                    handRigidbody.maxAngularVelocity = 20;
                }
            }
        }

        #region Update

        public override void FixedUpdate() {
            CalculateVelocity();
        }

        public override void ManualFixedUpdate(HandTarget _handTarget) {
            handTarget = _handTarget;

            if (hasCollided && !colliding) {
                handTarget.OnTouchEnd(handTarget.touchedObject);
                handTarget.touchedObject = null;
            }

            if (handTarget.touchedObject == null) { // Object may be destroyed
                hasCollided = false;
            }

            if (handRigidbody == null)
                Initialize();

            // Check for stuck hands. Only when hand is kinematic you can pull the hand loose
            // it will then turn into a kinematic hand, which results in snapping the hand back
            // onto the forearm.
            if (handTarget.forearm.bone.transform != null && handRigidbody != null && !handRigidbody.isKinematic) {
                float distance = Vector3.Distance(handTarget.hand.bone.transform.position, handTarget.forearm.bone.transform.position) - handTarget.forearm.bone.length;
                if (distance > 0.05F) {
                    handTarget.colliders = SetKinematic(handRigidbody);
                }
            }

            UpdateRigidbody();

            colliding = false;
        }

        public void UpdateRigidbody() {
            if (handRigidbody == null) {
                UpdateGrabbedMechanicalJoint();
                return;
            }

            if (mode == PhysicsMode.VelocityUpdate) {
                UpdateRigidbodyVelocity();
                return;
            }

            if ((mode == PhysicsMode.NonKinematic || mode == PhysicsMode.NonKinematicVelocity) && handRigidbody.isKinematic)
                SetNonKinematic(handRigidbody, handTarget.colliders);

            Quaternion targetRotation = handTarget.transform.rotation;

            Quaternion rot = Quaternion.Inverse(handRigidbody.rotation) * targetRotation;
            float angle;
            Vector3 axis;
            rot.ToAngleAxis(out angle, out axis);

            if (handRigidbody.isKinematic)
                UpdateKinematicRigidbody();
            else
                UpdateNonKinematicRigidbody();
        }

        private void UpdateGrabbedMechanicalJoint() {
            Rigidbody grabbedRigidbody = handTarget.hand.bone.transform.GetComponentInParent<Rigidbody>();
            if (grabbedRigidbody == null)
                return;

            MechanicalJoint grabbedMechanicalJoint = grabbedRigidbody.GetComponent<MechanicalJoint>();
            if (grabbedMechanicalJoint == null)
                return;

            Vector3 locationDifference = handTarget.hand.target.transform.position - handTarget.hand.bone.transform.position;
            grabbedRigidbody.transform.position += locationDifference;

            Vector3 correctionVector = grabbedMechanicalJoint.GetCorrectionVector();
            grabbedRigidbody.transform.position += correctionVector;

            Quaternion rotationDifference = handTarget.hand.target.transform.rotation * Quaternion.Inverse(handTarget.hand.bone.targetRotation);
            grabbedRigidbody.transform.rotation = rotationDifference * grabbedRigidbody.transform.rotation;

            Quaternion correctionRotation = grabbedMechanicalJoint.GetCorrectionAxisRotation();
            grabbedRigidbody.transform.rotation = grabbedRigidbody.transform.rotation * correctionRotation;
        }

        private void UpdateKinematicRigidbody() {
            if (mode == PhysicsMode.NonKinematic ||
                mode == PhysicsMode.HybridKinematic && (
                    handRigidbody.mass > HybridPhysics.kinematicMass ||
                    handRigidbody.GetComponent<Joint>() != null
                )
                ) {

                SetNonKinematic();
                return;
            }

            force = Vector3.zero;
            torque = Vector3.zero;
        }

        protected override void UpdateNonKinematicRigidbody() {
            if (mode != PhysicsMode.NonKinematicVelocity) {
                //torque = CalculateTorque();
                //ApplyTorqueAtPosition(torque, handTarget.handPalm.position);

                //Vector3 wristTorque = CalculateWristTorque();
                //ApplyTorqueAtPosition(wristTorque, handTarget.hand.bone.transform.position);

                force = CalculateForce();
                ApplyForceAtPosition(force, handTarget.handPalm.position);

                // Kinematic Hand Rotation
                handRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
                handTarget.hand.SetBoneRotation(handTarget.hand.target.transform.rotation);

                if (handTarget.humanoid.haptics && force.magnitude > 5)
                    handTarget.Vibrate(force.magnitude / 25);
            }
            else {
                force = Vector3.zero;
                torque = Vector3.zero;
            }

            if (!hasCollided &&
                !handRigidbody.useGravity &&
                handRigidbody.mass <= HybridPhysics.kinematicMass &&
                mode != PhysicsMode.NonKinematic &&
                mode != PhysicsMode.NonKinematicVelocity &&
                handRigidbody.GetComponent<Joint>() == null
                ) {

                if (!handRigidbody.isKinematic)
                    handTarget.colliders = SetKinematic(handRigidbody);
            }
        }

        private const float MaxVelocityChange = 10f;
        private const float MaxAngularVelocityChange = 20f;
        private const float VelocityMagic = 6000f;
        private const float AngularVelocityMagic = 50f;
        private const float ExpectedDeltaTime = 0.0111f;

        protected virtual void UpdateRigidbodyVelocity() {
            if (handRigidbody.isKinematic)
                SetNonKinematic();

            Vector3 handPosition = handRigidbody.transform.position;
            Quaternion handRotation = handRigidbody.transform.rotation * handTarget.hand.bone.toTargetRotation;

            Vector3 targetPosition = handTarget.hand.target.transform.position;
            Quaternion targetRotation = handTarget.hand.target.transform.rotation;

            float velocityMagic = VelocityMagic / (Time.fixedDeltaTime / ExpectedDeltaTime);
            float angularVelocityMagic = AngularVelocityMagic / (Time.fixedDeltaTime / ExpectedDeltaTime);

            Vector3 positionDelta;
            Quaternion rotationDelta;

            float angle;
            Vector3 axis;

            positionDelta = (targetPosition - handPosition);
            rotationDelta = targetRotation * Quaternion.Inverse(handRotation);

            Vector3 velocityTarget = (positionDelta * velocityMagic) * Time.fixedDeltaTime;
            if (float.IsNaN(velocityTarget.x) == false)
                handRigidbody.velocity = Vector3.MoveTowards(handRigidbody.velocity, velocityTarget, MaxVelocityChange);

            rotationDelta.ToAngleAxis(out angle, out axis);

            if (angle > 180)
                angle -= 360;

            if (angle != 0) {
                Vector3 angularTarget = angle * axis;
                if (!float.IsNaN(angularTarget.x)) {
                    angularTarget = (angularTarget * angularVelocityMagic) * Time.fixedDeltaTime;
                    handRigidbody.angularVelocity = Vector3.MoveTowards(handRigidbody.angularVelocity, angularTarget, MaxAngularVelocityChange);
                }
            }
        }

        #endregion Update

        #region Events

        public override void OnTriggerEnter(Collider collider) {
            bool otherHasKinematicPhysics = false;
            bool otherIsHumanoid = false;

            Rigidbody otherRigidbody = collider.attachedRigidbody;
            if (otherRigidbody != null) {
                AdvancedHandPhysics kp = otherRigidbody.GetComponent<AdvancedHandPhysics>();
                if (kp == null)
                    kp = otherRigidbody.GetComponentInParent<AdvancedHandPhysics>();
                otherHasKinematicPhysics = (kp != null);
                HumanoidControl humanoid = otherRigidbody.GetComponent<HumanoidControl>();
                otherIsHumanoid = (humanoid != null);
            }

            bool collidedWithMyself = IsHumanoidCollider(collider, handTarget.humanoid);

            if (mode == PhysicsMode.HybridKinematic &&
                handRigidbody != null &&
                handRigidbody.isKinematic &&
                (!collider.isTrigger || otherHasKinematicPhysics) &&
                !otherIsHumanoid &&
                !collidedWithMyself) {

                colliding = true;
                hasCollided = true;
                if (otherRigidbody != null) {
                    handTarget.touchedObject = otherRigidbody.gameObject;
                    SetNonKinematic(handRigidbody, handTarget.colliders);
                }
                else {
                    handTarget.touchedObject = collider.gameObject;
                    SetNonKinematic(handRigidbody, handTarget.colliders);
                }

                ProcessFirstCollision(handRigidbody, collider);
            }

            if (hasCollided) {
                if (otherRigidbody != null)
                    handTarget.GrabCheck(otherRigidbody.gameObject);
                else
                    handTarget.GrabCheck(collider.gameObject);
            }
        }

        /// <summary>
        /// Determine if the collider is part of this humanoid
        /// or attached to this humanoid.
        /// </summary>
        /// <returns></returns>
        /// To be merged with HumanoidControl.IsMyRigidbody ?
        private bool IsHumanoidCollider(Collider collider, HumanoidControl humanoid) {
            Rigidbody otherRigidbody = collider.attachedRigidbody;
            if (otherRigidbody == null)
                return false;

            // is part of the humanoid?
            if (otherRigidbody == humanoid.leftHandTarget.handRigidbody ||
                otherRigidbody == humanoid.rightHandTarget.handRigidbody ||
                otherRigidbody == humanoid.humanoidRigidbody ||
                otherRigidbody == humanoid.characterRigidbody) {
                return true;
            }

            /* Why have this here?
             * With this, you cannot touch anything attached to your body (like buttons)
            // is attached to this humanoid?
            if (otherRigidbody.transform.parent == null)
                return false;

            Rigidbody parentRigidbody = otherRigidbody.transform.parent.GetComponentInParent<Rigidbody>();
            if (parentRigidbody == null)
                return false;

            if (parentRigidbody == humanoid.leftHandTarget.handRigidbody ||
                parentRigidbody == humanoid.rightHandTarget.handRigidbody ||
                parentRigidbody == humanoid.humanoidRigidbody ||
                parentRigidbody == humanoid.characterRigidbody) {
                return true;
            }
            */
            return false;
        }

        public override void OnTriggerExit(Collider collider) {
        }

        public override void OnCollisionEnter(Collision collision) {
            if (collision.rigidbody != null && collision.rigidbody.gameObject == handTarget.grabbedObject)
                // Don't collide with the things you are holding
                return;

            colliding = true;

            if (collision.contacts.Length > 0)
                contactPoint = collision.contacts[0].point;
            base.OnCollisionEnter(collision);

            // Forward the Collision Enter event to the object the hand is holding
            // This is needed because the rigidbody of the grabbedobject has been disabled
            // Without a rigidbody, the object will no longer receive collision events
            if (handTarget.grabbedRigidbody)
                handTarget.grabbedObject.SendMessage("OnCollisionEnter", collision, SendMessageOptions.DontRequireReceiver);
        }

        public void OnCollisionStay(Collision collision) {
            if (collision.rigidbody != null && collision.rigidbody.gameObject == handTarget.grabbedObject)
                // Don't collide with the things you are holding
                return;

            if (collision.rigidbody != null && handTarget.grabbedObject != null && collision.rigidbody.transform.IsChildOf(handTarget.grabbedObject.transform))
                // Don't collide with the things you are holding
                return;

            colliding = true;

            if (collision.contacts.Length > 0)
                contactPoint = collision.contacts[0].point;
        }

        public override void OnCollisionExit(Collision collision) {
            DebugLog("Collision Exit " + collision.gameObject);

            if (handRigidbody != null && !handRigidbody.useGravity) {
                // The sweeptests fails quite often when holding nested rigidbodies
                // But sweeptest are required to get stable collisions with the environment
                RaycastHit hit;
                if (!handRigidbody.SweepTest(handTarget.transform.position - handRigidbody.position, out hit)) {
                    handTarget.OnTouchEnd(handTarget.touchedObject);
                    hasCollided = false;
                    contactPoint = Vector3.zero;
                    handTarget.touchedObject = null;
                }
            }

            // Forward the Collision Exit event to the object the hand is holding
            // This is needed because the rigidbody of the grabbedobject has been disabled
            // Without a rigidbody, the object will no longer receive collision events
            if (handTarget.grabbedRigidbody)
                handTarget.grabbedObject.SendMessage("OnCollisionExit", collision, SendMessageOptions.DontRequireReceiver);
        }

        #endregion

        public void DeterminePhysicsMode(float kinematicMass = 1) {
            mode = DeterminePhysicsMode(handRigidbody, kinematicMass);
        }

        public static PhysicsMode DeterminePhysicsMode(Rigidbody rigidbody, float kinematicMass = 1) {
            if (rigidbody == null)
                return PhysicsMode.Kinematic;

            PhysicsMode physicsMode;
            if (rigidbody.useGravity) {
                physicsMode = PhysicsMode.NonKinematic;
            }
            else {
                float mass = CalculateTotalMass(rigidbody);
                if (mass > kinematicMass)
                    physicsMode = PhysicsMode.NonKinematic;
                else
                    physicsMode = PhysicsMode.HybridKinematic;
            }
            return physicsMode;
        }

        public static float CalculateTotalMass(Rigidbody rigidbody) {
            if (rigidbody == null)
                return 0;

            float mass = rigidbody.gameObject.isStatic ? Mathf.Infinity : rigidbody.mass;
            Joint[] joints = rigidbody.GetComponents<Joint>();
            for (int i = 0; i < joints.Length; i++) {
                // Seems to result in cycle in spine in some cases
                //if (joints[i].connectedBody != null)
                //    mass += CalculateTotalMass(joints[i].connectedBody);
                //else
                // mass = Mathf.Infinity;

                // Disabled to support dummy joints to prevent distroying the rigidbodies
            }
            return mass;
        }

        public Vector3 boneVelocity;
        private Vector3 lastPosition = Vector3.zero;
        private void CalculateVelocity() {
            if (lastPosition != Vector3.zero) {
                boneVelocity = (handTarget.hand.bone.transform.position - lastPosition) / Time.fixedDeltaTime;
            }
            lastPosition = handTarget.hand.bone.transform.position;
        }

        public void ProcessFirstCollision(Rigidbody rigidbody, Collider otherCollider) {

#if IMPULSE
		CalculateCollisionImpuls(rigidbody, otherRigidbody, collisionPoint);
#endif
        }

#if IMPULSE
	private static void CalculateCollisionImpuls(Rigidbody rigidbody, Rigidbody otherRigidbody, Vector3 collisionPoint) {
		if (otherRigidbody != null) {
			Vector3 myImpuls = (rigidbody.mass / 10) * rigidbody.velocity;
			otherRigidbody.AddForceAtPosition(myImpuls, collisionPoint, ForceMode.Impulse);
		}
	}
#endif

        public static void SetNonKinematic(Rigidbody rigidbody, List<Collider> colliders) {
            if (rigidbody == null)
                return;

            DebugLog("SetNonKinematic " + rigidbody.name);

            GameObject obj = rigidbody.gameObject;
            if (obj.isStatic == false) {
                rigidbody.isKinematic = false;
                Target.UnsetColliderToTrigger(colliders);
            }
        }

        public static List<Collider> SetKinematic(Rigidbody rigidbody) {
            if (rigidbody == null)
                return null;

            DebugLog("SetKinematic " + rigidbody.name + " " + rigidbody.isKinematic);

            GameObject obj = rigidbody.gameObject;
            if (obj.isStatic == false) {
                rigidbody.isKinematic = true;
                return Target.SetColliderToTrigger(obj);
            }
            return null;
        }

        public void SetNonKinematic() {
            DebugLog("SetNonKinematic");

            if (handRigidbody == null)
                return;

            handRigidbody.isKinematic = false;
            UnsetCollidersToTrigger();
        }

        public void SetKinematic() {
            DebugLog("SetKinematic");

            if (handRigidbody == null) {
                handTarget.colliders = null;
                return;
            }

            handRigidbody.isKinematic = true;
            SetCollidersToTrigger();
        }

        public void SetCollidersToTrigger() {
            DebugLog("SetCollidersToTrigger");

            List<Collider> changedColliders = handTarget.colliders ?? new List<Collider>();

            Collider[] thisColliders = handRigidbody.GetComponentsInChildren<Collider>();
            for (int j = 0; j < thisColliders.Length; j++) {
                Rigidbody colliderRigidbody = thisColliders[j].attachedRigidbody;
                if (colliderRigidbody == null || colliderRigidbody == handRigidbody) {
                    if (!thisColliders[j].isTrigger) {
                        thisColliders[j].isTrigger = true;
                        if (!changedColliders.Contains(thisColliders[j]))
                            changedColliders.Add(thisColliders[j]);
                    }
                }
            }
            handTarget.colliders = changedColliders;
        }

        public void UnsetCollidersToTrigger() {
            DebugLog("UnsetCollidersToTrigger");

            if (handTarget.colliders == null)
                return;

            foreach (Collider c in handTarget.colliders)
                c.isTrigger = false;
        }
    }
}