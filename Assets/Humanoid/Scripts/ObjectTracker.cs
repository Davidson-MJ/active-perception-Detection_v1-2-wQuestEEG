using UnityEngine;

namespace Passer {
    [System.Serializable]
    public class ObjectTracker : Target {

        #region Sensors

        public SensorComponent sensorComponent;

        public override void InitSensors() {
        }

        public override void StartSensors() {
        }
        #endregion

        #region Settings

        public bool physics;

        #endregion

        [SerializeField]
        protected Vector3 sensor2ObjectPosition;
        [SerializeField]
        protected Quaternion sensor2ObjectRotation;

        #region Start
        public void Start() {
            StartTarget();
        }

        public override void StartTarget() {
            InitSensors();
            StartSensors();

            if (physics) {
                StartPhysics();
            }
        }

        private void StartPhysics() {
            //Rigidbody targetRigidbody = targetTransform.gameObject.AddComponent<Rigidbody>();
            //targetRigidbody.isKinematic = true;
            //targetRigidbody.useGravity = false;

            //ConfigurableJoint joint = gameObject.AddComponent<ConfigurableJoint>();
            //joint.connectedBody = targetRigidbody;

            //joint.autoConfigureConnectedAnchor = false;
            //joint.anchor = Vector3.zero;
            //joint.connectedAnchor = Vector3.zero;

            //joint.xMotion = ConfigurableJointMotion.Locked;
            //joint.yMotion = ConfigurableJointMotion.Locked;
            //joint.zMotion = ConfigurableJointMotion.Locked;

            //joint.angularXMotion = ConfigurableJointMotion.Locked;
            //joint.angularYMotion = ConfigurableJointMotion.Locked;
            //joint.angularZMotion = ConfigurableJointMotion.Locked;
        }
        #endregion

        #region Update
        void Update() {
            UpdateTarget();
        }

        public override void UpdateTarget() {
            sensorComponent.UpdateComponent();

            UpdateTransform();
        }

        // See HumanoidSensor.UpdateTargetTransform
        private void UpdateTransform() {
            this.transform.rotation = sensorComponent.transform.rotation * sensor2ObjectRotation;
            this.transform.position = sensorComponent.transform.position + this.transform.rotation * sensor2ObjectPosition;
        }

        public void ShowTrackers(bool shown) {
        }

        #endregion
    }
}
