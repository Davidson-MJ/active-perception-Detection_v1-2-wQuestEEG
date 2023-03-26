using UnityEngine;

namespace Passer.Humanoid {

    [CreateAssetMenu(menuName = "Humanoid/Configuration", fileName = "HumanoidConfiguration", order = 100)]
    [System.Serializable]
    public class Configuration : ScriptableObject {
        public bool openVRSupport = false;
        public bool viveTrackerSupport = false;
        public bool oculusSupport = true;
        public bool windowsMRSupport = true;
        public bool vrtkSupport = true;
        public bool neuronSupport;
        public bool hi5Support;
        public bool realsenseSupport;
        public bool leapSupport;
        public bool kinect1Support;
        public bool kinect2Support;
        public bool kinect4Support;
        public bool astraSupport;
        public bool hydraSupport;
        public bool arkitSupport;
        public bool tobiiSupport;
        public bool optitrackSupport;
        public bool pupilSupport;
        public bool antilatencySupport;

        public NetworkingSystems networkingSupport;
        public bool networkingVoiceSupport;

        public string humanoidSceneName;

        public static Configuration Load(string configurationName) {
#if UNITY_EDITOR
            string[] foundAssets = UnityEditor.AssetDatabase.FindAssets(configurationName + " t:Configuration");
            if (foundAssets.Length == 0)
                return null;

            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(foundAssets[0]);
            Configuration configuration = UnityEditor.AssetDatabase.LoadAssetAtPath<Configuration>(path);
            return configuration;
#else
            return null;
#endif
        }
    }
}