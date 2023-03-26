using UnityEngine;

namespace Passer {
#if pHUMANOID
    using Humanoid;
#endif

    /// <summary>The teleporter is a simple tool to teleport transforms</summary>
    [HelpURLAttribute("https://passervr.com/documentation/humanoid-control/tools/teleport/")]
    public class Teleporter : InteractionPointer {
        ///  <summary>Determines how the Transform is moved to the Target Point.</summary>
        public enum TransportType {
            Teleport,   //< Direct placement on the target point
            Dash        //< A quick movement in a short time from the originating point to the target point
        }

        /// <summary>The TransportType to use when teleporting.</summary>
        public TransportType transportType = TransportType.Teleport;
        /// <summary>The transform which will be teleported</summary>
        public Transform transformToTeleport;
#if pHUMANOID
        protected HumanoidControl humanoid;
#endif
        protected override void Awake() {
            base.Awake();

            if (transformToTeleport == null)
                transformToTeleport = this.transform;
#if pHUMANOID
            humanoid = transformToTeleport.GetComponent<HumanoidControl>();
            if (humanoid == null)
                humanoid = transformToTeleport.GetComponentInParent<HumanoidControl>();
            if (humanoid != null)
                transformToTeleport = humanoid.transform;
#endif

        }

        public float capsuleCheckHeight = 2;
        public float capsuleCheckRadius = 0.3F;

        /// <summary>Teleport the transform</summary>
        public virtual void TeleportTransform() {
            if (transformToTeleport == null)
                transformToTeleport = this.transform;
#if pHUMANOID
            if (humanoid == null)
#endif
                transformToTeleport.Teleport(focusPointObj.transform.position);
#if pHUMANOID
            else {
                Vector3 interactionPointerPosition = humanoid.GetHumanoidPosition() - transformToTeleport.position;

                Vector3 teleportTargetPosition = focusPointObj.transform.position- interactionPointerPosition;

                Vector3 teleportVector = teleportTargetPosition - transformToTeleport.position;
                Vector3 teleportDirection = teleportVector.normalized;
                float teleportDistance = teleportVector.magnitude;

                Vector3 capsuleBottom = transformToTeleport.position + capsuleCheckRadius * Vector3.up;
                Vector3 capsuleTop = transformToTeleport.position + (capsuleCheckHeight - capsuleCheckRadius) * Vector3.up;
                RaycastHit[] hits = Physics.CapsuleCastAll(capsuleTop, capsuleBottom, capsuleCheckRadius, teleportDirection, teleportDistance);
                float closestDistance = float.PositiveInfinity;
                foreach (RaycastHit hit in hits) {
                    if (hit.rigidbody == null || !humanoid.IsMyRigidbody(hit.rigidbody))
                        closestDistance = hit.distance;
                }
                if (closestDistance < teleportDistance)
                    teleportTargetPosition = transformToTeleport.position + teleportDirection * closestDistance;

                switch (transportType) {
                    case TransportType.Teleport:
                        transformToTeleport.Teleport(teleportTargetPosition);
                        break;
                    case TransportType.Dash:
                        StartCoroutine(TransformMovements.DashCoroutine(transformToTeleport, teleportTargetPosition));
                        break;
                    default:
                        break;
                }
            }
#endif
        }

        /// <summary>Adds a default Teleporter to the transform</summary>
        /// <param name="parentTransform">The transform to which the Teleporter will be added</param>
        /// <param name="pointerType">The interaction pointer type for the Teleporter</param>
        public static new Teleporter Add(Transform parentTransform, PointerType pointerType = PointerType.Ray) {
            GameObject pointerObj = new GameObject("Teleporter");
            pointerObj.transform.SetParent(parentTransform, false);

            GameObject destinationObj = new GameObject("Destination");
            destinationObj.transform.SetParent(pointerObj.transform);
            destinationObj.transform.localPosition = Vector3.zero;
            destinationObj.transform.localRotation = Quaternion.identity;

            if (pointerType == PointerType.FocusPoint) {
                GameObject focusPointSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                focusPointSphere.transform.SetParent(destinationObj.transform);
                focusPointSphere.transform.localPosition = Vector3.zero;
                focusPointSphere.transform.localRotation = Quaternion.identity;
                focusPointSphere.transform.localScale = Vector3.one * 0.1F;
            }
            else {
                LineRenderer pointerRay = destinationObj.AddComponent<LineRenderer>();
                pointerRay.startWidth = 0.01F;
                pointerRay.endWidth = 0.01F;
                pointerRay.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                pointerRay.receiveShadows = false;
                pointerRay.useWorldSpace = false;
            }

            Teleporter teleporter = pointerObj.AddComponent<Teleporter>();
            teleporter.focusPointObj = destinationObj;
            teleporter.rayType = RayType.Bezier;

            return teleporter;
        }

        public override void Click(bool clicking) {
            if (clicking)
                TeleportTransform();
        }
    }
}
