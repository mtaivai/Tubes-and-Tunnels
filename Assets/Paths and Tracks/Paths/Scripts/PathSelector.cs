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
	// TODO do we really need separate IPathInfo? Maybe yes, because some path related
	// tools can be used without the actual Path object, for example PathModifiers don't
	// really need a reference to Path, the just need valid array of PathPoints. In some
	// cases we don't have the original Path instance available, for example Track has
	// its own set of PathModifiers.

	[System.Serializable]
	public class PathSelector
	{

		// TODO should we store Path by id instead?
		[SerializeField]
		private Path
			path;

		[SerializeField]
		private int
			dataSetId; // 0 == default

		[SerializeField]
		private bool
			useSnapshot;

		[SerializeField]
		private string
			snapshotName;

		public PathSelector (Path path, int dataSetId, bool useSnapshot, string snapshotName)
		{
			this.path = path;
			this.dataSetId = dataSetId;
			this.useSnapshot = useSnapshot;
			this.snapshotName = snapshotName;
		}
		public PathSelector (Path path, int dataSetId, string snapshotName) : this(path, dataSetId, true, snapshotName)
		{
			
		}

		public PathSelector (Path path, int dataSetId) : this(path, dataSetId, false, "")
		{
			
		}

		public PathSelector (Path path) : this(path, 0)
		{
			
		}

		public PathSelector (PathSelector src) : this(src.path, src.dataSetId, src.useSnapshot, src.snapshotName)
		{
			
		}

		public PathSelector () : this(null, 0)
		{
			
		}

		public Path Path { get { return this.path; } }
		public int DataSetId { get { return this.dataSetId; } }

		public int ActualDataSetId {
			get {
				if (null != path && dataSetId == 0) {
					// Default id; dereference:
					return path.GetDefaultDataSetId ();
				} else {
					return dataSetId;
				}
			}
		}

		public bool UseSnapshot { get { return this.useSnapshot; } }
		public string SnapshotName { get { return this.snapshotName; } }

		public IPathData PathData {
			get {
				return (null != path) ? path.FindDataSetById (dataSetId) : null;
			}
		}

		public PathPoint[] PathPoints {
			get {
				PathPoint[] points = DoGetPathPoints ();
				return (null != points) ? points : new PathPoint[0];
			}
		}

		public int PathOutputFlags {
			get {
				return DoGetOutputFlags ();
			}
		}
		public bool HasValidData {
			get {
				// TODO 
				PathPoint[] points;
				int flags;
				return DoGetPathPoints (false, out points, false, out flags, true);
			}
		}

		public PathPoint[] GetPathPoints (out int flags)
		{
			PathPoint[] points;
			DoGetPathPoints (out points, out flags);
			return points;
		}

		private void DoGetPathPoints (out PathPoint[] points, out int flags)
		{
			DoGetPathPoints (true, out points, true, out flags);
		}

		private PathPoint[] DoGetPathPoints ()
		{
			PathPoint[] points;
			int flags;
			DoGetPathPoints (true, out points, false, out flags);
			return points;
		}
		private int DoGetOutputFlags ()
		{
			PathPoint[] points;
			int flags;
			DoGetPathPoints (false, out points, true, out flags);
			return flags;
		}
		private bool DoGetPathPoints (bool getPoints, out PathPoint[] points, bool getFlags, out int flags, bool supressWarnings = false)
		{
			bool gotData = false;
			IPathData data = PathData;
			if (null != data) {
				if (useSnapshot) {
					IPathSnapshotManager ssm = data.GetPathSnapshotManager ();
					if (null != ssm && ssm.SupportsSnapshots ()) {
						if (ssm.ContainsSnapshot (snapshotName)) {
							if (getPoints && getFlags) {
								PathDataSnapshot ss = ssm.GetSnapshot (snapshotName);
								points = ss.Points;
								flags = ss.Flags;
							} else if (getPoints) {
								points = ssm.GetSnapshotPoints (snapshotName);
								flags = 0;
							} else if (getFlags) {
								points = null;
								flags = ssm.GetSnapshotPointFlags (snapshotName);
							} else {
								points = null;
								flags = 0;
							}
							gotData = true;

						} else {
							// Snapshot not found
							if (!supressWarnings) {
								Debug.LogWarning ("Snapshot not found: " + ToString ());
							}
							points = null;
							flags = 0;
						}
					} else {
						// Data doesn't support snapshots
						if (!supressWarnings) {
							Debug.LogWarning ("PathData doesn't support snapshots: " + ToString ());
						}
						points = null;
						flags = 0;
					}
				} else {
					points = getPoints ? data.GetAllPoints () : null;
					flags = getFlags ? data.GetOutputFlags () : 0;
					gotData = true;
				}
			} else {
				if (!supressWarnings) {
					Debug.LogWarning ("Data not available: " + ToString ());
				}
				points = null;
				flags = 0;
			}
			return gotData;
		}

		public override String ToString ()
		{
			return ToString (path, PathData, useSnapshot, snapshotName);
		}
		public static String ToString (Path path, IPathData pathData)
		{
			return ToString (path, pathData, false, "");
		}

		public static String ToString (Path path, IPathData pathData, bool useSnapshot, string snapshotName)
		{
			string s;
			if (null == path) {
				s = "(none)";
			} else {
				s = path.name;
				if (null != pathData) {
					s += ":" + pathData.GetName ();
				} else {
					s += ":(none)";
				}
				if (useSnapshot) {
					s += "; snapshot='" + snapshotName + "'";
				}
			}
			return s;
		}
		public PathSelector WithPath (Path value)
		{
			return new PathSelector (value, dataSetId, useSnapshot, snapshotName);
		}
		public PathSelector WithDataSetId (int value)
		{
			return new PathSelector (path, value, useSnapshot, snapshotName);
		}
		public PathSelector WithUseSnapshot (bool value)
		{
			return new PathSelector (path, dataSetId, value, snapshotName);
		}
		public PathSelector WithSnapshot ()
		{
			return WithUseSnapshot (true);
		}
		public PathSelector WithoutSnapshot ()
		{
			return WithUseSnapshot (false);
		}
		public PathSelector WithSnapshotName (string value)
		{
			return new PathSelector (path, dataSetId, useSnapshot, value);
		}
		public override bool Equals (System.Object obj)
		{
			PathSelector that = obj as PathSelector;
			return this == that;
		}

		public override int GetHashCode ()
		{
			unchecked {
				int hash = (path != null ? path.GetHashCode () : 0) ^ ActualDataSetId.GetHashCode () ^ useSnapshot.GetHashCode ();
				if (useSnapshot) {
					hash ^= (snapshotName != null ? snapshotName.GetHashCode () : 0);
				}
				return hash;
			}
		}

		public static bool operator == (PathSelector a, PathSelector b)
		{
			if (ReferenceEquals (a, b)) {
				return true;
			} else if (ReferenceEquals (a, null)) {
				// if 'a' is null, 'b' can't be null (see previous test)
				return false;
			} else {
				if (!ReferenceEquals (a.path, b.path)) {
					return false;
				} else if (a.ActualDataSetId != b.ActualDataSetId) {
					return false;
				} else if (a.useSnapshot != b.useSnapshot) {
					return false;
				} else if (a.useSnapshot && a.snapshotName != b.snapshotName) {
					return false;
				} else {
					return true;
				}
			}
		}
		public static bool operator != (PathSelector a, PathSelector b)
		{
			return ! (a == b);
		}
	}

	// TODO rename to AbstractPathData

}
