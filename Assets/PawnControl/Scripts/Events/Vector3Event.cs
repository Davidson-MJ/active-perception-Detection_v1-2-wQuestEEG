using UnityEngine;

namespace Passer {

    /// <summary>
    /// A list of event handlers with a Vector3 parameter
    /// </summary>
    [System.Serializable]
    public class Vector3EventList : EventHandlers<Vector3Event> {
        public Vector3 value {
            get {
                if (events == null || events.Count == 0)
                    return Vector3.zero;
                return events[0].value;
            }
            set {
                foreach (Vector3Event floatEvent in events)
                    floatEvent.value = value;
            }
        }
    }

    /// <summary>
    /// An event Handler calling a function with a Vector3 parameter
    /// </summary>
    [System.Serializable]
    public class Vector3Event : EventHandler {
        public Vector3Event(Type newEventType = Type.OnChange) {
            eventType = newEventType;
        }

        protected Vector3 _vectorValue;
        protected bool vectorChanged;

        public virtual Vector3 value {
            get { return _vectorValue; }
            set {
                vectorChanged = (value != _vectorValue);
                _vectorValue = value;
                Update();
            }
        }

        protected override void UpdateVector3() {
            if (CheckCondition(_vectorValue.sqrMagnitude > 0, vectorChanged, vectorChanged))
                functionCall.Execute(_vectorValue);
        }
    }

}