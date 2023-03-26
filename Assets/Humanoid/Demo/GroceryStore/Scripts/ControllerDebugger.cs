using UnityEngine;
using System.Collections;

using Passer;

public class ControllerDebugger : MonoBehaviour {

    [System.Serializable]
    public struct ControllerSideDebugger {
        public float stickHorizontal;
        public float stickVertical;
        public bool stickButton;
        public bool stickTouch;

        public float touchpadHorizontal;
        public float touchpadVertical;
        public bool touchpadButton;
        public bool touchpadTouch;

        public bool[] buttons;

        public float bumper;
        public float trigger;

        public bool option;
    }

    public ControllerSideDebugger left;
    public ControllerSideDebugger right;

    private Controller controller;

	private void Start () {
        controller = Controllers.GetController(0);

        left.buttons = new bool[4];
        right.buttons = new bool[4];
	}
	
	private void Update () {
        UpdateSide(ref left, controller.left);
        UpdateSide(ref right, controller.right);
	}

    void UpdateSide(ref ControllerSideDebugger sideDebugger, ControllerSide controllerSide) {
        sideDebugger.stickHorizontal = controllerSide.stickHorizontal;
        sideDebugger.stickVertical = controllerSide.stickVertical;
        sideDebugger.stickButton = controllerSide.stickButton;
        sideDebugger.stickTouch = controllerSide.stickTouch;

        sideDebugger.touchpadHorizontal = controllerSide.touchpadHorizontal;
        sideDebugger.touchpadVertical = controllerSide.touchpadVertical;
        sideDebugger.touchpadButton = controllerSide.touchpadPress;
        sideDebugger.touchpadTouch = controllerSide.touchpadTouch;

        sideDebugger.buttons[0] = controllerSide.buttons[0];
        sideDebugger.buttons[1] = controllerSide.buttons[1];
        sideDebugger.buttons[2] = controllerSide.buttons[2];
        sideDebugger.buttons[3] = controllerSide.buttons[3];

        sideDebugger.bumper = controllerSide.trigger1;
        sideDebugger.trigger = controllerSide.trigger2;

        sideDebugger.option = controllerSide.option;
    }
}
