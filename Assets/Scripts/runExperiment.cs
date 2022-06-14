using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.InputSystem;
using LSL;

/// <summary>
///  Single target detection, quest staircase, jittered onset and contrast.
///  
///  v1_2 --------------- UPDATED for EEG and lab streaming layer
/// 
/// </summary>
public class runExperiment : MonoBehaviour
{
    /// <summary> // Now including a quest staircase, and jittered presentation after the calibration period.
    /// /// This script
    /// Imports parameters and handles the flow of the experiment.
    /// e.g. Trial progression, listeners etc.
    /// /// </summary>/// 

    // basic experiment structure/ parameters to toggle.
    public string participant;
    public int TrialCount; //n walk trajectories
    public int TrialType;  // n targs absent, n present
    public int targCount; // targs presented (acculative), used to track data.
    public bool isPractice = true; // determines walking guide motion (stationary during practice).
    public bool isStationary = true;
    int nStairs = 1; // number of staircases to use in quest procedure.
    bool disturbQuestContrast = false; // option to add some jitter to the quest estimates, during staircase.


    // flow managers
    public bool trialinProgress; // handles current state within experiment 
    private bool FAthistrial; // listen for FA in no targ trials, pass to update staircase/recording data.
    private bool SetUpSession; // for alignment of walking space.
    private int usematerial;  // change walk image (stop sign and arrows).
    private int useStair; // we have (up to)  3 staircases running to see if they converge.
    public bool updateText;
    private bool setXpos;
    bool questready; // boool to switch quest params on.
    bool expContrastset;

    // passed to other scripts (couroutine, record data etc).
    public bool collectTrialSummary; // passed to recordData.
    public float trialTime; // clock within trial time, for RT analysis.
    public int targState; // targ currently on screen, used to synchron recordings in recordData (frame by frame).
    public int detectIndex; // index to allocate response to correct target within walk.
    public int pauseRW; // used to pause the RW of a target while flash is being presented.
    public bool hasResponded; //listener for trigger responses after target onset < respone Window.

    //trial  
    public List<float> FA_withintrial = new List<float>(); // collect RT of FA within each trial (wipes every trial) passed to RecordData.

    // speak to.

    ViveInput viveInput;
    recordData recordData;
    randomWalk randomWalk;
    walkParameters walkParams;
    walkingGuide walkingGuide;
    trialParameters trialParams;
    showText showText;
    changeDirectionMaterial changeMat;
    targetAppearance targetAppearance;
    myMathsMethods myMathsMethods;


    // declare public GObjs.
    public GameObject hmd;
    public GameObject effector;
    public GameObject SphereShader;
    GameObject redX;

    //for quest
    public QuestParam questP;
    public QuestStaircase questStair1, questStair2, questStair3;
    // Result file
    // Current test value
    [SerializeField] [ReadOnly] private float tmpQ; // quest mean for each trial


    // prep an LSL stream:
    string StreamName = "LSL4Unity";
    string StreamType = "Markers";
    private StreamOutlet outlet;
    private string[] sample = { "" };

    void Start()
    {
        // dependencies
        
        targetAppearance = GameObject.Find("TargetCylinder").GetComponent<targetAppearance>();
        randomWalk = GameObject.Find("TargetCylinder").GetComponent<randomWalk>();
        walkingGuide = GameObject.Find("motionPath").GetComponent<walkingGuide>();
        
        viveInput = GetComponent<ViveInput>();
        recordData = GetComponent<recordData>();
        walkParams = GetComponent<walkParameters>();        
        trialParams =GetComponent<trialParameters>();
        questP = GetComponent<QuestParam>();
        questStair1 = GetComponent<QuestStaircase>();
        questStair2 = GetComponent<QuestStaircase>();
        myMathsMethods = GetComponent<myMathsMethods>();


        showText = GameObject.Find("Instructions (TMP)").GetComponent<showText>();
        changeMat = GameObject.Find("directionCanvas").GetComponent<changeDirectionMaterial>();
        redX = GameObject.Find("RedX");

        // params, storage
        // make sure nAllTrials is divisible by 10.
        
        tmpQ = 0.46f; // starting guess (for quest threshold).

        //flow managers
        TrialCount = 0;
        targCount = 0;
        trialinProgress = false;
        expContrastset = false;

        SetUpSession = true;
        collectTrialSummary = false; // send info after each target to be written to a csv file
        questready = false;
        updateText = true;
        usematerial = 0; // 0=show stop sign, later changed to arrows for walk guide.
        pauseRW = 0;
        setXpos = false;
        changeMat.update(0); // render stop sign
        showText.updateText(1); // pre  exp instructions

        // initialize LSL outlet"
        var hash = new Hash128();
        hash.Append(StreamName);
        hash.Append(StreamType);
        hash.Append(gameObject.GetInstanceID());
        // set up stream params (note this may need to change from string to float type.)
        StreamInfo streamInfo = new StreamInfo(StreamName, StreamType, 1, LSL.LSL.IRREGULAR_RATE, channel_format_t.cf_string, hash.ToString());
        outlet = new StreamOutlet(streamInfo);


        print("setting up ... Press <space>  or <click> to confirm origin location");


    }


