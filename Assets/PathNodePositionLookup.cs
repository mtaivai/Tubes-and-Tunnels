// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

using Paths;



// k-d tree, a BST for 'k' dimensions, here k=3
public class KDTree<T> where T : IWithPosition
{

	private class PPComparer : IComparer<T>
	{
		private int axis;
		public PPComparer (int axis)
		{
			this.axis = axis;
		}
		public int Compare (T x, T y)
		{
			float d = x.GetPosition () [axis] - y.GetPosition () [axis];
			return d < 0f ? -1 : d > 0f ? 1 : 0;
		}
	}
	private class BuildContext
	{
		public PPComparer[] comparers;
		public BuildContext ()
		{
			comparers = new PPComparer[k];
			for (int i = 0; i < k; i++) {
				comparers [i] = new PPComparer (i);
			}
		}
	}
	private class FindNearestContext
	{
		public T currentBest;
		public float distToBest;
	}

	
	const int k = 3;
	private T value;
	private KDTree<T> leftChild;
	private KDTree<T> rightChild;


//	public override string ToString ()
//	{
//		return ToString ("");
//	}
//	private string ToString (string indent)
//	{
//		return string.Format ("({1:f},{2:f})\n{0}  Left: {3}\n{0}  Right: {4}", indent, 
//		               value.Position.x, value.Position.y, 
//		                      null != leftChild ? leftChild.ToString (indent + "  ") : "", 
//		                      null != rightChild ? rightChild.ToString (indent + "  ") : "");
//	}




	private KDTree ()
	{
	}

	public static KDTree<T> Build (T[] points)
	{
		BuildContext context = new BuildContext ();
		int count = points.Length;
		return Build (points, 0, count, 0, context);
	}

	private static KDTree<T> Build (T[] points, int firstIndex, int count, int depth, BuildContext context)
	{
		if (count < 1) {
			return new KDTree<T> ();
		}

		// Select axis based on depth so that axis cycles through all valid values
		int axis = depth % k;

		// Choose median as pivot element
		// Sort point list and choose median as pivot element

		// TODO quickselect here
		Array.Sort (points, firstIndex, count, context.comparers [axis]);

		int medianIndex = firstIndex + count / 2;

		T median = points [medianIndex];

		// Create node and construct subtrees
		KDTree<T> node = new KDTree<T> ();
		node.value = median;

		int leftCount = medianIndex - firstIndex; // before median
		int rightCount = count - leftCount - 1; // after median

		if (leftCount > 0) {
			node.leftChild = Build (points, firstIndex, leftCount, depth + 1, context);
		}
		if (rightCount > 0) {
			node.rightChild = Build (points, medianIndex + 1, rightCount, depth + 1, context);
		}
		return node;
	}

	public T FindNearest (Vector3 position)
	{
		FindNearestContext context = new FindNearestContext ();
		DoFindNearest (position, 0, context);
		return context.currentBest;
	}



