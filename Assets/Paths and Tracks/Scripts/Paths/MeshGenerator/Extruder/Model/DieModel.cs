// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using System;
using UnityEngine;
using System.Collections.Generic;

using Util;
using Paths;
using Paths.MeshGenerator;
using Paths.MeshGenerator.Extruder.Model.Internal;

namespace Paths.MeshGenerator.Extruder.Model
{


	public interface IDieModelContainer
	{
		DieModel GetDieModel ();
	}
	[ExecuteInEditMode]
	public class DieModel : MonoBehaviour, IMutableDieModel, IDieModelGraphSupport, IDieModelEditorSupport, IDieModelContainer
	{
		private event DieModelChangeHandler Changed;


		[SerializeField]
		private List<Vertex>
			vertices;


		[SerializeField]
		private List<Edge>
			edges;

//		[SerializeField]
//		private List<int>
//			seams;


//		[NonSerialized]
//		private Dictionary<int, Vector3>
//			edgeNormals = new Dictionary<int, Vector3> ();


		//private string name;
		[NonSerialized]
		private int
			_batchDepth = 0;

		[NonSerialized]
		private DefaultDieModelSelectionSupport
			selectionSupport = new DefaultDieModelSelectionSupport ();

		public DieModel ()
		{
		}

		void OnEnable ()
		{
			selectionSupport.AttachModel (this);
		}
		void OnDisable ()
		{

		}
		void OnDestroy ()
		{
			selectionSupport.DetachModel (this);
		}

		// From IDieModelContainer
		public DieModel GetDieModel ()
		{
			return this;
		}

		public void AddDieModelChangeHandler (DieModelChangeHandler h)
		{
			this.Changed -= h;
			this.Changed += h;
		}
		public void RemoveDieModelChangeHandler (DieModelChangeHandler h)
		{
			this.Changed -= h;
		}

		public void FireRefreshEvent ()
		{
			DieModelChangedEventArgs e = DieModelChangedEventArgs.RefreshModel (this);
//			FireChangedEvent (false, e);
			FireChangedEvent (EventPhase.After, e);

		}
		public IDieModelSelectionSupport GetDieModelSelectionSupport ()
		{
			return selectionSupport;
		}


		public void BatchOperation (string name, Action<IDieModel> a)
		{
			// In batch
			try {
				_batchDepth++;
				FireBatchOperationEvent (EventPhase.Before, name);
				a (this);
				FireBatchOperationEvent (EventPhase.After, name);
			} finally {
				_batchDepth--;
			}
		}

		private void FireBatchOperationEvent (EventPhase phase, string name)
		{
			DieModelChangedEventArgs e = new DieModelChangedEventArgs (this, phase, name);
			if (null != Changed) {
				try {
					Changed (e);
				} catch (Exception ex) {
					Debug.LogError ("An exception in Changed handler: " + ex);
				}
			}
		}

		private void FireChangedEvent (EventPhase phase, DieModelChangedEventArgs e)
		{
			if (_batchDepth == 0) {
				if (null != Changed) {
					DieModelChangedEventArgs e2 = new DieModelChangedEventArgs (e, phase);
					try {
						Changed (e2);
					} catch (Exception ex) {
						Debug.LogError ("An exception in Changed handler: " + ex);
					} finally {
					}
				}
			}
		}

		void ModifyVertex (int vertexIndex, Action a)
		{
			if (vertexIndex < 0 || vertexIndex >= vertices.Count) {
				throw new ArgumentOutOfRangeException ("vertexIndex", vertexIndex, "vertexIndex < 0 || vertexIndex >= " + vertices.Count);
			}
			if (_batchDepth <= 0) {
				DieModelChangedEventArgs e = DieModelChangedEventArgs.VerticesModified (this, vertexIndex);
				FireChangedEvent (EventPhase.Before, e);
				a ();
				FireChangedEvent (EventPhase.After, e);
			} else {
				a ();
			}
		}
		
