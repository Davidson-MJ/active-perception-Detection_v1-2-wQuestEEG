using UnityEngine;

namespace Passer {

    /// <summary>
    /// A list of event handlers with a float parameter
    /// </summary>
    [System.Serializable]
    public class FloatEventHandlers : EventHandlers<FloatEvent> {
        public float value {
            get {
                if (events == null || events.Count == 0)
                    return 0;
                return events[0].value;
            }
            set {
                foreach (FloatEvent floatEvent in events)
                    floatEvent.value = value;
            }
        }
    }

    /// <summary>
    /// An event handler calling a function with a float parameter
    /// </summary>
    [System.Serializable]
    public class FloatEvent : EventHandler {
        public FloatEvent(Type newEventType = Type.OnChange) {
            eventType = newEventType;
        }

        public float floatParameter;

        public float floatTriggerLow = 0.01F;
        public float floatTriggerHigh = 0.99F;
        public float multiplicationFactor = 1;

        public int intTriggerLow = 0;
        public int intTriggerHigh = 1;

        public virtual float value {
            get { return _floatValue; }
            set {
                floatChanged = true;
                _floatValue = value * multiplicationFactor;

                bool newBoolValue = boolValue ? (value <= floatTriggerHigh) : (value <= floatTriggerLow);
                if (boolInverse)
                    newBoolValue = !newBoolValue;
                if (initialized)
                    boolChanged = (newBoolValue != boolValue);
                else {
                    boolChanged = true;
                    initialized = true;
                }
                _boolValue = newBoolValue;

                intChanged = true; // should be improved to rounded numbers
                _intValue = (int)value;

                Update();
            }
        }

        //public virtual int intValue {
        //    get { return _intValue; }
        //    set {
        //        intChanged = (value != _intValue);
        //        _intValue = (int)(value * multiplicationFactor);
        //        bool newBoolValue = _boolValue ? (value >= intTriggerLow) : (value >= intTriggerHigh);
        //        boolChanged = (newBoolValue != _boolValue);
        //        _boolValue = newBoolValue;
        //        _floatValue = value;
        //        Update();
        //    }
        //}


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

        protected override void UpdateStringInt(string s) {
            if (CheckCondition(boolValue, boolChanged, intChanged)) {
                if (functionCall.parameters[0].fromEvent)
                    functionCall.ExecuteString(s, _intValue);
                else
                    functionCall.ExecuteString(s, functionCall.parameters[0].intConstant);
            }
        }

        protected override void UpdateStringFloat(string s) {
            if (CheckCondition(boolValue, boolChanged, floatChanged)) {
                if (functionCall.parameters[0].fromEvent)
                    functionCall.ExecuteString(s, _floatValue);
                else
                    functionCall.ExecuteString(s, functionCall.parameters[0].floatConstant);
            }
        }

    }

}