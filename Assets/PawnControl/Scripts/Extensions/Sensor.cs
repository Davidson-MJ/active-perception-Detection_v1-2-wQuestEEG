using UnityEngine;

namespace Passer {

    [System.Serializable]
    public class Sensor {
        public bool enabled;
        public virtual Tracker.Status status { get; set; }

        public Target target;
        public Tracker tracker;

        public virtual string name { get { return ""; } }

        public Transform sensorTransform;

        public virtual void Start(Transform targetTransform) {
            target = targetTransform.GetComponent<Target>();
        }

        public virtual void Update() {
            if (tracker == null || !tracker.enabled || !enabled)
                return;
        }

        public virtual void ShowSensor(bool shown) {
            if (sensorTransform == null)
                return;

            if (!Application.isPlaying)
                sensorTransform.gameObject.SetActive(shown);

            Renderer[] renderers = sensorTransform.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++) {
                if (!(renderers[i] is LineRenderer))
                    renderers[i].enabled = shown;
            }
        }
    }
}