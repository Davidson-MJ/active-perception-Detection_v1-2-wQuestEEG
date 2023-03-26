using UnityEngine;
#if hNW_MIRROR
using Mirror;
#endif

namespace Passer {
#if hNW_MIRROR    
    public class MirrorStarter : INetworkingStarter {
        GameObject INetworkingStarter.GetHumanoidPrefab() {
            GameObject humanoidPrefab = Resources.Load<GameObject>("HumanoidPlayer");
            return humanoidPrefab;
        }

        void INetworkingStarter.StartHost(NetworkingStarter nwStarter) {
            NetworkManager networkManager = nwStarter.GetComponent<NetworkManager>();
            if (networkManager == null) {
                Debug.LogError("Could not start host: NetworkManager is missing");
                return;
            }

            networkManager.StartHost();
        }

        void INetworkingStarter.StartClient(NetworkingStarter nwStarter) {
            // Pose messages may be received before the Client is started, 
            // Without a handler, Mirror will then disconnect,
            // so we set a /dev/null handler here.
            // At the start of the client, the handler will be replaced by a proper function
            NetworkClient.ReplaceHandler<Humanoid.HumanoidPlayer.PoseMessage>(IgnoreHumanoidPose);

            NetworkManager networkManager = nwStarter.GetComponent<NetworkManager>();
            networkManager.networkAddress = nwStarter.serverIpAddress;
            networkManager.StartClient();
        }

        private void IgnoreHumanoidPose(NetworkConnection nwConnection, Humanoid.HumanoidPlayer.PoseMessage msg) {
        }

        void INetworkingStarter.StartClient(NetworkingStarter nwStarter, string roomName, int gameVersion) {
            ((INetworkingStarter)this).StartClient(nwStarter);
        }
    }
#endif
}