// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

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
		bool IsLoop ();
		//      int GetPointCount();
	}
	

	public interface IPathData
	{
//		Path GetPath ();
		int GetId ();
		string GetName ();

		Color GetColor ();
		void SetColor (Color value);

		bool IsDrawGizmos ();
		void SetDrawGizmos (bool value);

		// TODO do we really need this?
		IPathInfo GetPathInfo ();

		IPathSnapshotManager GetPathSnapshotManager ();
		IPathModifierContainer GetPathModifierContainer ();

		// TODO should control point operations be in different interface?
		int GetControlPointCount ();
		Vector3 GetControlPointAtIndex (int index);
		void SetControlPointAtIndex (int index, Vector3 pt);

		PathPoint[] GetAllPoints ();
		int GetPointCount ();
		PathPoint GetPointAtIndex (int index);

		int GetOutputFlags ();
		int GetOutputFlagsBeforeModifiers ();
		float GetTotalDistance ();

		/// <summary>
		/// Determines whether data of this instance is up to date with the configuration.
		/// </summary>
		/// <returns><c>true</c> if this instance is up to date; otherwise, <c>false</c>.</returns>
		bool IsUpToDate ();


		/// <summary>
		/// Gets the status token that can be used later to determine if the path data has been
		/// modified since we got this status token.
		/// </summary>
		/// <returns>The status token.</returns>
		long GetStatusToken ();
		
	}


	public interface IAttachableToPath
	{
		void AttachToPath (Path path, PathChangedEventHandler dataChangedHandler);
		void DetachFromPath (Path path);
	}

	// TODO rename to AbstractPathData
	[Serializable]
	public abstract class AbstractPathData : IPathData, IAttachableToPath, ISerializationCallbackReceiver, IPathSnapshotManager
	{
		private Path _path;
		private PathChangedEventHandler pathChangedEventHandler;

		private DefaultPathModifierContainer pathModifierContainer = null;
		
		[SerializeField]
		private int
			id;
		
		[SerializeField]
		private string
			name;
		
		[SerializeField]
		private Color
			color = Color.cyan;

		[SerializeField]
		private bool
			drawGizmos = true;
		
//		[SerializeField]
//		private PathDataInputSource.SourceType
//			sourceType = PathDataInputSource.SourceType.Self;
//		
//		[SerializeField]
//		private PathDataInputSourceDataSet
//			dataSetSource = new PathDataInputSourceDataSet ();
//		
		
		// NOt serialized:
//		private PathDataInputSource _cachedInputSourceObj = null;
		
		[SerializeField]
		private List<PathPoint>
			pathPoints;

		[SerializeField]
		private bool
			pathPointsDirty = true;
		
		[SerializeField]
		private List<PathDataSnapshot>
			snapshots;
		
		[SerializeField]
		private int
			pathPointFlags = 0;
		
		[SerializeField]
		private int
			rawPathPointFlags = 0;
		
		[SerializeField]
		private ParameterStore
			parameterStore = new ParameterStore ();
		

		[SerializeField]
		private bool
			totalDistanceKnown = false;

		[SerializeField]
		private float
			totalDistance;

		// Don't serialize
		private long updateToken = 0;

		private IPathInfo pathInfo;

		protected bool addLastPointToLoop = true;

//		public AbstractPathData (int id) : this(null, id, "")
//		{
//		}
//		public AbstractPathData (Path path, int id) : this(path, id, "")
//		{
//		}
		protected AbstractPathData (int id, string name)
		{
			//SetPath (path);
			//this.pathInternals = pathInternals;
			this.id = id;
			this.name = name;

			updateToken = 0;// TODO use GenerateUpdateToken() to generate random
		}

		// TODO change "path" to regular field
		private Path path {
			set {
				this._path = value;
			}
			get {
				return this._path;

			}
		}
		// TODO move abstract methods HERE




		// TODO move this away from here!
		public class AlreadyAttachedToPathException : Exception
		{

		}
	

		private bool _inAttachToPath;
		public void AttachToPath (Path path, PathChangedEventHandler dataChangedHandler)
		{
			if (!_inAttachToPath) {
				if (null == path) {
					throw new ArgumentException ("AttachToPath requires a valid Path reference (got null)");
				} else if (this.path != null && this.path != path) {
					// Already attached to different path
					throw new AlreadyAttachedToPathException ();
				} else if (this.path != path) {
					this.path = path;


					_inAttachToPath = true;
					try {
						OnAttachToPath (path);
					} finally {
						_inAttachToPath = false;
					}
				}
				this.pathChangedEventHandler = dataChangedHandler;
			}
		}

		protected virtual void OnAttachToPath (Path path)
		{
		}

		private bool _inDetachFromPath;
		public void DetachFromPath (Path path)
		{
			if (!_inDetachFromPath) {
				if (null == path) {
					throw new ArgumentException ("DetachFromPath requires a valid Path reference (got null)");
				}

				if (this.path != null) {
					this.path = null;
					this.pathChangedEventHandler = null;

					_inDetachFromPath = true;
					try {
						OnDetachFromPath (path);
					} finally {
						_inDetachFromPath = false;
					}
				}
			}
		}
		protected virtual void OnDetachFromPath (Path path)
		{
		}

		protected void FireChangedEvent ()
		{
			if (null != pathChangedEventHandler) {
				Debug.Log ("Firing PathChangedEvent from PathData: " + GetName ());
				try {
					PathChangedEvent ev = new PathChangedEvent (path, this);
					pathChangedEventHandler (ev);
				} catch (Exception e) {
					Debug.LogError ("Catched an exception from event handler: " + e);
				}
			}
		}

		public bool IsUpToDate ()
		{
			return pathPointsDirty == false;
		}
		public long GetStatusToken ()
		{
			return this.updateToken;
		}

		public void OnBeforeSerialize ()
		{
			parameterStore.OnBeforeSerialize ();
			// TODO should we save pathmodifiers in here????
		}

		public void OnAfterDeserialize ()
		{
			parameterStore.OnAfterDeserialize ();
			
//			this._cachedInputSourceObj = null;
			
			//				 Materialize PathModifiers
			IPathModifierContainer pmc = GetPathModifierContainer ();
			if (pmc is DefaultPathModifierContainer) {
				((DefaultPathModifierContainer)pmc).LoadConfiguration ();
			}
		}

		public int GetId ()
		{
			return id;
		}
		
		public string GetName ()
		{
			return name;
		}
		public void SetName (string name)
		{
			this.name = name;
		}
		
		public Color GetColor ()
		{
			return color;
		}
		public void SetColor (Color color)
		{
			this.color = color;
			// TODO fire an event?
		}

		public bool IsDrawGizmos ()
		{
			return drawGizmos;
		}
		public void SetDrawGizmos (bool value)
		{
			this.drawGizmos = value;
		}

		// TODO move this on top of the class
		private class PathInfoImpl : IPathInfo
		{
			private AbstractPathData data;
			public PathInfoImpl (AbstractPathData data)
			{
				this.data = data;
			}
			
			public bool IsLoop ()
			{
				return data.IsLoop ();
			}
		}
		public IPathInfo GetPathInfo ()
		{
			if (null == pathInfo) {
				pathInfo = new PathInfoImpl (this);
			}
			return pathInfo;
		}

		public abstract bool IsLoop ();

		public abstract int GetControlPointCount ();
		public abstract Vector3 GetControlPointAtIndex (int index);
		public abstract void SetControlPointAtIndex (int index, Vector3 pt);
		public abstract void InsertControlPoint (int index, Vector3 pt);
		public abstract void RemoveControlPointAtIndex (int index);


		public IPathSnapshotManager GetPathSnapshotManager ()
		{
			return this;
		}
		
		public PathPoint[] GetAllPoints ()
		{
			this.UpdatePathPoints (false);
			// TODO we should return a deep clone instead?
			return pathPoints.ToArray ();
		}
		
		public int GetPointCount ()
		{
			this.UpdatePathPoints (false);
			return pathPoints.Count;
		}
		
		public PathPoint GetPointAtIndex (int index)
		{
			this.UpdatePathPoints (false);
			// TODO we should return a clone instead!
			return pathPoints [index];
		}
		
		public float GetTotalDistance ()
		{
			this.UpdatePathPoints (false);
			if (!totalDistanceKnown) {
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
				
				totalDistanceKnown = true;
				totalDistance = td;
			}
			return totalDistance;
		}
		
		public int GetOutputFlags ()
		{
			this.UpdatePathPoints (false);
			return pathPointFlags;
		}
		
		public int GetOutputFlagsBeforeModifiers ()
		{
			
			this.UpdatePathPoints (false);
			return rawPathPointFlags;
		}


		private bool _inPathPointsChanged;

		// TODO how to make reusable pattern of the recursion call guard?
		public void PathPointsChanged ()
		{
			if (!_inPathPointsChanged) {
				_inPathPointsChanged = true;
				try {
					pathPointsDirty = true;
					OnPathPointsChanged ();
					//FireChangedEvent ();
				} finally {
					_inPathPointsChanged = false;
				}
			} else {
				// TODO should we raise an exception?
				Debug.LogError ("Recursive call to PathPointsChanged detected; ignoring to prevent infinite loop");
			}
		}


		public virtual void OnPathPointsChanged ()
		{
			
		}
		
		public void ForceUpdatePathPoints ()
		{
			this.pathPointsDirty = true;
			// TODO notify the Path about dirty status!
			UpdatePathPoints (true);
		}

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

		// TODO move this to top of the class
		// TODO we should implement real thread-safe locking (lock)
		private bool _inUpdatePathPoints = false;
		private void UpdatePathPoints (bool manualRefresh)
		{
			bool doRefresh = path.FrozenStatus == Path.PathStatus.Dynamic
				|| (manualRefresh && path.FrozenStatus == Path.PathStatus.ManualRefresh);
			if (doRefresh && (null == this.pathPoints || this.pathPointsDirty)) {
				
				if (_inUpdatePathPoints) {
					// TODO log error and throw better exception!
					throw new Exception ("Recursive call to UpdatePathPoints detected. Circular path references?");
				}
				
				try {
					_inUpdatePathPoints = true;
					DoUpdatePathPoints ();

					FireChangedEvent ();
					// TODO should we fire an event here
				} finally {
					_inUpdatePathPoints = false;
				}
				
			} else if (this.pathPoints == null) {
				this.pathPoints = new List<PathPoint> ();
			}
			
			
		}
		private void DoUpdatePathPoints ()
		{
			this.totalDistanceKnown = false;
			
			this.totalDistance = 0.0f;
			if (null == snapshots) {
				this.snapshots = new List<PathDataSnapshot> ();
			} else {
				this.snapshots.Clear ();
			}
			
			if (null == pathPoints) {
				pathPoints = new List<PathPoint> ();
			} else {
				this.pathPoints.Clear ();
			}
			this.pathPointFlags = 0;
			
			
			int flags = 0;
			List<PathPoint> inputPoints = GetInputPoints (out flags);
			
			this.rawPathPointFlags = flags;
			
			// DoGetPathPoints doesn't return the last point for looped paths. So PathModifiers
			// will not have it on their input and it shouldn't be on their output!
			
			// TODO could we change PathModifier to get input as List (and also to return lists)
			IPathInfo pathInfo = GetPathInfo ();
			IPathModifierContainer pmc = GetPathModifierContainer ();
			PathPoint[] pp = inputPoints.ToArray ();
			
			pp = PathModifierUtil.RunPathModifiers (new PathModifierContext (
				pathInfo, pmc, flags), pp, ref flags, true);
			
			
			this.pathPointFlags = flags;
			this.pathPoints = new List<PathPoint> (pp);
			
			// Add last point for looped paths:
			// TODO add some documentation to DoGetPathPoints() and PathModifiers for looped paths
			// - DoGetPathPoints() should not return the last point (i.e. the duplicated first point)
			// - PathModifiers are fed without the last duplicated point and it's added after automatically
			//   after the last pathmodifier
			if (IsLoop () && addLastPointToLoop && this.pathPoints.Count > 1) {
				// Duplicate of the first point, except for distance components
				PathPoint lastPoint = new PathPoint (this.pathPoints [0]);
				bool setDistanceFromPrevious = PathPoint.IsFlag (flags, PathPoint.DISTANCE_FROM_PREVIOUS);
				bool setDistanceFromBegin = PathPoint.IsFlag (flags, PathPoint.DISTANCE_FROM_BEGIN);
				
				float distFromPrev;
				if (setDistanceFromPrevious || setDistanceFromBegin) {
					distFromPrev = (this.pathPoints [0].Position - this.pathPoints [this.pathPoints.Count - 1].Position).magnitude;
					if (setDistanceFromPrevious) {
						lastPoint.DistanceFromPrevious = distFromPrev;
					}
					if (setDistanceFromBegin) {
						lastPoint.DistanceFromBegin = this.pathPoints [this.pathPoints.Count - 1].DistanceFromBegin + distFromPrev;
					}
				}
				this.pathPoints.Add (lastPoint);
			}
			
			
			this.pathPointsDirty = false;
			
			// TODO this could be incremental, would it be better?
			this.updateToken = System.DateTime.Now.Millisecond;

		}

		// TODO should this be virtual method?
		protected List<PathPoint> GetInputPoints (out int flags)
		{
			return DoGetPathPoints (out flags);
		}

		public IPathModifierContainer GetPathModifierContainer ()
		{
			if (null == this.pathModifierContainer) {
				this.pathModifierContainer = CreatePathModifierContainer ();
			}
			return this.pathModifierContainer;
		}


		protected void ApplyPointsToControlPoints (PathPoint[] pathPoints)
		{
			int cpCount = GetControlPointCount ();
			int existingCount = Mathf.Min (cpCount, pathPoints.Length);
			for (int i = 0; i < existingCount; i++) {
				SetControlPointAtIndex (i, pathPoints [i].Position);
			}
			if (pathPoints.Length > existingCount) {
				// Add new points
				for (int i = existingCount; i < pathPoints.Length; i++) {
					InsertControlPoint (i, pathPoints [i].Position);
				}
			} else {
				// Remove extra points
				for (int i = cpCount - 1; i >= pathPoints.Length; i--) {
					RemoveControlPointAtIndex (i);
				}
			}
		}


		protected DefaultPathModifierContainer CreatePathModifierContainer ()
		{
			DefaultPathModifierContainer pmc = new DefaultPathModifierContainer (
				GetPathInfo,
				DoGetPathPointsArray,
				ApplyPointsToControlPoints,
				() => path, GetPathSnapshotManager, () => parameterStore);
			// TODO should parameterStore above be a child store with prefic "pathModifiers."?
			pmc.PathModifiersChanged += new PathModifiersChangedHandler (PathModifiersChanged);
			return pmc;
		}

		private void PathModifiersChanged (object sender, PathModifierContainerEvent e)
		{
			this.PathPointsChanged ();
		}
		
		public bool SupportsSnapshots ()
		{
			return true;
		}
		public void StoreSnapshot (string name, PathPoint[] points, int flags)
		{
			if (null == snapshots) {
				snapshots = new List<PathDataSnapshot> ();
			}
			// Find existing
			int insertAtIndex = snapshots.Count;
			for (int i = 0; i < snapshots.Count; i++) {
				PathDataSnapshot s = snapshots [i];
				if (s.Name == name) {
					// Found exising snapshot; update this
					// TODO should this be an error? Should we at least log the incident?
					insertAtIndex = i;
					break;
				}
			} 
			// Create a deep copy of points:
			PathPoint[] copiedPoints = new PathPoint[points.Length];
			for (int i = 0; i < points.Length; i++) {
				copiedPoints [i] = new PathPoint (points [i]);
			}
			PathDataSnapshot snapshot = new PathDataSnapshot (name, copiedPoints, flags);
			
			if (insertAtIndex < snapshots.Count) {
				// Insert in the middle
				snapshots [insertAtIndex] = snapshot;
			} else {
				// Add as new
				snapshots.Add (snapshot);
			}
		}
		
		public PathDataSnapshot GetSnapshot (string name)
		{
			// TODO should we maintain lookup dictionaries?
			
			PathDataSnapshot snapshot = null;
			foreach (PathDataSnapshot s in snapshots) {
				if (s.Name == name) {
					snapshot = s;
					break;
				}
			}
			// Return a deep clone:
			return (null != snapshot) ? new PathDataSnapshot (snapshot) : null;
		}
		public bool ContainsSnapshot (string name)
		{
			// TODO should we maintain lookup dictionaries?
			PathDataSnapshot snapshot = null;
			foreach (PathDataSnapshot s in snapshots) {
				if (s.Name == name) {
					snapshot = s;
					break;
				}
			}
			return null != snapshot;
		}
	}

}
