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
	public delegate void DieModelChangeHandler (DieModelChangedEventArgs e);


	[Flags]
	public enum FindEdgesFlags : int
	{
		None = 0x00,
		DontWalkBackwards = 0x01,
		DontWalkForwards = 0x02,
		IncludeDistantEdges = 0x04,
		StopOnSplitVertices = 0x08,
		StopOnBranches = 0x10,
	}

	public interface IDieModel
	{
		int GetVertexCount ();
		Vector3 GetVertexAt (int index);
		Vector3[] GetVertices ();

		bool SupportsSplitVertices ();
		bool IsSeamAt (int vertexIndex);
		bool IsSharpVertexAt (int vertexIndex);

		bool SupportsUvs ();
		bool IsUvSetAt (int index);
		bool IsUvSetAt (int index, int edgeIndex);
		Vector2 GetUvAt (int vertexIndex);
		Vector2 GetUvAt (int vertexIndex, int edgeIndex);
		Vector2[] GetUvs ();

		bool SupportsVertexNormals ();
		Vector3 GetNormalAt (int vertexIndex);
		Vector3 GetNormalAt (int vertexIndex, int edgeIndex);
		Vector3[] GetNormals ();

		int GetEdgeCount ();
		Edge GetEdgeAt (int index);
//		int[] GetEdgeVertexIndices(int edgeIndex);
		Vector3[] GetEdgeVertices (int edgeIndex);
		int[] GetConnectedEdgeIndices (int vertexIndex);

	}

	public interface IDieModelGraphSupport
	{

		int FindEdgeIndex (int fromVertex, int toVertex);
		List<int> FindConnectedEdgeIndices (int vertexIndex, FindEdgesFlags flags);
		List<List<int>> FindConnectedEdgeGraphs (bool stopOnSplitVertices, bool stopOnBranches);
	}




	public interface IDieModelEditorSupport
	{
		void FireRefreshEvent ();
		IDieModelSelectionSupport GetDieModelSelectionSupport ();
	}
	public sealed class NoDieModelEditorSupport : IDieModelEditorSupport
	{
		private static readonly NoDieModelEditorSupport _instance = new NoDieModelEditorSupport ();
		public static IDieModelEditorSupport Instance {
			get {
				return _instance;
			}
		}

		public void FireRefreshEvent ()
		{

		}
		public IDieModelSelectionSupport GetDieModelSelectionSupport ()
		{
			return NoDieModelSelectionSupport.Instance;
		}


	}

	[Flags]
	public enum SupportedModelOps
	{
		None			= 0x000,
		SetVertex		= 0x001,
		AddVertex		= 0x002,
		RemoveVertex	= 0x004,
		SetEdge			= 0x008,
		AddEdge			= 0x010,
		RemoveEdge		= 0x020,
		All				= 0x03f,
	}


	public interface IMutableDieModel : IDieModel
	{
		void AddDieModelChangeHandler (DieModelChangeHandler h);
		void RemoveDieModelChangeHandler (DieModelChangeHandler h);

		SupportedModelOps GetSupportedModelOps ();

		void SetVertexAt (int index, Vector3 v);
		void SetVerticesAt (int[] indices, Vector3[] positions);
		int AddVertex (Vector3 v);
		void InsertVertex (int insertAt, Vector3 v);
		void RemoveVertexAt (int index);
		void RemoveVerticesAt (params int[] indices);
		void RemoveVerticesAt (IEnumerable<int> indices);
		
		void SetSeamAt (int vertexIndex, bool value);
		void SetSharpVertexAt (int vertexIndex, bool value);

		void SetUvAt (int index, Vector2 uv);
		void SetUvAt (int vertexIndex, int edgeIndex, Vector2 uv);
		void ClearUvAt (int index);
		void ClearUvAt (int index, int edgeIndex);

		
		//void SetNormalAt (int index, Vector2 normal);
		//void SetNormalAt (int vertexIndex, int edgeIndex, Vector2 normal);

		/// <summary>
		/// Modify the Edge at specified index. If there's already an edge with specified From and To vertex,
		/// this function modifies the existing Edge instance - and returns its index.
		/// </summary>
		/// <param name="index">Index.</param>
		/// <param name="e">The new Edge configuration</param>
		int SetEdgeAt (int index, Edge e);
		int AddEdge (int fromVertexIndex, int toVertexIndex);
		int InsertEdge (int insertAt, int fromVertexIndex, int toVertexIndex);
		void RemoveEdgeAt (int edgeIndex, bool deleteOrphanVertices);
		void RemoveEdgesAt (bool deleteOrphanVertices, params int[] indices);
		void RemoveEdgesAt (bool deleteOrphanVertices, IEnumerable<int> indices);


		void BatchOperation (string name, Action<DieModel> a);
	}

}
