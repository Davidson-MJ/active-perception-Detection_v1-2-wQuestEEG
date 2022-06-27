using System.Runtime.InteropServices;
using System;
using System.Collections;
using UnityEngine;
using ViveSR.anipal;
using ViveSR.anipal.Eye;

public class EyetrackProcesses : MonoBehaviour
{
    private static EyeData_v2 eyeData = new EyeData_v2();
    private bool eye_callback_registered = false;
  
    public static Vector3 vectGazeOrigin, vectGazeDirection;
    trialParameters trialParameters;
    runExperiment runExperiment;
    void Start()
    {
        trialParameters = GameObject.Find("scriptHolder").GetComponent<trialParameters>();
        runExperiment = GameObject.Find("scriptHolder").GetComponent<runExperiment>();
    }
    public void eyeStartup()
    {
        if(!SRanipal_Eye_Framework.Instance.EnableEye)
        {
            Debug.Log("When in doubt, go to the Framework",GameObject.Find("SRanipal")); // Adapted from quote by Ron Weasley
            if (UnityEditor.EditorApplication.isPlaying)
            {
                UnityEditor.EditorApplication.isPlaying = false;
            }
        } 
        if(!SRanipal_Eye_API.IsViveProEye())
        {
            Debug.Log("This ain't the Vive you're looking for"); // Adapted from quote by Obi-Wan Kenobi
            if (UnityEditor.EditorApplication.isPlaying)
            {
                UnityEditor.EditorApplication.isPlaying = false;
            }
        }
        if (SRanipal_Eye_API.GetEyeData_v2(ref eyeData) == ViveSR.Error.WORK)
        {
            Debug.Log("Pro Eye good to go!");
        }
        if (SRanipal_API.Initial(SRanipal_Eye_v2.ANIPAL_TYPE_EYE_V2, IntPtr.Zero) == ViveSR.Error.WORK)
        {
            Debug.Log("The Vive abides"); // Adapted from quote by The Dude
        }
        else
        {
            Debug.Log("It's the job that's never started as takes longest to finish"); // Quote by Samwise Gamgee
            if (UnityEditor.EditorApplication.isPlaying)
            {
                UnityEditor.EditorApplication.isPlaying = false;
            }
        }
        //SRanipal_Eye_API.IsUserNeedCalibration(ref needsCalibration);
        //if (needsCalibration)
        //{
            Debug.Log("Two steps forward and one step back is still one step forward"); // Quote by Rosa Diaz
            if (SRanipal_Eye_v2.LaunchEyeCalibration() == false)
            {
                Debug.Log("You are running away from your responsibilities. Does it feel good?"); // Adapted from quote by Michael Scott
                if (UnityEditor.EditorApplication.isPlaying)
                {
                //UnityEditor.EditorApplication.isPlaying = false; //MD toggled, to keep the editor running.
                }
            }
            else
            {
                Debug.Log("You, my friend, are responsible for delaying our rendezvous with Eye Tracking Data"); // Adapted from quote by Buzz Lightyear
            }
        //}
    }
    public void Update()
    {
        if (runExperiment.isEyeTracked && runExperiment.trialinProgress)
        {
            if (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING &&
                SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.NOT_SUPPORT) return;
            if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == true && eye_callback_registered == false)
            {
                SRanipal_Eye_v2.WrapperRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
                eye_callback_registered = true;
            }
            else if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == false && eye_callback_registered == true)
            {
                SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
                eye_callback_registered = false;
            }
        }

    }
    static void EyeCallback(ref EyeData_v2 eye_data)
    {
        eyeData = eye_data;
        vectGazeDirection = eyeData.verbose_data.combined.eye_data.gaze_direction_normalized;
        vectGazeOrigin = eyeData.verbose_data.combined.eye_data.gaze_origin_mm;
    }
    private void OnDisable()
    {
        Release();
    }
    private void OnApplicationQuit()
    {
        Release();
    }
    private void Release()
    {
        if (eye_callback_registered == true)
        {
            SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
            eye_callback_registered = false;
        }
    }
}