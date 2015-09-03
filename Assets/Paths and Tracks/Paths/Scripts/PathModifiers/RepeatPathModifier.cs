using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Paths
{
	[PathModifier(requiredInputFlags=PathPoint.POSITION, 
                  passthroughCaps=PathPoint.POSITION | PathPoint.DIRECTION | PathPoint.DISTANCE_FROM_PREVIOUS | PathPoint.UP | PathPoint.ANGLE, 
                  generateCaps=PathPoint.DISTANCE_FROM_BEGIN)]
	public class RepeatPathModifier : AbstractPathModifier
	{

		public int repeatCount = 1;

		public RepeatPathModifier ()
		{
		}

		public override int GetProcessFlags (PathModifierContext context)
		{
			if (PathPoint.IsDistanceFromBegin (context.InputFlags)) {
				return PathPoint.DISTANCE_FROM_BEGIN;
			} else {
				return PathPoint.NONE;
			}
            
		}

		public override int GetGenerateFlags (PathModifierContext context)
		{
			if (PathPoint.IsDistanceFromPrevious (context.InputFlags) && !PathPoint.IsDistanceFromBegin (context.InputFlags)) {
				return PathPoint.DISTANCE_FROM_BEGIN;
			} else {
				return PathPoint.NONE;
			}

		}

		// TODO distanceFromBegin is not working correctly!
		protected override PathPoint[] DoGetModifiedPoints (PathPoint[] points)
		{
			if (points.Length < 2) {
				return points;
			}

			int ppFlags = GetOutputFlags (context);

			int resultPointCount = points.Length + (points.Length - 1) * repeatCount;

			PathPoint[] results = new PathPoint[resultPointCount];
			//Vector3 ptOffs = '

			// Copy original points to beginning:
			float pathLength = 0.0f;
			for (int i = 0; i < points.Length; i++) {
				results [i] = points [i];
				pathLength += results [i].DistanceFromPrevious;
			}

			for (int rep = 0; rep < repeatCount; rep++) {
				int repOffset = rep * (points.Length - 1);

				Vector3 ptOffs = results [repOffset + points.Length - 1].Position - results [0].Position;

				for (int i = 1; i < points.Length; i++) {
					int resultIndex = repOffset + i + points.Length - 1;

					float distFromPrev = points [i].DistanceFromPrevious;
					float distFromBegin = points [i].DistanceFromBegin + pathLength;
					Vector3 dir = points [i].Direction;

					Vector3 pos = points [i].Position + ptOffs;

					results [resultIndex] = new PathPoint (pos, dir, points [i].Up, points [i].Angle, distFromPrev, distFromBegin, ppFlags);
				} 
			}

			return results;
		}
        
		public override void OnSerialize (Serializer store)
		{
			store.Property ("repeatCount", ref repeatCount);
		}

        
	}
}