	private void DoFindNearest (Vector3 position, int depth, FindNearestContext context)
	{
		// 1. Starting with the root node, the algorithm moves down the tree recursively, 
		//    in the same way that it would if the search point were being inserted (i.e. it goes 
		//    left or right depending on whether the point is lesser than or greater than the 
		//    current node in the split dimension).

		// TODO this first comparision may be unnecessary / actually slowing us down if chance of exact match is not likely!
		Vector3 valuePos = value.GetPosition ();
		if (valuePos == position) {
			context.currentBest = value;
			context.distToBest = 0f;
		} else {
			float dist = (position - valuePos).sqrMagnitude;

			int axis = depth % k;
			float splitValue = valuePos [axis];
			KDTree<T> childToGo = position [axis] < splitValue ? leftChild : rightChild;

			if (null != childToGo) {
				childToGo.DoFindNearest (position, depth + 1, context);
				//
				// 3. The algorithm unwinds the recursion of the tree, performing the following
				//    steps at each node:
				//
				//    3.1) If the current node is closer than the current best, then it becomes the current best.
				//
				if (dist < context.distToBest) {
					context.currentBest = value;
					context.distToBest = dist;
				}

				//    3.2) The algorithm checks whether there could be any points on the other side of the splitting
				//       plane that are closer to the search point than the current best. In concept, this is done
				//       by intersecting the splitting hyperplane with a hypersphere around the search point that
				//       has a radius equal to the current nearest distance. Since the hyperplanes are all 
				//       axis-aligned this is implemented as a simple comparison to see whether the difference
				//       between the splitting coordinate of the search point and current node is lesser than the
				//       distance (overall coordinates) from the search point to the current best.


				KDTree<T> otherChild = ReferenceEquals (childToGo, leftChild) ? rightChild : leftChild;
				if (null != otherChild) {
					// TODO THIS may not work, e.g. we may dive too eagerly to other child?
					float dSearchToCurrent = (valuePos [axis] - position [axis]);
					float dSearchToCurrent2 = dSearchToCurrent * dSearchToCurrent; // we're using square magnitudes
					if (dSearchToCurrent2 < context.distToBest) {
						// 3.2.1) If the hypersphere crosses the plane, there could be nearer points on the other side
						//        of the plane, so the algorithm must move down the other branch of the tree from the
						//        current node looking for closer points, following the same recursive process as the
						//        entire search.
						otherChild.DoFindNearest (position, depth + 1, context);
					}
					//
					// 3.2.2) If the hypersphere doesn't intersect the splitting plane, then the algorithm continues
					//        walking up the tree, and the entire branch on the other side of that node is eliminated.

				}
			} else {
				// 2. Once the algorithm reaches a leaf node, it saves that node point as the "current best"
				if (null == context.currentBest) {
					context.currentBest = value;
					context.distToBest = dist;
				}
			}
		}
	}

}

// TODO if we don't have distances already, let's calculate only squareDistance and
// get sqr per request
public class PathPositionLookup
{
	//		private PathWithDataId pathSelection;
	private IPathData pathData;
	private long currentStatusToken;
	private PathNode[] previousNodeLookup = null;
	private int pathNodeCount = 0;

	private int stepsPerPoint = 10; // With 10 steps per point we get cache hit ratio of over 95 %
	
	private PathNode _firstPathNode;
	private float _totalDistance = 0.0f;

	private KDTree<PathNode> nearestTree = null;

	// TODO do we need statistics in here?
	private int cacheMissCount = 0;
	private int lookupCount = 0;
	
