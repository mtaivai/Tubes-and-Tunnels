using UnityEngine;
using UnityEditor;

using System.Collections;
using System.Collections.Generic;
using System;

using Paths;

namespace Paths.Bezier
{

    // Cubic bezier curve using four control points
    // Heavily inspired by (based on): http://catlikecoding.com/unity/tutorials/curves-and-splines/

    public class BezierPath : Path, ISerializationCallbackReceiver
    {

        public const int CONTROL_POINTS_PER_SEGMENT = 3;
		
		#region Fields
        [SerializeField]
        private Vector3[]
            controlPoints;
		
        [SerializeField]
        private BezierPathSegment[]
            segments;
		
        [SerializeField]
        private BezierPathSegmentJoint[]
            segmentJoints;
		
        [SerializeField]
        private bool
            loop = false;
		
        [SerializeField]
        private int
            pointsPerSegment = 10;
		
//		[SerializeField]
//		private List<PathPoint> pathPoints;
		
//		private bool pathPointsDirty = true;
		
        //private Vector3[] points;
        //private Vector3[] directions;
		
		#endregion
		
		#region	Constructors
        public BezierPath()
        {
			
        }
		#endregion
		
        /*
	#region Finalizers (Destructors)
	#endregion

	#region	Delegates
	#endregion

	#region	Events
	#endregion
	
	#region	Enums
	#endregion
	
	#region	Interfaces
	#endregion*/
		
		#region	Properties


        public int SegmentCount
        {
            get
            {
                return null == controlPoints ? 0 : (controlPoints.Length - 1) / CONTROL_POINTS_PER_SEGMENT;
            }
        }
        public int ControlPointCount
        {
            get
            {
                return null == controlPoints ? 0 : controlPoints.Length;
            }
        }
        internal Vector3[] ControlPoints
        {
            get
            {
                return controlPoints;
            }
        }
        public bool Loop
        {
            get
            {
                return loop;
            }
            set
            {
                loop = value;
            }
        }
		
        public int PointsPerSegment
        {
            get
            {
                return pointsPerSegment;
            }
            set
            {
                this.pointsPerSegment = value;
                SetPointsDirty();
            }
        }
	
		#endregion
		
		#region	Indexers
		#endregion
		
		#region	Overriden methods

		
        void xOnDrawGizmos()
        {
            DrawPath();
        }
		
        // Use this for initialization
        void Start()
        {
			
        }
		
        // Update is called once per frame
        void Update()
        {
			
        }
		
		#endregion
		
		
		#region	Methods

        public override bool IsLoop()
        {
            return Loop;
        }

        public PathPoint[] GeneratePathPoints(int resolution)
        {
            return DoGeneratePathPoints(resolution, true, true, true);
        }
		
        // TODO rename this to UpdatePoints() and generate points already???
        public void SetPointsDirty()
        {
            //this.points = null;
            //this.directions = null;
//			this.pathPoints = null;
//			this.pathPointsDirty = true;
            PathPointsChanged();
        }
//		public void UpdatePathPoints() {
//			if (pathPointsDirty || null == pathPoints) {
//				ForceUpdatePathPoints();
//				
//			}
//		}
        protected override PathPoint[] DoGetPathPoints(out int outputFlags)
        {
            List<PathPoint> points = null;
            DoGeneratePathPoints(ref points, pointsPerSegment, true, true, true);
            outputFlags = PathPoint.POSITION | PathPoint.DIRECTION | PathPoint.UP;
            return points.ToArray();
        }
		
