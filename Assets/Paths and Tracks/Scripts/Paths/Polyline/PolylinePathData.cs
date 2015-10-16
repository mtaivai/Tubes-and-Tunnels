// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Paths;

namespace Paths.Polyline
{
	[System.Serializable]
	public class PolylinePathData : AbstractPathData
	{
//		[SerializeField]
//		[UnityEngine.Serialization.FormerlySerializedAs("controlPoints")]
//		private List<Vector3>
//			_controlPoints = new List<Vector3> ();

		// TODO rename to controlPoints after everything is migrated!
		[SerializeField]
		//		[UnityEngine.Serialization.FormerlySerializedAs("controlPathPoints")]
		private List<PathPoint>
			controlPoints = new List<PathPoint> ();

		[SerializeField]
		private bool
			loop = false;
		
		public PolylinePathData (int id) : base(id, "")
		{
		}
		protected override void HandleOnBeforeSerialize ()
		{
		}
		
		protected override void HandleOnAfterDeserialize ()
		{
			// Don't serialize controlPoints (migrate to pathpoints)
//			if (null != _controlPoints && _controlPoints.Count > 0) {
//				// Migrate to PathPoints
//				controlPathPoints = new List<PathPoint> ();
//				foreach (Vector3 p in _controlPoints) {
//					controlPathPoints.Add (new PathPoint (p));
//				}
//				_controlPoints = null;
//			}
		}

		public override bool IsLoop ()
		{
			return loop;
		}
		
		public void SetLoop (bool value)
		{
			if (value != this.loop) {
				this.loop = value;
				this.PathPointsChanged (true);
			}
		}
		
		//			public PathDataImpl (Path path, int id, string name)
		//			{
		//				SetPath (path);
		//				this.id = id;
		//				this.name = name;
		//			}
		
		protected override List<PathPoint> DoGetInputPoints (out int outputFlags)
		{
			int cpCount = (null != controlPoints) ? controlPoints.Count : 0;
			List<PathPoint> pp = new List<PathPoint> ();
			for (int i = 0; i < cpCount; i++) {
				pp.Add (DoGetInputPointAtIndex (i));
			}
			// TODO currently we don't generate distances!
			outputFlags = PathPoint.POSITION | PathPoint.DIRECTION;
			return pp;
		}
		
		protected PathPoint DoGetInputPointAtIndex (int index)
		{
			PathPoint pp = controlPoints [index];
			if (!pp.HasDirection) {
				int lastIndex = controlPoints.Count - 1;
				Vector3 dir;
				if (index == 0) {
					// First point; direction is from this to next
					if (index == lastIndex) {
						//...but this is the only point so we use "forward" as direction
						dir = Vector3.forward;
					} else {
						dir = (controlPoints [index + 1].Position - controlPoints [index].Position).normalized;
					}
				} else if (index == lastIndex) {
					// Last point; direction is from previous to this
					dir = (controlPoints [index].Position - controlPoints [index - 1].Position).normalized;
				} else {
					// Average direction from previous to next
					Vector3 prevDir = (controlPoints [index].Position - controlPoints [index - 1].Position).normalized;
					Vector3 nextDir = (controlPoints [index + 1].Position - controlPoints [index].Position).normalized;
					dir = ((nextDir + prevDir) / 2.0f).normalized;
				}
				pp.Direction = dir;
			}
			return new PathPoint (pp);
		}
		
		public override int GetControlPointCount ()
		{
			return controlPoints != null ? controlPoints.Count : 0;
		}
		
		public override PathPoint GetControlPointAtIndex (int index)
		{
			return new PathPoint (controlPoints [index], PathPoint.POSITION);
		}
		
		public override void SetControlPointAtIndex (int index, PathPoint pt)
		{
			// TODO currently we remove all components but POSITION
			PathPoint pp = new PathPoint (pt, PathPoint.POSITION);

			// Set near-zero components to zero
			Vector3 pos = pp.Position;
			for (int i = 0; i < 3; i++) {
				if (Mathf.Abs (pos [i]) < 0.00001f) {
					pos [i] = 0.0f;
				}
			}
			pp.Position = pos;

			// TODO reimplment following?
			// Don't use Vector's == operator because it's also returning "true" for "near enough" vectors!
//			if (pt.x != controlPoints [index].x || pt.y != controlPoints [index].y || pt.z != controlPoints [index].z) {
//				controlPoints [index] = pt;
			// TODO fire an event!
//				PathPointsChanged ();
//			}
			if (!pp.Equals (controlPoints [index])) {
				controlPoints [index] = pp;
				PathPointsChanged (true);
			}
		}
		
		public void AddControlPoint (PathPoint pt)
		{
			InsertControlPoint (controlPoints.Count, pt);
		}
		
		public override void InsertControlPoint (int index, PathPoint pt)
		{
			controlPoints.Insert (index, new PathPoint (pt));
			PathPointsChanged (true);
		}
		
		public override void RemoveControlPointAtIndex (int index)
		{
			controlPoints.RemoveAt (index);
			PathPointsChanged (true);
		}
		//			public override void RemoveAllControlPoints() {
		//				controlPoints.Clear();
		//			}
		
		//		protected override DefaultPathModifierContainer CreatePathModifierContainer ()
		//		{
		////          return new PathModifierContainer() {
		////              public override bool IsSupportsApplyPathModifier() {
		////                  return true;
		////              }
		////          };
		//			DefaultPathModifierContainer pmc = base.CreatePathModifierContainer (SetPathPoints);
		//			return pmc;
		//		}
		
		//			
		//			protected override void SetControlPoints (PathPoint[] pathPoints)
		//			{
		//				controlPoints.Clear ();
		//				foreach (PathPoint pp in pathPoints) {
		//					controlPoints.Add (pp.Position);
		//				}
		//			}
	}
	
}
