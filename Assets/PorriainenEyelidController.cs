using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class PorriainenEyelidController : MonoBehaviour
{

	public Transform eyelid;
	public Transform eye;


	public Vector3 fullyClosedEyelidAngle;
	public Vector3 fullyOpenedEyelidAngle;

	public bool closeEyes;

	public float eyelidSpeed = 10f;
	public float blinkSpeed = 15f;

	public float minBlinkInterval = 0.5f;
	public float maxBlinkInterval = 10f;
	//public float blinkRandomness = 0.5f;

	private float tSinceLastBlink = 0f;
//	private Quaternion[] eyelidOriginalRotations;

	private int blinkState = 0; // 0 == none, 1 == closing, 2 == opening
	private float blinkT = 0f;
	private float nextBlinkInterval = 0f;

	//private Vector3 normalEyelidAngle; // -14.07, -11.36, 0
	private Quaternion normalEyelidRotation;

	void OnEnable ()
	{
//
//		if (eyelid != null) {
//			eyelidOriginalRotations = new Quaternion[1];
//			eyelidOriginalRotations [i] = eyelid.localRotation;
//		}
		if (null != eyelid) {
			normalEyelidRotation = eyelid.localRotation;
		}
	}
	void OnDisable ()
	{
		if (null != eyelid) {
			eyelid.localRotation = normalEyelidRotation;
		}
	}

	// Use this for initialization
	void Start ()
	{
		nextBlinkInterval = Random.Range (minBlinkInterval, maxBlinkInterval);
	}
	
	// Update is called once per frame
	void Update ()
	{
		float t = Application.isPlaying ? Time.deltaTime : 1.0f;

		if (closeEyes) {
			eyelid.localRotation = Quaternion.Lerp (eyelid.localRotation, Quaternion.Euler (fullyClosedEyelidAngle), t * eyelidSpeed);
			tSinceLastBlink = 0f;
			blinkState = 0;
		} else {

			//Vector3 currentEyelidAngle = eyelid.localRotation.eulerAngles;

			Quaternion targetRot = normalEyelidRotation;
			Vector3 targetAngle = targetRot.eulerAngles;
			if (Application.isPlaying) {
				DoUpdateBlink (targetAngle);
			}
			if (blinkState == 0) {
				if (null != eye) {
					Vector3 eyeAngles = eye.localRotation.eulerAngles;
					Debug.Log ("EyeRot: " + eyeAngles);

					// Lift upper eyelid if necessary

					int eyeTiltAxis = 0; // x
					float eyeTiltThreshold = -7f;
					float eyeTiltMin = -30f;

					float eyeTilt = Mathf.Repeat (eyeAngles [eyeTiltAxis], 360f);
					if (eyeTilt >= 180) {
						eyeTilt -= 360f;
					}


					if (eyeTilt <= eyeTiltThreshold) {
						// Fully open eyelid
						float min = eyeTiltMin;
						float tt = (eyeTilt - eyeTiltThreshold) / (min - eyeTiltThreshold);
						targetRot = Quaternion.Lerp (normalEyelidRotation, Quaternion.Euler (fullyOpenedEyelidAngle), tt);
					} else {
						targetRot = normalEyelidRotation;
					}
				}
				eyelid.localRotation = Quaternion.Lerp (eyelid.localRotation, targetRot, t * eyelidSpeed);
			}
		}
	}

	private bool DoUpdateBlink (Vector3 normalAngle)
	{
		float t = Application.isPlaying ? Time.deltaTime : 1.0f;
		Vector3 targetAngle = normalAngle;
		switch (blinkState) {
		case 0:
			// Not currently blinking
			tSinceLastBlink += t;
			if (tSinceLastBlink >= nextBlinkInterval || tSinceLastBlink >= maxBlinkInterval) {
				// Start blink
				blinkState = 1;
				blinkT = 0f;
				
				nextBlinkInterval = Random.Range (minBlinkInterval, maxBlinkInterval);
			}
			break;
		case 1:
			// Closing for blink:
			if (blinkT >= 1.0f) {
				// closed; start opening
				blinkState = 2;
				blinkT = 0f;
			} else {
				targetAngle = fullyClosedEyelidAngle;
				blinkT += t * blinkSpeed;
			}
			break;
		case 2:
			// Opening from blink
			if (blinkT >= 1.0f) {
				// Open again, end blink cycle
				blinkState = 0;
				targetAngle = normalAngle;
				tSinceLastBlink = 0f;
				blinkT = 0f;
			} else {
				targetAngle = normalAngle;
				blinkT += t * blinkSpeed;
			}
			break;
		}
		if (blinkState > 0) {
			Quaternion normalRot = Quaternion.Euler (normalAngle);
			eyelid.localRotation = Quaternion.Lerp (normalRot, Quaternion.Euler (targetAngle), blinkT);
		}
		return blinkState != 0;
	}
}
