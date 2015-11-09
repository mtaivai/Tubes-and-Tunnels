using UnityEngine;
using System.Collections;

[System.Serializable]
public class RotationLimit
{

	public bool limit;
	[Range(-359, 359)]
	public float
		min;

	[Range(-359, 359)]
	public float
		max;

	[Range(0, 360)]
	public float
		untrackableAngleThreshold = 20f;

	public float untrackableTimeThreshold = 3f;


	private float _untrackableTime = 0f;

	public RotationLimit ()
	{
	}
	public RotationLimit (bool limit, float min, float max, float untrackableAngleThreshold, float untrackableTimeThreshold)
	{
		this.limit = limit;
		this.min = min;
		this.max = max;
		this.untrackableAngleThreshold = untrackableAngleThreshold;
		this.untrackableTimeThreshold = untrackableTimeThreshold;
		this._untrackableTime = 0f;
	}
//	public RotationLimit WithOffset (float offs)
//	{
//		float offsMin = Mathf.Repeat (min + offs, 360f);
//		if (offsMin > 180f) {
//			offsMin -= 360f;
//		}
//		float offsMax = Mathf.Repeat (max + offs, 360f);
//		if (offsMax > 180f) {
//			offsMax -= 360f;
//		}
//
//		return new RotationLimit (limit, offsMin, offsMax, giveUpLimit);
//	}

	public bool Clamp (ref float value, float restValue, out bool untrackable)
	{
		bool clamped = false;
		untrackable = false;
		if (limit) {

			value = Mathf.Repeat (value, 360f);
			if (value > 180f) {
				value -= 360f;
			}
			if (min == max) {
				clamped = value != min;
				value = min;
			} else {


				float delta;
				if (value < min) {
					delta = min - value;
					value = min;
					clamped = true;
				} else if (value > max) {
					delta = value - max;
					value = max;
					clamped = true;
				} else {
					delta = 0f;
					clamped = false;
				}
				if (clamped) {
					if (delta > untrackableAngleThreshold) {
						if (_untrackableTime >= untrackableTimeThreshold) {
							untrackable = true;
							value = restValue;
						}
						_untrackableTime += Time.deltaTime;
					} else {
						value = Mathf.Clamp (value, min, max);
					}
				}

			}
		} else {
			clamped = true;
		}
		if (!clamped) {
			_untrackableTime = 0f;
		} 

		return clamped;
	}
}

[System.Serializable]
public class RotationLimits
{
	public RotationLimit x;
	public RotationLimit y;
	public RotationLimit z;


//	public RotationLimits WithOffset (Vector3 offs)
//	{
//		RotationLimits l = new RotationLimits ();
//		l.x = x.WithOffset (offs.x);
//		l.y = y.WithOffset (offs.y);
//		l.z = z.WithOffset (offs.z);
//		return l;
//	}

	public Vector3 Clamp (Vector3 rot, Vector3 restRot)
	{
		bool gaveUp = false;

		x.Clamp (ref rot.x, restRot.x, out gaveUp);
		if (!gaveUp) {
			y.Clamp (ref rot.y, restRot.y, out gaveUp);
			if (!gaveUp) {
				z.Clamp (ref rot.z, restRot.z, out gaveUp);
			}
		}
		if (gaveUp) {
			rot = restRot;
		}
			
		return rot;
	}

}

[ExecuteInEditMode]
public class PorriainenLookAt : MonoBehaviour
{

	public Transform lookAtTarget;
	public Transform[] eyes;
	public bool trackTarget;
	public float trackSpeed = 2.0f;
	public RotationLimits rotationLimits;
	public Vector3 rotationOffset;
	public bool useParentRotationOffset;

	private Quaternion[] originalRotations;

	void Reset ()
	{
		lookAtTarget = null;
		eyes = new Transform[0];
		trackTarget = false;
		trackSpeed = 2.0f;
		rotationLimits = new RotationLimits ();

		if (null != transform.parent) {
			rotationOffset = transform.parent.rotation.eulerAngles;
		} else {
			rotationOffset = Vector3.zero;
		}
	}

	void OnEnable ()
	{
		if (eyes != null) {
			originalRotations = new Quaternion[eyes.Length];
			for (int i = 0; i < eyes.Length; i++) {
				originalRotations [i] = eyes [i].localRotation;
			}
		}
	}
	void OnDisable ()
	{
		if (eyes != null && originalRotations != null && eyes.Length == originalRotations.Length) {
			for (int i = 0; i < originalRotations.Length; i++) {
				eyes [i].localRotation = originalRotations [i];
			}
		}
	}
	// Use this for initialization
	void Start ()
	{
	}
	void Update ()
	{
		float t = (Application.isPlaying) ? Time.deltaTime * trackSpeed : 1.0f;

		// Track eyes to target
		if (trackTarget && lookAtTarget != null) {
			Vector3 vLookAt = lookAtTarget.position;

			// Track head to target
			//Quaternion headLookRot = Quaternion.LookRotation (vLookAt, Vector3.up);
			//transform.rotation = Quaternion.Slerp (transform.rotation, headLookRot, t * 0.1f);

			foreach (Transform eye in eyes) {

				
				Vector3 lookDir = vLookAt - eye.position;

				Quaternion lookRot = Quaternion.LookRotation (lookDir, Vector3.up);

				Vector3 rotAngles = lookRot.eulerAngles;

				Quaternion parentRot = (null != eye.parent) ? eye.parent.rotation : Quaternion.identity;

				Vector3 rotOffs = useParentRotationOffset ? parentRot.eulerAngles : rotationOffset;

				Quaternion localTargetRot = Quaternion.Inverse (parentRot) * Quaternion.Euler (rotAngles + rotOffs);
				Vector3 localTargetRotAngles = localTargetRot.eulerAngles;

				for (int axis = 0; axis < 3; axis++) {
					localTargetRotAngles [axis] = Mathf.Repeat (localTargetRotAngles [axis], 360f);
				}

				localTargetRotAngles = rotationLimits.Clamp (localTargetRotAngles, Vector3.zero);
				localTargetRot = Quaternion.Euler (localTargetRotAngles);

				eye.localRotation = Quaternion.Slerp (eye.localRotation, localTargetRot, t);


			}
		} else {
			// Restore original pose
			for (int i = 0; i < eyes.Length; i++) {
				Transform eye = eyes [i];
				eye.localRotation = Quaternion.Slerp (eye.localRotation, originalRotations [i], t);
			}
		}
	}

	void OnDrawGizmos ()
	{
		if (null != lookAtTarget) {
			Vector3 vLookAt = lookAtTarget.position;
			foreach (Transform eye in eyes) {
				Vector3 lookDir = vLookAt - eye.position;
				Gizmos.color = Color.white;
				Gizmos.DrawLine (eye.position, eye.position + lookDir);
			}
		}
	}
}
