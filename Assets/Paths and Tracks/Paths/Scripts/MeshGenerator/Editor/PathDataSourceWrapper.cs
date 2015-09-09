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

namespace Paths.MeshGenerator.Editor
{

	// TODO move this class outside this class!
	// This is for PathModifierEditor drawing only, wraps the TrackDataSource's unprocessed data
	internal class PathDataSourceWrapper : IPathData
	{
//		private Track track;
		private PathDataSource dataSource;
		private int initialOutputFlags;

		public PathDataSourceWrapper (PathDataSource ds)
		{
			this.dataSource = ds;
			initialOutputFlags = ds.PathSelector.HasValidData ? ds.PathSelector.PathData.GetOutputFlags () : 0;
		}
		public IPathInfo GetPathInfo ()
		{
			return dataSource.GetPathInfo ();
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
			return dataSource.PathSelector.PathData.GetColor ();
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
			return dataSource.GetPathModifierContainer ();
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
