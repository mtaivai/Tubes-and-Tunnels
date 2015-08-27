using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Paths
{
	[PathModifier(displayName="Displace", requiredInputFlags=PathPoint.POSITION, 
	              processCaps=PathPoint.POSITION, passthroughCaps=PathPoint.ALL & ~PathPoint.POSITION)]
	public class DisplacePathModifier : AbstractPathModifier
	{
		public Vector3 displacement;

		public override PathPoint[] GetModifiedPoints (PathPoint[] points, PathModifierContext context)
		{
			for (int i = 0; i < points.Length; i++) {
				points [i].Position += displacement;
			} 
			return points;
		}

		public override void OnSerialize (Serializer store)
		{
			displacement = store.ReturnProperty ("Displacement", displacement);
		}

		
	}
	
}
