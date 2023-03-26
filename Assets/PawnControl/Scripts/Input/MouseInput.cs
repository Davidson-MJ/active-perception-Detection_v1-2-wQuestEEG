using UnityEngine;
using UnityEngine.Events;

namespace Passer {

	public class MouseInput : MonoBehaviour {

		public enum Button {
			MouseY,
			MouseX,
			ScrollWheel,
			Left,
			Middle,
			Right
        }

		public ControllerEventHandlers[] mouseInputEvents = new ControllerEventHandlers[] {
			new ControllerEventHandlers() { label = "Mouse Vertical", id = 0 },
			new ControllerEventHandlers() { label = "Mouse Horizontal", id = 1 },
			new ControllerEventHandlers() { label = "Mouse Scroll", id = 2 },
			new ControllerEventHandlers() { label = "Left Button", id = 3 },
			new ControllerEventHandlers() { label = "Middle button", id = 4 },
			new ControllerEventHandlers() { label = "Right Button", id = 5 },
		};

		protected virtual void Update() {
			mouseInputEvents[0].floatValue -= Input.GetAxis("Mouse Y");
			mouseInputEvents[1].floatValue += Input.GetAxis("Mouse X");
			mouseInputEvents[2].floatValue += Input.GetAxis("Mouse ScrollWheel");
			mouseInputEvents[3].floatValue = Input.GetMouseButton(0) ? 1 : 0;
			mouseInputEvents[4].floatValue = Input.GetMouseButton(2) ? 1 : 0;
			mouseInputEvents[5].floatValue = Input.GetMouseButton(1) ? 1 : 0;
		}

		protected void UpdateInputList(ControllerEventHandlers inputEventHandlers, float value) {
			foreach (ControllerEventHandler handler in inputEventHandlers.events)
				handler.floatValue = value;
		}

        #region API

		public void SetEventHandler(Button button, EventHandler.Type eventType, UnityAction<bool> boolEvent) {
			if (boolEvent == null)
				return;

			ControllerEventHandlers eventHandlers = GetInputHandlers(button);

			Object target = (Object)boolEvent.Target;
			string methodName = boolEvent.Method.Name;
			methodName = target.GetType().Name + "/" + methodName;

			if (eventHandlers.events == null || eventHandlers.events.Count == 0)
				eventHandlers.events.Add(new ControllerEventHandler(gameObject, eventType));
			else
				eventHandlers.events[0].eventType = eventType;

			ControllerEventHandler eventHandler = eventHandlers.events[0];
			eventHandler.functionCall.targetGameObject = FunctionCall.GetGameObject(target);
			eventHandler.functionCall.methodName = methodName;
			eventHandler.functionCall.AddParameter();
			FunctionCall.Parameter parameter = eventHandler.functionCall.AddParameter();
			parameter.type = FunctionCall.ParameterType.Bool;
			parameter.localProperty = "From Event";
			parameter.fromEvent = true;
		}

		protected ControllerEventHandlers GetInputHandlers(Button button) {
			return mouseInputEvents[(int)button];
        }

		public void PressLeft(float value) {
			mouseInputEvents[(int)Button.Left].floatValue = value;
        }

		public void Release(Button button) {
			mouseInputEvents[(int)button].floatValue = 0;
        }

        #endregion
    }
}