using UnityEngine;

namespace Passer.Humanoid {

    public class HandMovements : Movements {
        public HandTarget handTarget;

        #region Init
        //public void Init(Transform targetTransform) {
        //    handTarget = targetTransform.GetComponent<HandTarget>();
        //}

        public override void Start(HumanoidControl humanoid, HumanoidTarget _target) {
            handTarget = (HandTarget)_target;

            if (handTarget.humanoid.avatarRig == null || handTarget.hand.bone.transform == null)
                return;


            if (humanoid.physics && handTarget.physics) {
                AdvancedHandPhysics physics = handTarget.hand.bone.transform.GetComponent<AdvancedHandPhysics>();
                if (physics == null) {
                    physics = handTarget.hand.bone.transform.gameObject.AddComponent<AdvancedHandPhysics>();
                }

                physics.handTarget = handTarget;
                if (handTarget.physicsMode == AdvancedHandPhysics.PhysicsMode.HybridKinematic)
                    physics.mode = AdvancedHandPhysics.DeterminePhysicsMode(handTarget.handRigidbody, HybridPhysics.kinematicMass);
                else 
                    physics.mode = handTarget.physicsMode;                
            }
            else {
                BasicHandPhysics physics = handTarget.hand.bone.transform.GetComponent<BasicHandPhysics>();
                if (physics == null) {
                    physics = handTarget.hand.bone.transform.gameObject.AddComponent<BasicHandPhysics>();
                }
                physics.handTarget = handTarget;
            }
        }

        public static void DetachHand(HandTarget handTarget) {
            if (handTarget.physics == false || handTarget.humanoid.physics == false)
                return;

            if (handTarget.hand.bone.transform == null)
                return;

            handTarget.hand.bone.transform.parent = handTarget.humanoid.transform;
            int layer = LayerMask.NameToLayer("Humanoid");
            if (layer > 0)
                handTarget.hand.bone.transform.gameObject.layer = layer;

            if (handTarget.handRigidbody == null) {
                handTarget.handRigidbody = handTarget.hand.bone.transform.GetComponent<Rigidbody>();
                if (handTarget.handRigidbody == null)
                    handTarget.handRigidbody = handTarget.hand.bone.transform.gameObject.AddComponent<Rigidbody>();
            }
            handTarget.handRigidbody.mass = 1;
            handTarget.handRigidbody.drag = 0;
            handTarget.handRigidbody.angularDrag = 10;
            handTarget.handRigidbody.useGravity = false;
            handTarget.handRigidbody.isKinematic = true;
            handTarget.handRigidbody.interpolation = RigidbodyInterpolation.None;
#if UNITY_2018_3_OR_NEWER
            handTarget.handRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
#else
            handTarget.handRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
#endif
            handTarget.handRigidbody.centerOfMass = handTarget.handPalm.position - handTarget.hand.bone.transform.position;

            if (handTarget.stretchlessTarget == null) {
                handTarget.stretchlessTarget = handTarget.hand.target.transform.Find("Stretchless Target");
                if (handTarget.stretchlessTarget == null) {
                    GameObject stretchlessTargetObj = new GameObject("Stretchless Target");
                    handTarget.stretchlessTarget = stretchlessTargetObj.transform;
                    handTarget.stretchlessTarget.parent = handTarget.hand.target.transform;
                    handTarget.stretchlessTarget.localPosition = Vector3.zero;
                    handTarget.stretchlessTarget.localRotation = Quaternion.identity;
                }
            }
        }

        public void ReattachHand() {
            if (handTarget == null || handTarget.hand.bone.transform == null)
                return;
            handTarget.hand.bone.transform.parent = handTarget.forearm.bone.transform;
        }
        #endregion

        #region Update

        public static void Update(HandTarget handTarget) {
            if (handTarget == null)
                return;

            handTarget.handMovements.HandUpdate();
            //HandInteraction.CheckLetGo(handTarget);
        }

        public Vector3 toOtherHandle;
        public Quaternion hand2handle;
        private void HandUpdate() {
            if (handTarget != null && handTarget.twoHandedGrab) { 
                // target.hand is the primary grabbing hand
                // target.otherHand is the secondary grabbing hand

                //Vector3 toOtherTarget = handTarget.otherHand.hand.target.transform.position - handTarget.hand.target.transform.position;

                //Quaternion lookRot = Quaternion.LookRotation(toOtherTarget);
                //handTarget.hand.target.transform.rotation = lookRot * hand2handle;
            }
        }

        public void FixedUpdate() {
            if (handTarget == null || handTarget.humanoid == null || handTarget.humanoid.avatarRig == null || handTarget.hand.bone.transform == null)
                return;

            //AdvancedHandPhysics handPhysics = handTarget.hand.bone.transform.GetComponent<AdvancedHandPhysics>();
            BasicHandPhysics handPhysics = handTarget.hand.bone.transform.GetComponent<BasicHandPhysics>();
            if (handPhysics == null)
                return;

            handPhysics.ManualFixedUpdate(handTarget);
        }

        #endregion

        #region Collisions
        public static void SetAllColliders(Transform transform, bool enabled) {
            Collider[] colliders = transform.GetComponentsInChildren<Collider>();
            foreach (Collider c in colliders)
                c.enabled = enabled;
        }

        public static void SetAllColliders(GameObject obj, bool enabled) {
            Collider[] colliders = obj.GetComponentsInChildren<Collider>();
            foreach (Collider c in colliders)
                c.enabled = enabled;
        }
        #endregion
    }
}