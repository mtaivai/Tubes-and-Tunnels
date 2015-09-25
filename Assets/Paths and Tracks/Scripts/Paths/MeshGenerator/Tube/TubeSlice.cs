using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

using Util;
using Paths;
using Paths.MeshGenerator;
using Paths.MeshGenerator.SliceStrip;

namespace Paths.MeshGenerator.Tube
{
	public class TubeSlice : SliceStripSlice
	{

		public TubeSlice (int edges, float startAngle, float arcLength, Vector2 size) : base()
		{

			// 0   1
			// +---+
			// |   |
			// +---+
			// 3   2
//			startAngle = Mathf.Clamp (startAngle, 0f, 360f);
			arcLength = Mathf.Clamp (arcLength, 0f, 360f);

			this.closedShape = Mathf.Approximately (360f, arcLength);

			CreateEllipse (edges, size.x, size.y, startAngle, arcLength, out this.points, out this.normals);

			circumference = 0.0f;
			if (closedShape) {
				for (int i = 0; i < points.Length; i++) {
					Vector3 pt0 = points [i];
					Vector3 pt1 = points [(i < points.Length - 1) ? i + 1 : 0];
					circumference += (pt1 - pt0).magnitude;
				}
			} else {
				for (int i = 1; i < points.Length; i++) {
					Vector3 pt0 = points [i - 1];
					Vector3 pt1 = points [i];
					circumference += (pt1 - pt0).magnitude;
				}
			}
        
		}
    
		void CreateEllipse (int edges, float width, float height, float startAngle, float arcLength, out Vector3[] points, out Vector3[] normals)
		{
			int pointCount = closedShape ? edges : edges + 1;
			points = new Vector3[pointCount];
			normals = new Vector3[pointCount];

			// Rotate to keep ceiling up and floor down:
			// * 270 = first point is on the top
			// * 225 = first point is on the top left corner (useful for quads to keep the floor down)

			float fixRotation = 270f * Mathf.Deg2Rad;

			float tStart = startAngle / 360f;
			float tRange = arcLength / 360f;
//			float tEnd = tStart + tRange;

			for (int i = 0; i < pointCount; i++) {
				Vector3 pt;
            
				float t = ((float)i / (float)edges) * tRange + tStart;
				float a = t * Mathf.PI * 2 + fixRotation;
				float x = width * Mathf.Cos (-a);
				float y = height * Mathf.Sin (-a);
				pt = new Vector3 (x, y, 0);
				points [i] = pt;
				normals [i] = -pt.normalized;
			}
		}

	}
}
