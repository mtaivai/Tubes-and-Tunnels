using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Paths;

namespace Paths.Polyline
{

	[ExecuteInEditMode]
	public class PolylinePath : Path
	{
		
		
		
		[SerializeField]
		private List<PolylinePathData>
			dataSets = new List<PolylinePathData> ();

		public PolylinePath ()
		{

		}
		public override int GetDataSetCount ()
		{
			return dataSets.Count;
		}
		public override IPathData GetDataSetAtIndex (int index)
		{
			return dataSets [index];
		}
		protected override void DoInsertDataSet (int index, IPathData data)
		{
			dataSets.Insert (index, (PolylinePathData)data);
		}
		protected override void DoRemoveDataSet (int index)
		{
			dataSets.RemoveAt (index);
		}

		public override bool IsSetDataSetIndexSupported ()
		{
			return true;
		}
	
		public override void SetDataSetIndex (IPathData data, int newIndex)
		{
			if (newIndex < 0 || newIndex >= dataSets.Count) {
				throw new System.ArgumentOutOfRangeException ("newIndex");
			}
			int currentIndex = IndexOfDataSet (data);
			
			if (newIndex != currentIndex) {
				PolylinePathData swapWith = dataSets [newIndex];
				dataSets [newIndex] = (PolylinePathData)data;
				dataSets [currentIndex] = swapWith;
			}
		}


		protected override IPathData CreatePathData (int id)
		{
			return new PolylinePathData (id);
		}
//		protected override void OnAttachPathData (IPathData data)
//		{
//
//		}


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

		// Use this for initialization
		void Start ()
		{
		}
        
		// Update is called once per frame
		void Update ()
		{
		}

//		void OnSceneGUI ()
//		{
//			super.OnSceneGUI();
//			
//		}


	}
}

