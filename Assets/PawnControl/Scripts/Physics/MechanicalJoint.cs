using UnityEngine;

namespace Passer {

    /// <summary>Mechanical Joints can be used to limit the movements of a Kinematic Rigidbody in local space.</summary>
    [HelpURLAttribute("https://passervr.com/documentation/humanoid-control/grabbing-objects/rigidbody-limitations/")]
    public class MechanicalJoint : MonoBehaviour {
        protected Rigidbody rb;
        protected Transform t;
        public Transform parent;

        /// <summary>When true, the local X-position is limited</summary>
        /// The limits are set by the X member of the minLocalPosition and maxLocalPosition
        /// When the bounds are both zero, the local X position is locked.
        public bool limitX = true;
        /// <summary>When true, the local Y-position is limited</summary>
        /// The limits are set by the Y member of the minLocalPosition and maxLocalPosition
        /// When the bounds are both zero, the local Y position is locked.
        public bool limitY = true;
        /// <summary>When true, the local Z-position is limited</summary>
        /// The limits are set by the Z member of the minLocalPosition and maxLocalPosition
        /// When the bounds are both zero, the local Z position is locked.
        public bool limitZ = true;

        /// <summary>The base position for the bounds</summary>
        /// The minLocalPosition and maxLocalPosition are relative to the basePosition.
        public Vector3 basePosition;
        /// <summary>The minimum local position.</summary>
        /// This localPosition is relative to the basePosition.
        public Vector3 minLocalPosition;
        /// <summary>The maximum local position.</summary>
        /// This localPosition is relative to the basePosition.
        public Vector3 maxLocalPosition;

        /// <summary>Linearly interpolates between min and maxLocalPostion.x</summary>
        /// 0 matches the minLocalPosition on the X axis
        /// 1 matches the maxLocalPosition on the X axis
        public void LerpX(float f) {
            if (!limitX)
                return;

            Vector3 localPosition = GetLocalPosition();

            float positionX = Mathf.Lerp(minLocalPosition.x, maxLocalPosition.x, f);
            localPosition = new Vector3(positionX, localPosition.y, localPosition.z);

            transform.position = GetWorldPosition(localPosition);
        }

        /// <summary>Linearly interpolates between min and maxLocalPostion.y</summary>
        /// 0 matches the minLocalPosition on the Y axis
        /// 1 matches the maxLocalPosition on the Y axis
        public void LerpY(float f) {
            if (!limitY)
                return;

            Vector3 localPosition = GetLocalPosition();

            float positionY = Mathf.Lerp(minLocalPosition.y, maxLocalPosition.y, f);
            localPosition = new Vector3(localPosition.x, positionY, localPosition.z);

            transform.position = GetWorldPosition(localPosition);
        }

        /// <summary>Linearly interpolates between min and maxLocalPostion.z</summary>
        /// 0 matches the minLocalPosition on the Z axis
        /// 1 matches the maxLocalPosition on the Z axis
        public void LerpZ(float f) {
            if (!limitZ)
                return;

            Vector3 localPosition = GetLocalPosition();

            float positionZ = Mathf.Lerp(minLocalPosition.z, maxLocalPosition.z, f);
            localPosition = new Vector3(localPosition.x, localPosition.y, positionZ);

            transform.position = GetWorldPosition(localPosition);
        }

        protected Vector3 GetLocalPosition() {
            Vector3 localPosition = transform.localPosition;
            Quaternion localRotation = transform.localRotation;
            if (parent != null) {
                localPosition = parent.InverseTransformPoint(transform.position);
                localRotation = Quaternion.Inverse(parent.rotation) * transform.rotation;
            }

            Vector3 localPos = Quaternion.Inverse(localRotation) * (localPosition - basePosition);
            return localPos;
        }

