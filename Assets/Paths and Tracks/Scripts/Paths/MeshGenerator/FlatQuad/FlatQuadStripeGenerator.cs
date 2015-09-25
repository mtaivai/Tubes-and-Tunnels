
using System;
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
		private DynParam width = new DynParam (2.0f);



		public FlatQuadStripeGenerator () : base()
		{
			FacesDir = MeshFaceDir.Up;
		}
		public DynParam Width {
			get {
				return this.width;
			}
			set {
				this.width = value;
			}
		}
		public override string DisplayName {
			get {
				return "Flat Quad";
			}
		}

		protected override int GetSliceEdgeCount ()
		{
			return 1;
		}
//		protected override bool IsSliceClosedShape ()
//		{
//			return false;
//		}

		public override void OnLoadParameters (ParameterStore store)
		{
			base.OnLoadParameters (store);
			// TODO add DynParam support to ParameterStore!

			width = DynParam.Load (store, "width");
			//width = store.GetFloat ("width", width);
		}

		public override void OnSaveParameters (ParameterStore store)
		{
			base.OnSaveParameters (store);
			width.Save (store, "width");
			//store.SetFloat ("width", width);

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

		protected override SliceStripSlice CreateSlice (PathDataSource dataSource, int pointIndex, PathPoint pp)
		{
//			IPathData data = dataSource.ProcessedPathData;
			// TODO do we have metadata?
			float? widthValue = width.GetValue (pp, UnsupportedPathMetadata.Instance);
			if (null == widthValue) {
				throw new Exception ("No width value available for point at index " + pointIndex + ": " + widthValue);
			}
			return new FlatQuadStripeSlice ((float)widthValue);
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
		public FlatQuadStripeSlice (float width)
			: base(new Vector3[] { new Vector3 (-width / 2.0f, 0, 0), new Vector3 (width / 2.0f, 0, 0) }, new Vector3[] {Vector3.up, Vector3.up}, false)
		{
		}
    
	}
}