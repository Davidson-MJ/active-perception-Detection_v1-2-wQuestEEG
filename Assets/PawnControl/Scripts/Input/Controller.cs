using UnityEngine;

namespace Passer {

    public enum GameControllers {
        Generic,
        Xbox,
        PS4,
        Steelseries,
        GameSmart,
        Oculus,
        OpenVR,
#if hDAYDREAM && UNITY_ANDROID
        Daydream
#endif
    }

    /// <summary>Controller input for all controllers</summary>
    /// Max 4 controllers are supports
    public static class Controllers {
        private static int maxControllers = 4;
        /// <summary>Array containing all controllers</summary>
        public static Controller[] controllers;

        /// <summary>Update the current values of the controller input</summary>
        public static void Update() {
            if (controllers != null) {
                for (int i = 0; i < controllers.Length; i++) {
                    if (controllers[i] != null)
                        controllers[i].Update();
                }
            }
        }

        /// <summary>Retrieves a controller and creates it when it is first accessed</summary>
        /// <param name="controllerID">The index of the controller</param>
        public static Controller GetController(int controllerID) {
            if (controllers == null)
                controllers = new Controller[maxControllers];
            if (controllers[controllerID] == null)
                controllers[controllerID] = new Controller();            
       
            return controllers[controllerID];
        }

        /// <summary>Reset the values of all buttons</summary>
        public static void Clear() {
            if (controllers != null) {
                for (int i = 0; i < controllers.Length; i++) {
                    if (controllers[i] != null)
                        controllers[i].Clear();
                }
            }
        }

        /// <summary>Called at the end of the frame to indicate that new controller values can be read</summary>
        public static void EndFrame() {
            if (controllers != null) {
                for (int i = 0; i < controllers.Length; i++)
                    if (controllers[i] != null)
                        controllers[i].EndFrame();
            }
        }
    }

    /// <summary>Controller input for a single controller</summary>
    public class Controller {
        /// <summary>Identification for the left or right side of the controller</summary>
        public enum Side {
            Left,
            Right
        }
        /// <summary>Button identification values</summary>
        public enum Button {
            ButtonOne = 0,
            ButtonTwo = 1,
            ButtonThree = 2,
            ButtonFour = 3,
            Bumper = 10,
            BumperTouch = 11,
            Trigger = 12,
            TriggerTouch = 13,
            StickButton = 14,
            StickTouch = 15,
            //Up = 20,
            //Down = 21,
            //Left = 22,
            //Right = 23,
            Option = 30,
            None = 9999
        }

        /// <summary>The left side of the controller</summary>
        public ControllerSide left;
        /// <summary>The right side of the controller</summary>
        public ControllerSide right;

        /// <summary>Update the current values of the controller input</summary>
        public void Update() {
            left.Update();
            right.Update();
        }

        /// <summary>Constructor for access to the controller input</summary>
        public Controller() {
            left = new ControllerSide();
            right = new ControllerSide();
        }

        private bool cleared;
        /// <summary>Reset the values of all buttons</summary>
        public void Clear() {
            if (cleared)
                return;

            cleared = true;
            left.Clear();
            right.Clear();
        }

        /// <summary>Called at the end of the frame to indicate that new controller values can be read</summary>
        public void EndFrame() {
            cleared = false;
        }

        /// <summary>Retrieve the pressed state of a button</summary>
        /// <param name="side">The identification of the side of the controller</param>
        /// <param name="buttonID">The identification of the requested button</param>
        public bool GetButton(Side side, Button buttonID) {
            switch (side) {
                case Side.Left:
                    return left.GetButton(buttonID);
                case Side.Right:
                    return right.GetButton(buttonID);
                default:
                    return false;

            }
        }
    }

    /// <summary>Controller input for the left or right side of the controller (pair)</summary>
    public class ControllerSide {
        /// <summary>The vertical value of the thumbstick</summary>
        /// Values: -1..1
        public float stickHorizontal;
        /// <summary>The horizontal value of the thumbstick</summary>
        /// Values: -1..1
        public float stickVertical;
        /// <summary>The pressed state of the thumbstick</summary>
        public bool stickButton;
        /// <summary>The touched state of the thumbstick</summary>
        public bool stickTouch;

        /// <summary>The vertical value of the touchpad</summary>
        /// Values: -1..1
        public float touchpadVertical;
        /// <summary>The horizontal value of the touchpad</summary>
        /// Values: -1..1
        public float touchpadHorizontal;
        /// <summary>The pressed state of the touchpad</summary>
        public bool touchpadPress;
        /// <summary>The touched state of the touchpad</summary>
        public bool touchpadTouch;

