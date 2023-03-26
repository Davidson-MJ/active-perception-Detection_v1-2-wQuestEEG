using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;

using UnityEngine;
#if UNITY_2019_1_OR_NEWER
using System.Collections.Generic;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
#endif
#if !UNITY_2019_1_OR_NEWER
#endif
#if hPHOTON2
using Photon.Pun;
#endif

namespace Passer.Humanoid {

    [InitializeOnLoad]
    public class ConfigurationCheck {
        static ConfigurationCheck() {
            if (isHumanoid4)
                Configuration_Editor.GlobalDefine("h4");

            CheckXrSdks();
        }

        public static void CheckXrSdks() {
            Configuration_Editor.FindHumanoidFolder();

            Configuration configuration = Configuration_Editor.GetConfiguration();


#if (UNITY_STANDALONE_WIN || UNITY_ANDROID)
            bool oculusSupported
#if hLEGACYXR
                = Oculus_Editor.OculusSupported();
#else
                = false;
#endif
            if (oculusSupported && !configuration.oculusSupport) {
                configuration.oculusSupport = true;
            }
#endif

#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
            bool steamVrSupported
#if hLEGACYXR
                = Passer.Humanoid.OpenVR_Editor.OpenVRSupported();
#else
                = false;
#endif
            if (steamVrSupported && !configuration.openVRSupport) {
                configuration.openVRSupport = true;
            }
#endif

#if (UNITY_2017_2_OR_NEWER && UNITY_WSA_10_0)
            bool windowsMrSupported
#if hLEGACYXR
                = WindowsMR_Editor.MixedRealitySupported();
#else
                = false;
#endif
            if (windowsMrSupported && !configuration.windowsMRSupport) {
                configuration.windowsMRSupport = true;
            }
#if !hWINDOWSMR
            if (windowsMrSupported)
                configuration.windowsMRSupport = false;
#endif
#endif
        }

        [DidReloadScripts]
        protected static void DidReloadScripts() {
            Configuration_Editor.FindHumanoidFolder();

            Configuration configuration = Configuration_Editor.GetConfiguration();
            Configuration_Editor.CheckExtensions(configuration);
        }

        #region Availability

        #region Support
        private static bool isHumanoid4 {
            get {
                return Configuration_Editor.DoesTypeExist("Passer.Site");
            }
        }
        #endregion Suport

        #endregion Availability
    }

    [CustomEditor(typeof(Configuration))]
    public class Configuration_Editor : Pawn.Configuration_Editor {
        private Configuration configuration;

        private static string humanoidPath;

        private const string openVRPath = "Extensions/OpenVR/OpenVR.cs";
        private const string oculusPath = "Extensions/Oculus/Oculus.cs";
        private const string windowsMRPath = "Extensions/WindowsMR/WindowsMR.cs";
        private const string neuronPath = "Extensions/PerceptionNeuron/PerceptionNeuron.cs";
        private const string realsensePath = "Extensions/IntelRealsense/IntelRealsense.cs";
        private const string kinect1Path = "Extensions/MicrosoftKinect1/MicrosoftKinect1.cs";
        private const string kinect2Path = "Extensions/MicrosoftKinect2/MicrosoftKinect2.cs";
        private const string kinect4Path = "Extensions/MicrosoftKinect4/AzureKinect.cs";
        private const string hydraPath = "Extensions/RazerHydra/RazerHydra.cs";
        public const string arkitPath = "Extensions/Arkit/ArKit.cs";
        private const string pupilPath = "Extensions/Pupil/PupilTracker.cs";

        private const string facePath = "FaceControl/EyeTarget.cs";

        public static Configuration CreateDefaultConfiguration() {
            Configuration configuration;

            Debug.Log("Created new Default Configuration");
            // Create new Default Configuration
            configuration = CreateInstance<Configuration>();
            configuration.oculusSupport = true;
            configuration.openVRSupport = true;
            configuration.windowsMRSupport = true;

            string path = humanoidPath.Substring(0, humanoidPath.Length - 1); // strip last /
            path = path.Substring(0, path.LastIndexOf('/') + 1); // strip Scripts;
            path = "Assets" + path + "DefaultConfiguration.asset";
            AssetDatabase.CreateAsset(configuration, path);
            AssetDatabase.SaveAssets();
            return configuration;
        }

        public static Configuration GetConfiguration() {
            string humanoidPath = FindHumanoidFolder();
            humanoidPath = humanoidPath.Substring(0, humanoidPath.Length - 1); // strip last /
            humanoidPath = humanoidPath.Substring(0, humanoidPath.LastIndexOf('/') + 1); // strip Scripts;

            string configurationString = EditorPrefs.GetString("HumanoidConfigurationKey", "DefaultConfiguration");
#if UNITY_2018_3_OR_NEWER
            var settings = AssetDatabase.LoadAssetAtPath<HumanoidSettings>("Assets" + humanoidPath + "HumanoidSettings.asset");
            if (settings == null) {
                settings = CreateInstance<HumanoidSettings>();

                AssetDatabase.CreateAsset(settings, HumanoidSettings.settingsPath);
                AssetDatabase.SaveAssets();
            }

            SerializedObject serializedSettings = new SerializedObject(settings);
            SerializedProperty configurationProp = serializedSettings.FindProperty("configuration");
            if (settings.configuration == null) {
#endif
            Configuration configuration = LoadConfiguration(configurationString);
            if (configuration == null) {
                configurationString = "DefaultConfiguration";
                configuration = LoadConfiguration(configurationString);
                if (configuration == null) {
                    Debug.Log("Created new Default Configuration");
                    // Create new Default Configuration
                    configuration = CreateInstance<Configuration>();
                    configuration.oculusSupport = true;
                    configuration.openVRSupport = true;
                    configuration.windowsMRSupport = true;
                    string path = "Assets" + humanoidPath + configurationString + ".asset";
                    Debug.Log(configuration + " " + path);
                    AssetDatabase.CreateAsset(configuration, path);
                    AssetDatabase.SaveAssets();
                }
            }
#if UNITY_2018_3_OR_NEWER
                configurationProp.objectReferenceValue = configuration;
            }
            serializedSettings.ApplyModifiedProperties();
            return (Configuration)configurationProp.objectReferenceValue;
#else

            return configuration;
#endif
        }

