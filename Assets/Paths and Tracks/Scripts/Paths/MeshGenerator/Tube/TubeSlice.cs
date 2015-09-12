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
		private Vector3[] points;
		private Vector3[] normals;

		public TubeSlice (Vector3 center, Quaternion rotation, int edges, float width, float height, float ellipseRotation) 
    : base(center, rotation)
		{

			// 0   1
			// +---+
			// |   |
			// +---+
			// 3   2
			//points = new Vector3[] {
			//  new Vector3(-sliceSize, sliceSize, 0),
			//  new Vector3(sliceSize, sliceSize, 0),
			//  new Vector3(sliceSize, -sliceSize, 0),
			//  new Vector3(-sliceSize, -sliceSize, 0),
			//};
			CreateEllipse (edges, width, height, ellipseRotation, out points, out normals);

			circumference = 0.0f;
			for (int i = 0; i < points.Length; i++) {
				Vector3 pt0 = points [i];
				Vector3 pt1 = points [(i < points.Length - 1) ? i + 1 : 0];
				circumference += (pt1 - pt0).magnitude;
			}
        
		}
    
		void CreateEllipse (int edges, float width, float height, float sliceRotation, out Vector3[] points, out Vector3[] normals)
		{
			points = new Vector3[edges];
			normals = new Vector3[edges];

			// Rotate to keep ceiling up and floor down:
			float rotation = (180.0f + sliceRotation) * Mathf.Deg2Rad;

			for (int i = 0; i < edges; i++) {
				Vector3 pt;
            
				float t = ((float)i / (float)edges) * Mathf.PI * 2 + rotation;
				float x = width * Mathf.Cos (-t);
				float y = height * Mathf.Sin (-t);
				pt = new Vector3 (x, y, 0);
				points [i] = pt;
				normals [i] = -pt.normalized;
			}
		}
    
		protected override Vector3[] GetLocalPoints ()
		{
			return points;
		}
		protected override Vector3[] GetLocalNormals ()
		{
			return normals;
		}
	}
}
