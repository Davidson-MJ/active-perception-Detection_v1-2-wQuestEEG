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
    public float reachHeight;
    public float walkingPathDistance;
    public float walkingSpeed;
    public float walkDuration;
    public float guideDistance;
    public float reachBelowPcnt;


    // Hide the following in the inspector, as we don't want tweaking.
    //[HideInInspector]
    public float rampDistance;
    [HideInInspector]
    public float rampDuration;

    [HideInInspector]
    public Vector2 stepDurationRange;
    [HideInInspector]
    public Vector2 stepDistanceRange;



    // dimensionality
    [Header("Target Location and Boundaries")]
    public Vector3 planeOrigin;
    public Vector3 cubeOrigin;
    public Vector3 planeDimensions;
    public Vector3 cubeDimensions;
    public Vector3 upperBoundaries;
    public Vector3 lowerBoundaries;

    [HideInInspector]
    public Vector3 passiveTaskOrigin;
    [HideInInspector]
    public float passiveTaskDistance;

    GameObject motionPath;

    // set all variables at Awake, to set variables at initialization.
    // That way, these fields will be available in other scripts Start () calls
    void Start()
    {
        walkDuration = 9f;// 
        walkingPathDistance = 9.5f;//  Determines end point. 

        //walkDuration = 7f;
        //walkingPathDistance = 7.5f;//  Determines end point. 

        //approx steps is dist / 0.5
        reachBelowPcnt = 0.85f;
        rampDistance = 0f;// 0.7f; // used in walkingGuide, added to total path distance above.
        rampDuration = 1f; // used in walkingGuide
        guideDistance = 0.5f; // this is an offset, used to place the WG in front of the HMD, on calibration
        // dimensionality

        planeOrigin = new Vector3(0, 0, 0);
        cubeOrigin = new Vector3(0, 0, 0);
        planeDimensions = new Vector3(0.22f, 1f, .1f);
        //cubeDimensions = new Vector3(.25f, .25f, .25f); // sets boundaries for the RW

        cubeDimensions = new Vector3(2f, 2f, 2f); // sets boundaries for the RW (now scaled for small screen).

        //RW params set in randomWalk.cs
        stepDurationRange = new Vector2(0.2f, 0.4f); // update the direction of targer in this interval.
        // for sphere shader:
        //stepDistanceRange = new Vector2(0.03f, 0.045f); // set with David 2020-02-13 (RTKeys)

        // for cylinder:
        stepDistanceRange = new Vector2(0.7f, 0.8f); // not xy, but a range for the distance RW will step. now much smoother.

        motionPath = GameObject.Find("motionPath");

    }
    
    // METHODS:
    public void updateReachHeight()
    {
        Vector3 currentPos = motionPath.transform.localPosition;
        //note that reachHeight is updated in runExperiment, based on hmd position.
        Vector3 updatePosition = new Vector3(currentPos.x, reachHeight, currentPos.z);
        //update motionPath height.

        motionPath.transform.localPosition = updatePosition;


    }
}


