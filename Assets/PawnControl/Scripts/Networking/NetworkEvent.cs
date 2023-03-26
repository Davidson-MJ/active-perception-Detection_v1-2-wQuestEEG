using UnityEngine;

namespace Passer {

    public partial class NetworkEvent {
    }

    public interface INetworkEvent {
        void BoolEvent(Object target, string methodName, bool value);
    }
}