// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using Util;
using Paths;
using Paths.MeshGenerator;

namespace Paths.MeshGenerator.SliceStrip
{
	// Rename to AbstractSliceStripGenerator
	public abstract class AbstractSliceStripGenerator : AbstractMeshGenerator
	{

		//
		private MeshFaceDir facesDir = MeshFaceDir.Up;
		//private float sliceRotation = 45.0f;
		private bool perSideSubmeshes = true;
		private bool perSideVertices = true;

		// TODO is this used?
		private bool createTangents = false;

		protected AbstractSliceStripGenerator () : base()
		{
		}


		
//		public float SliceRotation {
//			get {
//				return this.sliceRotation;
//			}
//			set {
//				sliceRotation = value;
//			}
//		}
//		
		public MeshFaceDir FacesDir {
			get {
				return this.facesDir;
			}
			set {
				facesDir = value;
			}
		}
		
		public bool PerSideSubmeshes {
			get {
				return this.perSideSubmeshes;
			}
			set {
				perSideSubmeshes = value;
			}
		}
		
		public bool PerSideVertices {
			get {
				return this.perSideVertices;
			}
			set {
				perSideVertices = value;
			}
		}
		
		public bool CreateTangents {
			get {
				return this.createTangents;
			}
			set {
				createTangents = value;
			}
		}

		public override void OnLoadParameters (ParameterStore store)
		{
//			sliceRotation = store.GetFloat ("sliceRotation", sliceRotation);
			facesDir = store.GetEnum ("facesDir", facesDir);
			perSideSubmeshes = store.GetBool ("perSideSubmeshes", perSideSubmeshes);
			perSideVertices = store.GetBool ("perSideVertices", perSideVertices);
		}
		
		public override void OnSaveParameters (ParameterStore store)
		{
			// TODO what't "name" in here? Why? Is it used? Should it be removed?
			store.SetString ("name", Name);
//			store.SetFloat ("sliceRotation", sliceRotation);
			store.SetEnum ("facesDir", facesDir);
			store.SetBool ("perSideSubmeshes", perSideSubmeshes);
			store.SetBool ("perSideVertices", perSideVertices);
		}

		TransformedSlice[] CreateSlices (PathDataSource dataSource)
		{
			return CreateSlices (dataSource, false);
		}

		protected abstract  int GetSliceEdgeCount ();
//		protected abstract  bool IsSliceClosedShape ();

		/// <summary>
		/// Called to create a SliceStripSlice for the given PathPoints. Slice points should
		/// not be transformed to the point position or rotation.
		/// </summary>
		/// <returns>The slice.</returns>
		/// <param name="pp">Pp.</param>
		protected abstract SliceStripSlice CreateSlice (PathPoint pp);
		
