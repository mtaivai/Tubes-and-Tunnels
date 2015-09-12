using UnityEngine;
using System.Collections;
using System;

namespace Paths.MeshGenerator.SliceStrip
{
	public abstract class SliceStripSlice
	{

		//private Vector3[] points;

		private Vector3[] transformedPoints;
		private Vector3[] transformedNormals;

		private Vector3 center;
		private Quaternion rotation;
		protected float circumference;

		protected SliceStripSlice (Vector3 center, Quaternion rotation)
		{
			this.center = center;
			this.rotation = rotation;
		}


		public Vector3 Center {
			get {
				return center;
			}
		}

		public Vector3[] Points {
			get {
				if (null == transformedPoints) {
					Vector3[] localPoints = GetLocalPoints ();
					transformedPoints = new Vector3[localPoints.Length];
					for (int i = 0; i < localPoints.Length; i++) {
						transformedPoints [i] = rotation * localPoints [i] + center;
					}
				}
				return transformedPoints;
			}
		}
		public Vector3[] Normals {
			get {
				if (null == transformedNormals) {
					Vector3[] localNormals = GetLocalNormals ();
					transformedNormals = new Vector3[localNormals.Length];
					for (int i = 0; i < localNormals.Length; i++) {
						transformedNormals [i] = rotation * localNormals [i];
					}
				}
				return transformedNormals;
			}
		}

		public float Circumference {
			get {
				return circumference;
			}
		}

		public Vector3 Direction {
			get {
				return rotation * Vector3.forward;
			}
		}

//		public abstract int GetEdgeCount ();
		protected abstract Vector3[] GetLocalPoints ();
		protected abstract Vector3[] GetLocalNormals ();
	}
}
