using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Passer.Humanoid {

    public class LegSensor : HumanoidSensor {
        protected FootTarget footTarget {
            get { return (FootTarget)target; }
        }
        protected new Humanoid.Tracking.LegSensor sensor;

        #region Start
        public virtual void Init(FootTarget footTarget) {
            target = footTarget;
        }

        public override void Start(HumanoidControl _humanoid, Transform targetTransform) {
            base.Start(_humanoid, targetTransform);
            target = targetTransform.GetComponent<FootTarget>();
        }

#if UNITY_EDITOR
        public void InitController(SerializedProperty sensorProp, FootTarget footTarget) {
            if (sensorProp == null)
                return;

            Init(footTarget);

            SerializedProperty sensorTransformProp = sensorProp.FindPropertyRelative("sensorTransform");
            sensorTransformProp.objectReferenceValue = sensorTransform;

            SerializedProperty targetProp = sensorProp.FindPropertyRelative("target");
            targetProp.objectReferenceValue = target;

            if (tracker == null || !tracker.enabled || !enabled)
                return;

            CheckSensorTransform();
            sensorTransformProp.objectReferenceValue = sensorTransform;

            ShowSensor(footTarget.humanoid.showRealObjects && footTarget.showRealObjects);

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
            CreateSensorTransform(footTarget.foot.target.transform, resourceName, sensor2TargetPosition, sensor2TargetRotation);
        }
        #endregion

        protected virtual void UpdateUpperLeg(Humanoid.Tracking.LegSensor legSensor) {
            if (footTarget.upperLeg.target.transform != null) {
                if (legSensor.upperLeg.confidence.position > 0)
                    footTarget.upperLeg.target.transform.position = HumanoidTarget.ToVector3(legSensor.upperLeg.position);
                //else
                // footTarget.upperLeg.target.transform.position = footTarget.shoulder.target.transform.position + footTarget.shoulder.target.transform.rotation * footTarget.outward * footTarget.shoulder.bone.length;

                if (legSensor.upperLeg.confidence.rotation > 0)
                    footTarget.upperLeg.target.transform.rotation = HumanoidTarget.ToQuaternion(legSensor.upperLeg.rotation);

                footTarget.upperLeg.target.confidence = legSensor.upperLeg.confidence;
            }
        }

        protected virtual void UpdateLowerLeg(Humanoid.Tracking.LegSensor legSensor) {
            if (footTarget.lowerLeg.target.transform == null)
                return;

            if (legSensor.lowerLeg.confidence.position > 0)
                footTarget.lowerLeg.target.transform.position = HumanoidTarget.ToVector3(legSensor.lowerLeg.position);
            else
                footTarget.lowerLeg.target.transform.position = footTarget.upperLeg.target.transform.position + footTarget.upperLeg.target.transform.rotation * Vector3.down * footTarget.upperLeg.bone.length;

            if (legSensor.lowerLeg.confidence.rotation > 0)
                footTarget.lowerLeg.target.transform.rotation = HumanoidTarget.ToQuaternion(legSensor.lowerLeg.rotation);

            footTarget.lowerLeg.target.confidence = legSensor.lowerLeg.confidence;
        }

        protected virtual void UpdateFoot(Humanoid.Tracking.LegSensor legSensor) {
            if (footTarget.foot.target.transform != null) {
                if (legSensor.foot.confidence.position > 0)
                    footTarget.foot.target.transform.position = HumanoidTarget.ToVector3(legSensor.foot.position);
                else
                    footTarget.foot.target.transform.position = footTarget.lowerLeg.target.transform.position + footTarget.lowerLeg.target.transform.rotation * Vector3.down * footTarget.lowerLeg.bone.length;

                if (legSensor.foot.confidence.rotation > 0)
                    footTarget.foot.target.transform.rotation = HumanoidTarget.ToQuaternion(legSensor.foot.rotation);

                footTarget.foot.target.confidence = legSensor.foot.confidence;
            }
        }
    }
}
