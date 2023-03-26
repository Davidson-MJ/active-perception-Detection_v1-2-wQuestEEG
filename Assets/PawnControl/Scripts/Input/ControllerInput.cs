using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Passer {
    using Pawn;
#if pHUMANOID
    using Humanoid;
#endif

    /// <summary>A unified method for using controller input</summary>
    /// ControllerInput can be used to access the state and change of input buttons, touchpads and thumbsticks
    /// on any kind of input controller including game controllers and VR/XR controllers.
    public class ControllerInput : MonoBehaviour {

        public enum SideButton {
            StickVertical,
            StickHorizontal,
            StickButton,
            TouchpadVertical,
            TouchpadHorizontal,
            TouchpadButton,
            Button1,
            Button2,
            Button3,
            Button4,
            Trigger1,
            Trigger2,
            Option,
        }

        protected PawnControl pawn;
#if pHUMANOID
        public HumanoidControl humanoid;
        protected Humanoid.HandTarget leftHandTarget;
        protected Humanoid.HandTarget rightHandTarget;

        public bool fingerMovements = true;
#endif

        /// <summary>The game controller type for this pawn</summary>
        public GameControllers gameController;
        //private TraditionalDevice traditionalInput = new TraditionalDevice();

        public float leftStickVertical { get { return controller.left.stickVertical; } }
        public float leftStickHorizontal { get { return controller.left.stickHorizontal; } }

        public float leftTouchpadVertical { get { return controller.left.touchpadVertical; } }
        public float leftTouchpadHorizontal { get { return controller.left.touchpadHorizontal; } }

        public float leftTrigger1 { get { return controller.left.trigger1; } }
        public bool leftTrigger1Touched { get { return controller.left.trigger1 > 0; } }
        public bool leftTrigger1Pressed { get { return controller.left.trigger1 > 0.8F; } }
        public float leftTrigger2 { get { return controller.left.trigger2; } }
        public bool leftTrigger2Touched { get { return controller.left.trigger2 > 0; } }
        public bool leftTrigger2Pressed { get { return controller.left.trigger2 > 0.8F; } }
        public bool leftButton1Pressed { get { return controller.left.buttons[0]; } }
        public bool leftButton2Pressed { get { return controller.left.buttons[1]; } }
        public bool leftButton3Pressed { get { return controller.left.buttons[2]; } }
        public bool leftButton4Pressed { get { return controller.left.buttons[3]; } }
        public bool leftOptionPressed { get { return controller.left.option; } }


        public float rightStickVertical { get { return controller.right.stickVertical; } }
        public float rightStickHorizontal { get { return controller.right.stickHorizontal; } }

        public float rightTouchpadVertical { get { return controller.right.touchpadVertical; } }
        public float rightTouchpadHorizontal { get { return controller.right.touchpadHorizontal; } }

        public float rightTrigger1 { get { return controller.right.trigger1; } }
        public bool rightTrigger1Touched { get { return controller.right.trigger1 > 0; } }
        public bool rightTrigger1Pressed { get { return controller.right.trigger1 > 0.8F; } }
        public float rightTrigger2 { get { return controller.right.trigger2; } }
        public bool rightTrigger2Touched { get { return controller.right.trigger2 > 0; } }
        public bool rightTrigger2Pressed { get { return controller.right.trigger2 > 0.8F; } }
        public bool rightButton1Pressed { get { return controller.right.buttons[0]; } }
        public bool rightButton2Pressed { get { return controller.right.buttons[1]; } }
        public bool rightButton3Pressed { get { return controller.right.buttons[2]; } }
        public bool rightButton4Pressed { get { return controller.right.buttons[3]; } }
        public bool rightOptionPressed { get { return controller.left.option; } }

        protected static string[] eventTypeLabels = new string[] {
                "Never",
                "On Press",
                "On Release",
                "While Down",
                "While Up",
                "On Change",
                "Continuous"
        };

        public ControllerEventHandlers[] leftInputEvents = new ControllerEventHandlers[] {
            new ControllerEventHandlers() { label = "Left Vertical", id = 0, defaultParameterProperty = "leftStickVertical" },
            new ControllerEventHandlers() { label = "Left Horizontal", id = 1, defaultParameterProperty = "leftStickHorizontal" },
            new ControllerEventHandlers() { label = "Left Stick Button", id = 2, eventTypeLabels = eventTypeLabels },
            new ControllerEventHandlers() { label = "Left Touchpad Vertical", id = 3, defaultParameterProperty = "leftTouchpadVertical" },
            new ControllerEventHandlers() { label = "Left Touchpad Horizontal", id = 4, defaultParameterProperty = "leftTouchpadHorizontal" },
            new ControllerEventHandlers() { label = "Left Touchpad Button", id = 5, eventTypeLabels = eventTypeLabels },
            new ControllerEventHandlers() { label = "Left Button 1", id = 6, eventTypeLabels = eventTypeLabels },
            new ControllerEventHandlers() { label = "Left Button 2", id = 7, eventTypeLabels = eventTypeLabels },
            new ControllerEventHandlers() { label = "Left Button 3", id = 8, eventTypeLabels = eventTypeLabels },
            new ControllerEventHandlers() { label = "Left Button 4", id = 9, eventTypeLabels = eventTypeLabels },
            new ControllerEventHandlers() { label = "Left Trigger 1", id = 10, eventTypeLabels = eventTypeLabels },
            new ControllerEventHandlers() { label = "Left Trigger 2", id = 11, eventTypeLabels = eventTypeLabels },
            new ControllerEventHandlers() { label = "Left Option", id = 12, eventTypeLabels = eventTypeLabels },
        };
        public ControllerEventHandlers[] rightInputEvents = new ControllerEventHandlers[] {
            new ControllerEventHandlers() { label = "Right Vertical", id = 0, eventTypeLabels = eventTypeLabels },
            new ControllerEventHandlers() { label = "Right Horizontal", id = 1, eventTypeLabels = eventTypeLabels },
            new ControllerEventHandlers() { label = "Right Stick Button", id = 2, eventTypeLabels = eventTypeLabels },
            new ControllerEventHandlers() { label = "Right Touchpad Vertical", id = 3, eventTypeLabels = eventTypeLabels },
            new ControllerEventHandlers() { label = "Right Touchpad Horizontal", id = 4, eventTypeLabels = eventTypeLabels },
            new ControllerEventHandlers() { label = "Right Touchpad Button", id = 5, eventTypeLabels = eventTypeLabels },
            new ControllerEventHandlers() { label = "Right Button 1", id = 6, eventTypeLabels = eventTypeLabels },
            new ControllerEventHandlers() { label = "Right Button 2", id = 7, eventTypeLabels = eventTypeLabels },
            new ControllerEventHandlers() { label = "Right Button 3", id = 8, eventTypeLabels = eventTypeLabels },
            new ControllerEventHandlers() { label = "Right Button 4", id = 9, eventTypeLabels = eventTypeLabels },
            new ControllerEventHandlers() { label = "Right Trigger 1", id = 10, eventTypeLabels = eventTypeLabels },
            new ControllerEventHandlers() { label = "Right Trigger 2", id = 11, eventTypeLabels = eventTypeLabels },
            new ControllerEventHandlers() { label = "Right Option", id = 12, eventTypeLabels = eventTypeLabels },
        };

        protected Controller controller;

        public ControllerEventHandlers GetInputEvent(bool isLeft, SideButton controllerButton) {
            if (isLeft)
                return GetInputEvent(leftInputEvents, controllerButton);
            else
                return GetInputEvent(rightInputEvents, controllerButton);
        }

        private ControllerEventHandlers GetInputEvent(ControllerEventHandlers[] inputEvents, SideButton controllerButton) {
            if (inputEvents.Length < 13) {
                int ix = (int)controllerButton;
                if (ix < 3)
                    return inputEvents[ix];
                else
                    return inputEvents[ix - 3];
            }
            else
                return inputEvents[(int)controllerButton];
        }

        protected virtual void Awake() {
            pawn = GetComponent<PawnControl>();
#if pHUMANOID
            humanoid = GetComponent<HumanoidControl>();
            if (humanoid != null) {
                leftHandTarget = humanoid.leftHandTarget;
                rightHandTarget = humanoid.rightHandTarget;
            }
#endif
        }

        protected virtual void Start() {
            //    traditionalInput.SetControllerID(0);
            controller = Controllers.GetController(0);
        }

        protected virtual void Update() {
            if (leftInputEvents.Length < 13) {
                UpdateInputList(leftInputEvents[0], Mathf.Clamp(controller.left.stickVertical + controller.left.touchpadVertical, -1, 1));
                UpdateInputList(leftInputEvents[1], Mathf.Clamp(controller.left.stickHorizontal + controller.left.touchpadHorizontal, -1, 1));
                UpdateInputList(leftInputEvents[2], Mathf.Clamp(
                    (controller.left.stickTouch ? 0 : -1) + (controller.left.stickButton ? 1 : 0) +
                    (controller.left.touchpadTouch ? 0 : -1) + (controller.left.touchpadPress ? 1 : 0),
                    -1, 1));
                UpdateInputList(leftInputEvents[3], controller.left.buttons[0] ? 1 : 0);
                UpdateInputList(leftInputEvents[4], controller.left.buttons[1] ? 1 : 0);
                UpdateInputList(leftInputEvents[5], controller.left.buttons[2] ? 1 : 0);
                UpdateInputList(leftInputEvents[6], controller.left.buttons[3] ? 1 : 0);
                UpdateInputList(leftInputEvents[7], controller.left.trigger1);
                UpdateInputList(leftInputEvents[8], controller.left.trigger2);
                UpdateInputList(leftInputEvents[9], controller.left.option ? 1 : 0);
            }
            else {
                UpdateInputList(leftInputEvents[0], controller.left.stickVertical);
                UpdateInputList(leftInputEvents[1], controller.left.stickHorizontal);
                UpdateInputList(leftInputEvents[2], (controller.left.stickTouch ? 0 : -1) + (controller.left.stickButton ? 1 : 0));
                UpdateInputList(leftInputEvents[3], controller.left.touchpadVertical);
                UpdateInputList(leftInputEvents[4], controller.left.touchpadHorizontal);
                UpdateInputList(leftInputEvents[5], (controller.left.touchpadTouch ? 0 : -1) + (controller.left.touchpadPress ? 1 : 0));
                UpdateInputList(leftInputEvents[6], controller.left.buttons[0] ? 1 : 0);
                UpdateInputList(leftInputEvents[7], controller.left.buttons[1] ? 1 : 0);
                UpdateInputList(leftInputEvents[8], controller.left.buttons[2] ? 1 : 0);
                UpdateInputList(leftInputEvents[9], controller.left.buttons[3] ? 1 : 0);
                UpdateInputList(leftInputEvents[10], controller.left.trigger1);
                UpdateInputList(leftInputEvents[11], controller.left.trigger2);
                UpdateInputList(leftInputEvents[12], controller.left.option ? 1 : 0);
            }

            if (rightInputEvents.Length < 13) {
                UpdateInputList(rightInputEvents[0], Mathf.Clamp(controller.right.stickVertical + controller.right.touchpadVertical, -1, 1));
                UpdateInputList(rightInputEvents[1], Mathf.Clamp(controller.right.stickHorizontal + controller.right.touchpadHorizontal, -1, 1));
                UpdateInputList(rightInputEvents[2], Mathf.Clamp(
                    (controller.right.stickTouch ? 0 : -1) + (controller.right.stickButton ? 1 : 0) +
                    (controller.right.touchpadTouch ? 0 : -1) + (controller.right.touchpadPress ? 1 : 0),
                    -1, 1));
                UpdateInputList(rightInputEvents[3], controller.right.buttons[0] ? 1 : 0);
                UpdateInputList(rightInputEvents[4], controller.right.buttons[1] ? 1 : 0);
                UpdateInputList(rightInputEvents[5], controller.right.buttons[2] ? 1 : 0);
                UpdateInputList(rightInputEvents[6], controller.right.buttons[3] ? 1 : 0);
                UpdateInputList(rightInputEvents[7], controller.right.trigger1);
                UpdateInputList(rightInputEvents[8], controller.right.trigger2);
                UpdateInputList(rightInputEvents[9], controller.right.option ? 1 : 0);
            }
            else {
                UpdateInputList(rightInputEvents[0], controller.right.stickVertical);
                UpdateInputList(rightInputEvents[1], controller.right.stickHorizontal);
                UpdateInputList(rightInputEvents[2], (controller.right.stickTouch ? 0 : -1) + (controller.right.stickButton ? 1 : 0));
                UpdateInputList(rightInputEvents[3], controller.right.touchpadVertical);
                UpdateInputList(rightInputEvents[4], controller.right.touchpadHorizontal);
                UpdateInputList(rightInputEvents[5], (controller.right.touchpadTouch ? 0 : -1) + (controller.right.touchpadPress ? 1 : 0));
                UpdateInputList(rightInputEvents[6], controller.right.buttons[0] ? 1 : 0);
                UpdateInputList(rightInputEvents[7], controller.right.buttons[1] ? 1 : 0);
                UpdateInputList(rightInputEvents[8], controller.right.buttons[2] ? 1 : 0);
                UpdateInputList(rightInputEvents[9], controller.right.buttons[3] ? 1 : 0);
                UpdateInputList(rightInputEvents[10], controller.right.trigger1);
                UpdateInputList(rightInputEvents[11], controller.right.trigger2);
                UpdateInputList(rightInputEvents[12], controller.right.option ? 1 : 0);
            }

#if pHUMANOID
            if (fingerMovements)
                UpdateFingerMovements();
#endif
        }

        //protected void UpdateGameControllerInput() {
        //    Debug.Log("2");
        //    traditionalInput.UpdateGameController(gameController);
        //}

        protected void UpdateInputList(ControllerEventHandlers inputEventList, float value) {
            foreach (ControllerEventHandler inputEvent in inputEventList.events)
                inputEvent.floatValue = value;
        }

        public void SetEventHandler(bool isLeft, SideButton sideButton, UnityAction<bool> boolEvent) {
            ControllerEventHandlers eventHandlers = GetInputEvent(isLeft, sideButton);
            SetBoolMethod(gameObject, eventHandlers, EventHandler.Type.OnChange, boolEvent);

        }

        public static void SetBoolMethod(GameObject gameObject, ControllerEventHandlers eventHandlers, EventHandler.Type eventType, UnityAction<bool> boolEvent) {
            if (boolEvent == null)
                return;

            Object target = (Object)boolEvent.Target;
            string methodName = boolEvent.Method.Name;
            methodName = target.GetType().Name + "/" + methodName;

            if (eventHandlers.events == null || eventHandlers.events.Count == 0)
                eventHandlers.events.Add(new ControllerEventHandler(gameObject, eventType));
            else
                eventHandlers.events[0].eventType = eventType;

            ControllerEventHandler eventHandler = eventHandlers.events[0];
            eventHandler.functionCall.targetGameObject = FunctionCall.GetGameObject(target);
            eventHandler.functionCall.methodName = methodName;
            eventHandler.functionCall.AddParameter();
            FunctionCall.Parameter parameter = eventHandler.functionCall.AddParameter();
            parameter.type = FunctionCall.ParameterType.Bool;
            parameter.localProperty = "From Event";
            parameter.fromEvent = true;
        }

#if pHUMANOID
        protected void UpdateFingerMovements() {
            if (leftHandTarget != null)
                UpdateFingerMovementsSide(leftHandTarget.fingers, controller.left);
            if (rightHandTarget != null)
                UpdateFingerMovementsSide(rightHandTarget.fingers, controller.right);
        }

        protected void UpdateFingerMovementsSide(FingersTarget fingers, ControllerSide controllerSide) {
            //float thumbCurl = Mathf.Max(controllerSide.trigger2, controllerSide.trigger1);
            //fingers.thumb.curl = controllerSide.stickTouch ? thumbCurl + 0.3F : thumbCurl;
            //fingers.index.curl = controllerSide.trigger1;
            //fingers.middle.curl = Mathf.Max(controllerSide.trigger2, controllerSide.trigger1);
            //fingers.ring.curl = Mathf.Max(controllerSide.trigger2, controllerSide.trigger1);
            //fingers.little.curl = Mathf.Max(controllerSide.trigger2, controllerSide.trigger1);

            float thumbCurl = Mathf.Max(controllerSide.trigger2, controllerSide.trigger1);
            fingers.thumb.curl = !controllerSide.stickTouch ? -0.5F : thumbCurl;

            float indexValue = controllerSide.trigger1;
            SetFingerCurl(fingers.index, indexValue);

            float fingersValue = Mathf.Max(controllerSide.trigger2, controllerSide.trigger1);
            SetFingerCurl(fingers.middle, fingersValue);
            SetFingerCurl(fingers.ring, fingersValue);
            SetFingerCurl(fingers.little, fingersValue);
        }

        private void SetFingerCurl(FingersTarget.TargetedFinger finger, float inputValue) {
            if (inputValue < 0)
                finger.curl = 0.1F * inputValue;
            else
                finger.curl = inputValue;
        }

#endif
        public void SetRightTrigger1Value(float value) {
            rightInputEvents[3].floatValue = value;
            rightInputEvents[7].floatValue = value;
        }
    }
}