using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Passer {

	public class KeyboardInput : MonoBehaviour {

		public static string[] eventTypeLabels = new string[] {
				"Never",
				"On Press",
				"On Release",
				"While Down",
				"While Up",
				"On Change",
				"Continuous"
		};

		[SerializeField]
		public List<KeyboardEventHandlers> keyboardHandlers = new List<KeyboardEventHandlers>();

		protected virtual void Update() {
			foreach (KeyboardEventHandlers keyboardHandler in keyboardHandlers)
				keyboardHandler.floatValue = Input.GetKey(keyboardHandler.keyCode) ? 1 : 0;
		}
	}

	[System.Serializable]
	public class KeyboardEventHandlers : ControllerEventHandlers {
		public KeyCode keyCode;
	}

}