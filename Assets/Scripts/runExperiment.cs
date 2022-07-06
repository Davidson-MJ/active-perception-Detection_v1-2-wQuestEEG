using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.InputSystem;
using LSL;

/// <summary>
///  This script runs a  single target detection experiment,
///  with continuous quest staircase (separate staircases per condition).
///  The contrast of each target is jittered based on the trial history.
///  
/// 
/// ---------------- v1_2 ------------------------
/// ----- adding support for eyetracking and EEG
/// ----------------------------------------------
/// </summary>
public class runExperiment : MonoBehaviour
{

    // basic experiment structure/ parameters to toggle.
    public string participant;
    public int TrialCount; //n walk trajectories
    public int TrialType;  // n targs absent, n present
    public int BlockType; // walking, stationary (int)
    public int targCount; // targs presented (acculative), used to track data.
    public bool forceEyeCalibration = false;
    public bool isPractice = true; // determines walking guide motion (stationary during practice).
    public bool isStationary = true;
    public bool prepLSL = false;
    public bool recordEEG = true;
    public bool isEyeTracked = false;
    private int npractrials = 1; // 0 : n practice trials before staircase is initiated.
    // flow managers
    public bool trialinProgress; // handles current state within experiment 
    private bool FAthistrial; // listen for FA in no targ trials, pass to update staircase/recording data.
    private bool SetUpSession; // for alignment of walking space.
    private int usematerial;  // change walk image (stop sign and arrows).
    private int useStair; // we have (up to)  3 staircases running to see if they converge.
    public bool updateText;
    private bool setXpos;
    public bool questready; // boool to switch quest params on.
    int indexRandom; // index to select from contrast jitters within trial sequenece.

    // passed to other scripts (couroutine, record data etc).
    public bool collectTrialSummary; // passed to recordData.
    public float trialTime; // clock within trial time, for RT analysis.
    public int targState; // targ currently on screen, used to synchron recordings in recordData (frame by frame).
    public int detectIndex; // index to allocate response to correct target within walk.
    public int pauseRW; // used to pause the RW of a target while flash is being presented.
    public bool hasResponded; //listener for trigger responses after target onset < respone Window.

    //trial  
    public List<float> FA_withintrial = new List<float>(); // collect RT of FA within each trial (wipes every trial) passed to RecordData.

    // speak to:
    ViveInput viveInput;
    recordData recordData;
    BrownianMotion BrownianMotion;
    walkParameters walkParams;
    walkingGuide walkingGuide;
    trialParameters trialParams;
    showText showText;
    changeDirectionMaterial changeMat;
    targetAppearance targetAppearance;
    myMathsMethods myMathsMethods;
    EyetrackProcesses EyetrackProcesses;
    SerialController SerialController;
    // declare public Game Objects.
    public GameObject hmd, effector, SphereShader, redX, objSRanipal;


    //For quest:
    public QuestParam questP;
    //public QuestStaircase[] questStair, questStair2;
             QuestStaircase[] questStair;

    [SerializeField] [ReadOnly] private float Qcontrast, Qlowquantile; // quest mean , and lower quantile for each iteration
    public float[] contrastOptions;


    // prep an LSL stream:

