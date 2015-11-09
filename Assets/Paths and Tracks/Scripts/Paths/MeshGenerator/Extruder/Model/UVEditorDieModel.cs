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

	public class UVEditorDieModel : IDieModel, IMutableDieModel, IDieModelEditorSupport
	{
		private UnwrappedDieModel unwrappedModel;
		private DefaultDieModelSelectionSupport selectionSupport = new DefaultDieModelSelectionSupport ();
//		private event DieModelChangeHandler Changed; 

		private int _batchDepth = 0;

		private UVEditorDieModel (UnwrappedDieModel unwrappedModel)
		{
			this.unwrappedModel = unwrappedModel;
		}

		public static UVEditorDieModel Build (UnwrappedDieModel unwrappedModel)
		{
			UVEditorDieModel m = new UVEditorDieModel (unwrappedModel);
			m.Rebuild (false);
			return m;
		}
		private class StripVertex
		{
			public int vertexIndex;
			public float distanceFromPrevious;
			public bool hasUv;
			public Vector2 uv;
			public StripVertex (int index)
			{
				this.vertexIndex = index;
			}
		}
		public void Rebuild (bool rebuildUnwrappedModel)
		{
			if (rebuildUnwrappedModel) {
				unwrappedModel.Rebuild ();
			}

			BatchOperation ("UV Unwrap", DoInterpolateWrappedModelUVs);

			//Vector2[] uvs = unwrappedModel.GetUvs ();
//			int vertexCount = unwrappedModel.GetVertexCount ();
//			nextSliceUvs = new Vector2[vertexCount];
//			nextSliceUvSet = new bool[vertexCount];
//			for (int i = 0; i < vertexCount; i++) {
//				bool uvSet = unwrappedModel.IsUvSetAt (i);
//				nextSliceUvSet [i] = uvSet;
//				if (uvSet) {
//					Vector2 uv = unwrappedModel.GetUvAt (i);
//					nextSliceUvs [i] = new Vector2 (uv.x, 1.0f);
//				}
//			}
//
			FireRefreshEvent ();
		}

		private List<StripVertex> BuildVertexStrip (int stripIndex)
		{
			List<StripVertex> stripVertices = new List<StripVertex> ();
			int[] stripEdges = unwrappedModel.GetStripAt (stripIndex);
			int stripEdgeCount = stripEdges.Length;
			if (stripEdgeCount > 0) {

				// First vertex is the 'from' vertex of the first edge:
				Edge firstEdge = GetEdgeAt (stripEdges [0]);
				int firstVertex = firstEdge.GetFromVertexIndex ();
				StripVertex firstStripVertex = new StripVertex (firstVertex);
				firstStripVertex.distanceFromPrevious = 0f;
				firstStripVertex.hasUv = IsUvSetAt (firstVertex);
				firstStripVertex.uv = GetUvAt (firstVertex);
				stripVertices.Add (firstStripVertex);
				float currentDist = firstEdge.GetLength (unwrappedModel);

				// Add 'to' vertices of each edge:
				for (int i = 0; i < stripEdgeCount; i++) {
					int ei = stripEdges [i];
					Edge e = GetEdgeAt (ei);
					
					int vi = e.GetToVertexIndex ();
					StripVertex sv = new StripVertex (vi);
					sv.distanceFromPrevious = currentDist;
					sv.hasUv = IsUvSetAt (vi);
					sv.uv = GetUvAt (vi);
					stripVertices.Add (sv);
					
					currentDist += e.GetLength (unwrappedModel);
				}
			}
			return stripVertices;
		}
		private void DoInterpolateWrappedModelUVs (IDieModel model)
		{
			int stripCount = unwrappedModel.GetStripCount ();
			for (int si = 0; si < stripCount; si++) {
				
				List<StripVertex> stripVertices = BuildVertexStrip (si);
				int vertexCount = stripVertices.Count;
				if (vertexCount > 0) {
					// Set first and last UV if not already set:
					if (!stripVertices [0].hasUv) {
						stripVertices [0].uv = Vector2.zero;
						stripVertices [0].hasUv = true;
						SetUvAt (0, Vector2.zero);
					}
					if (!stripVertices [vertexCount - 1].hasUv) {
						stripVertices [vertexCount - 1].uv = new Vector2 (1, 0);
						stripVertices [vertexCount - 1].hasUv = true;
						SetUvAt (vertexCount - 1, new Vector2 (1, 0));
					}
					
					// Now interpolate all missing values
					for (int i = 0; i < vertexCount; i++) {
						if (!stripVertices [i].hasUv) {
							// Interpolate until next known; first find the next known:
							int nextWithUv = -1;
							float distToNextKnown = 0.0f;
							for (int j = i + 1; j < vertexCount; j++) {
								distToNextKnown += stripVertices [j].distanceFromPrevious;
								if (stripVertices [j].hasUv) {
									// Found with uv
									nextWithUv = j;
									break;
								}
							}

							float currentDistFromPrev = 0f;
							for (int j = i; j < nextWithUv; j++) {
								int vi = stripVertices [j].vertexIndex;
								currentDistFromPrev += stripVertices [j].distanceFromPrevious;
								
								Vector2 uvFrom = stripVertices [i - 1].uv;
								Vector2 uvTo = stripVertices [nextWithUv].uv;
								float t = currentDistFromPrev / distToNextKnown;
								Vector2 uv = Vector2.Lerp (uvFrom, uvTo, t);
								// TODO we don't actually need stripvertex uv's after this point so
								// following two lines are unnecessary:
								stripVertices [j].uv = uv;
								stripVertices [j].hasUv = true;
								SetUvAt (vi, uv);
							}
							// Continue main loop after this segment:
							i = nextWithUv + 1;
						}
					}
				}
			}
		}


		public UnwrappedDieModel UnwrappedModel {
			get {
				return unwrappedModel;
			}
		}

		private void Modify (DieModelChangedEventArgs e, Action a)
		{
//			if (_batchDepth <= 0) {
//				FireChangedEvent (EventPhase.Before, e);
//			}
			a ();
//			if (_batchDepth <= 0) {
//				FireChangedEvent (EventPhase.After, e);
//			}
		}

//		private void FireChangedEvent (EventPhase phase, DieModelChangedEventArgs e, bool fireInBatch = false)
//		{
//			if ((fireInBatch || _batchDepth >= 0) && null != Changed) {
//				try {
//					DieModelChangedEventArgs e2 = new DieModelChangedEventArgs (e, phase);
//					Changed (e2);
//				} catch (Exception ex) {
//					Debug.LogError ("Catched an exception from Changed handler: " + ex);
//				}
//			}
//		}

		// IDieModel
		public int GetVertexCount ()
		{
			return unwrappedModel.GetVertexCount ();
		}

		// IDieModel
		public Vector3 GetVertexAt (int index)
		{
			return unwrappedModel.GetUvAt (index);
		}

		// IDieModel
		public Vector3[] GetVertices ()
		{
			Vector2[] uvs = unwrappedModel.GetUvs ();
			Vector3[] vertices = new Vector3[uvs.Length];
			for (int i = 0; i < vertices.Length; i ++) {
				vertices [i] = uvs [i];
			}
			return vertices;
		}

		// IDieModel
		public bool SupportsSplitVertices ()
		{
			return false;
		}

		// IDieModel
		public bool IsSeamAt (int vertexIndex)
		{
			return unwrappedModel.IsSeamAt (vertexIndex);
		}

		// IDieModel
		public bool IsSharpVertexAt (int vertexIndex)
		{
			return unwrappedModel.IsSharpVertexAt (vertexIndex);
		}

		// IDieModel
		public bool SupportsUvs ()
		{
			return false;
		}

		// IDieModel
		public bool IsUvSetAt (int vertexIndex)
		{
			return unwrappedModel.IsUvSetAt (vertexIndex);

		}

		// IDieModel
		public bool IsUvSetAt (int vertexIndex, int edgeIndex)
		{
			return IsUvSetAt (vertexIndex);
		}

		// IDieModel
		public Vector2 GetUvAt (int vertexIndex)
		{
			return GetVertexAt (vertexIndex);

		}

		// IDieModel
		public Vector2 GetUvAt (int vertexIndex, int edgeIndex)
		{
			return GetUvAt (vertexIndex);
		}

		// IDieModel
		public Vector2[] GetUvs ()
		{
			return unwrappedModel.GetUvs ();
		}

		// IDieModel
		public bool SupportsVertexNormals ()
		{
			return false;
		}

		// IDieModel
		public Vector3 GetNormalAt (int vertexIndex)
		{
			return unwrappedModel.GetNormalAt (vertexIndex);
		}

		// IDieModel
		public Vector3 GetNormalAt (int vertexIndex, int edgeIndex)
		{
			return unwrappedModel.GetNormalAt (vertexIndex, edgeIndex);
		}

		// IDieModel
		public Vector3[] GetNormals ()
		{
			return unwrappedModel.GetNormals ();
		}

		// IDieModel
		public int GetEdgeCount ()
		{
			return unwrappedModel.GetEdgeCount ();
		}

		// IDieModel
		public Edge GetEdgeAt (int index)
		{
			return unwrappedModel.GetEdgeAt (index);
		}

		// IDieModel
		public Vector3[] GetEdgeVertices (int edgeIndex)
		{
			return unwrappedModel.GetEdgeVertices (edgeIndex);
		}
		// IDieModel
		public int[] GetConnectedEdgeIndices (int vertexIndex)
		{
			return unwrappedModel.GetConnectedEdgeIndices (vertexIndex);
		}

		// IMutableDieModel
		public void AddDieModelChangeHandler (DieModelChangeHandler h)
		{
//			Changed -= h;
//			Changed += h;
			((IMutableDieModel)unwrappedModel.WrappedModel).AddDieModelChangeHandler (h);
		}

		// IMutableDieModel
		public void RemoveDieModelChangeHandler (DieModelChangeHandler h)
		{
//			Changed -= h;
			((IMutableDieModel)unwrappedModel.WrappedModel).RemoveDieModelChangeHandler (h);
		}

		// IMutableDieModel
		public SupportedModelOps GetSupportedModelOps ()
		{
			return SupportedModelOps.SetVertex;
		}

		// IMutableDieModel
		public void SetVertexAt (int index, Vector3 v)
		{
			DieModelChangedEventArgs e = DieModelChangedEventArgs.VerticesModified (this, index);
			Modify (e, () => DoSetVertexAt (index, v));
		}
		private void DoSetVertexAt (int index, Vector3 v)
		{
			unwrappedModel.SetUvAt (index, v);
		}

		// IMutableDieModel
		public void SetVerticesAt (int[] indices, Vector3[] positions)
		{
			DieModelChangedEventArgs e = DieModelChangedEventArgs.VerticesModified (this, indices);
			Modify (e, () => {
				for (int i = 0; i < indices.Length; i++) {
					int vi = indices [i];
					DoSetVertexAt (vi, positions [i]);
				}
			});
		}

		// IMutableDieModel
		public int AddVertex (Vector3 v)
		{
			throw new NotSupportedException ("Vertices can't be added to the UV mapping model");
		}

		// IMutableDieModel
		public void InsertVertex (int insertAt, Vector3 v)
		{
			throw new NotSupportedException ("Vertices can't be added to the UV mapping model");
		}

		// IMutableDieModel
		public void RemoveVertexAt (int index)
		{
			throw new NotSupportedException ("Vertices can't be removed from the UV mapping model");
		}

		// IMutableDieModel
		public void RemoveVerticesAt (params int[] indices)
		{
			throw new NotSupportedException ("Vertices can't be removed from the UV mapping model");
		}

		// IMutableDieModel
		public void RemoveVerticesAt (IEnumerable<int> indices)
		{
			throw new NotSupportedException ("Vertices can't be removed from the UV mapping model");
		}

		// IMutableDieModel
		public void SetSeamAt (int vertexIndex, bool value)
		{
			throw new NotSupportedException ("Seams can't be defined within the UV mapping model");
		}

		// IMutableDieModel
		public void SetSharpVertexAt (int vertexIndex, bool value)
		{
			throw new NotSupportedException ("Sharp vertices can't be defined within the UV mapping model");
		}

		// IMutableDieModel
		public void SetUvAt (int index, Vector2 uv)
		{
			SetVertexAt (index, uv);
		}

		// IMutableDieModel
		public void SetUvAt (int vertexIndex, int edgeIndex, Vector2 uv)
		{
			SetUvAt (vertexIndex, uv);
		}

		// IMutableDieModel
		public void ClearUvAt (int index)
		{
			DieModelChangedEventArgs e = DieModelChangedEventArgs.VerticesModified (this, index);
			Modify (e, () => {
				DoSetVertexAt (index, Vector3.zero);
				unwrappedModel.ClearUvAt (index);
			});
		}

		// IMutableDieModel
		public void ClearUvAt (int index, int edgeIndex)
		{
			ClearUvAt (index);
		}

		// IMutableDieModel
		public int SetEdgeAt (int index, Edge e)
		{
			throw new NotSupportedException ("Edges can't be modified to the UV mapping model");
		}

		// IMutableDieModel
		public int AddEdge (int fromVertexIndex, int toVertexIndex)
		{
			throw new NotSupportedException ("Edges can't be added to the UV mapping model");
		}

		// IMutableDieModel
		public int InsertEdge (int insertAt, int fromVertexIndex, int toVertexIndex)
		{
			throw new NotSupportedException ("Edges can't be added to the UV mapping model");
		}

		// IMutableDieModel
		public void RemoveEdgeAt (int edgeIndex, bool deleteOrphanVertices)
		{
			throw new NotSupportedException ("Edges can't be removed to the UV mapping model");
		}

		// IMutableDieModel
		public void RemoveEdgesAt (bool deleteOrphanVertices, params int[] indices)
		{
			throw new NotSupportedException ("Edges can't be removed to the UV mapping model");
		}

		// IMutableDieModel
		public void RemoveEdgesAt (bool deleteOrphanVertices, IEnumerable<int> indices)
		{
			throw new NotSupportedException ("Edges can't be removed to the UV mapping model");
		}

		// IMutableDieModel
		public void BatchOperation (string name, Action<IDieModel> a)
		{
			IMutableDieModel mutableWrappedModel = unwrappedModel.WrappedModel as IMutableDieModel;
			mutableWrappedModel.BatchOperation (name, (wmodel) => {
				a (this);
			});
//			try {
//				_batchDepth++;
//
//				DieModelChangedEventArgs e = new DieModelChangedEventArgs (this, EventPhase.Before, name);
//				FireChangedEvent (EventPhase.Before, e, true);
//
//				a (this);
//
//				FireChangedEvent (EventPhase.After, e, true);
//
//			} finally {
//				_batchDepth --;
//			}
		}

		// IDieModelEditorSupport
		public void FireRefreshEvent ()
		{
//			FireChangedEvent (EventPhase.After, DieModelChangedEventArgs.RefreshModel (this));
			if (unwrappedModel.WrappedModel is IDieModelEditorSupport) {
				((IDieModelEditorSupport)unwrappedModel.WrappedModel).FireRefreshEvent ();
			}
		}
		
		// IDieModelEditorSupport
		public IDieModelSelectionSupport GetDieModelSelectionSupport ()
		{
			return selectionSupport;
		}
	}

}
