using UnityEngine;

namespace Passer.Humanoid {
    using Passer.Tracking;

    [System.Serializable]
    public class UnityXRHand : ArmController {
#if pUNITYXR
        public override string name {
            get { return "Unity XR"; }
        }

        protected new UnityXR tracker;
        [SerializeField]
        private UnityXRController _unityXrController;
        public UnityXRController unityXrController {
            get {
                if (_unityXrController == null) {
                    if (tracker == null && handTarget.humanoid != null && handTarget.humanoid.unityXR != null)
                        tracker = handTarget.humanoid.unityXR.tracker;
                    if (tracker != null)
                        _unityXrController = handTarget.isLeft ? tracker.leftController : tracker.rightController;
                }
                return _unityXrController;
            }
            set {
                _unityXrController = value;
            }
        }

        #region Manage

        public override void CheckSensor(HandTarget handTarget) {
#if pUNITYXR
            if (this.handTarget == null)
                this.target = handTarget;
            if (this.handTarget == null)
                return;

            if (handTarget.humanoid.unity == null)
                handTarget.humanoid.unityXR.CheckTracker(handTarget.humanoid);

            if (tracker == null)
                tracker = handTarget.humanoid.unity;

            if (enabled && tracker != null && tracker.enabled) {
                if (unityXrController == null) {
#if hWINDOWSMR
                    Vector3 position = handTarget.transform.TransformPoint(handTarget.isLeft ? -0.1F : 0.1F, -0.04F, 0.04F);
                    Quaternion localRotation = handTarget.isLeft ? Quaternion.Euler(180, 120, 90) : Quaternion.Euler(180, -120, -90);
#else
                    Vector3 position = handTarget.transform.TransformPoint(handTarget.isLeft ? -0.1F : 0.1F, -0.05F, 0.04F);
                    Quaternion localRotation = handTarget.isLeft ? Quaternion.Euler(180, 90, 90) : Quaternion.Euler(180, -90, -90);
#endif
                    Quaternion rotation = handTarget.transform.rotation * localRotation;
																														 
                    unityXrController = handTarget.humanoid.unity.GetController(handTarget.isLeft, position, rotation);
                }
                if (unityXrController != null)
                    sensorTransform = unityXrController.transform;

                if (!Application.isPlaying)
                    SetSensor2Target();
            }
            else {
#if UNITY_EDITOR
                if (!Application.isPlaying) {
                    if (unityXrController != null)
                        Object.DestroyImmediate(unityXrController.gameObject, true);
                }
#endif
                unityXrController = null;
                sensorTransform = null;
            }
#endif
        }

        #endregion

        #region Start

        public override void Start(HumanoidControl _humanoid, Transform targetTransform) {
            base.Start(_humanoid, targetTransform);
            tracker = handTarget.humanoid.unity;

            if (tracker == null || tracker.enabled == false || enabled == false)
                return;

#if hWINDOWSMR
            Vector3 position = handTarget.transform.TransformPoint(handTarget.isLeft ? -0.1F : 0.1F, -0.04F, 0.04F);
            Quaternion localRotation = handTarget.isLeft ? Quaternion.Euler(180, 120, 90) : Quaternion.Euler(180, -120, -90);
#else
            Vector3 position = handTarget.transform.TransformPoint(handTarget.isLeft ? -0.1F : 0.1F, -0.05F, 0.04F);
            Quaternion localRotation = handTarget.isLeft ? Quaternion.Euler(180, 90, 90) : Quaternion.Euler(180, -90, -90);
#endif
            Quaternion rotation = handTarget.transform.rotation * localRotation;

            unityXrController = tracker.GetController(handTarget.isLeft, position, rotation);
            if (unityXrController != null) {
                unityXrController.show = handTarget.humanoid.showRealObjects && handTarget.showRealObjects;
                sensorTransform = unityXrController.transform;
            }

            SetSensor2Target();
        }

        #endregion

        #region Update

        public override void Update() {
            if (tracker == null || tracker.enabled == false || enabled == false)
                return;

            if (unityXrController != null)
                unityXrController.UpdateComponent();


            UpdateTarget(handTarget.hand.target, unityXrController);
            UpdateInput();
        }

        #region Controller

        private void UpdateInput() {
            if (controllerInput == null)
                return;

            if (handTarget.isLeft)
                UpdateInputSide(controllerInput.left);
            else
                UpdateInputSide(controllerInput.right);
        }

        private void UpdateInputSide(ControllerSide controllerInputSide) {
            controllerInputSide.stickHorizontal = unityXrController.primaryAxis.x;
            controllerInputSide.stickVertical = unityXrController.primaryAxis.y;
            controllerInputSide.stickButton |= (unityXrController.primaryAxis.z > 0.5F);
            controllerInputSide.stickTouch |= (unityXrController.primaryAxis.z > -0.5F);

            controllerInputSide.touchpadHorizontal = unityXrController.secondaryAxis.x;
            controllerInputSide.touchpadVertical = unityXrController.secondaryAxis.y;
            controllerInputSide.touchpadPress |= (unityXrController.secondaryAxis.z > 0.5F);
            controllerInputSide.touchpadTouch |= (unityXrController.secondaryAxis.z > -0.5F);

            controllerInputSide.buttons[0] |= (unityXrController.primaryButton > 0.5F);
            controllerInputSide.buttons[1] |= (unityXrController.secondaryButton > 0.5F);

            controllerInputSide.trigger1 = unityXrController.trigger;
            controllerInputSide.trigger2 = unityXrController.grip;
            controllerInputSide.option = unityXrController.menu > 0;
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


        #endregion Controller

        #endregion
#endif
    }

}