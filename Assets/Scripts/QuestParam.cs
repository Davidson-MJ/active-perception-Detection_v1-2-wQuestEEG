using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class QuestParam : MonoBehaviour
{

    
    //[HideInInspector]
    public float tGuess; // this is the initial threshold estimate.


    //[System.Serializable]
    public struct Parameters
    {
        public float tGuess, tGuessSd, pThreshold, beta, delta, gamma, grain, range;
    }
    
    
    // define the initial quest parameters in a public structure type Parameters
    public Parameters QuestParameters; // call struct (defined below).

    //method to set params:
    public QuestParam()
    {
        // tGuess is the initial value for quest procedure
        // this will be randomly chosen between below vs. above threhold values
        // defined in Initialvalues through runExperiment.cs

        // parameters for Quest, can only be changed directly in the script 
        // before running an experiment
        // set the initial values:
        //tGuess is your prior threshold estimate.
        //tGuessSd is the sd you assign to that guess.Be generous
        //QuestParameters.tGuess = 20*Mathf.Log10(0.45f);
        QuestParameters.tGuess = 0.45f;
        QuestParameters.tGuessSd = 2f;
        QuestParameters.pThreshold = 0.75f;
        QuestParameters.beta = 3.5f; // steepness of psychometric fxn
        QuestParameters.delta = 0.01f; // lapse rate (fraction of trials ppant responses are random)
        QuestParameters.gamma = 0.5f; //is the fraction of trials that will generate response 1 when intensity == -in
        QuestParameters.grain = .002f;//// quantization(step size) of the interval table
        QuestParameters.range = 2f; //// in same scale. range/2:tGuess:range/2

        //QuestParameters.targetContrast = 0.45f;

       

        // params all increased by factor of 100, to avoid bugs in questcalcs.
    }

   
}