		TransformedSlice[] CreateSlices (PathDataSource dataSource, bool repeatFirstInLoop)
		{
			TransformedSlice[] slices;
			long startTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
			
			// Create slices
			//int slicesPerSgement = path.PointsPerSegment;
			
			if (null == dataSource) {
				Debug.LogError ("No Data Source configured; not creating any slices");
				slices = new TransformedSlice[0];
			} else {
				slices = DoCreateSlices (dataSource, repeatFirstInLoop);
			}
			
			long endTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
			
			long deltaTime = endTime - startTime;
			Debug.Log ("Creating " + slices.Length + " slices took " + deltaTime + " ms");
			
			return slices;
		}
		private TransformedSlice[] DoCreateSlices (PathDataSource dataSource, bool repeatFirstInLoop)
		{
			int ppFlags;
			PathPoint[] points = dataSource.GetProcessedPoints (out ppFlags);
			if (null == points) {
				throw new Exception ("No data available in data source: " + dataSource.PathSelector);
			}
			
			//Debug.Log ("dirs: " + directions.Length + "; pts: " + points.Length);
			int sliceCount = points.Length;
			bool isLoop = false; // TODO IMPLEMENT THIS FOR REAL?
			if (isLoop && !repeatFirstInLoop) {
				sliceCount -= 1;
			}
			
			TransformedSlice[] slices = new TransformedSlice[sliceCount];
			
			// TODO split long segments to shorter
			bool usingGeneratedDirections = false;
			//int lastIndex = points.Length - 1;
			for (int i = 0; i < slices.Length; i++) {
				Vector3 pt0 = points [i].Position;
				Vector3 dir;
				if (PathPoint.IsDirection (ppFlags) && points [i].HasDirection) {
					dir = points [i].Direction;
				} else {
					usingGeneratedDirections = true;
					if (i < slices.Length - 1) {
						// Calculate direction from this to next
						dir = (points [i + 1].Position - pt0).normalized;
					} else if (i > 0) {
						// Last point; calculate direction from previous point to this
						dir = (pt0 - points [i - 1].Position).normalized;
					} else {
						// Unknown direction, set to "forward"
						dir = Vector3.forward;
					}
				}
				
				Vector3 up;
				if (PathPoint.IsUp (ppFlags) && points [i].HasUp) {
					up = points [i].Up;
				} else {
					up = Vector3.up;
				}
				
				//Quaternion sliceRot = Quaternion.LookRotation(dir);
				
				Quaternion sliceRot = Quaternion.LookRotation (dir, up);
				//Quaternion sliceRot = Quaternion.FromToRotation(Vector3.forward, dir);

				SliceStripSlice slice = CreateSlice (points [i]);
				TransformedSlice transformedSlice = new TransformedSlice (slice, pt0, sliceRot);
				slices [i] = transformedSlice;
			}
			if (usingGeneratedDirections) {
				Debug.LogWarning ("Using calculated directions while generating Track; expect some inaccurancy!");
			}
			if (isLoop && repeatFirstInLoop) {
				slices [sliceCount - 1] = slices [0];
			}
			
			return slices;
		}
		public override Mesh CreateMesh (PathDataSource dataSource, Mesh mesh)
		{
			
			//      return DoCreateMesh(path, mesh, sliceEdges, true, facesOutwards, facesInwards);
			
//			if (facesDir == MeshFaceDir.Both && perSideSubmeshes) {
//				// Inwards mesh:
//				//          DoCreateMesh(path, mesh, faceDir, 0, sliceEdges, true, false);
//				//
//				//          // Outwards mesh:
//				//          DoCreateMesh(path, mesh, faceDir, 0, sliceEdges, true, false);
//				
//			} else {
//				
//			}
			DoCreateMesh (dataSource, mesh);
			return mesh;
		}

