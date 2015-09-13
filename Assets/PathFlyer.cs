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

	public class PathPositionLookup
	{
//		private PathWithDataId pathSelection;
		private IPathData pathData;
		private long currentStatusToken;
		private int[] previousIndexLookup = null;
		private int stepsPerPoint = 10; // With 10 steps per point we get accuracy of over 95 %

		private PathPoint[] _pathPoints;
		private float _totalDistance = 0.0f;

		private int cacheMissCount = 0;
		private int lookupCount = 0;

		public PathPositionLookup (IPathData pathData)
		{
			this.pathData = pathData;
		}
		public int GetLookupCount ()
		{
			return lookupCount;
		}
		public int GetCacheMissCount ()
		{
			return cacheMissCount;
		}


		private static int Repeat (int val, int max)
		{
			if (val > max) {
				return val % (max + 1);
			} else {
				return val;
			}
		}

		public PathPoint[] GetPathPoints ()
		{
			if (null == _pathPoints || pathData.GetStatusToken () != currentStatusToken) {
				_pathPoints = pathData.GetAllPoints ();
				// TODO generate required components!
				int flags = pathData.GetOutputFlags ();
				if (!PathPoint.IsFlag (flags, PathPoint.DISTANCES) && _pathPoints.Length > 0) {

					_pathPoints [0].DistanceFromPrevious = 0.0f;
					_pathPoints [0].DistanceFromBegin = 0.0f;
					float distFromBegin = 0.0f;
					for (int i = 1; i < _pathPoints.Length; i++) {
						float distFromPrev = (_pathPoints [i].Position - _pathPoints [i - 1].Position).magnitude;
						distFromBegin += distFromPrev;
						_pathPoints [i].DistanceFromPrevious = distFromPrev;
						_pathPoints [i].DistanceFromBegin = distFromBegin;

					}
				}

				this._totalDistance = (_pathPoints.Length > 0) ? _pathPoints [_pathPoints.Length - 1].DistanceFromBegin : 0.0f;
			}
			return _pathPoints;
		}
		public float GetTotalDistance ()
		{
			// Make sure that distance is updated:
			GetPathPoints ();

			return _totalDistance;
		}
		public PathPoint GetPathPointAtDistance (float d)
		{
			float actualDistance;
			return GetPathPointAtDistance (d, out actualDistance);
		}
		public PathPoint GetPathPointAtDistance (float d, out float actualDistance)
		{
			lookupCount++;

			PathPoint[] pathPoints = GetPathPoints ();
			float totalDistance = GetTotalDistance ();

			int pointCount = pathPoints.Length;
			int lastPointIndex = pointCount - 1;

			// Rotate / repeat the distance
			if (d > totalDistance || d < -totalDistance) {
				d = d % totalDistance;
			}
			if (d < 0.0f) {
				d = totalDistance + d;
			}

			actualDistance = d;

			float t = d / totalDistance;

			PathPoint pp;
			int prevPtIndex = LookupPreviuosPointIndex (t);
			if (prevPtIndex < 0) {
				// TODO WHAT TO DO?
				Debug.LogWarning ("WHOA! Got negative from LookupPreviuosPointIndex()");
				pp = new PathPoint ();
				return pp;
			}


			// The pointIndex is the _previous_ point before the target distance
			
			// TODO what if we return too big point????
			//prevPtIndex = Repeat(prevPtIndex + 1, pathPoints.Length - 1); // (prevPtIndex < pathPoints.Length - 1) ? prevPtIndex + 1 : 0;

			// METHOD 1: use exact direction
			// Don't use pp.Direction since it may be an average, i.e. not an exact
			// direction between to points

			// Look up the correct point:
			// Check if we got right index (the lookup table might be a bit inaccurate)
			// TODO why it's inaccurate?
			PathPoint prevPt, nextPt;
			float distFromPrevPtToCurrentPos;
			int nextPtIndex;

			bool foundPrevPt = false;
			int correctionIterationsRemaining = lastPointIndex;
			do {
				prevPt = pathPoints [prevPtIndex];
				nextPtIndex = Repeat (prevPtIndex + 1, lastPointIndex);//prevPtIndex >= (pathPoints.Length - 1) ? 0 : prevPtIndex + 1;
				nextPt = pathPoints [nextPtIndex];

				distFromPrevPtToCurrentPos = d - prevPt.DistanceFromBegin;


				if (distFromPrevPtToCurrentPos > nextPt.DistanceFromPrevious) {
					// Move to next point:
					prevPtIndex = Repeat (prevPtIndex + 1, lastPointIndex);//(prevPtIndex >= (pathPoints.Length - 1)) ? 0 : prevPtIndex + 1;
					cacheMissCount ++;
				} else {
					// FOUND
					// TODO Fix the cache?
					SetPreviousPointIndex (t, prevPtIndex);

					foundPrevPt = true;
					correctionIterationsRemaining = 0;
				}
			} while (!foundPrevPt && --correctionIterationsRemaining > 0);

			if (!foundPrevPt) {
				// TODO WHAT TO DO?

				Debug.LogError ("WHOA! Didn't found previous point!!!");
			}
			// Extrapolate forwards from the found point
//			int nextAfterNextPtIndex = Repeat (nextPtIndex + 1, lastPointIndex);//nextPtIndex >= (pathPoints.Length - 1) ? 0 : nextPtIndex + 1;
//				PathPoint nextAfterNextPt = pathPoints [nextAfterNextPtIndex];
			
			Vector3 targetPos = nextPt.Position;
			
			Vector3 dir = (targetPos - prevPt.Position).normalized;
			
			// METHOD 2: Lerp direction between previous and next
			//			PathPoint nextPoint = pointIndex >= (pathPoints.Length - 1) ? pathPoints [0] : pathPoints [pointIndex + 1];
			//			float distBetweenPoints = (nextPoint.Position - pp.Position).magnitude;
			//			Vector3 dir = Vector3.Lerp (pp.Direction, nextPoint.Direction, (distFromPos / distBetweenPoints));
			
			
			// METHOD1 for position: accurate path follow
			Vector3 pos = prevPt.Position + dir * distFromPrevPtToCurrentPos;
//				Vector3 dir = prevPt.Direction;

			Vector3 currentDir = Vector3.Lerp (prevPt.Direction, nextPt.Direction, distFromPrevPtToCurrentPos / nextPt.DistanceFromPrevious);

			pp = new PathPoint (pos, currentDir);

			
			return pp;
		}

		public int LookupPreviuosPointIndex (float t)
		{
			if (null == previousIndexLookup || pathData.GetStatusToken () != currentStatusToken) {
				DoBuilLookUpTable ();
				currentStatusToken = pathData.GetStatusToken ();
			}

			t = Mathf.Repeat (t, 1.0f);

			int tableIndex = Mathf.FloorToInt (t * (float)(previousIndexLookup.Length));
			int prevIndex = previousIndexLookup [tableIndex];
			return prevIndex;
		}

		private void SetPreviousPointIndex (float t, int ptIndex)
		{
			t = Mathf.Repeat (t, 1.0f);
			int tableIndex = Mathf.FloorToInt (t * (float)(previousIndexLookup.Length));
			if (tableIndex >= previousIndexLookup.Length) {
				// TODO throw an exception!
				Debug.Log ("HUH: t=" + t + ", ptIndex=" + ptIndex + ", tableIndex=" + tableIndex);
			} else {
				previousIndexLookup [tableIndex] = ptIndex;
			}
		}


		private void DoBuilLookUpTable ()
		{
			PathPoint[] pathPoints = GetPathPoints ();

			int totalPointCount = pathPoints.Length;

			int tableSize = stepsPerPoint * totalPointCount;
			previousIndexLookup = new int[tableSize];

			// Table: distance --> previous point index

			float totalDistance = GetTotalDistance ();

			int nextLookupStartIndex = 0;
			for (int i = 0; i < tableSize; i++) {
				float d = (float)i / (float)(tableSize);
				float currentDist = totalDistance * d;
				// Get point before "currentDist"

				int pointIndex = -1;
				for (int j = nextLookupStartIndex; j < totalPointCount; j++) {
					PathPoint pp = pathPoints [j];

					// TODO this fails if we don't have DistanceFromBegin attribute. Consider about
					// an option to calculate the distance in here!
					float ppDist = pp.DistanceFromBegin;
					// TODO this is inaccurate:
					if (ppDist == currentDist) {
						// Exact match; considered as "previous"
						pointIndex = j;
						break;
					} else if (ppDist > currentDist) {
						// Previous point is what we're looking for
						pointIndex = j - 1;
						break;
						
					}
				}
				// TODO what if pointIndex is not found?
				previousIndexLookup [i] = pointIndex;
				nextLookupStartIndex = pointIndex;
			}

		}
	}
}
