using UnityEngine;

namespace Passer {

    public partial class NetworkedTransform : MonoBehaviour {
#if !hNW_UNET && !hNW_MIRROR && !hNW_PHOTON && !hNW_BOLT
        public static GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation) {
            return Instantiate(prefab, position, rotation);
        }
#endif
    }

}