		protected void DoCreateMesh (PathDataSource dataSource, Mesh mesh)
		{
			
			mesh.Clear (false);
			mesh.tangents = null;
			
			TransformedSlice[] slices = CreateSlices (dataSource, true);
			if (slices.Length == 0) {
				return;
			}
			int sliceEdges = GetSliceEdgeCount ();
			int sliceCount = slices.Length;

			// Parameters:
			int verticesPerSlice = sliceEdges + 1; // The first point needs to be doubled (first == last)
			int verticesPerSliceSide = verticesPerSlice;
			
			int faceSides = (facesDir == MeshFaceDir.Both) ? 2 : 1;

			if (perSideVertices) {
				verticesPerSlice *= faceSides;
			}
			
			int verticeCount = sliceCount * verticesPerSlice;

			// Assign mesh vertices and calculate normals:
			Vector3[] vertices = new Vector3[verticeCount];
			Vector3[] normals = new Vector3[vertices.Length];
			Vector2[] uv = new Vector2[vertices.Length];

			// Tangents / experimental
			Vector4[] tangents = createTangents ? new Vector4[verticeCount] : null;
			
			//  create triangle stripe
			// vertices, normals, uv
			//
			float v = 0.0f; // for uv mapping
			for (int i = 0; i < sliceCount; i++) { 
				
				TransformedSlice slice = slices [i];

				// Circumference of the slice: use this to calculate multiplier
				// for UV mapping
				float sliceCircum = slice.Circumference;
				bool closedShape = slice.ClosedShape;

				Vector3 sliceCenter = slice.Center;
				if (i > 0) {
					// add distance between slices to "u"
					float dist = (sliceCenter - slices [i - 1].Center).magnitude;
					// TODO: precalculate the u/v factor below:
					v += dist * (1.0f / sliceCircum * 4.0f); // 4.0f here is texture.width / texture.height !
					//v += dist * (1.0f / 2.80f);
					//u -= 0.25f;
				}
				
				// Assign slice vectors
				
				// Slice vertices:
				//
				// y  0   1
				// ^  +---+
				// |  |   |
				// |  +---+
				// |  3   2
				// +--------> x
				
				
				//float u = 0.0f;
				// slice doesn't have the last vertice (it's connected to the first one)
				int lastSliceVerticeIndex = verticesPerSliceSide - 2;
				
				// voffs = vertice array offset
				int voffs = i * verticesPerSlice;
				
				for (int j = 0; j < verticesPerSliceSide; j++) {
					int vi = voffs + j;

					int slicePtIndex;
					if (closedShape) {
						slicePtIndex = (j <= lastSliceVerticeIndex) ? j : 0;
					} else {
						slicePtIndex = j;
					}
					vertices [vi] = slice.Points [slicePtIndex];
					if (facesDir == MeshFaceDir.Down) {
						// Faces point "downwards"; invert slice normals
						normals [vi] = -slice.Normals [slicePtIndex];
					} else {
						// Faces point "upwards"; use slice normals as they are
						normals [vi] = slice.Normals [slicePtIndex];
					}
					// uv mapping 
					/*if (j > 0) {
                    //v += (pt - slice.points[j - 1]).magnitude;

                }*/
					
					float u = (float)j / (float)(verticesPerSliceSide - 1);
					
					uv [vi] = new Vector2 (u, v);
					//Debug.Log ("uv: " + uv[vi] + "; j=" + j);
					//u += 0.25f;
					
					// Tangents / experimental 
					// TODO this is not really working
					if (createTangents) {
						throw new NotImplementedException ("Creating of tangents is not implemented");
						//tangents [vi] = new Vector4 (slice.Direction.x, slice.Direction.y, slice.Direction.z, -1f);
					}
				}
				if (perSideVertices && faceSides > 1) {
					// Other side ("Down side")
					// Clone first side vertices and invert normals
					for (int j = 0; j < verticesPerSliceSide; j++) {
						int vi0 = voffs + j;
						int vi = vi0 + verticesPerSliceSide;
						
						vertices [vi] = vertices [vi0];
						normals [vi] = -normals [vi0];
						if (createTangents) {
							tangents [vi] = tangents [vi0];
						}
						uv [vi] = uv [vi0];
					}
				}
			}
			
			
			mesh.vertices = vertices;
			mesh.normals = normals;
			mesh.uv = uv;
			mesh.tangents = tangents;
			
			int triangleCount = CalculateTriangleCount (sliceCount, sliceEdges, facesDir == MeshFaceDir.Both);
			int facesPerSegment = verticesPerSliceSide - 1;
			
			
			if (facesDir == MeshFaceDir.Both) {
				
				if (triangleCount % 2 > 0) {
					throw new System.Exception ("Double-sided faces requested but triangleCount is uneven!");
				}
				
				
				
				int trianglesOffset2;
				int[] triangles;
				if (perSideSubmeshes) {
					mesh.subMeshCount = 2;
					triangles = new int[triangleCount * 3 / 2];
					trianglesOffset2 = 0;
				} else {
					mesh.subMeshCount = 1;
					triangles = new int[triangleCount * 3];
					trianglesOffset2 = triangles.Length / 2;
				}
				int verticesOffset = perSideVertices ? verticesPerSliceSide : 0;
				
				// First  side ("up")
				DoCreateTriangles (triangles, 0, MeshFaceDir.Up, facesPerSegment, verticesPerSlice, 0, sliceCount);
				if (perSideSubmeshes) {
					mesh.SetTriangles (triangles, 0);
				}
				
				// Second side ("down")
				// we can recycle the same "triangles" array
				DoCreateTriangles (triangles, trianglesOffset2, MeshFaceDir.Down, facesPerSegment, verticesPerSlice, verticesOffset, sliceCount);
				
				mesh.SetTriangles (triangles, perSideSubmeshes ? 1 : 0);
				
			} else {
				mesh.subMeshCount = 1;
				int[] triangles = new int[triangleCount * 3];
				DoCreateTriangles (triangles, 0, facesDir, facesPerSegment, verticesPerSlice, 0, sliceCount);
				mesh.SetTriangles (triangles, 0);
			}
			
			mesh.MarkDynamic ();
			
			Debug.Log ("Created a Mesh with " + vertices.Length + " vertices and " + triangleCount + " triangles in " + mesh.subMeshCount + " submeshes.");
			
		}
		
		private int CalculateTriangleCount (int sliceCount, int sliceEdges, bool doubleSided)
		{
			//			const int verticesPerTriangle = 3;
			
			int trianglesPerFace = doubleSided ? 4 : 2;
			
			// Parameters:
			int verticesPerSlice = sliceEdges + 1; // The first point needs to be doubled (first == last)
			int facesPerSegment = verticesPerSlice - 1; 
			int trianglesPerSegment = facesPerSegment * trianglesPerFace;
			
			//const int verticesPerSegment = facesPerSegment * verticesPerFace;
			int segmentCount = sliceCount - 1;
			int triangleCount = trianglesPerSegment * segmentCount;
			
			return triangleCount;
		}
		