        protected Vector3 GetWorldPosition(Vector3 localPosition) {
            Quaternion localRotation = transform.localRotation;
            if (parent != null) {
                localRotation = Quaternion.Inverse(parent.rotation) * transform.rotation;
            }

            Vector3 position = basePosition + localRotation * localPosition;
            Vector3 worldPosition = parent != null ? parent.TransformPoint(position) : position;
            return worldPosition;
        }

        /// <summary>The base rotation for the bounds</summary>
        public Quaternion baseRotation = Quaternion.identity;

        /// <summary>When the the local rotation is limited</summary>
        /// The limitation is determined by a maxLocalAngle around the limitAnglesAxis.
        /// With this limitation, the rigidbody can only rotated around the limmitAngleAxis.
        public bool limitAngle = true;
        /// <summary>The maximum angle around the limitAngleAxis</summary>
        /// When this is zero, the local rotation is locked.
        public float minLocalAngle;
        /// <summary>The maximum angle around the limitAngleAxis</summary>
        /// When this is zero, the local rotation is locked.
        public float maxLocalAngle;
        /// <summary>The axis around which the Rigidbody can rotate</summary>
        public Vector3 limitAngleAxis = Vector3.up;

        public enum RotationMethod {
            AngleDifference,
            PositionDifference
        }
        public RotationMethod rotationMethod;


        #region Init

        protected virtual void Awake() {
            t = GetComponent<Transform>();
            parent = t.parent;
        }

        protected virtual void Start() {
            gameObjectEvent.value = this.gameObject;
        }

        #endregion

        #region Update

        virtual protected void FixedUpdate() {
            if (speed.sqrMagnitude != 0) {
                float speedX = ((speed.x < 0 && xValue > 0) || (speed.x > 0 && xValue < 1)) ? speed.x : 0;
                float speedY = ((speed.y < 0 && yValue > 0) || (speed.y > 0 && yValue < 1)) ? speed.y : 0;
                float speedZ = ((speed.z < 0 && zValue > 0) || (speed.z > 0 && zValue < 1)) ? speed.z : 0;
                speed = new Vector3(speedX, speedY, speedZ);

                transform.position = transform.position + transform.rotation * speed * Time.fixedDeltaTime;
            }

            if (rotationSpeed != 0) {
                if ((rotationSpeed < 0 && angleValue <= 0) || (rotationSpeed > 0 && angleValue >= 1))
                    rotationSpeed = 0;

                transform.rotation *= Quaternion.AngleAxis(rotationSpeed * Time.fixedDeltaTime, limitAngleAxis);
            }

            if (rb == null) {
                rb = GetComponent<Rigidbody>();
                if (rb == null)
                    return;
            }

            Vector3 correctionTranslation = GetCorrectionVector();
            rb.transform.position = rb.position + correctionTranslation;

            Quaternion correctionRotation = GetCorrectionAxisRotation();
            rb.transform.rotation = rb.rotation * correctionRotation;

            UpdateEvents();
        }

        public Vector3 GetCorrectionVector() {
            Vector3 localPosition = GetLocalPosition();

            float x = limitX ? Mathf.Clamp(localPosition.x, minLocalPosition.x, maxLocalPosition.x) : localPosition.x;
            float y = limitY ? Mathf.Clamp(localPosition.y, minLocalPosition.y, maxLocalPosition.y) : localPosition.y;
            float z = limitZ ? Mathf.Clamp(localPosition.z, minLocalPosition.z, maxLocalPosition.z) : localPosition.z;
            localPosition = new Vector3(x, y, z);

            Vector3 worldPosition = GetWorldPosition(localPosition);

            xValue = (x - minLocalPosition.x) / (maxLocalPosition.x - minLocalPosition.x);
            yValue = (y - minLocalPosition.y) / (maxLocalPosition.y - minLocalPosition.y);
            zValue = (z - minLocalPosition.z) / (maxLocalPosition.z - minLocalPosition.z);

            Vector3 correctionVector = worldPosition - transform.position;
            return correctionVector;
        }

