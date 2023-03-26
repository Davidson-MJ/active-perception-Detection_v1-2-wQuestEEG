using UnityEngine;
using UnityEngine.EventSystems;

namespace Passer {

    /// <summary>A generic pointer to interact with objects in the scene using the Unity Event system. </summary>
    /// The objects can receive and react on these interactions when an Unity Event Trigger component has been added to them. 
    [HelpURLAttribute("https://passervr.com/documentation/humanoid-control/tools/interaction-pointer/")]
    public class InteractionPointer : MonoBehaviour {
        protected static void DebugLog(string s) {
#if UNITY_EDITOR
            //Debug.Log(s);
#endif
        }

        /// <summary>Is the interaction pointer active?</summary>
        /// When an interaction pointer is active, it will actively point to objects.
        /// The objectInFocus will be updated based on the pointer.
        /// When a focusPointObj is set, this object will be set active.
        /// When a Line Renderer is set, the line renderer will be updated
        /// according to the properties of the pointer.
        public bool active = true;

        /// <summary>Automatically initiates a click when the objectInFocus does not change for the set amount of seconds.</summary>
        /// The value 0 disables this function.
        public float timedClick;

        protected bool hasClicked = false;

        /// <summary>The GameObject which represents the focus point of the Interaction Pointer when it is active.</summary>
        /// If this is set and the pointer is active, the object will be objected based on the pointer's properties
        /// This GameObject will be disabled when the pointer is not active.
        public GameObject focusPointObj;

        /// <summary>The object to which the Interaction Pointer is pointing at.</summary>
        /// This is updated at runtime while the pointer is active.
        /// The value is null when the pointer is not reaching any object.
        public GameObject objectInFocus;

        //protected new EventSystem eventSystem;
        //protected PointerEventData data;
        protected InteractionModule interactionModule;
        protected int interactionID;
        protected InteractionModule.InteractionPointer interactionPointer;

        /// <summary>The LineRender for this pointer when available.</summary>
        protected LineRenderer lineRenderer;

        /// <summary>The ray modes for interaction pointers.</summary>
        public enum RayType {
            Straight,   //< The straight mode will cast a ray from the Transform position in the forward direction until it hits an object.
            Bezier,     //< In bezier mode, a bezier curve will be cast from the Transform position in the forward direction.
            Gravity,    //< In gravity mode, a curve will be cast from the Transform position in the forward direction. This curve follows a gravity path as if an object is launched.
            SphereCast, //< Is similar to the Straight mode, but casts a sphere instead of a ray.
        }
        /// <summary>The ray mode for this interaction pointer.</summary>
        public RayType rayType = RayType.Straight;

        /// <summary>The maximum length of the curve in units(meters)</summary>
        /// This value is used for Straight, SphereCast and Bezier RayTypes
        public float maxDistance = 10;

        /// <summary>The size of a segment in the curve</summary>
        /// Lower values will give a smoother curve, but requires more performance
        /// This value is used for Bezier and Gravity RayTypes
        public float resolution = 0.2F;

        private int nCurveSegments;
        private Vector3[] curvePoints;

