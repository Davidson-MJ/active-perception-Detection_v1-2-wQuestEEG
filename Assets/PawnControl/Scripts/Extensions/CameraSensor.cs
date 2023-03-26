namespace Passer.Pawn {

    public class CameraSensor : Sensor {
        protected PawnHead cameraTarget {
            get { return (PawnHead)target; }
        }
    }

}