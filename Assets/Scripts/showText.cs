using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class showText : MonoBehaviour
{


    private TextMeshProUGUI textMesh;
    
    private string thestring;
    // Start is called before the first frame update
    runExperiment runExperiment;
    trialParameters trialParameters;

    void Start()
    {
        textMesh = gameObject.GetComponent<TextMeshProUGUI>();
        runExperiment = GameObject.Find("scriptHolder").GetComponent<runExperiment>();
        trialParameters = GameObject.Find("scriptHolder").GetComponent<trialParameters>();

    }

    //
    public void updateText(int text2show)
    {
        if (text2show == 0)
        {
            // hide text 
            thestring = ""; // blank
        }
        else if (text2show == 1)
        {
            // update at certain points.
            thestring = "Welcome! \n  When the target inside the circle is green: <Left click> to start a Trial \n\n" +
               " It will then disappear. Your task is to watch inside the circle, and <Right click> if the target reappears. \n\n" +
               "Let's practice standing still..  \n\n " +
               "Pull the <left> Trigger to begin practice trials.";

        }
        else if (text2show == 2)
        {
            // update at certain points.
            thestring = "Well done! \n Now, Practice is over. " +
                 "For the remainder of the experiment, the same task must be completed while " +
                 "walking, or standing still. " +
                   "Please stand on the red X position. \n\n" +
                    " When ready, pull the <left> Trigger to begin";

        }

        else if (text2show == 3)
        {
            thestring = "Pull the <left> trigger to begin Trial " + (trialParameters.trialD.trialID + 2) + " / " + trialParameters.ntrialsperBlock +"\n\n" + // +2 because the trialID is incremented after left click.
                "(Block " + (trialParameters.trialD.blockID + 1) + " of " + trialParameters.nBlocks +")."; // blank
        }
        else if (text2show == 4)
        {
            thestring = "Experiment over, thank you for your participation";
        }
        else if (text2show == 5)
        {
            // standing instructions.
            thestring = "For the next block of trials, \n\n" +
               "the same task must be completed while " +
               " standing still." +
                 " \n\n" +
                  "When ready, pull the <left> Trigger to begin";
        }
        else if (text2show == 6)
        {
            // standing instructions.
            thestring = "For the next block of trials, \n\n" +
               "the same task must be completed while " +
               " walking." +
                 " \n\n" +
                  "When ready, pull the <left> Trigger to begin";
        }


        thestring.Replace("\\n", "\n");
        textMesh.text = thestring;
    }
}
