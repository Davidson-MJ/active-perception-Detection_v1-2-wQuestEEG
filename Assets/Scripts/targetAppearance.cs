using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class targetAppearance : MonoBehaviour
{
    /// <summary>
    /// Handles the co-routine to precisely time changes to target appearance during walk trajectory.
    /// 
    /// Main method called from runExperiment.
   
    
    public bool processNoResponse;
    private float waitTime;
    private float subtr;
    private float[] targRange;

    runExperiment runExperiment;
    Renderer rend;
    trialParameters trialParams;
    //Staircase ppantStaircase;
    walkParameters motionParams;
    // for recording EEG triggers
    SerialController SerialController;
    private Color targColor;
    private void Start()
    {
        runExperiment = GameObject.Find("scriptHolder").GetComponent<runExperiment>();
        trialParams = GameObject.Find("scriptHolder").GetComponent<trialParameters>();
        //ppantStaircase = GameObject.Find("scriptHolder").GetComponent<Staircase>();
        motionParams = GameObject.Find("scriptHolder").GetComponent<walkParameters>();

        SerialController = GameObject.Find("scriptHolder").GetComponent<SerialController>();

        rend = GetComponent<Renderer>(); // change colour of shade, not texture (separate sphere).
        processNoResponse = false;
        targColor = rend.material.color;
    }

   public void startSequence()
    {
        targRange = new float[2];
        targRange[0] = trialParams.preTrialsec + trialParams.responseWindow; // minimum targ onset time.

        // note that trial duration changes with walk speed.
        float trialDur = motionParams.walkDuration;
        targRange[1] = trialDur - trialParams.responseWindow - 0.3f; // max onset time, w/ extra buffer for late targets to be detected.

        StartCoroutine("trialProgress");
    }

    /// <summary>
    /// Coroutine controlling target appearance with precise timing.
    /// </summary>
    /// <returns></returns>

    // the following coroutine controls the timing of stimulus changes.
    IEnumerator trialProgress()
    {
        while (runExperiment.trialinProgress) // this creates a never-ending loop for the co-routine.
        {
            // trial progress:
            /// The timing of trial elements is determined on the fly.
            /// Boundaries set in trialParameters.
            
            // set an original orientation for time between targets.
            // reverting back to this orientation removes large jitter in the attached particle ring.
            int origRot = Random.value < .5 ? -45 : +45;
            transform.localEulerAngles = new Vector3(0.0f, origRot, 0.0f);

            // predetermine target onset times:
          
            float[] preTargISI = new float[(int)trialParams.trialD.trialType]; // 
            float[] gapsare = new float[(int)trialParams.trialD.trialType]; // used to calc preTargISI below
            float jitter = Random.Range(0.01f, 0.02f);
            // pseudo randomly space targets, with minimum ITI of responseWindow



            // note that targRange is the time within walk, excluding an onset/offset buffer.

            // shift the intertrial ISI on random trials:
            subtr = Random.Range(-.5f, .5f);
            // note that we have walkParameters.walkDuration total sec to play with. Make sure the subtr jitter, doesn't interfere
            
            if (trialParams.trialD.trialType == 8)
            {
                gapsare[0] = 7.7f - subtr; // 
                gapsare[1] = 6.6f - subtr;
                gapsare[2] = 5.5f - subtr;
                gapsare[3] = 4.4f - subtr;
                gapsare[4] = 3.3f - subtr;
                gapsare[5] = 2.2f - subtr;
                gapsare[6] = 1.1f - subtr;
                gapsare[7] = 0f;
            }
            else if (trialParams.trialD.trialType == 7)
            {
                gapsare[0] = 8f - subtr; // 
                gapsare[1] = 6.9f - subtr;
                gapsare[2] = 5.8f - subtr;
                gapsare[3] = 4.7f - subtr;
                gapsare[4] = 3.6f - subtr;
                gapsare[5] = 2.5f;
                gapsare[6] = 0f;
            }
            else if (trialParams.trialD.trialType == 6)
            {
                gapsare[0] = 5.75f - subtr; // 
                gapsare[1] = 4.75f - subtr;
                gapsare[2] = 3.75f - subtr;
                gapsare[3] = 2.75f - subtr;
                gapsare[4] = 1.75f - subtr;
                gapsare[5] = 0f;

            }
            else if (trialParams.trialD.trialType == 5)
            {
                gapsare[0] = 4.25f - subtr;
                gapsare[1] = 3.25f - subtr;
                gapsare[2] = 2.25f - subtr;
                gapsare[3] = 1.25f - subtr;
                gapsare[4] = 0f;
            }
            else if (trialParams.trialD.trialType == 4)
            {
                gapsare[0] = 3.75f - subtr;
                gapsare[1] = 2.5f - subtr;
                gapsare[2] = 1.5f - subtr;
                gapsare[3] = 0.5f - subtr;
            }
            else if (trialParams.trialD.trialType == 0) // no targets
            {
                trialParams.trialD.targOnsetTime = -1;
                trialParams.trialD.targResponse = -1;
                trialParams.trialD.targResponseTime = -1;
                
                //targCorrList appended after (see Update()), based on whether clicks recorded (FAthistrial)
            }

            // when targets should be presented, pre calculate ISI based on gaps defined above:
            if (trialParams.trialD.trialType > 0)
            {

                // now prefill the preTargISI
                for (int itargindx = 0; itargindx < gapsare.Length; itargindx++)
                {
                    if (itargindx == 0) // start at trial beginning. targRange[0]
                    {
                        preTargISI[itargindx] = Random.Range(targRange[0], targRange[1] - gapsare[itargindx] * (trialParams.minITI + jitter));

                    }
                    else // use prev targ presentation as earliest point:
                    {
                        preTargISI[itargindx] = Random.Range(preTargISI[itargindx - 1], targRange[1] - gapsare[itargindx] * (trialParams.minITI + jitter));

                    }

                }
            }

            // begin target presentation:
            runExperiment.detectIndex = 0; // listener, to assign correct responses per target [0 = FA, 1 = targ1, 2 = targ 2]

            

            // change target colour to indicate trial prep ("Get Ready!")
            setColour(trialParams.preTrialColor);

            //now change colour and wait before target Onset.
            yield return new WaitForSecondsRealtime(trialParams.preTrialsec);
            setColour(trialParams.probeColor);


            // show target [use duration or colour based on staircase method].
            // show target on present trials.
            if (trialParams.trialTypeList[runExperiment.TrialCount] == "present") // all targ types (1 incl).
            {
                //// however many targets we have to present this trial, cycle through and present

                for (int itargindx = 0; itargindx < trialParams.trialD.trialType; itargindx++)
                {
                    // first target has no ISI adjustment
                    if (itargindx == 0)
                    {
                        waitTime = preTargISI[0];
                    }
                    else
                    {// adjust for time elapsed.
                        waitTime = preTargISI[itargindx] - runExperiment.trialTime;
                    }

                    // wait before presenting target:
                    yield return new WaitForSecondsRealtime(waitTime);



                    // to increase difficulty, and remove expectancy, only show on the % of trials.
                    if (Random.value <= .9f) // proportion to show targets (now have jitter also).
                    {

                        
                        //

                        // change colour - detect window begins. 
                        runExperiment.pauseRW = 1; // pause RW of target (so  flashes are in same location).
                        // randomize orientation between two options +-45
                        //int rottmp = Random.value < .5 ? -45 : +45;

                        //Vector3 newrot = new Vector3(0, rottmp, 0);
                        //transform.localEulerAngles = new Vector3(0.0f, rottmp, 0.0f);

                        setColour(trialParams.targetColor);
                        runExperiment.targState = 1; // target is shown
                        runExperiment.detectIndex = itargindx + 1; //  click responses collected in this response window will be 'correct'
                        runExperiment.hasResponded = false;  //switched if targ detected.
                        trialParams.trialD.targOnsetTime = runExperiment.trialTime;
                        // also send trigger to eeg
                        if (runExperiment.recordEEG)
                        {
                           
                                SerialController.SendSerialMessage("T"); // target presented
                           
                        }

                        //how long to show target for?
                        yield return new WaitForSecondsRealtime(trialParams.targDurationsec);
                        //remove target, wait until response window has passed.
                        setColour(trialParams.probeColor);
                        runExperiment.targState = 0; // target has been removed
                        // revert to original orientation
                        
                        transform.localEulerAngles = new Vector3(0.0f, origRot, 0.0f);
                        runExperiment.pauseRW = 0; // restart RW
                        

                        yield return new WaitForSecondsRealtime(trialParams.responseWindow);

                        // if no click in time, count as a miss.
                        if (!runExperiment.hasResponded) // no response 
                        {
                            processNoResponse = true;
                        }
                        runExperiment.detectIndex = 0; //clicks from now  counted as incorrect (too slow).
                        //runExperiment.targCount++;
                    } else // continue without showing a target (keep timings the same.
                    {
                        print("Hiding target");
                        // no colour change, no change to targ state, detectindex=0,
                        //how long to show target for?
                        yield return new WaitForSecondsRealtime(trialParams.targDurationsec);

                        yield return new WaitForSecondsRealtime(trialParams.responseWindow);
                        //trialParams.trialD.targOnsetTime = 0;
                        processNoResponse = false; // don't count as a miss (since no targets).
                    }
                }

               
            }
            // after for loop, wait for trial end:
            while (runExperiment.trialTime < motionParams.walkDuration)
            {
                yield return null;  // wait until next frame. 
            }

        }

    }

    // color change method.
    public void setColour(Color newCol)
    {
        // because we are changing the sphere shaders colour, keep the alpha.
        //print("New Color: " + newCol);
        rend.material.SetColor("_Color", newCol);


    }
}
