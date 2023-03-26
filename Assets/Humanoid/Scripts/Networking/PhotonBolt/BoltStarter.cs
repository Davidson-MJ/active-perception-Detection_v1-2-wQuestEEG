#if hNW_BOLT
using System;
using UdpKit;
using Bolt.Matchmaking;
#endif
using UnityEngine;

namespace Passer {

#if hNW_BOLT
    public class BoltStarter : INetworkingStarter {

        public GameObject playerPrefab;

        GameObject INetworkingStarter.GetHumanoidPrefab() {
            GameObject humanoidPrefab = Resources.Load<GameObject>("HumanoidPlayer");
            return humanoidPrefab;
        }

        void INetworkingStarter.StartClient(NetworkingStarter nwStarter) {
            Debug.Log("StartClient");
            playerPrefab = nwStarter.playerPrefab;
            BoltLauncher.StartClient();
        }

        void INetworkingStarter.StartClient(NetworkingStarter nwStarter, string roomName, int gameVersion) {
            Debug.Log("StartClient2");
            playerPrefab = nwStarter.playerPrefab;
            BoltLauncher.StartClient();
        }

        void INetworkingStarter.StartHost(NetworkingStarter nwStarter) {
            Debug.Log("Start Host");
            playerPrefab = nwStarter.playerPrefab;
            BoltLauncher.StartServer();
        }

        public virtual void OnStarted() {
            if (BoltNetwork.IsServer) {
                string matchName = Guid.NewGuid().ToString();
                BoltMatchmaking.CreateSession(matchName);
            }

            if (playerPrefab != null)
                BoltNetwork.Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        }

        public virtual void OnConnectedToServer(Map<Guid, UdpSession> sessionList) {
            foreach (var session in sessionList) {
                UdpSession photonSession = session.Value as UdpSession;

                if (photonSession.Source == UdpSessionSource.Photon) {
                    BoltMatchmaking.JoinSession(photonSession);
                }
            }
        }
    }
#endif
}