using UnityEngine;
using UnityEditor;

namespace Passer {
    using Pawn;

    [CustomEditor(typeof(PlayerSelector))]
    public class PlayerSelector_Editor : Editor {

        // Needs to be converted to [InitializeOnLoad] of zoiets
        private void OnEnable() {
            PlayerSelector playerSelector = (PlayerSelector)target;
            PawnControl pawnControl = playerSelector.GetComponentInChildren<PawnControl>();
            if (pawnControl != null)
                return;

            GameObject pawnPrefab;
#if pHUMANOID
            if (playerSelector.playerType == PlayerSelector.PlayerType.Humanoid) {
                pawnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Humanoid/Prefabs/Humanoid.prefab");
                if (pawnPrefab == null) {
                    Debug.LogError("Humanoid prefab is not found in Assets/Humanoid/Prefabs");
                    return;
                }
            } else
#endif
            {
                pawnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PawnControl/Prefabs/Pawn.prefab");
                if (pawnPrefab == null) {
                    Debug.LogError("Pawn prefab is not found in Assets/PawnControl/Prefabs");
                    return;
                }
            }

            GameObject pawn = Instantiate(pawnPrefab);
            pawn.transform.SetParent(playerSelector.transform);
        }

    }

}