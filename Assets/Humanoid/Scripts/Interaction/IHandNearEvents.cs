namespace Passer.Humanoid {

	/// <summary>
	/// Interface for handling near events on objects
	/// </summary>
	/// This interface can be used to receive near events on objects
	/// If you implement this interface on a script which is connected to the object
	/// the On.. functions are called at the appropriate times.
	/// This enables you to implement behaviour when a hand is near an object
	/// Note: the near distance is set with HandTarget::nearDistance
	public interface IHandNearEvents {

		/// <summary>
		/// Function is called when the hand gets near this object
		/// </summary>
		/// <param name="handTarget">The hand touching the object</param>
		void OnHandNearStart(HandTarget handTarget);

		/// <summary>
		/// Function is called when the hand is no longer near this object
		/// </summary>
		/// <param name="handTarget">The hand which touched the object</param>
		void OnHandNearEnd(HandTarget handTarget);

	}
}