using System;
using System.IO;
#if hNW_BOLT
using UdpKit;
#endif
using UnityEngine;

namespace Passer {
    [System.Serializable]
    [HelpURLAttribute("https://passervr.com/documentation/humanoid-control/networking-support/")]
#if hNW_PHOTON
#if hPHOTON2
    public class NetworkingStarter : Photon.Pun.MonoBehaviourPunCallbacks {
#else
    public class NetworkingStarter : Photon.PunBehaviour {
#endif
#elif hNW_BOLT
    public class NetworkingStarter : Bolt.GlobalEventListener {
#else
    public class NetworkingStarter : MonoBehaviour {
#endif

        public bool autoStart = true;
#if hNW_UNET
        protected INetworkingStarter starter = new UnetStarter();
#elif hNW_PHOTON
        protected INetworkingStarter starter = new PunStarter();
#elif hNW_BOLT
        protected INetworkingStarter starter = new BoltStarter();
#elif hNW_MIRROR
        protected INetworkingStarter starter = new MirrorStarter();
#else
        protected INetworkingStarter starter;
#endif

        public string serverIpAddress = "127.0.0.1";
        public string roomName = "default";
        public int gameVersion = 1;

        public GameObject playerPrefab;
        public bool connected = false;
        public bool connecting = false;

        public enum ServerType {
            CloudServer,
            OwnServer
        }
        public ServerType serverType;

        public bool useRoleFile = false;
        public string roleFileName = "Role.txt";
        public enum Role {
            Host,
            Client,
            //Server,
        }
        public Role role;

        public int sendRate = 25;

        protected virtual void Start() {
            if (!autoStart || starter == null)
                return;

            if (playerPrefab == null)
                playerPrefab = starter.GetHumanoidPrefab(); //GetHumanoidNetworkingPrefab();

            if (serverType == ServerType.CloudServer)
                StartClient(roomName, gameVersion);
            else {
                if (useRoleFile) {
                    string filename = Application.streamingAssetsPath + "/" + roleFileName;
                    StreamReader file = File.OpenText(filename);
                    string roleText = file.ReadLine();
                    serverIpAddress = file.ReadLine();
                    if (roleText == "Host")
                        role = Role.Host;
                    else if (roleText == "Client")
                        role = Role.Client;                                            
                    file.Close();
                }
                if (role == Role.Host)
                    StartHost();
                else
                    StartClient();
            }
        }

        /// <summary>Start local networking with Host role</summary>
        public void StartHost() {
            starter.StartHost(this);
        }

        /// <summary>Start local networking with Client role</summary>
        public void StartClient() {
            starter.StartClient(this);
        }
        /// <summary>Start cloud networking with Client role</summary>
        public void StartClient(string roomName, int gameVersion) {
            starter.StartClient(this, roomName, gameVersion);
        }

#if hNW_PHOTON
        public override void OnConnectedToMaster() {
            ((PunStarter)starter).OnConnectedToMaster();
        }

#if hPHOTON2
        public override void OnJoinRoomFailed(short returnCode, string message) {
#else
        public override void OnPhotonJoinRoomFailed(object[] codeAndMsg) {
#endif
            ((PunStarter)starter).OnPhotonJoinRoomFailed();
        }

        public override void OnJoinedRoom() {
            ((PunStarter)starter).OnJoinedRoom(playerPrefab);
        }
#elif hNW_BOLT
        public override void BoltStartDone() {
            ((BoltStarter)starter).OnStarted();
        }

        public override void SessionListUpdated(Map<Guid, UdpSession> sessionList) {
            ((BoltStarter)starter).OnConnectedToServer(sessionList);
        }
#endif
    }

    public interface INetworkingStarter {
        void StartHost(NetworkingStarter nwStarter);
        void StartClient(NetworkingStarter nwStarter);
        void StartClient(NetworkingStarter nwStarter, string roomName, int gameVersion);
        GameObject GetHumanoidPrefab();
    }
}