        public Vector3 GetControlPoint(int index, Transform transform = null)
        {
            //Vector3 pt;
            if (loop && index == controlPoints.Length - 1)
            {
                index = 0;
            }
            return null != transform ? transform.TransformPoint(controlPoints [index]) : controlPoints [index];
        }
        public Vector3[] GetControlPoints(int firstIndex, int count, Transform transform = null)
        {
            Vector3[] points = new Vector3[count];
            return GetControlPoints(firstIndex, count, ref points, transform);
        }
        public Vector3[] GetControlPoints(int firstIndex, int count, ref Vector3[] points, Transform transform = null)
        {
            if (null == points || points.Length == 0)
            {
                points = new Vector3[count];
            }
            Array.Copy(controlPoints, firstIndex, points, 0, count);
            if (loop && firstIndex + count == controlPoints.Length)
            {
                points [count - 1] = controlPoints [0];
            }
            if (null != transform)
            {
                for (int i = 0; i < count; i++)
                {
                    points [i] = transform.TransformPoint(points [i]);
                }
            }
            return points;
        }
		
        public void SetControlPoint(int index, Vector3 pt)
        {
			
            if (loop && index == controlPoints.Length - 1)
            {
                index = 0;
            }
			
            // "...whenever you move a point or change a point's mode, the constraints will
            // be enforced. But when moving a middle point, the previous point always stays 
            // fixed and the next point is always enforced. This might be fine, but it's 
            // intuitive if both other points move along with the middle one. So let's adjust 
            // SetControlPoint so it moves them together."
            if (index % 3 == 0)
            { // XXX TODO set to zero!!!!
                Vector3 delta = pt - controlPoints [index];
                if (loop)
                {
                    if (index == 0)
                    {
                        controlPoints [1] += delta;
                        controlPoints [controlPoints.Length - 2] += delta;
                        // points[points.Length - 1] = point;
                    } else
                    {
                        controlPoints [index - 1] += delta;
                        controlPoints [index + 1] += delta;
                    }
                } else
                {
                    if (index > 0)
                    {
                        controlPoints [index - 1] += delta;
                    }
                    if (index + 1 < controlPoints.Length)
                    {
                        controlPoints [index + 1] += delta;
                    }
                }
            }
			
            controlPoints [index] = pt;
            EnforceJointModeOfControlPoint(index);
			
            // Make dirty:
            SetPointsDirty();
        }
		
        public override int GetControlPointCount()
        {
            return this.controlPoints.Length;
        }
        public override Vector3 GetControlPointAtIndex(int index)
        {
            return GetControlPoint(index);
        }
        public override void SetControlPointAtIndex(int index, Vector3 pt)
        {
            this.SetControlPoint(index, pt);
        }

        void Reset()
        {
            controlPoints = new Vector3[] {
				new Vector3(0f, 0f, 1f),
				new Vector3(0f, 0f, 4f),
				new Vector3(0f, 0f, 7f),
				new Vector3(0f, 0f, 10f),
			};
            segments = new BezierPathSegment[] {
				new BezierPathSegment(),
			};
            foreach (BezierPathSegment segment in segments)
            {
                segment.Reset();
            }
            segmentJoints = new BezierPathSegmentJoint[] {
				new BezierPathSegmentJoint(),
				new BezierPathSegmentJoint(),
			};
            foreach (BezierPathSegmentJoint joint in segmentJoints)
            {
                joint.Reset();
            }
            SetPointsDirty();
        }
        public void AddSegment()
        {
            // Disable loop while adding new segment
            bool wasLoop = loop;
            this.loop = false;
			
            Vector3 lastPoint = controlPoints [controlPoints.Length - 1];
            Vector3 lastDir = GetTangentDirection(1.0f);
			
            Vector3 lastPoint1 = controlPoints [controlPoints.Length - 2];
            Vector3 lastPoint2 = controlPoints [controlPoints.Length - 3];
            Vector3 lastPoint3 = controlPoints [controlPoints.Length - 4];
			
			
            float pointDistance = ((lastPoint - lastPoint1).magnitude + 
                (lastPoint1 - lastPoint2).magnitude + 
                (lastPoint2 - lastPoint3).magnitude) / 3.0f;
			
			
            //Debug.Log ("lastDir: " + lastDir);
            //Debug.Log ("pointDistance: " + pointDistance);
			
            Array.Resize(ref controlPoints, controlPoints.Length + CONTROL_POINTS_PER_SEGMENT);
			
            for (int i = controlPoints.Length - 3; i < controlPoints.Length; i++)
            {
                Vector3 newPoint = lastPoint + lastDir * pointDistance;
                controlPoints [i] = newPoint;
                lastPoint = newPoint;
            }
			
            // Segments
            Array.Resize(ref segments, segments.Length + 1);
            segments [segments.Length - 1] = new BezierPathSegment();
			
            // SegmentJoints
            Array.Resize(ref segmentJoints, segmentJoints.Length + 1);
            segmentJoints [segmentJoints.Length - 1] = new BezierPathSegmentJoint();
			
            EnforceJointModeOfControlPoint(controlPoints.Length - 4);
			
            // Restore loop mode
            loop = wasLoop;
			
            SetPointsDirty();
        }
		
