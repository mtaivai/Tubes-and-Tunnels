// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Paths
{

	[CodeQuality.Experimental]
	[PathModifier(requiredInputFlags=PathPoint.NONE, passthroughCaps=PathPoint.ALL)]
	public class StoreSnapshotPathModifier : AbstractPathModifier
	{
		public string snapshotName = "";

		public override void OnSerialize (Serializer store)
		{
			store.Property ("snapshotName", ref snapshotName);
		}
		protected override PathPoint[] DoGetModifiedPoints (PathPoint[] points)
		{
			// TODO detect recursive / infinite loops, i.e. circular references

			IPathSnapshotManager snapshotManager = context.PathModifierContainer.GetPathSnapshotManager ();
			if (snapshotManager.SupportsSnapshots ()) {
				// TODO what if we already have the snapshot?
				snapshotManager.StoreSnapshot (snapshotName, points, context.InputFlags);
			} else {
				Debug.LogError ("The Path container does not support snapshots");
			}

			return points;
		}
	}

}
