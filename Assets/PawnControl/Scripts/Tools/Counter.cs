using UnityEngine;

namespace Passer {

    /// <summary>
    /// A Counter can be used to record a integer number
    /// </summary>
    public class Counter : MonoBehaviour {
        [SerializeField]
        protected int _value;
        /// <summary>
        /// Sets or gets the value of the Counter
        /// </summary>
        public int value {
            get { return _value; }
            set { _value = value; }
        }

        /// <summary>
        /// The minimum value for the Counter
        /// </summary>
        public int min = 0;
        /// <summary>
        /// The maximum value for the Counter
        /// </summary>
        public int max = 10;

        /// <summary>
        /// Decrements the Counter value by 1
        /// </summary>
        /// If the Counter value is equal or lower than the minimum value,
        /// the value is not changed
        public void Decrement() {
            if (_value > min) {
                _value--;
                counterEvent.value = _value;
            }
        }

        /// <summary>
        /// Increments the Counter value by 1
        /// </summary>
        /// If the Counter value is equal or higher than the maximum value,
        /// the value is not changed
        public void Increment() {
            if (_value < max) {
                _value++;
                counterEvent.value = _value;
            }
        }

        /// <summary>
        /// Sets the Counter value to the minimum value
        /// </summary>
        public void SetValueToMin() {
            value = min;
        }

        /// <summary>
        /// Sets the Counter value to the maximum value
        /// </summary>
        public void SetValueToMax() {
            value = max;
        }


        #region Events


        /// <summary>
        /// Can be used to call values based on the Counter value
        /// </summary>
        public IntEventHandlers counterEvent = new IntEventHandlers() {
            label = "Value Change Event",
            tooltip = 
                "Call functions using counter values\n" +
                "Parameter: the counter value"   ,
            eventTypeLabels = new string[] {
                "Never",
                "On Min",
                "On Max",
                "While Min",
                "While Max",
                "When Changed",
                "Always"
            }
        };

        #endregion
    }

}