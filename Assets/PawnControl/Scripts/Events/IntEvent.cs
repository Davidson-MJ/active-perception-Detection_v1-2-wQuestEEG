namespace Passer {

    /// <summary>
    /// A list of event handlers with an integer parameter
    /// </summary>
    [System.Serializable]
    public class IntEventHandlers : EventHandlers<IntEventHandler> {
        public int value {
            get {
                if (events == null || events.Count == 0)
                    return 0;
                return events[0].value;
            }
            set {
                foreach (IntEventHandler intEvent in events)
                    intEvent.value = value;
            }
        }
    }

    /// <summary>
    /// An event handler calling a function with an integer parameter
    /// </summary>
    [System.Serializable]
    public class IntEventHandler : EventHandler {
        public IntEventHandler() {
            this.eventType = Type.OnChange;
        }
        public IntEventHandler(Type eventType) {
            this.eventType = eventType;
        }

        public int minValue;
        public int maxValue;

        public int intTriggerLow = 0;
        public int intTriggerHigh = 1;
        public int multiplicationFactor = 1;

        public virtual int value {
            get { return _intValue; }
            set {
                intChanged = true;
                _intValue = 
                    value < minValue ? minValue :
                    value > maxValue ? maxValue :
                    value;

                bool newBoolValue = boolValue ? (value < intTriggerHigh) : (value <= intTriggerLow);
                if (boolInverse)
                    newBoolValue = !newBoolValue;
                if (initialized)
                    boolChanged = (newBoolValue != boolValue);
                else {
                    boolChanged = true;
                    initialized = true;
                }
                _boolValue = newBoolValue;

                floatChanged = true;
                _floatValue = (float)value;

                Update();
            }
        }

        override protected void UpdateBool() {
            if (CheckCondition(boolValue, boolChanged, boolChanged)) {
                if (functionCall.parameters[0].fromEvent)
                    functionCall.Execute(_boolValue);
                else
                    functionCall.Execute(functionCall.parameters[0].boolConstant);
            }
        }

        override protected void UpdateInt() {
            if (CheckCondition(boolValue, boolChanged, intChanged)) {
                if (functionCall.parameters[0].fromEvent)
                    functionCall.Execute(_intValue);
                else
                    functionCall.Execute(functionCall.parameters[0].intConstant);
            }
        }

        override protected void UpdateFloat() {
            if (CheckCondition(boolValue, boolChanged, intChanged)) {
                if (functionCall.parameters[0].fromEvent)
                    functionCall.Execute(_floatValue);
                else
                    functionCall.Execute(functionCall.parameters[0].floatConstant);
            }
        }
    }
}