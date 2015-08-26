using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

using Util;
using Paths;
using Tracks;

namespace Tracks.Tube
{
    public class TubeGenerator : TrackGenerator
    {

        // TODO move this to global scope?
        public enum FaceDir
        {
            Up, // In
            Down, // Out
            Both, // Both
        }

        private int sliceEdges = 16;
        private float sliceRotation = 45.0f;
        private Vector2 sliceSize = new Vector2(2f, 2f);
        private FaceDir facesDir = FaceDir.Up;
        private bool perSideSubmeshes = true;
        private bool perSideVertices = true;
        private bool createTangents = false;

        public override string DisplayName
        {
            get
            {
                return "Tube";
            }
        }

        public TubeGenerator() : base()
        {
        }

        public virtual int SliceEdges
        {
            get
            {
                return this.sliceEdges;
            }
            set
            {
                sliceEdges = value;
            }
        }

        public float SliceRotation
        {
            get
            {
                return this.sliceRotation;
            }
            set
            {
                sliceRotation = value;
            }
        }

        public Vector2 SliceSize
        {
            get
            {
                return this.sliceSize;
            }
            set
            {
                sliceSize = value;
            }
        }

        public FaceDir FacesDir
        {
            get
            {
                return this.facesDir;
            }
            set
            {
                facesDir = value;
            }
        }

        public bool PerSideSubmeshes
        {
            get
            {
                return this.perSideSubmeshes;
            }
            set
            {
                perSideSubmeshes = value;
            }
        }

        public bool PerSideVertices
        {
            get
            {
                return this.perSideVertices;
            }
            set
            {
                perSideVertices = value;
            }
        }

        public bool CreateTangents
        {
            get
            {
                return this.createTangents;
            }
            set
            {
                createTangents = value;
            }
        }

        public override void LoadParameters(ParameterStore store)
        {

            base.LoadParameters(store);

            // To dictionary:
            //Dictionary<string, TrackParameter> map = new Dictionary<string, TrackParameter>();
            //foreach (TrackParameter tp in parameters) {
            //  map[tp.name] = tp;
            //}



            //Debug.Log("Store: " + store);
            /*if (store["name"] != Name) {
            // Not out store
            Debug.Log ("Not our store: " + store["name"]);
            return;
        }*/

            sliceEdges = store.GetInt("sliceEdges", sliceEdges);
            sliceRotation = store.GetFloat("sliceRotation", sliceRotation);
            sliceSize = store.GetVector2("sliceSize", sliceSize);
            facesDir = store.GetEnum("facesDir", facesDir);
            perSideSubmeshes = store.GetBool("perSideSubmeshes", perSideSubmeshes);
            perSideVertices = store.GetBool("perSideVertices", perSideVertices);

        }

        public override void SaveParameters(ParameterStore store)
        {
            base.SaveParameters(store);

            store.SetString("name", Name);
            store.SetInt("sliceEdges", sliceEdges);
            store.SetFloat("sliceRotation", sliceRotation);
            store.SetVector2("sliceSize", sliceSize);
            store.SetEnum("facesDir", facesDir);
            store.SetBool("perSideSubmeshes", perSideSubmeshes);
            store.SetBool("perSideVertices", perSideVertices);

        }

        protected override TrackSlice CreateSlice(Vector3 center, Quaternion sliceRotation)
        {
            return new TubeSlice(center, sliceRotation, sliceEdges, sliceSize.x, sliceSize.y, this.sliceRotation);
        }

        public override Mesh CreateMesh(Track track, Mesh mesh)
        {
        
//      return DoCreateMesh(path, mesh, sliceEdges, true, facesOutwards, facesInwards);

            if (facesDir == FaceDir.Both && perSideSubmeshes)
            {
                // Inwards mesh:
//          DoCreateMesh(path, mesh, faceDir, 0, sliceEdges, true, false);
//
//          // Outwards mesh:
//          DoCreateMesh(path, mesh, faceDir, 0, sliceEdges, true, false);

            } else
            {

            }
            DoCreateMesh(track, mesh, true);
            return mesh;
        }

