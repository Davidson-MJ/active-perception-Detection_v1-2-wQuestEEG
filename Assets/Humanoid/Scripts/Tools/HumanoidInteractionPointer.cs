using UnityEngine;

namespace Passer.Humanoid {
    /*
    /// <summary>An Inetraction Pointer for Humanoids</summary>
    public class HumanoidInteractionPointer : InteractionPointer {

        /// <summary>The InteractionModule used for interacting with Unity UI elements</summary>
        protected InteractionModule interactionModule;
        /// <sumaary>The ID of the interaction used for interaction with Unity UI elemnts</sumaary>
        protected int interactionID;


        /// <summary>Adds a default InteractionPointer to the transform</summary>
        /// <param name="parentTransform">The transform to which the Teleporter will be added</param>
        /// <param name="pointerType">The interaction pointer type for the Teleporter.</param>
        public static new InteractionPointer Add(Transform parentTransform, PointerType pointerType = PointerType.Ray) {
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

            InteractionPointer pointer = pointerObj.AddComponent<HumanoidInteractionPointer>();
            pointer.focusPointObj = focusPointObj;
            pointer.rayType = RayType.Straight;
            return pointer;
        }

        private HeadTarget headTarget;

        protected override void Awake() {
            HumanoidControl humanoid = this.transform.root.GetComponentInChildren<HumanoidControl>();
            if (humanoid == null)
                base.Awake();

            else {
                //inputModule = humanoid.GetComponent<Interaction>();
                interactionModule = FindObjectOfType<InteractionModule>();
                if (interactionModule == null) {
                    interactionModule = humanoid.gameObject.AddComponent<InteractionModule>();
                }

                interactionID = interactionModule.CreateNewInteraction(transform, timedClick);

                if (focusPointObj == null) {
                    focusPointObj = new GameObject("Focus Point");
                    focusPointObj.transform.parent = transform;
                }

                lineRenderer = focusPointObj.GetComponent<LineRenderer>();
                if (lineRenderer != null) {
                    lineRenderer.useWorldSpace = false;
                    lineRenderer.SetPosition(1, Vector3.zero);
                }

                headTarget = transform.parent.GetComponent<HeadTarget>();
            }
        }

        #region Update

        protected override void Update() {
            if (focusPointObj == null)
                return;

            interactionModule.ActivatePointing(interactionID, active);

            if (interactionModule.IsPointing(interactionID)) {
                focusPointObj.SetActive(true);

                if (rayType == RayType.SphereCast) {
                    UpdateSpherecast();
                }
                else if (lineRenderer != null) {
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
                }
                else {
                    Vector3 focusPoint = interactionModule.GetFocusPoint(interactionID);
                    Vector3 localFocusPoint = transform.InverseTransformPoint(focusPoint);
                    focusPointObj.transform.position = transform.TransformPoint(localFocusPoint - Vector3.forward * 0.01F);
                    focusPointObj.transform.rotation = interactionModule.GetFocusRotation(interactionID);
                }
                objectInFocus = interactionModule.GetFocusObject(interactionID);
                if (objectInFocus != null) {
                    Rigidbody rigidbodyInFocus = objectInFocus.GetComponentInParent<Rigidbody>();
                    if (rigidbodyInFocus != null)
                        objectInFocus = rigidbodyInFocus.gameObject;
                }

                if (timedClick != 0) {
                    if (!hasClicked && interactionModule.IsTimedClick(interactionID)) {
                        Click(true);
                        hasClicked = true;
                        Click(false);
                    }
                }

            }
            else {
                focusPointObj.SetActive(false);
                focusPointObj.transform.position = transform.position;
                objectInFocus = null;
                hasClicked = false;
            }

            UpdateFocus();
            UpdateFocusPoint();

            if (headTarget != null) {
#if hFACE
                transform.rotation = Quaternion.LookRotation(headTarget.face.gazeDirection);
#endif
            }
        }

        protected override void UpdateStraight() {
            Vector3 focusPosition = transform.position + transform.forward * 10;
            Quaternion focusRotation = Quaternion.LookRotation(-transform.forward);
            GameObject focusObject = null;

            RaycastHit hit;
            bool raycastHit = Physics.Raycast(transform.position, transform.forward, out hit, maxDistance);
            if (raycastHit) {
                focusPosition = hit.point;
                focusRotation = Quaternion.LookRotation(hit.normal);
                focusObject = hit.transform.gameObject;
            }

            interactionModule.SetExternalRayCast(interactionID, focusPosition, focusRotation, focusObject);

            focusPointObj.transform.position = focusPosition;
            focusPointObj.transform.rotation = focusRotation;

            Vector3 endPosition = focusPointObj.transform.InverseTransformPoint(transform.position);

#if UNITY_5_6_OR_NEWER
            lineRenderer.positionCount = 2;
#else
            lineRenderer.numPositions = 2;
#endif
            lineRenderer.SetPosition(0, endPosition);
        }

        #endregion
        /// <summary>Sets the diration in which the pointer points</summary>
        /// While writing this, I wonder why the rotation of the transform is not used here.
        public void SetRayDirection(Vector3 direction) {
            interactionModule.SetPointingDirection(interactionID, direction);
        }

        public override void Activation(bool _active) {
            base.Activation(_active);
        }

        /// <summary>Automatically clicks when it is deactivated</summary>
        /// This function will activate the interaction pointer when the active parameter is true
        /// and will perform a click when the interaction pointer is deactivated again.
        /// This enables you to do interaction with one button.
        /// <param name="_active">The new activation status of the interaction pointer</param> 
        public void ActivationClick(bool _active) {
            if (active && !_active) {
                base.Click(true);
                base.Activation(false);
                base.Click(false);
            }
            else if (!active && _active) {
                base.Activation(true);
            }
        }

        public override void Click(bool clicking) {
            base.Click(clicking);
        }

    }
    */
}