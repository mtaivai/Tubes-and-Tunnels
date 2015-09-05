// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Paths
{

	public interface IPathSnapshotManager
	{
		bool SupportsSnapshots ();
		bool ContainsSnapshot (string name);
		void StoreSnapshot (string name, PathPoint[] points, int flags);
		PathDataSnapshot GetSnapshot (string name);
		int GetSnapshotPointFlags (string name);
		PathPoint[] GetSnapshotPoints (string name);
	}
	public sealed class UnsupportedSnapshotManager : IPathSnapshotManager
	{
		public static readonly UnsupportedSnapshotManager Instance = new UnsupportedSnapshotManager ();
		private UnsupportedSnapshotManager ()
		{
		}
		public bool SupportsSnapshots ()
		{
			return false;
		}
		public bool ContainsSnapshot (string name)
		{
			return false;
		}
		public void StoreSnapshot (string name, PathPoint[] points, int flags)
		{
			throw new NotSupportedException ("Snapshots are not supported");
		}
		public PathDataSnapshot GetSnapshot (string name)
		{
			throw new NotSupportedException ("Snapshots are not supported");
		}
		public int GetSnapshotPointFlags (string name)
		{
			throw new NotSupportedException ("Snapshots are not supported");
		}
		
		public PathPoint[] GetSnapshotPoints (string name)
		{
			throw new NotSupportedException ("Snapshots are not supported");
		}
	}
}
