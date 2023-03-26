using UnityEngine;

namespace Passer.Humanoid {

    /// <summary>Have a humaonid grab objects from a distance</summary>
    /// The Humanoid Telegrabber enables humanoids to grab objects
    /// which are normally out of reach for the hands.
    /// This Interaction Pointer should be used on a child of a HandTarget.
    [HelpURLAttribute("https://passervr.com/documentation/humanoid-control/tools/telegrabber/")]
    public class Telegrabber : InteractionPointer {

        protected override void Awake() {
            clickEvent.SetMethod(EventHandler.Type.OnStart, GrabObject);
            base.Awake();
        }

        /// <summary>Grab the object currently in focus of the InteractionPointer</summary>
        /// If current objectInFocus is a Rigidbody, this function will have the Humanoid hand
        /// try to grab the object.
        public void GrabObject() {
            if (objectInFocus == null)
                return;

            HandTarget handTarget = transform.GetComponentInParent<HandTarget>();
            if (handTarget == null)
                return;

            Rigidbody rigidbodyInFocus = objectInFocus.GetComponentInParent<Rigidbody>();
            if (rigidbodyInFocus != null)
                handTarget.GrabOrLetGo(rigidbodyInFocus.gameObject, false);
        }
    }
}