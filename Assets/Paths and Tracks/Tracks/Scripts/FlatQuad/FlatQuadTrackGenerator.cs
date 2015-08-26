using UnityEngine;
using UnityEditor;
using Util;
using Paths;

using Tracks;
using Tracks.Tube;

namespace Tracks.FlatQuad
{
    public class FlatQuadTrackGenerator : TubeGenerator
    {
        private float width = 2.0f;

        public override string DisplayName
        {
            get
            {
                return "Flat Quad";
            }
        }

        public FlatQuadTrackGenerator() : base()
        {
            SliceEdges = 1;
            FacesDir = FaceDir.Up;
        }

        public override int SliceEdges
        {
            get
            {
                return 1;
            }
            set
            {
                base.SliceEdges = 1;
            } 
        }

        public override void LoadParameters(ParameterStore store)
        {
            //base.LoadParameters (store);
            width = store.GetFloat("width", width);
        }

        public override void SaveParameters(ParameterStore store)
        {
            //base.SaveParameters (store);
            store.SetFloat("width", width);

        }
        /*
    public override void DrawInspectorGUI (TrackInspector trackInspector) {
        //base.OnInspectorGUI(trackInspector);

        EditorGUI.BeginChangeCheck();
        width = EditorGUILayout.FloatField("Width", width);
        if (EditorGUI.EndChangeCheck()) {
            //EditorUtility.SetDirty(trackInspector.target);
            trackInspector.TrackGeneratorModified();
        }
    }*/

        protected override TrackSlice CreateSlice(Vector3 center, Quaternion sliceRotation)
        {
            return new FlatQuadTrackSlice(center, sliceRotation, width);
        }

        public override Mesh CreateMesh(Track track, Mesh mesh)
        {
//      return DoCreateMesh(path, mesh, 1, false, true, false);
            DoCreateMesh(track, mesh, false);
            return mesh;
        }


    }

    public class FlatQuadTrackSlice : TrackSlice
    {
        private Vector3[] points;
    
        public FlatQuadTrackSlice(Vector3 center, Quaternion rotation, float width) 
    : base(center, rotation)
        {
        
            points = new Vector3[] {
            new Vector3(-width / 2.0f, 0, 0),
            new Vector3(width / 2.0f, 0, 0),

        };

            circumference = width;
        }
    
        protected override Vector3[] GetLocalPoints()
        {
            return points;
        }
    }
}

