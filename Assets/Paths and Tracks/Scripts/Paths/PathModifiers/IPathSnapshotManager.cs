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
	[Serializable]
	public class PathDataSnapshot
	{
		[SerializeField]
		private string
			name;
		
		[SerializeField]
		private PathPoint[]
			points;
		
		[SerializeField]
		private int
			flags;
		
		public PathDataSnapshot (string name, PathPoint[] points, int flags)
		{
			if (null == name) {
				throw new ArgumentException ("Mandatory argument 'name' is not specified (is null)");
			}
			if (null == points) {
				throw new ArgumentException ("Mandatory argument 'points' is not specified (is null)");
			}
			this.name = name;
			this.points = points;
			this.flags = flags;
		}
		/// <summary>
		/// Construct a deep clone.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="points">Points.</param>
		/// <param name="flags">Flags.</param>
		public PathDataSnapshot (PathDataSnapshot src)
		{
			this.name = src.name;
			this.points = new PathPoint[src.points.Length];
			for (int i = 0; i < this.points.Length; i++) {
				this.points [i] = new PathPoint (src.points [i]);
			}
			this.flags = src.flags;
		}
		public string Name {
			get {
				return name;
			}
		}
		public PathPoint[] Points {
			get {
				return points;
			}
		}
		public int Flags {
			get {
				return flags;
			}
		}
	}

	public interface IPathSnapshotManager
	{
		bool SupportsSnapshots ();
		bool ContainsSnapshot (string name);
		void StoreSnapshot (string name, PathPoint[] points, int flags);
		PathDataSnapshot GetSnapshot (string name);
		int GetSnapshotPointFlags (string name);
		PathPoint[] GetSnapshotPoints (string name);

		string[] GetAvailableSnapshotNames ();
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
		public string[] GetAvailableSnapshotNames ()
		{
			return new string[0];
		}

	}
}
