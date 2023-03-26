﻿using UnityEngine;

public class Redrop : MonoBehaviour {

	private Rigidbody thisRigidbody;
	private Vector3 startPosition;
	private Quaternion startRotation;

	void Start () {
		startPosition = transform.position;
		startRotation = transform.rotation;
	}
	
	void Update () {
		if (transform.position.y < 0.2f) {
            thisRigidbody = transform.GetComponent<Rigidbody>();
            if (thisRigidbody != null) {
                thisRigidbody.MovePosition(new Vector3(startPosition.x, 2, startPosition.z));
                thisRigidbody.MoveRotation(startRotation);
                thisRigidbody.velocity = Vector3.zero;
                thisRigidbody.angularVelocity = Vector3.zero;
            }
		}
	}
}
