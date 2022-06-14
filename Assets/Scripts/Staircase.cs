using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Staircase : MonoBehaviour
{
    /// <summary>
    ///  Controls the contrast value of a target, based on previous trial history.
    ///  actual colour changes are performed in the coroutine of paradigm.cs;
  
    /// </summary>
    // working with binary (yes/no) detecton responses, so can simplify:

    [Header("Difficulty setting:")]
    public string curUpdate;
    public float PercentDetect; 
    public int  prevResp, callCount, nCorrReverse, nErrReverse, corrCount, errCount, numCorrect, numError, reverseCount;
    //public bool ascending, initialAscending, updateStepSize = false; // initially going up/down?

    private float targetTestContrast;
   
    private Color newTargColor;

    public float probeContrast, targetContrast, stepSize;
   
    // colors [ contrast is updated within staircase]
    public Color preTrialColor; // green, to show ready/idle state
    public Color probeColor; // grey
    public Color targetColor; // white, decreasing in contrast to match probe over staircase.
    public float targetAlpha;
   
    
    private void Start()
    {
        // get nStaircasetrials and other 


        //float stepSize = .01f; // needs to be pilotted
        //int nCorrReverse = 2; // adjust the contrast values if 2 correct in a row (increase difficulty).
        //int nErrReverse = 1;  // adjust the contrast values if 1 error (decrease difficulty)
        stepSize = .02f;
        callCount = 0;
        reverseCount = 0;
        corrCount = 0; errCount = 0;
        numCorrect = 0;
        numError = 0;
       
        curUpdate = null;
        nCorrReverse = 3; // 3 down 1 up approximates 80% cor (1/2)^(1/3)
        //nCorrReverse = 2; // 2 down 1 up approximates 74% cor (1/2)^(1/3)
        nErrReverse = 1;
        PercentDetect = 0f;
        targetAlpha = 1f; // .75f
        // set colours
         preTrialColor= new Color(0f, 0.5f, 0f, targetAlpha); //drk green
         probeColor = new Color(0.4f, 0.4f, 0.4f, targetAlpha); // dark grey
        targetColor= new Color(.7f, .7f, .7f, targetAlpha); // light grey (start easy, become difficult).


        probeContrast = probeColor[1];
        targetContrast = targetColor[1];




    }


    public void UpdateStaircase(int responseAcc, float prvTargContrast, string trialType)
    {

        callCount++;
        // work through options:
        // correct detects first (staircase isn't updated after correct rejections).
        if (reverseCount == 7 && numError <8) // reduce size once
        {
            print("reducing step size");
            stepSize = stepSize / 2;
            reverseCount = 0;

        }
        if (responseAcc == 1)
        {
            corrCount++;
            numCorrect++;

            if (corrCount >= nCorrReverse)
            {
                // if response was correct n times in a row, decrease contrast
                corrCount = 0; // reset
                curUpdate = "Increasing difficulty.";
                targetTestContrast = prvTargContrast - stepSize;


                if (targetTestContrast < .409f)
                {
                    targetTestContrast = 0.41f;
                }
                if (targetTestContrast < probeColor[0])
                {
                    targetTestContrast= probeColor[0]; //avoid overshooting.
                }
                
                    
            } else
            {
                // maintain difficulty.
               
                curUpdate = "corr (no change)";
                targetTestContrast = prvTargContrast;

            }
        }
            

        // incorrect:
        if (responseAcc == 0)
        {

            errCount++; // running total, resets on reverse
            numError++; // grand total

            if (errCount >= nErrReverse)
            {
                // if response was incorrect nErrReverse times in a row, increase contrast, decrease difficulty.
                errCount = 0; //reset counter
                
                reverseCount++;
                curUpdate = "Decreasing difficulty"; //by increasing contrast.
                targetTestContrast = prvTargContrast + stepSize;
            } else
            {
                // do something. since nErrReverse is 1, 
                
                targetTestContrast = prvTargContrast;
                curUpdate = "errNochange";
            }
        }


        PercentDetect = ((float)numCorrect / (float)callCount)*100; // total correct detect divide amount of targs presented.

        newTargColor = new Color(targetTestContrast, targetTestContrast, targetTestContrast, targetAlpha); // grey scale.
        prevResp = responseAcc;

        print(curUpdate);
        print("Targ Contrast: " + targetTestContrast);
        // update public target color:
        // display public params:
        targetColor = newTargColor;
        targetContrast = targetTestContrast;
        probeContrast = probeColor[1];
    }
}