        #region Enable 
        public void OnEnable() {
            configuration = (Configuration)target;

            humanoidPath = FindHumanoidFolder();
        }

        public static string FindHumanoidFolder() {
            // Path is correct
            if (IsFileAvailable("HumanoidControl.cs"))
                return humanoidPath;

            // Determine in which (sub)folder HUmanoid Control has been placed
            // This makes it possible to place Humanoid Control is a different folder
            string[] hcScripts = AssetDatabase.FindAssets("HumanoidControl");
            for (int i = 0; i < hcScripts.Length; i++) {
                string assetPath = AssetDatabase.GUIDToAssetPath(hcScripts[i]);
                if (assetPath.Length > 36 && assetPath.Substring(assetPath.Length - 27, 27) == "/Scripts/HumanoidControl.cs") {
                    humanoidPath = assetPath.Substring(6, assetPath.Length - 24);
                    return humanoidPath;
                }
            }

            // Defaulting to standard folder
            humanoidPath = "/Humanoid/Scripts/";
            return humanoidPath;
        }
        #endregion

        public override void OnInspectorGUI() {
            serializedObject.Update();

            bool anyChanged = ConfigurationGUI(serializedObject);
            if (GUI.changed || anyChanged) {
                EditorUtility.SetDirty(configuration);
            }

            serializedObject.ApplyModifiedProperties();
        }

        public static bool ConfigurationGUI(SerializedObject serializedConfiguration) {
            bool anyChanged = false;

            anyChanged |= SteamVRSettingUI(serializedConfiguration);
            anyChanged |= OculusSettingUI(serializedConfiguration);
            anyChanged |= WindowsMRSettingUI(serializedConfiguration);
            anyChanged |= VrtkSettingUI(serializedConfiguration);

            anyChanged |= LeapSettingUI(serializedConfiguration);
            anyChanged |= Kinect1SettingUI(serializedConfiguration);
            anyChanged |= Kinect2SettingUI(serializedConfiguration);
            anyChanged |= Kinect4SettingUI(serializedConfiguration);
            anyChanged |= AstraSettingUI(serializedConfiguration);

            anyChanged |= RealsenseSettingUI(serializedConfiguration);
            anyChanged |= HydraSettingUI(serializedConfiguration);
            anyChanged |= TobiiSettingUI(serializedConfiguration);
            anyChanged |= ArkitSettingUI(serializedConfiguration);
            anyChanged |= PupilSettingUI(serializedConfiguration);
            anyChanged |= NeuronSettingUI(serializedConfiguration);
            anyChanged |= Hi5SettingUI(serializedConfiguration);
            anyChanged |= OptitrackSettingUI(serializedConfiguration);
            anyChanged |= AntilatencySettingUI(serializedConfiguration);

            anyChanged |= NetworkingSettingUI(serializedConfiguration);

            return anyChanged;
        }

        #region SettingUI

        public static bool SteamVRSettingUI(SerializedObject serializedConfiguration) {
            bool anyChanged = false;

            SerializedProperty openVRSupportProp = serializedConfiguration.FindProperty("openVRSupport");

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
            bool steamVrSupported
#if hLEGACYXR
                = OpenVR_Editor.OpenVRSupported();
#else
                = false;
#endif

            if (steamVrSupported) {
                openVRSupportProp.boolValue = isOpenVrSupportAvailable;
                using (new EditorGUI.DisabledScope(true)) {
                    EditorGUILayout.Toggle("OpenVR Support", openVRSupportProp.boolValue);
                }
            }
            else if (!isOpenVrSupportAvailable)
                openVRSupportProp.boolValue = false;
            else
                openVRSupportProp.boolValue = EditorGUILayout.Toggle("OpenVR Support", openVRSupportProp.boolValue);

            ViveTrackerSettingUI(serializedConfiguration);
#else
            SerializedProperty viveTrackerSupportProp = serializedConfiguration.FindProperty("viveTrackerSupport");
            if (openVRSupportProp.boolValue | viveTrackerSupportProp.boolValue)
                anyChanged = true;
            openVRSupportProp.boolValue = false;
            viveTrackerSupportProp.boolValue = false;
#endif
            return anyChanged;
        }

        public static bool ViveTrackerSettingUI(SerializedObject serializedConfiguration) {
            SerializedProperty openVRSupportProp = serializedConfiguration.FindProperty("openVRSupport");
            SerializedProperty viveTrackerSupportProp = serializedConfiguration.FindProperty("viveTrackerSupport");

            using (new EditorGUI.DisabledScope(openVRSupportProp.boolValue == false)) {
                viveTrackerSupportProp.boolValue = isViveTrackerSupportAvailable && EditorGUILayout.Toggle("Vive Tracker Support", viveTrackerSupportProp.boolValue);
            }
            return false;
        }

        public static bool OculusSettingUI(SerializedObject serializedConfiguration) {
            bool anyChanged = false;

            SerializedProperty oculusSupportProp = serializedConfiguration.FindProperty("oculusSupport");

#if UNITY_STANDALONE_WIN || UNITY_ANDROID
            bool oculusSupported
#if hLEGACYXR
                = Oculus_Editor.OculusSupported();
#else
                = false;
#endif
            if (oculusSupported) {
                oculusSupportProp.boolValue = isOculusSupportAvailable;
                using (new EditorGUI.DisabledScope(true)) {
                    EditorGUILayout.Toggle("Oculus Support", oculusSupportProp.boolValue);
                }
            }
            else if (!isOculusSupportAvailable)
                oculusSupportProp.boolValue = false;
            else
                oculusSupportProp.boolValue = EditorGUILayout.Toggle("Oculus Support", oculusSupportProp.boolValue);
#else
            if (oculusSupportProp.boolValue)
                anyChanged = true;
            oculusSupportProp.boolValue = false;
#endif
            return anyChanged;
        }