		void ModifyVertices (int[] indices, Action<int> a)
		{
			if (_batchDepth <= 0) {
				DieModelChangedEventArgs e = DieModelChangedEventArgs.VerticesModified (this, indices);
				FireChangedEvent (EventPhase.Before, e);
				for (int i = 0; i < indices.Length; i++) {
					a (i);
				}
				FireChangedEvent (EventPhase.After, e);
			} else {
				for (int i = 0; i < indices.Length; i++) {
					a (i);
				}
			}
		}
		void ModifyEdge (int edgeIndex, Action a)
		{
			ModifyEdge (edgeIndex, () => {
				a ();
				return 0;
			});
		}
		int ModifyEdge (int edgeIndex, Func<int> f)
		{
			if (edgeIndex < 0 || edgeIndex >= edges.Count) {
				throw new ArgumentOutOfRangeException ("edgeIndex", edgeIndex, "edgeIndex < 0 || edgeIndex >= " + edges.Count);
			}
			int retval;
			if (_batchDepth <= 0) {
				DieModelChangedEventArgs e = DieModelChangedEventArgs.EdgesModified (this, edgeIndex);
				FireChangedEvent (EventPhase.Before, e);
				retval = f ();
				FireChangedEvent (EventPhase.After, e);
			} else {
				retval = f ();
			}
			return retval;
		}
		
		void ModifyEdges (int[] indices, Action<int> a)
		{
			if (_batchDepth <= 0) {
				DieModelChangedEventArgs e = DieModelChangedEventArgs.EdgesModified (this, indices);
				FireChangedEvent (EventPhase.Before, e);
				for (int i = 0; i < indices.Length; i++) {
					a (i);
				}
				FireChangedEvent (EventPhase.After, e);
			} else {
				for (int i = 0; i < indices.Length; i++) {
					a (i);
				}
			}
		}
		void Modify (DieModelChangedEventArgs e, Action a)
		{
			if (_batchDepth <= 0) {
				FireChangedEvent (EventPhase.Before, e);
				a ();
				FireChangedEvent (EventPhase.After, e);
			} else {
				a ();
			}
		}
		void Modify (DieModelChangedEventArgs e, Action<DieModelChangedEventArgs> a)
		{

			if (_batchDepth <= 0) {
				FireChangedEvent (EventPhase.Before, e);
				a (e);
				FireChangedEvent (EventPhase.After, e);
			} else {
				a (e);
			}
		}
		int Modify (DieModelChangedEventArgs e, Func<DieModelChangedEventArgs, int> f)
		{
			int retval;
			if (_batchDepth <= 0) {
				FireChangedEvent (EventPhase.Before, e);
				retval = f (e);
				FireChangedEvent (EventPhase.After, e);
			} else {
				retval = f (e);
			}
			return retval;
		}

		
		public string GetName ()
		{
			return "Test Model";
		}
		public bool SupportsSplitVertices ()
		{
			return true;
		}
		public bool SupportsUvs ()
		{
			return true;
		}
		public bool SupportsVertexNormals ()
		{
			return true;
		}

		public int GetVertexCount ()
		{
			return (null != vertices) ? vertices.Count : 0;
		}
		public Vector3 GetVertexAt (int index)
		{
			if (index < 0 || index >= vertices.Count) {
				throw new ArgumentOutOfRangeException ("index", index, "index < 0 || index >= " + vertices.Count);
			}
			
			return vertices [index].Position;
		}

		public bool IsUvSetAt (int index)
		{
			return IsUvSetAt (index, -1);
		}
		public bool IsUvSetAt (int vertexIndex, int edgeIndex)
		{
			if (vertexIndex < 0 || vertexIndex >= vertices.Count) {
				throw new ArgumentOutOfRangeException ("vertexIndex", vertexIndex, "vertexIndex < 0 || vertexIndex >= " + vertices.Count);
			}
			Vertex v = vertices [vertexIndex];
			return v.IsUvSet (edgeIndex);
		}

		public Vector2 GetUvAt (int vertexIndex)
		{
			return GetUvAt (vertexIndex, -1);
		}
		public Vector2 GetUvAt (int vertexIndex, int edgeIndex)
		{
			if (vertexIndex < 0 || vertexIndex >= vertices.Count) {
				throw new ArgumentOutOfRangeException ("vertexIndex", vertexIndex, "vertexIndex < 0 || vertexIndex >= " + vertices.Count);
			}
			Vertex v = vertices [vertexIndex];
			if (v.IsSeam && edgeIndex >= 0 && edgeIndex >= edges.Count) {
				throw new ArgumentOutOfRangeException ("edgeIndex", edgeIndex, "edgeIndex >= " + edges.Count);
			}
			return v.GetUv (edgeIndex);
		}
		public Vector3 GetNormalAt (int vertexIndex)
		{
			return GetNormalAt (vertexIndex, -1);
		}
		public Vector3 GetNormalAt (int vertexIndex, int edgeIndex)
		{
			if (vertexIndex < 0 || vertexIndex >= vertices.Count) {
				throw new ArgumentOutOfRangeException ("vertexIndex", vertexIndex, "vertexIndex < 0 || vertexIndex >= " + vertices.Count);
			}
			Vertex v = vertices [vertexIndex];
			if (v.IsSeam && edgeIndex >= 0 && edgeIndex >= edges.Count) {
				throw new ArgumentOutOfRangeException ("edgeIndex", edgeIndex, "edgeIndex >= " + edges.Count);
			}
			v.UpdateNormals (this);
			return v.GetNormal (edgeIndex);
		}
		public Vector3[] GetVertices ()
		{
			Vector3[] arr = new Vector3[vertices.Count];
			for (int i = 0; i < arr.Length; i++) {
				arr [i] = vertices [i].Position;
			}
			return arr;
		}
		public Vector2[] GetUvs ()
		{
			Vector2[] arr = new Vector2[vertices.Count];
			for (int i = 0; i < arr.Length; i++) {
				arr [i] = vertices [i].GetUv (-1);
			}
			return arr;
		}
		public Vector3[] GetNormals ()
		{
			Vector3[] arr = new Vector3[vertices.Count];
			for (int i = 0; i < arr.Length; i++) {
				arr [i] = vertices [i].GetNormal ();
			}
			return arr;
		}

