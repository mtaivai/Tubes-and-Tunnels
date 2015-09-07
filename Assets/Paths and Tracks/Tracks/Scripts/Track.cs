using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Tracks
{




	[ExecuteInEditMode]
	public class Track : MonoBehaviour, ISerializationCallbackReceiver
	{
		// TODO add multiple data sources:
//		[SerializeField]
//		private List<TrackDataSource>
//			dataSources = new List<TrackDataSource> ();


		// Primary path to follow
		[SerializeField]
		private TrackDataSource
			primaryDataSource;

		// Path for generating the mesh or meshes
		// Path for generating mesh collider(s)
//		
//		[SerializeField]
//		private PathWithDataId
//			meshPathData = new PathWithDataId ();
//		
//		[SerializeField]
//		private PathWithDataId
//			meshCollidersPathData = new PathWithDataId ();
		
		[SerializeField]
		private string
			trackGeneratorType;
		
		[SerializeField]
		internal ParameterStore
			parameters;
		
		[SerializeField]
		private SimpleReferenceContainer
			referenceContainer = new SimpleReferenceContainer ();
		
		// TODO should this be serialized?
		[SerializeField]
		private bool
			autoUpdateWithPath = false;
		
		[SerializeField]
		private bool
			autoUpdateMesh = false;
		
		// don't serialize!
		private DefaultPathModifierContainer pathModifierContainer = null;
		
		//[SerializeField]
		public Mesh generatedMesh;


		// nonserialized
		private TrackSlice[] generatedSlices;
		
		// nonserialized
		private ITrackGenerator _trackGeneratorInstance;


		public Track ()
		{
		}

#region Properties
		public bool AutomaticUpdateWithPath {
			get { return autoUpdateWithPath; }
			set { this.autoUpdateWithPath = value; }
		}
		
		public bool AutomaticMeshUpdate {
			get { return autoUpdateMesh; }
			set { this.autoUpdateMesh = value; }
		}
		
		public ITrackGenerator TrackGeneratorInstance {
			get {
				if (null == _trackGeneratorInstance) {
					if (null != trackGeneratorType && trackGeneratorType.Length > 0) {
						// Create the instance
						_trackGeneratorInstance = (ITrackGenerator)Activator.CreateInstance (Type.GetType (trackGeneratorType));
						// Load parameters
						// Load params:
						parameters.OnAfterDeserialize ();
						_trackGeneratorInstance.LoadParameters (GetTrackGeneratorParameterStore ());
					}
				}
				return _trackGeneratorInstance;
			}
		}
		
		
		public TrackDataSource PrimaryDataSource {
			get {
				if (null == primaryDataSource) {
					primaryDataSource = CreateDataSource ();
				}
				return primaryDataSource;
			}
		}
		
		
		public string TrackGeneratorType {
			get {
				return trackGeneratorType;
			}
			set {
				if (this.trackGeneratorType != value) {
					// Changed
					this.trackGeneratorType = value;
					TrackGeneratorChanged ();
				}
			}
		}
		
		// Returns a copy of the internal array
		public TrackSlice[] TrackSlices {
			get {
				if (null == generatedSlices) {
					generatedSlices = TrackGeneratorInstance.CreateSlices (this, false);
				}
				TrackSlice[] arr = new TrackSlice[generatedSlices.Length];
				Array.Copy (generatedSlices, arr, generatedSlices.Length);
				return arr;
			}
		}


		
		public ParameterStore ParameterStore {
			get {
				if (null == parameters) {
					parameters = new ParameterStore ();
				}
				return parameters;
			}
		}
#endregion

#region Lifecycle Events
		// Lifecycle events
		void Awake ()
		{
		}
		
		public void OnEnable ()
		{
			// Notify our data source(s); they will register listeners bound to path(s)
			primaryDataSource.OnEnable (this);
		}
		public void OnDisable ()
		{
			// Notify our data source(s); they will unregister any listeners bound to path(s)
			primaryDataSource.OnDisable (this);
		}
		public void OnDestory ()
		{
			// OnDisable() is already called, so no need to do anything in here
//			primaryDataSource.OnDisable (this);
		}

		// Use this for initialization
		void Start ()
		{
			
		}
		
		// Update is called once per frame
		void Update ()
		{
			
		}
		
		void OnDrawGizmos ()
		{
			// Draw mesh?
			if (null != generatedMesh) {
				Gizmos.color = Color.green;
				//Gizmos.DrawWireMesh(generatedMesh);
			}
		}

#endregion Lifecycle Events


#region Serialization
		public void OnBeforeSerialize ()
		{
			ParameterStore.OnBeforeSerialize ();
		}
		
		public void OnAfterDeserialize ()
		{
			ParameterStore.OnAfterDeserialize ();
			
			GetPathModifierContainer ().LoadConfiguration ();
			
			//			if (null != primaryDataSource) {
			//				primaryDataSource.OnEnable ();
			//			}
			
			this.TrackGeneratorChanged ();
			
			// Register event listener on data sources:
			primaryDataSource.DataChanged -= OnDataSourceDataChanged;
			primaryDataSource.DataChanged += OnDataSourceDataChanged;
			
		}

		public void SaveTrackGeneratorParameters ()
		{
			if (null != this.TrackGeneratorInstance) {
				ParameterStore store = GetTrackGeneratorParameterStore ();
				TrackGeneratorInstance.SaveParameters (store);
			}
			//          trackGenerator.SaveParameters (track.GetTrackGeneratorParameterStore ());
		}
#endregion Serialization


#region Event Handlers
		// Called from our DefaultPathModifierContainer when PathModifiers have changed (added/removed/edited)
		private void PathModifiersChanged (PathModifierContainerEvent e)
		{
			Debug.LogFormat ("PathModifiersChanged: {0}", e);
			primaryDataSource.InvalidateProcessedData ();

		}

		private void OnDataSourceDataChanged (TrackDataChangedEventArgs e)
		{
			// TODO what if we're destroyed?
			Debug.LogFormat ("OnDataSourceDataChanged: {0}", e);
			if (e.Stage == TrackDataStage.Unprocessed) {
				// Source data has changed
				if (autoUpdateWithPath) {
					// Invalidate our processed data to trigger its reprocessing
					e.DataSource.InvalidateProcessedData ();
				}
			} else if (e.Stage == TrackDataStage.Processed) {
				MarkSlicesDirty ();
				if (autoUpdateMesh) {
					GenerateTrackMesh ();
				}
			}

		}

//		private void OnDataSourceProcessedDataChanged ()
//		{
//			Debug.Log ("OnDataSourceProcessedDataChanged");
//			// Invalidate cache
//			primaryDataSource.InvalidateProcessedData ();
//
//			// Set slices dirty
//			MarkSlicesDirty ();
//
//			// Generate mesh
//			this.GenerateTrackMesh ();
//		}


		// Called when the attached TrackGenerator is changed
		private void TrackGeneratorChanged ()
		{
			// Force to create a new instance:
			this._trackGeneratorInstance = null;
		}

#endregion Event Handlers

		private void MarkSlicesDirty ()
		{
			this.generatedSlices = null;
		}

		protected TrackDataSource CreateDataSource ()
		{
			return new TrackDataSource (this);
		}
		private IPathInfo DoGetPathInfo ()
		{
			IPathData d = primaryDataSource.PathSelector.PathData;
			return (null != d) ? d.GetPathInfo () : null;
		}

		protected virtual DefaultPathModifierContainer CreatePathModifierContainer ()
		{

			DefaultPathModifierContainer pmc = new DefaultPathModifierContainer (
				DoGetPathInfo,
				primaryDataSource.GetUnprocessedPoints,
				null,
				() => this.referenceContainer, 
				() => null, 
				() => parameters);
			
			pmc.PathModifiersChanged += this.PathModifiersChanged;
			// TODO should "parameters" above be a child ParamaterStore with prefix "pathModifiers."?
			
			
			return pmc;
		}

		public DefaultPathModifierContainer GetPathModifierContainer ()
		{
			if (null == pathModifierContainer) {
				pathModifierContainer = CreatePathModifierContainer ();
			}
			return pathModifierContainer;
		}
		

		
		internal ParameterStore GetTrackGeneratorParameterStore ()
		{
			return new ParameterStore (this.parameters, _trackGeneratorInstance.GetType ().FullName);
		}
		

		

		
		/// <summary>
		/// Generates a Mesh with the current TrackGeneratorInstance and assigns it to 
		/// "generatedMesh" property.
		/// </summary>
		public void GenerateTrackMesh ()
		{
			
			//          Path path = Path;
			ITrackGenerator tg = TrackGeneratorInstance;
			if (null == tg) {
				Debug.LogError ("TrackGeneratorInstance is null");
				return;
			}
			
			if (generatedMesh == null) {
				Debug.Log ("Creating new Mesh instance");
				generatedMesh = new Mesh ();
				
			} else {
				Debug.Log ("Updating existing Mesh instance");
				generatedMesh.Clear ();
			}
			
			// Create initial Mesh name:
			if (StringUtil.IsEmpty (generatedMesh.name)) {
				generatedMesh.name = gameObject.name + "Mesh";
			}
			
			tg.CreateMesh (this, generatedMesh);
			
			// Upate MeshFilter
			MeshFilter mf = GetComponent<MeshFilter> ();
			if (null != mf) {
				
				mf.mesh = generatedMesh;
			}
		}
		
		public void GenerateTrackColliders ()
		{
			
			//			MeshCollider mc = track.GetComponent<MeshCollider> ();
			//			if (null == mc) {
			//				mc = track.gameObject.AddComponent<MeshCollider> ();
			//			}
			//
			//			xxx ();
			//			mc.sharedMesh = m;
		}
		
//		protected void UpdatePathPoints ()
//		{
//			
//			if (null == cachedPathPoints && pathData.HasValidData) {
//				IPathData data = pathData.PathData;
//				
//				this.cachedPathPoints = data.GetAllPoints ();
//				this.cachedPathPointFlags = data.GetOutputFlags ();
//				this.cachedPathPointsAfterModifiers = null;
//				this.cachedPathPointFlagsAfterModifiers = 0;
//			}
//			
//			if (null == cachedPathPointsAfterModifiers && null != cachedPathPoints) {
//				IPathData data = pathData.PathData;
//				
//				PathModifierContext context = new PathModifierContext (
//					data.GetPathInfo (), 
//					GetPathModifierContainer (), 
//					cachedPathPointFlags);
//				
//				int flags = this.cachedPathPointFlags;
//				this.cachedPathPointsAfterModifiers = PathModifierUtil.RunPathModifiers (
//					context, this.cachedPathPoints, ref flags, true);
//				this.cachedPathPointFlagsAfterModifiers = flags;
//			}
//		}
		
//		public PathPoint[] GetPathPoints (out int ppFlags)
//		{
//			UpdatePathPoints ();
//			ppFlags = cachedPathPointFlagsAfterModifiers;
//			return cachedPathPointsAfterModifiers != null ? cachedPathPointsAfterModifiers : new PathPoint[0];
//		}
		
//		public PathPoint[] GetPathPoints ()
//		{
//			int flags;
//			return GetPathPoints (out flags);
//		}
//		
//		public int GetPathOutputFlags ()
//		{
//			// Just to make sure that caches are up-to-date:
//			UpdatePathPoints ();
//			return cachedPathPointFlags;
//		}
		
		//		public void OnPathModifiersChanged ()
		//		{
		//			GetPathModifierContainer ().SaveConfiguration ();
		//			ConfigurationChanged ();
		//		}
		
		//		private IPathData GetCurrentPathData ()
		//		{
		//			Path path = pathDataId.Path;
		//			return (null != path) ? path.GetDefaultDataSet () : null;
		//		}
		

		

	}


}
