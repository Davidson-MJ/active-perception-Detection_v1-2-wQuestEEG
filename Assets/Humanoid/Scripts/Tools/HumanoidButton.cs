using Passer;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Passer.Humanoid {
    /// <summary>Unity UI button with information on which humanoid pressed the button</summary>
    /// Unity provides an great UI system which includes a Button component which can call functions on objects when it is pressed.
    /// A limitation is that you cannot determine who has pressed the button which can be useful in multiplayer environments.
    /// For this case we provide the Humanoid Button.
    /// When a Humanoid Button is pressed, a function can be called which takes a HumanoidControl parameter
    /// representing the humanoid who pressed the button. This parameter can be used to make the functionality dependent on who pressed the button.
    [HelpURLAttribute("https://passervr.com/documentation/humanoid-control/tools/humanoid-button/")]
    public class HumanoidButton : Button {

        /// <summary>The Event taking a HumanoidControl parameter</summary>
        [System.Serializable]
        public class HumanoidEvent : UnityEvent<HumanoidControl> { }

        /// <summary>The onClick event which replaces the standard onClick event</summary>
        /// This version takes an HumanoidControl parameter
        /// The standard does not take a parameter
        public new HumanoidEvent onClick = new HumanoidEvent();

        protected void Press(BaseEventData eventData) {
            if (!IsActive() || !IsInteractable())
                return;

            // Get the originator GameObject who clicked the button
            GameObject originator = eventData.currentInputModule.gameObject;
            if (originator == null) {
                Debug.LogError("Could not find the originator for this button click");
                return;
            }

            // Get the humanoid on the originator
            // and check if it exists
            HumanoidControl humanoid = originator.GetComponent<HumanoidControl>();
            if (humanoid == null) {
                Debug.LogError("Could not find the humanoid for this button click");
                return;
            }

            // Call the button click function with the humanoid as parameter
            onClick.Invoke(humanoid);
        }

        /// <summary>This function is called when the button is clicked</summary>
        /// <param name="eventData">Event payload associated with the humanoid</param>
        public override void OnPointerClick(PointerEventData eventData) {
            base.OnPointerClick(eventData);

            Press(eventData);
        }

        /// <summary>This function is called when the button is activated with the default button.</summary>
        /// This is not supported by Humanoid Control, but added for completeness
        /// /// <param name="eventData">Event payload associated with the humanoid</param>
        public override void OnSubmit(BaseEventData eventData) {
            base.OnSubmit(eventData);

            Press(eventData);
        }
    }
}