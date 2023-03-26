using UnityEngine;

namespace Passer {

    /// <summary>Implements input behaviour using Trigger Colliders</summary>
    /// This can be used to define input actions which are only active when the player is
    /// within a certain area.
    /// This component requires a Collider to specify the working area of this component.
    [RequireComponent(typeof(Collider))]
    public class TriggerEventHandler : MonoBehaviour {

        #region Events

        /// <summary> Trigger Event Handles </summary>
        /// Let you execute function calls based on the Trigger Events
        public GameObjectEventHandlers triggerHandlers = new GameObjectEventHandlers() {
            label = "Trigger Event",
            tooltip = 
                "Call functions using the trigger collider state\n" +
                "Parameter: the GameObject entering the trigger",
            eventTypeLabels = new string[] {
                "Never",
                "On Trigger Enter",
                "On Trigger Exit",
                "On Trigger Stay",
                "On Trigger Empty",
                "On Trigger Change",
                "Always"
            },
        };

        public ControllerEventHandlers[] leftInputEvents = {
            new ControllerEventHandlers() { label = "Left Vertical", id = 0 },
            new ControllerEventHandlers() { label = "Left Horizontal", id = 1 },
            new ControllerEventHandlers() { label = "Left Stick Button", id = 2 },
            new ControllerEventHandlers() { label = "Left Button 1", id = 3 },
            new ControllerEventHandlers() { label = "Left Button 2", id = 4 },
            new ControllerEventHandlers() { label = "Left Button 3", id = 5 },
            new ControllerEventHandlers() { label = "Left Button 4", id = 6 },
            new ControllerEventHandlers() { label = "Left Trigger 1", id = 7 },
            new ControllerEventHandlers() { label = "Left Trigger 2", id = 8 },
            new ControllerEventHandlers() { label = "Left Option", id = 9 },
        };
        public ControllerEventHandlers[] rightInputEvents = {
            new ControllerEventHandlers() { label = "Right Vertical", id = 0 },
            new ControllerEventHandlers() { label = "Right Horizontal", id = 1 },
            new ControllerEventHandlers() { label = "Right Stick Button", id = 2 },
            new ControllerEventHandlers() { label = "Right Button 1", id = 3 },
            new ControllerEventHandlers() { label = "Right Button 2", id = 4 },
            new ControllerEventHandlers() { label = "Right Button 3", id = 5 },
            new ControllerEventHandlers() { label = "Right Button 4", id = 6 },
            new ControllerEventHandlers() { label = "Right Trigger 1", id = 7 },
            new ControllerEventHandlers() { label = "Right Trigger 2", id = 8 },
            new ControllerEventHandlers() { label = "Right Option", id = 9 },
        };

        GameObject triggeringGameObject = null;

        private void FixedUpdate() {
            triggerHandlers.value = triggeringGameObject;

            triggeringGameObject = null;
        }

        private void OnTriggerStay(Collider other) {
            if (other.attachedRigidbody == null)
                triggeringGameObject = other.gameObject;
            else
                triggeringGameObject = other.attachedRigidbody.gameObject;
        }

        protected bool entered = false;
        protected virtual void OnTriggerEnter(Collider other) {
            //Debug.Log(this.gameObject.name + " Trigger Enter: " + other + " " + other.attachedRigidbody);
            if (other.attachedRigidbody == null)
                triggeringGameObject = other.gameObject;
            else
                triggeringGameObject = other.attachedRigidbody.gameObject;

            ControllerInput globalInput = other.GetComponentInParent<ControllerInput>();
            if (globalInput != null && !entered) {
                for (int i = 0; i < leftInputEvents.Length; i++)
                    if (leftInputEvents[i].events.Count > 0 &&
                        leftInputEvents[i].events[0].eventType != EventHandler.Type.Never) {

                        globalInput.leftInputEvents[i].events.Insert(0, leftInputEvents[i].events[0]);
                    }
                for (int i = 0; i < rightInputEvents.Length; i++)
                    if (rightInputEvents[i].events.Count > 0 &&
                        rightInputEvents[i].events[0].eventType != EventHandler.Type.Never) {

                        globalInput.rightInputEvents[i].events.Insert(0, rightInputEvents[i].events[0]);
                    }
                entered = true;
            }
        }

        protected virtual void OnTriggerExit(Collider other) {
            //Debug.Log(this.gameObject.name + " Trigger Exit");
            triggerHandlers.value = null;

            ControllerInput globalInput = other.GetComponentInParent<ControllerInput>();
            if (globalInput != null && entered) {
                for (int i = 0; i < leftInputEvents.Length; i++) {
                    if (leftInputEvents[i].events.Count > 0)
                        globalInput.leftInputEvents[i].events.RemoveAll(x => x == leftInputEvents[i].events[0]);
                }
                for (int i = 0; i < rightInputEvents.Length; i++) {
                    if (rightInputEvents[i].events.Count > 0)
                        globalInput.rightInputEvents[i].events.RemoveAll(x => x == rightInputEvents[i].events[0]);
                }
                entered = false;
            }
        }

        #endregion
    }
}