		public SupportedModelOps GetSupportedModelOps ()
		{
			return SupportedModelOps.All;
		}

		public void SetVertexAt (int index, Vector3 v)
		{
			ModifyVertex (index, () => DoSetVertexAt (index, v, false));
		}
		private void DoSetVertexAt (int index, Vector3 v, bool checkIndex)
		{
			if (checkIndex && index < 0 || index >= vertices.Count) {
				throw new ArgumentOutOfRangeException ("index", index, "index < 0 || index >= " + vertices.Count);
			}
			Vertex vertex = vertices [index];
			if (vertex.Position != v) {
				vertex.Position = v;

				// Reset connected edge metrics:
				foreach (int ei in FindConnectedEdgeIndices(index, FindEdgesFlags.None)) {
					Edge e = edges [ei];
					e.ResetMetrics ();
					vertices [e.GetFromVertexIndex ()].InvalidateNormals ();
					vertices [e.GetToVertexIndex ()].InvalidateNormals ();

				}
				vertex.InvalidateNormals ();
			}
		}
		public void SetVerticesAt (int[] indices, Vector3[] positions)
		{
			if (indices.Length != positions.Length) {
				throw new ArgumentOutOfRangeException ("indices.Length", indices.Length, "indices.Length != positions.Length");
			}
			ModifyVertices (indices, (i) => DoSetVertexAt (indices [i], positions [i], true));
		}
		public void SetUvAt (int index, Vector2 uv)
		{
			SetUvAt (index, -1, uv);
		}

		public void SetUvAt (int vertexIndex, int edgeIndex, Vector2 uv)
		{
			ModifyVertex (vertexIndex, () => DoSetUvAt (vertexIndex, edgeIndex, uv, false));
		}
		private void DoSetUvAt (int vertexIndex, int edgeIndex, Vector2 uv, bool checkVertexIndex)
		{
			if (checkVertexIndex && vertexIndex < 0 || vertexIndex >= vertices.Count) {
				throw new ArgumentOutOfRangeException ("vertexIndex", vertexIndex, "vertexIndex < 0 || vertexIndex >= " + vertices.Count);
			}
			Vertex v = vertices [vertexIndex];
			if (v.IsSeam && edgeIndex >= 0 && edgeIndex >= edges.Count) {
				throw new ArgumentOutOfRangeException ("edgeIndex", edgeIndex, "edgeIndex >= " + edges.Count);
			}
			v.SetUv (edgeIndex, uv);
//			if (v.seam) {
//				// Seam vertex, update per-edge uv's
//				if (edgeIndex >= 0) {
//					if (edgeIndex >= edges.Count) {
//						throw new ArgumentOutOfRangeException ("edgeIndex", edgeIndex, "edgeIndex >= " + edges.Count);
//					}
//					int uvIndex = v.connectedEdgeIndices.IndexOf (edgeIndex);
//					if (uvIndex < 0 || uvIndex >= v.seamEdgeUvs.Count) {
//						// Nonconsistent data! This is a bug!
//						throw new Exception ("Inconsistent state in DieModel.Vertex.seamEdgeIndices. This is a bug, please report!");
//					}
//					v.seamEdgeUvs [uvIndex] = uv;
//				} else {
//					// Edge index is not specified!
//					// TODO should we do something here? For example set ALL uv's of this vertex?
//					v.uv = uv;
//				}
//			} else {
//				// TODO should we still validate edgeIndex here?
//				v.uv = uv;
//			}
		}

