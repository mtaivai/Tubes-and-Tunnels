// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

using Util;

// TODO we need a GetPoint(float t) method, either directly in a path
// or as a separate utility. For pure bezier paths it would be easy to implement,
// but for polyline or composite paths it woudl need some more effort by
// implementing a smart smoothing / interpolating algorithm. One possiblity would
// be to convert polyline path to bezier path on-the-fly.

namespace Paths
{

	// TODO we don't really need the IPath interface since we're always referring to Path
	// (which is a GameObject)
//  public interface IPath
//  {
//      bool IsLoop ();
//
//  }

	// TODO do we really need separate IPathInfo? Maybe yes, because some path related
	// tools can be used without the actual Path object, for example PathModifiers don't
	// really need a reference to Path, the just need valid array of PathPoints. In some
	// cases we don't have the original Path instance available, for example Track has
	// its own set of PathModifiers.

	public class ClonePath : Path
	{
		[SerializeField]
		private Path
			sourcePath;

		public ClonePath ()
		{
			addLastPointToLoop = false;
		}

		public override void OnBeforePathSerialize ()
		{
		}
		
		public override void OnAfterPathDeserialize ()
		{
			if (null != sourcePath) {
				sourcePath.Changed -= SourcePathChanged;
				sourcePath.Changed += SourcePathChanged;
			}

		}

		private void SourcePathChanged (object sender, EventArgs e)
		{
			base.PathPointsChanged ();
		}

		public override bool IsLoop ()
		{
			return sourcePath != null ? sourcePath.IsLoop () : false;
		}

		protected override List<PathPoint> DoGetPathPoints (out int outputFlags)
		{
			if (null == sourcePath) {
				outputFlags = 0;
				return new List<PathPoint> ();
			} else {
				//return sourcePath.DoGetPathPoints (out outputFlags);
				outputFlags = sourcePath.GetDefaultDataSet ().GetOutputFlags ();
				PathPoint[] pts = sourcePath.GetDefaultDataSet ().GetAllPoints ();
				List<PathPoint> l = new List<PathPoint> ();
				for (int i = 0; i < pts.Length; i++) {
					l.Add (new PathPoint (pts [i]));
				}
				return l;
			}
		}
//
//		public override void OnPathModifiersChanged ()
//		{
//			base.OnPathModifiersChanged ();
//		}
//
//		public override void OnPathPointsChanged ()
//		{
//			base.OnPathPointsChanged ();
//		}
//
//		protected override DefaultPathModifierContainer CreatePathModifierContainer ()
//		{
//			return base.CreatePathModifierContainer ();
//		}
//
		public override int GetControlPointCount ()
		{
			return 0;
		}

		public override Vector3 GetControlPointAtIndex (int index)
		{
			throw new NotImplementedException ();
		}

		public override void SetControlPointAtIndex (int index, Vector3 pt)
		{
			throw new NotImplementedException ();
		}
	}

}
