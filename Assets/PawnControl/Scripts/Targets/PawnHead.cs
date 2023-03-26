using UnityEngine;

namespace Passer.Pawn {
    /// <summary>
    /// Pawn Control options for head related items
    /// </summary>
    public class PawnHead : Target {

        protected PawnControl _pawn;
        /// <summary>
        /// The Pawn for this head
        /// </summary>
        public PawnControl pawn {
            get {
#if UNITY_EDITOR
                if (_pawn == null)
                    _pawn = GetPawn();
#endif
                return _pawn;
            }
        }

        #region Manage

        private PawnControl GetPawn() {
            PawnControl[] pawns = FindObjectsOfType<PawnControl>();
            PawnControl foundPawn = null;

            for (int i = 0; i < pawns.Length; i++)
                if (pawns[i].headTarget == this)
                    foundPawn = pawns[i];

            return foundPawn;
        }

        /// <summary>
        /// Create a new CameraTarget GameObject for the pawn
        /// </summary>
        /// <param name="pawn">The pawn for which the CameraTarget needs to be created</param>
        /// <returns>The created CameraTarget</returns>
        /// The CameraTarget will be created as direct child of the pawn.
        public static PawnHead Create(PawnControl pawn) {
            GameObject targetObject = new GameObject() {
                name = "Head Target"
            };
            Transform targetTransform = targetObject.transform;

            targetTransform.parent = pawn.transform;

            targetTransform.localPosition = new Vector3(0, pawn.neckHeight, 0);
            targetTransform.localRotation = Quaternion.identity;

            PawnHead cameraTarget = targetTransform.gameObject.AddComponent<PawnHead>();
            cameraTarget._pawn = pawn;

            return cameraTarget;
        }

        public virtual void CheckSensors() {
#if pUNITYXR
            Tracking.UnityXR.RemoveCamera(this.transform);

            if (pawn == null || pawn.unity == null)
                return;

            if (enabled && pawn.unity.enabled) {
                if (unityCamera == null) {
                    Vector3 position = transform.TransformPoint(neck2eyes);
                    Quaternion rotation = transform.rotation;
                    unityCamera = pawn.unity.GetHmd(position, rotation);
                }

                //if (!Application.isPlaying)
                //    SetSensor2Target();
            }
            else {
#if UNITY_EDITOR
                if (!Application.isPlaying) {
                    if (unityCamera != null)
                        Object.DestroyImmediate(unityCamera.gameObject, true);
                }
#endif
                unityCamera = null;
            }
#endif
        }

        #endregion

        #region Sensors

#if pUNITYXR
        public Tracking.UnityXRHmd unityCamera;
#elif hLEGACYXR
        public UnityCamera unityCamera = new UnityCamera();
#endif

        public override void InitSensors() {
            if (pawn == null)
                return;

#if pUNITYXR
            Tracking.UnityXR.RemoveCamera(this.transform);

            unityCamera = pawn.unity.hmd;
            if (unityCamera == null) {
                Vector3 position = transform.TransformPoint(neck2eyes);
                Quaternion rotation = transform.rotation;
                unityCamera = pawn.unity.GetHmd(position, rotation);
            }
#endif
        }

#if hLEGACYXR
        public override void StartSensors() {
            unityCamera.Start(this.transform);
        }

        protected override void UpdateSensors() {
            unityCamera.Update();
        }
#endif

        public bool show {
            set {
#if hLEGACYXR
                if (unityCamera != null) { }                    //unity.Show(value && _showRealObjects);
#endif
            }
        }

        #endregion

        #region Configuration

        protected Vector3 neck2eyes;

        public virtual Vector3 neckEyeDelta {
            get {
#if pUNITYXR
                if (unityCamera.transform != null) {
                    Vector3 localCameraPosition = this.transform.InverseTransformPoint(unityCamera.transform.position);
                    return localCameraPosition;
                } else
#endif
#if hLEGACYXR
                if (unityCamera.sensorTransform != null) {
                    Vector3 localCameraPosition = this.transform.InverseTransformPoint(unityCamera.sensorTransform.position);
                    return localCameraPosition;
                }
                else
#endif
                {
                    Camera camera = GetComponentInChildren<Camera>();
                    if (camera != null) {
                        Vector3 localCameraPosition = this.transform.InverseTransformPoint(camera.transform.position);
                        return localCameraPosition;
                    }
                    else {
                        return Vector3.zero;
                    }
                }
                //return new Vector3(0, 0, pawn.radius);
                //CharacterController characterController = pawn.GetComponent<CharacterController>();
                //if (characterController != null) {
                //    Vector3 neckPosition = characterController.transform.position + characterController.center + Vector3.up * characterController.height / 4;
                //    Vector3 neck2eyes = this.transform.position - neckPosition;
                //    return neck2eyes;
                //}
                //return new Vector3(0, 0.15F, 0.15F);
            }
        }

