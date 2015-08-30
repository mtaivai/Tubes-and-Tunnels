using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

using Util;

// TODO we need a GetPoint(float t) method, either directly in a path
// or as a separate utility. For pure bezier paths it would be easy to implement,
// but for polyline or composite paths it woudl need some more effort by
// implementing a smart smoothing / interpolating algorithm. One possiblity would
// be to convert polyline path to bezier path on-the-fly.


namespace Paths
{
	public delegate void PathChangedEventHandler (object sender,EventArgs e);

	public class PathGizmoPrefs
	{
		public static readonly Color ControlPointConnectionLineColor = Color.gray;
		public static readonly Color ControlPointMarkerColor = Color.gray;
		public static readonly Color FinalPathLineColor = Color.cyan;
		public static readonly Color FinalPathPointMarkerColor = Color.cyan;
		public static readonly Color FinalPathFirstPointMarkerColor = Color.yellow;
		public static readonly Color UpVectorColor = new Color (0.1f, 1f, 0.1f);
		public static readonly Color DirVectorColor = new Color (0.1f, 0.1f, 1f);
		public static readonly Color RightVectorColor = new Color (1f, 0.1f, 0.1f);
		public static readonly float UpVectorLength = 1.0f;
		public static readonly float DirVectorLength = 1.0f;
		public static readonly float RightVectorLength = 1.0f;
		public static readonly float FinalPathPointMarkerSize = 0.1f;
		public static readonly float FinalPathFirstPointMarkerSize = 0.2f;
	}

	// TODO we don't really need the IPath interface since we're always referring to Path
	// (which is a GameObject)
//  public interface IPath
//  {
//      bool IsLoop ();
//
//  }

	// TODO do we really need separate IPathInfo? Maybe yes, because some path related
	// tools can be used without the actual Path object, for example PathModifiers don't
	// really need a reference to Path, the just need valid array of PathPoints. In some
	// cases we don't have the original Path instance available, for example Track has
	// its own set of PathModifiers.
	public interface IPathInfo
	{
		/// <summary>
		/// Determines whether this instance is loop path, i.e. the last point is connected
		/// to the first point. The returned last point has always the same position, direction,
		/// up vector etc as the first but its distance components are measured from the last
		/// actual point (or the last control point) to the first point.
		/// </summary>
		/// <returns><c>true</c> if this instance is loop path; otherwise, <c>false</c>.</returns>
		bool IsLoopPath ();
//      int GetPointCount();
	}

	public class PathInfo : IPathInfo
	{
		private Path path;

		public PathInfo (Path path)
		{
			this.path = path;
		}

		public bool IsLoopPath ()
		{
			return path.IsLoop ();
		}
	}

	public abstract class Path : MonoBehaviour, ISerializationCallbackReceiver, IReferenceContainer
	{

		public enum PathStatus
		{
			Dynamic,
			ManualRefresh,
			Frozen
		}



		public event PathChangedEventHandler Changed;

		[SerializeField]
		private ParameterStore
			parameterStore = new Util.ParameterStore ();

//      // don't serialize!
//      private List<IPathModifier> pathModifierInstances = new List<IPathModifier>();
		private DefaultPathModifierContainer pathModifierContainer = null;
		[SerializeField]
		private List<PathPoint>
			pathPoints;
		[SerializeField]
		private int
			pathPointFlags = 0;
		[SerializeField]
		private int
			rawPathPointFlags = 0;
		[SerializeField]
		private bool
			pathPointsDirty = true;
		[SerializeField]
		private PathStatus
			frozenStatus = PathStatus.Dynamic;
		[SerializeField]
		private List<UnityEngine.Object>
			referents = new List<UnityEngine.Object> ();

//        private PathEditorPrefs editorPrefs = PathEditorPrefs.Defaults;
//
//        public PathEditorPrefs EditorPrefs
//        {
//            get
//            {
//                return editorPrefs;
//            }
//        }

		private bool totalDistanceKnown = false;
		private float totalDistance;

		private IPathInfo pathInfo;

		private long updateToken = 0;

		public IPathInfo GetPathInfo ()
		{
			if (null == pathInfo) {
				pathInfo = new PathInfo (this);
			}
			return pathInfo;
		}