        public static bool WindowsMRSettingUI(SerializedObject serializedConfiguration) {
            bool anyChanged = false;

            SerializedProperty windowsMRSupportProp = serializedConfiguration.FindProperty("windowsMRSupport");

#if UNITY_WSA_10_0
            bool windowsMrSupported = WindowsMR_Editor.MixedRealitySupported();
            if (windowsMrSupported) {
                windowsMRSupportProp.boolValue = isWindowsMrSupportAvailable;
                using (new EditorGUI.DisabledScope(true)) {
                    EditorGUILayout.Toggle("Windows MR Support", windowsMRSupportProp.boolValue);
                }
            }
            else if (!isWindowsMrSupportAvailable)
                windowsMRSupportProp.boolValue = false;
            else
                windowsMRSupportProp.boolValue = EditorGUILayout.Toggle("Windows MR Support", windowsMRSupportProp.boolValue);
#else
            if (windowsMRSupportProp.boolValue)
                anyChanged = true;
            windowsMRSupportProp.boolValue = false;
#endif
            return anyChanged;
        }

        public static bool VrtkSettingUI(SerializedObject serializedConfiguration) {

            SerializedProperty vrtkSupportProp = serializedConfiguration.FindProperty("vrtkSupport");

            if (!isVrtkSupportAvailable)
                vrtkSupportProp.boolValue = false;

            else if (!isVrtkAvailable) {
                using (new EditorGUI.DisabledScope(true)) {
                    EditorGUILayout.Toggle("VRTK Supoort", false);
                }
                EditorGUILayout.HelpBox("VRTK is not available. Please download it from the Asset Store.", MessageType.Warning, true);
            }
            else
                vrtkSupportProp.boolValue = EditorGUILayout.Toggle("VRTK Support", vrtkSupportProp.boolValue);
            return false;
        }

        public static bool LeapSettingUI(SerializedObject serializedConfiguration) {
            SerializedProperty leapSupportProp = serializedConfiguration.FindProperty("leapSupport");

            bool oldLeapSupport = leapSupportProp.boolValue;
#if UNITY_STANDALONE_WIN || UNITY_WSA_10_0
            if (!isLeapSupportAvailable)
                leapSupportProp.boolValue = false;

            else if (!isLeapAvailable) {
                using (new EditorGUI.DisabledScope(true)) {
                    leapSupportProp.boolValue = EditorGUILayout.Toggle("Leap Motion Support", false);
                }
                EditorGUILayout.HelpBox("Leap Motion Core Assets are not available. Please download the Core Assets using the button below and import them into this project.", MessageType.Warning, true);
                if (GUILayout.Button("Download Leap Motion Unity Core Assets"))
                    Application.OpenURL("https://developer.leapmotion.com/unity");
            }

            else
                using (new EditorGUI.DisabledScope(true)) {
                    leapSupportProp.boolValue = EditorGUILayout.Toggle("Leap Motion Support", leapSupportProp.boolValue);
                }
#else
            leapSupportProp.boolValue = false;
#endif
            return (leapSupportProp.boolValue != oldLeapSupport);
        }

        public static bool Kinect1SettingUI(SerializedObject serializedConfiguration) {
            SerializedProperty kinect1SupportProp = serializedConfiguration.FindProperty("kinect1Support");

            bool oldKinectSupport = kinect1SupportProp.boolValue;
#if UNITY_STANDALONE_WIN || UNITY_WSA_10_0
            if (!isKinect1SupportAvailable)
                kinect1SupportProp.boolValue = false;
            else
                kinect1SupportProp.boolValue = EditorGUILayout.Toggle("Kinect 1 Support", kinect1SupportProp.boolValue);
#else
            kinect1SupportProp.boolValue = false;
#endif
            return (kinect1SupportProp.boolValue != oldKinectSupport);
        }

        public static bool Kinect2SettingUI(SerializedObject serializedConfiguration) {
            SerializedProperty kinect2SupportProp = serializedConfiguration.FindProperty("kinect2Support");

            bool oldKinectSupport = kinect2SupportProp.boolValue;
#if UNITY_STANDALONE_WIN || UNITY_WSA_10_0
            if (!isKinect2SupportAvailable)
                kinect2SupportProp.boolValue = false;
            else
                kinect2SupportProp.boolValue = EditorGUILayout.Toggle("Kinect 2 Support", kinect2SupportProp.boolValue);
#else
            kinect2SupportProp.boolValue = false;
#endif
            return (kinect2SupportProp.boolValue != oldKinectSupport);
        }

        public static bool Kinect4SettingUI(SerializedObject serializedConfiguration) {
            SerializedProperty kinect4SupportProp = serializedConfiguration.FindProperty("kinect4Support");

            bool oldKinectSupport = kinect4SupportProp.boolValue;
#if UNITY_STANDALONE_WIN || UNITY_WSA_10_0
            if (!isKinect4SupportAvailable)
                kinect4SupportProp.boolValue = false;
            else
                kinect4SupportProp.boolValue = EditorGUILayout.Toggle("Azure Kinect Support", kinect4SupportProp.boolValue);
#else
            kinect4SupportProp.boolValue = false;
#endif
            return (kinect4SupportProp.boolValue != oldKinectSupport);

        }

