// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using System;
using UnityEngine;
using System.Collections.Generic;

using Util;
using Paths;
using Paths.MeshGenerator;
using Paths.MeshGenerator.SliceStrip;

namespace Paths.MeshGenerator.Extruder.Model
{
	[Serializable]
	public class Edge
	{
		[Flags]
		public enum MetricsFlags : int
		{
			None = 0x00,
			Normal = 0x01,
			Dir = 0x02,
			Length = 0x04,
			MidPoint = 0x08,
			All = Normal | Dir | Length | MidPoint,
		}
		[SerializeField]
		private int
			fromVertexIndex;
		[SerializeField]
		private int
			toVertexIndex;
		
		[SerializeField]
		private Vector3
			normal;
		
		[SerializeField]
		private Vector3
			dir;
		
		[SerializeField]
		private float
			length;
		
		[SerializeField]
		private Vector3
			midPoint;
		
		[SerializeField]
		private MetricsFlags
			metricsFlags = MetricsFlags.None;
		
		public Edge () : this(-1, -1)
		{
			
		}
		public Edge (int fromVertex, int toVertex)
		{
			this.fromVertexIndex = fromVertex;
			this.toVertexIndex = toVertex;
		}
		public Edge (Edge src)
		{
			this.fromVertexIndex = src.fromVertexIndex;
			this.toVertexIndex = src.toVertexIndex;
			this.metricsFlags = src.metricsFlags;
			this.normal = src.normal;
			this.dir = src.dir;
			this.length = src.length;
			this.midPoint = src.midPoint;
			
		}
		// TODO refactor these to properties!
		public int GetFromVertexIndex ()
		{
			return fromVertexIndex;
		}
		public void SetFromVertexIndex (int index)
		{
			this.fromVertexIndex = index;
		}
		public int GetToVertexIndex ()
		{
			return toVertexIndex;
		}
		public void SetToVertexIndex (int index)
		{
			this.toVertexIndex = index;
		}
		public int[] GetVertexIndices ()
		{
			return new int[] {fromVertexIndex, toVertexIndex};
		}
		
		private bool IsMetricsFlag (MetricsFlags flag)
		{
			return (this.metricsFlags & flag) == flag;
		}
		private void SetMetricsFlag (MetricsFlags flag, bool value)
		{
			if (value) {
				this.metricsFlags |= flag;
			} else {
				this.metricsFlags &= ~flag;
			}
		}
		
		public bool IsMetricsKnown (MetricsFlags metrics)
		{
			return IsMetricsFlag (metrics);
		}
		
		public Vector3 GetNormal ()
		{
			return GetNormal (null);
		}
		public Vector3 GetNormal (IDieModel model)
		{
			if (!IsMetricsFlag (MetricsFlags.Normal) && null != model) {
				UpdateMetrics (model);
			}
			return normal;
		}
		public void SetNormal (Vector3 value)
		{
			this.normal = value;
			SetMetricsFlag (MetricsFlags.Normal, true);
		}
		
		public Vector3 GetDir ()
		{
			return GetDir (null);
		}
		public Vector3 GetDir (IDieModel model)
		{
			if (!IsMetricsFlag (MetricsFlags.Dir) && null != model) {
				UpdateMetrics (model);
			}
			return dir;
		}
		public void SetDir (Vector3 value)
		{
			this.dir = value;
			SetMetricsFlag (MetricsFlags.Dir, true);
		}
		
		public Vector3 GetMidPoint (IDieModel model)
		{
			if (!IsMetricsFlag (MetricsFlags.MidPoint) && null != model) {
				UpdateMetrics (model);
			}
			return midPoint;
		}
		public void SetMidPoint (Vector3 value)
		{
			this.midPoint = value;
			SetMetricsFlag (MetricsFlags.MidPoint, true);
		}
		public float GetLength ()
		{
			return GetLength (null);
		}
		public float GetLength (IDieModel model)
		{
			if (!IsMetricsFlag (MetricsFlags.Length) && null != model) {
				UpdateMetrics (model);
			}
			return length;
		}

		public void ResetMetrics ()
		{
			this.metricsFlags = MetricsFlags.None;
		}
		
		public void UpdateMetrics (IDieModel model)
		{
			if (metricsFlags != MetricsFlags.All) {
				Vector3 pt0 = model.GetVertexAt (fromVertexIndex);
				Vector3 pt1 = model.GetVertexAt (toVertexIndex);
				Vector3 v = (pt1 - pt0);
				
				if (!IsMetricsFlag (MetricsFlags.Dir)) {
					this.dir = v.normalized;
					SetMetricsFlag (MetricsFlags.Dir, true);
				}
				if (!IsMetricsFlag (MetricsFlags.Normal)) {
					this.normal = -Vector3.Cross (this.dir, Vector3.forward).normalized;
					SetMetricsFlag (MetricsFlags.Normal, true);
				}
				if (!IsMetricsFlag (MetricsFlags.Length)) {
					this.length = v.magnitude;
					SetMetricsFlag (MetricsFlags.Length, true);
				}
				if (!IsMetricsFlag (MetricsFlags.MidPoint)) {
					this.midPoint = pt0 + this.dir * this.length / 2f;
					SetMetricsFlag (MetricsFlags.MidPoint, true);
				}
			}
		}
		
	}
	
//	[Serializable]

}
