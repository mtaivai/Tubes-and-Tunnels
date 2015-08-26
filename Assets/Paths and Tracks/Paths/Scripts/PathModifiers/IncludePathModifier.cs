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

        //private const string PARAM_INCLUDED_PATH_INSTANCE_ID = "includedPath.InstanceID";
//        private Path _includedPath;
        private int includedPathRefIndex = -1;



//        public Path IncludedPath
//        {
//            get
//            {
//                if (null == _includedPath && includedPathRefIndex >= 0)
//                {
//                    _includedPath = (Path)
////                  RegisterListenerOnIncludedPath();
//                }
//                return this._includedPath;
//            }
//            set
//            {
//                if (_includedPath != value)
//                {
////                  if (null != _includedPath) {
////                      UnregisterListenerOnIncludedPath();
////                  }
//                    _includedPath = value;
////                  if (null != _includedPath) {
////                      RegisterListenerOnIncludedPath();
////                  }
//                }
//
//                includedPathInstanceId = (null != _includedPath) ? _includedPath.GetInstanceID() : 0;
//            }
//        }

        public override void OnDetach()
        {
            SetIncludedPath(GetContainer().GetReferenceContainer(), null);
        }

        public Path GetIncludedPath(IReferenceContainer refContainer)
        {
            Path refPath;
            if (includedPathRefIndex >= 0 && includedPathRefIndex < refContainer.GetReferentCount())
            {

                object refObj = refContainer.GetReferent(includedPathRefIndex);
                if (null == refObj)
                {
                    Debug.LogWarning("Referent #" + includedPathRefIndex + " is missing!");
                    refPath = null;
                } else if (!(refObj is Path))
                {
                    Debug.LogWarning("Referent #" + includedPathRefIndex + " is not Path (it's " + refObj.GetType() + ")");
                    refPath = null;
                } else
                {
                    refPath = (Path)refObj;
                }
            } else
            {
                refPath = null;
            }
            return refPath;
        }
        public void SetIncludedPath(IReferenceContainer refContainer, Path includedPath)
        {
            if (includedPathRefIndex >= 0 && includedPathRefIndex < refContainer.GetReferentCount())
            {
                // Replace or remove existing
                if (includedPath != null)
                {
                    refContainer.SetReferent(includedPathRefIndex, includedPath);
                } else
                {
                    // null includedPath; remove the referent
                    refContainer.RemoveReferent(includedPathRefIndex);
                }
            } else if (null != includedPath)
            {
                // Add new
                includedPathRefIndex = refContainer.AddReferent(includedPath);
            }
        }

        public override PathPoint[] GetModifiedPoints(PathPoint[] points, PathModifierContext context)
        {
            int ppFlags = (GetPassthroughFlags(context) & context.InputFlags) | GetGenerateFlags(context);

            PathPoint[] includedPoints;
            Path includedPath = GetIncludedPath(context.PathModifierContainer.GetReferenceContainer());
            if (null != includedPath)
            {
                includedPoints = includedPath.GetAllPoints();
                ppFlags &= includedPath.GetOutputFlags();
            } else
            {
                includedPoints = new PathPoint[0];
            }

            int totalPointCount = points.Length;
            if (includedPoints.Length > 1)
            {
                // We don't use the first point of the included path if we already have a
                // point serving as first point:
                totalPointCount += includedPoints.Length;
                if (points.Length > 0)
                {
                    totalPointCount--;
                }
            }

            PathPoint[] results = new PathPoint[totalPointCount];


            // Copy originals
            for (int i = 0; i < points.Length; i++)
            {
                //float distFromPrev = (i > 0) ? (points[i].Position - points[i - 1].Position).magnitude : 0.0f;

                results [i] = new PathPoint(
                    points [i].Position, points [i].Direction, 
                    points [i].Up, points [i].Angle,
                    points [i].DistanceFromPrevious, points [i].DistanceFromBegin, ppFlags);
            }

             
            // Copy included
            if (includedPoints.Length > 1)
            {

                bool updateDistFromPrev = PathPoint.IsDistanceFromPrevious(ppFlags);
                bool updateDistFromBegin = updateDistFromPrev && PathPoint.IsDistanceFromBegin(ppFlags);

                float totalDistance = updateDistFromBegin ? 
                    (points.Length > 0 ? points [points.Length - 1].DistanceFromBegin : 0.0f)
                        : 0.0f;

                int includedPointsStartOffset = points.Length > 0 ? 1 : 0;
                int resultsArrayOffset = points.Length - includedPointsStartOffset;

                Vector3 ptOffs = (points.Length > 0) ? points [points.Length - 1].Position : Vector3.zero;
                for (int i = includedPointsStartOffset; i < includedPoints.Length; i++)
                {

                    if (updateDistFromBegin)
                    {
                        totalDistance += includedPoints [i].DistanceFromPrevious;
                    }
                    float distFromPrevious = updateDistFromPrev ? includedPoints [i].DistanceFromPrevious : 0.0f;

                    results [i + resultsArrayOffset] = new PathPoint(
                        includedPoints [i].Position + ptOffs, includedPoints [i].Direction, 
                        includedPoints [i].Up, includedPoints [i].Angle,
                        distFromPrevious, totalDistance, ppFlags);

                }
            }
            return results;
        }
        
        public override void OnSerialize(Serializer store)
        {
            store.Property("includedPathRefIndex", ref includedPathRefIndex);
        }

      
        public override IPath[] GetPathDependencies()
        {

            IPath p = GetIncludedPath(GetContainer().GetReferenceContainer());
            return (null != p) ? new IPath[] {p} : new IPath[0];
        }
    }

}
