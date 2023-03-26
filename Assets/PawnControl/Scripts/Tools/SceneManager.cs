using UnityEngine;
#if hNW_UNET
using UnityEngine.Networking;
#endif
#if hPHOTON2
using Photon.Pun;
#endif

namespace Passer {

    ///<summary>The scene manager synchronizes scene changes with humanoids across a network.</summary>
    [HelpURLAttribute("https://passervr.com/documentation/humanoid-control/tools/scene-manager/")]
#if hNW_PHOTON
    [RequireComponent(typeof(PhotonView))]
#if hPHOTON2
    public class SceneManager : MonoBehaviourPunCallbacks 
#else
    public class SceneManager : Photon.MonoBehaviour 
#endif
#elif hNW_UNET
#pragma warning disable 0618
    [RequireComponent(typeof(NetworkIdentity))]
    public class SceneManager : NetworkBehaviour 
#pragma warning restore 0618
#else
    public class SceneManager : MonoBehaviour 
#endif
    {
        /// <summary>The index of the current scene.</summary>
        public int currentScene = 0;

#if hNW_UNET
#pragma warning disable 0618
        private NetworkManager nwManager;
#pragma warning restore 0618
#endif
        /// <summary>The list of scenes from the Build Settings.</summary>
        public string[] sceneNames;
        private string[] staticSceneNames;

        /// <summary>Will prevent the scene manager from being destroyed when the scene changes.</summary>
        public bool dontDestroyOnLoad = false;

        protected virtual void Awake() {
            if (dontDestroyOnLoad)
                DontDestroyOnLoad(this.transform.root);

            // This is needed to sync all Scene Managers
            // Cannot use static sceneNames directly,
            // because somehow it gets reset to null when Don't Destroy on Load is enabled.
            staticSceneNames = sceneNames;

            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            currentScene = scene.buildIndex;
        }

        /// <summary>Load the scene and causes a scene change.</summary>
        /// <param name="sceneId">The index of the new scene in the list of scenes</param>
        public void LoadScene(int sceneId) {
            if (staticSceneNames == null || sceneId < 0 || sceneId >= staticSceneNames.Length)
                return;

#if hNW_PHOTON
#if hPHOTON2
            PhotonNetwork.AutomaticallySyncScene = true;
#else
            PhotonNetwork.automaticallySyncScene = true;
#endif
            PhotonNetwork.LoadLevel(staticSceneNames[sceneId]);
#elif hNW_UNET
#pragma warning disable 0618
            if (nwManager == null)
                nwManager = FindObjectOfType<NetworkManager>();
            if (nwManager == null) {
                Debug.LogError("Cannot change scene without an Network Manager for Unity Networking");
                return;
            }
            nwManager.ServerChangeScene(staticSceneNames[sceneId]);
#pragma warning restore 0618
#else
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneId);
#endif
            currentScene = sceneId;
        }

        /// <summary>Changes the scene to the next scene in the list</summary>
        /// This will wrap around when the last scene in the list has been reached.
        public void NextScene() {
            currentScene = mod(currentScene + 1, staticSceneNames.Length);
            LoadScene(currentScene);
        }

        /// <summary>Changes the scene to the previous scene in the list</summary>
        /// This will wrap aorund when the first scene in the list has been reached.
        public void PreviousScene() {
            currentScene = mod(currentScene + 1, staticSceneNames.Length);
            LoadScene(currentScene);
        }

#if hNW_PHOTON
        [PunRPC]
        private void RpcLoadScene(string sceneName) {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
#elif hNW_UNET
#pragma warning disable 0618
        [Command] // @ server
        private void CmdLoadScene(string sceneName) {
            RpcClientLoadScene(sceneName);
        }

        [ClientRpc] // @ remote client
        private void RpcClientLoadScene(string sceneName) {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
#pragma warning restore 0618
#endif

        public static int mod(int k, int n) {
            k %= n;
            return (k < 0) ? k + n : k;
        }
    }
}