        public static bool AstraSettingUI(SerializedObject serializedConfiguration) {
            SerializedProperty astraSupportProp = serializedConfiguration.FindProperty("astraSupport");

            bool oldAstraSupport = astraSupportProp.boolValue;
#if UNITY_STANDALONE_WIN
            if (!isAstraSupportAvailable)
                astraSupportProp.boolValue = false;

            else if (!isAstraAvailable) {
                using (new EditorGUI.DisabledScope(true)) {
                    astraSupportProp.boolValue = EditorGUILayout.Toggle("Orbbec Astra Support", false);
                }
                EditorGUILayout.HelpBox("Astra SDK is not available. Please download the Astra Unity SDK using the button below and import them into this project.", MessageType.Warning, true);
                if (GUILayout.Button("Download Orbbec Astra SDK"))
                    Application.OpenURL("https://orbbec3d.com/develop/");
            }
            else
                using (new EditorGUI.DisabledScope(true)) {
                    astraSupportProp.boolValue = EditorGUILayout.Toggle("Orbbec Astra Support", astraSupportProp.boolValue);
                }
#else
            astraSupportProp.boolValue = false;
#endif
            return (astraSupportProp.boolValue != oldAstraSupport);
        }

        public static bool RealsenseSettingUI(SerializedObject serializedConfiguration) {
            SerializedProperty realsenseSupportProp = serializedConfiguration.FindProperty("realsenseSupport");

            bool oldRealsenseSupport = realsenseSupportProp.boolValue;
#if UNITY_STANDALONE_WIN
            if (!isRealsenseSupportAvailable)
                realsenseSupportProp.boolValue = false;
            else
                realsenseSupportProp.boolValue = EditorGUILayout.Toggle("Intel RealSense Support", realsenseSupportProp.boolValue);
#else
            realsenseSupportProp.boolValue = false;
#endif
            return (realsenseSupportProp.boolValue != oldRealsenseSupport);
        }

        public static bool HydraSettingUI(SerializedObject serializedConfiguration) {
            SerializedProperty hydraSupportProp = serializedConfiguration.FindProperty("hydraSupport");

            bool oldHydraSupport = hydraSupportProp.boolValue;
#if UNITY_STANDALONE_WIN || UNITY_WSA_10_0
            if (!isHydraSupportAvailable)
                hydraSupportProp.boolValue = false;
            else
                hydraSupportProp.boolValue = EditorGUILayout.Toggle("Hydra Support", hydraSupportProp.boolValue);
#else
            hydraSupportProp.boolValue = false;
#endif
            return (hydraSupportProp.boolValue != oldHydraSupport);
        }

        public static bool TobiiSettingUI(SerializedObject serializedConfiguration) {
            SerializedProperty tobiiSupportProp = serializedConfiguration.FindProperty("tobiiSupport");

            bool oldTobiiSupport = tobiiSupportProp.boolValue;
#if UNITY_STANDALONE_WIN || UNITY_WSA_10_0
            if (!isFaceSupportAvailable || !isTobiiSupportAvailable)
                tobiiSupportProp.boolValue = false;

            else if (!isTobiiAvailable) {
                using (new EditorGUI.DisabledScope(true)) {
                    tobiiSupportProp.boolValue = EditorGUILayout.Toggle("Tobii Support", false);
                }
                EditorGUILayout.HelpBox("Tobii Framework is not available. Please download the Tobii Unity SDK using the button below and import them into this project.", MessageType.Warning, true);
                if (GUILayout.Button("Download Tobii Unity SDK"))
                    Application.OpenURL("http://developer.tobii.com/tobii-unity-sdk/");
            }
            else
                using (new EditorGUI.DisabledScope(true)) {
                    tobiiSupportProp.boolValue = EditorGUILayout.Toggle("Tobii Support", tobiiSupportProp.boolValue);
                }
#else
            tobiiSupportProp.boolValue = false;
#endif
            return (tobiiSupportProp.boolValue != oldTobiiSupport);
        }

        public static bool ArkitSettingUI(SerializedObject serializedConfiguration) {
            SerializedProperty arkitSupportProp = serializedConfiguration.FindProperty("arkitSupport");

            bool oldArkitSupport = arkitSupportProp.boolValue;
#if UNITY_IOS && UNITY_2019_1_OR_NEWER
            if (!isFaceSupportAvailable || !isArKitSupportAvailable)
                arkitSupportProp.boolValue = false;

            else if (!isArKitAvailable) {
                using (new EditorGUI.DisabledScope(true)) {
                    arkitSupportProp.boolValue = EditorGUILayout.Toggle("ArKit Support", false);
                }
                EditorGUILayout.HelpBox(
                    "Required packages are not installed. " +
                    "Please open the Unity Package Manager and install 'AR Foundation', 'ARKit Face Tracking' and 'XR Legacy Input Helpers'.",
                    MessageType.Warning, true);
                if (GUILayout.Button("Open Package Manager"))
                    EditorApplication.ExecuteMenuItem("Window/Package Manager");
            }
            else
                using (new EditorGUI.DisabledScope(true)) {
                    arkitSupportProp.boolValue = EditorGUILayout.Toggle("ArKit Support", arkitSupportProp.boolValue);
                }
#else
            arkitSupportProp.boolValue = false;
#endif
            return (arkitSupportProp.boolValue != oldArkitSupport);
        }

        public static bool PupilSettingUI(SerializedObject serializedConfiguration) {
            SerializedProperty pupilSupportProp = serializedConfiguration.FindProperty("pupilSupport");

            bool oldPupilSupport = pupilSupportProp.boolValue;
#if UNITY_STANDALONE_WIN
            if (!isPupilSupportAvailable)
                pupilSupportProp.boolValue = false;

            else if (!isPupilAvailable) {
                EditorGUI.BeginDisabledGroup(true);
                pupilSupportProp.boolValue = EditorGUILayout.Toggle("Pupil Labs Support", false);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.HelpBox("Pupil Labs plugin is not available. Please download the plugin using the button below and import them into this project.", MessageType.Warning, true);
                if (GUILayout.Button("Download Pupil Unity Plugin"))
                    Application.OpenURL("https://github.com/pupil-labs/hmd-eyes/releases/tag/v0.5.1");
            }
            else
                pupilSupportProp.boolValue = EditorGUILayout.Toggle("Pupil Labs Support", pupilSupportProp.boolValue);
#else
            pupilSupportProp.boolValue = false;
#endif
            return (pupilSupportProp.boolValue != oldPupilSupport);
        }

