using System;
using UnityEngine;

namespace Passer.Humanoid {
#pragma warning disable 0618

#if hNW_UNET || hNW_MIRROR || hNW_PHOTON || hNW_BOLT
    public partial class HumanoidPlayer {

        [SerializeField]
        protected bool _syncFingerSwing = false;
        public bool syncFingerSwing {
            get { return _syncFingerSwing; }
        }

        [SerializeField]
        protected bool _syncFace = false;
        public bool syncFace {
            get { return _syncFace; }
        }

        [SerializeField]
        private bool _syncTracking = false;
        public bool syncTracking {
            get { return _syncTracking; }
            set { _syncTracking = value; }
        }

        public bool fuseTracking { get; set; }
#else
    public class HumanoidPlayer : MonoBehaviour {
#endif
    }

#pragma warning restore 0618
}
