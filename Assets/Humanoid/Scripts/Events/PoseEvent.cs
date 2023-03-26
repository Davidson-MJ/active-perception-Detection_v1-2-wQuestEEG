using UnityEngine.Events;

namespace Passer.Humanoid {

    [System.Serializable]
    public class PoseEventList : EventHandlers<PoseEvent> {
        public Pose value {
            get {
                if (events == null || events.Count == 0)
                    return null;
                return events[0].value;
            }
            set {
                foreach (PoseEvent poseEvent in events)
                    poseEvent.value = value;
            }
        }
    }

    [System.Serializable]
    public class UnityPoseEvent : UnityEvent<Pose> { }

    [System.Serializable]
    public class PoseEvent : EventHandler {
        public PoseEvent() {
            eventType = Type.OnChange;
        }

        public UnityPoseEvent poseEvent;

        public Pose value {
            get { return pose; }
            set {
                string valueName = value == null ? "" : value.name;
                string poseName = pose == null ? "" : pose.name;
                poseChanged = (valueName != poseName);
                pose = value;
                Update();
            }
        }

        protected Pose pose;
        protected bool poseChanged;

        protected override void Update() {
            if (poseEvent == null)
                return;

            switch (eventType) {
                case Type.WhileActive:
                    if (pose != null)
                        poseEvent.Invoke(pose);
                    break;
                case Type.WhileInactive:
                    if (pose == null)
                        poseEvent.Invoke(pose);
                    break;
                case Type.OnStart:
                    if (pose != null && poseChanged)
                        poseEvent.Invoke(pose);
                    break;
                case Type.OnEnd:
                    if (pose == null && poseChanged)
                        poseEvent.Invoke(pose);
                    break;
                case Type.OnChange:
                    if (poseChanged)
                        poseEvent.Invoke(pose);
                    break;
                case Type.Continuous:
                    poseEvent.Invoke(pose);
                    break;
                case Type.Never:
                default:
                    break;
            }
        }

        //public override UnityEventBase GetUnityEventBase() {
        //    return poseEvent;
        //}
    }
}