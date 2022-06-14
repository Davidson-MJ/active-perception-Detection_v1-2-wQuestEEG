using UnityEngine;
using Valve.VR;

public class ViveInput : MonoBehaviour
{

    /// <summary>
    ///  Mapping VIVE controller input to actions.
    ///  Make sure the specified Action (e.g. GrabPinch, Target detect) etc, is assigned 
    ///  to TriggerClick in the Inspector window.
    ///  
    ///  New actions need to be bound to the Vive action settings,
    /// Window > SteamVR Input > Open binding UI...

    /// </summary>

    // Name 
    public SteamVR_Action_Boolean triggerClick; // grabPinch is the trigger, here boolean is returned.
    public SteamVR_Action_Single triggerSqueeze; // single value between 0 and 1.

    // Source handling

    // separate hand tracking.
    private SteamVR_Input_Sources LeftHand = SteamVR_Input_Sources.LeftHand;
    private SteamVR_Input_Sources RightHand = SteamVR_Input_Sources.RightHand;

    public float triggerValue;
    public bool clickState;
    public bool clickLeft;
    public bool clickRight;
    public bool clickBoth; // used for trial start.

    private void Start() { } // scripts without Start cannot be disabled in Editor

    private void Update()
    {
        triggerValue = triggerSqueeze.GetAxis(SteamVR_Input_Sources.Any);
        // Debug.Log("Trigger Value is: " + triggerValue);

        clickState = triggerClick.GetStateDown(SteamVR_Input_Sources.Any);

        clickLeft = triggerClick.GetStateDown(LeftHand);
        clickRight = triggerClick.GetStateDown(RightHand);


        if (clickLeft && clickRight)
        {
            clickBoth = true;
        }
        else
        {
            clickBoth = false;
        }
    }

}
