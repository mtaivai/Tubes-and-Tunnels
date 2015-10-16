// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Paths
{

	[CodeQuality.Experimental]
	[PathModifier(requiredInputFlags=PathPoint.NONE, passthroughCaps=PathPoint.ALL, processCaps=PathPoint.NONE)]
	public class InterpolateWeightsPathModifier : AbstractPathModifier
	{
//		public bool allWeights = true;
//		public string weightId;
//
		protected override PathPoint[] DoGetModifiedPoints (PathPoint[] points)
		{
			HashSet<string> weightIdSet = new HashSet<string> ();

			// Collect all weight ids:
			int pointCount = points.Length;
			for (int i = 0; i < pointCount; i++) {
				PathPoint pp = points [i];
				string[] weightIds = pp.GetWeightIds ();
				foreach (string w in weightIds) {
					weightIdSet.Add (w);
				}
			}

			bool loopPath = context.PathInfo.IsLoop ();

			// Now interpolate
			foreach (string weightId in weightIdSet) {
				InterpolateWeight (weightId, points, loopPath);

			}

			return points;
		}

		public static void InterpolateWeight (string weightId, PathPoint[] points, bool loopPath)
		{
			// TODO read defaults from metadata!
			bool weightHasDefault = false;
			float defaultValue = 0.0f;

			int pointCount = points.Length;
			
			int previousWithWeightIndex = 0;
			bool previousWithWeightIndexKnown = false;
			
			int nextWithWeightIndex = 0;
			bool nextWithWeightIndexKnown = false;

			for (int i = 0; i < pointCount; i++) {
				PathPoint pp = points [i];
				if (!pp.HasWeight (weightId)) {
					// Find previous with valid weight
					if (!previousWithWeightIndexKnown) {
						previousWithWeightIndexKnown = FindIndexOfPreviousWithWeight (weightId, points, i - 1, loopPath, out previousWithWeightIndex);
					}
					// Find next with valid weight
					if (!nextWithWeightIndexKnown) {
						nextWithWeightIndexKnown = FindIndexOfNextWithWeight (weightId, points, i + 1, loopPath, out nextWithWeightIndex);
					}
					// TODO THIS IS NOT WORKING WITH LOOP PATHS YET!
					
					bool haveValue;
					float fromValue;
					int fromIndex;
					
					float toValue;
					int toIndex;
					
					if (!previousWithWeightIndexKnown) {
						// We don't have any previous weight values; extrapolate from the next value (just a straight line)
						// TODO or should we use default value already?
						if (nextWithWeightIndexKnown) {
							fromValue = points [nextWithWeightIndex].GetWeight (weightId);
							fromIndex = i;
							haveValue = true;
						} else {
							// We know nothing! Use default if it's available!
							haveValue = weightHasDefault;
							fromValue = defaultValue;
							fromIndex = i;
						}
					} else {
						// We have the previous weight
						fromValue = points [previousWithWeightIndex].GetWeight (weightId);
						fromIndex = previousWithWeightIndex;
						haveValue = true;
					}
					
					if (haveValue) {
						// We have at least the "fromValue", let's see if we have "toValue":
						if (nextWithWeightIndexKnown) {
							// Yes we have the "toValue"
							toValue = points [nextWithWeightIndex].GetWeight (weightId);
							toIndex = nextWithWeightIndex;
						} else {
							// We don't know the 'toValue' so let's just perform a straight line extrapolation:
							toValue = fromValue;
							toIndex = pointCount;
						}
						
						// j  t
						//z0  0
						// 1  0.2 
						// 2  0.4
						// 3  0.6
						// 4  0.8
						//z5  1



						float fromDist;
						float toDist;
						if (toIndex < i) {
							// Looped around first point
							toDist = (float)(toIndex + pointCount);
							toIndex = pointCount;
						} else {
							toDist = (float)toIndex;
						}

						int tOffs;
						if (fromIndex > i) {
							// Looped around last point
							tOffs = -(fromIndex - pointCount);
							fromDist = (float)(-tOffs);
							//fromDist = (float)(fromIndex - pointCount);
						} else {
							fromDist = (float)fromIndex;
							tOffs = 1;
						}
						float dist = toDist - fromDist;


						// Now let's interpolate
						for (int j = i; j < toIndex; j++) {
							float t = (float)(j - i + tOffs) / dist;
							float val = Mathf.Lerp (fromValue, toValue, t);
							points [j].SetWeight (weightId, val);
						}
						// Advance 'i' over already interpolated points:
						if (toIndex > i) {
							i = toIndex;
						} else {
							// Already looped around the path start
							i = pointCount - 1;
						}
						previousWithWeightIndex = i;
						previousWithWeightIndexKnown = true;
						nextWithWeightIndexKnown = false;
					}
					
				} else {
					// this has weight
					previousWithWeightIndexKnown = true;
					previousWithWeightIndex = i;
				}
			}
		}
		
		private static bool FindIndexOfPreviousWithWeight (string weightId, PathPoint[] points, int startIndex, bool loopPath, out int foundIndex)
		{
			if (!loopPath && startIndex <= 0) {
				foundIndex = -1;
				return false;
			} else {
				for (int i = startIndex; i >= 0; i--) {
					if (points [i].HasWeight (weightId)) {
						foundIndex = i;
						return true;
					}
				}
				if (loopPath) {
					// Continue iterating back from the last index
					for (int i = points.Length - 1; i > startIndex; i--) {
						if (points [i].HasWeight (weightId)) {
							foundIndex = i;
							return true;
						}
					}
				}

				foundIndex = -1;
				return false;
			}
		}
		private static bool FindIndexOfNextWithWeight (string weightId, PathPoint[] points, int startIndex, bool loopPath, out int foundIndex)
		{
			int lastIndex = points.Length - 1;
			//int lastValidStartIndex = lastIndex - 1;

			if (!loopPath && startIndex > lastIndex) {
				foundIndex = -1;
				return false;
			} else {
				for (int i = startIndex; i <= lastIndex; i++) {
					if (points [i].HasWeight (weightId)) {
						foundIndex = i;
						return true;
					}
				}
				if (loopPath) {
					// Continue iterating again from the first point
					for (int i = 0; i < startIndex; i++) {
						if (points [i].HasWeight (weightId)) {
							foundIndex = i;
							return true;
						}
					}
				}


				foundIndex = -1;
				return false;
			}
		}
	}
       
}