		// TODO how to clean up references???
		public int GetReferentCount ()
		{
			return referents.Count;
		}

		public UnityEngine.Object GetReferent (int index)
		{
			return referents [index];
		}

		public void SetReferent (int index, UnityEngine.Object obj)
		{
			referents [index] = obj;
		}

		public int AddReferent (UnityEngine.Object obj)
		{
			referents.Add (obj);
			return referents.Count - 1;
		}

		public void RemoveReferent (int index)
		{
			referents.RemoveAt (index);
		}

		public bool PointsDirty {
			get {
				return pathPointsDirty;
			}
		}

		public bool Frozen {
			get {
				return this.frozenStatus == PathStatus.Frozen;
			}
		}

		public PathStatus FrozenStatus {
			set {
				this.frozenStatus = value;
			}
			get {
				return this.frozenStatus;
			}
		}

		public ParameterStore EditorParameters {
			get {
				return new ParameterStore (this.parameterStore, "Editor.");
			}
		}

		private void FireChangedEvent ()
		{
			if (null != Changed) {
				Debug.Log ("FireChangedEvent: " + Changed.GetInvocationList ().Length);

				Changed (this, EventArgs.Empty);
			}
		}

		//void PathGeneratorModified(IPathGenerator pathGenerator);
		//IPathGenerator GetPathGenerator();

		// TODO this is already in IPathInfo! Do we need it in here?
		public abstract bool IsLoop ();
        
		//int GetSegmentCount();
		//int GetResolution();
        
//      public abstract PathPoint[] GetAllPoints();
//      public abstract int GetPointCount();
//      public abstract PathPoint GetPointAtIndex(int index);

		/// <summary>
		/// Called to get / produce path points. This will be called if the internal cache
		/// is invalidated, i.e. the path configuration has been changed.
		/// </summary>
		/// <returns>All path points, excluding the last point in looped paths (whose position is equal to position of first point)</returns>
		/// <param name="outputFlags">Output flags.</param>
		protected abstract List<PathPoint> DoGetPathPoints (out int outputFlags);

		protected PathPoint[] DoGetPathPointsArray (out int outputFlags)
		{
			return DoGetPathPoints (out outputFlags).ToArray ();
		}

		public void ForceUpdatePathPoints ()
		{
			this.pathPointsDirty = true;

			UpdatePathPoints (true);
		}

		public void UpdatePathPoints (bool manualRefresh)
		{
			bool doRefresh = frozenStatus == PathStatus.Dynamic || (manualRefresh && frozenStatus == PathStatus.ManualRefresh);
			if (doRefresh && (null == pathPoints || pathPointsDirty)) {

				this.totalDistanceKnown = false;

				int flags;


				List<PathPoint> rawPathPoints = DoGetPathPoints (out flags);
				this.rawPathPointFlags = flags;

				// DoGetPathPoints doesn't return the last point for looped paths. So PathModifiers
				// will not have it on their input and it shouldn't be on their output!

				// TODO could we change PathModifier to get input as List (and also to return lists)
				PathPoint[] pp = rawPathPoints.ToArray ();
				pp = PathModifierUtil.RunPathModifiers (new PathModifierContext (this, flags, new ParameterStore ()), 
                                                       pp, ref flags, true);


				this.pathPointFlags = flags;
				this.pathPoints = new List<PathPoint> (pp);

				// Add last point for looped paths:
				// TODO add some documentation to DoGetPathPoints() and PathModifiers for looped paths
				// - DoGetPathPoints() should not return the last point (i.e. the duplicated first point)
				// - PathModifiers are fed without the last duplicated point and it's added after automatically
				//   after the last pathmodifier
				if (IsLoop () && pathPoints.Count > 1) {
					// Duplicate of the first point, except for distance components
					PathPoint lastPoint = new PathPoint (pathPoints [0]);
					bool setDistanceFromPrevious = PathPoint.IsFlag (flags, PathPoint.DISTANCE_FROM_PREVIOUS);
					bool setDistanceFromBegin = PathPoint.IsFlag (flags, PathPoint.DISTANCE_FROM_BEGIN);
					
					float distFromPrev;
					if (setDistanceFromPrevious || setDistanceFromBegin) {
						distFromPrev = (pathPoints [0].Position - pathPoints [pathPoints.Count - 1].Position).magnitude;
						if (setDistanceFromPrevious) {
							lastPoint.DistanceFromPrevious = distFromPrev;
						}
						if (setDistanceFromBegin) {
							lastPoint.DistanceFromBegin = pathPoints [pathPoints.Count - 1].DistanceFromBegin + distFromPrev;
						}
					}
					pathPoints.Add (lastPoint);
				}


				pathPointsDirty = false;

				// TODO this could be incremental, would it be better?
				this.updateToken = System.DateTime.Now.Millisecond;

				FireChangedEvent ();

			} else if (pathPoints == null) {
				pathPoints = new List<PathPoint> ();
			}

		}
		public bool IsUpToDate (long statusToken)
		{
			return statusToken == this.updateToken;
		}
		public long GetStatusToken ()
		{
			return this.updateToken;
		}

