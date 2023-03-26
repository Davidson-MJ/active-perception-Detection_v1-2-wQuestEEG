using UnityEngine;

namespace Passer {

    /// <summary>
    /// A Transform represeting a place where objects can spawn
    /// </summary>
    public class SpawnPoint : MonoBehaviour {

        #region Gizmos

        private void OnDrawGizmos() {
            Gizmos.color = new Color(244F / 255F, 122F / 255F, 0);
            Gizmos.DrawCube(transform.position, new Vector3(0.2F, 0.01F, 0.2F));
        }

        #endregion  
    }
}

