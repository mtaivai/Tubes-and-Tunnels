using UnityEngine;
using System.Collections;
using System;

// Cubic bezier curve using four control points
// Heavily inspired by (based on): http://catlikecoding.com/unity/tutorials/curves-and-splines/
namespace Paths.Bezier
{
	class BezierUtil
	{

		public static int GetSectionOffset (int controlPointCount, ref float t)
		{
			int curveCount = (controlPointCount - 1) / 3;

			int offs = 0;
			if (t >= 1.0f) {
				// Last segment (curve)
				t = 1.0f;
				offs = controlPointCount - 4;
			} else {
				t = Mathf.Clamp01 (t) * curveCount;
				offs = (int)t * 3;
				t -= offs / 3;
				//offs *= 3;
			}
			return offs;
		}

		private static Vector3[] GetSegmentPoints (Vector3[] controlPoints, ref float t, bool loop)
		{
			Vector3[] pt = new Vector3[4];
			int pointCount = controlPoints.Length;
			int offs = GetSectionOffset (pointCount, ref t);
			if (loop && offs + 3 >= pointCount - 1) {
				// Last segment in loop mode
				pt [0] = controlPoints [pointCount - 4];
				pt [1] = controlPoints [pointCount - 3];
				pt [2] = controlPoints [pointCount - 2];
				pt [3] = controlPoints [0];
			} else {
				pt [0] = controlPoints [offs + 0];
				pt [1] = controlPoints [offs + 1];
				pt [2] = controlPoints [offs + 2];
				pt [3] = controlPoints [offs + 3];
			}
			return pt;
		}

		public static Vector3 GetPoint (Vector3[] controlPoints, float t, bool loop)
		{
			Vector3[] pt = GetSegmentPoints (controlPoints, ref t, loop);

			// Inverse t:
			float tInv = 1.0f - t;
            
			// Precalculate some equtation components:
			float tInv2 = tInv * tInv;
			float t2 = t * t;

			return tInv2 * tInv * pt [0] +
				3.0f * tInv2 * t * pt [1] +
				3.0f * tInv * t2 * pt [2] +
				t2 * t * pt [3];
		}

		public static Vector3 GetFirstDerivate (Vector3[] controlPoints, float t, bool loop)
		{

			Vector3[] pt = GetSegmentPoints (controlPoints, ref t, loop);

			// Inverse t:
			float tInv = 1.0f - t;

			return 3.0f * tInv * tInv * (pt [1] - pt [0]) +
				6.0f * tInv * t * (pt [2] - pt [1]) +
				3.0f * t * t * (pt [3] - pt [2]);
		}
	}
}