    private void Update()
    {

        // set up origin.
        if (SetUpSession)
        {

            CalibrateStartPos(); // align motion origin to centre, startflag, or player (


            targetAppearance.setColour(trialParams.preTrialColor); // indicates ready for click to begin trial

            // set initial Quest params:
            // for all trials, update thresholdGuess- starting from above or below the threshold.
            //float tmpUpDown = questP.InitialValue[TrialCount] < 0 ?
            //        questP.ContrastInitialValues.BelowThreshold : questP.ContrastInitialValues.AboveThreshold;
            if (!questready)
            {
                InitQuestParams(tmpQ);
                // first guess for quest:
                questready = true;
            }


        }

        if (updateText) // don't access method every frame, just at block end.
        {
            determineText(); // method called to determine instructions shown.
            updateText = false; // 
        }
        
        if (!trialinProgress && setXpos)
        {
            // end of a block, so position new start point:

            // move Red X to the next place on screen:

            float mvmnt = trialParams.blockTypeArray[TrialCount + 1, 2];
            // query if stationary or not.
            isStationary = mvmnt == 0 ? true : false; // 1 and 2 (in mvmnt) corresponds to stationary or moving)
            CalibrateStartPos();
        }

        // check for startbuttons, but only if not in trial.
        if (!trialinProgress && !setXpos && viveInput.clickLeft && TrialCount < trialParams.nTrials)
        {
            startTrial(); // starts coroutine, changes listeners, presets staircase.            
        }


        // increment within trial time.
        if (trialinProgress)
        {
            trialTime += Time.deltaTime;
            //print("Trial Time: " + trialTime);
        }

        //// check for target detection.(indicated by  right trigger click).
        if (trialinProgress && viveInput.clickRight)
        {
            collectDetect(); // places RTs within an array. [ function will determine correct or no]

        }
        // If no response recorded by end of response window, update trial summary data accordingly:
        if (targetAppearance.processNoResponse) 
        {
            collectOmit();
            targetAppearance.processNoResponse = false;

        }


        // check for trial end.
        if (trialinProgress && (trialTime >= walkParams.walkDuration)) // use duration (rather than distance), to account for stationary trials.
        {

            trialPackdown();
           
            TrialCount++;
        }
           
        if (TrialCount == trialParams.nTrials)
        {
            print("Experiment Over");
            targetAppearance.setColour(new Color(1, 0, 0));
            showText.updateText(4); // post exp 

        }
    }
    
    
    
    /// 
    /// ////////////////////////////////////////
    /// RUN EXPERIMEN METHODS:
    /// //////////////////////////////////////////
    /// 
    
    void InitQuestParams(float tmpQ)
    {
        //questP.tGuess = 20 * Mathf.Log10(tmpQ);

        questP.tGuess = tmpQ;
        // create 3 quest staircases for the current session. see if they converge, take mean at end.

        questStair1 = new QuestStaircase(questP.tGuess,
            questP.QuestParameters.tGuessSd,
            questP.QuestParameters.pThreshold,
            questP.QuestParameters.beta,
            questP.QuestParameters.delta,
            questP.QuestParameters.gamma,
            questP.QuestParameters.grain,
            questP.QuestParameters.range);

        if (nStairs == 2) 
        {
            questStair2 = new QuestStaircase(questP.tGuess,
           questP.QuestParameters.tGuessSd,
           questP.QuestParameters.pThreshold,
           questP.QuestParameters.beta,
           questP.QuestParameters.delta,
           questP.QuestParameters.gamma,
           questP.QuestParameters.grain,
           questP.QuestParameters.range);
        }
        

        
        
    }

