using Passer.Humanoid;
using UnityEngine;

namespace Passer {

    public class CalibrationPoint : MonoBehaviour {

        public Transform leftCalibrationPoint;
        public Transform rightCalibrationPoint;

        public bool leftDetected = false;
        public bool rightDetected = false;

        public float rotationMargin = 5;
        public float positionMargin = 0.01F;

        protected HumanoidControl humanoid;
        protected Transform leftController;
        protected Transform rightController;
        protected LineRenderer leftLineRenderer;
        protected LineRenderer rightLineRenderer;

        virtual protected void Awake() {
            humanoid = GetComponentInParent<HumanoidControl>();
        }

        virtual protected void Update() {
            if (humanoid == null || leftCalibrationPoint == null || rightCalibrationPoint == null)
                return;

#if hOCULUS &&  (UNITY_STANDALONE_WIN || UNITY_ANDROID)
            if (leftController == null) 
                leftController = humanoid.leftHandTarget.oculus.sensorTransform;            
            if (rightController == null) 
                rightController = humanoid.rightHandTarget.oculus.sensorTransform;
            
            if (leftLineRenderer == null && leftController != null && rightController != null) {
                Vector3 localPosition = leftCalibrationPoint.InverseTransformPoint(rightCalibrationPoint.position);
                localPosition = Vector3.Scale(localPosition, rightCalibrationPoint.lossyScale);
                leftLineRenderer = AddLineRenderer(leftController.gameObject, localPosition);
            }
            if (rightLineRenderer == null && leftController != null && rightController != null) {
                Vector3 localPosition = rightCalibrationPoint.InverseTransformPoint(leftCalibrationPoint.position);
                localPosition = Vector3.Scale(localPosition, rightCalibrationPoint.lossyScale);
                rightLineRenderer = AddLineRenderer(rightController.gameObject, localPosition);
            }
#endif

            if (leftController == null || rightController == null)
                return;

            leftDetected = DetectLeftFromRight();
            rightDetected = DetectRightFromLeft();

            if (leftDetected && rightDetected) {
                Vector3 direction = rightController.position - leftController.position;
                Vector3 calibrationDirection = rightCalibrationPoint.position - leftCalibrationPoint.position;
                Quaternion rotation = Quaternion.FromToRotation(direction, calibrationDirection);

                humanoid.AdjustTracking(Vector3.zero, rotation);

                Vector3 centerControllerPosition = (leftController.position + rightController.position) / 2;
                Vector3 centerCalibrationPosition = (leftCalibrationPoint.position + rightCalibrationPoint.position) / 2;
                Vector3 translation = centerCalibrationPosition - centerControllerPosition;

                humanoid.AdjustTracking(translation, Quaternion.identity);

                this.gameObject.SetActive(false);
                Destroy(leftLineRenderer);
                Destroy(rightLineRenderer);
            }
        }

        public void StartCalibrate() {
            this.gameObject.SetActive(true);
        }

        virtual protected bool DetectRightFromLeft() {
            Vector3 calibrationPosition = leftCalibrationPoint.InverseTransformPoint(rightCalibrationPoint.position);
            calibrationPosition = Vector3.Scale(calibrationPosition, leftCalibrationPoint.lossyScale);
            Vector3 sollPosition = leftController.TransformPoint(calibrationPosition);

            float distance = Vector3.Distance(sollPosition, rightController.position);
            return distance < positionMargin;
        }

        virtual protected bool DetectLeftFromRight() {
            Vector3 calibrationPosition = rightCalibrationPoint.InverseTransformPoint(leftCalibrationPoint.position);
            calibrationPosition = Vector3.Scale(calibrationPosition, rightCalibrationPoint.lossyScale);
            Vector3 sollPosition = rightController.TransformPoint(calibrationPosition);

            float distance = Vector3.Distance(sollPosition, leftController.position);
            return distance < positionMargin;
        }

        virtual protected LineRenderer AddLineRenderer(GameObject obj, Vector3 vector) {
            LineRenderer renderer = obj.AddComponent<LineRenderer>();

            renderer.useWorldSpace = false;
            renderer.widthMultiplier = 0.01F;
            renderer.SetPosition(1, vector);
            return renderer;
        }
    }
}