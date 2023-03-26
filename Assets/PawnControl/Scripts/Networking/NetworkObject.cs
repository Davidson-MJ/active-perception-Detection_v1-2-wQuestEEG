using UnityEngine;

namespace Passer {

//#if !(hNW_PHOTON && hPHOTON2) && !hNW_UNET
    public partial class NetworkObject : MonoBehaviour, INetworkObject {
//#else
//    public partial class NetworkObject : INetworkObject {
//#endif
        public static bool connected = false;

        public static INetworkObject GetINetworkObject(FunctionCall functionCall) {
            if (functionCall.targetGameObject == null)
                return null;

            INetworkObject networkObj = functionCall.targetGameObject.GetComponent<INetworkObject>();
            return networkObj;
        }
//#if !(hNW_PHOTON && hPHOTON2) && !hNW_UNET
        public void RPC(FunctionCall function) { }

        public void RPC(FunctionCall functionCall, bool value) { }

//#endif

    }

    public interface INetworkObject {

        void RPC(FunctionCall functionCall);

        void RPC(FunctionCall functionCall, bool value);
    }
}