        public void RemoveSegment(int segmentIndex)
        {
            if (segmentIndex < 0 || segmentIndex >= SegmentCount)
            {
                throw new ArgumentOutOfRangeException("segmentIndex", segmentIndex.ToString(), "segmentIndex out of range (0.." + (SegmentCount - 1) + ")");
            }
            // Disable loop while adding new segment
            bool wasLoop = loop;
            this.loop = false;
			
            if (segmentIndex < SegmentCount - 1)
            {
                // Not last to delete; we need to rebuild the arrays:
                Vector3[] controlPointsTail = new Vector3[controlPoints.Length - CONTROL_POINTS_PER_SEGMENT * (segmentIndex + 1)];
                Array.Copy(controlPoints, segmentIndex + 1, controlPointsTail, 0, controlPointsTail.Length);
                Array.Resize(ref controlPoints, controlPoints.Length - CONTROL_POINTS_PER_SEGMENT);
                Array.Copy(controlPointsTail, 0, controlPoints, segmentIndex, controlPointsTail.Length);
				
                BezierPathSegment[] segmentsTail = new BezierPathSegment[segments.Length - (segmentIndex + 1)];
                Array.Copy(segments, segmentIndex + 1, segmentsTail, 0, segmentsTail.Length);
                Array.Resize(ref segments, segments.Length - 1);
                Array.Copy(segmentsTail, 0, segments, segmentIndex, segmentsTail.Length);
				
                BezierPathSegmentJoint[] segmentJointsTail = new BezierPathSegmentJoint[segmentJoints.Length - (segmentIndex + 1)];
                Array.Copy(segmentJoints, segmentIndex + 1, segmentJointsTail, 0, segmentJointsTail.Length);
                Array.Resize(ref segmentJoints, segmentJoints.Length - 1);
                Array.Copy(segmentJointsTail, 0, segmentJoints, segmentIndex, segmentJointsTail.Length);
				
				
            } else
            {
                // Last segment; simply shrink arrays
                Array.Resize(ref controlPoints, controlPoints.Length - CONTROL_POINTS_PER_SEGMENT);
                Array.Resize(ref segments, segments.Length - 1);
                Array.Resize(ref segmentJoints, segmentJoints.Length - 1);
            }
			
			
            EnforceJointModeOfControlPoint(controlPoints.Length - 4);
			
            // Restore loop mode
            loop = wasLoop;
			
            SetPointsDirty();
        }
        public BezierPathSegment GetSegment(int index)
        {
            return segments [index];
        }
		
