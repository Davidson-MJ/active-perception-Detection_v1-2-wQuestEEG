using UnityEngine;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#else
using UnityEngine.VR;
#endif

namespace Passer.Humanoid {

    public class UnityVRHand : ArmController {

        #region Start

        public override void Start(HumanoidControl _humanoid, Transform targetTransform) {
            base.Start(_humanoid, targetTransform);

            sensor2TargetRotation = (handTarget.isLeft ? Quaternion.Euler(-90, 90, 0) : Quaternion.Euler(90, -90, 0));
            sensor2TargetPosition = Vector3.zero;
        }

        #endregion

        #region Update

        public override void Update() {
#if hDAYDREAM && UNITY_ANDROID
            // Only for Daydream support at the moment
#if UNITY_2017_2_OR_NEWER
            if (!XRSettings.enabled)
                return;

            XRNode vrNode = handTarget.isLeft ? XRNode.LeftHand : XRNode.RightHand;
#else
            if (!VRSettings.enabled)
                return;

            VRNode vrNode = handTarget.isLeft ? VRNode.LeftHand : VRNode.RightHand;
#endif

            if (target.transform == null)
                return;

            Quaternion localControllerRotation = InputTracking.GetLocalRotation(vrNode);
            if (localControllerRotation != Quaternion.identity) {
                handTarget.hand.target.transform.rotation = handTarget.humanoid.unity.trackerTransform.rotation * localControllerRotation * sensor2TargetRotation;
                handTarget.hand.target.confidence.rotation = 0.7F;
            } 

            Vector3 localControllerPosition = InputTracking.GetLocalPosition(vrNode);
            if (localControllerPosition != Vector3.zero) {
                // Daydream does not have 3DOF tracking...
                //handTarget.hand.target.transform.position = handTarget.humanoid.unity.trackerTransform.TransformPoint(localControllerPosition);
                handTarget.hand.target.transform.position = CalculateHandPosition(handTarget, sensor2TargetPosition);
                handTarget.hand.target.confidence.position = 0.7F;
            }
#endif
        }

        // arm model for 3DOF tracking: position is calculated from rotation
        static public Vector3 CalculateHandPosition(HandTarget handTarget, Vector3 sensor2TargetPosition) {
            Quaternion hipsYRotation = Quaternion.AngleAxis(handTarget.humanoid.hipsTarget.transform.eulerAngles.y, handTarget.humanoid.up);

            Vector3 pivotPoint = handTarget.humanoid.hipsTarget.transform.position + hipsYRotation * (handTarget.isLeft ? new Vector3(-0.25F, 0.15F, -0.05F) : new Vector3(0.25F, 0.15F, -0.05F));
            Quaternion forearmRotation = handTarget.hand.target.transform.rotation * (handTarget.isLeft ? Quaternion.Euler(0, -90, 0) : Quaternion.Euler(0, 90, 0));

            Vector3 localForearmDirection = handTarget.humanoid.hipsTarget.transform.InverseTransformDirection(forearmRotation * Vector3.forward);

            if (localForearmDirection.x < 0 || localForearmDirection.y > 0) {
                pivotPoint += hipsYRotation * Vector3.forward * Mathf.Lerp(0, 0.15F, -localForearmDirection.x * 3 + localForearmDirection.y);
            }
            if (localForearmDirection.y > 0) {
                pivotPoint += hipsYRotation * Vector3.up * Mathf.Lerp(0, 0.2F, localForearmDirection.y);
            }

            if (localForearmDirection.z < 0.2F) {
                localForearmDirection = new Vector3(localForearmDirection.x, localForearmDirection.y, 0.2F);
                forearmRotation = Quaternion.LookRotation(handTarget.humanoid.hipsTarget.transform.TransformDirection(localForearmDirection), forearmRotation * Vector3.up);
            }

            handTarget.hand.target.transform.position = pivotPoint + forearmRotation * Vector3.forward * handTarget.forearm.bone.length;

            Vector3 handPosition = handTarget.hand.target.transform.TransformPoint(-sensor2TargetPosition);

            return handPosition;
        }

        #endregion

    }

}