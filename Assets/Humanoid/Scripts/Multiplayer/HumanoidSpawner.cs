using UnityEngine;

namespace Passer.Humanoid {

    /// <summary>
    /// Component for spawning humanoids 
    /// </summary>
    public class HumanoidSpawner : Spawner {

        protected HumanoidControl[] spawnedHumanoids;
        protected static int nHumanoids;

        protected override void Start() {
            if (spawnAtStart)
                SpawnHumanoid();
        }

        /// <summary>
        /// Spawn one humanoid using the HumanoidSpawner settings.
        /// </summary>
        /// <returns>The HumanoidControl of the spawned humanoid. Will be null when the humanoid could be not spawned.</returns>
        public override GameObject SpawnObject() {
            HumanoidControl humanoidPrefab = prefab.GetComponent<HumanoidControl>();
            if (humanoidPrefab == null) {
                Debug.LogError("The prefab is not an Humanoid.");
                return null;
            }

            HumanoidControl humanoid = Spawn(humanoidPrefab, spawnPoints, spawnMethod);
            if (humanoid == null)
                return null;

            return humanoid.gameObject;
        }

        /// <summary>
        /// Spawn one humanoid using the HumanoidSpawner settings.
        /// </summary>
        /// <returns>The HumanoidControl of the spawned humanoid. Will be null when the humanoid could be not spawned.</returns>
        public virtual HumanoidControl SpawnHumanoid() {
            HumanoidControl humanoid = prefab.GetComponent<HumanoidControl>();
            if (humanoid == null) {
                Debug.LogError("The prefab is not an Humanoid.");
                return null;
            }

            return Spawn(humanoid, spawnPoints, spawnMethod);
        }

        /// <summary>
        /// Spawn a humanoid
        /// </summary>
        /// <param name="prefab">The humanoid prefab to spawn.</param>
        /// <returns>The HumanoidControl of the spawned humanoid. Will be null when the humanoid could be not spawned.</returns>
        public virtual HumanoidControl Spawn(HumanoidControl humanoidPrefab) {
            return Spawn(humanoidPrefab, spawnPoints, spawnMethod);
        }

        /// <summary>
        /// Spawn a humanoid.
        /// </summary>
        /// <param name="humanoidPrefab">The humanoid prefab to spawn.</param>
        /// <param name="spawnPoints">The array of possible spawn points.</param>
        /// <param name="spawnMethod">The SpawnMethod to use for spawning.</param>
        /// <returns>The HumanoidControl of the spawned humanoid. Will be null when the humanoid could be not spawned.</returns>
        public HumanoidControl Spawn(HumanoidControl humanoidPrefab, SpawnPoint[] spawnPoints, SpawnMethod spawnMethod = SpawnMethod.RoundRobin) {
            if (humanoidPrefab == null || (spawnMethod == SpawnMethod.SinglePlayer && nHumanoids > 0))
                return null;

            GameObject newPlayer = null;
            SpawnPoint spawnPoint = ChooseSpawnPoint(spawnPoints, spawnMethod);
            if (spawnPoint == null) {
                Debug.Log("Could not find an empty SpawnPoint");
                return null;
            }
            else {
                // No need to check for networking, this is handles by HumanoidNetworking
                newPlayer = Instantiate(humanoidPrefab.gameObject, spawnPoint.transform.position, spawnPoint.transform.rotation);
            }

            HumanoidControl humanoid = newPlayer.GetComponent<HumanoidControl>();
            if (humanoid == null) {
                humanoid = AddHumanoidToAvatar(newPlayer);
                if (humanoid == null) {
                    Debug.LogError("Avatar is not a Humanoid!");
                    return null;
                }
            }

            return humanoid;
        }

        protected int FindSpawnPointIndex() {
            for (int i = 0; i < spawnPoints.Length; i++) {
                int spawnPointIndex = GetSpawnPointIndex();
                if (spawnPoints[spawnPointIndex] is HumanoidSpawnPoint) {
                    if (((HumanoidSpawnPoint)spawnPoints[spawnPointIndex]).isFree)
                        return spawnPointIndex;
                }
                else
                    return spawnPointIndex;
            }
            return -1;
        }

        private int GetSpawnPointIndex() {
            if (spawnPoints.Length <= 0)
                return -1;

            switch (spawnMethod) {
                case SpawnMethod.RoundRobin:
                    return spawnIndex++ & spawnPoints.Length;

                case SpawnMethod.Random:
                default:
                    return Random.Range(0, spawnPoints.Length - 1);
            }
        }

        protected void DestroyHumanoid(HumanoidControl humanoid) {
            Destroy(humanoid.gameObject);
        }

        protected static HumanoidControl AddHumanoidToAvatar(GameObject avatar) {
            Animator animator = avatar.GetComponent<Animator>();
            if (animator == null || !animator.isHuman)
                return null;

            HumanoidControl humanoid = avatar.AddComponent<HumanoidControl>();
            return humanoid;
        }
    }
}