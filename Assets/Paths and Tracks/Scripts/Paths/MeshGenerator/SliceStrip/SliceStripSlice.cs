using UnityEngine;
using System.Collections;
using System;

namespace Paths.MeshGenerator.SliceStrip
{
	public abstract class SliceStripSlice
	{

		//private Vector3[] points;

		protected Vector3[] points;
		protected Vector3[] normals;
		protected bool closedShape;

		// TODO this could be calculated automatically:
		protected float circumference;

		protected SliceStripSlice ()
		{

		}
		protected SliceStripSlice (Vector3[] points, Vector3[] normals, bool closedShape)
		{
			if (points.Length != normals.Length) {
				throw new ArgumentException ("points.Length != normals.Length");
			}
			this.points = points;
			this.normals = normals;
			this.closedShape = closedShape;
		}

		public Vector3[] Points {
			get {
//				if (null == transformedPoints) {
//					Vector3[] localPoints = GetLocalPoints ();
//					transformedPoints = new Vector3[localPoints.Length];
//					for (int i = 0; i < localPoints.Length; i++) {
//						transformedPoints [i] = rotation * localPoints [i] + center;
//					}
//				}
//				return transformedPoints;
				return points;
			}
		}
		public Vector3[] Normals {
			get {
//				if (null == transformedNormals) {
//					Vector3[] localNormals = GetLocalNormals ();
//					transformedNormals = new Vector3[localNormals.Length];
//					for (int i = 0; i < localNormals.Length; i++) {
//						transformedNormals [i] = rotation * localNormals [i];
//					}
//				}
//				return transformedNormals;
				return normals;
			}
		}

		public float Circumference {
			get {
				return circumference;
			}
		}

//		public Vector3 Direction {
//			get {
//				return rotation * Vector3.forward;
//			}
//		}

		public bool ClosedShape {
			get {
				return closedShape;
			}
		}

//		public abstract int GetEdgeCount ();
//		public abstract bool IsClosedShape ();
//		protected abstract Vector3[] GetLocalPoints ();
//		protected abstract Vector3[] GetLocalNormals ();
	}

	public class TransformedSlice : SliceStripSlice
	{
		private Vector3 center;
		private Vector3 direction;

//		public Vector3[] transformedPoints;
//		public Vector3[] transformedNormals;
		public SliceStripSlice nontransformedSlice;
		public TransformedSlice (SliceStripSlice slice, Vector3 position, Quaternion rotation) : base()
		{
			this.nontransformedSlice = slice;
			this.closedShape = slice.ClosedShape;
			this.circumference = slice.Circumference;

			Vector3[] localPoints = slice.Points;
			Vector3[] localNormals = slice.Normals;

			if (localPoints.Length != localNormals.Length) {
				throw new ArgumentException ("localPoints.Length != localNormals.Length");
			}
			this.center = position;
			this.direction = rotation * Vector3.forward;
			
			int pointCount = localPoints.Length;
			this.points = new Vector3[pointCount];
			this.normals = new Vector3[pointCount];
			
			for (int i = 0; i < pointCount; i++) {
				this.points [i] = rotation * localPoints [i] + position;
				this.normals [i] = rotation * localNormals [i];
			}
		}

		public Vector3 Center {
			get {
				return this.center;
			}
		}

		public Vector3 Direction {
			get {
				return this.direction;
			}
		}

		public SliceStripSlice NontransformedSlice {
			get {
				return this.nontransformedSlice;
			}
		}
	}
}
