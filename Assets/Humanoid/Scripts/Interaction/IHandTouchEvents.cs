namespace Passer.Humanoid {

    /// <summary>
    /// Interface for handling touch events on objects
    /// </summary>
    /// This interface can be used to receive touch events on objects
    /// If you implement this interface on a script which is connected to the object
    /// the On.. functions are called at the appropriate times.
    /// This enables you to implement behaviour on the object when it is touched.
    public interface IHandTouchEvents {

        /// <summary>
        /// Function is called when the hand starts touching this object
        /// </summary>
        /// <param name="handTarget">The hand touching the object</param>
        void OnHandTouchStart(HandTarget handTarget);

        /// <summary>
        /// Function is called when the hand no longer touches this object
        /// </summary>
        /// <param name="handTarget">The hand which touched the object</param>
        void OnHandTouchEnd(HandTarget handTarget);
    }
}