        /// <summary>The type of interaction pointer</summary>
        public enum PointerType {
            FocusPoint, //< will not use a linerender, but a focuspoint mesh
            Ray         //< will use a linerendere, but not a focuspoint mesh
        }
        /// <summary>Adds a default InteractionPointer to the transform</summary>
        /// <param name="parentTransform">The transform to which the Teleporter will be added</param>
        /// <param name="pointerType">The interaction pointer type for the Teleporter.</param>
        public static InteractionPointer Add(Transform parentTransform, PointerType pointerType = PointerType.Ray) {
            GameObject pointerObj = new GameObject("Interaction Pointer");
            pointerObj.transform.SetParent(parentTransform);
            pointerObj.transform.localPosition = Vector3.zero;
            pointerObj.transform.localRotation = Quaternion.identity;

            GameObject focusPointObj = new GameObject("FocusPoint");
            focusPointObj.transform.SetParent(pointerObj.transform);
            focusPointObj.transform.localPosition = Vector3.zero;
            focusPointObj.transform.localRotation = Quaternion.identity;

            if (pointerType == PointerType.FocusPoint) {
                GameObject focusPointSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                focusPointSphere.transform.SetParent(focusPointObj.transform);
                focusPointSphere.transform.localPosition = Vector3.zero;
                focusPointSphere.transform.localRotation = Quaternion.identity;
                focusPointSphere.transform.localScale = Vector3.one * 0.1F;
                Collider collider = focusPointSphere.GetComponent<Collider>();
                DestroyImmediate(collider, true);
            }
            else {
                LineRenderer pointerRay = focusPointObj.AddComponent<LineRenderer>();
                pointerRay.startWidth = 0.01F;
                pointerRay.endWidth = 0.01F;
                pointerRay.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                pointerRay.receiveShadows = false;
                pointerRay.useWorldSpace = false;
            }

            InteractionPointer pointer = pointerObj.AddComponent<InteractionPointer>();
            pointer.focusPointObj = focusPointObj;
            pointer.rayType = RayType.Straight;
            return pointer;
        }

        #region Init

        protected virtual void Awake() {
            Transform rootTransform = this.transform.root;

            interactionModule = FindObjectOfType<InteractionModule>();
            if (interactionModule == null)
                interactionModule = CreateInteractionModule();
            EventSystem eventSystem = interactionModule.GetComponent<EventSystem>();
            interactionID = interactionModule.CreateNewInteraction(transform, timedClick, eventSystem);


            //EventSystem eventSystem = FindObjectOfType<EventSystem>();
            //if (eventSystem == null) {
            //    eventSystem = CreateEventSystem();
            //}
            //data = new PointerEventData(eventSystem);

            if (focusPointObj == null) {
                focusPointObj = new GameObject("Focus Point");
                focusPointObj.transform.parent = transform;
            }

            lineRenderer = focusPointObj.GetComponent<LineRenderer>();
            if (lineRenderer != null) {
                lineRenderer.useWorldSpace = false;
                lineRenderer.SetPosition(1, Vector3.zero);
            }
        }

        protected virtual InteractionModule CreateInteractionModule() {
#if pHUMANOID            
            Humanoid.HumanoidControl humanoid = GetComponentInParent<Humanoid.HumanoidControl>();
            if (humanoid != null) {
                InteractionModule humanoidInteractionModule = humanoid.gameObject.AddComponent<InteractionModule>();
                return humanoidInteractionModule;
            }
#endif

            GameObject interactionModuleObj = new GameObject("InteractionModule");
            InteractionModule interactionModule = interactionModuleObj.AddComponent<InteractionModule>();

            return interactionModule;

        }

        protected virtual EventSystem CreateEventSystem() {
#if pHUMANOID            
            Humanoid.HumanoidControl humanoid = GetComponentInParent<Humanoid.HumanoidControl>();
            if (humanoid != null) {
                EventSystem humanoidEventSystem = humanoid.gameObject.AddComponent<EventSystem>();
                return humanoidEventSystem;
            }
#endif

            GameObject eventSystemObject = new GameObject("EventSystem");
            EventSystem eventSystem = eventSystemObject.AddComponent<EventSystem>();


            return eventSystem;
        }

        protected virtual void Start() {
            nCurveSegments = Mathf.CeilToInt(maxDistance / resolution);
            curvePoints = new Vector3[nCurveSegments + 1];
        }

        #endregion

        #region Update
        protected float focusTimeToTouch = 0;
        protected float focusStart = 0;

