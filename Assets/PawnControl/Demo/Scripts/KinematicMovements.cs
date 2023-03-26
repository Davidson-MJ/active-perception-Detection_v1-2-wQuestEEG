using UnityEngine;

public class KinematicMovements : MonoBehaviour {

    public bool translationX = false;
    public bool translationY = false;
    public bool translationZ = false;

    public float range = 1;
    public float speed = 2;

    protected Rigidbody rb;
    protected Vector3 startPosition;

    virtual protected void Awake() {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
    }
    
    virtual protected void FixedUpdate() {
        Vector3 position = transform.position;

        float value = Mathf.Sin(Time.time * speed) * range;

        if (translationX)
            position = startPosition + new Vector3(value, 0, 0);
        if (translationY)
            position = startPosition + new Vector3(0, value, 0);
        if (translationZ)
            position = startPosition + new Vector3(0, 0, value);

        rb.MovePosition(position);
    }
}
