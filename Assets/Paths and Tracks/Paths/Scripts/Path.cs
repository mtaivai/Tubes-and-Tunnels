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

	public class PathChangedEvent : EventArgs
	{
		public enum EventReason
		{
			DataChanged,
			DefaultDataSetChanged,
		}
		private EventReason reason;
	
		private PathSelector changedData;

		public PathChangedEvent (EventReason reason, PathSelector changedData)
		{
			this.reason = reason;
			this.changedData = changedData;
		}
		public PathChangedEvent (EventReason reason, Path path, IPathData pathData) : this(reason, new PathSelector (path, pathData != null ? pathData.GetId() : 0))
		{
		}
		public EventReason Reason {
			get { return reason; }
		}
	
		public PathSelector ChangedData {
			get { return changedData; }
		}
		public override string ToString ()
		{
			return string.Format ("[PathChangedEvent: reason={0}, changedData={1}]", reason, changedData);
		}
		
	}

	public delegate void PathChangedEventHandler (object sender,PathChangedEvent e);

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
		public static readonly float FinalPathPointMarkerSize = 0.05f;
		public static readonly float FinalPathFirstPointMarkerSize = 0.1f;
	}




	// TODO Should this be renamed to AbstractPath?
	[ExecuteInEditMode]
	public abstract class Path : MonoBehaviour, ISerializationCallbackReceiver
	{
		public event PathChangedEventHandler Changed;

		public enum PathStatus
		{
			Dynamic,
			ManualRefresh,
			Frozen
		}

		// TODO move this to PathData??? Maybe not
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
		private PathStatus
			frozenStatus = PathStatus.Dynamic;

//		[SerializeField]
//		private List<UnityEngine.Object>
//			referents = new List<UnityEngine.Object> ();

		[SerializeField]
		private SimpleReferenceContainer
			referenceContainer;

//		[SerializeField]
//		private List<IPathData>
//			dataSets = new List<IPathData> ();

		[SerializeField]
		private int
			defaultDataSetId;

		[SerializeField]
		private int
			nextDataSetId = 1; // "0" is reserved for "default"

		[SerializeField]
		private int
			nextDataSetColorIndex = 0;

		[SerializeField]
		private int
			editorSceneViewDataSetId = 0;
		[SerializeField]
		private bool
			editorSceneViewDataSetLocked = false;

		private IPathInfo pathInfo;

		protected Path ()
		{
		}

		#region Abstracts
		public abstract int GetDataSetCount ();
		public abstract IPathData GetDataSetAtIndex (int index);
		protected abstract void DoInsertDataSet (int index, IPathData data);
		protected abstract void DoRemoveDataSet (int index);
		protected abstract IPathData CreatePathData (int id);

		#endregion Abstracts
		#region Properties
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
		#endregion Properties

		#region Unity Lifecycle Messages

		protected void Reset ()
		{
			Debug.Log ("Resetting path");

			IPathData defaultData = AddDataSet ();
			nextDataSetColorIndex = 0;
			SetDataSetName (defaultData, "Default");
			
			// NEVER reset the nextDataSetId!

			editorSceneViewDataSetId = 0; // default

			frozenStatus = PathStatus.Dynamic;
		}

		protected void OnEnable ()
		{
			Debug.Log ("OnEnable");
		}

		protected void OnDisable ()
		{
			Debug.Log ("OnDisable");
		}

		protected virtual void OnDrawGizmos ()
		{
			DrawGizmos ();
		}

		#endregion Unity Lifecycle Messages

		#region Serialization
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
			
			int dsCount = GetDataSetCount ();
			for (int i = 0; i < dsCount; i++) {
				IPathData d = GetDataSetAtIndex (i);
				
				AttachPathData (d);
			}
			
			// TODO Fire changed events? Do we know if anything was changed? Maybe not?
			OnAfterPathDeserialize ();
			
		}
		
		//		protected abstract void OnAttachPathData (IPathData data);
		
		public virtual void OnAfterPathDeserialize ()
		{
			
		}
		#endregion Serialization

		#region Events

		private void DoFireChangedEvent (PathChangedEvent ev)
		{
			// TODO we need to get parameters!
			if (null != Changed) {
				Debug.Log ("Firing PathChangedEvent to " + Changed.GetInvocationList ().Length + " receivers: " + ev);
				try {
					Changed (this, ev);
				} catch (Exception e) {
					Debug.LogErrorFormat ("Catched an exception from Changed event listener(s): {0}", e);
				}
			}
		}
		private void PathDataChanged (object sender, PathChangedEvent ev)
		{
			// One of our data sets was modified (this is called by the modified data set)
			
			Path changedPath = ev.ChangedData.Path;
			IPathData changedData = ev.ChangedData.PathData;
			
			Debug.Log ("Path " + name + " received PathChangedEvent from " + changedPath.name + "; ChangedData='" + changedData.GetName () + "'");
			
			// Route the event to datasets with input from the changed dataset
			
			
			// First check our data sets:
			List<IPathData> targetDataSets = GetDependedPathDataSets (changedPath, changedData);
			// Notify our dependent target data sets about the modified data set:
			foreach (IPathData targetData in targetDataSets) {
				Debug.Log ("Marking data '" + targetData.GetName () + "' dirty");
				MarkPathDataDirty (targetData);
			}

			if (ReferenceEquals (ev.ChangedData.Path, this)) {
				// Then notify other Path instances (enabled and active only):
				UnityEngine.Object[] pathObjects = GameObject.FindObjectsOfType (typeof(Path));
				foreach (UnityEngine.Object pathObj in pathObjects) {
					Path p = (Path)pathObj;
					if (p != this) {
						Debug.Log ("Path " + name + " sends PathDataChanged notification to Path " + p.name);
						p.PathDataChanged (this, ev);
						//					List<IPathData> targetDataSets = GetDependedTargetPathDataSets (p, this, changedData);
						//					p.PathDataChanged (ev);
					}
				}
			
				// Finally fire events to other interested receivers:
				DoFireChangedEvent (ev);
			}
			
		}
		#endregion Events

		public static Path FindParentPathObject (Transform obj)
		{
			Transform parent = obj.parent;
			if (null != parent) {
				Path path = parent.GetComponent<Path> ();
				if (null != path) {
					return path;
				} else {
					return FindParentPathObject (parent);
				}
			} else {
				return null;
			}
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


		public int GetDefaultDataSetId ()
		{
			//return Mathf.Clamp (this.defaultDataSetIndex, -1, dataSets.Count - 1);
			return this.defaultDataSetId;
		}
		public void SetDefaultDataSetId (int id)
		{
			if (id == 0) {
				throw new ArgumentException ("Data Set ID zero (0) is reserved for the 'default' data set", "id");
			} else if (id != this.defaultDataSetId) {
				IPathData d = FindDataSetById (id);
				if (null != d) {
					this.defaultDataSetId = d.GetId ();
				} else {
					throw new ArgumentException ("No dataset exists with id " + id, "id");
				}
				PathChangedEvent e = new PathChangedEvent (PathChangedEvent.EventReason.DefaultDataSetChanged, this, GetDefaultDataSet ());
				DoFireChangedEvent (e);
			}
		}
		// TODO move Set... methods to PathData itself? 
		public bool IsDefaultDataSet (IPathData data)
		{
			return null != data && data.GetId () == GetDefaultDataSetId ();
		}



		public IPathData GetDefaultDataSet ()
		{
			int defaultId = GetDefaultDataSetId ();
			if (defaultId == 0) {
				// No "default of default" available!
				return null;
			} else {
				return FindDataSetById (defaultId);
			}
		}

		public int IndexOfDataSet (IPathData data)
		{
			int dsCount = GetDataSetCount ();

			int index = -1;
			int idToFind = data.GetId ();
			for (int i = 0; i < dsCount; i++) {
				IPathData ds = GetDataSetAtIndex (i);
				if (ds.GetId () == idToFind) {
					index = i;
					break;
				}
			}
			return index;
		}
		public virtual bool IsSetDataSetIndexSupported ()
		{
			return false;
		}
		public virtual void SetDataSetIndex (IPathData data, int newIndex)
		{
			throw new NotSupportedException ();
		}


		public IPathData FindDataSetById (int id)
		{
			if (id == 0) {
				return GetDefaultDataSet ();
			} else {
				int dsCount = GetDataSetCount ();
				// TODO add lookup dictionary (we also need to listen to name changes!)
				for (int i = 0; i < dsCount; i++) {
					IPathData data = GetDataSetAtIndex (i);
					if (data.GetId () == id) {
						return data;
					}
				}
				return null;
			}
		}
		public IPathData FindDataSetByName (string name)
		{
			int dsCount = GetDataSetCount ();
			// TODO add lookup dictionary (we also need to listen to name changes!)
			for (int i = 0; i < dsCount; i++) {
				IPathData data = GetDataSetAtIndex (i);
				if (data.GetName () == name) {
					return data;
				}
			}
			return null;
		}


		public IPathData GetEditorSceneViewDataSet ()
		{
			IPathData data;
			data = FindDataSetById (editorSceneViewDataSetId);
			if (null == data) {
				data = GetDefaultDataSet ();
				this.editorSceneViewDataSetId = data.GetId ();
			}
			return data;
		}
		public void SetEditorSceneViewDataSetId (int value)
		{
			this.editorSceneViewDataSetLocked = true;
			this.editorSceneViewDataSetId = value;
		}
		public bool IsEditorSceneViewDataSetLocked ()
		{
			return this.editorSceneViewDataSetLocked;
		}
		public void SetEditorSceneViewDataSetLocked (bool value)
		{
			this.editorSceneViewDataSetLocked = value;
		}



		private void AttachPathData (IPathData data)
		{
			if (data is IAttachableToPath) {
				try {
					((IAttachableToPath)data).AttachToPath (this, PathDataChanged);
				} catch (Exception e) {
					Debug.LogError ("An exception occurred while invoking PathData.AttachToPath: " + e, this);
				}
			}
			try {
				OnAttachPathData (data);
			} catch (Exception e) {
				Debug.LogError ("An exception occurred while invoking OnAttachPathData(): " + e, this);
			}
		}
		protected void OnAttachPathData (IPathData data)
		{

		}
		private void DetachPathData (IPathData data)
		{
			if (data is IAttachableToPath) {
				try {
					((IAttachableToPath)data).DetachFromPath (this);
				} catch (Exception e) {
					Debug.LogError ("An exception occurred while invoking PathData.DetachFromPath: " + e, this);
				}
			}
			try {
				OnDetachPathData (data);
			} catch (Exception e) {
				Debug.LogError ("An exception occurred while invoking OnDetachPathData(): " + e, this);
			}
		}
		protected void OnDetachPathData (IPathData data)
		{
			
		}


		public IPathData AddDataSet ()
		{
			return InsertDataSet (GetDataSetCount ());
		}
		public IPathData InsertDataSet (int index)
		{
			int dsCount = GetDataSetCount ();
			IPathData data = CreatePathData (NextDataSetId ());
			if (dsCount == 0) {
				// First data set, i.e. the default
				this.defaultDataSetId = data.GetId ();
			}
			data.SetColor (NextDataSetColor ());
			DoInsertDataSet (index, data);

			AttachPathData (data);

			//			if (index < GetDefaultDataSetIndex ()) {
			//				// Update the default data set index
			//				this.defaultDataSetIndex++;
			//			}
			return data;
		}
		public void RemoveDataSetAtIndex (int index)
		{
			IPathData dataToRemove = GetDataSetAtIndex (index);
			if (dataToRemove.GetId () == GetDefaultDataSetId ()) {
				throw new ArgumentException ("Can't remove default Data Set");
			}

			DoRemoveDataSet (index);

			DetachPathData (dataToRemove);
			

		}

		public void SetDataSetName (IPathData data, string name)
		{
			if (data is AbstractPathData) {
				((AbstractPathData)data).SetName (StringUtil.Trim (name, true));
				// TODO update name lookup index!
			} else {
				throw new ArgumentException ("Renaming PathData of type '" + data.GetType ().FullName + "' is not supported");
			}
		}


//		public void SetDataSetInputSourceType (IPathData data, PathDataInputSource.SourceType sourceType)
//		{
//			((AbstractPathData)data).SetInputSourceType (sourceType);
//		}
//		public T SetDataSetInputSource<T> (IPathData data, T source) where T: PathDataInputSource
//		{
//			((AbstractPathData)data).SetInputSource (source);
//			return (T)((AbstractPathData)data).GetInputSource ();
//		}


		public void ForceUpdatePathData (IPathData data)
		{
			if (/*data.GetPath () != this ||*/ !(data is AbstractPathData)) {
				throw new ArgumentException ("Can't update foreign PathData: " + data);
			}
			((AbstractPathData)data).ForceUpdatePathPoints ();
		}

		public IReferenceContainer GetReferenceContainer ()
		{
			if (null == referenceContainer) {
				referenceContainer = new SimpleReferenceContainer ();
			}
			return referenceContainer;
		}



		// Override this if the IPathData provided by the Path is not extending AbstractPathData:
		protected void MarkPathDataDirty (IPathData data)
		{
			((AbstractPathData)data).PathPointsChanged ();
		}



		private List<IPathData> GetDependedPathDataSets (Path sourcePath, IPathData sourceData)
		{
			return GetDependedPathDataSets (this, sourcePath, sourceData);
		}

		private static List<IPathData> GetDependedPathDataSets (Path targetPath, Path sourcePath, IPathData sourceData)
		{
			List<IPathData> deps = new List<IPathData> ();

			for (int i = 0; i < targetPath.GetDataSetCount(); i++) {
				IPathData ds = targetPath.GetDataSetAtIndex (i);
				if (ds == sourceData) {
					// Shouldn't happen - there's an infinite loop if this happens
					continue;
				}
				IPathModifierContainer pmc = ds.GetPathModifierContainer ();
				IReferenceContainer refContainer = pmc.GetReferenceContainer ();
				IPathModifier[] pms = pmc.GetPathModifiers ();
				foreach (IPathModifier pm in pms) {
					if (pm.IsEnabled () && pm is IncludePathModifier) {
						Path includedPath = ((IncludePathModifier)pm).GetIncludedPath (refContainer);
						IPathData includedData = ((IncludePathModifier)pm).GetIncludedPathData (includedPath, refContainer);
						if (null != includedPath && null != includedData) {
							if (includedPath == sourcePath && includedData.GetId () == sourceData.GetId ()) {
								// Notify the including path:
								//Debug.Log ("Notifying " + p.name + ":" + ds.GetName () + " about changed data in " + name + ":" + changedData.GetName ());
								//((AbstractPathData)ds).PathPointsChanged ();
								deps.Add (ds);
								//p.PathPointsChanged();
							}
						}
					}
				}
			}
			return deps;
		}


		protected void DrawGizmos ()
		{

			// Draw all data sets:
			int dsCount = GetDataSetCount ();
			for (int dsIndex = 0; dsIndex < dsCount; dsIndex++) {

				IPathData data = GetDataSetAtIndex (dsIndex);
				if (!data.IsDrawGizmos ()) {
					continue;
				}

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

					Gizmos.color = (i == 0) ? firstPointMarkerColor : pointMarkerColor;
					Gizmos.DrawSphere (pt, (i == 0) ? firstPointMarkerSize : pointMarkerSize);
				}

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
