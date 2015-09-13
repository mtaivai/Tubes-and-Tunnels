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
		private float startAngle = 0f;
		private float arcLength = 360.0f;

		private Vector2 sliceSize = new Vector2 (2f, 2f);

		public TubeGenerator () : base()
		{
		}

		public override string DisplayName {
			get {
				return "Tube";
			}
		}

		public int SliceEdges {
			get {
				return this.sliceEdges;
			}
			set {
				sliceEdges = value;
			}
		}

		public float StartAngle {
			get {
				return this.startAngle;
			}
			set {
				this.startAngle = value;//Mathf.Clamp (value, 0f, 360f);
			}
		}

		public float ArcLength {
			get {
				return this.arcLength;
			}
			set {
				this.arcLength = Mathf.Clamp (value, 0f, 360f);
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
			startAngle = store.GetFloat ("startAngle", startAngle);
			arcLength = store.GetFloat ("arcLength", arcLength);
			sliceSize = store.GetVector2 ("sliceSize", sliceSize);
		}

		public override void OnSaveParameters (ParameterStore store)
		{
			base.OnSaveParameters (store);
			store.SetInt ("sliceEdges", sliceEdges);
			store.SetFloat ("startAngle", startAngle);
			store.SetFloat ("arcLength", arcLength);
			store.SetVector2 ("sliceSize", sliceSize);
		}

		protected override int GetSliceEdgeCount ()
		{
			return sliceEdges;
		}
//		protected override bool IsSliceClosedShape ()
//		{
//			return true;
//		}


		protected override SliceStripSlice CreateSlice (PathPoint pp)
		{
			return new TubeSlice (SliceEdges, startAngle, arcLength, sliceSize);
		}


	}

}