        protected virtual void Update() {
            if (focusPointObj == null)
                return;

            if (maxDistance < 0)
                maxDistance = 0;

            if (active) {
                focusPointObj.SetActive(true);

                if (rayType == RayType.SphereCast) {
                    UpdateSpherecast();
                }
                else {
                    if (lineRenderer != null)
                        lineRenderer.enabled = true;

                    switch (rayType) {
                        case RayType.Straight:
                            UpdateStraight();
                            break;
                        case RayType.Bezier:
                            UpdateBezier();
                            break;
                        case RayType.Gravity:
                            UpdateGravity();
                            break;
                    }

                    if (objectInFocus != null) {
                        Rigidbody rigidbodyInFocus = objectInFocus.GetComponentInParent<Rigidbody>();
                        if (rigidbodyInFocus != null)
                            objectInFocus = rigidbodyInFocus.gameObject;
                    }

                    if (timedClick != 0) {
                        if (!hasClicked && focusTimeToTouch != 0 && focusStart > 0 &&
                            Time.time - focusStart > focusTimeToTouch) {
                            Click(true);
                            hasClicked = true;
                            Click(false);
                        }
                    }
                }
                UpdateFocus();
                UpdateFocusPoint();
            }
            else {
                focusPointObj.SetActive(false);
                focusPointObj.transform.position = transform.position;
                objectInFocus = null;
                hasClicked = false;
            }
        }

        #region Straight

        protected virtual void UpdateStraight() {
            Vector3 focusPosition = transform.position + transform.forward * maxDistance;
            Quaternion focusRotation = Quaternion.LookRotation(-transform.forward);

            //Debug.DrawRay(transform.position, transform.forward, Color.white);
            RaycastHit hit;
            bool raycastHit = Physics.Raycast(transform.position, transform.forward, out hit, maxDistance);
            if (raycastHit) {
                focusPosition = hit.point;
                focusRotation = Quaternion.LookRotation(hit.normal);
                objectInFocus = hit.transform.gameObject;
            }
            else {
                objectInFocus = null;
            }

            focusPointObj.transform.position = focusPosition;
            focusPointObj.transform.rotation = focusRotation;

            Vector3 endPosition = focusPointObj.transform.InverseTransformPoint(transform.position);

            if (lineRenderer != null) {
#if UNITY_5_6_OR_NEWER
                lineRenderer.positionCount = 2;
#else
                lineRenderer.numPositions = 2;
#endif
                lineRenderer.SetPosition(0, endPosition);
            }
        }

        #endregion

        #region Bezier
        protected virtual void UpdateBezier() {
            Vector3 normal;
            GameObject focusObject = null;
            Vector3[] bezierPositions = UpdateBezierCurve(transform, maxDistance, out normal, out focusObject);

            Vector3 focusPosition = bezierPositions[bezierPositions.Length - 1];
            Quaternion focusRotation = Quaternion.LookRotation(normal, transform.forward);

            focusPointObj.transform.position = focusPosition; // bezierPositions[bezierPositions.Length - 1];
            focusPointObj.transform.rotation = focusRotation; // Quaternion.LookRotation(normal, transform.forward);


            for (int i = 0; i < bezierPositions.Length; i++)
                bezierPositions[i] = focusPointObj.transform.InverseTransformPoint(bezierPositions[i]);

            if (lineRenderer != null) {
#if UNITY_5_6_OR_NEWER
                lineRenderer.positionCount = bezierPositions.Length;
#else
                lineRenderer.numPositions = bezierPositions.Length;
#endif
                lineRenderer.SetPositions(bezierPositions);
            }
        }


        protected float heightLimitAngle = 100f;

        protected Vector3 startPosition = Vector3.zero;
        protected Vector3 intermediatePosition;
        protected Vector3 endPosition;

