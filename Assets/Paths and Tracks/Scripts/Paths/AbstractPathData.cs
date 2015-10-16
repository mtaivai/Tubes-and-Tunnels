// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

using Util;

namespace Paths
{

	[Serializable]
	public abstract class AbstractPathData : IPathData, IAttachableToPath, ISerializationCallbackReceiver, IPathSnapshotManager
	{
		[NonSerialized]
		private Path
			_path;

		[NonSerialized]
		private PathChangedEventHandler
			pathChangedEventHandler;

		[NonSerialized]
		private DefaultPathModifierContainer
			pathModifierContainer = null;
		
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
		[NonSerialized]
		private long
			updateToken = 0;

		[NonSerialized]
		private IPathInfo
			pathInfo;

		[SerializeField]
		private DefaultPathMetadata
			pathMetadata;

		protected AbstractPathData (int id, string name)
		{
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

		protected bool ShouldAddLastPointToLoop ()
		{
			return true;
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

#region Events
//		============= EVENTS ==================================================
		protected void FireChangedEvent (bool controlPointsChanged, bool finalDataChanged, bool metadataChanged)
		{
			// Notify the metadata:
			// TODO why?
			if (null != pathMetadata) {
				try {
					if (controlPointsChanged) {
						pathMetadata.PathDataChanged (PathDataScope.ControlPoints);
					}
					if (finalDataChanged) {
						pathMetadata.PathDataChanged (PathDataScope.FinalData);
					}
				} catch (Exception ex) {
					Debug.LogError ("Catched an exception from pathMetadata.PathDataChanged handler: " + ex);
				}
			}
			if (null != pathChangedEventHandler) {
				if (metadataChanged) {
					try {
						PathChangedEvent ev = new PathChangedEvent (PathChangedEvent.EventReason.MetadataChanged, path, this);
						pathChangedEventHandler (this, ev);
					} catch (Exception e) {
						Debug.LogError ("Catched an exception from event handler: " + e);
					}
				}
				if (finalDataChanged) {
					try {
						PathChangedEvent ev = new PathChangedEvent (PathChangedEvent.EventReason.DataChanged, path, this);
						//					Debug.Log ("Firing PathChangedEvent: " + ev);
						pathChangedEventHandler (this, ev);
					} catch (Exception e) {
						Debug.LogError ("Catched an exception from event handler: " + e);
					}
				}
			}
		}

		private bool _inPathPointsChanged;
		// TODO how to make reusable pattern of the recursion call guard?
		// TODO rename this to "MarkDataDirty" or similar
		public void PathPointsChanged (bool controlPointsChanged)
		{
			if (!_inPathPointsChanged) {
				_inPathPointsChanged = true;
				try {
//					MarkDataDirty (false);
					this.pathPointsDirty = true;
					OnPathPointsChanged ();
					// FIRE THE EVENT HERE!
					FireChangedEvent (controlPointsChanged, true, false);
				} finally {
					_inPathPointsChanged = false;
				}
			} else {
				// TODO should we raise an exception?
				Debug.LogError ("Recursive call to PathPointsChanged detected; ignoring to prevent infinite loop");
			}
		}
		
		// Custom behaviour, called after the current data is set dirty and before any events are fired
		protected virtual void OnPathPointsChanged ()
		{
			
		}


		private void PathMetadataChanged (object sender, PathMetadataChangedEventArgs e)
		{
			FireChangedEvent (false, false, true);
		}
//		
//		public void MarkDataDirty (bool forceUpdateNow = false)
//		{
//			this.pathPointsDirty = true;
//			if (forceUpdateNow) {
//				UpdatePathPoints (true);
//			}
//		}

		// Receives PathModifierContainerEvent from our PathModifierContainer instance
		private void PathModifiersChanged (PathModifierContainerEvent e)
		{
			this.PathPointsChanged (false);
		}

#endregion Events

		public bool IsUpToDate ()
		{
			return pathPointsDirty == false;
		}
		public long GetStatusToken ()
		{
			return this.updateToken;
		}

#region Serialization
//		============= SERIALIZATION ==================================================

		public void OnBeforeSerialize ()
		{
//			parameterStore.OnBeforeSerialize ();
			// TODO should we save pathmodifiers in here????
			HandleOnBeforeSerialize ();
		}
		protected virtual void HandleOnBeforeSerialize ()
		{

		}
		public void OnAfterDeserialize ()
		{
			if (null != pathMetadata) {
				pathMetadata.MetadataChanged -= PathMetadataChanged;
				pathMetadata.MetadataChanged += PathMetadataChanged;

			}

//			parameterStore.OnAfterDeserialize ();
			
//			this._cachedInputSourceObj = null;
			
			//				 Materialize PathModifiers
//			IPathModifierContainer pmc = GetPathModifierContainer ();
//			if (pmc is DefaultPathModifierContainer) {
//				((DefaultPathModifierContainer)pmc).LoadConfiguration ();
//			}
			HandleOnAfterDeserialize ();
		}
		protected virtual void HandleOnAfterDeserialize ()
		{

		}
#endregion Serialization
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
		public abstract PathPoint GetControlPointAtIndex (int index);
		public abstract void SetControlPointAtIndex (int index, PathPoint pt);
		public abstract void InsertControlPoint (int index, PathPoint pt);
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

		/// <summary>
		/// Called to get / produce path points. This will be called if the internal cache
		/// is invalidated, i.e. the path configuration has been changed.
		/// </summary>
		/// <returns>All path points, excluding the last point in looped paths (whose position is equal to position of first point)</returns>
		/// <param name="outputFlags">Output flags.</param>
		protected abstract List<PathPoint> DoGetInputPoints (out int outputFlags);
		
		protected PathPoint[] DoGetInputPointsArray (out int outputFlags)
		{
			return DoGetInputPoints (out outputFlags).ToArray ();
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
					string msg = string.Format ("Recursive call to UpdatePathPoints in {0} detected. Circular path references?", this);
					Debug.LogError (msg, path);
					throw new CircularPathReferenceException (msg);
				}
				
				try {
					_inUpdatePathPoints = true;
					DoUpdatePathPoints ();

					//FireChangedEvent ();
					// TODO should we fire an event here
				} finally {
					_inUpdatePathPoints = false;
				}
				
			} else if (this.pathPoints == null) {
				this.pathPoints = new List<PathPoint> ();
			}
			if (manualRefresh) {
				// TODO have our control points changed?
				FireChangedEvent (true, true, false);
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
			 
			IPathMetadata md = IsPathMetadataSupported () ? GetPathMetadata () : UnsupportedPathMetadata.Instance;

			PathModifierContext pmContext = new PathModifierContext (pathInfo, pmc, md, flags);
			//pp = PathModifierUtil.RunPathModifiers (pmContext, pp, false, ref flags, true);
			pp = pmc.RunPathModifiers (pmContext, pp, ref flags);
			if (pmContext.HasErrors) {
				string allErrors = "";
				pmContext.Errors.ForEach ((err) => allErrors += (allErrors.Length > 0 ? "; " + err : err));
				Debug.LogErrorFormat ("Errors occurred while running PathModifiers: {0}", allErrors);
			}
			
			this.pathPointFlags = flags;
			this.pathPoints = new List<PathPoint> (pp);

			if (IsEditablePathMetadataSupported ()) { // TODO this is quite slow op, please consider some alternative way!
				// Collect parameters (weights) and add to metadata if not already added
				HashSet<string> allWeightIds = new HashSet<string> ();
				for (int i = 0; i < pp.Length; i++) {
					PathPoint p = pp [i];
					foreach (string id in p.GetWeightIds()) {
						allWeightIds.Add (id);
					}
				}
				IEditablePathMetadata emd = (IEditablePathMetadata)md;
				int wdCount = emd.GetWeightDefinitionCount ();
				for (int i = 0; i < wdCount; i++) {
					allWeightIds.Remove (emd.GetWeightDefinitionAtIndex (i).WeightId);
				}
				// Add remaining:
				foreach (string id in allWeightIds) {
					emd.AddWeightDefinition (id);
				}
			}
			
			// Add last point for looped paths:
			// TODO add some documentation to DoGetPathPoints() and PathModifiers for looped paths
			// - DoGetPathPoints() should not return the last point (i.e. the duplicated first point)
			// - PathModifiers are fed without the last duplicated point and it's added after automatically
			//   after the last pathmodifier
			if (IsLoop () && this.pathPoints.Count > 1 && ShouldAddLastPointToLoop ()) {
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
			return DoGetInputPoints (out flags);
		}

		public IPathModifierContainer GetPathModifierContainer ()
		{
			if (null == this.pathModifierContainer) {
				this.pathModifierContainer = CreatePathModifierContainer (true);
			}
			return this.pathModifierContainer;
		}

		protected void ApplyPointsToControlPoints (PathPoint[] pathPoints)
		{
			int cpCount = GetControlPointCount ();
			int existingCount = Mathf.Min (cpCount, pathPoints.Length);
			for (int i = 0; i < existingCount; i++) {
				SetControlPointAtIndex (i, pathPoints [i]);
			}
			if (pathPoints.Length > existingCount) {
				// Add new points
				for (int i = existingCount; i < pathPoints.Length; i++) {
					InsertControlPoint (i, pathPoints [i]);
				}
			} else {
				// Remove extra points
				for (int i = cpCount - 1; i >= pathPoints.Length; i--) {
					RemoveControlPointAtIndex (i);
				}
			}
		}

		protected DefaultPathModifierContainer CreatePathModifierContainer (bool loadConfiguration)
		{
			DefaultPathModifierContainer pmc = new DefaultPathModifierContainer (
				GetPathInfo,
				GetPathMetadata,
				DoGetInputPointsArray,
				ApplyPointsToControlPoints,
				path.GetReferenceContainer, 
				GetPathSnapshotManager, 
				() => parameterStore);

			if (loadConfiguration) {
				pmc.LoadConfiguration ();
			}

			// TODO should parameterStore above be a child store with prefic "pathModifiers."?
			pmc.PathModifiersChanged += new PathModifiersChangedHandler (PathModifiersChanged);
			return pmc;
		}

		// TODO Refactor IPathSnapshotManager support to a composite class
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
		public int GetSnapshotPointFlags (string name)
		{
			int flags = 0;
			foreach (PathDataSnapshot s in snapshots) {
				if (s.Name == name) {
					flags = s.Flags;
					break;
				}
			}
			return flags;
		}
		public PathPoint[] GetSnapshotPoints (string name)
		{
			PathDataSnapshot ss = GetSnapshot (name);
			return (null != ss) ? ss.Points : null;
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

		public string[] GetAvailableSnapshotNames ()
		{
			// TODO what if pathmodifiers haven't ran?
			// TODO we should have persistent snapshots (that are persisted even after pm's are applied)
			List<string> names = new List<string> ();
			snapshots.ForEach ((ss) => names.Add (ss.Name));
			return names.ToArray ();
		}

		public bool IsPathMetadataSupported ()
		{
			return true;
		}
		public bool IsEditablePathMetadataSupported ()
		{
			return true;
		}

		public IPathMetadata GetPathMetadata ()
		{
			if (null == pathMetadata) {
				pathMetadata = new DefaultPathMetadata ();
				pathMetadata.MetadataChanged += PathMetadataChanged;
			}
			return pathMetadata;
		}

	}

}
