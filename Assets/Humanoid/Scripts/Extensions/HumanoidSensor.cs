using UnityEngine;

namespace Passer.Humanoid { 
    [System.Serializable]
    public class UnitySensor : Sensor {

        public UnitySensor() {
            enabled = true;
        }

        public new HumanoidTracker tracker;

        protected Tracking.Sensor sensor;

        [System.NonSerialized]
        public const string _name = "";
        public override string name { get { return _name; } }

        //public Transform sensorTransform;

        public Vector3 sensor2TargetPosition;
        public Quaternion sensor2TargetRotation;

        #region Start
        public virtual void Init(HumanoidTracker _tracker) {
            tracker = _tracker;
        }

        public virtual void Start(HumanoidControl _humanoid, Transform targetTransform) {
            target = targetTransform.GetComponent<Target>();
        }

        public virtual void CheckSensorTransform() {
            if (enabled && sensorTransform == null)
                CreateSensorTransform();
            else if (!enabled && sensorTransform != null)
                RemoveSensorTransform();

            if (sensor2TargetRotation.x + sensor2TargetRotation.y + sensor2TargetRotation.z + sensor2TargetRotation.w == 0)
                SetSensor2Target();
        }

        protected virtual void CreateSensorTransform() {
        }

        protected void CreateSensorTransform(Transform targetTransform, string resourceName, Vector3 _sensor2TargetPosition, Quaternion _sensor2TargetRotation) {
            GameObject sensorObject;
            if (resourceName == null) {
                sensorObject = new GameObject("Sensor");
            }
            else {
                Object controllerPrefab = Resources.Load(resourceName);
                if (controllerPrefab == null)
                    sensorObject = new GameObject("Sensor");
                else
                    sensorObject = (GameObject)Object.Instantiate(controllerPrefab);

                sensorObject.name = resourceName;
            }

            sensorTransform = sensorObject.transform;
            sensorTransform.parent = tracker.trackerTransform;

            sensor2TargetPosition = -_sensor2TargetPosition;
            sensor2TargetRotation = Quaternion.Inverse(_sensor2TargetRotation);

            UpdateSensorTransformFromTarget(targetTransform);
        }

        protected void RemoveSensorTransform() {
            if (Application.isPlaying)
                Object.Destroy(sensorTransform.gameObject);
            else
                Object.DestroyImmediate(sensorTransform.gameObject, true);
        }

        public virtual void SetSensor2Target() {
            if (sensorTransform == null || target == null)
                return;

            sensor2TargetRotation = Quaternion.Inverse(sensorTransform.rotation) * target.transform.rotation;
            sensor2TargetPosition = -InverseTransformPointUnscaled(target.transform, sensorTransform.position);
        }

        public static Vector3 InverseTransformPointUnscaled(Transform transform, Vector3 position) {
            var worldToLocalMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one).inverse;
            return worldToLocalMatrix.MultiplyPoint3x4(position);
        }
        #endregion

        #region Update
        public override void Update() {
            if (tracker == null || !tracker.enabled || !enabled)
                return;

            sensor.Update();
            if (sensor.status != Tracker.Status.Tracking)
                return;

            UpdateSensorTransform(sensor);
            UpdateTargetTransform();
        }

        protected void UpdateSensorTransform(Tracking.Sensor sensor) {
            if (sensorTransform == null)
                return;

            if (status == Tracker.Status.Tracking) {
                sensorTransform.gameObject.SetActive(true);
                sensorTransform.position = HumanoidTarget.ToVector3(sensor.sensorPosition);
                sensorTransform.rotation = HumanoidTarget.ToQuaternion(sensor.sensorRotation);
            }
            else {
                sensorTransform.gameObject.SetActive(false);
            }
        }

        public virtual void UpdateSensorTransformFromTarget(Transform targetTransform) {
            if (sensorTransform == null)
                return;

            sensorTransform.position = TransformPointUnscaled(targetTransform, -sensor2TargetPosition);
            sensorTransform.rotation = targetTransform.rotation * Quaternion.Inverse(sensor2TargetRotation);
        }

        protected static Vector3 TransformPointUnscaled(Transform transform, Vector3 position) {
            var localToWorldMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            return localToWorldMatrix.MultiplyPoint3x4(position);
        }

        protected virtual void UpdateTargetTransform() {
            target.transform.rotation = sensorTransform.rotation * sensor2TargetRotation;
            target.transform.position = sensorTransform.position + target.transform.rotation * sensor2TargetPosition;
        }
        #endregion

        #region Stop
        public virtual void Stop() { }
        #endregion

        public virtual void RefreshSensor() {
        }

        public virtual void ShowSensor(HumanoidTarget target, bool shown) { }
    }

    public class HumanoidSensor : UnitySensor {
        protected virtual void UpdateTarget(HumanoidTarget.TargetTransform target, Transform sensorTransform) {
            if (target.transform == null || sensorTransform == null)
                return;

            target.transform.rotation = GetTargetRotation(sensorTransform);
            target.confidence.rotation = 0.5F;

            target.transform.position = GetTargetPosition(sensorTransform);
            target.confidence.position = 0.5F;
        }

        protected virtual void UpdateTarget(HumanoidTarget.TargetTransform target, SensorComponent sensorComponent) {
            if (target.transform == null || sensorComponent.rotationConfidence + sensorComponent.positionConfidence <= 0)
                return;

            target.transform.rotation = GetTargetRotation(sensorComponent.transform);
            target.confidence.rotation = sensorComponent.rotationConfidence;

            target.transform.position = GetTargetPosition(sensorComponent.transform);
            target.confidence.position = sensorComponent.positionConfidence;
        }

        protected Vector3 GetTargetPosition(Transform sensorTransform) {
            Vector3 targetPosition = sensorTransform.position + sensorTransform.rotation * sensor2TargetRotation * sensor2TargetPosition;
            return targetPosition;
        }

        protected Quaternion GetTargetRotation(Transform sensorTransform) {
            Quaternion targetRotation = sensorTransform.rotation * sensor2TargetRotation;
            return targetRotation;
        }
    }

}