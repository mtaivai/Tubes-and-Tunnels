
using System;
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

		private DynParam startAngle = new DynParam (0f);
		private DynParam arcLength = new DynParam (360f);

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

		public DynParam StartAngle {
			get {
				return this.startAngle;
			}
			set {
				this.startAngle = value;//Mathf.Clamp (value, 0f, 360f);
			}
		}

		public DynParam ArcLength {
			get {
				return this.arcLength;
			}
			set {
//				this.arcLength = Mathf.Clamp (value, 0f, 360f);
				this.arcLength = value;
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
//			startAngle = store.GetFloat ("startAngle", startAngle);
//			arcLength = store.GetFloat ("arcLength", arcLength);
			startAngle = store.GetDynParam ("startAngle", startAngle);
			arcLength = store.GetDynParam ("arcLength", arcLength);

			sliceSize = store.GetVector2 ("sliceSize", sliceSize);
		}

		public override void OnSaveParameters (ParameterStore store)
		{
			base.OnSaveParameters (store);
			store.SetInt ("sliceEdges", sliceEdges);
//			store.SetFloat ("startAngle", startAngle);
//			store.SetFloat ("arcLength", arcLength);
			store.SetDynParam ("startAngle", startAngle);
			store.SetDynParam ("arcLength", arcLength);

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


		protected override SliceStripSlice CreateSlice (PathDataSource dataSource, int pointIndex, PathPoint pp)
		{
//			IPathData pathData = dataSource.ProcessedPathData;

			IPathMetadata metadata = dataSource.GetUnprocessedPathMetadata ();

			float startAngleValue = startAngle.GetRequiredValue (pp, metadata);
			float arcLengthValue = arcLength.GetRequiredValue (pp, metadata);
			return new TubeSlice (SliceEdges, startAngleValue, arcLengthValue, sliceSize);
		}



	}

}



