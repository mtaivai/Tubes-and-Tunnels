using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

using Util;

namespace Paths
{

    public class PathUtil
    {
        public static Vector3 IntersectDirection(Vector3[] points, int index)
        {
            Vector3 prevDir, nextDir;
            return IntersectDirection(points, index, Vector3.zero, out prevDir, out nextDir);
        }

        public static Vector3 IntersectDirection(Vector3[] points, int index, out Vector3 prevDir, out Vector3 nextDir)
        {
            return IntersectDirection(points, index, Vector3.zero, out prevDir, out nextDir);
        }
       
        public static Vector3 IntersectDirection(Vector3[] points, int index, Vector3 singlePointDir, out Vector3 prevDir, out Vector3 nextDir)
        {
            if (points.Length == 0)
            {
                prevDir = nextDir = Vector3.zero;
                return Vector3.zero;
            }

            Vector3 dir;
            bool hasPrev = index > 0;
            bool hasNext = index < points.Length - 1;

            if (!hasPrev)
            {
                // First point; direction is from this to next
                if (!hasNext)
                {
                    //...but this is the only point so we use zero as direction
                    dir = singlePointDir;
                } else
                {
                    dir = (points [index + 1] - points [index]).normalized;
                }
                prevDir = dir;
                nextDir = dir;
            } else if (!hasNext)
            {
                // Last point; direction is from previous to this
                dir = (points [index] - points [index - 1]).normalized;
                prevDir = dir;
                nextDir = dir;
            } else
            {
                // Average direction from previous to next
                prevDir = (points [index] - points [index - 1]).normalized;
                nextDir = (points [index + 1] - points [index]).normalized;
                dir = ((nextDir + prevDir) / 2.0f).normalized;
            }
            return dir;
        }

        public static Vector3 IntersectDirection(PathPoint[] points, int index)
        {
            Vector3 prevDir, nextDir;
            return IntersectDirection(points, index, Vector3.zero, out prevDir, out nextDir);
        }
        
        public static Vector3 IntersectDirection(PathPoint[] points, int index, out Vector3 prevDir, out Vector3 nextDir)
        {
            return IntersectDirection(points, index, Vector3.zero, out prevDir, out nextDir);
        }
        
        public static Vector3 IntersectDirection(PathPoint[] points, int index, Vector3 singlePointDir, out Vector3 prevDir, out Vector3 nextDir)
        {
            if (points.Length == 0)
            {
                prevDir = nextDir = Vector3.zero;
                return Vector3.zero;
            }

            // Build a small array of Vector3's to include the point, its preceeding and following point positions:
            int itemsAfterIndex = Math.Min(points.Length - index - 1, 1);

            Vector3[] positions;
            int positionIndex;
            if (itemsAfterIndex > 0)
            {
                // We have "nextDir"...
                if (index > 0)
                {
                    // ...and we also have "prevDir"
                    positions = new Vector3[]
                    {
                        points [index - 1].Position,
                        points [index].Position,
                        points [index + 1].Position
                    };
                    positionIndex = 1;
                } else
                {
                    // ...but we don't have "prevDir"
                    positions = new Vector3[]
                    {
                        points [index].Position,
                        points [index + 1].Position
                    };
                    positionIndex = 0;
                }
            } else
            {
                // We don't have "nextDir"...
                if (index > 0)
                {
                    // ...but we do have "prevDir"
                    positions = new Vector3[]
                    {
                        points [index - 1].Position,
                        points [index].Position
                    };
                    positionIndex = 1;

                } else
                {
                    // ...and we don't have "prevDir" - i.e. only single point!
                    positions = new Vector3[] {points [index].Position};
                    positionIndex = 0;
                }
            }
            return IntersectDirection(positions, positionIndex, singlePointDir, out prevDir, out nextDir);
        }

    }
    
}
