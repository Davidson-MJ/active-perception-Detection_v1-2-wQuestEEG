using UnityEngine;

namespace Passer {
    using Humanoid;

    /// <summary>
    /// The Menu Manager uses two Interaction Pointers for each hand:
    /// - An (straight) interaction pointer to interact with the menu when it is active
    /// - An (curved) interaction pointer to do teleporting when the menu is not active.
    /// Both pairs of interaction pointers needs to be set in the Inspector to work correctly.
    /// 
    /// The Menu Manager uses a Trigger Sphere Collider to detect if the user has moved too 
    /// far the the menu to interact with it.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class MenuManager : MonoBehaviour {

        public HumanoidControl humanoid;
        public float menuDistance = 0.5F;

        public InteractionPointer leftMenuPointer;
        public InteractionPointer rightMenuPointer;

        public InteractionPointer leftTeleporter;
        public InteractionPointer rightTeleporter;

        /// <summary>
        /// Initialization
        /// </summary>
        protected virtual void Awake() {
            // If the humanoid is not set, try to detect the local humanoid player
            if (humanoid == null)
                humanoid = FindHumanoid();

            if (humanoid != null) {
                InitInteractionPointers(humanoid);
                SetControllerInput(humanoid);
            }

            // Sphere collider needs to be a trigger collider
            SphereCollider collider = GetComponent<SphereCollider>();
            if (collider != null)
                collider.isTrigger = true;

            // We start with the menu disabled
            SetMenuActive(false);
        }

        /// <summary>
        /// Tries to find the local Humanoid
        /// </summary>
        /// <returns>The found humanoid, null if no local humanoid has been found</returns>
        protected HumanoidControl FindHumanoid() {
            HumanoidControl[] humanoids = FindObjectsOfType<HumanoidControl>();
            for (int i = 0; i < humanoids.Length; i++) {
                if (humanoids[i].isRemote == false)
                    return humanoids[i];
            }
            return null;
        }

        /// <summary>
        /// Detects the Teleporter and Menu Interaction pointer on the humanoid
        /// </summary>
        /// <param name="humanoid">The humanoid for which the interaction pointers need to be found</param>
        protected void InitInteractionPointers(HumanoidControl humanoid) {
            leftTeleporter = humanoid.leftHandTarget.GetComponentInChildren<Passer.Humanoid.ColoringInteractionPointer>();
            rightTeleporter = humanoid.rightHandTarget.GetComponentInChildren<Passer.Humanoid.ColoringInteractionPointer>();

            leftMenuPointer = GetInteractionPointer(humanoid.leftHandTarget, leftTeleporter);
            rightMenuPointer = GetInteractionPointer(humanoid.rightHandTarget, rightTeleporter);
        }

        /// <summary>
        /// Find interaction pointer on the hand
        /// </summary>
        /// <param name="handTarget">The hand to which the Interaction Pointer should be attached</param>
        /// <param name="invalidPointer">(optional) when give, the interaction pointer should be not euqual to the invalidPointer</param>
        /// <returns>The found interaction pointer</returns>
        protected InteractionPointer GetInteractionPointer(HandTarget handTarget, InteractionPointer invalidPointer = null) {
            InteractionPointer[] interactionPointers = handTarget.GetComponentsInChildren<InteractionPointer>();
            foreach (InteractionPointer interactionPointer in interactionPointers) {
                if (interactionPointer != invalidPointer)
                    return interactionPointer;
            }
            return null;
        }

        protected void SetControllerInput(HumanoidControl humanoid) {
            ControllerInput controllerInput = humanoid.GetComponent<ControllerInput>();
            if (controllerInput != null) {
                //controllerInput.leftOptionInput.SetMethod(SetMenuActive, InputEvent.EventType.Start);
                //controllerInput.rightOptionInput.SetMethod(SetMenuActive, InputEvent.EventType.Start);
                controllerInput.SetEventHandler(true, ControllerInput.SideButton.Option, SetMenuActive);
                controllerInput.SetEventHandler(false, ControllerInput.SideButton.Option, SetMenuActive);
            }
        }

        /// <summary>
        /// Activates or deactivates the menu and updates the interaction pointer behaviour
        /// </summary>
        /// <param name="active">Indication whether the menu has to be active</param>
        public void SetMenuActive(bool active) {
            if (humanoid == null)
                return;

            ControllerInput controllerInput = humanoid.GetComponent<ControllerInput>();

            // Hide the menu when it is visble (toggle function) or
            // when it is set to be inactive explicitly
            if (MenuActive() || active == false) {
                HideMenu();
                DisableMenuPointer(controllerInput);
                EnableTeleporter(controllerInput);
            }
            else {
                ShowMenu();
                DisableTeleporter(controllerInput);
                EnableMenuPointer(controllerInput);

                AdjustOutOfRangeDistance();
            }
        }

        /// <summary>
        /// Trigger handler for when an object moves out of the sphere collider
        /// </summary>
        protected void OnTriggerExit(Collider other) {

            if (MenuActive()) {
                // Is it the humanoid exiting the range sphere?
                HumanoidControl triggeringHumanoid = other.GetComponentInParent<HumanoidControl>();
                if (triggeringHumanoid == humanoid)
                    SetMenuActive(false);
            }
        }

