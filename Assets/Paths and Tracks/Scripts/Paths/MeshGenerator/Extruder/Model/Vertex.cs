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

namespace Paths.MeshGenerator.Extruder.Model.Internal
{
	[Serializable]
	class Vertex : ISerializationCallbackReceiver
	{
		[SerializeField]
		private Vector3
			position;

		// TODO we could use edgeUvMap to store the default uv as well (with index -1)
		[SerializeField]
		private Vector2
			uv;

		[SerializeField]
		private bool
			uvSet;

		[SerializeField]
		private Vector3
			normal;

		[SerializeField]
		private bool
			normalValid;

		[SerializeField]
		private bool
			seam; // red(dish)

		[SerializeField]
		private bool
			sharpEdge; // cyan

		[SerializeField]
		private List<Vector2>
			_edgeUvs;


		[NonSerialized]
		private Dictionary<int, Vector2>
			edgeUvMap = new Dictionary<int, Vector2> ();


		[SerializeField]
		private List<Vector3>
			_edgeNormals;

		[NonSerialized]
		private Dictionary<int, Vector3>
			edgeNormalMap = new Dictionary<int, Vector3> ();
		
		[SerializeField]
		private List<int>
			connectedEdgeIndices = new List<int> ();
		

		
		public Vertex ()
		{
			
		}
		public bool IsSplitVertex {
			get {
				return seam || sharpEdge;
			}
		}
		public bool IsSeam {
			get {
				return seam;
			}
			set {
				this.seam = value;
				if (!this.seam) {
					// OR should we preserve these?
					edgeUvMap.Clear ();
				}
			}
		}

		public bool IsSharpEdge {
			get {
				return sharpEdge;
			}
			set {
				if (this.sharpEdge != value) {
					this.sharpEdge = value;

					InvalidateNormals ();
				}
			}
		}


		public Vector3 Position {
			get {
				return position;
			}
			set {
				if (this.position != value) {
					this.position = value;
					InvalidateNormals ();
				}
			}
		}
		public void OnBeforeSerialize ()
		{
			this._edgeUvs = new List<Vector2> ();
			this._edgeNormals = new List<Vector3> ();
			foreach (int ei in connectedEdgeIndices) {
				if (edgeUvMap.ContainsKey (ei)) {
					this._edgeUvs.Add (edgeUvMap [ei]);
				} else {
					this._edgeUvs.Add (uv);
				}
				if (edgeNormalMap.ContainsKey (ei)) {
					this._edgeNormals.Add (edgeNormalMap [ei]);
				} else {
					this._edgeNormals.Add (normal);
				}
			}
		}
		
		public void OnAfterDeserialize ()
		{
			this.edgeUvMap.Clear ();
			this.edgeNormalMap.Clear ();
			
			int edgeCount = connectedEdgeIndices.Count;
			for (int i = 0; i < edgeCount; i++) {
				if (i < _edgeUvs.Count) {
					edgeUvMap.Add (connectedEdgeIndices [i], _edgeUvs [i]);
				}
			}
			for (int i = 0; i < edgeCount; i++) {
				if (i < _edgeNormals.Count) {
					edgeNormalMap.Add (connectedEdgeIndices [i], _edgeNormals [i]);
				}
			}
		}
		//			public Vector3 Position {
		//				get {
		//					return position;
		//				}
		//			}
		
