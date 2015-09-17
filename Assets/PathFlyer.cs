using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

using Paths;


public class PathFlyer : MonoBehaviour
{

//	public Path path;
	public PathSelector pathSelection;

	public float speed = 5.56f; // m/s = 20 km/h

//	private PathPoint[] pathPoints;

	public float currentDistance = 0.0f;
//	private float totalDistance;
	PathPositionLookup ppLookup;

	public int prevPtIndex;

	public float cacheHitRatio = 0.0f;

	//List<Vector3> flyPath = new List<Vector3> ();

	// Use this for initialization
	void Start ()
	{
//		IPathData pathData = path.GetDefaultDataSet ();
//		pathPoints = pathData.GetAllPoints ();
		currentDistance = 0.0f;
//		totalDistance = pathData.GetTotalDistance ();
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
//		for (int i = 1; i < flyPath.Count; i++) {
//
//			Gizmos.DrawLine (flyPath [i - 1], flyPath [i]);
//		}
	}
	
	void FixedUpdate ()
	{



//		currentDistance += Time.deltaTime * speed; // m/s

//		float previousDistance = this.currentDistance;


		currentDistance += Time.deltaTime * speed;
//		if (currentDistance > totalDistance) {
//			currentDistance = currentDistance % totalDistance;
//		}

		PathPoint pp = ppLookup.GetPathPointAtDistance (currentDistance, out currentDistance);

		int hitCount = ppLookup.GetLookupCount () - ppLookup.GetCacheMissCount ();
		this.cacheHitRatio = (float)hitCount / (float)ppLookup.GetLookupCount () * 100.0f;

		Paths.Path path = pathSelection.Path;
		transform.position = path.transform.TransformPoint (pp.Position);
		//flyPath.Add (transform.position);

		Quaternion rot = transform.rotation;
		rot.SetLookRotation (pp.Direction);
		transform.rotation = Quaternion.Slerp (transform.rotation, rot, Time.deltaTime * speed);
//		transform.rotation = rot;

		// TODO EXPERIMENTAL
		GameObject rollingBall = GameObject.Find ("Rolling Ball");
		if (null != rollingBall) {
			transform.LookAt (rollingBall.transform.position);
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

