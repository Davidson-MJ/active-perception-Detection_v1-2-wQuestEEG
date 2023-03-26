using UnityEngine;
using UnityEngine.Events;

namespace Passer {

    /// <summary>A list of event handlers with GameObject parameters</summary>
    /// This is used to implement a list of functions 
    /// which should be called with a GameObject as parameter.
    [System.Serializable]
    public class GameObjectEventHandlers : EventHandlers<GameObjectEvent> {
        public GameObject value {
            get {
                if (events == null || events.Count == 0)
                    return null;
                return events[0].value;
            }
            set {
                foreach (GameObjectEvent goEvent in events)
                    goEvent.value = value;
            }
        }

        public void SetMethod(EventHandler.Type eventType, UnityAction voidAction, int index = 0) {
            if (events != null && events.Count > index)
                events[index].SetMethod(EventHandler.Type.OnStart, voidAction);
        }
    }

    /// <summary>An EventHandler calling a function with a GameObject type parameter</summary>
    [System.Serializable]
    public class GameObjectEvent : EventHandler {
        public GameObjectEvent() {
            this.eventType = Type.OnChange;
        }
        public GameObjectEvent(Type eventType) {
            this.eventType = eventType;
        }

        public void SetMethod(Type newEventType, UnityAction voidAction) {
            eventType = newEventType;
            //boolEvent.AddListener(b => voidAction());
        }
        public void SetMethod(Type newEventType, UnityAction<bool> boolAction) {
            eventType = newEventType;
            //boolEvent.AddListener(boolAction);
        }
        public void SetMethod(Type newEventType, UnityAction<GameObject> gameObjectAction) {
            eventType = newEventType;
            //gameObjectEvent.AddListener(gameObjectAction);
        }

        protected GameObject gameObject;
        protected bool objectChanged;


        /// <summary>The GameObject value for this event</summary>
        public GameObject value {
            get { return gameObject; }
            set {
                bool newBoolValue = boolInverse ? (value == null) : (value != null);
                boolChanged = newBoolValue != boolValue;

                // This is disabled because of ticket #2158070 (https://passervr.com/support/upload/scp/tickets.php?id=1273)
                //if (!initialized) {
                //    boolChanged = true;
                //    initialized = true;
                //}
                _boolValue = newBoolValue;

                objectChanged = (value != gameObject);
                gameObject = value;
                Update();
            }
        }

        //override protected void Update() {
        //    if (functionCall.parameters.Length == 0)
        //        return;

        //    switch (functionCall.parameters[0].type) {
        //        case FunctionCall.ParameterType.Void:
        //            UpdateVoid();
        //            break;
        //        case FunctionCall.ParameterType.Float:
        //            UpdateFloat();
        //            break;
        //        case FunctionCall.ParameterType.Int:
        //            UpdateInt();
        //            break;
        //        case FunctionCall.ParameterType.Bool:
        //            UpdateBool();
        //            break;
        //        case FunctionCall.ParameterType.GameObject:
        //            UpdateGameObject();
        //            break;
        //        case FunctionCall.ParameterType.Rigidbody:
        //            UpdateRigidbody();
        //            break;
        //    }
        //}

        protected override void UpdateGameObject() {
            if (CheckCondition(boolValue, boolChanged, objectChanged)) {
                if (functionCall.parameters[0].fromEvent)
                    functionCall.Execute(gameObject);
                else
                    functionCall.Execute(functionCall.parameters[0].gameObjectConstant);
            }
        }

        protected override void UpdateBool() {
            if (CheckCondition(boolValue, boolChanged, boolChanged)) {
                if (functionCall.parameters[0].fromEvent)
                    functionCall.Execute(boolValue);
                else
                    functionCall.Execute(functionCall.parameters[0].boolConstant);
            }
        }
    }
}