    string StreamName = "LSL4Unity";
    string StreamType = "Markers";
    private StreamOutlet outlet;
    private string[] sample = { "" };
    

void Start()
{
    // Dependencies        
    targetAppearance = GameObject.Find("TargetCylinder").GetComponent<targetAppearance>();
    BrownianMotion = GameObject.Find("TargetCylinder").GetComponent<BrownianMotion>();
    walkingGuide = GameObject.Find("motionPath").GetComponent<walkingGuide>();
    viveInput = GetComponent<ViveInput>();
    recordData = GetComponent<recordData>();
    walkParams = GetComponent<walkParameters>();
    trialParams = GetComponent<trialParameters>();
    questP = GetComponent<QuestParam>();
    myMathsMethods = GetComponent<myMathsMethods>();
    showText = GameObject.Find("Instructions (TMP)").GetComponent<showText>();
    changeMat = GameObject.Find("directionCanvas").GetComponent<changeDirectionMaterial>();
   
    redX = GameObject.Find("RedX");

    EyetrackProcesses = GameObject.Find("SRanipal").GetComponent<EyetrackProcesses>();

        questStair = new QuestStaircase[2]; // one staircase each for walking, and stationary condition.
    for (int i = 0; i < 2; i++)
    {
        questStair[i] = GetComponent<QuestStaircase>();
    }

    //flow managers
    TrialCount = 0;
    targCount = 0;
    trialinProgress = false;
    SetUpSession = true;
    collectTrialSummary = false; // send info after each target to be written to a csv file
    questready = false;
    updateText = true;
    usematerial = 0; // 0=show stop sign, later changed to arrows for walk guide.
    pauseRW = 0;
    setXpos = false;
    changeMat.update(0); // render stop sign
    showText.updateText(1); // pre  exp instructions
    indexRandom = 4;

    // for Quest:
    Qcontrast = 0.46f; // starting guess (for quest threshold).
    contrastOptions = new float[7];

        // initialize LSL outlet"
    if (prepLSL)
    {
        var hash = new Hash128();
        hash.Append(StreamName);
        hash.Append(StreamType);
        hash.Append(gameObject.GetInstanceID());
        // set up stream params (note this may need to change from string to float type.)
        StreamInfo streamInfo = new StreamInfo(StreamName, StreamType, 1, LSL.LSL.IRREGULAR_RATE, channel_format_t.cf_string, hash.ToString());
        outlet = new StreamOutlet(streamInfo);
    }

        if (isEyeTracked)
        {
            EyetrackProcesses.eyeStartup();
        }
        else
        {
            objSRanipal.SetActive(false);
        }

        if (recordEEG)
        {
            SerialController = GetComponent<SerialController>();
        }
    }


private void Update()
{

    // set up origin.
    if (SetUpSession)
    {

        CalibrateStartPos(); // align motion origin to centre, startflag, or player (
        targetAppearance.setColour(trialParams.preTrialColor); // indicates ready for click to begin trial

        if (!questready)
        {
            InitQuestParams(Qcontrast);
            questready = true;
        }


    }

    //
     // check if eyeCalibration needs to be redone (bool state toggled in inspector window only).
        if(forceEyeCalibration)
        {
            EyetrackProcesses.eyeStartup();
           
            forceEyeCalibration = false;
            // if all else fails.
            //for (int i = 0; i < 2; i++)
            //{
            //    questStair[i] = GetComponent<QuestStaircase>();
            //}
            //InitQuestParams(Qcontrast);

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
        showText.updateText(4); // 

    }
}



/// 
/// ////////////////////////////////////////
/// RUN EXPERIMENT METHODS:
/// //////////////////////////////////////////
/// 


void InitQuestParams(float tmpQ) // initialize quest paramaterst based on starting guess
{

    questP.tGuess = tmpQ;
    // create 2 quest staircases for the current session.
    // One per condition, called when we are in that condition type.

    for (int stair = 0; stair < 2; stair++)
    {
        questStair[stair] = new QuestStaircase(questP.tGuess,
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


    if (isStationary) // align to origin.
    {
        motionOrigin.transform.position = startO;
        startX.y = redX.transform.position.y;
    }
    else
    {
        //align to start Pos (near the flag)
        GameObject startFlag = GameObject.Find("Start Flag pole");
        Vector3 flagpos = startFlag.transform.position;

        startO.x = flagpos.x - 1.5f;
        startO.z = flagpos.z - 0.5f;
        startX.x = flagpos.x - 1.5f;
        startX.z = flagpos.z - 0.5f;
    }

    motionOrigin.transform.position = startO;
    redX.transform.position = startX;
    setXpos = false;

    // also align Screen height (Y) to participant head (in case of slipping).
    // but only after the first frame on init.
    if (questready) { walkParams.updateScreenHeight(); }
        
    SetUpSession = false;
    walkingGuide.fillStartPos(); // update start pos in WG.                 
}


private void startTrial()
{

        // re-calibrate screen height to participants hmd:
        walkParams.updateScreenHeight();


    // clear previous trial info, reset, and assign from preallocated variables:
    trialinProgress = true; // for coroutine (handled in targetAppearance.cs).        
    showText.updateText(0); // remove text       
    redX.SetActive(false); //  // set redX to hidden:
    trialTime = 0;  // clock accurate reacton time from time start      
    targState = 0; // target is hidden.
    FAthistrial = false; // boolean passed to Update() to confirm whether absent types were correct or not.
    FA_withintrial.Clear();  // temp file for any FAs:

    // Establish (these) trial parameters:
    TrialType = trialParams.trialTypeArray[TrialCount]; //
    BlockType = trialParams.blockTypeArray[TrialCount, 2];

    // query if stationary or not.
    isStationary = BlockType == 0 ? true : false; // 0 and 1 (in BlockType) corresponds to stationary or moving)

    // add to trialD for recordData.cs
    trialParams.trialD.trialNumber = TrialCount;
    trialParams.trialD.blockID = trialParams.blockTypeArray[TrialCount, 0];
    trialParams.trialD.trialID = trialParams.blockTypeArray[TrialCount, 1];
    trialParams.trialD.trialType = TrialType; //  


    //store bool as int
    float fStat = isStationary ? 1 : 0;
    trialParams.trialD.isStationary = fStat;

    BrownianMotion.transform.localPosition = walkParams.cubeOrigin;
    BrownianMotion.origin = walkParams.cubeOrigin;
    walkParams.lowerBoundaries = walkParams.cubeOrigin - walkParams.cubeDimensions;
    walkParams.upperBoundaries = walkParams.cubeOrigin + walkParams.cubeDimensions;

    BrownianMotion.lowerBoundaries = walkParams.lowerBoundaries;
    BrownianMotion.upperBoundaries = walkParams.upperBoundaries;
    BrownianMotion.stepDurationRange = walkParams.stepDurationRange;
    // can't use   = stepDistanceRange; as the string is rounded to 1f precision.
    // so access the dimensions directly:
    BrownianMotion.stepDistanceRange.x = walkParams.stepDistanceRange.x;
    BrownianMotion.stepDistanceRange.y = walkParams.stepDistanceRange.y;

    // set fields in BrownianMotion and recordData to begin exp:
    BrownianMotion.walk = BrownianMotion.phase.start;
    recordData.recordPhase = recordData.phase.collectResponse;
    walkingGuide.walkMotion = walkingGuide.motion.start;
    walkingGuide.returnRotation = walkingGuide.motion.idle;

    // mark trial start in LSL
    if (prepLSL && outlet != null)
    {
        sample[0] = "trialType: " + TrialType;
        outlet.push_sample(sample);
    }


    if (recordEEG)
    {
            SerialController.SendSerialMessage("trialType:" + TrialType);
    }

        // define if within practice blocks
    if (trialParams.blockTypeArray[TrialCount, 0] < trialParams.nStaircaseBlocks)
    {
        // set for outside(BrownianMotion) listeners. When practice, motion guide is stationary.
        isPractice = true;
        changeMat.update(usematerial); // Render green arrow or stop.
    }
    else
    {
        isPractice = false;
        changeMat.update(usematerial); //usematerial determined by text instructions (stop or go arrow).
    }

    //start coroutine to control target onset and target behaviour:
    print("Starting Trial " + (TrialCount + 1) + " of " + trialParams.nTrials + ", " + TrialType + " to detect");
    targetAppearance.startSequence(); // co routine in another script.

    }

// method for collecting responses after target presentation, assigning correct or not.
private void collectDetect()
{
    //Record click - R click for target perceived present                       
    

            if (recordEEG)
            {
                SerialController.SendSerialMessage("R1"); // response 1
            }

            trialParams.trialD.clickOnsetTime = trialTime;
        //determine if this RT was within response window of targ.
        // we have a listener in the coroutine (detectIndex). this determines whether RT was appropriate.

        if (detectIndex != 0 && !hasResponded)  // first resp within allocated response window
        {

            trialParams.trialD.targCorrect = 1;
            trialParams.trialD.targResponse = 1;
            trialParams.trialD.targResponseTime = trialTime;
            trialParams.trialD.targContrast = Qcontrast;
            trialParams.trialD.targContrastPosIdx = indexRandom; // this is the index for the presented contrast.
                //targ onset times already appended, within coroutine.

                print("Hit!");

            hasResponded = true; // passed to coroutine, avoids processing omitted responses.


            // Update contrast, after the first 3 trials of both practice blocks.
            if (isPractice && trialParams.blockTypeArray[TrialCount, 1] > npractrials)
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

private void collectOmit() // only relevant to response window following targs.
{

        if (recordEEG)
        {
            SerialController.SendSerialMessage("R0"); // response 1
        }
        // Miss
        trialParams.trialD.targCorrect = 0;
    trialParams.trialD.targResponse = 0;
    trialParams.trialD.targResponseTime = 0;
    trialParams.trialD.targContrast = Qcontrast;
    trialParams.trialD.targContrastPosIdx = indexRandom;

        print("Miss!");
    // 
    // set up  contrast for the next target, if within staircase.
    // we want to run the staircase twice.
    // update targ contrast if within staircase block. and after first 3 trials.

    // Uupdate contrast, after the first 3 trials of both practice blocks.
    if (isPractice && trialParams.blockTypeArray[TrialCount, 1] > npractrials)
    {
        updateTargContrast(trialParams); // based on staircase.
    }
    else if (!isPractice)
    {

        updateTargContrast(trialParams); // 
    }
    recordData.collectTrialSummary();// pass to Record Data (after every missed targ)

}



private void updateTargContrast(trialParameters trialParams)
{

    //Using Quest, continuously now, so always update quest

    // was last respnse correct or no?
    int tmpAcc = (int)trialParams.trialD.targCorrect;

    // update the relevant staircase, based on whether we are moving or not.
    // then extract new contrast, before adding jitter based on quantile range of quest distribution

    if ( Qcontrast<=0 || Qcontrast==0.0f ) { // check if float is null
            

            Qcontrast = 0.4005f; print("contrast out of range, flooring");
    }
        print("BlockType:" + BlockType);
        print("Qcontrast:" + Qcontrast);
        print("tmpAcc: " + tmpAcc);
        print("questStair[0]:" + questStair[0]);
        print("questStair[1]:" + questStair[1]);

        questStair[BlockType].UpdateQ(Qcontrast, tmpAcc);

    Qcontrast = (float)questStair[BlockType].Mean();
    // space about qmean, based on history of guesses:
    // calling .Quantile is slow, so just do it once.
        float tdiff = Qcontrast - (float)questStair[BlockType].Quantile(.25);


        // after pilotting, these intervals help to recreate the psych-fxns (low lapse rates). quick saturation of accuracy above qmean, and below.
        contrastOptions[0] = Qcontrast - tdiff;
        contrastOptions[1] = Qcontrast - tdiff * 0.75f;
        contrastOptions[2] = Qcontrast - tdiff * 0.5f;
        contrastOptions[3] = Qcontrast;
        contrastOptions[4] = Qcontrast + tdiff * 0.5f;
        contrastOptions[5] = Qcontrast + tdiff * 0.75f;
        contrastOptions[6] = Qcontrast + tdiff; 


        // now select one of these at random:                
        indexRandom = Random.Range(0, 7);
        Qcontrast = contrastOptions[indexRandom];
    
        //}
       
            // sanity checks:
       if (Qcontrast <= 0.4005)
        {
            print("contrast out of range, flooring");
            Qcontrast = 0.4005f;
        }
        else if (Qcontrast >= 1)
        {
            print("contrast out of range, ceiling");
            Qcontrast = .95f;
        }

    // actually change target contrast (called in targetAppearance.cs);
    trialParams.targetColor = new Color(Qcontrast, Qcontrast, Qcontrast, trialParams.targetAlpha);

    // store the trial/target params for offline analysis.
    trialParams.trialD.stairCase = BlockType;
        
        print("Using staircase " + BlockType);     
        print("using index " + indexRandom);
        print("Contrast is : " + Qcontrast);
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
    }
    else if (trialParams.trialD.trialID > 0)
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
    BrownianMotion.walk = BrownianMotion.phase.stop;
    recordData.recordPhase = recordData.phase.stop;
    // also stop guide, and start return rotation
    walkingGuide.walkMotion = walkingGuide.motion.idle;

    walkingGuide.returnRotation = walkingGuide.motion.start;


    walkingGuide.returnRotation = walkingGuide.motion.start;
    print("End of Trial " + (TrialCount + 1));



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

        trialParams.trialD.targContrast = Qcontrast;
        recordData.collectTrialSummary(); // appends information to text file.
    }



    targetAppearance.setColour(trialParams.preTrialColor); // indicates ready for click to begin trial

    // write trial summary to text file (in debugging).
    //recordData.writeTrialSummary(); // writes text to csv after each trial.


    // if last trial of the block, prepare the placement of intructions / start pos./


    if (trialParams.trialD.trialID == trialParams.ntrialsperBlock - 1)
    {
        setXpos = true;
    }
    updateText = true; // show text between trials.


    // set redX to active:
    redX.SetActive(true);
}


}

