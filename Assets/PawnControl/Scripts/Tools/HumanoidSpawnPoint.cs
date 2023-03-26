using UnityEngine;
using Passer.Pawn;

namespace Passer {

    public class HumanoidSpawnPoint : SpawnPoint {

        public Vector3 center = new Vector3(0, 0.9F, 0);
        public float radius = 0.3F;
        public float height = 1.8F;

        public bool isFree {
            get {
                Vector3 toEnd = (height / 2 - radius - 0.01F) * Vector3.up;
                // We decrease the height with 1cm because otherwise the capsule won't be free 
                // if it touches the ground at transform.position
                return !Physics.CheckCapsule(transform.position + center - toEnd, transform.position + center + toEnd, radius);
            }
        }

        private void Awake() {
            PawnControl humanoid = FindObjectOfType<PawnControl>();
            if (humanoid == null)
                return;

            humanoid.transform.MoveTo(this.transform.position);
        }

        #region Gizmos

        private void OnDrawGizmos() {
            Gizmos.color = new Color(244F / 255F, 122F / 255F, 0);
            Gizmos.DrawCube(transform.position, new Vector3(0.2F, 0.01F, 0.2F));
            GizmosDrawCircle(transform.position, radius);
        }

        private void OnDrawGizmosSelected() {
            GizmosDrawCapsule(transform.position, radius);
        }

        private void GizmosDrawCapsule(Vector3 position, float radius) {
            Gizmos.color = new Color(244F / 255F, 122F / 255F, 0);

            GizmosDrawCircle(position + height * Vector3.up, radius);

            Vector3 toEnd = (height / 2) * Vector3.up;
            DrawCapsuleSide(position, center, toEnd, new Vector3(radius, 0, 0));
            DrawCapsuleSide(position, center, toEnd, new Vector3(-radius, 0, 0));
            DrawCapsuleSide(position, center, toEnd, new Vector3(0, 0, radius));
            DrawCapsuleSide(position, center, toEnd, new Vector3(0, 0, -radius));
        }

        private void DrawCapsuleSide(Vector3 position, Vector3 center, Vector3 toEnd, Vector3 delta) {
            Gizmos.DrawLine(position + center - toEnd + delta, position + center + toEnd + delta);
        }

        private void GizmosDrawCircle(Vector3 position, float radius) {
            float theta = 0;
            float x = radius * Mathf.Cos(theta);
            float y = radius * Mathf.Sin(theta);
            Vector3 pos = position + new Vector3(x, 0, y);
            Vector3 newPos = pos;
            Vector3 lastPos = pos;
            for (theta = 0.1f; theta < Mathf.PI * 2; theta += 0.1f) {
                x = radius * Mathf.Cos(theta);
                y = radius * Mathf.Sin(theta);
                newPos = position + new Vector3(x, 0, y);
                Gizmos.DrawLine(pos, newPos);
                pos = newPos;
            }
            Gizmos.DrawLine(pos, lastPos);
        }

        #endregion 
    }
}
