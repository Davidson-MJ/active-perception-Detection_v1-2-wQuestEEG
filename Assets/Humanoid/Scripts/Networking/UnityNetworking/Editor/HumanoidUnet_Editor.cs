using UnityEngine;
#if !UNITY_2019_1_OR_NEWER
using UnityEngine.Networking;
#endif

namespace Passer.Humanoid {

    public class OnLoadHumanoidPlayerUnet {

        public static void CheckHumanoidPlayer() {
#if !UNITY_2019_1_OR_NEWER
            string prefabPath = OnLoadHumanoidPlayer.GetHumanoidPlayerPrefabPath();
            GameObject playerPrefab = OnLoadHumanoidPlayer.GetHumanoidPlayerPrefab(prefabPath);

#pragma warning disable 0618
            NetworkManager nwManager = Object.FindObjectOfType<NetworkManager>();
#if hNW_UNET
            if (nwManager == null) {
                NetworkingStarter nwStarter = Object.FindObjectOfType<NetworkingStarter>();
                if (nwStarter != null)
                    nwManager = nwStarter.gameObject.AddComponent<NetworkManager>();
            }

            if (nwManager != null && nwManager.playerPrefab == null)
                nwManager.playerPrefab = (GameObject)Resources.Load("HumanoidPlayer");

            if (playerPrefab != null) {
                NetworkIdentity nwId = playerPrefab.GetComponent<NetworkIdentity>();
                if (nwId == null)
                    nwId = playerPrefab.AddComponent<NetworkIdentity>();
            }
#else
            if (nwManager != null)
                Object.DestroyImmediate(nwManager, true);

            if (playerPrefab != null) {
                NetworkIdentity nwId = playerPrefab.GetComponent<NetworkIdentity>();
                if (nwId != null)
                    Object.DestroyImmediate(nwId, true);
            }
#endif
#pragma warning restore 0618
            OnLoadHumanoidPlayer.UpdateHumanoidPrefab(playerPrefab, prefabPath);
#endif
        }
    }
}
