using System.Collections;
using UnityEngine;

public class FlashLight : MonoBehaviour {

    protected Light thisLight;

    public float duration = 0.1F;

    protected virtual void Awake() {
        thisLight = GetComponent<Light>();
    }

    public void Flash() {
        Flash(duration);
    }

    public void Flash(float duration) {
        if (thisLight == null)
            return;

        StartCoroutine(FlashRoutine(thisLight, duration));
    }

    protected IEnumerator FlashRoutine(Light light, float duration) {
        if (light == null)
            yield return null;

        light.enabled = true;
        yield return new WaitForSeconds(duration);
        light.enabled = false;
    }
}