    void CalibrateStartPos()
    {
        // align to room centre:
        Vector3 startO = new Vector3(-1, 0, 0);
        Vector3 startX = new Vector3(0, 0, 0);
        GameObject motionOrigin = GameObject.Find("motionOrigin");
        
        //retain Y position of X (above ground to avoid clipping).
        startX.y = redX.transform.position.y;

        float mvmnt = trialParams.blockTypeArray[TrialCount, 2];
        // query if stationary or not.
        isStationary = mvmnt == 0 ? true : false; // 1 and 2 (in mvmnt) corresponds to stationary or moving)


        if (isStationary)
        {
            motionOrigin.transform.position = startO;
            startX.y = redX.transform.position.y;
            
        } else
        {

            //align to start Pos.
            GameObject startFlag = GameObject.Find("Start Flag pole");
            Vector3 flagpos = startFlag.transform.position;

            startO.x = flagpos.x - 1.5f;
            startO.z = flagpos.z- 0.5f;
            startX.x = flagpos.x- 1.5f;
            startX.z = flagpos.z - 0.5f;
            
            

        }

        motionOrigin.transform.position = startO;
        redX.transform.position = startX;
        setXpos = false;

        // also align reach height (Y) to participant head (in case of slipping).
        if (questready) // this just omits this calibration, on the first frame of the experiment.
        {
            Vector3 headPosition = hmd.transform.position;
            headPosition.y = myMathsMethods.Round(headPosition.y, 1);
            walkParams.reachHeight = hmd.transform.position.y * walkParams.reachBelowPcnt;
            walkParams.updateReachHeight(); // 
        }


        SetUpSession = false;
         walkingGuide.fillStartPos(); // update start pos in WG.
                 
    }


    private void startTrial()
    {
       

        // clear previous trial info, reset, and assign from preallocated variables:
        trialinProgress = true; // for coroutine (handled in targetAppearance.cs).        
        showText.updateText(0); // remove text
        //  // set redX to hidden:
        
        redX.SetActive(false);
        trialTime = 0;  // clock accurate reacton time from time start      
        targState = 0;
        FAthistrial = false; // boolean passed to Update() to confirm whether absent types were correct or not.
        FA_withintrial.Clear();  // temp file for any FAs:

        // Establish (this) trial parameters:
        TrialType = trialParams.trialTypeArray[TrialCount]; //
        float mvmnt = trialParams.blockTypeArray[TrialCount, 2];
        // query if stationary or not.
        isStationary = mvmnt == 0 ? true : false; // 1 and 2 (in mvmnt) corresponds to stationary or moving)

        // add to trialD for recordData.cs
        trialParams.trialD.trialNumber = TrialCount;
        trialParams.trialD.blockID = trialParams.blockTypeArray[TrialCount, 0];
        trialParams.trialD.trialID = trialParams.blockTypeArray[TrialCount, 1];
        trialParams.trialD.trialType = TrialType;

        //store bool as int
        float fStat = isStationary ? 1 : 0;
        trialParams.trialD.isStationary = fStat;

        // TO DO: toggle these so that walk height is fixed after trial packdown.
        Vector3 headPosition = hmd.transform.position;
        headPosition.y = myMathsMethods.Round(headPosition.y, 1);
        walkParams.reachHeight = hmd.transform.position.y * walkParams.reachBelowPcnt;
        walkParams.updateReachHeight(); // 


        randomWalk.transform.localPosition = walkParams.cubeOrigin;
        randomWalk.origin = walkParams.cubeOrigin;
        walkParams.lowerBoundaries = walkParams.cubeOrigin - walkParams.cubeDimensions;
        walkParams.upperBoundaries = walkParams.cubeOrigin + walkParams.cubeDimensions;

        randomWalk.lowerBoundaries = walkParams.lowerBoundaries;
        randomWalk.upperBoundaries = walkParams.upperBoundaries;
        randomWalk.stepDurationRange = walkParams.stepDurationRange;
        // can't use   = stepDistanceRange; as the string is rounded to 1f precision.
        // so access the dimensions directly:
        randomWalk.stepDistanceRange.x = walkParams.stepDistanceRange.x;
        randomWalk.stepDistanceRange.y = walkParams.stepDistanceRange.y;

        // set fields in randomWalk and recordData to begin exp:
        randomWalk.walk = randomWalk.phase.start;
        recordData.recordPhase = recordData.phase.collectResponse;
        walkingGuide.walkMotion = walkingGuide.motion.start;
        walkingGuide.returnRotation = walkingGuide.motion.idle;

        // mark trial start in LSL
        if (outlet != null)
        {
            sample[0] = "trialType: " + TrialType;
        }


        // define if walk/stationary based on trial ID: (i.e. if within practice blocks)
        if (trialParams.blockTypeArray[TrialCount,0] < trialParams.nStaircaseBlocks)
        //if (TrialCount <= (nStaircaseTrials - 1))
        {
            // set for outside(randomWalk) listeners. When practice, motion guide is stationary.
            isPractice = true;
            changeMat.update(usematerial); // Render green arrow or stop.
        } else
        {
            isPractice = false;
            
            changeMat.update(usematerial); //usematerial determined by text instructions (stop or go arrow).
        }


        ////// note that we want to restart the staircase after first block of practice trials, if there are more practice blocks.
        if (TrialCount == trialParams.ntrialsperBlock && trialParams.nStaircaseBlocks>1)
        {
            print("restarting staircase");
            tmpQ = 0.46f; // reinitialize starting guest
            InitQuestParams(tmpQ);  //
            // reset target colour
            // actually change target contrast (called in targetAppearance.cs);
            // set target to 'easy' once more.
            trialParams.targetColor = new Color(0.55f,0.55f, 0.55f, trialParams.targetAlpha);

        }



        //start coroutine to control target onset and target behaviour:
        print("Starting Trial " + (TrialCount+1) + " of " + trialParams.nTrials+ ", " + TrialType + " to detect");
        targetAppearance.startSequence(); // co routine in another script.

    }