		public void ClearUvAt (int vertexIndex)
		{
			ClearUvAt (vertexIndex, -1);
		}

		public void ClearUvAt (int vertexIndex, int edgeIndex)
		{
			ModifyVertex (vertexIndex, () => {
				Vertex v = vertices [vertexIndex];
				v.ClearUv (edgeIndex);
			});
		}

//		public void SetUVsAt (int[] indices, Vector2[] uvs)
//		{
//			ModifyVertices (indices, (i) => DoSetUVAt (indices [i], uvs [i], true));
//		}


//		public void SetNormalAt (int index, Vector3 normal)
//		{
//			SetNormalAt (index, -1, normal);
//		}
//		
//		public void SetNormalAt (int vertexIndex, int edgeIndex, Vector3 normal)
//		{
//			ModifyVertex (vertexIndex, () => DoSetNormalAt (vertexIndex, edgeIndex, normal, false));
//		}
//		private void DoSetNormalAt (int vertexIndex, int edgeIndex, Vector3 normal, bool checkVertexIndex)
//		{
//			if (checkVertexIndex && vertexIndex < 0 || vertexIndex >= vertices.Count) {
//				throw new ArgumentOutOfRangeException ("vertexIndex", vertexIndex, "vertexIndex < 0 || vertexIndex >= " + vertices.Count);
//			}
//			Vertex v = vertices [vertexIndex];
//			if (v.IsSeam && edgeIndex >= 0 && edgeIndex >= edges.Count) {
//				throw new ArgumentOutOfRangeException ("edgeIndex", edgeIndex, "edgeIndex >= " + edges.Count);
//			}
//			v.SetNormal (edgeIndex, normal);
//		}


		public int AddVertex (Vector3 v)
		{
			InsertVertex (vertices.Count, v);
			return vertices.Count - 1;
		}
		public void InsertVertex (int insertAt, Vector3 v)
		{
			if (insertAt < 0 || insertAt > vertices.Count) {
				throw new ArgumentOutOfRangeException ("insertAt", insertAt, "insertAt < 0 || insertAt > " + vertices.Count);
			}
			Modify (
				DieModelChangedEventArgs.VerticesAdded (this, insertAt), 
				(e) => {
				Vertex vertex = new Vertex ();
				vertex.Position = v;
				if (insertAt == vertices.Count) {
					// Add to end
					vertices.Add (vertex);
				} else {
					// Insert in middle
					vertices.Insert (insertAt, vertex);
					// Remap edges
					foreach (Edge edge in edges) {

						int from = edge.GetFromVertexIndex ();
						if (from >= insertAt) {
							edge.SetFromVertexIndex (from + 1);
						}
						int to = edge.GetToVertexIndex ();
						if (to >= insertAt) {
							edge.SetToVertexIndex (to + 1);
						}
					}

				}

				return insertAt;
			});
		}
		
		public void RemoveVertexAt (int index)
		{
			Modify (
				DieModelChangedEventArgs.VerticesRemoved (this, index), 
			    () => DoRemoveVertexAt (index));
		}
		private void DoRemoveVertexAt (int index)
		{
			if (index < 0 || index >= vertices.Count) {
				throw new ArgumentOutOfRangeException ("index", index, "index < 0 || index >= " + vertices.Count);
			}
			
			List<int> edgesToDelete = FindConnectedEdgeIndices (index, FindEdgesFlags.None);
			
			// Reverse sort by edge index for faster delete operation:
			edgesToDelete.Sort ((x, y) => y - x);
			
			// Delete connected edges:
			foreach (int ei in edgesToDelete) {
				RemoveEdgeAt (ei, false);
			}
			// Now adjust remaining edges: decrease all vertex indices
			// greater than the index:
			foreach (Edge e in edges) {
				int from = e.GetFromVertexIndex ();
				int to = e.GetToVertexIndex ();
				if (from >= index) {
					e.SetFromVertexIndex (from - 1);
				}
				if (to >= index) {
					e.SetToVertexIndex (to - 1);
				}
			}
			
			vertices.RemoveAt (index);

		}
		public void RemoveVerticesAt (params int[] indices)
		{
			RemoveVerticesAt ((IEnumerable<int>)indices);
		}
		