        public static bool NeuronSettingUI(SerializedObject serializedConfiguration) {
            SerializedProperty neuronSupportProp = serializedConfiguration.FindProperty("neuronSupport");

            bool oldNeuronSupport = neuronSupportProp.boolValue;
#if UNITY_STANDALONE_WIN || UNITY_WSA_10_0
            if (!isNeuronSupportAvailable)
                neuronSupportProp.boolValue = false;

            else
                neuronSupportProp.boolValue = EditorGUILayout.Toggle("Perception Neuron Support", neuronSupportProp.boolValue);
#else
            neuronSupportProp.boolValue = false;
#endif
            return (neuronSupportProp.boolValue != oldNeuronSupport);
        }

        public static bool Hi5SettingUI(SerializedObject serializedConfiguration) {
            SerializedProperty hi5SupportProp = serializedConfiguration.FindProperty("hi5Support");

            bool oldHi5Support = hi5SupportProp.boolValue;
#if UNITY_STANDALONE_WIN || UNITY_WSA_10_0
            if (!isHi5SupportAvailable)
                hi5SupportProp.boolValue = false;
            //else if (!isHi5Available) {
            //    EditorGUI.BeginDisabledGroup(true);
            //    hi5SupportProp.boolValue = EditorGUILayout.Toggle("Hi5 Support", false);
            //    EditorGUI.EndDisabledGroup();
            //    EditorGUILayout.HelpBox("Hi5 Unity Plugin is not available. Please download the SDK using the button below and import them into this project.", MessageType.Warning, true);
            //    if (GUILayout.Button("Download Hi5 Unity Plugin"))
            //        Application.OpenURL("https://hi5vrglove.com/downloads");
            //}
            else {
                //using (new EditorGUI.DisabledScope(true)) {
                hi5SupportProp.boolValue = EditorGUILayout.Toggle("Hi5 Support", hi5SupportProp.boolValue);
                //}
            }
#else
            hi5SupportProp.boolValue = false;
#endif
            return (hi5SupportProp.boolValue != oldHi5Support);
        }

        public static bool OptitrackSettingUI(SerializedObject serializedConfiguration) {
            SerializedProperty optitrackSupportProp = serializedConfiguration.FindProperty("optitrackSupport");

            bool oldOptitrackSupport = optitrackSupportProp.boolValue;
#if UNITY_STANDALONE_WIN || UNITY_WSA_10_0
            if (!isOptitrackSupportAvailable)
                optitrackSupportProp.boolValue = false;

            else if (!isOptitrackAvailable) {
                using (new EditorGUI.DisabledScope(true)) {
                    optitrackSupportProp.boolValue = EditorGUILayout.Toggle("OptiTrack Support", false);
                }
                EditorGUILayout.HelpBox("OptiTrack Unity plugin is not available. Please download the plugin using the button below and import them into this project.", MessageType.Warning, true);
                if (GUILayout.Button("Download OptiTrack Unity Plugin"))
                    Application.OpenURL("https://optitrack.com/downloads/plugins.html#unity-plugin");
            }
            else {
                using (new EditorGUI.DisabledScope(true)) {
                    optitrackSupportProp.boolValue = EditorGUILayout.Toggle("OptiTrack Support", optitrackSupportProp.boolValue);
                }
            }
#else
            optitrackSupportProp.boolValue = false;
#endif
            return (optitrackSupportProp.boolValue != oldOptitrackSupport);
        }

        public static bool AntilatencySettingUI(SerializedObject serializedConfiguration) {
            SerializedProperty antilatencySupportProp = serializedConfiguration.FindProperty("antilatencySupport");

            bool oldAntilatencySupport = antilatencySupportProp.boolValue;
#if UNITY_STANDALONE_WIN
            if (!isAntilatencySupportAvailable)
                antilatencySupportProp.boolValue = false;
            else if (!isAntilatencyAvailable) {
                EditorGUI.BeginDisabledGroup(true);
                antilatencySupportProp.boolValue = EditorGUILayout.Toggle("Antilatency Support", false);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.HelpBox("Antilatency SDK is not available. Please download the SDK and import it into this project.", MessageType.Warning, true);
            }
            else {
                using (new EditorGUI.DisabledScope(true)) {
                    antilatencySupportProp.boolValue = EditorGUILayout.Toggle("Antilatency Support", antilatencySupportProp.boolValue);
                }
            }
#else
            antilatencySupportProp.boolValue = false;
#endif
            return (antilatencySupportProp.boolValue != oldAntilatencySupport);
        }

        private static bool NetworkingSettingUI(SerializedObject serializedConfiguration) {
            SerializedProperty networkingSupportProp = serializedConfiguration.FindProperty("networkingSupport");
            int oldNetworkingSupport = networkingSupportProp.intValue;
            networkingSupportProp.intValue = (int)(NetworkingSystems)EditorGUILayout.EnumPopup("Networking Support", (NetworkingSystems)networkingSupportProp.intValue);
#if hPHOTON2 && hNW_PHOTON
            bool voiceChanged = NetworkingVoiceSettingUI(serializedConfiguration);
            return voiceChanged || (oldNetworkingSupport != networkingSupportProp.intValue);
#else
            return (oldNetworkingSupport != networkingSupportProp.intValue);
#endif
        }

        private static bool NetworkingVoiceSettingUI(SerializedObject serializedConfiguration) {
            if (isPhotonVoice2Available) {
                SerializedProperty networkingVoiceSupportProp = serializedConfiguration.FindProperty("networkingVoiceSupport");
                bool oldNetworkingVoiceSupport = networkingVoiceSupportProp.boolValue;
                EditorGUI.indentLevel++;
                networkingVoiceSupportProp.boolValue = EditorGUILayout.Toggle("Networking Voice", networkingVoiceSupportProp.boolValue);
                EditorGUI.indentLevel--;
                return (oldNetworkingVoiceSupport != networkingVoiceSupportProp.boolValue);
            }
            else
                return false;
        }

        #endregion

