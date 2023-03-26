using UnityEngine.Events;

namespace Passer {

    /// <summary>
    /// A list of EventHandlers for takeing care of controller input
    /// </summary>
    [System.Serializable]
    public class ControllerEventHandlers : EventHandlers<ControllerEventHandler> {

        protected static string[] controllerEventTypeLabels = new string[] {
            "Never",
            "On Press",
            "On Release",
            "While Down",
            "While Up",
            "On Change",
            "Continuous"
        };

        /// <summary>
        /// Create new ControllerEventHandlers
        /// </summary>
        public ControllerEventHandlers() {
            this.eventTypeLabels = controllerEventTypeLabels;
        }

        /// <summary>
        /// For future use...
        /// </summary>
        public string defaultParameterProperty;

        /// <summary>
        /// The float input value for the controller event
        /// </summary>
        public float floatValue {
            get {
                if (events == null || events.Count == 0)
                    return 0;
                return events[0].floatValue;
            }
            set {
                foreach (ControllerEventHandler goEvent in events)
                    goEvent.floatValue = value;
            }
        }

        /// <summary>
        /// Cleanup the eventHandlers
        /// </summary>
        /// This will remove all EventHandlers for which isDead is true.
        /// <param name="eventHandlers">The array of eventHandlers to clean.</param>
        public static void Cleanup(ControllerEventHandlers[] eventHandlers) {
            foreach (ControllerEventHandlers inputEventList in eventHandlers) {
                inputEventList.events.RemoveAll(triggerEvent => triggerEvent.isDead);
            }
        }

        public void Clear() {
            events[0].eventType = EventHandler.Type.Never;
            //events[0].boolEvent.RemoveAllListeners();
        }

        public void SetMethod(EventHandler.Type eventType, UnityAction voidEvent) {
            events[0].functionCall.parameters[0].type = FunctionCall.ParameterType.Bool;
            events[0].eventType = eventType;

            //events[0].boolEvent.AddListener(b => voidEvent());
        }

        public void SetMethod(EventHandler.Type eventType, UnityAction<bool> boolEvent) {
            events[0].functionCall.parameters[0].type = FunctionCall.ParameterType.Bool;
            events[0].eventType = eventType;

            //events[0].boolEvent.AddListener(boolEvent);
        }
    }

    [System.Serializable]
    public class ControllerEventHandler : FloatEvent {
        public ControllerEventHandler(UnityEngine.GameObject gameObject, Type eventType = Type.OnChange) {
            this.eventType = eventType;
            this.functionCall = new FunctionCall();
        }

        public string label;

        public virtual float floatValue {
            get { return _floatValue / multiplicationFactor; }
            set {
                _floatValue = value * multiplicationFactor;
                bool newBoolValue = boolValue ? (value >= floatTriggerLow) : (value >= floatTriggerHigh);
                if (boolInverse)
                    newBoolValue = !newBoolValue;
                boolChanged = (newBoolValue != boolValue);
                _boolValue = newBoolValue;
                _intValue = (int)value;
                Update();
            }
        }

        public virtual float floatValue2 {
            get { return _floatValue / multiplicationFactor; }
            set {
                _floatValue = value * multiplicationFactor;
                bool newBoolValue = boolValue ? (value >= floatTriggerLow) : (value >= floatTriggerHigh);
                if (boolInverse)
                    newBoolValue = !newBoolValue;
                boolChanged = (newBoolValue != boolValue);
                _boolValue = newBoolValue;
                _intValue = (int)value;
                //UnityEngine.Debug.Log(value + " " + floatTriggerLow + " " + floatTriggerHigh + " " + boolChanged);
                Update();
            }
        }

#if pHUMANOID
        public Humanoid.Pose poseValue;
#endif
        public string stringValue;

        //public UnityStringFloatEvent stringFloatEvent;
        //public UnityStringIntEvent stringIntEvent;
        //public UnityStringBoolEvent stringBoolEvent;

#if pHUMANOID
        [System.Serializable]
        public class UnityPoseFloatEvent : UnityEvent<Humanoid.Pose, float> { }
        public UnityPoseFloatEvent poseFloatEvent;
#endif

        protected override void Update() {
#if pHUMANOID
            if (functionCall.methodName != null && functionCall.methodName.Length > 21 &&
                functionCall.methodName.Substring(0, 21).Equals("SetAnimatorParameter/")) {

                UpdateAnimationParameter();
                return;
            }
#endif            
            if (functionCall.parameters == null || functionCall.parameters.Length == 0)
                return;

            switch (functionCall.parameters[0].type) {
                case FunctionCall.ParameterType.Void:
                    UpdateVoid();
                    break;
                case FunctionCall.ParameterType.Float:
                    UpdateFloat();
                    break;
                case FunctionCall.ParameterType.Int:
                    UpdateInt();
                    break;
                case FunctionCall.ParameterType.Bool:
                    UpdateBool();
                    break;
                case FunctionCall.ParameterType.String:
                    UpdateString();
                    break;
                case FunctionCall.ParameterType.GameObject:
                    UpdateGameObject();
                    break;
            }
        }


#if pHUMANOID
        private void UpdatePoseFloat() {
            if (poseFloatEvent == null)
                return;

            switch (eventType) {
                case Type.Never:
                    break;
                case Type.WhileActive:
                    if (boolValue)
                        poseFloatEvent.Invoke(poseValue, floatValue);
                    break;
                case Type.WhileInactive:
                    if (!boolValue)
                        poseFloatEvent.Invoke(poseValue, floatValue);
                    break;
                case Type.OnStart:
                    if (boolValue && boolChanged)
                        poseFloatEvent.Invoke(poseValue, floatValue);
                    break;
                case Type.OnEnd:
                    if (!boolValue && boolChanged)
                        poseFloatEvent.Invoke(poseValue, floatValue);
                    break;
                case Type.OnChange:
                    if (intChanged)
                        poseFloatEvent.Invoke(poseValue, floatValue);
                    break;
                case Type.Continuous:
                default:
                    poseFloatEvent.Invoke(poseValue, floatValue);
                    break;
            }
        }
#endif

    }
}