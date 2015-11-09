using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Paths;


public class PathFlyer : MonoBehaviour
{

	public PathSelector pathSelection;

	public float constantSpeed = 5.56f; // m/s = 20 km/h

	public float currentDistance = 0.0f;
	private PathPositionLookup ppLookup;
	public float maxSpeed = 20f;

	public float currentSpeed = 0f;

	public bool applyRotation = true;
//	public int prevPtIndex;

	// Use this for initialization
	void Start ()
	{
		//currentDistance = 0.0f;
		ppLookup = new PathPositionLookup (pathSelection.PathData);

		PathPoint pp = ppLookup.GetPathPointAtDistance (currentDistance, out currentDistance);
		Paths.Path path = pathSelection.Path;
		transform.position = path.transform.TransformPoint (pp.Position);

		Quaternion rot = transform.rotation;
		rot.SetLookRotation (pp.Direction);
		transform.rotation = rot;

	}

	void OnDrawGizmos ()
	{
	}
	
	void FixedUpdate ()
	{
		float thrust = Input.GetAxis ("Vertical");
		float targetSpeed = Mathf.Min (maxSpeed, constantSpeed + thrust * maxSpeed);
		currentSpeed = Mathf.Lerp (currentSpeed, targetSpeed, Time.deltaTime * 10f);
		if (Mathf.Abs (currentSpeed) < 0.01f) {
			currentSpeed = 0f;
		}
		currentDistance += Time.deltaTime * currentSpeed;
//		if (currentDistance > totalDistance) {
//			currentDistance = currentDistance % totalDistance;
//		}

		PathPoint pp = ppLookup.GetPathPointAtDistance (currentDistance, out currentDistance);

		Paths.Path path = pathSelection.Path;
		transform.position = path.transform.TransformPoint (pp.Position);

	

		if (applyRotation) {
			// Look n units ahead
			PathPoint lookAtPp = ppLookup.GetPathPointAtDistance (currentDistance + 5f);
			Vector3 lookDir = (lookAtPp.Position - pp.Position).normalized;

			Quaternion rot = transform.rotation;
			rot.SetLookRotation (lookDir);
			transform.rotation = Quaternion.Slerp (transform.rotation, rot, Time.deltaTime * Mathf.Max (5f, currentSpeed / 10f));
		}

//		transform.rotation.SetLookRotation (pp.Direction);
		// METHOD2 for position: SMOOTH

//		Camera cam = GetComponent<Camera> ();
//		if (null != cam && cam.isActiveAndEnabled) {

//			// Look ahead!
//			// TODO look n points forward?
//			Vector3 nextDir = (nextAfterNextPt.Position - nextPt.Position).normalized;
//
//			// TODO distFromPrevPtToCurrentPos is inaccurate:
//			float tBetweenPrevAndNextPt = Mathf.Clamp01 (distFromPrevPtToCurrentPos / nextPt.DistanceFromPrevious);
//			Quaternion targetLookRot = Quaternion.LookRotation (Vector3.Lerp (dir, nextDir, tBetweenPrevAndNextPt));
//
//			//cam.transform.rotation = Quaternion.Lerp (cam.transform.rotation, targetLookRot, Time.deltaTime * speed / 5.0f);
//			cam.transform.rotation = targetLookRot;
//
////				cam.transform.LookAt (nextPos);
//		}



	}


}