    // method for collecting responses to target presentation, assigning correct or not.
    private void collectDetect()
    {
        //Record click - R click for target perceived present                       
        if (viveInput.clickRight)
        {

            // we have different critical windows, based on trial type. [0 ,1 ,2 targets];

            // first place the click into an array
            trialParams.trialD.clickOnsetTime = trialTime;
            //determine if this RT was within response window of targ.
            // we have a listener in the coroutine (detectIndex). this determines whether RT was appropriate.

            if (detectIndex != 0 && !hasResponded)  // first resp within allocated response window
            {
                //trialParams.targCorrectList.Add(1);
                //trialParams.targResponseList.Add( 1);
                //trialParams.targResponseTimeList.Add(trialTime); // passed to recordData.
                trialParams.trialD.targCorrect = 1;
                trialParams.trialD.targResponse = 1;
                trialParams.trialD.targResponseTime = trialTime;
                trialParams.trialD.targContrast = tmpQ;
                //targ onset times already appended, within coroutine.

                print("Hit!");

                hasResponded = true; // passed to coroutine, avoids processing omitted responses.
                                     // 
                                     // set up  contrast for the next target:

                // Uupdate contrast, after the first 3 trials of both practice blocks.
                if (isPractice && trialParams.blockTypeArray[TrialCount, 1] > 2)
                {
                    updateTargContrast(trialParams); // based on staircase.
                }
                else if (!isPractice)
                {

                    updateTargContrast(trialParams); // 
                }

                recordData.collectTrialSummary();// pass to Record Data (after every hit targ)
            }
            else
            {
                print("False alarm");
                FA_withintrial.Add(trialTime); // append to the list (cleared at every new walk trial).               
                FAthistrial = true;
            }
        }
    }

    private void collectOmit() // only relevant to response window following targs.
    {
                            
        if (trialParams.trialTypeList[TrialCount] == "present")
        {
            // Miss
            trialParams.trialD.targCorrect = 0;
            trialParams.trialD.targResponse = 0;
            trialParams.trialD.targResponseTime = 0;
            trialParams.trialD.targContrast = tmpQ;
             
            print("Miss!");
            // 
            // set up  contrast for the next target, if within staircase.
            // we want to run the staircase twice.
            // update targ contrast if within staircase block. and after first 3 trials.
            
            // Uupdate contrast, after the first 3 trials of both practice blocks.
            if (isPractice && trialParams.blockTypeArray[TrialCount, 1] >2)
            {
                updateTargContrast(trialParams); // based on staircase.
            } else if (!isPractice) {

                updateTargContrast(trialParams); // 
            }
            

            recordData.collectTrialSummary();// pass to Record Data (after every missed targ)

        }
           
    }