        public BezierPathSegment GetSegmentForControlPoint(int controlPointIndex)
        {
            int segmentIndex = GetSegmentIndex(controlPointIndex);
            return (segmentIndex >= 0) ? segments [segmentIndex] : null;
        }
        public int GetSegmentIndex(int controlPointIndex)
        {
            if (controlPointIndex < 0 || controlPointIndex >= controlPoints.Length)
            {
                return -1;
            } else if (controlPointIndex <= CONTROL_POINTS_PER_SEGMENT)
            {
                // The first segment has one extra control point (the first point)
                return 0;
            } else
            {
                // Open spline has actually controlPoints.Length -1 control points because
                // the first point is not actually part of the spline (it's representing the
                // last point of the "-1" segment. That's why we are using controlPointIndex - 1
                // here:
                return (controlPointIndex - 1) / CONTROL_POINTS_PER_SEGMENT;
            }
        }
        public int GetSegmentIndex(float t)
        {
            float tt = t;
            return GetSegmentIndex(ref tt);
        }
        public int GetSegmentIndex(ref float t)
        {
            int index;
            int segmentCount = segments.Length;
			
            if (t <= 0.0f)
            {
                // First segment
                t = 0.0f;
                index = 0;
            } else if (t >= 1.0f)
            {
                // Last segment
                t = 1.0f;
                index = segmentCount - 1; 
            } else
            {
                t = Mathf.Clamp01(t);
                // Open spline has actually controlPoints.Length -1 control points because
                // the first point is not actually part of the spline (it's representing the
                // last point of the "-1" segment. That's why we are using controlPoints - 2 as the
                // last control point index here:
                float tt = t * (float)(controlPoints.Length - 2) / (float)CONTROL_POINTS_PER_SEGMENT;
                index = (int)tt;
                t = tt - (int)tt;
            }
            return index;
        }
		
        public BezierPathSegment GetSegment(float t)
        {
            int index = GetSegmentIndex(t);
            return index >= 0 ? segments [index] : null;
        }
		
		
        public BezierPathSegmentJoint GetSegmentJoint(int controlPointIndex)
        {
            int index = GetSegmentJointIndex(controlPointIndex);
            return (index >= 0) ? segmentJoints [index] : null;
        }
        public int GetSegmentJointIndex(int controlPointIndex)
        {
            if (controlPointIndex < 0 || controlPointIndex >= controlPoints.Length)
            {
                return -1;
            } else if (controlPointIndex == 0)
            {
                return 0;
            } else
            {
                int jointIndex = Mathf.RoundToInt((float)controlPointIndex / (float)CONTROL_POINTS_PER_SEGMENT);
                if (loop && jointIndex >= segmentJoints.Length - 1)
                {
                    jointIndex = 0;
                }
                return jointIndex;
            }
        }
		
        /// <summary>
        /// Gets the interpolated path point.
        /// </summary>
        /// <returns>The point.</returns>
        /// <param name="t">T.</param>
        public Vector3 GetPoint(float t)
        {
            // Clamp t between 0 and 1:
            t = Mathf.Clamp01(t);
			
            return transform.TransformPoint(BezierUtil.GetPoint(controlPoints, t, loop));
            //return transform.TransformPoint(BezierUtil.GetPoint(segments, t));
        }
		
        private void DoGeneratePathPoints(ref List<PathPoint> dest, int pointsPerSegment, 
                                          bool includePositions, bool includeDirections, bool includeXY)
        {
            //int totalPoints = segments.Length * pointsPerSegment + 1;
            int totalPoints = SegmentCount * pointsPerSegment + 1;
			
            int lastPoint = totalPoints - 1;
			
            int ppFlags = 0;
            if (includePositions)
            {
                ppFlags |= PathPoint.POSITION;
            }

            if (includeDirections)
            {
                ppFlags |= PathPoint.DIRECTION;
            } 
            if (includeXY)
            {
                ppFlags |= PathPoint.UP;
            }

            if (null == dest)
            {
                dest = new List<PathPoint>();
            }
            for (int i = 0; i < totalPoints; i++)
            {
                // TODO calculate distances


                float t = (float)i / (float)lastPoint;

                Vector3 dir;
                if (includeDirections || includeXY)
                {
                    dir = GetTangentDirection(t);
                } else
                {
                    dir = Vector3.zero;
                }

                Vector3 up;
                Vector3 right;
                if (includeXY)
                {
                    up = GetSegment(t).upVector;
                    // "right" vector is the "up" vector rotated 90 degrees by z axis
//                    right = Quaternion.AngleAxis(-90.0f, dir) * up;

                } else
                {
                    up = Vector3.zero;
                }

                dest.Add(new PathPoint(
						includePositions ? transform.TransformPoint(BezierUtil.GetPoint(controlPoints, t, loop)) : Vector3.zero,
						includeDirections ? dir : Vector3.zero,
                        up,
                        0f, 
						0f, 0f, ppFlags));
            }
        }
        private PathPoint[] DoGeneratePathPoints(int pointsPerSegment, bool includePositions, bool includeDirections, bool includeUpVectors)
        {
            List<PathPoint> list = null;
            DoGeneratePathPoints(ref list, pointsPerSegment, includePositions, includeDirections, includeUpVectors);
			
            PathPoint[] points = new PathPoint[list.Count];
            list.CopyTo(points);
            return points;
        }
		