		public void RemoveVerticesAt (IEnumerable<int> indices)
		{
			// Create a list and sort it descending by index:
			List<int> toRemoveIndices = new List<int> (indices);
			toRemoveIndices.Sort ((a, b) => b - a);

			Modify (DieModelChangedEventArgs.VerticesRemoved (this, toRemoveIndices.ToArray ()),
			        (e) => {
				List<int> removedVertices = new List<int> ();
				foreach (int index in toRemoveIndices) {
					if (index < 0 || index >= vertices.Count) {
						// Just ignore this
						continue;
						//throw new ArgumentOutOfRangeException ("index", index, "index < 0 || index >= " + vertices.Count);
					}
					DoRemoveVertexAt (index);
					removedVertices.Add (index);
				}
				e.Indices = removedVertices.ToArray ();
			});
		}

		public bool IsSeamAt (int vertexIndex)
		{
			if (vertexIndex < 0 || vertexIndex >= vertices.Count) {
				return false;
			} else {
				return vertices [vertexIndex].IsSeam;
			}
		}

		public void SetSeamAt (int vertexIndex, bool value)
		{
			if (vertexIndex < 0 || vertexIndex >= vertices.Count) {
				throw new ArgumentOutOfRangeException ("vertexIndex", vertexIndex, "vertexIndex < 0 || vertexIndex >= " + vertices.Count);
			}
			Vertex v = vertices [vertexIndex];

			if (value != v.IsSeam) {
				ModifyVertex (vertexIndex, () => {
					v.IsSeam = value;
				});
			}
		}
		public bool IsSharpVertexAt (int vertexIndex)
		{
			if (vertexIndex < 0 || vertexIndex >= vertices.Count) {
				return false;
			} else {
				return vertices [vertexIndex].IsSharpEdge;
			}
		}


		public void SetSharpVertexAt (int vertexIndex, bool value)
		{
			if (vertexIndex < 0 || vertexIndex >= vertices.Count) {
				throw new ArgumentOutOfRangeException ("vertexIndex", vertexIndex, "vertexIndex < 0 || vertexIndex >= " + vertices.Count);
			}
			Vertex v = vertices [vertexIndex];

			if (value != v.IsSharpEdge) {
				ModifyVertex (vertexIndex, () => {
					v.IsSharpEdge = value;

				});
			}
		}
		
	
		public int GetEdgeCount ()
		{
			return (null != edges) ? edges.Count : 0;
		}
		public Edge GetEdgeAt (int index)
		{
			if (index < 0 || index >= edges.Count) {
				throw new ArgumentOutOfRangeException ("index", index, "index < 0 || index >= " + edges.Count);
			}
			Edge e = edges [index];
			e.UpdateMetrics (this);
			return new Edge (e);
		}
//		public int[] GetEdgeVertexIndices(int edgeIndex) {
//			Edge e = GetEdgeAt (edgeIndex);
//			return new int[] {e.GetFromVertexIndex (), e.GetToVertexIndex ()};
//		}
		public Vector3[] GetEdgeVertices (int edgeIndex)
		{
			Edge e = GetEdgeAt (edgeIndex);
			return new Vector3[] {GetVertexAt (e.GetFromVertexIndex ()), GetVertexAt (e.GetToVertexIndex ())};
		}
		public int[] GetConnectedEdgeIndices (int vertexIndex)
		{
			if (vertexIndex < 0 || vertexIndex >= vertices.Count) {
				throw new IndexOutOfRangeException ("vertexIndex < 0 || vertexIndex >= " + vertices.Count + ": " + vertexIndex);
			}

			int[] indices = vertices [vertexIndex].GetConnectedEdgeIndices ();
			int[] cp = new int[indices.Length];
			Array.Copy (indices, cp, indices.Length);
			return cp;
		}

