using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
///  Single target detection, quest staircase, jittered onset and contrast.
///  
///  v1_2                UPDATED for EEG
/// 
/// </summary>
public class trialParameters : MonoBehaviour
{
    //Update all on Start();

    [HideInInspector]
    public float preTrialsec, responseWindow, targDurationsec, minITI, trialDur, nTrials, targetAlpha;

    [Header("Experiment Protocol:")]
    public int nBlocks;
    public int ntrialsperBlock;
    public int nStaircaseBlocks;
    public int nRegularBlocks; 


    private float nUniqueConditions;
    private int trialsperCondition; // how many times can we repeat a target type (nTargs presented)

    //[Hide diff types of arrays from inspector]
    public int[] trialTypeArray; // namount targs presented each walk
    [HideInInspector]
    public int[,] blockTypeArray; //nTrials x 3 (block, trialID, type)

    [HideInInspector]
    public float[] targRange;

    [HideInInspector]
    public int[] blockTypelist;
    private int[] targsPresented; 

    
   walkParameters walkParameters;
   runExperiment runExperiment;


    [System.Serializable]
    public struct trialData
    {
        public float trialNumber, blockID, trialID, trialType, targContrast, targContrastPosIdx, targOnsetTime,
            clickOnsetTime, targResponse, targResponseTime, targCorrect, stairCase, degExperimentE, degPracticalE, intPracticalE;
        public bool isStationary;


    }

    [HideInInspector]
    public trialData trialD;

    // create public lists of for updating in runExperiment, read by RecordData 
    [HideInInspector]
    public List<string> trialTypeList = new List<string>(); // populated below.
    [HideInInspector]
    public List<int> trialsper = new List<int>(); // presented per walk.

    // colors [ contrast is updated within staircase]
    [HideInInspector]
    public Color preTrialColor, probeColor, targetColor;
    
