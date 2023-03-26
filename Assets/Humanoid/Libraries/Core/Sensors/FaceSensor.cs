namespace Passer.Humanoid.Tracking {
    public enum FaceBone {
        LeftOuterBrow,
        LeftBrow,
        LeftInnerBrow,
        RightInnerBrow,
        RightBrow,
        RightOuterBrow,

        LeftCheek,
        RightCheek,

        NoseTopLeft,
        NoseTop,
        NoseTopRight,
        NoseTip,
        NoseBottomLeft,
        NoseBottom,
        NoseBottomRight,

        UpperLipLeft,
        UpperLip,
        UpperLipRight,
        LipLeft,
        LipRight,
        LowerLipLeft,
        LowerLip,
        LowerLipRight,
        LastBone
    }

    public class FaceSensor : Sensor {
        public TrackedBrow leftBrow = new TrackedBrow();
        public TrackedBrow rightBrow = new TrackedBrow();

        public TrackedEye leftEye = new TrackedEye();
        public TrackedEye rightEye = new TrackedEye();

        public TargetData leftCheek = new TargetData();
        public TargetData rightCheek = new TargetData();

        public TrackedNose nose = new TrackedNose();

        public TrackedMouth mouth = new TrackedMouth();

        public TargetData jaw = new TargetData();

        public float smile;
        public float pucker;
        public float frown;

        public FaceSensor(DeviceView deviceView) : base(deviceView) { }

        public class TrackedBrow {
            public TargetData inner;
            public TargetData center;
            public TargetData outer;
        }

        public class TrackedEye {
            public float closed;
        }

        public class TrackedNose {
            public TargetData top;
            public TargetData topLeft;
            public TargetData topRight;
            public TargetData tip;
            public TargetData bottom;
            public TargetData bottomLeft;
            public TargetData bottomRight;
        }

        public class TrackedMouth {
            public TargetData upperLipLeft;
            public TargetData upperLip;
            public TargetData upperLipRight;

            public TargetData lipLeft;
            public TargetData lipRight;

            public TargetData lowerLipLeft;
            public TargetData lowerLip;
            public TargetData lowerLipRight;
        }

        public class FaceTargetData : TargetData {
            public new Vector startPosition;
        }
    }
}