        /// <summary>The pressed state of genertic buttons</summary>
        /// There can be up to 4 generic buttons.
        /// buttons[0] usually mathes the default fire button
        public bool[] buttons = new bool[4];

        /// <summary>The value of the first trigger</summary>
        /// Values: 0..1
        /// The first trigger is normally operated with the index finger
        public float trigger1;
        /// <summary>The value of the second trigger</summary>
        /// Values: 0..1
        /// The second trigger is normally operated with the middle finger
        public float trigger2;

        /// <summary>The pressed state of the option button</summary>
        /// The option button is usually a special button for accessing a specific menu
        public bool option;

        /// <summary>Event for handling Button down events</summary>
        public event OnButtonDown OnButtonDownEvent;
        /// <summary>Event for handling Button up events</summary>
        public event OnButtonUp OnButtonUpEvent;

        /// <summary>Function for processing button down events</summary>
        /// <param name="buttonNr">The idetification of the pressed button</param>
        public delegate void OnButtonDown(Controller.Button buttonNr);
        /// <summary>Function for processing button up events</summary>
        /// <param name="buttonNr">The identification of the released button</param>
        public delegate void OnButtonUp(Controller.Button buttonNr);

        private bool[] lastButtons = new bool[4];
        private bool lastBumper;
        private bool lastTrigger;
        private bool lastStickButton;
        private bool lastOption;

        /// <summary>Update the current values of the controller input</summary>
        public void Update() {
            for (int i = 0; i < 4; i++) {
                if (buttons[i] && !lastButtons[i]) {
                    if (OnButtonDownEvent != null)
                        OnButtonDownEvent((Controller.Button) i);

                } else if (!buttons[i] && lastButtons[i]) {
                    if (OnButtonUpEvent != null)
                        OnButtonUpEvent((Controller.Button) i);
                }
                lastButtons[i] = buttons[i];
            }

            if (trigger1 > 0.9F && !lastBumper) {
                if (OnButtonDownEvent != null)
                    OnButtonDownEvent(Controller.Button.Bumper);
                lastBumper = true;
            } else if (trigger1 < 0.1F && lastBumper) {
                if (OnButtonUpEvent != null)
                    OnButtonUpEvent(Controller.Button.Bumper);
                lastBumper = false;
            }

            if (trigger2 > 0.9F && !lastTrigger) {
                if (OnButtonDownEvent != null)
                    OnButtonDownEvent(Controller.Button.Trigger);
                lastTrigger = true;
            } else if (trigger2 < 0.1F && lastTrigger) {
                if (OnButtonUpEvent != null)
                    OnButtonUpEvent(Controller.Button.Trigger);
                lastTrigger = false;
            }

            if (stickButton && !lastStickButton) {
                if (OnButtonDownEvent != null)
                    OnButtonDownEvent(Controller.Button.StickButton);
            } else if (!stickButton && lastStickButton) {
                if (OnButtonUpEvent != null)
                    OnButtonUpEvent(Controller.Button.StickButton);
            }
            lastStickButton = stickButton;

            if (option && !lastOption) {
                if (OnButtonDownEvent != null)
                    OnButtonDownEvent(Controller.Button.Option);
            } else if (!option && lastOption) {
                if (OnButtonUpEvent != null)
                    OnButtonUpEvent(Controller.Button.Option);
            }
            lastOption = option;
        }

        /// <summary>Reset the values of all buttons</summary>
        public void Clear() {
            stickHorizontal = 0;
            stickVertical = 0;
            stickButton = false;
            stickTouch = false;

            touchpadHorizontal = 0;
            touchpadVertical = 0;
            touchpadPress = false;
            touchpadTouch = false;

            for (int i = 0; i < 4; i++)
                buttons[i] = false;

            trigger1 = 0;
            trigger2 = 0;

            option = false;
        }

        /// <summary>Retrieve the pressed state of a button</summary>
        /// <param name="buttonID">The identification of the requested button</param>
        public bool GetButton(Controller.Button buttonID) {
            switch (buttonID) {
                case Controller.Button.ButtonOne:
                    return buttons[0];
                case Controller.Button.ButtonTwo:
                    return buttons[1];
                case Controller.Button.ButtonThree:
                    return buttons[2];
                case Controller.Button.ButtonFour:
                    return buttons[3];
                case Controller.Button.Bumper:
                    return trigger1 > 0.9F;
                case Controller.Button.Trigger:
                    return trigger2 > 0.9F;
                case Controller.Button.StickButton:
                    return stickButton;
                case Controller.Button.StickTouch:
                    return stickTouch;
                case Controller.Button.Option:
                    return option;
                default:
                    return false;
            }
        }
    }
}

