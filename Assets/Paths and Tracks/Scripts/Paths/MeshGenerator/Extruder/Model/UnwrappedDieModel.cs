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


	public class UnwrappedDieModel : IDieModel
	{

		private class UnwrappedVertex
		{
			public Vector3 position;
			public Vector3 normal;
			public Vector2 uv;
			public bool uvSet;

			public bool seam;
			public bool sharp;
			public int wrappedVertexIndex;
			public int wrappedEdgeIndex;
			public int stripIndex;

		}
		private class UnwrappedEdge : Edge
		{
			public int wrappedEdgeIndex;

//			public UnwrappedEdge () : this(-1, -1)
//			{
//				
//			}
			public UnwrappedEdge (UnwrappedEdge src) : this(src, src.wrappedEdgeIndex)
			{
			}
			public UnwrappedEdge (Edge src, int srcIndex) : base(src)
			{
				this.wrappedEdgeIndex = srcIndex;
			}
		}
		private IDieModel wrappedModel;

		private List<UnwrappedVertex> vertices = new List<UnwrappedVertex> ();
		private List<UnwrappedEdge> edges = new List<UnwrappedEdge> ();
	
		private List<List<int>> _strips = new List<List<int>> ();

		private UnwrappedDieModel ()
		{
		}
		public IDieModel WrappedModel {
			get {
				return wrappedModel;
			}
		}


		public void Rebuild ()
		{
			this.vertices.Clear ();
			this.edges.Clear ();
			this._strips.Clear ();

			if (!(wrappedModel is IDieModelGraphSupport)) {
				//throw new NotSupportedException ("The provided IDieModel doesn't support IDieModelGraphSupport: " + dieModel);
				return;
			}
			IDieModelGraphSupport graphSupport = (IDieModelGraphSupport)wrappedModel;

			List<List<int>> wrappedStrips = graphSupport.FindConnectedEdgeGraphs (true, true);
			
			int stripCount = wrappedStrips.Count;
			for (int stripIndex = 0; stripIndex < stripCount; stripIndex++) {

				List<int> wrappedStrip = wrappedStrips [stripIndex];

				Dictionary<int, int> vertexToUnwrappedVertexMap = new Dictionary<int, int> ();
				
				// Also build a lookup fromVertex -> edge for ordering of edges (below)
				Dictionary<int, int> fromVertexIndexToEdgeIndexMap = new Dictionary<int, int> ();
				
				// And also a set of strip 'toVertex' indices
				HashSet<int> toVertexIndices = new HashSet<int> ();
				
				int firstEdgeWithSeamVertex = -1;
				
				List<UnwrappedEdge> edges = new List<UnwrappedEdge> ();
				foreach (int ei in wrappedStrip) {
					Edge e = wrappedModel.GetEdgeAt (ei);
					
					int fromUnwrappedVertexIndex, toUnwrappedVertexIndex;
					UnwrappedVertex vFrom = GetUnwrappedVertex (wrappedModel, this, vertexToUnwrappedVertexMap, e.GetFromVertexIndex (), ei, out fromUnwrappedVertexIndex);
					vFrom.stripIndex = stripIndex;
					
					UnwrappedVertex vTo = GetUnwrappedVertex (wrappedModel, this, vertexToUnwrappedVertexMap, e.GetToVertexIndex (), ei, out toUnwrappedVertexIndex);
					vTo.stripIndex = stripIndex;
					toVertexIndices.Add (toUnwrappedVertexIndex);

					UnwrappedEdge unwrappedEdge = new UnwrappedEdge (e, ei);
					unwrappedEdge.SetFromVertexIndex (fromUnwrappedVertexIndex);
					unwrappedEdge.SetToVertexIndex (toUnwrappedVertexIndex);
					
					edges.Add (unwrappedEdge);
					int unwrappedEdgeIndex = edges.Count - 1;
					fromVertexIndexToEdgeIndexMap [fromUnwrappedVertexIndex] = unwrappedEdgeIndex;
					
					if (firstEdgeWithSeamVertex < 0 && vFrom.seam) {
						firstEdgeWithSeamVertex = unwrappedEdgeIndex;
					}
				}
				
				// Find strip head edge
				// 1. Edge with 'from' vertex marked as seam
				// 2. Edge with 'fromVertex' outside the strip
				// 3. Random edge (= first edge in the strip)
				int headEdgeIndex = 0;
				if (firstEdgeWithSeamVertex >= 0) {
					headEdgeIndex = firstEdgeWithSeamVertex;
				} else {
//					HashSet<int> stripVertices = new HashSet<int> (vertexToUnwrappedVertexMap.Values);
					for (int ei = 0; ei < edges.Count; ei++) {
						Edge e = edges [ei];
						int from = e.GetFromVertexIndex ();
//						UnwrappedVertex v = this.vertices [from];
						if (!toVertexIndices.Contains (from)) {
							headEdgeIndex = ei;
							break;
						}
					}
				}
				
				// Add edges in order, starting from the "head"
				// Count the total length of the strip also
				List<int> unwrappedStrip = new List<int> ();

				float totalLength = 0.0f;
				for (int ei = headEdgeIndex; ei>= 0;) {
					UnwrappedEdge e = edges [ei];
					float len = e.GetLength (this);
					totalLength += len;
					this.edges.Add (e);
					unwrappedStrip.Add (ei);

					int toIndex = e.GetToVertexIndex ();
					if (fromVertexIndexToEdgeIndexMap.ContainsKey (toIndex)) {
						ei = fromVertexIndexToEdgeIndexMap [toIndex];
					} else {
						// No more
						ei = -1;
					}
				}
				
				this._strips.Add (unwrappedStrip);
			}
			Debug.LogFormat ("Unwrapped model: {0} vertices, {1} edges", this.vertices.Count, this.edges.Count);
		}

		public static UnwrappedDieModel Build (IDieModel dieModel)
		{
			UnwrappedDieModel um = new UnwrappedDieModel ();
			um.wrappedModel = dieModel;
			um.Rebuild ();

			return um;
		}
		private static UnwrappedVertex GetUnwrappedVertex (IDieModel dieModel, UnwrappedDieModel unwrappedModel, Dictionary<int, int> vertexToUnwrappedVertexMap, int wrappedVertexIndex, int wrappedEdgeIndex, out int unwrappedVertexIndex)
		{
			UnwrappedVertex unwrappedVertex;
			dieModel.IsSeamAt (wrappedVertexIndex);
			if (vertexToUnwrappedVertexMap.ContainsKey (wrappedVertexIndex)) {
				unwrappedVertexIndex = vertexToUnwrappedVertexMap [wrappedVertexIndex];
				unwrappedVertex = unwrappedModel.vertices [unwrappedVertexIndex];
			} else {
				unwrappedVertex = new UnwrappedVertex ();
				unwrappedVertex.position = dieModel.GetVertexAt (wrappedVertexIndex);
				unwrappedVertex.normal = dieModel.GetNormalAt (wrappedVertexIndex, wrappedEdgeIndex);
				unwrappedVertex.uvSet = dieModel.IsUvSetAt (wrappedVertexIndex, wrappedEdgeIndex);
				unwrappedVertex.uv = dieModel.GetUvAt (wrappedVertexIndex, wrappedEdgeIndex);
				unwrappedVertex.seam = dieModel.IsSeamAt (wrappedVertexIndex);
				unwrappedVertex.sharp = dieModel.IsSharpVertexAt (wrappedVertexIndex);
				unwrappedVertex.wrappedVertexIndex = wrappedVertexIndex;
				unwrappedVertex.wrappedEdgeIndex = wrappedEdgeIndex;
				unwrappedVertex.stripIndex = 0;
				unwrappedModel.vertices.Add (unwrappedVertex);
				unwrappedVertexIndex = unwrappedModel.vertices.Count - 1;
				vertexToUnwrappedVertexMap.Add (wrappedVertexIndex, unwrappedVertexIndex);
			}
			return unwrappedVertex;
		}

//	
		public int GetStripCount ()
		{
			return _strips.Count;
		}
		public int[] GetStripAt (int index)
		{
			return _strips [index].ToArray ();
		}
		public int GetWrappedVertexIndex (int index)
		{
			return vertices [index].wrappedVertexIndex;
		}
		public int GetUnwrappedVertexIndex (int index)
		{
			if (index >= 0) {
				// TODO create a lookup map!
				for (int i = 0; i < vertices.Count; i++) {
					if (vertices [i].wrappedVertexIndex == index) {
						return i;
					}
				}
			}
			return -1;
		}
		public int GetWrappedEdgeIndex (int index)
		{
			if (index >= 0) {
				UnwrappedEdge e = edges [index];
				return e.wrappedEdgeIndex;
			} else {
				return -1;
			}
		}
		public int GetUnwrappedEdgeIndex (int index)
		{
			if (index >= 0) {
				// TODO create a lookup map!
				for (int i = 0; i < edges.Count; i++) {
					if (edges [i].wrappedEdgeIndex == index) {
						return i;
					}
				}
			}
			return -1;
		}
		public int GetVertexStripIndex (int index)
		{
			return vertices [index].stripIndex;
		}
		public int GetEdgeStripIndex (int index)
		{
			int vertexIndex = this.edges [index].GetFromVertexIndex ();
			return GetVertexStripIndex (vertexIndex);
		}
		
		public void SetUvAt (int index, Vector2 uv)
		{
			UnwrappedVertex vertex = vertices [index];
			vertex.uv = uv;
			vertex.uvSet = true;
			if (wrappedModel is IMutableDieModel) {
				((IMutableDieModel)wrappedModel).SetUvAt (vertex.wrappedVertexIndex, vertex.wrappedEdgeIndex, uv);
			}
		}
		public void ClearUvAt (int index)
		{
			UnwrappedVertex vertex = vertices [index];
			vertex.uvSet = false;
			if (wrappedModel is IMutableDieModel) {
				((IMutableDieModel)wrappedModel).ClearUvAt (vertex.wrappedVertexIndex, vertex.wrappedEdgeIndex);
			}
		}

		// IDieModel
		public bool SupportsSplitVertices ()
		{
			return false;
		}

		// IDieModel
		public bool SupportsUvs ()
		{
			return true;
		}

		// IDieModel
		public bool SupportsVertexNormals ()
		{
			return true;
		}

		// IDieModel
		public int GetVertexCount ()
		{
			return vertices.Count;
		}

		// IDieModel
		public Vector3 GetVertexAt (int index)
		{
			return vertices [index].position;
		}

		// IDieModel
		public Vector3[] GetVertices ()
		{
			Vector3[] arr = new Vector3[vertices.Count];
			int i = 0;
			foreach (UnwrappedVertex v in vertices) {
				arr [i++] = v.position;
			}
			return arr;
		}

		// IDieModel
		public bool IsSeamAt (int vertexIndex)
		{
			return vertices [vertexIndex].seam;
		}

		// IDieModel
		public bool IsSharpVertexAt (int vertexIndex)
		{
			return vertices [vertexIndex].sharp;
		}

		// IDieModel
		public bool IsUvSetAt (int vertexIndex)
		{
			return vertices [vertexIndex].uvSet;
		}
		
		// IDieModel
		public bool IsUvSetAt (int vertexIndex, int edgeIndex)
		{
			return IsUvSetAt (vertexIndex);
		}

		// IDieModel
		public Vector2 GetUvAt (int vertexIndex)
		{
			return vertices [vertexIndex].uv;
		}

		// IDieModel
		public Vector2 GetUvAt (int vertexIndex, int edgeIndex)
		{
			return GetUvAt (vertexIndex);
		}

		// IDieModel
		public Vector2[] GetUvs ()
		{
			Vector2[] arr = new Vector2[vertices.Count];
			int i = 0;
			foreach (UnwrappedVertex v in vertices) {
				arr [i++] = v.uv;
			}
			return arr;
		}
		
		// IDieModel
		public Vector3 GetNormalAt (int vertexIndex)
		{
			return vertices [vertexIndex].normal;
		}

		// IDieModel
		public Vector3 GetNormalAt (int vertexIndex, int edgeIndex)
		{
			return vertices [vertexIndex].normal;
		}

		// IDieModel
		public Vector3[] GetNormals ()
		{
			Vector3[] arr = new Vector3[vertices.Count];
			int i = 0;
			foreach (UnwrappedVertex v in vertices) {
				arr [i++] = v.normal;
			}
			return arr;
		}
		
		// IDieModel
		public int GetEdgeCount ()
		{
			return edges.Count;
		}

		// IDieModel
		public Edge GetEdgeAt (int index)
		{
			return edges [index];
		}

		// IDieModel
		public Vector3[] GetEdgeVertices (int edgeIndex)
		{
			return new Vector3[] {
				vertices [edges [edgeIndex].GetFromVertexIndex ()].position,
				vertices [edges [edgeIndex].GetToVertexIndex ()].position,
			};
		}

		// IDieModel
		public int[] GetConnectedEdgeIndices (int vertexIndex)
		{
			if (vertexIndex < 0 || vertexIndex >= vertices.Count) {
				throw new IndexOutOfRangeException ("vertexIndex < 0 || vertexIndex >= " + vertices.Count + ": " + vertexIndex);
			}
			List<int> l = new List<int> ();
			int ei = -1;
			foreach (Edge e in edges) {
				ei++;
				int from = e.GetFromVertexIndex ();
				if (from == vertexIndex) {
					l.Add (ei);
				} else {
					int to = e.GetToVertexIndex ();
					if (to == vertexIndex) {
						l.Add (ei);
					}
				}
			}
			return l.ToArray ();
		}

	}


}
