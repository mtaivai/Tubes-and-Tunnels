using UnityEngine;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Paths
{

	//contextParams={"1", "2"}
	[CodeQuality.Beta]
	[Plugin]
	[PathModifier(requiredInputFlags = PathPoint.NONE, 
                  processCaps = PathPoint.NONE,
                  passthroughCaps = PathPoint.NONE, 
                  generateCaps = PathPoint.ALL
	              )]
	[ProducesContextParams(
		IncludePathModifier.IncludedPathStartIndexParam,
		IncludePathModifier.IncludedPathEndIndexParam,
		IncludePathModifier.IncludedPathPointCountParam
	)]
	public class IncludePathModifier : AbstractPathModifier
	{
		public const string IncludedPathStartIndexParam = "Include.IncludedPathStartIndex";
		public const string IncludedPathEndIndexParam = "Include.IncludedPathEndIndex";
		public const string IncludedPathPointCountParam = "Include.IncludedPathPointCount";

//		private bool includedPathIsSelf = true;
		private string includedPathRefIndex = "";


		public int includedPathDataSetId;
		public bool includedPathFromSnapshot;
		public string includedPathSnapshotName;
		public int includePosition; // "include at index"
		public bool removeDuplicates; // "smart include"
		public bool breakLoop; // if the included path is looped, break the loop, i.e. remove last point of the included path
		public bool alignFirstPoint; // align first point of the included path with the point in "includePositino"
		public Vector3 includedPathPosOffset;
		public bool importMetadata;
		public bool overwriteMetadata;

		private int _includedPointCount;
		private int _includedIndexOffset;
		private int _inputPointCount;


		private Vector3 _currentIncludedPathPosOffset;
//		Vector3 includePosition

		public IncludePathModifier ()
		{

		}



		public int CurrentIncludedPointCount {
			get { return _includedPointCount; }
		}
		public int CurrentIncludedIndexOffset {
			get { return _includedIndexOffset; }
		}
		public int CurrentInputPointCount {
			get { return _inputPointCount; }
		}
		public Vector3 CurrentIncludedPathPosOffset {
			get { return _currentIncludedPathPosOffset; }
		}

		protected override void OnReset ()
		{
			IPathModifierContainer container = GetContainer ();
			if (null != container) {
				SetIncludedPath (container.GetReferenceContainer (), null);
			}
//			includedPathIsSelf = true;
			includedPathDataSetId = 0;
			includedPathFromSnapshot = false;
			includedPathSnapshotName = "";

			includePosition = -1;
			_inputPointCount = 0;
			_includedPointCount = 0;
			_includedIndexOffset = 0;
			removeDuplicates = true;
			breakLoop = true;
			alignFirstPoint = false;
			includedPathPosOffset = Vector3.zero;
			importMetadata = true;
			overwriteMetadata = true;
			_currentIncludedPathPosOffset = includedPathPosOffset;

		}


		public override void OnSerialize (Serializer store)
		{
			if (includedPathRefIndex == null) {
				includedPathRefIndex = "";
			}
			store.Property ("includedPathRefIndex", ref includedPathRefIndex);
			store.Property ("includedPathDataSetId", ref includedPathDataSetId);
			store.Property ("includedPathFromSnapshot", ref includedPathFromSnapshot);
			store.Property ("includedPathSnapshotName", ref includedPathSnapshotName);

			store.Property ("includePosition", ref includePosition);
			store.Property ("removeDuplicates", ref removeDuplicates);
			store.Property ("breakLoop", ref breakLoop);
			store.Property ("alignFirstPoint", ref alignFirstPoint);
			store.Property ("includedPathPosOffset", ref includedPathPosOffset);

			store.Property ("importMetadata", ref importMetadata);
			store.Property ("overwriteMetadata", ref overwriteMetadata);
		}

		protected override void OnDetach ()
		{
			SetIncludedPath (GetContainer ().GetReferenceContainer (), null);
		}

		public override int GetGenerateFlags (PathModifierContext context)
		{
			IReferenceContainer refContainer = context.PathModifierContainer.GetReferenceContainer ();
			IPathData includedData = GetIncludedPathData (refContainer);
			if (null != includedData) {
				return includedData.GetOutputFlags ();
			} else {
				return PathPoint.NONE;
			}
		}

		public Path GetIncludedPath (IReferenceContainer refContainer)
		{
			Path refPath;
			if (includedPathRefIndex.Length > 0 && refContainer.ContainsReferentObject (includedPathRefIndex)) {

				object refObj = refContainer.GetReferentObject (includedPathRefIndex);
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

		public IPathData GetIncludedPathData (Path includedPath, IReferenceContainer refContainer)
		{
			IPathData data = null != includedPath ? includedPath.FindDataSetById (includedPathDataSetId) : null;
			return data;
		}
		public IPathData GetIncludedPathData (IReferenceContainer refContainer)
		{
			Path includedPath = GetIncludedPath (refContainer);
			IPathData data = null != includedPath ? includedPath.FindDataSetById (includedPathDataSetId) : null;
			return data;
		}


		public void SetIncludedPath (IReferenceContainer refContainer, Path includedPath)
		{
			if (null != refContainer) {
				if (includedPathRefIndex.Length > 0 && refContainer.ContainsReferentObject (includedPathRefIndex)) {
					// Replace or remove existing
					if (includedPath != null) {
						refContainer.SetReferentObject (includedPathRefIndex, includedPath);
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
				includedPathRefIndex = "";
			}
		}

		protected override PathPoint[] DoGetModifiedPoints (PathPoint[] points)
		{
			this._inputPointCount = points.Length;

			int ppFlags = GetOutputFlags (context);

			PathPoint[] includedPoints;
			IPathData includedData = GetIncludedPathData (context.PathModifierContainer.GetReferenceContainer ());
			if (null != includedData) {

				if (importMetadata) {
					if (includedData.IsPathMetadataSupported () && context.PathMetadata is IEditablePathMetadata) {
						((IEditablePathMetadata)context.PathMetadata).Import (includedData.GetPathMetadata (), overwriteMetadata);
					}
				}

				// TODO what about other than default path data sets?
				if (includedPathFromSnapshot) {
					IPathSnapshotManager sm = includedData.GetPathSnapshotManager ();
					if (!sm.SupportsSnapshots ()) {
						// TODO add error to context
						string msg = string.Format ("Requested include of snapshot '{0}' but dataset '{1}' does not support snapshots", 
						                            includedPathSnapshotName, includedData.GetName ());
						context.Errors.Add (msg);
						Debug.LogError (msg);

						includedPoints = new PathPoint[0];
					} else if (!sm.ContainsSnapshot (includedPathSnapshotName)) {
						string msg = string.Format ("Requested snapshot '{0}' not found in dataset '{1}'", 
						                            includedPathSnapshotName, includedData.GetName ());
						context.Errors.Add (msg);
						Debug.LogError (msg);

						includedPoints = new PathPoint[0];
					} else {
						PathDataSnapshot ss = sm.GetSnapshot (includedPathSnapshotName);
						includedPoints = ss.Points;
						ppFlags = ss.Flags;
					}
				} else {
					includedPoints = includedData.GetAllPoints ();
					ppFlags &= includedData.GetOutputFlags ();
				}
			} else {
				// TODO log warning
				// TODO add errors to context?
				includedPoints = new PathPoint[0];
			}

			if (includePosition < 0 || includePosition > points.Length) {
				includePosition = points.Length;
			}

			int includedPointCount = includedPoints.Length;

			// TODO Snapshot can't be a loop?
			if (breakLoop && includedPointCount > 1 && includedData.GetPathInfo ().IsLoop ()) {
				includedPointCount --;
			}

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

			this._includedPointCount = includedPointCount;
			this._includedIndexOffset = originalPathHeadPointCount;

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
			int resultsArrayOffset = originalPathHeadPointCount;

			if (includedPointCount > 1) {

				// TODO what about these? Should we have configurable process?
				bool setDistFromPrev = PathPoint.IsDistanceFromPrevious (ppFlags);
				bool setDistFromBegin = setDistFromPrev && PathPoint.IsDistanceFromBegin (ppFlags);

				// Initial total distance:
				float totalDistance = setDistFromBegin ? 
                    (points.Length > 0 ? points [points.Length - 1].DistanceFromBegin : 0.0f)
                        : 0.0f;

//				int includedPointsStartOffset = (includePosition > 0) ? 1 : 0;

				Vector3 ptOffs = includedPathFirstPointOffset + 
					((originalPathHeadPointCount > 0) ? points [originalPathHeadPointCount - 1].Position : Vector3.zero);
				for (int i = 0; i < includedPointCount; i++) {
					int includedPointsIndex = i + includedPointsOffset;

					if (setDistFromBegin) {
						totalDistance += includedPoints [includedPointsIndex].DistanceFromPrevious;
					}
					float distFromPrevious = setDistFromPrev ? includedPoints [includedPointsIndex].DistanceFromPrevious : 0.0f;

					PathPoint pp = new PathPoint (includedPoints [includedPointsIndex], ppFlags);
					pp.Position += ptOffs;
					if (setDistFromPrev) {
						pp.DistanceFromPrevious = distFromPrevious;
					}
					if (setDistFromBegin) {
						pp.DistanceFromBegin = totalDistance;
					}

					int resultsIndex = i + resultsArrayOffset;
					results [resultsIndex] = pp;
				}
			}

			// Add parameters
			// TODO we'll have a method or attribute defining available context params!!!

			AddContextParameter (IncludedPathStartIndexParam, resultsArrayOffset);
			AddContextParameter (IncludedPathEndIndexParam, resultsArrayOffset + includedPointCount);
			AddContextParameter (IncludedPathPointCountParam, includedPointCount);


			return results;
		}
        

		// TODO WHAT'S THIS AND IS THIS USED??? IF NOT, REMOVE OR REFACTOR TO SUPPORT THE NEW SYSTEM!
		public override Path[] GetPathDependencies ()
		{

			Path p = GetIncludedPath (GetContainer ().GetReferenceContainer ());
			return (null != p) ? new Path[] {p} : new Path[0];
		}
	}

}
