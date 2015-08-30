using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

using Util;

namespace Paths
{
	public class PathUtil
	{
		public static Vector3 GetPathDirectionAtPoint (Vector3[] points, int index, bool loop = false)
		{
			Vector3 prevDir, nextDir;
			return GetPathDirectionAtPoint (points, index, loop, Vector3.zero, out prevDir, out nextDir);
		}

		public static Vector3 GetPathDirectionAtPoint (Vector3[] points, int index, bool loop, out Vector3 prevDir, out Vector3 nextDir)
		{
			return GetPathDirectionAtPoint (points, index, loop, Vector3.zero, out prevDir, out nextDir);
		}
       
		// TODO rename to GetPathDirectionAtPoint or something else
		public static Vector3 GetPathDirectionAtPoint (Vector3[] points, int index, bool loop, Vector3 singlePointDir, out Vector3 prevDir, out Vector3 nextDir)
		{
			GetPointCountFn getPointCountFn = () => points.Length;
			GetPositionAtIndexFn getPositionAtIndexFn = (int pointIndex) => points [pointIndex];
			return DoGetPathDirectionAtPoint (index, loop, getPositionAtIndexFn, getPointCountFn, singlePointDir, out prevDir, out nextDir);

		}
		private delegate Vector3 GetPositionAtIndexFn (int index);
		private delegate int GetPointCountFn ();

		private static Vector3 DoGetPathDirectionAtPoint (int index, bool loop, GetPositionAtIndexFn getPosFunc, GetPointCountFn getPointCountFunc, Vector3 singlePointDir, out Vector3 prevDir, out Vector3 nextDir)
		{
			// Last point of looped path is a duplicate of the first point so we ignore it:
			int totalPointCount = getPointCountFunc ();
			int actualPointCount = loop ? totalPointCount - 1 : totalPointCount;
			
			if (actualPointCount < 2) {
				prevDir = nextDir = singlePointDir;
				return singlePointDir;
			}

			int lastPointIndex = actualPointCount - 1;

			if (index == (totalPointCount - 1) && loop) {
				// same as first point
				index = 0;
			}

			Vector3 dir;
			bool hasPrev = index > 0;
			bool hasNext = index < lastPointIndex;
			
			int prevIndex;
			int nextIndex;
			
			if (!hasPrev) {
				// First point; direction is from this to next
				// However, in loop mode the direction is from the last point to next:
				//              if (loop) {
				//                  dir = (points [index + 1] - points [points.Length - 1]).normalized;
				//              } else {
				//                  dir = (points [index + 1] - points [index]).normalized;
				//              }
				//              prevDir = dir;
				//              nextDir = dir;
				nextIndex = index + 1;
				prevIndex = loop ? lastPointIndex : index;
			} else if (!hasNext) {
				// Last point; direction is from previous to this
				// However, in loop mode direction is from previous to first point
				//              if (loop) {
				//                  dir = (points [0] - points [index - 1]).normalized;
				//              } else {
				//                  dir = (points [index] - points [index - 1]).normalized;
				//              }
				//              prevDir = dir;
				//              nextDir = dir;
				nextIndex = loop ? 0 : index;
				prevIndex = index - 1;
			} else {
				prevIndex = index - 1;
				nextIndex = index + 1;
			}
			
			// Average direction from previous to next
			Vector3 posAtIndex = getPosFunc (index);
			prevDir = (posAtIndex - getPosFunc (prevIndex)).normalized;
			nextDir = (getPosFunc (nextIndex) - posAtIndex).normalized;
			dir = ((nextDir + prevDir) / 2.0f).normalized;
			
			return dir;
		}


		public static Vector3 GetPathDirectionAtPoint (PathPoint[] points, int index, bool loop = false)
		{
			Vector3 prevDir, nextDir;
			return GetPathDirectionAtPoint (points, index, loop, Vector3.zero, out prevDir, out nextDir);
		}
        
		public static Vector3 GetPathDirectionAtPoint (PathPoint[] points, int index, bool loop, out Vector3 prevDir, out Vector3 nextDir)
		{
			return GetPathDirectionAtPoint (points, index, loop, Vector3.zero, out prevDir, out nextDir);
		}
        
		public static Vector3 GetPathDirectionAtPoint (PathPoint[] points, int index, bool loop, Vector3 singlePointDir, out Vector3 prevDir, out Vector3 nextDir)
		{
			GetPointCountFn getPointCountFn = () => points.Length;
			GetPositionAtIndexFn getPositionAtIndexFn = (int pointIndex) => points [pointIndex].Position;
			return DoGetPathDirectionAtPoint (index, loop, getPositionAtIndexFn, getPointCountFn, singlePointDir, out prevDir, out nextDir);
		}

	}
    
}
