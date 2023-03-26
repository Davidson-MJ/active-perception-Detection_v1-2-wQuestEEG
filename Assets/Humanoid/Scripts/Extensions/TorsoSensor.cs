using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Passer.Humanoid {

    public class TorsoSensor : HumanoidSensor {
        //protected HipsTarget hipsTarget;
        protected HipsTarget hipsTarget {
            get { return (HipsTarget)target; }
        }
        protected new Humanoid.Tracking.TorsoSensor sensor;

        #region Start
        public virtual void Init(HipsTarget hipsTarget) {
            target = hipsTarget;
        }

        public override void Start(HumanoidControl _humanoid, Transform targetTransform) {
            base.Start(_humanoid, targetTransform);
            target = targetTransform.GetComponent<HipsTarget>();
        }

#if UNITY_EDITOR
        public void InitController(SerializedProperty sensorProp, HipsTarget hipsTarget) {
            if (sensorProp == null)
                return;

            Init(hipsTarget);

            SerializedProperty sensorTransformProp = sensorProp.FindPropertyRelative("sensorTransform");
            sensorTransformProp.objectReferenceValue = sensorTransform;

            SerializedProperty targetProp = sensorProp.FindPropertyRelative("target");
            targetProp.objectReferenceValue = target;

            if (tracker == null || !tracker.enabled || !enabled)
                return;

            CheckSensorTransform();
            sensorTransformProp.objectReferenceValue = sensorTransform;

            ShowSensor(hipsTarget.humanoid.showRealObjects && hipsTarget.showRealObjects);

            SerializedProperty sensor2TargetPositionProp = sensorProp.FindPropertyRelative("sensor2TargetPosition");
            sensor2TargetPositionProp.vector3Value = sensor2TargetPosition;
            SerializedProperty sensor2TargetRotationProp = sensorProp.FindPropertyRelative("sensor2TargetRotation");
            sensor2TargetRotationProp.quaternionValue = sensor2TargetRotation;
        }

        public void RemoveController(SerializedProperty sensorProp) {
            if (sensorProp == null)
                return;

            SerializedProperty sensorTransformProp = sensorProp.FindPropertyRelative("sensorTransform");
            sensorTransformProp.objectReferenceValue = null;
        }
#endif

        protected virtual void CreateSensorTransform(string resourceName, Vector3 sensor2TargetPosition, Quaternion sensor2TargetRotation) {
            CreateSensorTransform(hipsTarget.hips.target.transform, resourceName, sensor2TargetPosition, sensor2TargetRotation);
        }
        #endregion

        #region Update
        /*
        protected virtual void UpdateHipsTarget(HumanoidTarget.TargetTransform target, Transform sensorTransform) {
            if (target.transform == null || sensorTransform == null)
                return;

            target.transform.rotation = GetTargetRotation(sensorTransform);
            target.confidence.rotation = 0.5F;

            target.transform.position = GetHipsTargetPosition(sensorTransform);
            target.confidence.position = 0.5F;
        }

        protected virtual void UpdateHipsTarget(HumanoidTarget.TargetTransform target, SensorComponent sensorComponent) {
            if (target.transform == null || sensorComponent.rotationConfidence + sensorComponent.positionConfidence <= 0)
                return;

            target.transform.rotation = GetTargetRotation(sensorComponent.transform);
            target.confidence.rotation = sensorComponent.rotationConfidence;

            target.transform.position = GetHipsTargetPosition(sensorComponent.transform);
            target.confidence.position = sensorComponent.positionConfidence;
        }

        protected Vector3 GetHipsTargetPosition(Transform sensorTransform) {
            Vector3 targetPosition = sensorTransform.position + sensorTransform.rotation * sensor2TargetRotation * sensor2TargetPosition;
            return targetPosition;
        }

        protected Quaternion GetTargetRotation(Transform sensorTransform) {
            Quaternion targetRotation = sensorTransform.rotation * sensor2TargetRotation;
            return targetRotation;
        }
        */
        #endregion
    }
}