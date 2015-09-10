using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

using Util;
using Paths;
using Paths.MeshGenerator;
using Paths.MeshGenerator.SliceStrip;

namespace Paths.MeshGenerator.Tube
{
	public class TubeGenerator : AbstractSliceStripGenerator
	{


		private int sliceEdges = 16;
		private Vector2 sliceSize = new Vector2 (2f, 2f);


		public TubeGenerator () : base()
		{
		}

		public override string DisplayName {
			get {
				return "Tube";
			}
		}

		public virtual int SliceEdges {
			get {
				return this.sliceEdges;
			}
			set {
				sliceEdges = value;
			}
		}

		public Vector2 SliceSize {
			get {
				return this.sliceSize;
			}
			set {
				sliceSize = value;
			}
		}

		public override void OnLoadParameters (ParameterStore store)
		{
			base.OnLoadParameters (store);
			sliceEdges = store.GetInt ("sliceEdges", sliceEdges);
			sliceSize = store.GetVector2 ("sliceSize", sliceSize);
		}

		public override void OnSaveParameters (ParameterStore store)
		{
			base.OnSaveParameters (store);
			store.SetInt ("sliceEdges", sliceEdges);
			store.SetVector2 ("sliceSize", sliceSize);
		}

		protected override int GetSliceEdgeCount ()
		{
			return sliceEdges;
		}
		protected override bool IsSliceClosedShape ()
		{
			return true;
		}


		protected override SliceStripSlice CreateSlice (Vector3 center, Quaternion sliceRotation)
		{
			return new TubeSlice (center, sliceRotation, SliceEdges, sliceSize.x, sliceSize.y, this.SliceRotation);
		}


	}

}



