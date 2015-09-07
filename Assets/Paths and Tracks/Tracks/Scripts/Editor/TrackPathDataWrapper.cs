// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

using Util;
using Util.Editor;
using Paths;
using Paths.Editor;

namespace Tracks.Editor
{

	// TODO move this class outside this class!
	// This is for PathModifierEditor drawing only, wraps the TrackDataSource's unprocessed data
	internal class TrackPathDataWrapper : IPathData
	{
		private Track track;
		private IPathData sourcePathData;
		private int initialOutputFlags;
		
		public TrackPathDataWrapper (Track track)
		{
			this.track = track;
			this.sourcePathData = track.PrimaryDataSource.PathSelector.PathData;
			initialOutputFlags = this.sourcePathData.GetOutputFlags ();
		}
		public IPathInfo GetPathInfo ()
		{
			return sourcePathData.GetPathInfo ();
		}
		public int GetId ()
		{
			return 1;
		}
		
		public string GetName ()
		{
			return "Default";
		}
		public Color GetColor ()
		{
			return sourcePathData.GetColor ();
		}
		public void SetColor (Color value)
		{
			throw new NotSupportedException ();
		}
		public bool IsDrawGizmos ()
		{
			return true;
		}
		
		public void SetDrawGizmos (bool value)
		{
			throw new NotSupportedException ();
		}
		//			public PathDataInputSource GetInputSource ()
		//			{
		//				return PathDataInputSourceSelf.Instance;
		//			}
		public IPathSnapshotManager GetPathSnapshotManager ()
		{
			return UnsupportedSnapshotManager.Instance;
		}
		public IPathModifierContainer GetPathModifierContainer ()
		{
			return track.GetPathModifierContainer ();
		}
		
		public PathPoint[] GetAllPoints ()
		{
			//				return track.PrimaryDataSource.UnprocessedPoints;
			throw new NotSupportedException ();
		}
		
		public int GetPointCount ()
		{
			//				return track.PrimaryDataSource.UnprocessedPoints.Length;
			throw new NotSupportedException ();
		}
		
		public PathPoint GetPointAtIndex (int index)
		{
			//				return track.PrimaryDataSource.UnprocessedPoints[index];
			throw new NotSupportedException ();
		}
		
		public int GetOutputFlags ()
		{
			return initialOutputFlags;
			//return track.PrimaryDataSource.ProcessedFlags;
			//				throw new NotSupportedException ();
		}
		
		public int GetOutputFlagsBeforeModifiers ()
		{
			return initialOutputFlags;
			//return track.PrimaryDataSource.UnprocessedFlags;
		}
		
		public float GetTotalDistance ()
		{
			throw new NotSupportedException ();
		}
		
		public int GetControlPointCount ()
		{
			return 0;
		}
		
		public Vector3 GetControlPointAtIndex (int index)
		{
			throw new NotSupportedException ();
		}
		
		public void SetControlPointAtIndex (int index, Vector3 pt)
		{
			throw new NotSupportedException ();
		}
		
		public bool IsUpToDate ()
		{
			//				return originalPathData.IsUpToDate ();
			throw new NotSupportedException ();
		}
		
		public long GetStatusToken ()
		{
			//				return originalPathData.GetStatusToken ();
			throw new NotSupportedException ();
		}
	}

}
