using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
#if !UNITY_2019_1_OR_NEWER
using UnityEngine.Networking;
#endif


namespace Passer.Humanoid {

    [InitializeOnLoad]
    public class OnLoadHumanoidPlayer {
        static OnLoadHumanoidPlayer() {
#if !UNITY_2018_3_OR_NEWER
            // Direct updating of the Prefabs is not possible (Unity Crash) in 2018.3+
            // so we signal that the HumanoidSettings should handle this.
            // This does not work in pre-2018.3, because that does not have a SettingsProvider

            OnLoadHumanoidPlayerUnet.CheckHumanoidPlayer();
            OnLoadHumanoidPlayerPun.CheckHumanoidPlayer();
            OnLoadHumanoidPlayerBolt.CheckHumanoidPlayer();
            OnLoadHumanoidPlayerMirror.CheckHumanoidPlayer();
#else
            HumanoidSettingsIMGUIRegister.reload = true;
#endif
        }

        public static string GetHumanoidPlayerPrefabPath() {
            string humanoidPath = Configuration_Editor.FindHumanoidFolder();
            string prefabPathWithoutScripts = humanoidPath.Substring(0, humanoidPath.Length - 8);
            string prefabPath = "Assets" + prefabPathWithoutScripts + "Prefabs/Networking/Resources/HumanoidPlayer.prefab";
            return prefabPath;
        }

        public static GameObject GetHumanoidPlayerPrefab(string prefabPath) {
#if UNITY_2018_3_OR_NEWER
            GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            return prefab;
#else
            GameObject prefab = (GameObject)Resources.Load("HumanoidPlayer");
            return prefab;
#endif
        }

        public static void UpdateHumanoidPrefab(GameObject prefab, string prefabPath) {
#if UNITY_2018_3_OR_NEWER
            if (!Application.isPlaying) {
                if (Application.isFocused) {
                    //Debug.Log("delaying save " + prefab);
                    HumanoidPlayer_Editor.prefabsToSave.Push(prefab);
                    HumanoidPlayer_Editor.prefabPaths.Push(prefabPath);
                    EditorApplication.delayCall += HumanoidPlayer_Editor.DelayedSave;
                }
                else {
                    //Debug.Log("updating " + prefab);
                    PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
                    PrefabUtility.UnloadPrefabContents(prefab);
                }
            }
#endif
        }

    }

    [CustomEditor(typeof(HumanoidPlayer))]
    public class HumanoidPlayer_Editor : HumanoidNetworking_Editor {
#if UNITY_2018_3_OR_NEWER

        public static Stack<GameObject> prefabsToSave = new Stack<GameObject>();
        public static Stack<string> prefabPaths = new Stack<string>();

        public static void DelayedSave() {
            if (Application.isPlaying)
                return;

            if (prefabsToSave.Count > 0) {
                GameObject prefab = prefabsToSave.Pop();
                //Debug.Log("Delayed save of prefab " + prefab);
                string path = prefabPaths.Pop();
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }
#endif

#if hNW_UNET
        public override void OnInspectorGUI() {
            serializedObject.Update();

            SendRateInspector();
            DebugLevelInspector();
            SmoothingInspector();
            SyncFingerSwingInspector();
            CreateLocalRemotesInspector();
            SyncTrackingInspector();

            serializedObject.ApplyModifiedProperties();
        }
#endif
    }
}