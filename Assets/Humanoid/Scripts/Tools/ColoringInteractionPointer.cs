using UnityEngine;

namespace Passer.Humanoid {

    public class ColoringInteractionPointer : InteractionPointer {

        public Material validMaterial;
        public Material invalidMaterial;

        protected override void Update() {
            base.Update();

            if (!active)
                return;

            if (objectInFocus != null) {
                TeleportTarget teleportTarget = objectInFocus.GetComponent<TeleportTarget>();
                Renderer[] renderers = GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers) {
                    if (teleportTarget != null)
                        renderer.sharedMaterial = validMaterial;
                    else
                        renderer.sharedMaterial = invalidMaterial;
                }
            }
            else
                lineRenderer.sharedMaterial = invalidMaterial;
        }
    }

}