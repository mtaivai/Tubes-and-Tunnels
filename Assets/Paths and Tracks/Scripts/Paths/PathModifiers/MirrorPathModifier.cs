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
	[PathModifier(requiredInputFlags=PathPoint.POSITION, 
	              processCaps=PathPoint.POSITION | PathPoint.DIRECTION, passthroughCaps=PathPoint.DISTANCES)]
	public class MirrorPathModifier : AbstractPathModifier
	{
		public Vector3 mirrorCenter = Vector3.zero;
		public bool mirrorX;
		public bool mirrorY;
		public bool mirrorZ;

		protected override void OnReset ()
		{
			mirrorCenter = Vector3.zero;
			mirrorX = mirrorY = mirrorZ = false;
		}
		public override void OnSerialize (Serializer ser)
		{
			ser.Property ("mirrorCenter", ref mirrorCenter);
			ser.Property ("mirrorX", ref mirrorX);
			ser.Property ("mirrorY", ref mirrorY);
			ser.Property ("mirrorZ", ref mirrorZ);
		}
		protected override PathPoint[] DoGetModifiedPoints (PathPoint[] points)
		{
			for (int i = 0; i < points.Length; i++) {
				Vector3 pt = points [i].Position;
				bool hasDir = points [i].HasDirection;

				Vector3 dir;
				if (hasDir) {
					dir = points [i].Direction;
				} else {
					dir = Vector3.zero;
				}

				if (mirrorX) {
					pt.x = mirrorCenter.x - pt.x;
					if (hasDir) {
						dir.x = -dir.x;
					}
				}
				if (mirrorY) {
					pt.y = mirrorCenter.y - pt.y;
					if (hasDir) {
						dir.y = -dir.y;
					}
				}
				if (mirrorZ) {
					pt.z = mirrorCenter.z - pt.z;
					if (hasDir) {
						dir.z = -dir.z;
					}
				}

				points [i].Position = pt;
				if (hasDir) {
					points [i].Direction = dir;
				}
			} 
			return points;
		}
		
	}
	
}