        private static bool IsFileAvailable(string filePath) {
            string path = Application.dataPath + humanoidPath + filePath;
            bool fileAvailable = File.Exists(path);
            return fileAvailable;
        }


        #region Availability

        #region SDKs

        private static bool isOpenVrAvailable {
            get {
#if UNITY_2020_1_OR_NEWER
                return DoesTypeExist("Valve.VR.CVRCompositor");
#else
                return true;
#endif
            }
        }

        private static bool isViveHandTrackingAvailable {
            get {
                return DoesTypeExist("ViveHandTracking.GestureProvider");
            }
        }

        private static bool isViveFaceTrackingAvailable {
            get {
                return DoesTypeExist("ViveSR.anipal.Eye.SRanipal_Eye_Framework");
            }
        }

        private static bool isVrtkAvailable {
            get {
                return DoesTypeExist("VRTK.VRTK_SDKManager");
            }
        }

        private static bool isLeapAvailable {
            get {
                return DoesTypeExist("Leap.Hand");
            }
        }

        private static bool isAstraAvailable {
            get {
                return DoesTypeExist("Astra.Body");
            }
        }

        private static bool isTobiiAvailable {
            get {
                return DoesTypeExist("Tobii.Gaming.TobiiAPI");
            }
        }

        private static bool isArKitAvailable {
            get {
                return IsArkitAvailable();
            }
        }

        private static bool IsArkitAvailable() {
#if UNITY_IOS && UNITY_2019_1_OR_NEWER
            if (Pawn.ConfigurationCheck.packageNameList == null)
                return false;

            bool arFoundationAvailable = Pawn.ConfigurationCheck.packageNameList.Contains("com.unity.xr.arfoundation");
            bool arKitFaceTrackingAvailable = Pawn.ConfigurationCheck.packageNameList.Contains("com.unity.xr.arkit-face-tracking");
            return arFoundationAvailable && arKitFaceTrackingAvailable;
#else
            return false;
#endif
        }

        private static bool isPupilAvailable {
            get {
                return IsPupilAvailable();
            }
        }

        private static bool IsPupilAvailable() {
            string path1 = Application.dataPath + "/pupil_plugin/Scripts/Networking/PupilTools.cs";
            string path2 = Application.dataPath + "/pupil_plugin/Plugins/x86_64/NetMQ.dll";
            return File.Exists(path1) && File.Exists(path2);
        }

        //private static bool isHi5Available {
        //    get {
        //        return DoesTypeExist("HI5.HI5_Source");
        //    }
        //}

        private static bool isOptitrackAvailable {
            get {
                return DoesTypeExist("OptitrackStreamingClient");
            }
        }

        private static bool isAntilatencyAvailable {
            get {
                return DoesTypeExist("Antilatency.Integration.DeviceNetwork");
            }
        }

        private static bool isUnetAvailable {
            get {
                return DoesTypeExist("UnityEngine.Networking.NetworkIdentity");
            }
        }

        private static bool isPhotonPun1Available {
            get {
                return DoesTypeExist("PhotonView");
            }
        }

        private static bool isPhotonPun2Available {
            get {
                return DoesTypeExist("Photon.Pun.PhotonView");
            }
        }

        private static bool isPhotonVoice2Available {
            get {
                return DoesTypeExist("Photon.Voice.PUN.PhotonVoiceView");
            }
        }

        private static bool isMirrorAvailable {
            get {
                string path = Application.dataPath + "/Mirror/Runtime/NetworkBehaviour.cs";
                return File.Exists(path);
            }
        }

        private static bool isPhotonBoltAvailable {
            get {
                string path = Application.dataPath + "/Photon/PhotonBolt/project.json";
                return File.Exists(path);
            }
        }


        #endregion SDKs

        #region Support

        private static bool isOpenVrSupportAvailable {
            get {
                return DoesTypeExist("Passer.Tracking.OpenVR");
            }
        }

        private static bool isViveTrackerSupportAvailable {
            get {
                return DoesTypeExist("Passer.Tracking.ViveTrackerComponent");
            }
        }

        private static bool isOculusSupportAvailable {
            get {
                return DoesTypeExist("Passer.Tracking.OculusTracker");
            }
        }

        private static bool isOculusHandSupportAvailable {
            get {
                return DoesTypeExist("Passer.Tracking.OculusHandSkeleton");
            }
        }

#if UNITY_WSA_10_0
        private static bool isWindowsMrSupportAvailable {
            get {
                return DoesTypeExist("Passer.Humanoid.Tracking.WindowsMRDevice");
            }
        }
#endif
        private static bool isVrtkSupportAvailable {
            get {
                return DoesTypeExist("Passer.Humanoid.VrtkTracker");
            }
        }

        private static bool isLeapSupportAvailable {
            get {
#if UNITY_STANDALONE_WIN
                return DoesTypeExist("Passer.Tracking.LeapMotionCamera");
#else
                return false;
#endif
            }
        }

        private static bool isAstraSupportAvailable {
            get {
#if UNITY_STANDALONE_WIN
                return DoesTypeExist("Passer.Tracking.AstraDevice");
#else
                return false;
#endif
            }
        }

        private static bool isKinect1SupportAvailable {
            get {
                return DoesTypeExist("Passer.Humanoid.Kinect1Tracker");
            }
        }

        private static bool isKinect2SupportAvailable {
            get {
                return DoesTypeExist("Passer.Humanoid.Kinect2Tracker");
            }
        }

        private static bool isKinect4SupportAvailable {
            get {
                return DoesTypeExist("Passer.Tracking.AzureKinect");
            }
        }

        private static bool isRealsenseSupportAvailable {
            get {
                return DoesTypeExist("Passer.Humanoid.RealsenseTracker");
            }
        }

        private static bool isHydraSupportAvailable {
            get {
                return DoesTypeExist("Passer.Tracking.HydraBaseStation");
            }
        }

        private static bool isHi5SupportAvailable {
            get {
                return DoesTypeExist("Passer.Humanoid.Hi5Tracker");
            }
        }

