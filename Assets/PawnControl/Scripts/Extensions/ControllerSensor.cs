namespace Passer.Pawn {

    public class ControllerSensor : Sensor {
        protected PawnHand controllerTarget {
            get { return (PawnHand)target; }
        }
    }

}