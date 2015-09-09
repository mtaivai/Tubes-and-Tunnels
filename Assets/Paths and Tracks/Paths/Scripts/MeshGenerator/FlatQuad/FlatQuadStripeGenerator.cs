using UnityEngine;
using UnityEditor;
using Util;
using Paths;
using Paths.MeshGenerator;
using Paths.MeshGenerator.SliceStrip;

namespace Paths.MeshGenerator.FlatQuad
{
	public class FlatQuadStripeGenerator : AbstractSliceStripGenerator
	{
		private float width = 2.0f;

		public override string DisplayName {
			get {
				return "Flat Quad";
			}
		}

		public FlatQuadStripeGenerator () : base()
		{
			SliceEdges = 1;
			FacesDir = MeshFaceDir.Up;
		}



		public override void OnLoadParameters (ParameterStore store)
		{
			base.OnLoadParameters (store);
			width = store.GetFloat ("width", width);
		}

		public override void OnSaveParameters (ParameterStore store)
		{
			base.OnSaveParameters (store);
			store.SetFloat ("width", width);

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

		protected override SliceStripSlice CreateSlice (Vector3 center, Quaternion sliceRotation)
		{
			return new FlatQuadStripeSlice (center, sliceRotation, width);
		}

//		public override Mesh CreateMesh (PathDataSource dataSource, Mesh mesh)
//		{
////      return DoCreateMesh(path, mesh, 1, false, true, false);
//			DoCreateMesh (dataSource, mesh, false);
//			return mesh;
//		}


	}

	public class FlatQuadStripeSlice : SliceStripSlice
	{
		private Vector3[] points;
    
		public FlatQuadStripeSlice (Vector3 center, Quaternion rotation, float width) 
    : base(center, rotation)
		{
        
			points = new Vector3[] {
            new Vector3 (-width / 2.0f, 0, 0),
            new Vector3 (width / 2.0f, 0, 0),

        };

			circumference = width;
		}
    
		protected override Vector3[] GetLocalPoints ()
		{
			return points;
		}
	}
}

