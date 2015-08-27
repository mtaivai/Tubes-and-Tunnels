using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Paths
{
    // TODO should we make PathPoint mutable? Vector3 itself is mutable...
    [Serializable]
    public class PathPoint
    {

        /// <summary>
        ///  Position.
        /// </summary>
        public const int POSITION = 0x001;

        /// <summary>
        /// Direction of the path in the current point, i.e. the forward vector, the local z-axis.
        /// </summary>
        public const int DIRECTION = 0x002;

        /// <summary>
        /// Distance from the previous point to this point.
        /// </summary>
        public const int DISTANCE_FROM_PREVIOUS = 0x004;

        /// <summary>
        /// Distance from beginning of the path to this point.
        /// </summary>
        public const int DISTANCE_FROM_BEGIN = 0x008;

        /// <summary>
        /// Mask for all Distances (DISTANCE_FROM_PREVIOUS, DISTANCE_FROM_BEGIN).
        /// </summary>
        public const int DISTANCES = 0x00c;

        /// <summary>
        /// The Up vector (local y-axis)
        /// </summary>
        public const int UP = 0x010;

        /// <summary>
        /// The right vector (local x-axis)
        /// </summary>
        private const int RIGHT_CALCULATED = 0x020;
        public const int ANGLE = 0x040;

        /// <summary>
        /// All flags.
        /// </summary>
        public const int ALL = 0x03f;

        /// <summary>
        /// No flags
        /// </summary>
        public const int NONE = 0x000;
        [SerializeField]
        private Vector3
            position;
        [SerializeField]
        private float
            distanceFromPrevious;
        [SerializeField]
        private float
            distanceFromBegin;
        [SerializeField]
        private Vector3
            direction; // direction from this to next point


        /// <summary>
        /// The up vector
        /// </summary>
        [SerializeField]
        private Vector3
            up;
        [SerializeField]
        private Vector3
            right;
        [SerializeField]
        private float
            angle;
        [SerializeField]
        private int
            flags;

        internal PathPoint (Vector3 pos, Vector3 dir, Vector3 up, float angle, float distFromPrev, float distFromBegin, int flags)
        {
            this.position = pos;
            this.direction = dir;
            this.up = up;
            this.angle = angle;
            this.distanceFromPrevious = distFromPrev;
            this.distanceFromBegin = distFromBegin;
            this.flags = (flags & ~RIGHT_CALCULATED);
        }
//        internal PathPoint(Vector3 pos, Vector3 dir, Vector3 up, float angle, float distFromPrev, float distFromBegin)
//            : this(pos, dir, up, angle, distFromPrev, distFromBegin, ALL)
//        {
//        }
//        internal PathPoint(Vector3 pos, Vector3 dir, Vector3 up, float distFromPrev, float distFromBegin)
//            : this(pos, dir, up, 0.0f, distFromPrev, distFromBegin, ALL & ~ANGLE)
//        {
//        }
//        internal PathPoint(Vector3 pos, Vector3 dir, float distFromPrev, float distFromBegin, int flags)
//            : this(pos, dir, Vector3.up, distFromPrev, distFromBegin, flags & ~(UP | ANGLE))
//        {
//        }
//        
//        internal PathPoint(Vector3 pos, Vector3 dir, float distFromPrev, float distFromBegin)
//        : this(pos, dir, Vector3.up, distFromPrev, distFromBegin, POSITION|DIRECTION|DISTANCES)
//        {
//            
//        }
//        
//        internal PathPoint(Vector3 pos, Vector3 dir, float distFromPrev)
//        : this(pos, dir, Vector3.up, distFromPrev, 0f, POSITION | DIRECTION | DISTANCE_FROM_PREVIOUS)
//        {
//        }
//        
        internal PathPoint (Vector3 pos, Vector3 dir)
            : this(pos, dir, Vector3.zero, 0f, 0f, 0f, POSITION | DIRECTION)
        {
        }
            
        internal PathPoint (Vector3 pos)
            : this(pos, Vector3.zero, Vector3.zero, 0f, 0f, 0f, POSITION)
        {
        }

        internal PathPoint (PathPoint src)
        : this(src, src.flags)
        {
        }

        internal PathPoint (PathPoint src, int flags)
        : this(src.position, src.direction, src.up, src.angle, src.distanceFromPrevious, src.distanceFromBegin, flags)
        {
            this.right = src.right;
            if (!IsFlag(flags, ALL))
            {
                if (!IsPosition(flags))
                {
                    this.position = Vector3.zero;
                }
                if (!IsDirection(flags))
                {
                    this.direction = Vector3.zero;
                }
                if (!IsUp(flags))
                {
                    this.up = Vector3.zero;
                }
                if (!IsDistanceFromPrevious(flags))
                {
                    this.distanceFromPrevious = 0.0f;
                }
                if (!IsDistanceFromBegin(flags))
                {
                    this.distanceFromBegin = 0.0f;
                }
                if (!IsAngle(flags))
                {
                    this.angle = 0.0f;
                }
            }
        }

        public Vector3 Position {
            get {
                return this.position;
            }
        }

        public Vector3 Direction {
            get {
                return this.direction;
            }
        }

        public Vector3 Up {
            get {
                return this.up;
            }
        }

        public Vector3 Right {
            get {
                if (!IsFlag(flags, RIGHT_CALCULATED) && HasUp && HasDirection)
                {
                    right = Quaternion.AngleAxis(-90.0f, direction) * up;
                    flags |= RIGHT_CALCULATED;
                }
                return this.right;
            }
        }

        public float Angle {
            get {
                return this.angle;
            }
        }

        public float DistanceFromPrevious {
            get {
                return this.distanceFromPrevious;
            }
        }

        public float DistanceFromBegin {
            get {
                return this.distanceFromBegin;
            }
        }

        public int Flags {
            get {
                return this.flags;
            }
        }

        public bool HasPosition {
            get {
                return POSITION == (flags & POSITION);
            }
        }

        public bool HasDirection {
            get {
                return DIRECTION == (flags & DIRECTION);
            }
        }

        public bool HasUp {
            get {
                return UP == (flags & UP);
            }
        }

        public bool HasRight {
            get {
                return IsFlag(flags, (UP | DIRECTION));
            }
        }

        public bool HasAngle {
            get {
                return ANGLE == (flags & ANGLE);
            }
        }

        public bool HasDistanceFromPrevious {
            get {
                return DISTANCE_FROM_PREVIOUS == (flags & DISTANCE_FROM_PREVIOUS);
            }
        }

        public bool HasDistanceFromBegin {
            get {
                return DISTANCE_FROM_BEGIN == (flags & DISTANCE_FROM_BEGIN);
            }
        }

        public static bool IsFlag(int flags, int flag)
        {
            return flag == (flags & flag);
        }

        public static bool IsPosition(int flags)
        {
            return IsFlag(flags, POSITION);
        }

        public static bool IsDirection(int flags)
        {
            return IsFlag(flags, DIRECTION);
        }

        public static bool IsUp(int flags)
        {
            return IsFlag(flags, UP);
        }
//        private static bool IsRight(int flags)
//        {
//            return IsFlag(flags, RIGHT);
//        }

        public static bool IsAngle(int flags)
        {
            return IsFlag(flags, ANGLE);
        }

        public static bool IsDistanceFromPrevious(int flags)
        {
            return IsFlag(flags, DISTANCE_FROM_PREVIOUS);
        }

        public static bool IsDistanceFromBegin(int flags)
        {
            return IsFlag(flags, DISTANCE_FROM_BEGIN);
        }
    }
}