    private void updateTargContrast(trialParameters trialParams)
    {
        // if within staircase practice blocks:
        if (isPractice)
        {
            //Using Quest:
            // update quest
            // was last respnse correct or no?
            int tmpAcc = (int)trialParams.trialD.targCorrect;
            

            // feed prev linear/log(contrast) to quest:
            if (useStair == 1)
            {
                questStair1.UpdateQ(tmpQ, tmpAcc);

            }
            else if (useStair == 2)
            {
                questStair2.UpdateQ(tmpQ, tmpAcc);
            }


            // which staircase to use next?
            //randomize stair selection (can be unbalanced).
            //int randomIndex = 0;
            //int[] opts = new int[2] { 1, 2 };
            //randomIndex = Random.Range(0, 2);            
            //useStair = opts[randomIndex];


            //Now alternating between staircases (balanced)
            if (nStairs == 1)
            {
                // or fix one stair case:
                useStair = 1;
            }
            else
            {
                // modTrial count sets staircase selected
                useStair = TrialCount % 2 == 0 ? 1 : 2;

            }



            trialParams.trialD.stairCase = useStair;

            if (useStair == 1)
            {
                
               tmpQ = (float)questStair1.Mean();
                

            }
            else if (useStair == 2)
            {

                tmpQ = (float)questStair2.Mean();
                

            }


            //// feed prev contrast to quest.
            //questStair.UpdateQ(tmpQ, tmpAcc);
            //// extract new contrast (revert from log scale)
            //tmpQ =(float)questStair.Mean();



            if (disturbQuestContrast)
            {   // add some jitter to our value (= +/- .002)   
                float adjustcontrast = Random.value < .5f ? -.002f : +.002f;
                tmpQ += adjustcontrast;
            }

            if (tmpQ <= 0.400005)
            {
                print("contrast out of range, flooring");
                tmpQ = 0.4005f;
            } else if (tmpQ >= 1)
            {
                print("contrast out of range, ceiling");
                tmpQ =.95f;
            }

            //print("(new) quest contrast value is " + tmpQ + ", Stair: " + useStair);


            // actually change target contrast (called in targetAppearance.cs);
            trialParams.targetColor = new Color(tmpQ, tmpQ, tmpQ, trialParams.targetAlpha);


        }
        else // for the remaineder of the experiment. jitter contrast values around the calibrated intensity:
        {
            // after the staircase, set the range:
            if (!expContrastset && trialParams.nStaircaseBlocks > 0) // if we did not yet set the range:
            {

                // set the boundaries of our quantile range (scale up from very low quantile)
                float uselow = 0;
                float usehigh = 0;
                float tmpQav;
                float tmpQnt1;
                float tmpQnt2;
                if (nStairs == 2)
                {
                    // take average of both staircases:
                        tmpQav = ((float)questStair1.Mean() + (float)questStair2.Mean()) / 2;
                        tmpQnt1 = (float)questStair1.Quantile(.25);
                        tmpQnt2 = (float)questStair2.Quantile(.25);
                    


                    if (tmpQnt1 < tmpQnt2)
                    {
                        uselow = tmpQnt1;
                    }
                    else { uselow = tmpQnt2; };


                }
                else
                {
                    // or just use the resultant output, to define range (one staircase).
                   
                        tmpQav = (float)questStair1.Mean();
                        uselow = (float)questStair1.Quantile(.15); // increased spread from 0.25
                   
                }


                // don' go below the backround contrast.
                if (uselow < trialParams.probeColor[0])
                {
                    uselow = trialParams.probeColor[0];
                    print("warning: lower quantile exceeds background contrast, adjusting range");
                }

                // problem with using upper quantile (0.75). Instead, reflect the same distance just used, about the mean.
                float tdiff = tmpQav - uselow;
                usehigh = tmpQav + tdiff;

                // now define range for subsequent trials"
                float[] lrange = myMathsMethods.Linspace(uselow, tmpQav, 3);
                float[] urange = myMathsMethods.Linspace(tmpQav, usehigh, 3);
                //add upper quantile to array

                trialParams.myCalibContrast[0] = lrange[0]; // lower
                trialParams.myCalibContrast[1] = lrange[1];
                trialParams.myCalibContrast[2] = lrange[2];
                trialParams.myCalibContrast[3] = tmpQav;
                trialParams.myCalibContrast[4] = urange[1]; // upper
                trialParams.myCalibContrast[5] = urange[2]; // upper
                trialParams.myCalibContrast[6] = usehigh; // upper


                print("staircase over: calibrated contrast [0]" + trialParams.myCalibContrast[0]);
                print("staircase over: calibrated contrast [1]" + trialParams.myCalibContrast[1]);
                print("staircase over: calibrated contrast [2]" + trialParams.myCalibContrast[2]);
                print("staircase over: calibrated contrast [3]" + trialParams.myCalibContrast[3]);
                print("staircase over: calibrated contrast [4]" + trialParams.myCalibContrast[4]);
                print("staircase over: calibrated contrast [5]" + trialParams.myCalibContrast[5]);
                print("staircase over: calibrated contrast [6]" + trialParams.myCalibContrast[6]);

                expContrastset = true;
            }
            else if (!expContrastset && trialParams.nStaircaseBlocks == 0)
            {
                // we will use the precalibrated contrast values:

                trialParams.myCalibContrast = trialParams.prevCalibContrast;

                print("skipping staircase: calibrated contrast [0]" + trialParams.myCalibContrast[0]);
                print("skipping staircase: calibrated contrast [1]" + trialParams.myCalibContrast[1]);
                print("skipping staircase: calibrated contrast [2]" + trialParams.myCalibContrast[2]);
                print("skipping staircase: calibrated contrast [3]" + trialParams.myCalibContrast[3]);
                print("skipping staircase: calibrated contrast [4]" + trialParams.myCalibContrast[4]);
                print("skipping staircase: calibrated contrast [5]" + trialParams.myCalibContrast[5]);
                print("skipping staircase: calibrated contrast [6]" + trialParams.myCalibContrast[6]);
                expContrastset = true;
            }
            // select from the range specified above
            int randomIndex;
            randomIndex = Random.Range(0, 7); // min-max exclusive.
            tmpQ = trialParams.myCalibContrast[randomIndex];
            print("next contr pos: " + randomIndex);
        }
        }