        public Quaternion GetCorrectionRotation() {
            Quaternion correctionRotation = Quaternion.identity;

            if (parent == null)
                correctionRotation = Quaternion.Inverse(transform.rotation) * baseRotation;
            else
                correctionRotation = Quaternion.Inverse(transform.rotation) * parent.rotation * baseRotation;

            return correctionRotation;
        }

        public Quaternion GetCorrectionAxisRotation() { // V0gue
            Quaternion localRotation = Quaternion.Inverse(baseRotation) * t.rotation;
            if (parent != null)
                localRotation = Quaternion.Inverse(baseRotation) * Quaternion.Inverse(parent.rotation) * t.rotation;

            Quaternion twist = GetTwist(localRotation, limitAngleAxis);

            Quaternion clampedLocalRotation;

            Vector3 twistAxis;
            float twistAngle;
            twist.ToAngleAxis(out twistAngle, out twistAxis);

            if (limitAngle == false || twistAngle != 0) {
                float angle = Vector3.Angle(limitAngleAxis, twistAxis);
                twistAngle = UnityAngles.Normalize(twistAngle);

                if ((angle < 90 && twistAngle > 0) || (angle > 90 && twistAngle < 0))
                    clampedLocalRotation = Quaternion.RotateTowards(Quaternion.identity, twist, maxLocalAngle);
                else
                    clampedLocalRotation = Quaternion.RotateTowards(Quaternion.identity, twist, -minLocalAngle);
            }
            else
                clampedLocalRotation = localRotation;

            //Quaternion clampedLocalRotation = limitAngle ? Quaternion.RotateTowards(Quaternion.identity, twist, maxLocalAngle) : localRotation;

            // This check will prevent instability when rotation > 90 degrees
            if (Quaternion.Angle(localRotation, clampedLocalRotation) == 0)
                clampedLocalRotation = localRotation;

            calculateAngleValue(clampedLocalRotation);

            Quaternion rbRotation;

            if (parent != null)
                rbRotation = parent.rotation * baseRotation * clampedLocalRotation;
            else
                rbRotation = baseRotation * clampedLocalRotation;

            Quaternion correctionRotation = Quaternion.Inverse(rb.rotation) * rbRotation;

            return correctionRotation;
        }

        public Quaternion GetCorrectionAxisRotation_old() {
            Quaternion localRotation = Quaternion.Inverse(baseRotation) * t.rotation;
            if (parent != null)
                localRotation = Quaternion.Inverse(parent.rotation) * localRotation;

            Quaternion twist = GetTwist(localRotation, limitAngleAxis);

            Quaternion clampedRotation;

            Vector3 twistAxis;
            float twistAngle;
            twist.ToAngleAxis(out twistAngle, out twistAxis);

            if (limitAngle == false || twistAngle != 0) {
                float angle = Vector3.Angle(limitAngleAxis, twistAxis);
                twistAngle = UnityAngles.Normalize(twistAngle);

                if ((angle < 90 && twistAngle > 0) || (angle > 90 && twistAngle < 0))
                    clampedRotation = Quaternion.RotateTowards(Quaternion.identity, twist, maxLocalAngle);
                else
                    clampedRotation = Quaternion.RotateTowards(Quaternion.identity, twist, -minLocalAngle);
            }
            else
                clampedRotation = localRotation;

            // This check will prevent instability when rotation > 90 degrees
            if (Quaternion.Angle(localRotation, clampedRotation) == 0)
                clampedRotation = localRotation;

            Quaternion transformRotation = t.rotation;
            if (parent != null) {
                transformRotation = Quaternion.Inverse(parent.rotation) * transformRotation;
            }

            Quaternion correctionRotation = clampedRotation * Quaternion.Inverse(transformRotation);

            calculateAngleValue(clampedRotation);

            return baseRotation * correctionRotation;
        }

