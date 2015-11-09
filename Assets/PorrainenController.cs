using UnityEngine;
using System.Collections;

public class PorrainenController : MonoBehaviour
{


	Vector3 referenceAccelleration;

	// Use this for initialization
	void Start ()
	{
		referenceAccelleration = Input.acceleration;
	}


	void DoKeyboardControls ()
	{
		float pitchSensitivity = 3f;
		float rollSensitivity = 2f;
		
		float t = Time.deltaTime;
		
		float roll = Input.GetAxis ("Horizontal");
		float pitch = Input.GetAxis ("Vertical");
		
		Rigidbody rigidbody = GetComponent<Rigidbody> ();
		
		rigidbody.AddRelativeTorque (Vector3.right * pitch * pitchSensitivity * t);
		rigidbody.AddRelativeTorque (Vector3.forward * -roll * rollSensitivity * t);
		
//		float targetRollAngle = 90 * roll;
//		float targetPitchAngle = 90 * pitch;
//		Quaternion rot = Quaternion.Euler (targetPitchAngle, 0f, -targetRollAngle);
//		rigidbody.MoveRotation (rot);
	}

	void FixedUpdate ()
	{
		float t = Time.deltaTime;
		DoKeyboardControls ();

		// Add fly force;
		Rigidbody rigidbody = GetComponent<Rigidbody> ();
		Debug.Log ("Velocity: " + rigidbody.velocity.magnitude);

		rigidbody.AddRelativeForce (Vector3.forward * t * 5f);
		//Debug.Log ("A:" + Input.acceleration);
//		SystemInfo.supportsGyroscope

	}

	void XXFixedUpdate ()
	{
		float turn = Input.GetAxis ("Horizontal");
		if (turn == 0.0f) {
			turn = Input.acceleration.x;
		}
		float turnSpeed = turn * Time.deltaTime * 1f;
		//transform.Rotate (Vector3.up * turnSpeed);

		Rigidbody rb = GetComponent<Rigidbody> ();
		if (Mathf.Approximately (0f, turn)) {
			// straighten up

//			Quaternion.RotateTowards(transform, 
//			transform.up = Vector3.RotateTowards (transform.up, Vector3.up, Time.deltaTime, 0f);
		} else {
			// Turn and roll
			rb.AddRelativeTorque (Vector3.forward * -turnSpeed);
		}

		float accel = Input.acceleration.y - referenceAccelleration.y;
		float accelSpeed = accel * Time.deltaTime * 20f;
		//rb.AddForce (transform.forward * accel);
		Debug.Log ("A:" + Input.acceleration);
		float tilt = Input.GetAxis ("Vertical");
		float tiltSpeed = tilt * Time.deltaTime * 20f;
		rb.AddRelativeTorque (Vector3.right * tiltSpeed);
	}
}