/*
#if PLAYMAKER
namespace HutongGames.PlayMaker.Actions {

    [ActionCategory("InstantVR")]
    [Tooltip("Controller input axis")]
    public class GetControllerAxis : FsmStateAction {

        [RequiredField]
        [Tooltip("Left or right (side) controller")]
        public BodySide controllerSide = BodySide.Left;

        [RequiredField]
        [UIHint(UIHint.Variable)]
        [Tooltip("Store the direction vector.")]
        public FsmVector3 storeVector;

        private IVR.ControllerInput controller0;

        public override void Awake() {
            controller0 = IVR.Controllers.GetController(0);
        }

        public override void OnUpdate() {
            IVR.ControllerInputSide controller = (controllerSide == BodySide.Left) ? controller0.left : controller0.right;

            storeVector.Value = new Vector3(controller.stickHorizontal, 0, controller.stickVertical);
        }
    }

    [ActionCategory("InstantVR")]
    [Tooltip("Controller input button")]
    public class GetControllerButton : FsmStateAction {

        [RequiredField]
        [Tooltip("Left or right (side) controller")]
        public BodySide controllerSide = BodySide.Right;

        [RequiredField]
        [UIHint(UIHint.Variable)]
        [Tooltip("Controller Button")]
        public ControllerButton button;

        [UIHint(UIHint.Variable)]
        [Tooltip("Store Result Bool")]
        public FsmBool storeBool;

        [UIHint(UIHint.Variable)]
        [Tooltip("Store Result Float")]
        public FsmFloat storeFloat;

        [Tooltip("Event to send when the button is pressed.")]
        public FsmEvent buttonPressed;

        [Tooltip("Event to send when the button is released.")]
        public FsmEvent buttonReleased;

        private IVR.ControllerInput controller0;

        public override void Awake() {
            controller0 = IVR.Controllers.GetController(0);
        }

        public override void OnUpdate() {
            IVR.ControllerInputSide controller = (controllerSide == BodySide.Left) ? controller0.left : controller0.right;

            bool oldBool = storeBool.Value;

            switch (button) {
                case ControllerInput.Button.StickButton:
                    storeBool.Value = controller.stickButton;
                    storeFloat.Value = controller.stickButton ? 1 : 0;
                    break;
                case ControllerInput.Button.Up:
                    storeBool.Value = controller.up;
                    storeFloat.Value = controller.up ? 1 : 0;
                    break;
                case ControllerInput.Button.Down:
                    storeBool.Value = controller.down;
                    storeFloat.Value = controller.down ? 1 : 0;
                    break;
                case ControllerInput.Button.Left:
                    storeBool.Value = controller.left;
                    storeFloat.Value = controller.left ? 1 : 0;
                    break;
                case ControllerInput.Button.Right:
                    storeBool.Value = controller.right;
                    storeFloat.Value = controller.right ? 1 : 0;
                    break;
                case ControllerInput.Button.Button0:
                    storeBool.Value = controller.buttons[0];
                    storeFloat.Value = controller.buttons[0] ? 1 : 0;
                    break;
                case ControllerInput.Button.Button1:
                    storeBool.Value = controller.buttons[1];
                    storeFloat.Value = controller.buttons[1] ? 1 : 0;
                    break;
                case ControllerInput.Button.Button2:
                    storeBool.Value = controller.buttons[2];
                    storeFloat.Value = controller.buttons[2] ? 1 : 0;
                    break;
                case ControllerInput.Button.Button3:
                    storeBool.Value = controller.buttons[3];
                    storeFloat.Value = controller.buttons[3] ? 1 : 0;
                    break;
                case ControllerInput.Button.Option:
                    storeBool.Value = controller.option;
                    storeFloat.Value = controller.option ? 1 : 0;
                    break;
                case ControllerInput.Button.Bumper:
                    storeBool.Value = controller.bumper > 0.9F;
                    storeFloat.Value = controller.bumper;
                    break;
                case ControllerInput.Button.Trigger:
                    storeBool.Value = controller.trigger > 0.9F;
                    storeFloat.Value = controller.trigger;
                    break;
            }

            if (storeBool.Value && !oldBool)
                Fsm.Event(buttonPressed);
            else if (!storeBool.Value && oldBool)
                Fsm.Event(buttonReleased);
        }
    }
}
#endif
*/