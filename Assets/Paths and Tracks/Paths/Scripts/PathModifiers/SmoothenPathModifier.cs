using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Paths
{
	// TODO we should have an option to smoothen "Up" vectors as a separate process
	// TODO we should recalculate directions, angles and distances if they are part of the input (we should have a configuration option)
	[PathModifier(processCaps=PathPoint.POSITION,passthroughCaps=PathPoint.DIRECTION | PathPoint.UP)]
	public class SmoothenPathModifier : AbstractPathModifier
	{
		//public Vector3 scaling = new Vector3(1f, 1f, 1f);

		public enum SmoothAlgorithm
		{
			MovingAverage,
		}
		public SmoothAlgorithm algorithm = SmoothAlgorithm.MovingAverage;

		// TODO use property that can adjust to minimum 2
		public int subsetSize = 5;
		public bool keepFirst = true;
		public bool keepLast = true;

		public override PathPoint[] GetModifiedPoints (PathPoint[] points, PathModifierContext context)
		{
			// Moving Average algorithm
			float halfSubset = (float)subsetSize / 2f;
			//Vector3 currentAverage = Vector3.zero;
			//Vector3 currentSum = Vector3.zero;

			int ppFlags = GetOutputFlags (context);

			// TODO we don't have to recalculate the subset average on each iteration of 'i':
			// (we just need to subtract the first point and add a new point to the moving average)
			PathPoint[] results = new PathPoint[points.Length];
			for (int i = 0; i < points.Length; i++) {
				if ((!keepFirst || i > 0) && (!keepLast || i < points.Length - 1)) {
					int subsetBegin = (int)((float)i - halfSubset);
					if (subsetBegin < 0) {
						subsetBegin = 0;
					}
					int subsetEnd = i + Mathf.RoundToInt (halfSubset);
					if (subsetEnd >= points.Length - 1) {
						subsetEnd = points.Length - 1;
					}
					int actualSubsetPoints = subsetEnd - subsetBegin + 1;

					Vector3 sumPt = Vector3.zero;
					for (int j = subsetBegin; j <= subsetEnd; j++) {
						sumPt += points [j].Position;
					}
					Vector3 avgPt = sumPt / (float)actualSubsetPoints;

					results [i] = new PathPoint (
                        avgPt, points [i].Direction, points [i].Up, 0.0f, 0.0f, 0.0f, ppFlags);
				} else {
					results [i] = new PathPoint (points [i], ppFlags);
				}
			} 
			return results;
		}
        
		public override void OnSerialize (Serializer store)
		{
			store.EnumProperty ("algorithm", ref algorithm);
			store.Property ("keepFirst", ref keepFirst);
			store.Property ("keepLast", ref keepLast);
			store.Property ("subsetSize", ref subsetSize);
		}

        
	}

    
}
