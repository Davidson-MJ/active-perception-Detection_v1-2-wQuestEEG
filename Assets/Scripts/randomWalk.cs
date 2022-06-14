using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class randomWalk : MonoBehaviour
{
    /// <summary>
    ///  This script is attached to the Target Game Object, controls RW behaviour.
    ///  Note that the parameters are passed between this script, walkingGuide and paradigm.
    /// </summary>

    


    // phase.[fields] controls flow for target appearance.
    // phase is accessible in other scripts. 
    public enum phase
    {
        idle,               // invisible, stationary
        visibleIdle,        // visible, stationary
        start,              // visible (pause)
        still,              // visible (pause)
        walking,            // visible, in motion, collects target position and sets new.
        stop                // invisible.
    };
    // begin in idle phase.
    public static phase walk = phase.idle;
   
    // random walk control
    Vector3 previousLocation, stepTowards, stepDirection;
    float stepDuration, stepDistance, t;

    // random walk parameters // assigned in stimulusParameters
    public Vector3 upperBoundaries, lowerBoundaries, origin;
    public Vector2 stepDurationRange, stepDistanceRange;

    // first step parameters.
    float stillDuration = .2f; // 200 ms before beginning RW at trial onset.
    float stillT0;


    walkParameters motionParams;
    runExperiment runExperiment;

    void Start()
    {
      
        GetComponent<MeshRenderer>().enabled = true;

        motionParams = GameObject.Find("scriptHolder").GetComponent<walkParameters>();
        runExperiment = GameObject.Find("scriptHolder").GetComponent<runExperiment>();


    }

    void Update()
    {

      
        // note that walk = phase.start also set in paradigm.cs

        // start   step0 parms     // ////////////////////
        if (walk == phase.start)
        {
            previousLocation = transform.localPosition; // local position relative to parent position
            stepTowards = transform.localPosition;
            stepDuration = Random.Range(stepDurationRange.x, stepDurationRange.y);

            GetComponent<MeshRenderer>().enabled = true;
            //targetCentre.GetComponent<MeshRenderer>().enabled = true;

            stillT0 = Time.time;
            t = stepDuration; // this is t0

            walk = phase.still;
        }
        // pause for stillDuration.
        if (walk == phase.still && Time.time - stillT0 > stillDuration)
        {
            walk = phase.walking;
        }
        
        
        // walking    // ////////////////////
        // The Meat and Veg performed here.

        if (walk == phase.walking)
        {
            t += Time.deltaTime;

            if (t > stepDuration)
            {
                previousLocation = transform.localPosition;
                upperBoundaries = motionParams.upperBoundaries;
                lowerBoundaries = motionParams.lowerBoundaries;

                // if target has traversed outside the boundaries of our random walk.
                if (previousLocation.x > upperBoundaries.x || previousLocation.x < lowerBoundaries.x ||
                previousLocation.y > upperBoundaries.y || previousLocation.y < lowerBoundaries.y ||
                previousLocation.z > upperBoundaries.z || previousLocation.z < lowerBoundaries.z)
                {
                    // post-bump target location = origin + (directional unit sphere * radius)
                    //// origin is the goal (i.e., back to centre)
                    //// (directional unit sphere * radius) is which way, and how far: you don't want the target to "snap" back to the centre
                    // directional unit sphere = normalised position difference between 'previous location' and origin
                    // radius = how far the endpoint [post-bump target] is away from centre, so, the current distance minus the stepsize

                    Vector3 positionDifference = previousLocation - origin;
                    float radius = positionDifference.magnitude - Random.Range(stepDistanceRange.x, stepDistanceRange.y);

                    stepTowards = origin + (positionDifference.normalized * radius);
                }
                else // otherwise keep walking. Set goal position for this step.
                {
                    //stepDirection = Random.onUnitSphere; //  for 3D RW (includes depth).
                    //Vector3 stepDirection = new Vector3(Random.Range(-0.01f,.01f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)); // for removing depth (interferes with gait).

                    //removing RW for debugging.
                    //Vector3 stepDirection = new Vector3(Random.Range(-0.01f, .01f), Random.Range(-.5f, .5f), Random.Range(-.5f, .5f)); // for removing depth (interferes with gait).


                    // these params for target Cylinder:
                    Vector3 stepDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-.001f, .001f), Random.Range(-1f, 1f)); // for removing depth (interferes with gait).

                    stepDistance = Random.Range(stepDistanceRange.x, stepDistanceRange.y);
                    stepTowards = previousLocation + (stepDirection * stepDistance);
                }

                stepDuration = Random.Range(stepDurationRange.x, stepDurationRange.y);
                t = 0f;
            }

            // Lerp creates 3d point interpolated between previous location and stepTowards loc.
            // Interpolates between the points a and b by the interpolant t. The parameter t is clamped to the range [0, 1].
            //Vector3 updatePosition = Vector3.Lerp(previousLocation, stepTowards, t / stepDuration);

            // new: do not update if the targets are being presented (otherwise smeared results).
            if (runExperiment.pauseRW == 0)
            {
                Vector3 updatePosition = Vector3.Lerp(previousLocation, stepTowards, t / stepDuration);

                // new. ensure cylinder isn't moving in screen plane:
                updatePosition.y = previousLocation.y; // Note that Y (because of rotation), is zplane relevant to observer
                transform.localPosition = updatePosition;
            }

        }

        // end when phase.stop
        if (walk == phase.stop)
        {
            //GetComponent<MeshRenderer>().enabled = false;
            //targetCentre.GetComponent<MeshRenderer>().enabled = false;
            walk = phase.idle;
        }
    }
}