	public PathPositionLookup (IPathData pathData)
	{
		this.pathData = pathData;
	}
	public PathPositionLookup (PathSelector pathSelector)
	{
		this.pathData = pathSelector.PathData;
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
	
	public PathNode GetFirstPathNode ()
	{
		if (null == _firstPathNode || pathData.GetStatusToken () != currentStatusToken) {

			bool loopPath = pathData.GetPathInfo ().IsLoop ();
			PathPoint[] pathPoints = pathData.GetAllPoints ();
			pathNodeCount = pathPoints.Length;

			this._firstPathNode = PathNode.BuildLinkedList (pathPoints, loopPath);
			if (null == _firstPathNode) {
				return null;
			}

			int flags = pathData.GetOutputFlags ();
			bool generateDistances = !PathPoint.IsFlag (flags, PathPoint.DISTANCES);

			float distFromBegin = 0.0f;
			if (generateDistances) {
				_firstPathNode.Point.DistanceFromPrevious = 0.0f;
				_firstPathNode.Point.DistanceFromBegin = 0.0f;
			}
			PathNode pathNode = _firstPathNode.NextIfNotLast;

			PathNode lastNode = pathNode;

			while (pathNode != null) {
				PathPoint pp = pathNode;
				Vector3 d = pp.Position - pathNode.Previous.Point.Position;
				if (generateDistances) {
					// We need to calculate magnitude because we need the total distance (sqrMagnitude won't do it)
					float distFromPrev = d.magnitude;
					distFromBegin += distFromPrev;
					pp.DistanceFromPrevious = distFromPrev;
					pp.DistanceFromBegin = distFromBegin;
				}
				// Generate directions suitable for our purposes:
				pathNode.Previous.Point.Direction = d.normalized;
				lastNode = pathNode;
				pathNode = pathNode.NextIfNotLast;
			}

			if (loopPath) {
				// Direction of the last point == direction of the first point
				_firstPathNode.Previous.Point.Direction = _firstPathNode.Point.Direction;
			} else {
				// Direction of the last point == direction of the second last point
				lastNode.Point.Direction = lastNode.Previous.Point.Direction;
			}

			this._totalDistance = (null != lastNode) ? lastNode.Point.DistanceFromBegin : 0.0f;
		}
		return _firstPathNode;
	}
	public float GetTotalDistance ()
	{
		// Make sure that distance is updated:
		GetFirstPathNode ();
		
		return _totalDistance;
	}
	
	//		public int GetNearestNextPointIndex(Vector3 position) {
	//			// TODO this is _very_ inefficent!
	//			float shortestDist;
	//			int neatestIndex;
	//			PathPoint[] points = GetPathPoints();
	//			for (int i = 0; i < points.Length; i++) {
	//				float distFromPos = Vector3.Magnitude(points[i].Position - position);
	//				if (distFromPos
	//
	//			}
	//		}
	//
	public PathPoint GetPathPointAtDistance (float d)
	{
		float actualDistance;
		return GetPathPointAtDistance (d, out actualDistance);
	}
	public PathPoint GetPathPointAtDistance (float d, out float actualDistance)
	{
		lookupCount++;

//		PathNode firstPathNode = GetFirstPathNode ();
		float totalDistance = GetTotalDistance ();
		
//		int pointCount = pathPoints.Length;
//		int lastPointIndex = pointCount - 1;
		
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
		PathNode prevNode = LookupPreviousNode (t);
		if (prevNode == null) {
			// TODO WHAT TO DO?
			Debug.LogWarning ("WHOA! Got null from LookupPreviousNode()");
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
		//PathPoint prevPt, nextPt;
		float distFromPrevPtToCurrentPos;
		PathNode nextNode;
		
		bool foundPrevPt = false;
		int correctionIterationsRemaining = pathNodeCount - 1;
		do {
			nextNode = prevNode.Next;

			distFromPrevPtToCurrentPos = d - prevNode.Point.DistanceFromBegin;
			
			if (distFromPrevPtToCurrentPos > nextNode.Point.DistanceFromPrevious) {
				// Move to next point:
				prevNode = prevNode.Next;//Repeat (prevPtIndex + 1, lastPointIndex);//(prevPtIndex >= (pathPoints.Length - 1)) ? 0 : prevPtIndex + 1;
				cacheMissCount ++;
			} else {
				// FOUND
				// TODO Fix the cache?
				SetPreviousNode (t, prevNode);
				
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

		PathPoint nextPt = nextNode;
		PathPoint prevPt = prevNode;
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
	
	public PathNode LookupPreviousNode (float t)
	{
		if (null == previousNodeLookup || pathData.GetStatusToken () != currentStatusToken) {
			DoBuildDistanceLookUpTable ();
			currentStatusToken = pathData.GetStatusToken ();
		}
		
		t = Mathf.Repeat (t, 1.0f);
		
		int tableIndex = Mathf.FloorToInt (t * (float)(previousNodeLookup.Length));
		PathNode prevNode = previousNodeLookup [tableIndex];
		return prevNode;
	}
	
	private void SetPreviousNode (float t, PathNode node)
	{
		t = Mathf.Repeat (t, 1.0f);
		int tableIndex = Mathf.FloorToInt (t * (float)(previousNodeLookup.Length));
		if (tableIndex >= previousNodeLookup.Length) {
			// TODO throw an exception!
			Debug.LogError ("Whoa! Calculated tableIndex >= previousNodeLookup.Length");
		} else {
			previousNodeLookup [tableIndex] = node;
		}
	}
	
	private void DoBuildDistanceLookUpTable ()
	{
		PathNode firstNode = GetFirstPathNode ();
		int totalPointCount = pathNodeCount;
		
		int tableSize = stepsPerPoint * totalPointCount;
		previousNodeLookup = new PathNode[tableSize];
		
		// Table: distance --> previous point index
		
		float totalDistance = GetTotalDistance ();

		PathNode currentNode = firstNode;

//		int nextLookupStartIndex = 0;
		for (int i = 0; i < tableSize; i++) {
			float d = (float)i / (float)(tableSize);
			float currentDist = totalDistance * d;
			// Get point before "currentDist"
			
//			int pointIndex = -1;
			//PathNode prevNode = null;
			while (currentNode != null) {
				PathPoint pp = currentNode;

				// TODO this fails if we don't have DistanceFromBegin attribute. Consider about
				// an option to calculate the distance in here!
				float ppDist = pp.DistanceFromBegin;
				// TODO this is inaccurate:
				if (ppDist == currentDist) {
					// Exact match; considered as "previous"
					//pointIndex = j;
					break;
				} else if (ppDist > currentDist) {
					// Previous point is what we're looking for
					currentNode = currentNode.Previous;
					break;
				} else {
					currentNode = currentNode.Next;
				}
			}
			// TODO what if pointIndex is not found?
			previousNodeLookup [i] = currentNode;
		}
	}
//	public float GetPositionOfPointAtIndex (int index)
//	{
//		return GetPathPoints () [index].DistanceFromBegin / GetTotalDistance ();
//	}

	public float GetNearestPointPosition (Vector3 position)
	{
//		PathNode firstNode = GetFirstPathNode ();

		PathNode nearestNode = GetNearestPathNode (position);
		PathPoint nearestPoint = nearestNode;

		Vector3 toNearestPointVector = nearestPoint.Position - position;

		// We known the nearest point, let's project the "position" on it's normal:
		float dot = Vector3.Dot (nearestPoint.Direction, toNearestPointVector);

		if (dot >= 0.0f) {
			// Is behind, use the previous point instead
			// TODO should we iterate until we actually find the first point behind the pos?
			if (nearestNode.IsFirst) {
				if (pathData.GetPathInfo ().IsLoop ()) {
					// Second last node is our nearest (last node's position == first node's position)
					nearestNode = nearestNode.Previous.Previous;
				}
			} else {
				nearestNode = nearestNode.Previous;
			}
			nearestPoint = nearestNode;
			toNearestPointVector = nearestPoint.Position - position;
		} else {
			// Is in front of the nearest point

		}
//		Vector3 pathDir = nearestPoint.Direction;
		Vector3 projection = Vector3.Project (toNearestPointVector, nearestPoint.Direction);
		// Our project is negative but its magnitude is still positive
		float distFromPrevPoint = projection.magnitude;
//
		Debug.DrawLine (position, nearestPoint.Position - projection, Color.red);
		Debug.DrawLine (position, nearestPoint.Position, Color.yellow);
		return (nearestPoint.DistanceFromBegin + distFromPrevPoint) / GetTotalDistance ();
	}
	public PathNode GetNearestPathNode (Vector3 position)
	{
		// TODO use k-d tree

		if (null == nearestTree) {
			nearestTree = KDTree<PathNode>.Build (GetFirstPathNode ().ToArray ());
		}

		PathNode nearestNode = nearestTree.FindNearest (position);
//			node = GetFirstPathNode ();
//			float nearestDistance = (position - node.Point.Position).magnitude;
//			while (null != (node = node.NextIfNotLast)) {
//				float d = (position - node.Point.Position).magnitude;
//				if (d < nearestDistance) {
//					nearestNode = node;
//					nearestDistance = d;
//					if (Mathf.Approximately (0f, d)) {
//						// Exact match (well, almost exact)
//						break;
//					}
//				}
//			}
		return nearestNode;
	}
}
