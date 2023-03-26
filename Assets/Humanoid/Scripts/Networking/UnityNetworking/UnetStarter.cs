using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;

namespace Passer {

#if hNW_UNET && !UNITY_2019_1_OR_NEWER
#pragma warning disable 0618
    public class UnetStarter : INetworkingStarter {
        private string roomName;
        private int gameVersion;

        bool matchCreated;
        private NetworkManager networkManager;
        private NetworkMatch networkMatch;

        GameObject INetworkingStarter.GetHumanoidPrefab() {
            GameObject humanoidPrefab = Resources.Load<GameObject>("HumanoidPlayer");
            return humanoidPrefab;
        }

        void INetworkingStarter.StartHost(NetworkingStarter nwStarter) {
            Debug.Log("start Unet Host");
            NetworkManager networkManager = nwStarter.GetComponent<NetworkManager>();
            if (networkManager == null) {
                Debug.LogError("Could not start host: NetworkManager is missing");
                return;
            }

            networkManager.StartHost();
        }

        void INetworkingStarter.StartClient(NetworkingStarter nwStarter) {
            Debug.Log("start Unet Client");
            NetworkManager networkManager = nwStarter.GetComponent<NetworkManager>();
            NetworkClient nwClient = networkManager.StartClient();
            nwClient.Connect(nwStarter.serverIpAddress, networkManager.networkPort);
        }

        public static void StartClient(string serverIpAddress) {
            Debug.Log("start Unet Client");
            NetworkManager networkManager = Object.FindObjectOfType<NetworkManager>();
            NetworkClient nwClient = networkManager.StartClient();
            nwClient.Connect(serverIpAddress, networkManager.networkPort);
        }

        void INetworkingStarter.StartClient(NetworkingStarter networking, string _roomName, int _gameVersion) {
            roomName = _roomName;
            gameVersion = _gameVersion;

            networkMatch = networking.gameObject.AddComponent<NetworkMatch>();
            networkManager = networking.GetComponent<NetworkManager>();

            networkMatch.ListMatches(0, 10, "", true, 0, gameVersion, OnMatchList);
        }

        #region Events

        public void OnMatchList(bool success, string extendedInfo, List<MatchInfoSnapshot> matches) {
            if (success && matches != null) {
                int foundRoom = -1;
                for (int i = 0; i < matches.Count; i++) {
                    if (matches[i].name == roomName)
                        foundRoom = i;
                }

                if (foundRoom == -1) {
                    networkMatch.CreateMatch(roomName, 1000, true, "", "", "", 0, gameVersion, OnMatchCreated);
                }
                else {
                    networkMatch.JoinMatch(matches[foundRoom].networkId, "", "", "", 0, 0, OnMatchJoined);

                }
            }
            else if (!success) {
                Debug.LogError("List match failed: " + extendedInfo);
            }
        }

        public void OnMatchCreated(bool success, string extendedInfo, MatchInfo matchInfo) {
            if (success) {
                matchCreated = true;
                networkManager.StartHost(matchInfo);
            }
            else {
                Debug.LogError("Create match failed: " + extendedInfo);
            }
        }

        //bool joined;
        public void OnMatchJoined(bool success, string extendedInfo, MatchInfo matchInfo) {
            if (success) {
                if (matchCreated) {
                    Debug.LogWarning("Match already set up, aborting...");
                    return;
                }
                //joined = true;
                NetworkClient nwClient = networkManager.StartClient(matchInfo);
#if UNITY_WSA_10_0 && !UNITY_EDITOR
                //nwClient.Connect(matchInfo); not supported on WSA...
#else
                nwClient.Connect(matchInfo);
#endif
            }
            else {
                Debug.LogError("Join match failed " + extendedInfo);
            }
        }

        #endregion
    }
#pragma warning restore 0618
#endif


}