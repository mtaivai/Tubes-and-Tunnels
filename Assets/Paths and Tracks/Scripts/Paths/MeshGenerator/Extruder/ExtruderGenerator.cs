// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using System;
using UnityEngine;
using System.Collections.Generic;

using Util;
using Paths;
using Paths.MeshGenerator;
using Paths.MeshGenerator.Extruder.Model;

namespace Paths.MeshGenerator.Extruder
{

	public abstract class AbstractDieModelMeshGenerator : AbstractMeshGenerator
	{
		private DieModel dieModel;
		private string dieModelRefId;

		public DieModel DieModel {
			get {
				return dieModel;
			}
			set {
				this.dieModel = value;
			}
		}
		
		private void DieModelChanged (DieModelChangedEventArgs e)
		{
			if (e.IsAfterEvent) {
				if ((IDieModel)this.dieModel == e.Model) {
					Debug.Log ("DieModelChanged: " + e);
					MeshGeneratorEventArgs mge = new MeshGeneratorEventArgs (this, MeshGeneratorEventArgs.Reason.MeshChanged);
					FireMeshGeneratorEvent (mge);
				}
			}
			
		}
		
		public override void OnLoadParameters (ParameterStore store, IReferenceContainer refContainer)
		{
			if (null != dieModel) {
				dieModel.RemoveDieModelChangeHandler (DieModelChanged);
			}
			dieModelRefId = store.GetString ("dieModelRefId", "");
			if (!StringUtil.IsEmpty (dieModelRefId)) {
				this.dieModel = (DieModel)refContainer.GetReferentObject (dieModelRefId);
				// TODO what if we got null?
				this.dieModel.AddDieModelChangeHandler (DieModelChanged);
			} else {
				this.dieModel = null;
			}
		}
		
		public override void OnSaveParameters (ParameterStore store, IReferenceContainer refContainer)
		{
			if (null != dieModel) {
				if (StringUtil.IsEmpty (dieModelRefId)) {
					// Reference not yet stored
					dieModelRefId = refContainer.AddReferent (dieModel);
				} else {
					refContainer.SetReferentObject (dieModelRefId, dieModel);
				}
				
			} else {
				// DieModel is null
				if (!StringUtil.IsEmpty (dieModelRefId)) {
					// Remove stored ref
					refContainer.RemoveReferent (dieModelRefId);
					dieModelRefId = "";
				}
			}
			
			store.SetString ("dieModelRefId", dieModelRefId);
			
		}
	}

	public class ExtruderGenerator : AbstractDieModelMeshGenerator
	{

		public override int GetMaterialSlotCount ()
		{
			return null != DieModel ? 1 : 0;
		}


		void DoFooUvMapping (DieModel model, UnwrappedDieModel unwrapped)
		{
			// Map u=0..1 for each strip

//			int stripIndex = 0;
//			foreach (List<int> strip in model.FindConnectedEdgeGraphs(true)) {
//				string stripString = "";
//				strip.ForEach ((ei) => {
//					if (stripString.Length > 0)
//						stripString += ", ";
//					stripString += ei;});
//				Debug.LogFormat ("{0}: {1}", stripIndex++, stripString);
//
//				// Is branch?
////				ei.ForEach((i) => {});
//			}
//
//			Vector2[] uvs = model.GetUvs ();
//			Debug.LogFormat ("wrapped: {0}, unwrapped: {1}", model.GetVertexCount (), unwrapped.GetVertices ().Length);

			// Where to start from????

		}

