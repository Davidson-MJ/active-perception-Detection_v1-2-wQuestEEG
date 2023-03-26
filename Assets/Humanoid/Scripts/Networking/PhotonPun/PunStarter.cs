using UnityEngine;
#if hPHOTON2
using Photon.Pun;
using Photon.Realtime;
#endif

namespace Passer {
#if hNW_PHOTON
    public class PunStarter : INetworkingStarter {

        public GameObject playerPrefab;

        public string roomName;
        public int gameVersion;
        public int sendRate;

        GameObject INetworkingStarter.GetHumanoidPrefab() {
            GameObject humanoidPrefab = Resources.Load<GameObject>("HumanoidPlayer");
            return humanoidPrefab;
        }

        void INetworkingStarter.StartHost(NetworkingStarter nwStarter) {
            ((INetworkingStarter)this).StartClient(nwStarter);
        }

        void INetworkingStarter.StartClient(NetworkingStarter nwStarter) {
            ((INetworkingStarter)this).StartClient(nwStarter, nwStarter.roomName, nwStarter.gameVersion);
        }

        void INetworkingStarter.StartClient(NetworkingStarter nwStarter, string _roomName, int _gameVersion) {
            roomName = _roomName;
            gameVersion = _gameVersion;
            playerPrefab = Resources.Load<GameObject>("HumanoidPlayer");
            sendRate = nwStarter.sendRate;

#if hPHOTON2
            PhotonNetwork.SendRate = sendRate;
            PhotonNetwork.SerializationRate = sendRate;
            PhotonNetwork.GameVersion = gameVersion.ToString();
            PhotonNetwork.ConnectUsingSettings();
#else
            PhotonNetwork.sendRate = sendRate;
            PhotonNetwork.sendRateOnSerialize = sendRate;
            PhotonNetwork.ConnectUsingSettings(gameVersion.ToString());
#endif
        }

        public void OnConnectedToPhoton() {
            Debug.Log("Photon");
        }

        public void OnConnectedToMaster() {
            RoomOptions roomOptions = new RoomOptions() { IsVisible = false, MaxPlayers = 4 };
            PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, TypedLobby.Default);
        }

        public void OnPhotonJoinRoomFailed() {
            Debug.LogError("Could not joint the " + roomName + " room");
        }

        public virtual void OnJoinedRoom(GameObject playerPrefab) {
            if (playerPrefab != null)
                PhotonNetwork.Instantiate(playerPrefab.name, Vector3.zero, Quaternion.identity, 0);

            //NetworkingSpawner spawner = FindObjectOfType<NetworkingSpawner>();
            //if (spawner != null)
            //    spawner.OnNetworkingStarted();
        }
    }
#endif
}
