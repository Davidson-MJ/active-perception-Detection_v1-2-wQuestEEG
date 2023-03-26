using UnityEngine;
using UnityEditor;

namespace Passer.Humanoid {

    public class OnLoadHumanoidPlayerMirror {

        public static void CheckHumanoidPlayer() {
#if hMIRROR
            if (Application.isPlaying)
                return;

		   
            string prefabPath = OnLoadHumanoidPlayer.GetHumanoidPlayerPrefabPath();

            using (PrefabUtility.EditPrefabContentsScope editingScope = new PrefabUtility.EditPrefabContentsScope(prefabPath)) {
                GameObject playerPrefab = editingScope.prefabContentsRoot;
																							  

                Mirror.NetworkManager nwManager = Object.FindObjectOfType<Mirror.NetworkManager>();
#if hNW_MIRROR
                if (nwManager == null) {
                    NetworkingStarter nwStarter = Object.FindObjectOfType<NetworkingStarter>();
                    if (nwStarter != null) {
                        nwManager = nwStarter.gameObject.AddComponent<Mirror.NetworkManager>();
                    }
                }
			 

                if (nwManager != null && nwManager.playerPrefab == null) {
                    nwManager.playerPrefab = (GameObject)Resources.Load("HumanoidPlayer");
								  
                }

                if (playerPrefab != null) {
                    Mirror.NetworkIdentity nwId = playerPrefab.GetComponent<Mirror.NetworkIdentity>();
                    if (nwId == null) {
                        nwId = playerPrefab.AddComponent<Mirror.NetworkIdentity>();
                    }
                }
			 
#else
                if (nwManager != null) {
                    Object.DestroyImmediate(nwManager, true);
								  
                }

                Mirror.Transport transport = Object.FindObjectOfType<Mirror.Transport>();
                if (transport != null) {
                    Object.DestroyImmediate(transport, true);
								  
                }

                if (playerPrefab != null) {
                    Mirror.NetworkIdentity nwId = playerPrefab.GetComponent<Mirror.NetworkIdentity>();
                    if (nwId != null) {
                        Object.DestroyImmediate(nwId, true);
                    }
                }
#endif
            }
#endif



        }

    }
}
