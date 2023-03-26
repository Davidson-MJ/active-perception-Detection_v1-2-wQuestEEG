using UnityEngine;

namespace Passer.Humanoid {

    /// <summary>
    /// Interface for handling touch events on objects
    /// </summary>
    /// This interface can be used to receive touch events on objects
    /// If you implement this interface on a script which is connected to the object
    /// the On.. functions are called at the appropriate times.
    /// This enables you to implement behaviour on the object when it is touched.
    public interface IHandTriggerEvents {

        /// <summary>
        /// Function is called when the hand starts touching this object
        /// </summary>
        /// <param name="handTarget">The hand which touches the object</param>
        void OnHandTriggerEnter(HandTarget handTarget, Collider collider);

        /// <summary>
        /// Function is called when the hand no longer touches this object
        /// </summary>
        /// <param name="handTarget">The hand which touched the object</param>
        void OnHandTriggerExit(HandTarget handTarget, Collider collider);
    }
}