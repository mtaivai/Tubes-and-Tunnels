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


		private Vector2 sliceSize = new Vector2 (2f, 2f);


		public override string DisplayName {
			get {
				return "Tube";
			}
		}

		public TubeGenerator () : base()
		{
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
			sliceSize = store.GetVector2 ("sliceSize", sliceSize);


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

		}

		public override void OnSaveParameters (ParameterStore store)
		{
			base.OnSaveParameters (store);

			store.SetVector2 ("sliceSize", sliceSize);

		}

		protected override SliceStripSlice CreateSlice (Vector3 center, Quaternion sliceRotation)
		{
			return new TubeSlice (center, sliceRotation, SliceEdges, sliceSize.x, sliceSize.y, this.SliceRotation);
		}


	}

}



