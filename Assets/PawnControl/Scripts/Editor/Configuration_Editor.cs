using System;
using UnityEngine;
using UnityEditor;
#if UNITY_2019_1_OR_NEWER
using System.Collections.Generic;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
#endif

using UnityEngine.XR;

namespace Passer.Pawn {

    [InitializeOnLoad]
    public class ConfigurationCheck {
        static ConfigurationCheck() {
#if UNITY_2019_1_OR_NEWER
            RetrievePackageList();
#else
            Configuration_Editor.CheckExtensionLegacyXR();
#endif
        }

#if UNITY_2019_1_OR_NEWER
        protected static ListRequest request;
        public static List<string> packageNameList;

        public static void RetrievePackageList() {
            request = Client.List();    // List packages installed for the Project
            EditorApplication.update += Progress;
        }

        public static void Progress() {
            if (request.IsCompleted) {
                if (request.Status == StatusCode.Success) {
                    packageNameList = new List<string>();
                    foreach (UnityEditor.PackageManager.PackageInfo package in request.Result)
                        packageNameList.Add(package.name);

                    Configuration_Editor.CheckExtensionUnityXR();
                    Configuration_Editor.CheckExtensionLegacyXR();
                }
                else if (request.Status >= StatusCode.Failure)
                    Debug.Log(request.Error.message);

                EditorApplication.update -= Progress;
            }
        }
#endif
    }

    public class Configuration_Editor : Editor {

        #region Extension Checks

        public static void CheckExtensionLegacyXR() {
            CheckExtension(isLegacyXRAvailable && isLegacyXRSupportAvailable, "hLEGACYXR");
        }

        public static void CheckExtensionUnityXR() {
#if UNITY_2019_3_OR_NEWER
            if (ConfigurationCheck.packageNameList != null)
                CheckExtension(isUnityXRAvailable, "pUNITYXR");
#else
            GlobalUndefine("pUNITYXR");
#endif
        }

        protected static void CheckExtension(bool enabled, string define) {
            if (enabled)
                GlobalDefine(define);
            else
                GlobalUndefine(define);
        }

        public static void GlobalDefine(string name) {
            //Debug.Log("Define " + name);
            string scriptDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            if (!scriptDefines.Contains(name)) {
                string newScriptDefines = scriptDefines + " " + name;
                if (EditorUserBuildSettings.selectedBuildTargetGroup != 0)
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, newScriptDefines);
            }
        }

        public static void GlobalUndefine(string name) {
            //Debug.Log("Undefine " + name);
            string scriptDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            if (scriptDefines.Contains(name)) {
                int playMakerIndex = scriptDefines.IndexOf(name);
                string newScriptDefines = scriptDefines.Remove(playMakerIndex, name.Length);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, newScriptDefines);
            }

        }

        #endregion Extension Checks

        #region Availability

        public static bool DoesTypeExist(string className) {
            System.Reflection.Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (System.Reflection.Assembly assembly in assemblies) {
                if (assembly.GetType(className) != null)
                    return true;
            }
            return false;
        }


        #region SDKs

        protected static bool isLegacyXRAvailable {
            get {
#if UNITY_2020_1_OR_NEWER || pUNITYXR
                return false;
#else
                return !isUnityXRAvailable;
#endif
            }
        }
        protected static bool isUnityXRAvailable {
            get {
#if UNITY_2019_1_OR_NEWER
                if (ConfigurationCheck.packageNameList == null)
                    return false;
                if (ConfigurationCheck.packageNameList.Contains("com.valvesoftware.unity.openvr"))
                    return true;
                if (ConfigurationCheck.packageNameList.Contains("com.unity.xr.management"))
                    return true;
                else if (ConfigurationCheck.packageNameList.Contains("com.unity.xr.openxr"))
                    // Somehow management is no longer available when OpenXR is used
                    return true;
                else
                    return false;
#else
                return DoesTypeExist("UnityEngine.XR.InputDevice");
#endif
            }
        }

        #endregion SDKs

        #region Support

        private static bool isLegacyXRSupportAvailable {
            get {
                return DoesTypeExist("Passer.Humanoid.UnityVRTracker");
            }
        }

        #endregion

        #endregion Availability
    }
}