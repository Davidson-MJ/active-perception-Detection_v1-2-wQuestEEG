using UnityEngine;

namespace Passer {

    /// <summary>Implements behaviour using Colliders</summary>
    /// This component requires a Collider to trigger the collisions.
    [RequireComponent(typeof(Collider))]
    public class CollisionEventHandler : MonoBehaviour {

        #region Events

        public GameObjectEventHandlers collisionHandlers = new GameObjectEventHandlers() {
            label = "Collision Event",
            tooltip =
                "Call functions using the collider state\n" +
                "Parameter: the GameObject colliding with the collider",
            eventTypeLabels = new string[] {
                "Never",
                "On Collision Start",
                "On Collision End",
                "While Colliding",
                "While not Colliding",
                "On Collision Change",
                "Always"
            },
        };

        protected virtual void OnCollisionEnter(Collision collision) {
            collisionHandlers.value = collision.gameObject;
        }

        protected virtual void OnCollisionExit(Collision collision) {
            collisionHandlers.value = null;
        }

        #endregion

    }

}