    void Start()
    {

        walkParameters = GameObject.Find("scriptHolder").GetComponent<walkParameters>();
        runExperiment = GameObject.Find("scriptHolder").GetComponent<runExperiment>();

        // gather presets
        preTrialsec = 0.5f; // buffer for no targ onset. (1 step)
        trialDur = walkParameters.walkDuration; // in second, determines how many targets we can fit in.
        responseWindow = 0.8f;
        targDurationsec = .02f;
        minITI = responseWindow + targDurationsec;
        targetAlpha = .75f; // .75f // 
        // set colours
        preTrialColor = new Color(0f, 1f, 0f, 1); //drk green
        probeColor = new Color(0.4f, 0.4f, 0.4f, targetAlpha); // dark grey
        targetColor = new Color(.55f, .55f, .55f, targetAlpha); // light grey (start easy, become difficult).

        //
        ntrialsperBlock = 20; // to do
        //
        nStaircaseBlocks = 1; // to do
       

        nRegularBlocks = 9;

        //float ratioSt2wlk = 1 / 4; // how many stand vs walking blocks?

        nBlocks = nStaircaseBlocks + nRegularBlocks;
        nTrials = (nBlocks) * ntrialsperBlock;
        blockTypelist = new int[(int)nBlocks]; // populated below.

       

        // calculate target presentations, and block order for this experiment:
        float availTime = trialDur - (preTrialsec + responseWindow); // when can targets appear in walk?
        float nTargPres = Mathf.Floor(availTime / minITI); // how many targs in this window

        nUniqueConditions = nTargPres - 1;// 
      
        // % split. for the n conditions.
        trialsperCondition = (int)Mathf.Floor(nTrials / 10); // 10% of trials as catch, 
        print("creating trial allocation for max " + (nUniqueConditions - 1) + " targets");

        // next, we will determine how many targets to present in our given walk duration (max 3 for home testing).
        // prefill the trialTypeArrayy as we go:

        // prefill target information (present or absent trial types).
        trialTypeArray = new int[(int)nTrials];
        
       
        // what % of trials per condition?:
        trialsper.Add(trialsperCondition * 2); // n X 10% of trials
        trialsper.Add(trialsperCondition * 4); //
        trialsper.Add(trialsperCondition * 4); //max target case

        // prefill array for conditions ids:
        targsPresented = new int[3];
        targsPresented[0] = (int)walkParameters.walkDuration-2;
        targsPresented[1] = (int)walkParameters.walkDuration-1;
        targsPresented[2] = (int)walkParameters.walkDuration-1;
        
        // prefill trial type array (nTargs on a given trial)
        int icounter = 0;

        for (int icond= 0; icond < 3; icond++)
        {
            for (int itrial = 0; itrial < trialsper[icond]; itrial++)
            {
                trialTypeArray[icounter] = targsPresented[icond];
                icounter++;
            }

        }
        
        // now shuffle this array:
        shuffleArray(trialTypeArray);

        ////// Now populate our lists.
        for (int itrial = 0; itrial < nTrials; itrial++) // for every walk trajectory.
        {
            // how many targs this walk?
            int thisN = trialTypeArray[itrial];
            if (thisN == 0)
            {
                trialTypeList.Add("absent");
            }else
            {
                trialTypeList.Add("present");
            }
         

        }

        // also create wrapper to determine which blocks can be stationary or walking.
        // first staircase blocks are always stationary.
        // hardcoded for current trial numbers.
        // TODO: flexibly update for nTrials required.
        
        icounter = 0;
        blockTypelist = new int[nRegularBlocks]; // omit first 2 (calib) blocks.

        // pseudo randomly allocate remaining (we want less stationary blocks).
        //float nStand = Mathf.Ceil(nRegularBlocks / ratioSt2wlk);
        //float nWalk = nRegularBlocks - nStand;

        //// 

        //for (float iblock = 0; iblock < nStand; iblock++)
        //{
        //    blockTypelist[icounter] = 0;
        //}
        //for (float iblock = 0; iblock < nWalk; iblock++)
        //{
        //    blockTypelist[icounter] = 1;
        //}

        //hard coded for 8, but code above can prefill if uncommented.
        blockTypelist[0] = 0;
        blockTypelist[1] = 0; 
        blockTypelist[2] = 1;
        blockTypelist[3] = 1;
        blockTypelist[4] = 1;
        blockTypelist[5] = 1;
        blockTypelist[6] = 1;
        blockTypelist[7] = 1;
        


        // shuffle the order of stationary (0s) and walking (1s) blocks
        shuffleArray(blockTypelist); 

        blockTypeArray = new int[(int)nTrials, 3]; // 3 columns.
                                                   // ensure first staircased trials are stationary.

        // force two first blocks to be type 0, 1, (stationary, walking).
        for (int iblock = 0; iblock < 2; iblock++)
        {
            for (int itrial = 0; itrial < ntrialsperBlock; itrial++)
            {
                blockTypeArray[icounter, 0] = iblock;
                blockTypeArray[icounter, 1] = itrial; // trial within block
                blockTypeArray[icounter, 2] = iblock; // [0 or 1]

                icounter++;
            }

        }

        //now fill remaining blocks 
        //
        for (int iblock = 2; iblock < nBlocks; iblock++)
        {
            for (int itrial = 0; itrial < ntrialsperBlock; itrial++)
            {
                blockTypeArray[icounter,0] = iblock;
                blockTypeArray[icounter, 1] = itrial;
                blockTypeArray[icounter, 2] = blockTypelist[iblock-2]; //.

                icounter++;
            }

        }

    }

    /// 
    /// 
    /// METHODS:
    /// 
    /// 
    /// 
    // shuffle array once populated.
    void shuffleArray(int[] a)
    {
        int n = a.Length;


        for (int id = 0; id < n; id++)
        {
            swap(a, id, id + Random.Range(0, n - id));
        }
    }
    void swap(int[] inputArray, int a, int b)
    {
        int temp = inputArray[a];
        inputArray[a] = inputArray[b];
        inputArray[b] = temp;

    }
}



