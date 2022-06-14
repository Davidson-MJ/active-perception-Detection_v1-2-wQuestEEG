using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class changeDirectionMaterial : MonoBehaviour
{
    /// <summary>
    ///  change the material rendered on top of the WG cube,
    ///  to show either an arrow or stop sign (depending on experiment position)
    /// </summary>


    public Material[] material; // m for our object
    Renderer rend;
    runExperiment runExp; // to know trial progression.
    walkingGuide wG; // to know arrow direction.
    Quaternion localRot;

    void Start()
    {
        rend = GetComponent<Renderer>();
        rend.enabled = true; // just in case
        rend.sharedMaterial = material[0];

        runExp = GameObject.Find("scriptHolder").GetComponent<runExperiment>();
        wG = GameObject.Find("motionPath").GetComponent<walkingGuide>();
        localRot = transform.localRotation;
    }

    // update the material when called.
    public void update(int materialIndex)
    {
        /// called from runExperiment. 
        rend.sharedMaterial = material[materialIndex];

    }
    public void flipArrow()
    {
        // if not practice, flip arrow to match walk direction.        
        
        if (runExp.TrialCount % 2 == 0)
        {
            // even trials.
            transform.localEulerAngles = new Vector3(90f, 0f, 270f);       
        }
        else
        {
            transform.localEulerAngles = new Vector3(90f,0f, 90f);
        }
    }
}
