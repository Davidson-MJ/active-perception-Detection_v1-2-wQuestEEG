using UnityEngine;

namespace Passer {

    /// <summary>
    /// A generic tracking sensor
    /// </summary>
    public class SensorComponent : MonoBehaviour {
        protected Transform trackerTransform;

        public Tracker.Status status;

        public float rotationConfidence;
        public float positionConfidence;

        public bool autoUpdate = true;

        private bool _show;
        public bool show {
            set {
                if (value == true && !_show) {
                    renderController = true;

                    _show = true;
                }
                else if (value == false && _show) {
                    renderController = false;

                    _show = false;
                }
            }
            get {
                return _show;
            }
        }

        public void CheckRenderers() {
            Renderer[] renderers = this.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
                renderer.enabled = _show;
        }
        protected bool renderController {
            set {
                Renderer[] renderers = this.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                    renderer.enabled = value;
            }
        }

        virtual protected void Awake() {
            if (trackerTransform == null)
                trackerTransform = transform.parent;
        }

        virtual protected void Start() {
            //if (autoUpdate)
            //    StartComponent(trackerTransform);
        }

        public virtual void StartComponent(Transform trackerTransform) {
            // When this function has been called, the sensor will no longer update from Unity Updates.
            // Instead, UpdateComponent needs to be called to update the sensor data
            autoUpdate = false;
            this.trackerTransform = trackerTransform;
        }

        private void Update() {
            if (autoUpdate)
                UpdateComponent();
        }

        public virtual void UpdateComponent() {
            status = Tracker.Status.Unavailable;
            positionConfidence = 0;
            rotationConfidence = 0;
        }
    }

}
