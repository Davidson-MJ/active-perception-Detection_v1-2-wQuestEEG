using UnityEngine;

namespace Passer {

    public class CharacterCapsule : MonoBehaviour {

        public CharacterController characterController;

        // Use this for initialization
        void Start() {
            characterController = GetComponentInParent<CharacterController>();
        }

        // Update is called once per frame
        void Update() {
            if (characterController != null) {
                float diameter = characterController.radius * 2;
                this.transform.localScale = new Vector3(diameter, characterController.height / 2, diameter);
                this.transform.localPosition = characterController.center;
            }
        }
    }
}