        protected virtual Vector3[] UpdateBezierCurve(Transform transform, float maxDistance, out Vector3 normal, out GameObject focusObject) {
            float distance = maxDistance;

            float attachedRotation = Vector3.Dot(Vector3.up, transform.forward);
            if ((attachedRotation * 100f) > heightLimitAngle) {
                float controllerRotationOffset = 1f - (attachedRotation - (heightLimitAngle / 100f));
                distance = (maxDistance * controllerRotationOffset) * controllerRotationOffset;
            }

            intermediatePosition = Vector3.forward * distance;
            //Debug.DrawLine(this.transform.position, transform.TransformPoint(intermediatePosition));

            RaycastHit rayHit;
            if (Physics.Raycast(transform.TransformPoint(intermediatePosition), Vector3.down, out rayHit)) {
                normal = rayHit.normal;
                focusObject = rayHit.transform.gameObject;
                endPosition = transform.InverseTransformPoint(rayHit.point);

                for (int i = 0; i <= nCurveSegments; i++)
                    curvePoints[i] = GetPoint(i / (float)nCurveSegments, transform);

            }
            else {
                normal = Vector3.up;
                focusObject = null;
            }

            return curvePoints;
        }

        protected Vector3 GetPoint(float t, Transform transform) {
            Vector3 localPoint = GetBezierPoint(startPosition, intermediatePosition, endPosition, t);
            return transform.TransformPoint(localPoint);
        }

        protected Vector3 GetVelocity(float t, Transform transform) {
            Vector3 localVelocity = GetBezierFirstDerivative(startPosition, intermediatePosition, endPosition, t);
            return transform.TransformPoint(localVelocity) - transform.position;
        }

        private static Vector3 GetBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t) {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return
                oneMinusT * oneMinusT * p0 +
                2f * oneMinusT * t * p1 +
                t * t * p2;
        }