		/// <summary>
		/// Modify the Edge at specified index. If there's already an edge with specified From and To vertex,
		/// this function modifies the existing Edge instance - and returns its index.
		/// </summary>
		/// <param name="index">Index.</param>
		/// <param name="e">The new Edge configuration</param>
		public int SetEdgeAt (int index, Edge e)
		{
			return ModifyEdge (index, () => {
				// NOTE: index is already validated by ModifyEdge function!

				// Modify the existing edge index
				int newFromIndex = e.GetFromVertexIndex ();
				int newToIndex = e.GetToVertexIndex ();
				int existingIndex = FindEdgeIndex (newFromIndex, newToIndex);
				if (existingIndex >= 0) {
					// No changes in connections
					index = existingIndex;
				} else {
					// Connections changed
					Edge currentEdge = edges [index];
					int oldFromIndex = currentEdge.GetFromVertexIndex ();
					int oldToIndex = currentEdge.GetToVertexIndex ();

					if (oldFromIndex == newToIndex && oldToIndex == newFromIndex) {
						// Just switch of drirection
						vertices [oldFromIndex].InvalidateNormals ();
						vertices [oldToIndex].InvalidateNormals ();
					} else {
						if (oldFromIndex != newFromIndex) {
							Vertex oldFromVertex = vertices [oldFromIndex];
							oldFromVertex.EdgeDisconnected (index);

							Vertex newFromVertex = vertices [newFromIndex];
							newFromVertex.EdgeConnected (index);
						}

						if (oldToIndex != newToIndex) {
							Vertex oldToVertex = vertices [oldToIndex];
							oldToVertex.EdgeDisconnected (index);

							Vertex newToVertex = vertices [newToIndex];
							newToVertex.EdgeConnected (index);
						}
					}
				}

				edges [index] = new Edge (e);
				edges [index].ResetMetrics ();
				return index;
			});

		}
		public int FindEdgeIndex (int fromVertex, int toVertex)
		{
			for (int i = 0; i < edges.Count; i++) {
				Edge e = edges [i];
				if (e.GetFromVertexIndex () == fromVertex && e.GetToVertexIndex () == toVertex) {
					return i;
				}
			}
			return -1;
		}
		public int AddEdge (int fromVertexIndex, int toVertexIndex)
		{
			return InsertEdge (edges.Count, fromVertexIndex, toVertexIndex);
		}

		public int InsertEdge (int insertAt, int fromVertexIndex, int toVertexIndex)
		{
			if (insertAt < 0 || insertAt > edges.Count) {
				throw new ArgumentOutOfRangeException ("insertAt", insertAt, "insertAt < 0 || insertAt >" + edges.Count);
			} else if (fromVertexIndex < 0 || fromVertexIndex >= vertices.Count) {
				throw new ArgumentOutOfRangeException ("fromVertexIndex", fromVertexIndex, "fromVertexIndex != (0.." + vertices.Count + ")");
			} else if (toVertexIndex < 0 || toVertexIndex >= vertices.Count) {
				throw new ArgumentOutOfRangeException ("toVertexIndex", toVertexIndex, "toVertexIndex != (0.." + vertices.Count + ")");
			} else if (toVertexIndex == fromVertexIndex) {
				throw new ArgumentOutOfRangeException ("toVertexIndex", toVertexIndex, "toVertexIndex == fromVertexIndex");
			}
			// Don't allow duplicates
			int edgeIndex = FindEdgeIndex (fromVertexIndex, toVertexIndex);
			if (edgeIndex < 0) {
				// No existing edge (fromVertexIndex -> toVertexIndex) found; insert new:
				edgeIndex = Modify (
					DieModelChangedEventArgs.EdgesAdded (this, insertAt), (ev) => {
					
					Edge e = new Edge ();
					e.SetFromVertexIndex (fromVertexIndex);
					e.SetToVertexIndex (toVertexIndex);
					bool addToEnd = (insertAt == edges.Count);

					edges.Insert (insertAt, e);

					if (!addToEnd) {
						// Remap links to edges after the inserted index
						foreach (Vertex v in vertices) {
							v.EdgeAdded (insertAt);
						}
					}

					vertices [fromVertexIndex].EdgeConnected (insertAt);
					vertices [toVertexIndex].EdgeConnected (insertAt);

					// Update the event with the actual edge index:
					ev.Indices [0] = insertAt;
					return insertAt;
				});
			}
			return edgeIndex;
		}


