using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RingJitter : MonoBehaviour
{
    // Start is called before the first frame update
    randomWalk randomWalk;
    runExperiment runExperiment;
    targetAppearance targetAppearance;
    GameObject targetCylinder;
    Vector3 newpos, oldpos, upperBounds, lowerBounds, origin, origRotation;
    public Vector2 stepDistanceRange, stepDurationRange;
    float newX;
    float newZ;
    float stepDuration, t, stepDistance;
    void Start()
    {
        randomWalk = GameObject.Find("TargetCylinder").GetComponent<randomWalk>();
        targetCylinder = GameObject.Find("TargetCylinder");
        runExperiment = GameObject.Find("scriptHolder").GetComponent<runExperiment>();
        targetAppearance = GameObject.Find("TargetCylinder").GetComponent<targetAppearance>();

        upperBounds = new Vector3(12,0,5f);
        lowerBounds = new Vector3(-12, 0, -5f);
        stepDistanceRange = new Vector2(.03f,.05f);
        stepDurationRange = new Vector2(0.3f, .6f);
        stepDuration = Random.Range(stepDistanceRange.x, stepDistanceRange.y);
        
    }

    // Update is called once per frame
    void Update()
    {
        // if walk RW is active, jitter the local position of the sphere around the target.

        if (!runExperiment.trialinProgress)
        {

            origin = new Vector3(0, 0, 0);// targetCylinder.transform.localPosition; // keep reference to TC.
            transform.localPosition = origin;
        }

        if (randomWalk.walk == randomWalk.phase.start)
        {
            t = 0;
            origRotation = transform.localEulerAngles;
            // realign with tC

            origin = new Vector3(0, 0, 0);// targetCylinder.transform.localPosition; // keep reference to TC.
            transform.localPosition = origin;
        }
        
        if (randomWalk.walk == randomWalk.phase.walking)
        {
            t += Time.deltaTime;

            // if an orientation flip is in action (for the targetCyl), we want to avoid large ring mvmnts.



            if (t>stepDuration)
            { 
               
                oldpos = transform.localPosition;
               
                    // if target has traversed outside the boundaries of our random walk.
                if (oldpos.x > upperBounds.x || oldpos.x < lowerBounds.x ||
                oldpos.y > upperBounds.y || oldpos.y < lowerBounds.y ||
                oldpos.z > upperBounds.z || oldpos.z < lowerBounds.z)
                {
                    // post-bump target location = origin + (directional unit sphere * radius)
                    //// origin is the goal (i.e., back to centre)
                    //// (directional unit sphere * radius) is which way, and how far: you don't want the target to "snap" back to the centre
                    // directional unit sphere = normalised position difference between 'previous location' and origin
                    // radius = how far the endpoint [post-bump target] is away from centre, so, the current distance minus the stepsize

                    Vector3 positionDifference = oldpos/8 - origin;

                    float radius = positionDifference.magnitude - Random.Range(stepDistanceRange.x, stepDistanceRange.y);

                    newpos = origin + (positionDifference.normalized * radius);
                }
                else // otherwise keep walking. Set goal position for this step.
                {
                    
                    // these params for target Cylinder:
                    Vector3 stepDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-.001f, .001f), Random.Range(-1f, 1f)); // for removing depth (interferes with gait).

                    // need to scale stepDirectionX for the cylinder:
                    stepDirection.x = stepDirection.x * 6;

                    stepDistance = Random.Range(stepDistanceRange.x, stepDistanceRange.y);
                    newpos = oldpos+ (stepDirection * stepDistance);
                }

                stepDuration = Random.Range(stepDurationRange.x, stepDurationRange.y);
               

                if (runExperiment.pauseRW == 0 ) // don't update until flip has finished (otherwise we get jumps).
                { 
                    Vector3 updatePosition = Vector3.Lerp(oldpos, newpos, t / stepDuration);

                    // new. ensure cylinder isn't moving in screen plane:
                    updatePosition.y = oldpos.y; // Note that Y (because of rotation), is zplane relevant to observer
                    transform.localPosition = updatePosition;

                    // FIX THE ROTATION, else ring flips with targetCylinder.
                    
                }
            }
   
        }

    }
}
