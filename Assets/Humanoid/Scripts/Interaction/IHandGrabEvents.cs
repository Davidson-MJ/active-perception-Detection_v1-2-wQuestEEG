namespace Passer.Humanoid {

    /// <summary>
    /// Interface for handling grabbing events on objects
    /// </summary>
    /// This interface can be used to receive grabbing events on grabbable objects
    /// If you implement this interface on a script which is connected to the grabbable object
    /// the On.. functions are called at the appropriate times.
    /// This enables you to implement behaviour on the object when it is grabbed or let go.
    public interface IHandGrabEvents {

        /// <summary>
        /// Function is called when the object is grabbed
        /// </summary>
        /// <param name="handTarget">The hand which grabbed the object</param>
        void OnHandGrabbed(HandTarget handTarget);

        /// <summary>
        /// Function is called when the object is let go
        /// </summary>
        /// <param name="handTarget">The hand which let go the object</param>
        void OnHandLetGo(HandTarget handTarget);
    }
}