		public void RemoveEdgeAt (int edgeIndex, bool deleteOrphanVertices)
		{
			Modify (DieModelChangedEventArgs.EdgesRemoved (this, edgeIndex), (ev) => {
				DoRemoveEdgeAt (edgeIndex, deleteOrphanVertices);
			});

		}
		private void DoRemoveEdgeAt (int edgeIndex, bool deleteOrphanVertices)
		{
			Edge e = GetEdgeAt (edgeIndex);
			edges.RemoveAt (edgeIndex);

			// Remap remaining edge indices
			foreach (Vertex v in vertices) {
				v.EdgeRemoved (edgeIndex);
			}

			//vertices [edgeFrom].EdgeDisconnected (edgeIndex);
			//vertices [edgeTo].EdgeDisconnected (edgeIndex);

			if (deleteOrphanVertices) {
				int edgeFrom = e.GetFromVertexIndex ();
				int edgeTo = e.GetToVertexIndex ();
				List<int> connectedEdges = FindConnectedEdgeIndices (edgeFrom, FindEdgesFlags.None);
				if (connectedEdges.Count == 0) {
					RemoveVertexAt (edgeFrom);
				}
				connectedEdges = FindConnectedEdgeIndices (edgeTo, FindEdgesFlags.None);
				if (connectedEdges.Count == 0) {
					RemoveVertexAt (edgeTo);
				}
			}

		}
		public void RemoveEdgesAt (bool deleteOrphanVertices, params int[] indices)
		{
			RemoveEdgesAt (deleteOrphanVertices, (IEnumerable<int>)indices);
		}
		public void RemoveEdgesAt (bool deleteOrphanVertices, IEnumerable<int> indices)
		{
			// Create a list and sort it descending by index:
			List<int> toRemoveIndices = new List<int> (indices);
			toRemoveIndices.Sort ((a, b) => b - a);

			Modify (DieModelChangedEventArgs.EdgesRemoved (this, toRemoveIndices.ToArray ()), (ev) => {
				List<int> removedIndices = new List<int> ();
				foreach (int index in toRemoveIndices) {
					if (index < 0 || index >= edges.Count) {
						// Just ignore this
						continue;
						//throw new ArgumentOutOfRangeException ("index", index, "index < 0 || index >= " + edges.Count);
					}
					DoRemoveEdgeAt (index, deleteOrphanVertices);
					removedIndices.Add (index);
				}
				// Update the event with actually removed indices:
				ev.Indices = removedIndices.ToArray ();
			});
		}



		private static bool HasFindEdgesFlag (FindEdgesFlags flags, FindEdgesFlags flag)
		{
			return (flags & flag) == flag;
		}



		/// <summary>
		/// Finds indices of edges connected to a vertex at <c>vertexIndex</c>.
		/// 
		/// </summary>
		/// <returns>List connected edge indices in no particular order</returns>
		/// <param name="vertexIndex">Vertex index.</param>
		public List<int> FindConnectedEdgeIndices (int vertexIndex, FindEdgesFlags flags)
		{
			List<int> connectedEdges;
//			bool includeDistantEdges, bool stopOnSplitVertices;
			bool walkBackwards = !HasFindEdgesFlag (flags, FindEdgesFlags.DontWalkBackwards);
			bool walkForwards = !HasFindEdgesFlag (flags, FindEdgesFlags.DontWalkForwards);
			bool includeDistantEdges = HasFindEdgesFlag (flags, FindEdgesFlags.IncludeDistantEdges);

			if (walkForwards == false && walkBackwards == false) {
				connectedEdges = new List<int> ();
			} else if (includeDistantEdges) {
				connectedEdges = new List<int> ();
				HashSet<int> visitedEdges = new HashSet<int> ();
				DoFindConnectedEdgeIndices (vertexIndex, visitedEdges, connectedEdges, flags);
			} else {
				// Direct only

				if (vertexIndex < 0 || vertexIndex >= vertices.Count) {
					throw new IndexOutOfRangeException ("vertexIndex < 0 || vertexIndex >= " + vertices.Count + ": " + vertexIndex);
				}

				connectedEdges = new List<int> ();

//				bool stopOnBranches = HasFindEdgesFlag (flags, FindEdgesFlags.StopOnBranches);

				Vertex v = vertices [vertexIndex];
				int[] connectedEdgeIndices = v.GetConnectedEdgeIndices ();
				// if stopOnBranches == true, include no more than two edges!
//				int maxEdgeCount = stopOnBranches ? Mathf.Min (2, connectedEdgeIndices.Length) : connectedEdgeIndices.Length;


				if (walkForwards && walkBackwards) {
					// Both directions, but if stopOnBranches == true, include no more than two edges!
//					if (maxEdgeCount < connectedEdgeIndices.Length) {
//						for (int i = 0; i < maxEdgeCount; i++) {
//							connectedEdges.Add (connectedEdgeIndices [i]);
//						}
//					} else {
					connectedEdges.AddRange (connectedEdgeIndices);
//					}
				} else if (walkForwards) {
//					int c = 0;
					foreach (int ei in connectedEdgeIndices) {
						if (edges [ei].GetFromVertexIndex () == vertexIndex) {
							connectedEdges.Add (ei);
//							if (++c >= maxEdgeCount) {
//								break;
//							}
						}
					}
				} else if (walkBackwards) {
//					int c = 0;
					foreach (int ei in connectedEdgeIndices) {
						if (edges [ei].GetToVertexIndex () == vertexIndex) {
							connectedEdges.Add (ei);
//							if (++c >= maxEdgeCount) {
//								break;
//							}
						}
					}
				}
//				for (int ei = 0; ei < edges.Count; ei++) {
//					Edge e = edges [ei];
//					int efrom = e.GetFromVertexIndex ();
//					int eto = e.GetToVertexIndex ();
//					if ((walkForwards && efrom == vertexIndex) || (walkBackwards && eto == vertexIndex)) {
//						connectedEdges.Add (ei);
//					}
//				}
			}
			return connectedEdges;
		}

