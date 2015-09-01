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

	public abstract class PathDataInputSource
	{
		public enum SourceType
		{
			None,
			Path,
			DataSet,
		}
		private SourceType type;
		protected PathDataInputSource (SourceType type)
		{
			this.type = type;
		}
		public SourceType GetSourceType ()
		{
			return type;
		}
	}
	public class PathDataInputSourceNone : PathDataInputSource
	{
		public static readonly PathDataInputSourceNone Instance = new PathDataInputSourceNone ();

		public PathDataInputSourceNone () : base(SourceType.None)
		{
		}
	}
	public class PathDataInputSourcePath : PathDataInputSource
	{
		public static readonly PathDataInputSourcePath Instance = new PathDataInputSourcePath ();

		public PathDataInputSourcePath () : base(SourceType.Path)
		{
		}
	}
	public class PathDataInputSourceDataSet : PathDataInputSource
	{
		private int dataSetId;
		private bool fromSnapshot;
		private string snapshotName;
	
		public PathDataInputSourceDataSet (int dataSetId, bool fromSnapshot, string snapshotName) : base(SourceType.DataSet)
		{
			this.dataSetId = dataSetId;
			this.fromSnapshot = fromSnapshot;
			this.snapshotName = snapshotName;
		}
	
		public int GetDataSetId ()
		{
			return dataSetId;
		}
		public bool IsFromSnapshot ()
		{
			return fromSnapshot;
		}
		public string GetSnapshotName ()
		{
			return snapshotName;
		}


	}


	// TODO should we rename this to IPathData (MS convention)
	public interface PathData
	{
		Path GetPath ();
		int GetId ();
		string GetName ();

		Color GetColor ();

		PathDataInputSource GetInputSource ();
		IPathSnapshotManager GetPathSnapshotManager ();
		IPathModifierContainer GetPathModifierContainer ();

		PathPoint[] GetAllPoints ();
		int GetPointCount ();
		PathPoint GetPointAtIndex (int index);

		int GetOutputFlags ();
		int GetOutputFlagsBeforeModifiers ();
		float GetTotalDistance ();

	}

	public abstract class Path : MonoBehaviour, ISerializationCallbackReceiver, IReferenceContainer
	{
		public event PathChangedEventHandler Changed;

		public enum PathStatus
		{
			Dynamic,
			ManualRefresh,
			Frozen
		}



		// TODO move this to its own file, or at least parts of it!
		[Serializable]
		private class PathDataImpl : PathData, ISerializationCallbackReceiver, IPathSnapshotManager
		{
			private Path path;

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
			private PathDataInputSource.SourceType
				sourceType = PathDataInputSource.SourceType.Path;

			[SerializeField]
			private int
				sourceDataSetId = -1;

			[SerializeField]
			private bool
				sourceFromSnapshot = false;

			[SerializeField]
			private string
				sourceSnapshotName = "";

			// NOt serialized:
			private PathDataInputSource _cachedInputSourceObj = null;

			[SerializeField]
			private List<PathPoint>
				pathPoints;

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

			// TODO add following here:
//			[SerializeField]
//			private bool
//				pathPointsDirty = true;


			public bool totalDistanceKnown = false;
			public float totalDistance;
			public PathDataImpl (int id) : this(null, id, "")
			{
			}
			public PathDataImpl (Path path, int id) : this(path, id, "")
			{
			}
			public PathDataImpl (Path path, int id, string name)
			{
				SetPath (path);
				this.id = id;
				this.name = name;
			}

			public void OnBeforeSerialize ()
			{
				parameterStore.OnBeforeSerialize ();
				// TODO should we save pathmodifiers in here????
			}
			public void OnAfterDeserialize ()
			{
				parameterStore.OnAfterDeserialize ();

				this._cachedInputSourceObj = null;

//				 Materialize PathModifiers
				IPathModifierContainer pmc = GetPathModifierContainer ();
				if (pmc is DefaultPathModifierContainer) {
					((DefaultPathModifierContainer)pmc).LoadPathModifiers (parameterStore);
				}
			}

			public void PathModifiersChanged ()
			{
//				pathPointsDirty = true;
//				OnPathModifiersChanged ();
				
				IPathModifierContainer pmc = GetPathModifierContainer ();
				if (pmc is DefaultPathModifierContainer) {
					((DefaultPathModifierContainer)pmc).SavePathModifiers (parameterStore);
				}
			}

			public Path GetPath ()
			{
				return path;
			}
			public void SetPath (Path path)
			{
				this.path = path;
//				if (null != path) {
//					this.parameterStore = new ParameterStore(path.parameterStore, null);
//				}
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
			}


			public PathDataInputSource GetInputSource ()
			{
				if (null == _cachedInputSourceObj) {
					switch (sourceType) {
					case PathDataInputSource.SourceType.None:
						_cachedInputSourceObj = PathDataInputSourceNone.Instance;
						break;
					case PathDataInputSource.SourceType.Path:
						_cachedInputSourceObj = PathDataInputSourcePath.Instance;
						break;
					case PathDataInputSource.SourceType.DataSet:
						_cachedInputSourceObj = new PathDataInputSourceDataSet (sourceDataSetId, sourceFromSnapshot, sourceSnapshotName);
						break;
					default:
						throw new ArgumentException ("Invalid internal source type: " + sourceType);
					}
				}
				return _cachedInputSourceObj;
			}

			public void SetInputSourceType (PathDataInputSource.SourceType type)
			{
				this._cachedInputSourceObj = null;
				this.sourceType = type;
			}
			public void SetInputSource (PathDataInputSource inputSource)
			{
				this._cachedInputSourceObj = null;
				this.sourceType = inputSource.GetSourceType ();
				if (inputSource is PathDataInputSourceDataSet) {
					PathDataInputSourceDataSet dsSource = (PathDataInputSourceDataSet)inputSource;

					this.sourceDataSetId = dsSource.GetDataSetId ();
					this.sourceFromSnapshot = dsSource.IsFromSnapshot ();
					this.sourceSnapshotName = dsSource.GetSnapshotName ();
				}
			}

			public IPathSnapshotManager GetPathSnapshotManager ()
			{
				return this;
			}

			public PathPoint[] GetAllPoints ()
			{
				this.UpdatePathPoints (false);
				// TODO we should return a deep clone instead!
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

			public void ForceUpdatePathPoints ()
			{
				path.pathPointsDirty = true;
				
				UpdatePathPoints (true);
			}

			private bool _inUpdatePathPoints = false;
			public void UpdatePathPoints (bool manualRefresh)
			{
				bool doRefresh = path.frozenStatus == PathStatus.Dynamic || (manualRefresh && path.frozenStatus == PathStatus.ManualRefresh);
				if (doRefresh && (null == this.pathPoints || path.pathPointsDirty)) {

					if (_inUpdatePathPoints) {
						// TODO log error and throw better exception!
						throw new Exception ("Recursive call to UpdatePathPoints detected. Circular path references?");
					}

					try {
						_inUpdatePathPoints = true;
						DoUpdatePathPoints ();
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
				IPathInfo pathInfo = path.GetPathInfo ();
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
				if (path.IsLoop () && path.addLastPointToLoop && this.pathPoints.Count > 1) {
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
				
				
				path.pathPointsDirty = false;
				
				// TODO this could be incremental, would it be better?
				path.updateToken = System.DateTime.Now.Millisecond;
				
				path.FireChangedEvent ();
			}

			List<PathPoint> GetInputPoints (out int flags)
			{
				PathDataInputSource inputSource = GetInputSource ();
				PathDataInputSource.SourceType sourceType = inputSource.GetSourceType ();
				
				List<PathPoint> inputPoints;
				switch (sourceType) {
				case PathDataInputSource.SourceType.None:
					inputPoints = new List<PathPoint> ();
					flags = 0;
					break;
				case PathDataInputSource.SourceType.Path:
					inputPoints = path.DoGetPathPoints (out flags);
					break;
				case PathDataInputSource.SourceType.DataSet:
					inputPoints = GetInputPointsFromDataSet (out flags);
					break;
				default:
					Debug.LogError ("Unsupported / unknown path input source type: " + sourceType);
					inputPoints = new List<PathPoint> ();
					flags = 0;
					// TODO ADD ERROR
					break;
				}
				return inputPoints;
			}

			List<PathPoint> GetInputPointsFromDataSet (out int flags)
			{
				List<PathPoint> inputPoints = new List<PathPoint> ();
				flags = 0;
				
				PathDataInputSourceDataSet dataSetSource = (PathDataInputSourceDataSet)GetInputSource ();
				int dsId = dataSetSource.GetDataSetId ();
				PathData sourceDataSet = path.FindDataSetById (dsId);
				if (null == sourceDataSet) {
					// TODO ADD ERROR
					Debug.LogWarning ("Source PathData with id " + dsId + " not found");
				} else if (this == sourceDataSet) {
					// TODO ADD ERROR
					Debug.LogWarning ("Source PathData refers to itself; id = " + dsId);
				} else {
					bool fromSnapshot = dataSetSource.IsFromSnapshot ();
					if (fromSnapshot) {
						string snapshotName = dataSetSource.GetSnapshotName ();
						
						// Get snapshot
						IPathSnapshotManager snapshotManager = sourceDataSet.GetPathSnapshotManager ();
						if (!snapshotManager.SupportsSnapshots ()) {
							// TODO add error!
							Debug.LogWarning ("Source dataset does not support snapshots: " + sourceDataSet.GetName ());
						} else if (!snapshotManager.ContainsSnapshot (snapshotName)) {
							// TODO add error
							Debug.LogWarning ("No such snapshot found in source dataset '" + sourceDataSet.GetName () + "': '" + snapshotName + "'");
						} else {
							PathDataSnapshot snapshot = snapshotManager.GetSnapshot (snapshotName);
							inputPoints.AddRange (snapshot.Points);
							flags = snapshot.Flags;
						}
						
					} else {
						inputPoints.AddRange (sourceDataSet.GetAllPoints ());
						flags = sourceDataSet.GetOutputFlags ();
					}
				}

				return inputPoints;
			}

			
			public IPathModifierContainer GetPathModifierContainer ()
			{
				if (null == this.pathModifierContainer) {
					this.pathModifierContainer = path.CreatePathModifierContainer ();
					this.pathModifierContainer.SetPathBranchManager (GetPathSnapshotManager ());
				}
				return this.pathModifierContainer;
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

		private static readonly Color[] DefaultDataSetColors = new Color[]{
			Color.cyan,
			Color.yellow,
			Color.white,
			Color.magenta,
			Color.grey,
		};



		[SerializeField]
		private ParameterStore
			parameterStore = new Util.ParameterStore ();



		[SerializeField]
		private bool
			pathPointsDirty = true;

		[SerializeField]
		private PathStatus
			frozenStatus = PathStatus.Dynamic;

		[SerializeField]
		private List<UnityEngine.Object>
			referents = new List<UnityEngine.Object> ();

		[SerializeField]
		private List<PathDataImpl>
			dataSets = new List<PathDataImpl> ();

		[SerializeField]
		private int
			defaultDataSetId;

		[SerializeField]
		private int
			nextDataSetId = 1;

		[SerializeField]
		private int
			nextDataSetColorIndex = 0;

		private IPathInfo pathInfo;

		private long updateToken = 0;

		protected bool addLastPointToLoop = true;


		public Path ()
		{
			// TODO add Reset() method!
			PathData defaultData = AddDataSet ();
			SetDataSetName (defaultData, "Default");

			nextDataSetColorIndex = 0;

			updateToken = 0;// TODO use GenerateUpdateToken() to generate random
			frozenStatus = PathStatus.Dynamic;
			pathPointsDirty = true;
		}

		private int NextDataSetId ()
		{
			return nextDataSetId++;
		}
		private Color NextDataSetColor ()
		{
			if (nextDataSetColorIndex >= DefaultDataSetColors.Length) {
				nextDataSetColorIndex = 0;
			}
			return DefaultDataSetColors [nextDataSetColorIndex++];

		}



		public IPathInfo GetPathInfo ()
		{
			if (null == pathInfo) {
				pathInfo = new PathInfo (this);
			}
			return pathInfo;
		}

		public int GetDefaultDataSetId ()
		{
			//return Mathf.Clamp (this.defaultDataSetIndex, -1, dataSets.Count - 1);
			return this.defaultDataSetId;
		}
		public void SetDefaultDataSetId (int id)
		{
			PathData d = FindDataSetById (id);
			if (null != d) {
				this.defaultDataSetId = d.GetId ();
			}
		}
		public bool IsDefaultDataSet (PathData data)
		{
			return null != data && data.GetId () == GetDefaultDataSetId ();
//			int i = dataSets.IndexOf ((PathDataImpl)data);
//			return i == GetDefaultDataSetIndex ();
		}
//		private PathData FindInputForDataSet (string targetDataSetName)
//		{
//			string branchName = "BranchTo." + targetDataSetName;
//			for (int i = 0; i < dataSets.Count; i++) {
//				PathDataImpl data = dataSets [i];
//				if (data.GetName () == targetDataSetName) {
//					// Skip itself:
//					continue;
//				}
//				if (data.ContainsSnapshot (branchName)) {
//					// Found
//					return data;
//				}
//			}
//			return null;
//		}
		public int GetDataSetCount ()
		{
			return dataSets.Count;
		}

		public PathData GetDefaultDataSet ()
		{
			return FindDataSetById (GetDefaultDataSetId ());
		}
		public PathData GetDataSetAtIndex (int dataSetIndex)
		{
			return dataSets [dataSetIndex];
		}
		public int IndexOfDataSet (PathData data)
		{
			int index = -1;
			int idToFind = data.GetId ();
			for (int i = 0; i < dataSets.Count; i++) {
				if (dataSets [i].GetId () == idToFind) {
					index = i;
					break;
				}
			}
			return index;
		}
		public PathData FindDataSetById (int id)
		{
			// TODO add lookup dictionary (we also need to listen to name changes!)
			foreach (PathDataImpl data in dataSets) {
				if (data.GetId () == id) {
					return data;
				}
			}
			return null;
		}
		public PathData FindDataSetByName (string name)
		{
			// TODO add lookup dictionary (we also need to listen to name changes!)
			foreach (PathDataImpl data in dataSets) {
				if (data.GetName () == name) {
					return data;
				}
			}
			return null;
		}
		public PathData AddDataSet ()
		{
			return InsertDataSet (dataSets.Count);
		}
		public PathData InsertDataSet (int index)
		{
			PathDataImpl data = new PathDataImpl (this, NextDataSetId ());
			if (dataSets.Count == 0) {
				// First data set, i.e. the default
				this.defaultDataSetId = data.GetId ();
			}
			data.SetColor (NextDataSetColor ());
			dataSets.Insert (index, data);
			//			if (index < GetDefaultDataSetIndex ()) {
			//				// Update the default data set index
			//				this.defaultDataSetIndex++;
			//			}
			return data;
		}
		public void RemoveDataSetAtIndex (int index)
		{
			PathData dataToRemove = GetDataSetAtIndex (index);
			if (dataToRemove.GetId () == GetDefaultDataSetId ()) {
				throw new ArgumentException ("Can't remove default Data Set");
			}
			dataSets.RemoveAt (index);
		}

		public void SetDataSetName (PathData data, string name)
		{
			if (data is PathDataImpl) {
				((PathDataImpl)data).SetName (StringUtil.Trim (name, true));
				// TODO update name lookup index!
			} else {
				throw new ArgumentException ("Renaming PathData of type '" + data.GetType ().FullName + "' is not supported");
			}
		}
		public void SetDataSetColor (PathData data, Color value)
		{
			if (data is PathDataImpl) {
				((PathDataImpl)data).SetColor (value);
				// TODO update name lookup index!
			} else {
				throw new ArgumentException ("Renaming PathData of type '" + data.GetType ().FullName + "' is not supported");
			}
		}

		public void SetDataSetInputSourceType (PathData data, PathDataInputSource.SourceType sourceType)
		{
			((PathDataImpl)data).SetInputSourceType (sourceType);
		}
		public T SetDataSetInputSource<T> (PathData data, T source) where T: PathDataInputSource
		{
			((PathDataImpl)data).SetInputSource (source);
			return (T)((PathDataImpl)data).GetInputSource ();
		}
//		public void SetDataSetInputSourceType(PathData data, PathDataInputSource.SourceType sourceType) {
//			
//		}

		public void ForceUpdatePathData (PathData data)
		{
			if (data.GetPath () != this || !(data is PathDataImpl)) {
				throw new ArgumentException ("Can't update foreign PathData: " + data);
			}
			((PathDataImpl)data).ForceUpdatePathPoints ();
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
				try {
					Changed (this, EventArgs.Empty);
				} catch (Exception e) {
					Debug.LogError ("Catched an exception from Changed event listener(s): " + e);
				}
			}
		}

		// TODO this is already in IPathInfo! Do we need it in here?
		public abstract bool IsLoop ();
    

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
			this, null);
			
			return pmc;
		}



		public bool IsUpToDate (long statusToken)
		{
			return statusToken == this.updateToken;
		}
		public long GetStatusToken ()
		{
			return this.updateToken;
		}




		public ParameterStore GetParameterStore ()
		{
			return parameterStore;
		}

		public void OnBeforeSerialize ()
		{
			parameterStore.OnBeforeSerialize ();
			// TODO should we call OnBeforeSerialize() for all data sets here?
			OnBeforePathSerialize ();
		}
		public virtual void OnBeforePathSerialize ()
		{

		}
		public void OnAfterDeserialize ()
		{
			parameterStore.OnAfterDeserialize ();

			int dsCount = dataSets.Count;
			for (int i = 0; i < dsCount; i++) {
				PathDataImpl d = dataSets [i];

				d.SetPath (this);
			}


			// TODO Fire changed events? Do we know if anything was changed? Maybe not?
			OnAfterPathDeserialize ();

		}
		public virtual void OnAfterPathDeserialize ()
		{

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

			foreach (PathDataImpl d in dataSets) {
				d.SetPath (this);
				d.PathModifiersChanged ();
//				IPathModifierContainer pmc = d.GetPathModifierContainer ();
//				if (pmc is DefaultPathModifierContainer) {
//					((DefaultPathModifierContainer)pmc).SavePathModifiers (parameterStore);
//				}
			}


			FireChangedEvent ();

		}

		public virtual void OnPathModifiersChanged ()
		{

		}

		public virtual void OnPathPointsChanged ()
		{

		}


       
		public abstract int GetControlPointCount ();

		public abstract Vector3 GetControlPointAtIndex (int index);

		public abstract void SetControlPointAtIndex (int index, Vector3 pt);

		void OnDrawGizmos ()
		{

			// Draw all data sets:
			int dsCount = GetDataSetCount ();
			for (int dsIndex = 0; dsIndex < dsCount; dsIndex++) {

				PathData data = GetDataSetAtIndex (dsIndex);

				PathPoint[] pp = data.GetAllPoints ();


				//Vector3[] transformedPoints = new Vector3[pp.Length];


				// Draw the actual (final) path:
				Gizmos.color = data.GetColor ();//PathGizmoPrefs.FinalPathLineColor;
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


}