        private static bool isTobiiSupportAvailable {
            get {
                return DoesTypeExist("Passer.Tracking.TobiiDevice");
            }
        }

        private static bool isArKitSupportAvailable {
            get {
                return DoesTypeExist("Passer.Humanoid.ArKitTracker");
            }
        }

        private static bool isPupilSupportAvailable {
            get {
                return DoesTypeExist("Passer.Tracking.Pupil.Device");
            }
        }

        private static bool isNeuronSupportAvailable {
            get {
                return DoesTypeExist("Passer.Humanoid.NeuronTracker");
            }
        }

        private static bool isOptitrackSupportAvailable {
            get {
                return DoesTypeExist("Passer.Tracking.OptitrackDevice");
            }
        }

        private static bool isAntilatencySupportAvailable {
            get {
#if UNITY_STANDALONE_WIN
                return DoesTypeExist("Passer.Tracking.Antilatency");
#else
                return false;
#endif
            }
        }

        private static bool isFaceSupportAvailable {
            get {
                return DoesTypeExist("Passer.Humanoid.FaceTarget");
            }
        }

        #endregion Support

        #endregion

        #region Extension Checks   

        public static void CheckExtensions(Configuration configuration) {
            GlobalDefine("pHUMANOID3");

            CheckExtensionUnityXR();

            configuration.openVRSupport = CheckExtensionOpenVR(configuration);
            configuration.viveTrackerSupport = CheckExtensionViveTracker(configuration);
            configuration.oculusSupport = CheckExtensionOculus(configuration);
            configuration.windowsMRSupport = CheckExtensionWindowsMR(configuration);
            configuration.vrtkSupport = CheckExtensionVRTK(configuration);
            configuration.neuronSupport = CheckExtensionNeuron(configuration);
            configuration.hi5Support = CheckExtensionHi5(configuration);
            configuration.realsenseSupport = CheckExtensionRealsense(configuration);
            configuration.leapSupport = CheckExtensionLeap(configuration);
            configuration.kinect1Support = CheckExtensionKinect1(configuration);
            configuration.kinect2Support = CheckExtensionKinect2(configuration);
            configuration.kinect4Support = CheckExtensionKinect4(configuration);
            configuration.astraSupport = CheckExtensionAstra(configuration);
            configuration.hydraSupport = CheckExtensionHydra(configuration);
            configuration.tobiiSupport = CheckExtensionTobii(configuration);
            configuration.arkitSupport = CheckExtensionArkit(configuration);
            configuration.pupilSupport = CheckExtensionPupil(configuration);
            configuration.optitrackSupport = CheckExtensionOptitrack(configuration);
            configuration.antilatencySupport = CheckExtensionAntilatency(configuration);

            CheckExtensionNetworking(configuration);
            CheckFaceTracking(configuration);
        }

        public static bool CheckExtensionOpenVR(Configuration configuration) {
            bool available = isOpenVrAvailable;
            bool enabled = configuration.openVRSupport;
            CheckExtension(isViveHandTrackingAvailable, "hVIVEHAND");
            CheckExtension(isViveFaceTrackingAvailable, "hVIVEFACE");
            return CheckExtension(available & enabled, openVRPath, "hOPENVR");
        }

        public static bool CheckExtensionViveTracker(Configuration configuration) {
            bool available = isViveTrackerSupportAvailable;
            bool enabled = configuration.viveTrackerSupport;
            CheckExtension(available && enabled, "hVIVETRACKER");
            return available && enabled;
        }

        public static bool CheckExtensionOculus(Configuration configuration) {
            bool enabled = configuration.oculusSupport;
            CheckExtension(isOculusHandSupportAvailable, "hOCHAND");
            return CheckExtension(enabled, oculusPath, "hOCULUS");
        }

        public static bool CheckExtensionWindowsMR(Configuration configuration) {
            bool enabled = configuration.windowsMRSupport;
            return CheckExtension(enabled, windowsMRPath, "hWINDOWSMR");
        }

        public static bool CheckExtensionVRTK(Configuration configuration) {
            bool enabled = configuration.vrtkSupport && isVrtkAvailable;
            CheckExtension(enabled, "hVRTK");
            return enabled;
        }

        public static bool CheckExtensionNeuron(Configuration configuration) {
            bool enabled = configuration.neuronSupport;
            return CheckExtension(enabled, neuronPath, "hNEURON");
        }

        public static bool CheckExtensionHi5(Configuration configuration) {
            bool available = configuration.hi5Support;
            CheckExtension(available, "hHI5");
            configuration.hi5Support = available;
            return available;
        }

        public static bool CheckExtensionRealsense(Configuration configuration) {
            bool enabled = configuration.realsenseSupport;
            return CheckExtension(enabled, realsensePath, "hREALSENSE");
        }

        public static bool CheckExtensionLeap(Configuration configuration) {
            bool available = isLeapAvailable && isLeapSupportAvailable;
            CheckExtension(available, "hLEAP");
            configuration.leapSupport = available;
            return available;
        }

        public static bool CheckExtensionKinect1(Configuration configuration) {
            bool enabled = configuration.kinect1Support;
            return CheckExtension(enabled, kinect1Path, "hKINECT1");
        }

        public static bool CheckExtensionKinect2(Configuration configuration) {
            bool enabled = configuration.kinect2Support;
            return CheckExtension(enabled, kinect2Path, "hKINECT2");
        }

        public static bool CheckExtensionKinect4(Configuration configuration) {
            bool enabled = configuration.kinect4Support;
            return CheckExtension(enabled, kinect4Path, "hKINECT4");
        }

        public static bool CheckExtensionAstra(Configuration configuration) {
            bool available = isAstraAvailable && isAstraSupportAvailable;
            CheckExtension(available, "hORBBEC");
            configuration.astraSupport = available;
            return available;
        }

        public static bool CheckExtensionHydra(Configuration configuration) {
            bool enabled = configuration.hydraSupport;
            return CheckExtension(enabled, hydraPath, "hHYDRA");
        }

