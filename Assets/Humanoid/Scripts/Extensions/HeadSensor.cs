using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Passer.Humanoid {

    public class HeadSensor : HumanoidSensor {
        protected HeadTarget headTarget {
            get { return (HeadTarget)target; }
        }
        protected new Humanoid.Tracking.HeadSensor sensor;

        #region Manage

        public virtual void CheckSensor(HeadTarget headTarget) {
            if (this.headTarget == null)
                this.target = headTarget;
        }

        #endregion

        #region Start
        public virtual void Init(HeadTarget headTarget) {
            target = headTarget;
        }

        public override void Start(HumanoidControl _humanoid, Transform targetTransform) {
            target = targetTransform.GetComponent<HeadTarget>();
            base.Start(_humanoid, targetTransform);
        }

#if UNITY_EDITOR
        public void InitController(SerializedProperty sensorProp, HeadTarget target) {
            if (sensorProp == null)
                return;

            Init(target);

            SerializedProperty sensorTransformProp = sensorProp.FindPropertyRelative("sensorTransform");
            sensorTransformProp.objectReferenceValue = sensorTransform;

            SerializedProperty targetProp = sensorProp.FindPropertyRelative("target");
            targetProp.objectReferenceValue = base.target;

            if (tracker == null || !tracker.enabled || !enabled)
                return;

            CheckSensorTransform();
            ShowSensor(target.humanoid.showRealObjects && target.showRealObjects);

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
            CreateSensorTransform(headTarget.head.target.transform, resourceName, sensor2TargetPosition, sensor2TargetRotation);
        }
        #endregion

        #region Update
        public override void Update() {
            if (tracker == null || !tracker.enabled || !enabled)
                return;

            status = sensor.Update();
            UpdateSensorTransform(sensor);

            if (status != Tracker.Status.Tracking)
                return;

            UpdateHeadTargetTransform(sensor);
        }

        protected virtual void UpdateHeadTargetTransform(Humanoid.Tracking.HeadSensor headTracker) {
            if (headTarget.head.target.transform != null) {
                if (headTracker.head.confidence.rotation > 0)
                    headTarget.head.target.transform.rotation = HumanoidTarget.ToQuaternion(headTracker.head.rotation) * sensor2TargetRotation;
                if (headTracker.head.confidence.position > 0)
                    headTarget.head.target.transform.position = HumanoidTarget.ToVector3(headTracker.head.position) + headTarget.head.target.transform.rotation * sensor2TargetPosition;
                headTarget.head.target.confidence = headTracker.head.confidence;
            }
        }

        virtual protected void UpdateNeckTargetFromHead() {
            Vector3 headPosition = headTarget.head.target.transform.position;
            Quaternion headRotation = headTarget.head.target.transform.rotation;

            headTarget.neck.target.transform.rotation = headTarget.head.target.transform.rotation;
            headTarget.neck.target.transform.position = headPosition - headTarget.neck.target.transform.rotation * Vector3.up * headTarget.neck.bone.length;

            headTarget.head.target.transform.position = headPosition;
            headTarget.head.target.transform.rotation = headRotation;
        }
        #endregion
    }
}