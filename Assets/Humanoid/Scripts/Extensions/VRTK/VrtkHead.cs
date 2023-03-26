#if hVRTK
using UnityEngine;

namespace Passer.Humanoid {

    [System.Serializable]
    public class VrtkHead : HeadSensor {
        public override string name {
            get { return "VRTK"; }
        }

#region Start
        public override void Init(HeadTarget _headTarget) {
            base.Init(_headTarget);
            tracker = headTarget.humanoid.vrtk;
        }

        public override void Start(HumanoidControl _humanoid, Transform targetTransform) {
            base.Start(_humanoid, targetTransform);
            Init(headTarget);

            if (tracker == null || !tracker.enabled || !enabled)
                return;

            sensor2TargetPosition = -headTarget.head2eyes;
            sensor2TargetRotation = Quaternion.identity;
        }
#endregion

#region Update
        public override void Update() {
            if (tracker == null || !tracker.enabled || !enabled)
                return;

            UpdateHeadTargetTransform();
            UpdateNeckTargetFromHead();
        }

        private void UpdateHeadTargetTransform() {
            if (sensorTransform == null || headTarget.head.target.transform == null)
                return;

            headTarget.head.target.transform.rotation = sensorTransform.rotation * sensor2TargetRotation;
            headTarget.head.target.transform.position = sensorTransform.position + headTarget.head.target.transform.rotation * sensor2TargetPosition;
            headTarget.head.target.confidence.position = 1;
            headTarget.head.target.confidence.rotation = 1;
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
#endif