        public Quaternion GetCorrectionAxisRotation2() {
            Quaternion localRotation = Quaternion.Inverse(baseRotation) * t.rotation;
            if (parent != null)
                localRotation = Quaternion.Inverse(parent.rotation) * localRotation;

            Quaternion twist = GetTwist(localRotation, limitAngleAxis);

            Quaternion clampedRotation = limitAngle ? Quaternion.RotateTowards(Quaternion.identity, twist, maxLocalAngle) : localRotation;

            // This check will prevent instability when rotation > 90 degrees
            if (Quaternion.Angle(localRotation, clampedRotation) == 0)
                clampedRotation = localRotation;

            Quaternion rbRotation = clampedRotation;
            if (parent != null)
                rbRotation = parent.rotation * rbRotation;

            Quaternion correctionRotation = rbRotation * Quaternion.Inverse(localRotation);

            calculateAngleValue(clampedRotation);

            return correctionRotation;
        }

        private void calculateAngleValue(Quaternion clampedRotation) {
            float angle;
            Vector3 axis;
            clampedRotation.ToAngleAxis(out angle, out axis);
            if (VectorIsNan(axis)) {
                angle = 0;
                axis = limitAngleAxis;
            }
            angle = UnityAngles.Normalize(angle);
            if (Vector3.Angle(axis, limitAngleAxis) > 90)
                angle = -angle;

            if (angle > 0 && maxLocalAngle > 0)
                angleValue = angle / maxLocalAngle;
            else if (angle < 0 && minLocalAngle < 0)
                angleValue = angle / -minLocalAngle;
            else
                angleValue = 0;
        }

