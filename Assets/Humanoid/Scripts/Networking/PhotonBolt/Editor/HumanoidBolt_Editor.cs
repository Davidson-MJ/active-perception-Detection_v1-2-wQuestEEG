using UnityEngine;

namespace Passer.Humanoid {

    public class OnLoadHumanoidPlayerBolt {
        public static void CheckHumanoidPlayer() {
#if hBOLT
            string prefabPath = OnLoadHumanoidPlayer.GetHumanoidPlayerPrefabPath();
            GameObject playerPrefab = OnLoadHumanoidPlayer.GetHumanoidPlayerPrefab(prefabPath);
#if hNW_BOLT
            if (playerPrefab != null) {
                BoltEntity boltEntity = playerPrefab.GetComponent<BoltEntity>();
                if (boltEntity == null) {
                    boltEntity = playerPrefab.AddComponent<BoltEntity>();
                }
            }
#else
            if (playerPrefab != null) {               
                BoltEntity boltEntity = playerPrefab.GetComponent<BoltEntity>();
                if (boltEntity != null)
                    Object.DestroyImmediate(boltEntity, true);
            }
#endif
            OnLoadHumanoidPlayer.UpdateHumanoidPrefab(playerPrefab, prefabPath);
#endif
        }
    }
}
