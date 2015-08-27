using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Paths
{
	[PathModifier(requiredInputFlags=PathPoint.NONE, 
                  processCaps=PathPoint.NONE,
                  passthroughCaps=PathPoint.NONE, 
                  generateCaps=PathPoint.ALL)]
	public class IncludePathModifier : AbstractPathModifier
	{
		private int includedPathRefIndex = -1;

		public int includePosition; // "include at index"
		private int _inputPointCount;

		public bool removeDuplicates; // "smart include"

		public bool alignFirstPoint;

		public Vector3 includedPathPosOffset;
		private Vector3 _currentIncludedPathPosOffset;
//		Vector3 includePosition

		protected override void OnReset ()
		{
			IPathModifierContainer container = GetContainer ();
			if (null != container) {
				SetIncludedPath (container.GetReferenceContainer (), null);
			}

			includePosition = -1;
			_inputPointCount = 0;
			removeDuplicates = true;
			alignFirstPoint = false;
			includedPathPosOffset = Vector3.zero;
			_currentIncludedPathPosOffset = includedPathPosOffset;

		}
		public override void OnSerialize (Serializer store)
		{
			store.Property ("includedPathRefIndex", ref includedPathRefIndex);
			store.Property ("includePosition", ref includePosition);
			store.Property ("removeDuplicates", ref removeDuplicates);
			store.Property ("alignFirstPoint", ref alignFirstPoint);
			store.Property ("includedPathPosOffset", ref includedPathPosOffset);
		}

		public override void OnDetach ()
		{
			SetIncludedPath (GetContainer ().GetReferenceContainer (), null);
		}

		public int GetCurrentInputPointCount ()
		{
			return _inputPointCount;
		}
		public Vector3 GetCurrentIncludedPathPosOffset ()
		{
			return _currentIncludedPathPosOffset;
		}
		public Path GetIncludedPath (IReferenceContainer refContainer)
		{
			Path refPath;
			if (includedPathRefIndex >= 0 && includedPathRefIndex < refContainer.GetReferentCount ()) {

				object refObj = refContainer.GetReferent (includedPathRefIndex);
				if (null == refObj) {
					Debug.LogWarning ("Referent #" + includedPathRefIndex + " is missing!");
					refPath = null;
				} else if (!(refObj is Path)) {
					Debug.LogWarning ("Referent #" + includedPathRefIndex + " is not Path (it's " + refObj.GetType () + ")");
					refPath = null;
				} else {
					refPath = (Path)refObj;
				}
			} else {
				refPath = null;
			}
			return refPath;
		}

		public void SetIncludedPath (IReferenceContainer refContainer, Path includedPath)
		{
			if (null != refContainer) {
				if (includedPathRefIndex >= 0 && includedPathRefIndex < refContainer.GetReferentCount ()) {
					// Replace or remove existing
					if (includedPath != null) {
						refContainer.SetReferent (includedPathRefIndex, includedPath);
					} else {
						// null includedPath; remove the referent
						refContainer.RemoveReferent (includedPathRefIndex);
					}
				} else if (null != includedPath) {
					// Add new
					includedPathRefIndex = refContainer.AddReferent (includedPath);
				}
			}
			if (null == includedPath) {
				includedPathRefIndex = -1;
			}
		}

		public override PathPoint[] GetModifiedPoints (PathPoint[] points, PathModifierContext context)
		{
			this._inputPointCount = points.Length;

			int ppFlags = GetOutputFlags (context);

			PathPoint[] includedPoints;
			Path includedPath = GetIncludedPath (context.PathModifierContainer.GetReferenceContainer ());
			if (null != includedPath) {
				includedPoints = includedPath.GetAllPoints ();
				ppFlags &= includedPath.GetOutputFlags ();
			} else {
				includedPoints = new PathPoint[0];
			}

			if (includePosition < 0 || includePosition > points.Length) {
				includePosition = points.Length;
			}

			int includedPointCount = includedPoints.Length;

			//int originalPathIncludePointCount;
			int includedPointsOffset = 0;
			int originalPathHeadPointCount;
			int originalPathTailPointCount;
			int originalPathTailSkipOffset = 0;


			Vector3 includedPathFirstPointOffset;

			if (points.Length < 1) {
				// empty original path
				originalPathHeadPointCount = originalPathTailPointCount = 0;
				includedPathFirstPointOffset = includedPathPosOffset;
			} else if (includedPointCount < 1) {
				// Nothing to include; keep all originals
				originalPathHeadPointCount = points.Length;
				originalPathTailPointCount = 0;
				includedPathFirstPointOffset = includedPathPosOffset;
			} else if (includePosition == 0) {
				// Included in the begin, remove first point of the original path if it's zero
				// (to prevent duplicate points)
				originalPathHeadPointCount = 0;
				includedPathFirstPointOffset = includedPathPosOffset;
				if (removeDuplicates && points [0].Position == Vector3.zero) {
					originalPathTailPointCount = points.Length - 1;
					originalPathTailSkipOffset = 1;
				} else {
					originalPathTailPointCount = points.Length;
				}
			} else if (includePosition == points.Length) {
				// Included in the end, remove first point of the included path if it's zero
				// (to remove duplicate points)
				originalPathHeadPointCount = points.Length;
				originalPathTailPointCount = 0;
				includedPathFirstPointOffset = alignFirstPoint ? -includedPoints [0].Position : includedPathPosOffset;
				//includedPosOffset = Vector3.zero;
				if (removeDuplicates && (includedPoints [0].Position + includedPathFirstPointOffset == Vector3.zero)) {
					includedPointCount--;
					includedPointsOffset = 1;
				}
			} else {
				// Included in the middle, remove first point of the included path if it's zero
				// (to remove duplicate points)
				originalPathHeadPointCount = includePosition;
				originalPathTailPointCount = points.Length - includePosition;
				includedPathFirstPointOffset = alignFirstPoint ? -includedPoints [0].Position : includedPathPosOffset;
				if (removeDuplicates && (alignFirstPoint || includedPoints [0].Position == Vector3.zero)) {
					includedPointCount--;
					includedPointsOffset = 1;
				}
			}

			this._currentIncludedPathPosOffset = includedPathFirstPointOffset;

			int totalPointCount = includedPointCount + originalPathHeadPointCount + originalPathTailPointCount;
			PathPoint[] results = new PathPoint[totalPointCount];

			// Copy originals (two parts: 1. before included and 2. after included)
			int originalPathTailOffset = originalPathHeadPointCount + originalPathTailSkipOffset;
			int resultPathTailOffset = originalPathHeadPointCount + includedPointCount;


			for (int i = 0; i < originalPathHeadPointCount; i++) {
				//float distFromPrev = (i > 0) ? (points[i].Position - points[i - 1].Position).magnitude : 0.0f;
				int resultIndex = i;
				results [resultIndex] = points [i];
				results [resultIndex].Flags = ppFlags;
			}
			if (originalPathTailPointCount > 0) {
				Vector3 tailPosOffs = includedPointCount > 0 ? includedPoints [includedPoints.Length - 1] .Position : Vector3.zero;

				for (int i = 0; i < originalPathTailPointCount; i++) {
					//float distFromPrev = (i > 0) ? (points[i].Position - points[i - 1].Position).magnitude : 0.0f;
					int resultIndex = i + resultPathTailOffset;
					results [resultIndex] = points [i + originalPathTailOffset];
					results [resultIndex].Position += tailPosOffs;
					results [resultIndex].Flags = ppFlags;
				}
			}
             
			// Copy included
			if (includedPointCount > 1) {

				// TODO what about these? Should we have configurable process?
				bool updateDistFromPrev = PathPoint.IsDistanceFromPrevious (ppFlags);
				bool updateDistFromBegin = updateDistFromPrev && PathPoint.IsDistanceFromBegin (ppFlags);

				// Initial total distance:
				float totalDistance = updateDistFromBegin ? 
                    (points.Length > 0 ? points [points.Length - 1].DistanceFromBegin : 0.0f)
                        : 0.0f;

//				int includedPointsStartOffset = (includePosition > 0) ? 1 : 0;
				int resultsArrayOffset = originalPathHeadPointCount;

				// First point offset:
				;

				Vector3 ptOffs = includedPathFirstPointOffset + 
					((originalPathHeadPointCount > 0) ? points [originalPathHeadPointCount - 1].Position : Vector3.zero);
				for (int i = 0; i < includedPointCount; i++) {
					int includedPointsIndex = i + includedPointsOffset;

					if (updateDistFromBegin) {
						totalDistance += includedPoints [includedPointsIndex].DistanceFromPrevious;
					}
					float distFromPrevious = updateDistFromPrev ? includedPoints [includedPointsIndex].DistanceFromPrevious : 0.0f;

					PathPoint pp = new PathPoint (includedPoints [includedPointsIndex], ppFlags);
					pp.Position += ptOffs;
					pp.DistanceFromPrevious = distFromPrevious;
					pp.DistanceFromBegin = totalDistance;

					int resultsIndex = i + resultsArrayOffset;
					results [resultsIndex] = pp;
				}
			}
			return results;
		}
        

      
		public override Path[] GetPathDependencies ()
		{

			Path p = GetIncludedPath (GetContainer ().GetReferenceContainer ());
			return (null != p) ? new Path[] {p} : new Path[0];
		}
	}

}