		public void EdgeConnected (int edgeIndex)
		{
			if (!connectedEdgeIndices.Contains (edgeIndex)) {
				connectedEdgeIndices.Add (edgeIndex);
				InvalidateNormals ();
			}
		}
		public void EdgeDisconnected (int edgeIndex)
		{
			if (connectedEdgeIndices.Contains (edgeIndex)) {
				connectedEdgeIndices.Remove (edgeIndex);
				InvalidateNormals ();
			}
			edgeUvMap.Remove (edgeIndex);
			edgeNormalMap.Remove (edgeIndex);

			//				int uvIndex = connectedEdgeIndices.IndexOf (edgeIndex);
			//				if (uvIndex >= 0) {
			//					connectedEdgeIndices.RemoveAt (uvIndex);
			//					seamEdgeUvs.RemoveAt (uvIndex);
			//				}
		}
		public int GetConnectedEdgeCount ()
		{
			return connectedEdgeIndices.Count;
		}
		public int[] GetConnectedEdgeIndices ()
		{
			return connectedEdgeIndices.ToArray ();
		}
		public void EdgeAdded (int edgeIndex)
		{
			EdgeAddedOrRemoved (edgeIndex, 1);
			
		}
		public void EdgeRemoved (int edgeIndex)
		{
			EdgeAddedOrRemoved (edgeIndex, -1);
			
		}
		private void EdgeAddedOrRemoved (int edgeIndex, int count)
		{
			if (count < -1 || count > 1) {
				throw new ArgumentOutOfRangeException ("count");
			}
			if (count < 0) {
				if (connectedEdgeIndices.Contains (edgeIndex)) { 
					EdgeDisconnected (edgeIndex); 
				}
			}
			
			int c = connectedEdgeIndices.Count;
			for (int i = 0; i < c; i++) {
				if (connectedEdgeIndices [i] >= edgeIndex) {
					connectedEdgeIndices [i] += count;
				}
			}
			Dictionary<int, Vector2> newUvMap = new Dictionary<int, Vector2> ();
			foreach (KeyValuePair<int, Vector2> kvp in this.edgeUvMap) {
				int ei = kvp.Key;
				if (ei >= edgeIndex) {
					ei += count;
				}
				newUvMap.Add (ei, kvp.Value);
				
			}
			this.edgeUvMap = newUvMap;

			Dictionary<int, Vector3> newNormalMap = new Dictionary<int, Vector3> ();
			foreach (KeyValuePair<int, Vector3> kvp in this.edgeNormalMap) {
				int ei = kvp.Key;
				if (ei >= edgeIndex) {
					ei += count;
				}
				newNormalMap.Add (ei, kvp.Value);
				
			}
			this.edgeNormalMap = newNormalMap;
		}
		public bool IsUvSet (int edgeIndex)
		{
			if (edgeIndex >= 0) {
				return edgeUvMap.ContainsKey (edgeIndex);
			} else {
				return uvSet;
			}
		}
		public void ClearUv (int edgeIndex)
		{
			if (edgeIndex >= 0) {
				edgeUvMap.Remove (edgeIndex);
			} else {
				uvSet = false;
				uv = Vector2.zero;
			}

		}
		public Vector2 GetUv (int edgeIndex)
		{
			if (edgeIndex >= 0) {
				if (edgeUvMap.ContainsKey (edgeIndex)) {
					return edgeUvMap [edgeIndex];
				} else {
					return uv;
				}
			} else {
				return uv;
			}

		}
		public void SetUv (Vector2 uv)
		{
			SetUv (-1, uv);
		}
		public void SetUv (int edgeIndex, Vector2 uv)
		{
			if (edgeIndex >= 0) {
				if (edgeUvMap.ContainsKey (edgeIndex)) {
					edgeUvMap [edgeIndex] = uv;
				} else {
					edgeUvMap.Add (edgeIndex, uv);
				} 
			} else {
				this.uv = uv;
				this.uvSet = true;
				edgeUvMap.Clear ();
			}
		}
		public bool IsNormalValid ()
		{
			return normalValid;
		}

		public Vector3 GetNormal ()
		{
			return GetNormal (-1);
		}
		
		public Vector3 GetNormal (int edgeIndex)
		{
			if (edgeIndex >= 0) {
				if (edgeNormalMap.ContainsKey (edgeIndex)) {
					return edgeNormalMap [edgeIndex];
				} else {
					return normal;
				}
			} else {
				return normal;
			}
		}
		private void SetNormal (int edgeIndex, Vector3 normal)
		{
			if (edgeIndex >= 0) {
				if (edgeNormalMap.ContainsKey (edgeIndex)) {
					edgeNormalMap [edgeIndex] = normal;
				} else {
					edgeNormalMap.Add (edgeIndex, normal);
				} 
			} else {
				// TODO if seam, should we set the normal for all edge connections?
				this.normal = normal;
			}
		}

		public void InvalidateNormals ()
		{
			this.normalValid = false;
			edgeNormalMap.Clear ();
		}

		public void UpdateNormals (DieModel dieModel)
		{
			if (!normalValid) {
				// Average of all edge normals!
				if (connectedEdgeIndices.Count > 0) {
					Vector3 enTotal = Vector3.zero;
					foreach (int ei in connectedEdgeIndices) {
						Edge e = dieModel.GetEdgeAt (ei);
						enTotal += e.GetNormal ();
					}
					this.normal = enTotal / (float)(connectedEdgeIndices.Count);
				} else {
					this.normal = Vector3.zero;
				}
				if (sharpEdge) {
					// Sharp edge normals = face normals
					foreach (int ei in connectedEdgeIndices) {
						Edge e = dieModel.GetEdgeAt (ei);
						Vector3 n = e.GetNormal (dieModel);
						SetNormal (ei, n);
					}
				}
				normalValid = true;
			}
		}
	}
}
