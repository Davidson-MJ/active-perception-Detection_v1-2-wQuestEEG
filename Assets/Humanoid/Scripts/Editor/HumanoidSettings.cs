using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Passer.Humanoid {
    [System.Serializable]
    public class HumanoidSettings : ScriptableObject {
        public static bool help = false;
#if UNITY_2018_3_OR_NEWER
        public const string settingsPath = "Assets/Humanoid/HumanoidSettings.asset";

        public Configuration configuration;

        internal static HumanoidSettings GetOrCreateSettings() {
            string humanoidPath = Configuration_Editor.FindHumanoidFolder();
            humanoidPath = humanoidPath.Substring(0, humanoidPath.Length - 1); // strip last /
            humanoidPath = humanoidPath.Substring(0, humanoidPath.LastIndexOf('/') + 1); // strip Scripts;
            string settingsPath = "Assets" + humanoidPath + "HumanoidSettings.asset";
            HumanoidSettings settings = AssetDatabase.LoadAssetAtPath<HumanoidSettings>(settingsPath);
            SerializedObject serializedSettings = new SerializedObject(settings);
            if (settings == null) {
                settings = CreateInstance<HumanoidSettings>();

                AssetDatabase.CreateAsset(settings, settingsPath);
            }
            if (settings.configuration == null) {
                string configurationString = EditorPrefs.GetString("HumanoidConfigurationKey", "DefaultConfiguration");
                Configuration configuration = Configuration_Editor.LoadConfiguration(configurationString);
                if (configuration == null) {
                    configurationString = "DefaultConfiguration";
                    Configuration_Editor.LoadConfiguration(configurationString);
                    if (configuration == null) {
                        Debug.Log("Created new Default Configuration");
                        // Create new Default Configuration
                        configuration = CreateInstance<Configuration>();
                        string path = "Assets" + humanoidPath + configurationString + ".asset";
                        AssetDatabase.CreateAsset(configuration, path);
                        AssetDatabase.SaveAssets();
                    }
                }
                SerializedProperty configurationProp = serializedSettings.FindProperty("configuration");
                configurationProp.objectReferenceValue = configuration;
                EditorUtility.SetDirty(settings);
            }
            serializedSettings.ApplyModifiedProperties();
            return (HumanoidSettings)serializedSettings.targetObject;//settings;
        }

        internal static SerializedObject GetOrCreateSerializedSettings() {
            string humanoidPath = Configuration_Editor.FindHumanoidFolder();
            humanoidPath = humanoidPath.Substring(0, humanoidPath.Length - 1); // strip last /
            humanoidPath = humanoidPath.Substring(0, humanoidPath.LastIndexOf('/') + 1); // strip Scripts;
            string settingsPath = "Assets" + humanoidPath + "HumanoidSettings.asset";
            HumanoidSettings settings = AssetDatabase.LoadAssetAtPath<HumanoidSettings>(settingsPath);

            if (settings == null) {
                Debug.Log("Created new Settings");
                settings = CreateInstance<HumanoidSettings>();

                AssetDatabase.CreateAsset(settings, settingsPath);
            }
            if (settings.configuration == null) {
                Debug.Log("Settings Configuration is not set");
                string configurationString = "DefaultConfiguration";
                Configuration configuration = Configuration_Editor.LoadConfiguration(configurationString);
                if (configuration == null) {
                    configurationString = "DefaultConfiguration";
                    Configuration_Editor.LoadConfiguration(configurationString);
                    if (configuration == null) {
                        Debug.Log("Created new Default Configuration");
                        // Create new Default Configuration
                        configuration = CreateInstance<Configuration>();
                        string path = "Assets" + humanoidPath + configurationString + ".asset";
                        AssetDatabase.CreateAsset(configuration, path);
                        AssetDatabase.SaveAssets();
                    }
                }
                settings.configuration = configuration;
            }
            SerializedObject serializedSettings = new SerializedObject(settings);
            return serializedSettings;
        }
#endif
    }

    static class HumanoidSettingsIMGUIRegister {
#if UNITY_2018_3_OR_NEWER
        public static bool reload;

        [SettingsProvider]
        public static SettingsProvider CreateHumanoidSettingsProvider() {
            var provider = new SettingsProvider("Preferences/HumanoidControlSettings", SettingsScope.User) {
                label = "Humanoid Control",
                guiHandler = (searchContext) => {

                    SerializedObject serializedSettings = HumanoidSettings.GetOrCreateSerializedSettings();

                    SerializedProperty configurationProp = serializedSettings.FindProperty("configuration");
                    Configuration oldConfiguration = (Configuration)configurationProp.objectReferenceValue;

                    configurationProp.objectReferenceValue = EditorGUILayout.ObjectField("Configuration", configurationProp.objectReferenceValue, typeof(Configuration), false);
                    SerializedObject serializedConfiguration = new SerializedObject(configurationProp.objectReferenceValue);

                    bool anyChanged = false;
                    anyChanged |= (configurationProp.objectReferenceValue != oldConfiguration);
                    anyChanged |= Configuration_Editor.ConfigurationGUI(serializedConfiguration);
                    serializedConfiguration.ApplyModifiedProperties();
                    serializedSettings.ApplyModifiedProperties();

                    Configuration_Editor.CheckExtensions((Configuration)configurationProp.objectReferenceValue);

                    if (reload) {
                        reload = false;
#if hUNET
                        OnLoadHumanoidPlayerUnet.CheckHumanoidPlayer();
#endif
#if hPHOTON1 || hPHOTON2
                        OnLoadHumanoidPlayerPun.CheckHumanoidPlayer();
#endif
#if hBOLT
                        OnLoadHumanoidPlayerBolt.CheckHumanoidPlayer();
#endif
#if hMIRROR
                        OnLoadHumanoidPlayerMirror.CheckHumanoidPlayer();
#endif
                    }
                },
                keywords = new HashSet<string>(
                    new[] { "Humanoid", "Oculus", "SteamVR" }
                    )
            };
            return provider;
        }
#endif

    }

}