        public static bool CheckExtensionTobii(Configuration configuration) {
            bool available = isTobiiAvailable && isTobiiSupportAvailable;
            CheckExtension(available, "hTOBII");
            configuration.tobiiSupport = available;
            return available;
        }

        public static bool CheckExtensionArkit(Configuration configuration) {
#if UNITY_2019_1_OR_NEWER
            if (Pawn.ConfigurationCheck.packageNameList == null)
                return configuration.arkitSupport;
            bool available = isArKitSupportAvailable && isArKitAvailable;
            CheckExtension(available, arkitPath, "hARKIT");
            configuration.arkitSupport = available;
            return available;
#else
            return false;
#endif
        }

        public static bool CheckExtensionPupil(Configuration configuration) {
            bool enabled = configuration.pupilSupport && isPupilAvailable;
            return CheckExtension(enabled, pupilPath, "hPUPIL");
        }

        public static bool CheckExtensionOptitrack(Configuration configuration) {
            bool available = isOptitrackAvailable && isOptitrackSupportAvailable;
            CheckExtension(available, "hOPTITRACK");
            configuration.optitrackSupport = available;
            return available;
        }

        public static bool CheckExtensionAntilatency(Configuration configuration) {
            bool available = isAntilatencyAvailable && isAntilatencySupportAvailable;
            CheckExtension(available, "hANTILATENCY");
            configuration.antilatencySupport = available;
            return available;
        }

        private static void CheckExtensionNetworking(Configuration configuration) {
            if (isPhotonPun2Available) {
                GlobalDefine("hPHOTON2");
                GlobalUndefine("hPHOTON1");
                //Debug.Log(isPhotonVoice2Available);
                //if (isPhotonVoice2Available)
                //    GlobalDefine("hPUNVOICE2");
                //else
                //    GlobalUndefine("hPUNVOICE2");
            }
            else if (isPhotonPun1Available) {
                GlobalDefine("hPHOTON1");
                GlobalUndefine("hPHOTON2");
            }
            else {
                GlobalUndefine("hPHOTON1");
                GlobalUndefine("hPHOTON2");
            }

            if (isMirrorAvailable)
                GlobalDefine("hMIRROR");
            else
                GlobalUndefine("hMIRROR");

            if (isPhotonBoltAvailable)
                GlobalDefine("hBOLT");
            else
                GlobalUndefine("hBOLT");


#if !UNITY_2019_1_OR_NEWER
            CheckExtension(isUnetAvailable, "hUNET");

            if (configuration.networkingSupport == NetworkingSystems.UnityNetworking)
                GlobalDefine("hNW_UNET");
            else
                GlobalUndefine("hNW_UNET");
#endif
#if hPHOTON1 || hPHOTON2
            if (configuration.networkingSupport == NetworkingSystems.None)
                configuration.networkingSupport = NetworkingSystems.PhotonNetworking;  
            if (configuration.networkingSupport == NetworkingSystems.PhotonNetworking)
                GlobalDefine("hNW_PHOTON");
            else
                GlobalUndefine("hNW_PHOTON");
#endif
#if hMIRROR
            if (configuration.networkingSupport == NetworkingSystems.None)
                configuration.networkingSupport = NetworkingSystems.MirrorNetworking;  
            if (configuration.networkingSupport == NetworkingSystems.MirrorNetworking)
                GlobalDefine("hNW_MIRROR");
            else
                GlobalUndefine("hNW_MIRROR");
#endif
#if hBOLT
            if (configuration.networkingSupport == NetworkingSystems.None)
                configuration.networkingSupport = NetworkingSystems.PhotonBolt;  
            if (configuration.networkingSupport == NetworkingSystems.PhotonBolt)
                GlobalDefine("hNW_BOLT");
            else
                GlobalUndefine("hNW_BOLT");
#endif
#if hPHOTON2 && hNW_PHOTON
            if (configuration.networkingVoiceSupport)
                GlobalDefine("hPUNVOICE2");
            else
#endif
            GlobalUndefine("hPUNVOICE2");
        }

        private static void CheckFaceTracking(Configuration configuration) {
            if (IsFileAvailable(facePath)) {
                GlobalDefine("hFACE");
            }
            else {
                GlobalUndefine("hFACE");
            }
        }

        public static bool CheckExtension(bool enabled, string filePath, string define) {
            if (enabled) {
                if (IsFileAvailable(filePath)) {
                    GlobalDefine(define);
                    return true;
                }
                else {
                    GlobalUndefine(define);
                    return false;
                }

            }
            else {
                GlobalUndefine(define);
                return false;
            }
        }

        public static bool CheckExtension(string filePath, string define) {
            if (IsFileAvailable(filePath)) {
                GlobalDefine(define);
                return true;
            }
            else {
                GlobalUndefine(define);
                return false;
            }
        }
        public static Configuration LoadConfiguration(string configurationName) {
            string[] foundAssets = AssetDatabase.FindAssets(configurationName + " t:Configuration");
            if (foundAssets.Length == 0)
                return null;

            string path = AssetDatabase.GUIDToAssetPath(foundAssets[0]);
            Configuration configuration = AssetDatabase.LoadAssetAtPath<Configuration>(path);
            return configuration;
        }

        #endregion
    }

    public static class CustomAssetUtility {
        public static void CreateAsset<T>() where T : ScriptableObject {
            T asset = ScriptableObject.CreateInstance<T>();

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (path == "") {
                path = "Assets";
            }
            else if (Path.GetExtension(path) != "") {
                path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
            }

            string assetTypeName = typeof(T).ToString();
            int dotIndex = assetTypeName.LastIndexOf('.');
            if (dotIndex >= 0)
                assetTypeName = assetTypeName.Substring(dotIndex + 1); // leave just text behind '.'
            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/New " + assetTypeName + ".asset");

            AssetDatabase.CreateAsset(asset, assetPathAndName);

            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }
    }

}