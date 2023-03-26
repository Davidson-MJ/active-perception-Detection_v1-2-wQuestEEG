using System.Collections.Generic;
using UnityEngine;

namespace Passer.Humanoid {

    [CreateAssetMenu(menuName = "Humanoid/Real World Configuration", fileName = "RealWorldConfiguration", order = 102)]
    [System.Serializable]
    public class RealWorldConfiguration : ScriptableObject {

        public List<TrackingSpace> trackers = new List<TrackingSpace>();
        
        [System.Serializable]
        public class TrackingSpace {
            public Passer.Tracking.TrackerId trackerId;
            public Vector3 position;
            public Quaternion rotation;
        }
    }
}