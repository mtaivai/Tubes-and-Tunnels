using UnityEngine;
using System.Collections;
using Paths;


public class TrackFollower : MonoBehaviour
{
	public PathSelector trackPathSelector;

	public GameObject target;
	public bool autoTargetPlayer;
	public float maxVelocity;
	public float targetPositionOffset;

	private float currentPathPos;
	private float currentVelocity;
	private PathPositionLookup trackPosLookup;

	void Reset ()
	{
		trackPathSelector = new PathSelector ();
		trackPosLookup = null;
		currentPathPos = 0.0f;
		targetPositionOffset = 0.0f;
		currentVelocity = 0.0f;
		maxVelocity = 5.0f;
		target = null;
		autoTargetPlayer = true;
	}
	// Use this for initialization
	void Start ()
	{
		if (autoTargetPlayer) {
			target = GameObject.FindGameObjectWithTag ("Player");
		}

		this.trackPosLookup = new PathPositionLookup (trackPathSelector);

//		this.currentPathPos = 0.0f;
		this.currentVelocity = 0.0f;

		// Get our initial position:
		currentPathPos = trackPosLookup.GetNearestPointPosition (transform.position);

		// Force initialization of lookup tables:
		//trackPosLookup.GetPathPointAtDistance (0.0f);
	}

	
	// Update is called once per frame
	void Update ()
	{
		Vector3 targetPos = target.transform.position;

		// Find the camera track position closest to the target object:
		float trackLength = trackPosLookup.GetTotalDistance ();
		float targetPathPos = trackPosLookup.GetNearestPointPosition (targetPos);

		// Try to stay about "targetPositionOffset" meters behind/in front of the target:
		targetPathPos += (targetPositionOffset / trackLength);

		// Do we need to pass zero on our way to the target position?
		// How do we know? If distance from currentPathPos to targetPathPos is greater than
		// currentPathPos + targetPathPos, the shortest path is to go via zero point
		float distToTargetDirectly = Mathf.Abs (currentPathPos - targetPathPos);
		float distToTargetViaZero;//
		if (currentPathPos > targetPathPos) {
			distToTargetViaZero = 1.0f - currentPathPos + targetPathPos;
		} else if (currentPathPos < targetPathPos) {
			distToTargetViaZero = 1.0f - targetPathPos + currentPathPos;
		} else {
			distToTargetViaZero = 1.0f;
		}


		if (distToTargetDirectly > distToTargetViaZero) {
			// Go via zero
			if (targetPathPos > currentPathPos) {
				// go towards negative target pos
				targetPathPos = -1.0f + targetPathPos;
			} else if (targetPathPos < currentPathPos) {
				targetPathPos = 1 + targetPathPos;
			}
		} else {
			// Go directly

		}
		float maxVelocityNormal = maxVelocity / trackLength;

		bool useSmoothDamp = true;
		if (useSmoothDamp) {
			float smoothTime = 1.0f; // One second

			float currentVelocityNormal = currentVelocity / trackLength;
			currentPathPos = Mathf.Repeat (
				Mathf.SmoothDamp (currentPathPos, targetPathPos, ref currentVelocityNormal, smoothTime, maxVelocityNormal),
				1.0f);
			currentVelocity = currentVelocityNormal * trackLength;
		} else {

//			Debug.LogFormat ("{0:f2}, {1:f2}", currentPathPos, targetPathPos);
			currentPathPos = Mathf.Repeat (Mathf.Lerp (currentPathPos, targetPathPos, Time.deltaTime * maxVelocityNormal), 1.0f);
		}

		Vector3 pos = trackPosLookup.GetPathPointAtDistance (currentPathPos * trackLength).Position;

		// Smoothen the position transform, just a bit (the "pathPos" is already smoothed)
		transform.position = Vector3.Lerp (transform.position, pos, Time.deltaTime * 25.0f);
	

//		Camera cam = GetComponent<Camera> ();
//		cam.transform.LookAt (target.transform);
	}
}
