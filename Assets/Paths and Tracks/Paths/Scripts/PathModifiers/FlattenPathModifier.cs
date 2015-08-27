using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Paths
{
	[PathModifier(requiredInputFlags=PathPoint.POSITION, passthroughCaps=PathPoint.UP, processCaps=PathPoint.POSITION)]
	public class FlattenPathModifier : AbstractPathModifier
	{
		public bool SetX { get; set; }

		public bool SetY { get; set; }

		public bool SetZ { get; set; }

		public FlattenPathModifier ()
		{
		}

		public override PathPoint[] GetModifiedPoints (PathPoint[] points, PathModifierContext context)
		{

			int ppFlags = GetOutputFlags (context);

			Vector3 pt0 = points [0].Position;
			if (points.Length > 0) {
				for (int i = 1; i < points.Length; i++) {
					Vector3 pt = points [i].Position;
					points [i].Position = new Vector3 (SetX ? pt0.x : pt.x, SetY ? pt0.y : pt.y, SetZ ? pt0.z : pt.z);
					points [i].Flags = ppFlags;
				}
			}
			return points;
		}
        
		public override void OnSerialize (Serializer store)
		{
			SetX = store.ReturnProperty ("SetX", SetX);
			SetY = store.ReturnProperty ("SetY", SetY);
			SetZ = store.ReturnProperty ("SetZ", SetZ);

		}

        
	}
       
}