        public virtual Vector3 neckPosition {
            get {
                Vector3 neckPosition = transform.TransformPoint(-neck2eyes);
                return neckPosition;
            }
        }

        #endregion Configuration

        #region Init

        public void Init(PawnControl pawn) {
            this._pawn = pawn;
        }

        public override void StartTarget() {
            InitSensors();

            neck2eyes = neckEyeDelta;
        }

        #endregion

        #region Update

        protected bool calibrated = false;

        /// <summary>Update all head sensors</summary>
        public override void UpdateTarget() {
            UpdateSensors();

#if pUNITYXR
            if (!pawn.isRemote && unityCamera.transform != null) 
#endif
#if hLEGACYXR
            if (!pawn.isRemote && unityCamera.sensorTransform != null)
#endif
            {
#if hLEGACY
                if (UnityTracker.xrDevice != UnityTracker.XRDeviceType.None) {
                    if (unityCamera.status == Tracker.Status.Tracking)
                        UpdateHeadTargetFromCamera();
                    else
                        UpdateCameraParentFromHeadTarget();
                }
                else
#endif
                UpdateHeadTargetFromCamera();
            }
        }

        protected void UpdateHeadTargetFromCamera() {
            // Don't do this, it interferes with the PawnControl.CalculateMovement()
            // Where, how? Without this, the head won't follow the camera
            // (and I don't see any issues now...)
#if hLEGACYXR
            transform.rotation = unityCamera.sensorTransform.rotation;
            Vector3 cameraPosition = unityCamera.sensorTransform.position;
            transform.position = unityCamera.sensorTransform.TransformPoint(new Vector3(0, 0, -pawn.radius));

            // restore the camera position because it can be a child of the head
            unityCamera.sensorTransform.position = cameraPosition;
#endif
        }

        protected void UpdateCameraFromHeadTarget() {
#if hLEGACYXR
            unityCamera.sensorTransform.rotation = transform.rotation;
            unityCamera.sensorTransform.position = transform.TransformPoint(new Vector3(0, 0, pawn.radius));
#endif
        }

        // This is used for tracked camera's because we cannot update the camera transform then
        protected void UpdateCameraParentFromHeadTarget() {
#if hLEGACYXR
            Quaternion cameraRotation = transform.rotation;
            Vector3 cameraPosition = transform.TransformPoint(new Vector3(0, 0, pawn.radius));

            unityCamera.sensorTransform.parent.rotation *= Quaternion.Inverse(unityCamera.sensorTransform.rotation) * cameraRotation;
            unityCamera.sensorTransform.parent.position += cameraPosition - unityCamera.sensorTransform.position;
#endif
        }

        #endregion

        #region Pose

        /// <summary>Sets the rotation of the HMD around the X axis</summary>
        public void RotationX(float xAngle) {
            Quaternion localHmdRotation = Quaternion.Inverse(_pawn.transform.rotation) * transform.rotation;
            Vector3 angles = localHmdRotation.eulerAngles;
            transform.rotation = _pawn.transform.rotation * Quaternion.Euler(xAngle, angles.y, angles.z);
        }

        /// <summary>Sets the rotation of the HMD around the Y axis</summary>
        public void RotationY(float yAngle) {
            Vector3 neckPosition = transform.TransformPoint(-neck2eyes);

            Quaternion localHmdRotation = Quaternion.Inverse(_pawn.transform.rotation) * transform.rotation;
            Vector3 angles = localHmdRotation.eulerAngles;
            transform.rotation = _pawn.transform.rotation * Quaternion.Euler(angles.x, yAngle, angles.z);

            transform.position = neckPosition + transform.rotation * neck2eyes;
        }

        #endregion

    }
}