        protected void DoCreateMesh(Track track, Mesh mesh, bool closedShape)
        {

            mesh.Clear(false);
            mesh.tangents = null;

            TrackSlice[] slices = CreateSlices(track, true);
            if (slices.Length == 0)
            {
                return;
            }
            int sliceCount = slices.Length;
//      int segmentCount = sliceCount - 1;
        
            // Non-volatiles:
            //const int verticesPerFace = 4;
            //const int verticesPerTriangle = 3;

            //int trianglesPerFace = facesDir == FaceDir.Both ? 4 : 2;
        
            // Parameters:
            int verticesPerSlice = sliceEdges + 1; // The first point needs to be doubled (first == last)
//      int facesPerSegment = verticesPerSlice - 1; 
            //int trianglesPerSegment = facesPerSegment * trianglesPerFace;
        
            //const int verticesPerSegment = facesPerSegment * verticesPerFace;

            int verticesPerSliceSide = verticesPerSlice;

            int faceSides = (facesDir == FaceDir.Both) ? 2 : 1;
            //int verticeSides;
            if (perSideVertices)
            {
                //verticeSides = faceSides;
                verticesPerSlice *= faceSides;
            } else
            {
                //verticeSides = 1;
            }

            int verticeCount = sliceCount * verticesPerSlice;
            //int triangleCount = trianglesPerSegment * segmentCount;
        
            //Debug.Log ("Creating Mesh (vertices: " + verticeCount + "; triangles: " + triangleCount + ")");
        
        
            // Assign mesh vertices and calculate normals:
            Vector3[] vertices = new Vector3[verticeCount];
            Vector3[] normals = new Vector3[vertices.Length];
            Vector2[] uv = new Vector2[vertices.Length];
            //int[] triangles = new int[triangleCount * verticesPerTriangle]; 
        
            // Tangents / experimental
            Vector4[] tangents = createTangents ? new Vector4[verticeCount] : null;
        
            //  create triangle stripe
            // vertices, normals, uv
            //
            float v = 0.0f; // for uv mapping
            for (int i = 0; i < sliceCount; i++)
            { 

                TrackSlice slice = slices [i];
            
                // Circumference of the slice: use this to calculate multiplier
                // for UV mapping
                float sliceCircum = slice.Circumference;
            
            
                Vector3 sliceCenter = slice.Center;
                if (i > 0)
                {
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

                for (int j = 0; j < verticesPerSliceSide; j++)
                {
                    int vi = voffs + j;
                
                    Vector3 pt;
                    if (closedShape)
                    {
                        pt = (j <= lastSliceVerticeIndex) ? slice.Points [j] : slice.Points [0];
                    } else
                    {
                        pt = slice.Points [j];
                    }
                
                    vertices [vi] = pt;

                    if (facesDir == FaceDir.Down)
                    {
                        normals [vi] = (pt - sliceCenter).normalized;

                    } else
                    {
                        // 'up' side; also the first side of double-sided meshes
                        normals [vi] = (sliceCenter - pt).normalized;
                    }
                
                    // uv mapping 
                    /*if (j > 0) {
                    //v += (pt - slice.points[j - 1]).magnitude;

                }*/
                
                    float u = (float)j / (float)(verticesPerSliceSide - 1);
                
                    uv [vi] = new Vector2(u, v);
                    //Debug.Log ("uv: " + uv[vi] + "; j=" + j);
                    //u += 0.25f;
                
                    // Tangents / experimental
                    if (createTangents)
                    {
                        tangents [vi] = new Vector4(slice.Direction.x, slice.Direction.y, slice.Direction.z, -1f);
                    }
                }
                if (perSideVertices && faceSides > 1)
                {
                    // Other side ("Down side")
                    // Clone first side vertices and invert normals
                    for (int j = 0; j < verticesPerSliceSide; j++)
                    {
                        int vi0 = voffs + j;
                        int vi = vi0 + verticesPerSliceSide;

                        vertices [vi] = vertices [vi0];
                        normals [vi] = normals [vi0] * -1.0f;
                        if (createTangents)
                        {
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

            int triangleCount = CalculateTriangleCount(sliceCount, facesDir == FaceDir.Both);
            int facesPerSegment = verticesPerSliceSide - 1;


            if (facesDir == FaceDir.Both)
            {

                if (triangleCount % 2 > 0)
                {
                    throw new System.Exception("Double-sided faces requested but triangleCount is uneven!");
                }



                int trianglesOffset2;
                int[] triangles;
                if (perSideSubmeshes)
                {
                    mesh.subMeshCount = 2;
                    triangles = new int[triangleCount * 3 / 2];
                    trianglesOffset2 = 0;
                } else
                {
                    mesh.subMeshCount = 1;
                    triangles = new int[triangleCount * 3];
                    trianglesOffset2 = triangles.Length / 2;
                }
                int verticesOffset = perSideVertices ? verticesPerSliceSide : 0;

                // First  side ("up")
                DoCreateTriangles(triangles, 0, FaceDir.Up, facesPerSegment, verticesPerSlice, 0, sliceCount);
                if (perSideSubmeshes)
                {
                    mesh.SetTriangles(triangles, 0);
                }

                // Second side ("down")
                // we can recycle the same "triangles" array
                DoCreateTriangles(triangles, trianglesOffset2, FaceDir.Down, facesPerSegment, verticesPerSlice, verticesOffset, sliceCount);

                mesh.SetTriangles(triangles, perSideSubmeshes ? 1 : 0);

            } else
            {
                mesh.subMeshCount = 1;
                int[] triangles = new int[triangleCount * 3];
                DoCreateTriangles(triangles, 0, facesDir, facesPerSegment, verticesPerSlice, 0, sliceCount);
                mesh.SetTriangles(triangles, 0);
            }

            mesh.MarkDynamic();

            Debug.Log("Created a Mesh with " + vertices.Length + " vertices and " + triangleCount + " triangles in " + mesh.subMeshCount + " submeshes.");

        }

        private int CalculateTriangleCount(int sliceCount, bool doubleSided)
        {
            const int verticesPerTriangle = 3;
        
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

        private static void DoCreateTriangles(int[] triangles, int offset, FaceDir facesDir, int facesPerSegment, int verticesPerSlice, int verticesOffset, int sliceCount)
        {

            if (facesDir == FaceDir.Both)
            {
                throw new System.ArgumentException("Can't create triangles with FaceDir.Both - use separate calls for both directions");
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
        
            for (int i = 1; i < sliceCount; i++)
            { 
            
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
                for (int j = 0; j < facesPerSegment; j++)
                {
                
                    int ftoffs = toffs + trianglesPerFace * verticesPerTriangle * j;
                    //Debug.Log ("Slice " + i + "/" + sliceCount + "; ftoffs=" + ftoffs);
                
                    if (facesDir == FaceDir.Down)
                    {
                        // "outside"
                        triangles [ftoffs + 0] = voffs1 + j;
                        triangles [ftoffs + 1] = voffs1 + j + 1;
                        triangles [ftoffs + 2] = voffs0 + j;
                    
                        triangles [ftoffs + 3] = voffs1 + j + 1;
                        triangles [ftoffs + 4] = voffs0 + j + 1;
                        triangles [ftoffs + 5] = voffs0 + j;
                    } else
                    {
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



