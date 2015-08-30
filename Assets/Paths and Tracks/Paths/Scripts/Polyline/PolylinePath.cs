using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Paths;

namespace Paths.Polyline
{
	public class PolylinePath : Path
	{

		[SerializeField]
		private List<Vector3>
			controlPoints = new List<Vector3> ();

		[SerializeField]
		private bool
			loop = false;

		// not serialized


		public override bool IsLoop ()
		{
			return loop;
		}

		public void SetLoop (bool value)
		{
			if (value != this.loop) {
				this.loop = value;
				PathPointsChanged ();
			}
		}

		public override void OnPathModifiersChanged ()
		{
//          pathPointsDirty = true;
		}

		public override void OnPathPointsChanged ()
		{
//          pathPointsDirty = true;
		}

//      private void UpdatePathPoints() {
//          if (null == pathPoints || pathPointsDirty) {
//              int cpCount = controlPoints.Count;
//              PathPoint[] pp = new PathPoint[cpCount];
//              for (int i = 0; i < cpCount; i++) {
//                  pp[i] = DoGetPathPointAtIndex(i);
//              }
//
//              IPathModifier[] modifiers = GetPathModifiers();
//              foreach (IPathModifier mod in modifiers) {
//                  if (!mod.IsEnabled()) {
//                      continue;
//                  }
//                  pp = mod.GetModifiedPoints(pp);
//              }
//
//              this.pathPoints = new List<PathPoint>(pp);
//              pathPointsDirty = false;
//          }
//      }

//      public override PathPoint[] GetAllPoints ()
//      {
//          UpdatePathPoints();
//          return pathPoints.ToArray();
//      }
//
//      public override int GetPointCount ()
//      {
//          UpdatePathPoints();
//          return pathPoints.Count;
//      }
//
//      public override PathPoint GetPointAtIndex (int index)
//      {
//          UpdatePathPoints();
//          return pathPoints[index];
//      }

		protected override List<PathPoint> DoGetPathPoints (out int outputFlags)
		{
			int cpCount = (null != controlPoints) ? controlPoints.Count : 0;
			List<PathPoint> pp = new List<PathPoint> ();
			for (int i = 0; i < cpCount; i++) {
				pp.Add (DoGetPathPointAtIndex (i));
			}
			// TODO currently we don't generate distances!
			outputFlags = PathPoint.POSITION | PathPoint.DIRECTION;
			return pp;
		}

		protected PathPoint DoGetPathPointAtIndex (int index)
		{
			int lastIndex = controlPoints.Count - 1;
			Vector3 dir;
			if (index == 0) {
				// First point; direction is from this to next
				if (index == lastIndex) {
					//...but this is the only point so we use "forward" as direction
					dir = Vector3.forward;
				} else {
					dir = (controlPoints [index + 1] - controlPoints [index]).normalized;
				}
			} else if (index == lastIndex) {
				// Last point; direction is from previous to this
				dir = (controlPoints [index] - controlPoints [index - 1]).normalized;
			} else {
				// Average direction from previous to next
				Vector3 prevDir = (controlPoints [index] - controlPoints [index - 1]).normalized;
				Vector3 nextDir = (controlPoints [index + 1] - controlPoints [index]).normalized;
				dir = ((nextDir + prevDir) / 2.0f).normalized;
			}
			return new PathPoint (controlPoints [index], dir);
		}

		// Use this for initialization
		void Start ()
		{
		}
        
		// Update is called once per frame
		void Update ()
		{
		}

		public override int GetControlPointCount ()
		{
			return controlPoints != null ? controlPoints.Count : 0;
		}

		public override Vector3 GetControlPointAtIndex (int index)
		{
			return controlPoints [index];
		}

		public override void SetControlPointAtIndex (int index, Vector3 pt)
		{
			// Set near-zero components to zero
			for (int i = 0; i < 3; i++) {
				float v = pt [i];
				if (Mathf.Abs (v) < 0.00001f) {
					pt [i] = 0.0f;
				}
			}

			// Don't use Vector's == operator because it's also returning "true" for "near enough" vectors!
			if (pt.x != controlPoints [index].x || pt.y != controlPoints [index].y || pt.z != controlPoints [index].z) {
				controlPoints [index] = pt;
				PathPointsChanged ();

			}
		}

		public void AddControlPoint (Vector3 pt)
		{
			controlPoints.Add (pt);
			PathPointsChanged ();
		}

		public void InsertControlPoint (int index, Vector3 pt)
		{
			controlPoints.Insert (index, pt);
			PathPointsChanged ();
		}

		public void RemoveControlPointAtIndex (int index)
		{
			controlPoints.RemoveAt (index);
			PathPointsChanged ();
		}

		protected override DefaultPathModifierContainer CreatePathModifierContainer ()
		{
//          return new PathModifierContainer() {
//              public override bool IsSupportsApplyPathModifier() {
//                  return true;
//              }
//          };
			DefaultPathModifierContainer pmc = base.CreatePathModifierContainer (SetPathPoints);
			return pmc;
		}

		protected void SetPathPoints (PathPoint[] pathPoints)
		{
			controlPoints.Clear ();
			foreach (PathPoint pp in pathPoints) {
				controlPoints.Add (pp.Position);
			}
		}
	}
}

