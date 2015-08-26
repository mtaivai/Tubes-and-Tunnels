using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Paths
{

    // TODO add scaling of directions and distances!
    [PathModifier(passthroughCaps=PathPoint.POSITION)]
    public class ScalePathModifier : AbstractPathModifier
    {
        public Vector3 scaling = new Vector3(1f, 1f, 1f);

        public override PathPoint[] GetModifiedPoints(PathPoint[] points, PathModifierContext context)
        {
            PathPoint[] results = new PathPoint[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                results [i] = new PathPoint(
                    Vector3.Scale(points [i].Position, scaling));
            } 
            return results;
        }
        
        public override void OnSerialize(Serializer store)
        {
            store.Property("scaling", ref scaling);
        }
        

    }
    
}