		// TODO does this work? 
		private void DoFindConnectedEdgeIndices (int vertexIndex, HashSet<int> visitedEdges, List<int> results, FindEdgesFlags flags)
		{
			bool walkBackwards = !HasFindEdgesFlag (flags, FindEdgesFlags.DontWalkBackwards);
			bool walkForwards = !HasFindEdgesFlag (flags, FindEdgesFlags.DontWalkForwards);
			if (walkBackwards == false && walkForwards == false) {
				return;
			}
			bool stopOnSplitVertices = HasFindEdgesFlag (flags, FindEdgesFlags.StopOnSplitVertices);
			bool stopOnBranches = HasFindEdgesFlag (flags, FindEdgesFlags.StopOnBranches);
			
			// First find edges directly connected to the vertex
			bool stopped = false;
			List<int> connectedEdges = FindConnectedEdgeIndices (vertexIndex, flags & ~FindEdgesFlags.IncludeDistantEdges);
			for (int i = 0; !stopped && i < connectedEdges.Count; i++) {
				int ei = connectedEdges [i];
				Edge ce = edges [ei];
				if (visitedEdges.Contains (ei)) {
					continue;
				}
				results.Add (ei);
				visitedEdges.Add (ei);

				int ceFromIndex = ce.GetFromVertexIndex (); 
				int ceToIndex = ce.GetToVertexIndex ();

				if (walkForwards && ceFromIndex == vertexIndex) {
					if ((stopOnSplitVertices && vertices [ceToIndex].IsSplitVertex) ||
						(stopOnBranches && vertices [ceToIndex].GetConnectedEdgeCount () > 2)) {
						// This is a seam and stopOnSeams == true
						// or this is a branch and stopOnBranches == true
						stopped = true;
					} else {
						// The found edge starts from our vertex; continue looking forward from its 'to' index...
						DoFindConnectedEdgeIndices (ceToIndex, visitedEdges, results, flags);
					}
				} else if (walkBackwards) {
					// The found edge ends in our vertex; continue looking backward from its 'from' vertex...
					if ((stopOnSplitVertices && vertices [ceFromIndex].IsSplitVertex) ||
						(stopOnBranches && vertices [ceFromIndex].GetConnectedEdgeCount () > 2)) {
						// This is a seam and stopOnSeams == true
						// or this is a branch and stopOnBranches == true
						stopped = true;
					} else {
						// ...but only if we didn't hit a seam with stopOnSeams = true
						// ... and only if we din't hit a branch vertex with stopOnBranches = true
						DoFindConnectedEdgeIndices (ceFromIndex, visitedEdges, results, flags);
					}
				}
				if (!stopped && ((stopOnSplitVertices && vertices [vertexIndex].IsSplitVertex) ||
					(stopOnBranches && vertices [vertexIndex].GetConnectedEdgeCount () > 2))) {
					// The vertex itself is a seam and stopOnSeams == true
					// or it's is a branch and stopOnBranches == true
					stopped = true;
				}
			}
		}

		public List<List<int>> FindConnectedEdgeGraphs (bool stopOnSplitVertices, bool stopOnBranches)
		{
			FindEdgesFlags findFlags = FindEdgesFlags.IncludeDistantEdges;
			if (stopOnSplitVertices) {
				findFlags |= FindEdgesFlags.StopOnSplitVertices;
			}
			if (stopOnBranches) {
				findFlags |= FindEdgesFlags.StopOnBranches;
			}
			int edgeCount = edges.Count;
			HashSet<int> visitedEdges = new HashSet<int> ();
			List<List<int>> results = new List<List<int>> ();
			for (int i = 0; i < edgeCount; i++) {
				if (!visitedEdges.Contains (i)) {
					Edge e = edges [i];
					//int eFrom = e.GetFromVertexIndex ();

					int eTo = e.GetToVertexIndex ();
					List<int> connectedEdges = new List<int> ();
					DoFindConnectedEdgeIndices (eTo, visitedEdges, connectedEdges, findFlags);
					results.Add (connectedEdges);
				}				
			}
			return results;
		}

	}

}
