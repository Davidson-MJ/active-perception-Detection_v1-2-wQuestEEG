using UnityEngine;

namespace Passer {

    public class NetworkingStatusUI : MonoBehaviour {
        protected NetworkingStarter networkingStarter;
        protected UnityEngine.UI.Text textcomponent;

        void Start() {
            networkingStarter = FindObjectOfType<NetworkingStarter>();
            textcomponent = GetComponent<UnityEngine.UI.Text>();
        }

        void Update() {
#if hNW_UNET || hNW_PHOTON
           if (networkingStarter == null)
                return;

            //if (textcomponent != null)
//                textcomponent.text = networkingStarter.networkingStatus.ToString();
#endif
        }
    }

}