using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Passer {

    public class PlayerSelector : MonoBehaviour {
#if pHUMANOID
        public enum PlayerType {
            Pawn,
            Humanoid
        }
        public PlayerType playerType;
#endif

    }

}