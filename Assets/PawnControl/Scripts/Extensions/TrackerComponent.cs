using System.Diagnostics;
using UnityEngine;

namespace Passer.Tracking {

    public enum TrackerId {
        Oculus,
        SteamVR,
        WindowsMR,
        LeapMotion,
        Kinect1,
        Kinect2,
        OrbbecAstra,
        Realsense,
        RazerHydra,
        Optitrack,
        Tobii
    };

    /// <summary>
    /// Generic Tracking device
    /// </summary>
    public class TrackerComponent : MonoBehaviour {

        public Tracker.Status status;

        protected Transform realWorld;

        protected virtual void Start() { }

        protected virtual void Update() { }

        public virtual void ShowSkeleton(bool shown) { }
    }

}