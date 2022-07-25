using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class walkParameters : MonoBehaviour
{

    // params for random walk and path motion. Are set to public for easy tweaking in inspector mode.
    // To hide in the inspector (but be serialized), use 
    // [HideInInspector]


    // random walk params
    [Header("Walking/Tracking Parameters")]
    //public float trialDuration;
    public float ScreenHeight;
    public float walkingPathDistance;
    public float walkingSpeed;
    public float walkDuration;
    public float guideDistance;
    public float reachBelowPcnt;


    // Hide the following in the inspector, as we don't want accidental tweaking.
    [HideInInspector]
    public float rampDistance, rampDuration;
    
    [HideInInspector]
    public Vector2 stepDurationRange, stepDistanceRange;
   
    [HideInInspector]
    public Vector3 planeOrigin, cubeOrigin, planeDimensions, cubeDimensions, upperBoundaries, lowerBoundaries, passiveTaskOrigin;

    GameObject motionPath;
    GameObject hmd;
    // set all variables at Awake, to set variables at initialization.
    // That way, these fields will be available in other scripts Start () calls
    void Start()
    {
        ScreenHeight = 1.7f;
        walkDuration = 9f;// 
        walkingPathDistance = 9.5f;//  Determines end point. 

        //walkDuration = 7f;
        //walkingPathDistance = 7.5f;//  Determines end point. 

        //approx steps is dist / 0.5
        reachBelowPcnt = 0.90f;
        rampDistance = 0f;// 0.7f; // used in walkingGuide, added to total path distance above.
        rampDuration = 1f; // used in walkingGuide
        guideDistance = 0.5f; // this is an offset, used to place the WG in front of the HMD, on calibration
        // dimensionality

        planeOrigin = new Vector3(0, 0, 0);
        cubeOrigin = new Vector3(0, 0, 0);
        planeDimensions = new Vector3(0.22f, 1f, .1f);
        //cubeDimensions = new Vector3(.25f, .25f, .25f); // sets boundaries for the RW

        cubeDimensions = new Vector3(2f, 2f, 2f); // sets boundaries for the RW (now scaled for small screen).

        //RW params set in BrownianMotion.cs
        stepDurationRange = new Vector2(0.2f, 0.4f); // update the direction of targer in this interval.
        // for sphere shader:
        //stepDistanceRange = new Vector2(0.03f, 0.045f); // set with David 2020-02-13 (RTKeys)

        // for cylinder:
        stepDistanceRange = new Vector2(0.7f, 0.8f); // not xy, but a range for the distance RW will step. now much smoother.

        motionPath = GameObject.Find("motionPath");
        hmd = GameObject.Find("VRCamera");
        
    }

    // METHODS:
    public void updateScreenHeight()
    {

        Vector3 headPosition = hmd.transform.position;
        ScreenHeight = hmd.transform.position.y * reachBelowPcnt;
         
        Vector3 currentPos = motionPath.transform.localPosition;
        //note that reachHeight is updated in runExperiment, based on hmd position.
        Vector3 updatePosition = new Vector3(currentPos.x, ScreenHeight, currentPos.z);
        //update motionPath height.

        motionPath.transform.localPosition = updatePosition;

        // alternatively:
        //objHoverscreen.transform.position = new Vector3(objHoverscreen.transform.position.x, objHMD.transform.position.y, objHoverscreen.transform.position.z);

    }
}