        public Vector3 GetTangentVelocity(float t)
        {
            // Clamp t between 0 and 1:
            t = Mathf.Clamp01(t);
			
            return transform.TransformPoint(BezierUtil.GetFirstDerivate(controlPoints, t, loop)) -
                transform.position;
        }
		
        public Vector3 GetTangentDirection(float t)
        {
            return GetTangentVelocity(Mathf.Clamp01(t)).normalized;
        }
		
        // TODO refactor this method
        private void EnforceJointModeOfControlPoint(int controlPointIndex)
        {
			
            int jointIndex = GetSegmentJointIndex(controlPointIndex);
			
            BezierJointMode mode = segmentJoints [jointIndex].controlPointMode;
			
			
			
            if (mode == BezierJointMode.Free || (!loop && jointIndex <= 0) || jointIndex >= segmentJoints.Length - 1)
            {
                return;
            }
            int middleIndex = jointIndex * CONTROL_POINTS_PER_SEGMENT;
            int fixedIndex, enforcedIndex;
            if (controlPointIndex <= middleIndex)
            {
                fixedIndex = middleIndex - 1;
                enforcedIndex = middleIndex + 1;
            } else if (loop && controlPointIndex == controlPoints.Length - 2)
            {
                fixedIndex = controlPointIndex;
                enforcedIndex = 1;
            } else
            {
                fixedIndex = middleIndex + 1;
                enforcedIndex = middleIndex - 1;
            }
			
            if (fixedIndex < 0)
            {
                fixedIndex = controlPoints.Length - 2;
            }
            if (enforcedIndex < 0)
            {
                enforcedIndex = controlPoints.Length - 2;
            }
			
            //Debug.Log ("mi: " + middleIndex + "; cpi: " + controlPointIndex + "; ji: " + jointIndex + "; ei: " + enforcedIndex + "; fi: " + fixedIndex);
			
            Vector3 middle = controlPoints [middleIndex];
            Vector3 enforcedTangent = middle - controlPoints [fixedIndex];
            if (mode == BezierJointMode.Aligned)
            {
                enforcedTangent = enforcedTangent.normalized * Vector3.Distance(middle, controlPoints [enforcedIndex]);
            }
			
            controlPoints [enforcedIndex] = middle + enforcedTangent;
			
            SetPointsDirty();
			
        }
		
		
		
        private void DrawPath()
        {
			
            // Follow the path!
			
            PathPoint[] pathPoints = GetAllPoints();
			
            // Connect control points:
            if (pathPoints.Length > 0)
            {
                Vector3 startPoint = pathPoints [0].Position;
				
                for (int i = 1; i < pathPoints.Length; i++)
                {
                    Vector3 endPoint = pathPoints [i].Position;
					
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(startPoint, endPoint);
                    Gizmos.DrawSphere(startPoint, 0.3f);
					
                    startPoint = endPoint;
                }
            }
			
        }
		
		#endregion
		
		#region	Structs
		#endregion
		
		#region	Classes
		#endregion
    }
}