        /// <summary>
        /// Show the menu at 'distance' from the players' head.
        /// But take core it is not placed inside objects.
        /// If this happens, the menu is places closer to the humanoid.
        /// </summary>
        protected void ShowMenu() {
            float distance = menuDistance;

            // check if the menu will intersect with an object
            RaycastHit hit;
            Vector3 direction = humanoid.headTarget.transform.forward;
            Vector3 origin = humanoid.headTarget.transform.position + direction * 0.1F;
            if (Physics.Raycast(origin, direction, out hit, menuDistance))
                distance = hit.distance;

            // Position the menu at 'distance' from the player's head
            this.transform.position = humanoid.headTarget.transform.TransformPoint(0, 0, distance);
            this.transform.rotation = Quaternion.LookRotation(humanoid.headTarget.transform.forward, Vector3.up);

            this.gameObject.SetActive(true);
        }

        /// <summary>
        /// Hide the menu
        /// </summary>
        protected void HideMenu() {
            this.gameObject.SetActive(false);
        }

        /// <summary>
        /// Is the menu currently visible?
        /// </summary>
        /// <returns>boolean indicating whether the menu is visible</returns>
        protected bool MenuActive() {
            return this.gameObject.activeInHierarchy;
        }

        /// <summary>
        /// Enable the Menu Pointer. This will also set the controller input.
        /// </summary>
        /// <param name="controllerInput">The ControllerInput to update</param>
        protected void EnableMenuPointer(ControllerInput controllerInput) {
            // activate the interaction pointer
            if (leftMenuPointer != null)
                leftMenuPointer.Activation(true);
            if (rightMenuPointer != null)
                rightMenuPointer.Activation(true);

            // enable the button click
            if (controllerInput != null) {
                if (leftMenuPointer != null)
                    controllerInput.SetEventHandler(true, ControllerInput.SideButton.Trigger1, leftMenuPointer.Click);
                if (rightMenuPointer != null)
                    controllerInput.SetEventHandler(false, ControllerInput.SideButton.Trigger1, rightMenuPointer.Click);
            }
        }

        /// <summary>
        /// Disable the Menu Pointer. This will also disable the controller input.
        /// </summary>
        /// <param name="controllerInput">The ControllerInput to update</param>
        protected void DisableMenuPointer(ControllerInput controllerInput) {
            // deactivate the interaction pointer
            if (leftMenuPointer != null)
                leftMenuPointer.Activation(false);
            if (rightMenuPointer != null)
                rightMenuPointer.Activation(false);

            // disable the button click
            if (controllerInput != null) {
                controllerInput.SetEventHandler(true, ControllerInput.SideButton.Trigger1, null);
                controllerInput.SetEventHandler(false, ControllerInput.SideButton.Trigger1, null);
            }
        }

        /// <summary>
        /// Enable the teleporter. This will also set the controller input.
        /// </summary>
        /// <param name="controllerInput">The ControllerInput to update</param>
        protected void EnableTeleporter(ControllerInput controllerInput) {
            // enable the button activation and click
            if (controllerInput != null) {
                if (leftTeleporter != null) {
                    controllerInput.SetEventHandler(true, ControllerInput.SideButton.StickButton, leftTeleporter.Activation);
                    controllerInput.SetEventHandler(true, ControllerInput.SideButton.Trigger1, leftTeleporter.Click);
                }
                if (rightTeleporter != null) {
                    controllerInput.SetEventHandler(false, ControllerInput.SideButton.StickButton, rightTeleporter.Activation);
                    controllerInput.SetEventHandler(false, ControllerInput.SideButton.Trigger1, rightTeleporter.Click);
                }
            }
        }

        /// <summary>
        /// Disable the Teleporter. This will also disable the controller input
        /// </summary>
        /// <param name="controllerInput">The ControllerInput to update</param>
        protected void DisableTeleporter(ControllerInput controllerInput) {
            // deactivate the interaction pointer
            if (leftTeleporter != null)
                leftTeleporter.Activation(false);
            if (rightTeleporter != null)
                rightTeleporter.Activation(false);

            // disable the button activation
            if (controllerInput != null) {
                controllerInput.SetEventHandler(true, ControllerInput.SideButton.StickButton, null);
                controllerInput.SetEventHandler(false, ControllerInput.SideButton.StickButton, null);
            }
        }

        /// <summary>
        /// Adjust trigger sphere collider radius for out-of-range
        /// </summary>
        protected void AdjustOutOfRangeDistance() {
            SphereCollider collider = GetComponent<SphereCollider>();
            if (collider != null) {
                float radius = collider.radius;
                if (leftMenuPointer != null)
                    radius = Mathf.Max(radius, leftMenuPointer.maxDistance);
                if (rightMenuPointer != null)
                    radius = Mathf.Max(radius, rightMenuPointer.maxDistance);
                collider.radius = radius;
            }
        }
    }

}