		public PathPoint[] GetAllPoints ()
		{
			UpdatePathPoints (false);
			// TODO we should return a deep clone instead!
			return pathPoints.ToArray ();
		}
        
		public int GetPointCount ()
		{
			UpdatePathPoints (false);
			return pathPoints.Count;
		}
        
		public PathPoint GetPointAtIndex (int index)
		{
			UpdatePathPoints (false);
			// TODO we should return a clone instead!
			return pathPoints [index];
		}



		public float GetTotalDistance ()
		{
			UpdatePathPoints (false);
			if (!this.totalDistanceKnown) {
				float td = 0.0f;

				PathPoint[] points = GetAllPoints ();
				if (points.Length == 0) {
					td = 0.0f;
				} else {
					// TODO we should have a configuration option to force "always calculate total distance"

					// Do we have "DistanceFromBegin" flag?
					if (PathPoint.IsFlag (GetOutputFlags (), PathPoint.DISTANCE_FROM_BEGIN) && 
						points [points.Length - 1].HasDistanceFromBegin) {
						// Use the already calculated value:
						td = points [points.Length - 1].DistanceFromBegin;
					} else {
						// Calculate now
						for (int i = 1; i < points.Length; i++) {
							float d = (points [i].Position - points [i - 1].Position).magnitude;
							td += d;
						}
					}
				}

				this.totalDistanceKnown = true;
				this.totalDistance = td;
			}
			return this.totalDistance;
		}

		public int GetOutputFlags ()
		{
			UpdatePathPoints (false);
			return pathPointFlags;
		}

		public int GetOutputFlagsBeforeModifiers ()
		{
			UpdatePathPoints (false);
			return rawPathPointFlags;
		}

		public ParameterStore GetParameterStore ()
		{
			return parameterStore;
		}

		public void OnBeforeSerialize ()
		{
			parameterStore.OnBeforeSerialize ();
		}

		public void OnAfterDeserialize ()
		{
			parameterStore.OnAfterDeserialize ();

			// Materialize PathModifiers
			GetPathModifierContainer ().LoadPathModifiers (parameterStore);


		}

		public void PathPointsChanged ()
		{
			pathPointsDirty = true;
			OnPathPointsChanged ();
			FireChangedEvent ();
		}

		public void PathModifiersChanged ()
		{
			pathPointsDirty = true;
			OnPathModifiersChanged ();
			GetPathModifierContainer ().SavePathModifiers (parameterStore);


			FireChangedEvent ();

		}

		public virtual void OnPathModifiersChanged ()
		{

		}

		public virtual void OnPathPointsChanged ()
		{

		}

		protected virtual DefaultPathModifierContainer CreatePathModifierContainer ()
		{
			return CreatePathModifierContainer (null);
		}

		protected DefaultPathModifierContainer CreatePathModifierContainer (DefaultPathModifierContainer.SetPathPointsDelegate setPathPointsFunc)
		{
			DefaultPathModifierContainer pmc = new DefaultPathModifierContainer (
                GetPathInfo,
                PathModifiersChanged,
                PathPointsChanged,
                DoGetPathPointsArray,
                () => {
				this.pathPointsDirty = true;},
                setPathPointsFunc,
                 this);
       
			return pmc;
		}