        private static Vector3 GetBezierFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, float t) {
            return
                2f * (1f - t) * (p1 - p0) +
                2f * t * (p2 - p1);
        }

        #endregion

        #region Gravity

        /// <summary>The horizontal speed for a gravity curve.</summary>
        /// A higher speed will reach further positions.
        public float speed = 3;

        protected virtual void UpdateGravity() {
            Vector3 normal;
            GameObject focusObject = null;
            UpdateGravityCurve(transform, speed, out normal, out focusObject);

            Vector3 focusPosition = curvePoints[curvePoints.Length - 1];
            Quaternion focusRotation = Quaternion.LookRotation(normal, transform.forward);

            focusPointObj.transform.position = focusPosition;
            focusPointObj.transform.rotation = focusRotation;

            for (int i = 0; i < curvePoints.Length; i++)
                curvePoints[i] = focusPointObj.transform.InverseTransformPoint(curvePoints[i]);

            if (lineRenderer != null) {
#if UNITY_5_6_OR_NEWER
                lineRenderer.positionCount = curvePoints.Length;
#else
                lineRenderer.numPositions = curvePoints.Length;
#endif
                lineRenderer.SetPositions(curvePoints);
            }
        }

        protected virtual void UpdateGravityCurve(Transform transform, float forwardSpeed, out Vector3 normal, out GameObject hitObject) {
            curvePoints[0] = transform.position;
            Vector3 segVelocity = transform.forward * forwardSpeed;
            normal = Vector3.up;
            hitObject = null;

            for (int i = 1; i < nCurveSegments + 1; i++) {
                if (hitObject != null) {
                    curvePoints[i] = curvePoints[i - 1];
                    continue;
                }
                // Time it takes to traverse one segment of length segScale (careful if velocity is zero)
                float segTime = (segVelocity.sqrMagnitude != 0) ? resolution / segVelocity.magnitude : 0;

                // Add velocity from gravity for this segment's timestep
                segVelocity = segVelocity + Physics.gravity * segTime;

                // Check to see if we're going to hit a physics object
                RaycastHit hit;
                if (Physics.Raycast(curvePoints[i - 1], segVelocity.normalized, out hit, resolution)) {
                    normal = hit.normal;
                    hitObject = hit.transform.gameObject;

                    // set next position to the position where we hit the physics object
                    curvePoints[i] = curvePoints[i - 1] + segVelocity.normalized * hit.distance;
                }
                // If our raycast hit no objects, then set the next position to the last one plus v*t
                else {
                    curvePoints[i] = curvePoints[i - 1] + segVelocity * segTime;
                }
            }
        }
        #endregion

        #region Spherecast
        /// <summary>The radius of the sphere in a SphereCast</summary>
        public float radius = 0.1F;

        protected virtual void UpdateSpherecast() {
            RaycastHit hit;
            if (Physics.SphereCast(transform.position, radius, transform.forward, out hit, maxDistance)) {
                objectInFocus = hit.transform.gameObject;

                focusPointObj.transform.position = hit.point;
                focusPointObj.transform.rotation = Quaternion.LookRotation(hit.normal, transform.forward);
            }
            else {
                objectInFocus = null;
                focusPointObj.transform.position = transform.position + transform.forward * maxDistance;
                focusPointObj.transform.rotation = Quaternion.identity;
            }
        }
        #endregion

        #endregion

        #region Methods

        public void LaunchRigidbody(Rigidbody rigidbody) {
            // Only gravity rays are supported
            if (rayType != RayType.Gravity)
                return;

            rigidbody.velocity = transform.forward * speed;
        }

        public void LaunchPrefab(GameObject prefab) {
            GameObject gameObject = Instantiate(prefab, transform.position, transform.rotation);
            Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
            if (rigidbody == null)
                rigidbody = gameObject.AddComponent<Rigidbody>();

            LaunchRigidbody(rigidbody);
        }

        /// <summary>
        /// Spawns a prefab as a child of the objectInFocus
        /// </summary>
        /// <param name="prefab">The prefab to spawn</param>
        /// The spawn position and rotation will match the focusPoint transform.
        /// When the objectInFocus is null, the prefab will be spawned as a root transform.
        public void SpawnOnObjectInFocus(GameObject prefab) {
            GameObject spawnedObject = Spawner.Spawn(prefab, focusPointObj.transform.position, focusPointObj.transform.rotation);
            if (objectInFocus != null)
                spawnedObject.transform.SetParent(objectInFocus.transform, true);
        }

        public void ApplyForce(float magnitude) {
            if (objectInFocus == null)
                return;
            Rigidbody objRigidbody = objectInFocus.GetComponent<Rigidbody>();
            if (objRigidbody == null)
                return;

            // needs to use hitDirection in the future
            Vector3 direction = Vector3.Normalize(focusPointObj.transform.position - transform.position);
            objRigidbody.AddForceAtPosition(direction * magnitude, focusPointObj.transform.position);
        }

        #endregion

        #region Events

        #region Focus

        /// <summary>Event based on the current focus.</summary>
        /// This event is generated from the objectInFocus.
        /// It has the objectInFocus as parameter. It can be used to execute functions when the focus changes between objects.
        public GameObjectEventHandlers focusEvent = new GameObjectEventHandlers() {
            label = "Focus Event",
            tooltip =
                "Call functions using the current focus\n" +
                "Parameter: the Object in Focus"
        };

        protected GameObject previousObjectInFocus;
        protected void UpdateFocus() {
            focusEvent.value = objectInFocus;

            if (objectInFocus != previousObjectInFocus) {
                DebugLog("Focus Change " + objectInFocus);
                PointerEventData data = interactionModule.pointers[interactionID].data;
                if (previousObjectInFocus != null) {
                    ExecuteEvents.ExecuteHierarchy(previousObjectInFocus, data, ExecuteEvents.pointerExitHandler);
                    ExecuteEvents.ExecuteHierarchy(previousObjectInFocus, data, ExecuteEvents.deselectHandler);
                }
                ExecuteEvents.ExecuteHierarchy(objectInFocus, data, ExecuteEvents.pointerEnterHandler);
                ExecuteEvents.ExecuteHierarchy(objectInFocus, data, ExecuteEvents.selectHandler);
                previousObjectInFocus = objectInFocus;
            }
        }

        #endregion

        #region FocusPoint

        /// <summary>Event based on the current focus.</summary>
        /// This event is generated from the objectInFocus.
        /// It has the objectInFocus as parameter. It can be used to execute functions when the focus changes between objects.
        public Vector3EventList focusPointEvent = new Vector3EventList() {
            label = "Focus Point Event",
            tooltip =
                "Call functions using the current focus point\n" +
                "Parameter: the position of the focus point"
        };

        protected void UpdateFocusPoint() {
            focusPointEvent.value = focusPointObj.transform.position;
        }
        #endregion

        #region Click

        /// <summary>Event based on the clicking status</summary>
        /// This event is generated from the clicking boolean.
        /// It has the objectInFocus as parameter. It can be used to exectue functions when the user has clicked on an object.
        public GameObjectEventHandlers clickEvent = new GameObjectEventHandlers() {
            label = "Click Event",
            tooltip =
                "Call functions using the clicking status\n" +
                "Parameter: the Object in Focus when clicking"
        };

        /// <summary>Click on the objectInFocus</summary>
        /// This function will do a full click: a button down followed by a button up.
        public void FullClick() {
            Click(true);
            hasClicked = true;
            Click(false);
        }

        /// <summary>Change the click status on the objectInFocus</summary>
        /// <param name="clicking">Indicates if the button is down</param>
        public virtual void Click(bool clicking = true) {
            if (clicking)
                clickEvent.value = objectInFocus;
            else
                clickEvent.value = null;

            if (objectInFocus != null) {
                PointerEventData data = interactionModule.pointers[interactionID].data;
                RaycastResult raycastResult = new RaycastResult() {
                    worldPosition = focusPointObj.transform.position,
                    worldNormal = focusPointObj.transform.forward,
                    distance = (focusPointObj.transform.position - this.transform.position).magnitude,
                    gameObject = focusPointObj
                };
                data.pointerCurrentRaycast = raycastResult;

                if (clicking) {
                    ExecuteEvents.ExecuteHierarchy(objectInFocus, data, ExecuteEvents.pointerDownHandler);
                }
                else {
                    ExecuteEvents.ExecuteHierarchy(objectInFocus, data, ExecuteEvents.pointerUpHandler);
                    ExecuteEvents.ExecuteHierarchy(objectInFocus, data, ExecuteEvents.pointerClickHandler);
                }
            }
        }

        #endregion

        #region Active

        /// <summary>Event based on the active status</summary>
        /// This event is genereated from the active boolean
        /// Is has the active boolean as parameter. It can be used to execute functions when the interaction pointer is activated.
        public BoolEvent activeEvent = new BoolEvent();

        /// <summary>Change the active status of the InteractionPointer</summary>
        /// <param name="_active">Indicates if the InteractionPointer is active</param>
        public virtual void Activation(bool _active) {
            active = _active;
            activeEvent.value = _active;
            if (active)
                previousObjectInFocus = null;
            else if (objectInFocus != null) {
                PointerEventData data = interactionModule.pointers[interactionID].data;
                ExecuteEvents.ExecuteHierarchy(previousObjectInFocus, data, ExecuteEvents.pointerExitHandler);
                ExecuteEvents.ExecuteHierarchy(previousObjectInFocus, data, ExecuteEvents.deselectHandler);
            }
        }

        #endregion

        public virtual void ActivateClick(bool _active) {
            if (active && !_active) {
                Click();
                Activation(false);
            }
            else if (!active && _active) {
                Activation(true);
            }
        }


        #endregion

        #region Gizmos
        protected virtual void OnDrawGizmosSelected() {
            if (rayType == RayType.SphereCast) {
                Gizmos.color = Color.green;

                Gizmos.DrawWireSphere(transform.position, radius);
                Gizmos.DrawRay(transform.position + transform.up * radius, transform.forward * maxDistance);
                Gizmos.DrawRay(transform.position - transform.up * radius, transform.forward * maxDistance);
                Gizmos.DrawRay(transform.position + transform.right * radius, transform.forward * maxDistance);
                Gizmos.DrawRay(transform.position - transform.right * radius, transform.forward * maxDistance);
                Gizmos.DrawWireSphere(transform.position + transform.forward * maxDistance, radius);
            }
        }
        #endregion
    }
}
