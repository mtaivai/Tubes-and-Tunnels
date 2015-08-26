using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Paths
{

    [PathModifier(requiredInputFlags=PathPoint.POSITION, 
                  processCaps=PathPoint.POSITION, 
                  passthroughCaps=PathPoint.DIRECTION | PathPoint.DISTANCES,
                  generateCaps=PathPoint.DIRECTION | PathPoint.DISTANCES)]
    public class NoisePathModifier : AbstractPathModifier
    {

        public enum DirAndDistOutputType
        {
            Passthrough,
            Remove
        }

        public enum PositionOutputType
        {
            Process,
            Passthrough
        }

        public Vector3 NoiseVolume { get; set; }

        public int Seed { get; set; }

        public PositionOutputType positionOutput = PositionOutputType.Process;
        public DirAndDistOutputType directionOutput = DirAndDistOutputType.Passthrough;
        public DirAndDistOutputType distanceOutput = DirAndDistOutputType.Passthrough;

        public override int GetGenerateFlags(PathModifierContext context)
        {
            int f = 0;
            if (positionOutput == PositionOutputType.Process)
            {
                f |= PathPoint.POSITION;
            }
            return f;
        }
        
        public override int GetPassthroughFlags(PathModifierContext context)
        {
            int f = 0;
            if (positionOutput == PositionOutputType.Passthrough)
            {
                f |= PathPoint.POSITION;
            }
            if (directionOutput == DirAndDistOutputType.Passthrough)
            {
                f |= PathPoint.DIRECTION;
            }
            if (distanceOutput == DirAndDistOutputType.Passthrough)
            {
                f |= PathPoint.DISTANCE_FROM_BEGIN | PathPoint.DISTANCE_FROM_PREVIOUS;
            }
            return f;
        }

        public NoisePathModifier()
        {
            Seed = 0;
        }
        // TODO we need to recalculate directions and distances!
        // TODO limit the amplitude between pathpoints!
        public override PathPoint[] GetModifiedPoints(PathPoint[] points, PathModifierContext context)
        {
            System.Random rnd = new System.Random(Seed);

            int ppFlags = (GetPassthroughFlags(context) & context.InputFlags) | GetGenerateFlags(context);

            PathPoint[] results = new PathPoint[points.Length];
            for (int i = 0; i < points.Length; i++)
            {

                Vector3 pos, dir;
                float distFromBegin, distFromPrev;

                if (positionOutput == PositionOutputType.Process)
                {
                    Vector3 displacement = NoiseVolume * (float)rnd.Next(100) / 100.0f;
                    pos = points [i].Position + displacement;
                } else
                {
                    pos = points [i].Position;
                }

                if (directionOutput == DirAndDistOutputType.Passthrough)
                {
                    dir = points [i].Direction;
                } else
                {
                    // Remove direction
                    dir = Vector3.zero;
                }

                if (distanceOutput == DirAndDistOutputType.Passthrough)
                {
                    distFromPrev = points [i].DistanceFromPrevious;
                    distFromBegin = points [i].DistanceFromBegin;
                } else
                {
                    // Remove distances
                    distFromPrev = 0f;
                    distFromBegin = 0f;
                }
                results [i] = new PathPoint(pos, dir, Vector3.zero, 0.0f, distFromPrev, distFromBegin, ppFlags);
            } 
            return results;
        }
        
        public override void OnSerialize(Serializer store)
        {
            NoiseVolume = store.ReturnProperty("NoiseVolume", NoiseVolume);
            Seed = store.ReturnProperty("Seed", Seed);
            store.EnumProperty("positionOutput", ref positionOutput);
            store.EnumProperty("directionOutput", ref directionOutput);
            store.EnumProperty("distanceOutput", ref distanceOutput);

        }

        
    }

}