        private bool VectorIsNan(Vector3 v) {
            if (float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z))
                return true;
            if (float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z))
                return true;
            return false;
        }

        private static Quaternion GetTwist(Quaternion rotation, Vector3 axis) {
            Vector3 ra = new Vector3(rotation.x, rotation.y, rotation.z); // rotation axis
            Vector3 p = Vector3.Project(ra, axis); // return projection v1 on to v2  (parallel component)
            Quaternion twist = new Quaternion(p.x, p.y, p.z, rotation.w);
            Quaternion normalizedTwist = Normalize(twist);
            return normalizedTwist;
        }

        private static Quaternion Normalize(Quaternion q) {
            float length = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            float scale = 1.0f / length;
            Quaternion q1 = new Quaternion(q.x * scale, q.y * scale, q.z * scale, q.w * scale);
            return q1;
        }

        #endregion

        protected virtual void OnDestroy() {
            gameObjectEvent.value = null;
        }

        protected float xValue;
        protected float yValue;
        protected float zValue;

        protected float angleValue;

        #region Animations

        protected Vector3 speed;
        protected float rotationSpeed;

        public void MoveForward(float speedZ) {
            speed = new Vector3(speed.x, speed.y, speedZ);
        }
        public void MoveSideward(float speedX) {
            speed = new Vector3(speedX, speed.y, speed.z);
        }
        public void MoveUpward(float speedY) {
            speed = new Vector3(speed.x, speedY, speed.z);
        }

        public void SetSpeed(Vector3 speed) {
            this.speed = speed;
        }

        public void Rotate(float speed) {
            rotationSpeed = speed; // Quaternion.AngleAxis(speed, limitAngleAxis);
        }

        #endregion

        #region Events

        public GameObjectEventHandlers gameObjectEvent = new GameObjectEventHandlers() {
            label = "GameObject Event",
            id = 0,
            tooltip = "Call functions based on the GameObject life cycle",
            eventTypeLabels = new string[] {
                "Never",
                "Start",
                "On Destroy",
                "Update",
                " ",
                " ",
                " "
            }
        };

        protected static string[] sliderEventTypeLabels = new string[] {
                "Never",
                "On Min",
                "On Max",
                "While Min",
                "While Max",
                "On Change",
                "Continuous"
        };

        public FloatEventHandlers xSliderEvents = new FloatEventHandlers() {
            label = "X Axis",
            id = 1,
            eventTypeLabels = sliderEventTypeLabels,
            tooltip =
                "Call function using the X axis range value\n" +
                "Parameter: the range along the X axis (-1..1)"
        };
        public FloatEventHandlers ySliderEvents = new FloatEventHandlers() {
            label = "Y Axis",
            id = 2,
            eventTypeLabels = sliderEventTypeLabels,
            tooltip =
                "Call function using the Y axis range value\n" +
                "Parameter: the range along the Y axis (-1..1)"
        };
        public FloatEventHandlers zSliderEvents = new FloatEventHandlers() {
            label = "Z Axis",
            id = 3,
            eventTypeLabels = sliderEventTypeLabels,
            tooltip =
                "Call function using the Z axis range value\n" +
                "Parameter: the range along the Z axis (-1..1)"
        };

        public FloatEventHandlers angleEvents = new FloatEventHandlers() {
            label = "Angle",
            id = 4,
            eventTypeLabels = sliderEventTypeLabels,
        };

        protected void UpdateEvents() {
            xSliderEvents.value = xValue;
            ySliderEvents.value = yValue;
            zSliderEvents.value = zValue;
            angleEvents.value = angleValue;
        }

        #endregion

        #region Gizmos

        protected virtual void OnDrawGizmosSelected() {
            if (!isActiveAndEnabled)
                return;

            if (!Application.isPlaying)
                parent = transform.parent;

            Vector3 localPosition = transform.localPosition;
            Quaternion localRotation = transform.localRotation;
            if (parent != null) {
                localPosition = parent.InverseTransformPoint(transform.position);
                localRotation = Quaternion.Inverse(parent.rotation) * transform.rotation;
            }

            if (limitX) {
                Gizmos.color = Color.red;
                Vector3 localRight = localRotation * Vector3.right;
                // Project localPosition on basePane
                Vector3 basePositionX = ProjectPointOnPlane(localPosition, basePosition, localRight);

                Vector3 minPositionX = basePositionX + localRight * minLocalPosition.x;
                Vector3 maxPositionX = basePositionX + localRight * maxLocalPosition.x;
                DrawRange(minPositionX, maxPositionX);
            }
            if (limitY) {
                Gizmos.color = Color.green;
                Vector3 localUp = localRotation * Vector3.up;
                // Project localPosition on baseline;
                Vector3 basePositionY = ProjectPointOnPlane(localPosition, basePosition, localUp);

                Vector3 minPositionY = basePositionY + localUp * minLocalPosition.y;
                Vector3 maxPositionY = basePositionY + localUp * maxLocalPosition.y;
                DrawRange(minPositionY, maxPositionY);
            }
            if (limitZ) {
                Gizmos.color = Color.blue;
                Vector3 localForward = localRotation * Vector3.forward;
                // Project localPosition on baseline;
                Vector3 basePositionZ = ProjectPointOnPlane(localPosition, basePosition, localForward);
                Vector3 minPositionZ = basePositionZ + localForward * minLocalPosition.z;
                Vector3 maxPositionZ = basePositionZ + localForward * maxLocalPosition.z;
                DrawRange(minPositionZ, maxPositionZ);
            }
        }

        protected Vector3 ProjectPointOnPlane(Vector3 point, Vector3 planeOrigin, Vector3 planeNormal) {
            Vector3 v = point - planeOrigin;
            Vector3 d = Vector3.Project(v, planeNormal.normalized);
            Vector3 projectedPoint = point - d;
            return projectedPoint;
        }

        private void DrawRange(Vector3 minPosition, Vector3 maxPosition) {
            Vector3 worldMinPosition = parent != null ? parent.TransformPoint(minPosition) : minPosition;
            Vector3 worldMaxPosition = parent != null ? parent.TransformPoint(maxPosition) : maxPosition;

            Gizmos.DrawLine(worldMinPosition, worldMaxPosition);
            Gizmos.DrawSphere(worldMinPosition, 0.005F);
            Gizmos.DrawSphere(worldMaxPosition, 0.005F);
        }

        #endregion
    }

}