		private static void DoCreateTriangles (int[] triangles, int offset, MeshFaceDir facesDir, int facesPerSegment, int verticesPerSlice, int verticesOffset, int sliceCount)
		{
			
			if (facesDir == MeshFaceDir.Both) {
				throw new System.ArgumentException ("Can't create triangles with FaceDir.Both - use separate calls for both directions");
			}
			
			// Non-volatiles:
			//const int verticesPerFace = 4;
			
			const int verticesPerTriangle = 3;
			const int trianglesPerFace = 2;
			
			// Parameters:
			//int facesPerSegment = verticesPerSlice - 1;
			int trianglesPerSegment = facesPerSegment * trianglesPerFace;
			
			//const int verticesPerSegment = facesPerSegment * verticesPerFace;
			//      int segmentCount = sliceCount - 1;
			//      int triangleCount = trianglesPerSegment * segmentCount;
			
			// Assign mesh vertices and calculate normals:
			//      int[] triangles = new int[triangleCount * 3]; 
			
			for (int i = 1; i < sliceCount; i++) { 
				
				// voffs = vertice array offset
				int voffs = i * verticesPerSlice + verticesOffset;
				
				// Slice vertices:
				//
				// y  0   1
				// ^  +---+
				// |  |   |
				// |  +---+
				// |  3   2
				// +--------> x
				
				
				// toffs = triangle array offset
				int toffs = offset + (i - 1) * trianglesPerSegment * verticesPerTriangle;
				
				// Vertice array offset of the current slice
				int voffs1 = voffs;
				
				// Vertice array offset of the previous slice
				int voffs0 = voffs1 - verticesPerSlice;
				
				// Faces inwards:               Faces outwards:
				//
				//  
				//  s1.0 s1.1 s1.2 s1.3 s1.4    s1.0 s1.1 s1.2 s1.3 s1.4
				//   *----*----*----*---->       *----*----*----*---->
				//   |t1 /|t3 /|t5 /|t7 /|       |t1 /|t3 /|t5 /|t7 /|
				//   |  / |  / |  / |  / |       |  / |  / |  / |  / |
				//   | /  | /  | /  | /  |       | /  | /  | /  | /  |
				//   |/ t2|/ t4|/ t6|/ t8|       |/ t2|/ t4|/ t6|/ t8|
				//   *----*----*----*---->       *----*----*----*---->
				//  s0.0 s0.1 s0.2 s0.3 s0.4    s0.0 s0.1 s0.2 s0.3 s0.4
				//  
				//
				// t1: s1.0 --> s0.0 --> s1.1   t1: s1.0 --> s0.0 --> s1.1   
				// t2: s1.1 --> s0.0 --> s0.1   t2: s1.1 --> s0.0 --> s0.1
				//
				// t3: s1.1 --> s0.1 --> s0.2   (Swap second and third vector)
				// t4: s1.2 --> s0.1 --> s0.2   ( --::--)
				//
				// t5: s1.2 --> s0.2 --> s1.3   
				// t6: s1.3 --> s0.2 --> s1.3   
				//
				// t7: s1.3 --> s0.3 --> s1.4
				// t8: s1.4 --> s0.3 --> s0.4
				
				//int lastFaceIndex = facesPerSegment - 1;
				for (int j = 0; j < facesPerSegment; j++) {
					
					int ftoffs = toffs + trianglesPerFace * verticesPerTriangle * j;
					//Debug.Log ("Slice " + i + "/" + sliceCount + "; ftoffs=" + ftoffs);
					
					if (facesDir == MeshFaceDir.Down) {
						// "outside"
						triangles [ftoffs + 0] = voffs1 + j;
						triangles [ftoffs + 1] = voffs1 + j + 1;
						triangles [ftoffs + 2] = voffs0 + j;
						
						triangles [ftoffs + 3] = voffs1 + j + 1;
						triangles [ftoffs + 4] = voffs0 + j + 1;
						triangles [ftoffs + 5] = voffs0 + j;
					} else {
						// "inside"
						triangles [ftoffs + 0] = voffs1 + j;
						triangles [ftoffs + 1] = voffs0 + j;
						triangles [ftoffs + 2] = voffs1 + j + 1;
						
						triangles [ftoffs + 3] = voffs1 + j + 1;
						triangles [ftoffs + 4] = voffs0 + j;
						triangles [ftoffs + 5] = voffs0 + j + 1;
					}
				}
			}
		}

	}
}
