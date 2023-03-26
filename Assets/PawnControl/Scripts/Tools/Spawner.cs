using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Passer {

    /// <summary>
    /// Component for spawning objects
    /// </summary>
    public class Spawner : SpawnPoint {

        /// <summary>
        /// The prefab which will be spawned
        /// </summary>
        public GameObject prefab;

        /// <summary>
        /// The available SpawnPoints.
        /// </summary>
        /// When the list is empty, the scene will be searched for available spawn points when it becomes enabled.
        /// If no spawn points can be found, this transform will be used as a SpawnPoint
        public SpawnPoint[] spawnPoints;

        /// <summary>
        /// The SpawnMethod to use for spawning the humanoids.
        /// </summary>
        public SpawnMethod spawnMethod;
        /// <summary>
        /// Spawning methods.
        /// </summary>
        public enum SpawnMethod {
            SinglePlayer,   ///< Only one humanoid will be spawned. It will be located at the first SpawnPoint.
            Random,         ///< A SpawnPoint is chosen randomly.
            RoundRobin      ///< Spawn points are chosen in round robin manner.
        }
        protected int spawnIndex = 0;

        /// <summary>
        /// Spawn an object when the scene starts
        /// </summary>
        public bool spawnAtStart;

        /// <summary>
        /// Will spawn only one object for this prefab
        /// </summary>
        /// When spawn is called a second time for the same prefab, the original spawned object
        /// will be teleported to the new spawn point
        public bool singleInstance;
        // used for uniqueObject
        private GameObject lastPrefabInstance;

        protected virtual void OnEnable() {
            if (spawnPoints != null && spawnPoints.Length > 0)
                return;

            spawnPoints = FindObjectsOfType<SpawnPoint>();
            if (spawnPoints.Length == 0) {
                SpawnPoint thisSpawnPoint = this.gameObject.AddComponent<SpawnPoint>();
                spawnPoints = new SpawnPoint[] { thisSpawnPoint };
            }
        }

        protected virtual void Start() {
            if (spawnAtStart)
                SpawnObject();
        }

        /// <summary>
        /// Spawn one GameObject using the Spawner settings. 
        /// </summary>
        /// <returns>The instantiated GameObject. Will be null when the prefab could not be instantiated.</returns>
        public virtual GameObject SpawnObject() {
            if (prefab == null)
                return null;

            return Instantiate(prefab, transform.position, transform.rotation);
        }

        public void DoSpawn(GameObject prefab) {
            Spawn(prefab);
        }

        /// <summary>
        /// Spawn a GameObject
        /// </summary>
        /// <param name="prefab">The GameObject prefab to spawn.</param>
        /// <returns>The instantiated GameObject. Will be null when the prefab could not be spawned.</returns>
        public virtual GameObject Spawn(GameObject prefab) {
            return Spawn(prefab, spawnPoints, spawnMethod);
        }

        /// <summary>
        /// Spawn a GameObject
        /// </summary>
        /// <param name="prefab">The GameObject prefab to spawn.</param>
        /// <param name="spawnPoints">The array of possible spawn points.</param>
        /// <param name="spawnMethod">The SpawnMethod to use for spawning.</param>
        /// <returns>The instantiated GameObject. Will be null when the prefab could not be spawned.</returns>
        public virtual GameObject Spawn(GameObject prefab, SpawnPoint[] spawnPoints, SpawnMethod spawnMethod = SpawnMethod.RoundRobin) {
            if (prefab == null)
                return null;

            GameObject newGameObject = null;
            SpawnPoint spawnPoint = ChooseSpawnPoint(spawnPoints, spawnMethod);
            if (spawnPoint == null)
                Debug.Log("Could not find an empty SpawnPoint");
            else {
                if (singleInstance && lastPrefabInstance != null) {
                    lastPrefabInstance.transform.position = spawnPoint.transform.position;
                    lastPrefabInstance.transform.rotation = spawnPoint.transform.rotation;
                    newGameObject = lastPrefabInstance;
                }
                else {

                    //NetworkedTransform networkedTransform = prefab.GetComponent<NetworkedTransform>();
                    //if (networkedTransform != null)
                    //    newGameObject = NetworkedTransform.Instantiate(prefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
                    //else
                    //    newGameObject = Instantiate(prefab, spawnPoint.transform.position, spawnPoint.transform.rotation);

                    newGameObject = Spawn(prefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
                    lastPrefabInstance = newGameObject;
                }
            }

            return newGameObject;
        }

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation) {
            GameObject newGameObject;

            NetworkedTransform networkedTransform = prefab.GetComponent<NetworkedTransform>();
            if (networkedTransform != null)
                newGameObject = NetworkedTransform.Instantiate(prefab, position, rotation);
            else
                newGameObject = Instantiate(prefab, position, rotation);

            return newGameObject;
        }

        protected SpawnPoint ChooseSpawnPoint(SpawnPoint[] spawnPoints, SpawnMethod spawnMethod) {
            if (spawnPoints == null)
                return null;

            List<SpawnPoint> availablePoints = new List<SpawnPoint>();
            foreach (SpawnPoint spawnPoint in spawnPoints) {
                if (spawnPoint is HumanoidSpawnPoint) {
                    if (((HumanoidSpawnPoint)spawnPoint).isFree)
                        availablePoints.Add(spawnPoint);
                } else
                    availablePoints.Add(spawnPoint);
            }

            int spawnPointIndex = GetSpawnPointIndex(availablePoints.Count, spawnMethod);
            if (spawnPointIndex == -1)
                return null;

            return availablePoints[spawnPointIndex];
        }

        private int GetSpawnPointIndex(int nSpawnPoints, SpawnMethod spawnMethod) {
            if (nSpawnPoints <= 0)
                return -1;

            switch (spawnMethod) {
                case SpawnMethod.RoundRobin:
                    return spawnIndex++ & nSpawnPoints;

                case SpawnMethod.Random:
                default:
                    return Random.Range(0, nSpawnPoints - 1);
            }
        }

    }

}