		public DefaultPathModifierContainer GetPathModifierContainer ()
		{
			if (null == pathModifierContainer) {
				pathModifierContainer = CreatePathModifierContainer ();
			}
			return pathModifierContainer;
		}
       
		public abstract int GetControlPointCount ();

		public abstract Vector3 GetControlPointAtIndex (int index);

		public abstract void SetControlPointAtIndex (int index, Vector3 pt);

		void OnDrawGizmos ()
		{
//            Debug.Log("Path: OnDrawGizmos");
			// Draw the actual path:
			PathPoint[] pp = GetAllPoints ();
			//Vector3[] transformedPoints = new Vector3[pp.Length];


			// Draw the actual (final) path:
			Gizmos.color = PathGizmoPrefs.FinalPathLineColor;
			for (int i = 1; i < pp.Length; i++) {
				Vector3 pt0 = transform.TransformPoint (pp [i - 1].Position);
				Vector3 pt1 = transform.TransformPoint (pp [i].Position);
				Gizmos.DrawLine (pt0, pt1);
			}

			// GetAllPoints() already returns the last point for loop, which is 
			// equal to the first point
//			if (IsLoop () && pp.Length > 1) {
//				// Connect last and first points:
//				Vector3 pt0 = transform.TransformPoint (pp [0].Position);
//				Vector3 pt1 = transform.TransformPoint (pp [pp.Length - 1].Position);
//				Gizmos.DrawLine (pt0, pt1);
//			}

			// Direction Vectors (Forward, Right and Up) and point markers
			Color upVectorColor = PathGizmoPrefs.UpVectorColor;
			Color dirVectorColor = PathGizmoPrefs.DirVectorColor;
			Color rightVectorColor = PathGizmoPrefs.RightVectorColor;

			Color pointMarkerColor = PathGizmoPrefs.FinalPathPointMarkerColor;
			Color firstPointMarkerColor = PathGizmoPrefs.FinalPathFirstPointMarkerColor;

			float upVectorLength = PathGizmoPrefs.UpVectorLength;
			float dirVectorLength = PathGizmoPrefs.DirVectorLength;
			float rightVectorLength = PathGizmoPrefs.RightVectorLength;

			float pointMarkerSize = PathGizmoPrefs.FinalPathPointMarkerSize;
			float firstPointMarkerSize = PathGizmoPrefs.FinalPathFirstPointMarkerSize;

			// TODO transform directions etc!
			for (int i = 0; i < pp.Length; i++) {
				Vector3 pt = transform.TransformPoint (pp [i].Position);

				// Draw dir vector
				if (pp [i].HasDirection) {
					Gizmos.color = dirVectorColor;
					Gizmos.DrawLine (pt, pt + pp [i].Direction * dirVectorLength);
				}

				// Draw up vector
				if (pp [i].HasUp) {
					Gizmos.color = upVectorColor;
					Gizmos.DrawLine (pt, pt + pp [i].Up * upVectorLength);
				}

				// Draw ortho (right) vector
				if (pp [i].HasRight) { 
					Gizmos.color = rightVectorColor;
					Gizmos.DrawLine (pt, pt + pp [i].Right * rightVectorLength);
				}



				Gizmos.color = (i == 0) ? firstPointMarkerColor : pointMarkerColor;
				Gizmos.DrawSphere (pt, (i == 0) ? firstPointMarkerSize : pointMarkerSize);

			}
            
            
			//          // Draw handles
			//          
			//          for (int i = 0; i < pp.Length; i++) {
			//              float worldHandleSize = HandleUtility.GetHandleSize(transformedPoints[i]);
			//              float handleSize, pickSize;
			//              
			//              handleSize = Constants.controlPointHandleSize * worldHandleSize;
			//              pickSize = Constants.controlPointPickSize * worldHandleSize;
			//              
			//              Handles.Button(transformedPoints[i], transform.rotation, 
			//                             handleSize, pickSize, 
			//                             Handles.DotCap);
			//              
			//          }
		}
	}


}