		public override void CreateMeshes (PathDataSource dataSource, Mesh[] existingMeshes)
		{
			if (null == DieModel) {
				return;
			}

			UnwrappedDieModel unwrappedModel = UnwrappedDieModel.Build (DieModel);


			DoFooUvMapping (DieModel, unwrappedModel);


			// Create one submesh per surface, surface == strip of connected edges

			// Get edges
			// One quad shape per edge:
			//
			// z
			// ^ 
			// |   
			// |1.0----1----2
			// |  | a /| c /|
			// |  |  / |  / |
			// |  | /  | /  |
			// |  |/ b |/ d |
			// |0.0----1----2
			// |    E    F
			// +--------------------> x
			//
			// Slice n: Edge e (v0.0 -> v0.1), Edge b (v0.1 -> v0.2)
			// Slice n+1: Edge f (v1.0 -> v1.1), Edge b (v1.1 -> v1.2)
			//
			// Triangle a = 0.0, 1.0, 1.1
			// Triangle b = 0.0, 1.1, 0.1
			// Triangle c = 0.1, 1.1, 1.2
			// Triangle d = 0.1, 1.2, 0.2

			int edgeCount = unwrappedModel.GetEdgeCount ();
			int[][] edgeIndices = new int[edgeCount] [];
			for (int i = 0; i < edgeCount; i++) {
				Edge edge = unwrappedModel.GetEdgeAt (i);
				//edges [i] = edge;
				edgeIndices [i] = new int[2];
				edgeIndices [i] [0] = edge.GetFromVertexIndex ();
				edgeIndices [i] [1] = edge.GetToVertexIndex ();

			}

			// Create vertices and uvs array
			PathPoint[] pathPoints = dataSource.ProcessedPoints;
			//int vertexPerSliceCount = dieModel.GetVertexCount ();
			Vector3[] sliceVertices = unwrappedModel.GetVertices ();
			Vector3[] sliceNormals = unwrappedModel.GetNormals ();
			Vector2[] sliceUvs = unwrappedModel.GetUvs ();
			int vertexPerSliceCount = sliceVertices.Length;

			int sliceCount = pathPoints.Length;
			int totalVertexCount = vertexPerSliceCount * sliceCount;
			Vector3[] vertices = new Vector3[totalVertexCount];
			Vector3[] normals = new Vector3[totalVertexCount];
			Vector2[] uvs = new Vector2[totalVertexCount];

			int trianglesPerSegment = edgeCount * 2;
			int segmentCount = sliceCount - 1;
			int totalTriangleCount = trianglesPerSegment * segmentCount;
			int triangleVerticesPerSegment = trianglesPerSegment * 3;
			int totalTriangleVerticeCount = totalTriangleCount * 3;

			int[] triangleVertices = new int[totalTriangleVerticeCount];

			// Pass 1: Create vertices array
			float v = 0.0f;
			for (int i = 0; i < sliceCount; i++) {
				v = (float)i / (float)(sliceCount - 1);
				// Add vertices
				PathPoint pp = pathPoints [i];
				Vector3 pathPos = pp.Position;
				
				// Vertices array offset of this slice
				int viOffs = i * vertexPerSliceCount;

				for (int j = 0; j < vertexPerSliceCount; j++) {
					// TODO transform oh yeah
					int vi = viOffs + j;
					vertices [vi] = sliceVertices [j] + pathPos;
					normals [vi] = sliceNormals [j]; // TODO transform normal?
					Vector2 uv = sliceUvs [j];
					uvs [vi] = new Vector2 (uv.x, v + uv.y);
				}
			}

			// Pass 2: Create faces

			for (int i = 0; i < segmentCount; i++) {
				// Add vertices
//				PathPoint pp = pathPoints [i];
//				Vector3 pathPos = pp.Position;
//
				// Vertices array offset of this slice
				int viOffs0 = i * vertexPerSliceCount;

				// Vertices array offset of the next slice:
				int viOffs1 = viOffs0 + vertexPerSliceCount;


				// Create faces (each face has two triangles = six triangle vertices per face)
				int segmentTriangleVerticesOffs = (i * triangleVerticesPerSegment);
				for (int j = 0; j < edgeCount; j++) {
					int triangleVerticesOffs = segmentTriangleVerticesOffs + j * 6;

					int fromIndex = edgeIndices [j] [0];
					int toIndex = edgeIndices [j] [1];

					int fromIndex0 = viOffs0 + fromIndex;
					int toIndex0 = viOffs0 + toIndex;

					int fromIndex1 = viOffs1 + fromIndex;
					int toIndex1 = viOffs1 + toIndex;

					// First triangle:
					triangleVertices [triangleVerticesOffs + 0] = fromIndex0; 
					triangleVertices [triangleVerticesOffs + 1] = fromIndex1; 
					triangleVertices [triangleVerticesOffs + 2] = toIndex1; 

					// Second triangle:
					triangleVertices [triangleVerticesOffs + 3] = fromIndex0; 
					triangleVertices [triangleVerticesOffs + 4] = toIndex1; 
					triangleVertices [triangleVerticesOffs + 5] = toIndex0;

				}
			}

			Mesh mesh = existingMeshes [0];

			mesh.Clear ();
			mesh.MarkDynamic ();
			mesh.vertices = vertices;
			mesh.normals = normals;
			mesh.triangles = triangleVertices;
			mesh.uv = uvs;
			//mesh.RecalculateNormals ();

		}




//		protected override int GetSliceEdgeCount ()
//		{
//			if (dieModel == null) {
//				return 0;
//			} else {
//				return dieModel.GetEdgeCount();
//			}
//		}
//
//		protected override SliceStripSlice CreateSlice (PathDataSource dataSource, int pointIndex, PathPoint pp)
//		{
//			throw new NotImplementedException ();
//		}

	}

}
