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
        public Vector3 Displacement { get; set; }

        public override PathPoint[] GetModifiedPoints(PathPoint[] points, PathModifierContext context)
        {
            PathPoint[] results = new PathPoint[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                results [i] = new PathPoint(
					points [i].Position + Displacement, points [i].Direction, 
                    points [i].Up, points [i].Angle, 
					points [i].DistanceFromPrevious, points [i].DistanceFromBegin, 
					context.InputFlags);
            } 
            return results;
        }

        public override void OnSerialize(Serializer store)
        {
            Displacement = store.ReturnProperty("Displacement", Displacement);
        }

		
    }
	
}