    // based on various listeners, and experiment position, determine which text to show (or hide) from
    // the participant.
    public void determineText()
    {

        if (trialParams.trialD.trialID == trialParams.ntrialsperBlock - 1)
        {

            // stationary or not on the next block?

            float mvmnt = trialParams.blockTypeArray[TrialCount + 1, 2];
            // query if stationary or not.
            isStationary = mvmnt == 0 ? true : false;

            if (isStationary)
            {
                showText.updateText(5);
                usematerial = 0;
                changeMat.update(0); // Render green arrow.
            }
            else
            {
                showText.updateText(6);
                usematerial = 1; //green arrow.
                changeMat.update(usematerial); // Render green arrow.
            }


            setXpos = true;
        } else if (trialParams.trialD.trialID>0)
        {
            showText.updateText(3); // show Trial count between trials.
            setXpos = false;
        }




    }
    

    void trialPackdown()
    {
        trialinProgress = false;
        trialTime = 0;

        // safety, these should already have been stopped in walkingGuide
        randomWalk.walk = randomWalk.phase.stop;
        recordData.recordPhase = recordData.phase.stop;
        // also stop guide, and start return rotation
        walkingGuide.walkMotion = walkingGuide.motion.idle;
        
        walkingGuide.returnRotation = walkingGuide.motion.start;

        
        walkingGuide.returnRotation = walkingGuide.motion.start;
        print("End of Trial " + (TrialCount+1));



        // if absent trial has just ended - pass that to Record data too.
        if (TrialType == 0)
        {
            if (!FAthistrial)
            {
                print("Correct Rejection!"); // do not update staircase.
                trialParams.trialD.targCorrect = 1;

            }
            else
            {
                print("FA recorded...");
                trialParams.trialD.targCorrect = 0;
            }
            // pass to Record Data (after every absent trial)

            trialParams.trialD.targContrast = tmpQ;
            recordData.collectTrialSummary(); // appends information to text file.
        }



        targetAppearance.setColour(trialParams.preTrialColor); // indicates ready for click to begin trial

        // write trial summary to text file (in debugging).
        //recordData.writeTrialSummary(); // writes text to csv after each trial.


        // if last trial of the block, prepare the placement of intructions / start pos./

      
        if (trialParams.trialD.trialID== trialParams.ntrialsperBlock-1)
        {
            setXpos = true;
        }
        updateText = true; // show text between trials.


        // set redX to active:
        redX.SetActive(true);
    }


}

