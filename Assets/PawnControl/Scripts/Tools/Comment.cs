using UnityEngine;

namespace Passer {

    public class Comment : MonoBehaviour {
        [SerializeField]
        private string text;

        private void Start() {
            transform.forward = Vector3.up;
        }
    }

}