using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using Util;
using Paths;

namespace Paths.MeshGenerator
{

	[ExecuteInEditMode]
	[AddComponentMenu("Paths/Path Mesh Generator", 3)]
	public class PathMeshGenerator : MonoBehaviour, ISerializationCallbackReceiver
	{
		private class PathDataSourceContainer : IPathDataSourceContainer
		{
			private PathMeshGenerator parent;
			public PathDataSourceContainer (PathMeshGenerator parent)
			{
				this.parent = parent;
			}
			public IReferenceContainer GetReferenceContainer ()
			{
				return parent.GetReferenceContainer ();
			}
			public ParameterStore GetParameterStore (PathDataSource ds)
			{
				return parent.ParameterStore.ChildWithPrefix ("DataSource[" + ds.Id + "]");
			}
		}

				
		[SerializeField]
		private string
			meshGeneratorType;
				
		[NonSerialized]
		private IMeshGenerator
			_meshGeneratorInstance;
				
		[SerializeField]
		private Mesh[]
			generatedMeshes;
				
		// nonserialized; only for editor / gizmos drawing!
		// TODO do we really need this?
//		private PathMeshSlice[] _generatedSlices;


		[SerializeField]
		private PathDataSource
			dataSource;

		[SerializeField]
		private ParameterStore
			parameters;
		
		[SerializeField]
		private SimpleReferenceContainer
			referenceContainer = new SimpleReferenceContainer ();
		
		[SerializeField]
		private bool
			autoUpdateWithPath = false;
		
		[SerializeField]
		private bool
			autoUpdateMesh = false;


		[SerializeField]
		public bool
			createShapes = true;

		[SerializeField]
		private Material[]
			shapeMaterials;


		[SerializeField]
		public bool
			createMeshColliders = false;
//
//		[SerializeField]
//		public bool
//			updateMeshFilter = true;
//		[SerializeField]
//		public bool
//			createMeshFilter = true;
//		[SerializeField]
//		public bool
//			createMeshRenderer = true;
//
//		[SerializeField]
//		public bool
//			updateMeshCollider = true;
//		[SerializeField]
//		public bool
//			createMeshCollider = true;

		[SerializeField]
		private HierarchicalObjectContainer
			meshCollidersContainer = new HierarchicalObjectContainer ("_MeshColliders");

		[SerializeField]
		private HierarchicalObjectContainer
			shapesContainer = new HierarchicalObjectContainer ("_Shapes");



//		[SerializeField]
//		public bool
//			updateMeshColliders2 = true;
//		[SerializeField]
//		public bool
//			createMeshColliders2 = true;
//		
//		[SerializeField]
//		public int
//			meshColliderCount = 1;
//
//

		
		// don't serialize!
		//private DefaultPathModifierContainer pathModifierContainer = null;

		public PathMeshGenerator ()
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

		
		public ParameterStore ParameterStore {
			get {
				if (null == parameters) {
					parameters = new ParameterStore ();
				}
				return parameters;
			}
		}

		public PathDataSource DataSource {
			get {
				return dataSource;
			}
		}
			
		public string MeshGeneratorType {
			get {
				return meshGeneratorType;
			}
			set {
				DoSetMeshGeneratorType (value);
			}
		}
		public IMeshGenerator MeshGeneratorInstance {
			get {
				return DoGetMeshGeneratorInstance ();
			}
		}
//		public Mesh GeneratedMesh {
//			get {
//				return generatedMesh;
//			}
//			set {
//				this.generatedMesh = value;
//			}
//		}
//		public PathMeshSlice[] GeneratedSlices {
//			get {
//				return _generatedSlices;
//			}
//			set {
//				// TODO should we clone the array?
//				this._generatedSlices = value;
//			}
//		}


		public Material[] ShapeMaterials {
			get {
				IMeshGenerator mg = MeshGeneratorInstance;
				int matCount = (null != mg) ? mg.GetMaterialSlotCount () : 0;
				if (shapeMaterials == null) {
					shapeMaterials = new Material[matCount];
				} else if (matCount > shapeMaterials.Length) {
					// TODO should we also shrink the array? In that case the user would
					// lose any assigned materials
					Array.Resize (ref shapeMaterials, matCount);
				}
				return shapeMaterials;
			}
			set {
				// TODO should this be public?
				this.shapeMaterials = value;
			}
		}



		public MeshObjectVisibility ShapeObjectsVisibility {
			get {
				return new MeshObjectVisibility (shapesContainer);
			}
		}
		public MeshObjectVisibility ColliderObjectsVisibility {
			get {
				return new MeshObjectVisibility (meshCollidersContainer);
			}
		}

#endregion

#region IMeshGenerator operations


		private bool CallMeshGeneratorLifecycle (Action message, bool throwExceptions)
		{
			if (throwExceptions) {
				message ();
				return true;
			} else {
				try {
					message ();
					return true;
				} catch (Exception e) {
					Debug.LogErrorFormat ("Catched an exception while calling {0}: {1}", message, e);
					return false;
				}
			}
		}

		private void DoSetMeshGeneratorType (string value)
		{
			string prevType = this.meshGeneratorType;
			this.meshGeneratorType = value;
			if (prevType != this.meshGeneratorType) {
				// Force recreation of the instance:
				if (null != this._meshGeneratorInstance) {
					if (isActiveAndEnabled) {
						CallMeshGeneratorLifecycle (_meshGeneratorInstance.OnDisable, false);
					}
					CallMeshGeneratorLifecycle (_meshGeneratorInstance.OnDestroy, false);
				}
				this._meshGeneratorInstance = null;
			}
		}
		private IMeshGenerator DoGetMeshGeneratorInstance ()
		{
			if (null != _meshGeneratorInstance && _meshGeneratorInstance.GetType ().FullName != this.meshGeneratorType) {
				// Type has changed
				if (isActiveAndEnabled) {
					CallMeshGeneratorLifecycle (_meshGeneratorInstance.OnDisable, false);
				}
				CallMeshGeneratorLifecycle (_meshGeneratorInstance.OnDestroy, false);
				try {
					_meshGeneratorInstance.RemoveMeshGeneratorEventHandler (MeshGeneratorChanged);
				} catch (Exception ex) {
					// NOP
					// TODO this smells
				}
				_meshGeneratorInstance = null;
			}

			if (null == _meshGeneratorInstance) {
				_meshGeneratorInstance = DoCreateMeshGeneratorInstance (true, true, true);
			} 
			return _meshGeneratorInstance;

		}
		private IMeshGenerator DoCreateMeshGeneratorInstance (bool callOnCreate, bool callOnEnableIfEnabled, bool addEventHandlers)
		{
			IMeshGenerator mg;
			if (null != meshGeneratorType && meshGeneratorType.Length > 0) {
				// Create the instance
				mg = (IMeshGenerator)Activator.CreateInstance (Type.GetType (meshGeneratorType));
				
				// Notify the MG:
				if (callOnCreate) {
					CallMeshGeneratorLifecycle (mg.OnCreate, true);
				}

				if (addEventHandlers) {
					mg.AddMeshGeneratorEventHandler (MeshGeneratorChanged);
				}
				
				// TODO we must prefix the store!
				mg.LoadParameters (GetParameterStoreForMG (mg), GetReferenceContainer ());
				
				if (callOnEnableIfEnabled && isActiveAndEnabled) {
					CallMeshGeneratorLifecycle (mg.OnEnable, true);
				}


				
			} else {
				mg = null;
			}
			return mg;
		}
		
		private ParameterStore GetParameterStoreForMG (IMeshGenerator mg)
		{
			string prefix = string.Format ("MeshGenerators[{0}]", mg.GetType ().FullName);
			return parameters.ChildWithPrefix (prefix);
		}


		private void MeshGeneratorChanged (MeshGeneratorEventArgs e)
		{
			Debug.Log ("MeshGeneratorChanged: " + e);
			MeshGeneratorModified ();
		}

#endregion

#region Lifecycle Events
		// Lifecycle events
		void Awake ()
		{
		}

		protected void Reset ()
		{
			Debug.Log ("Reset Track: " + this.ToString ());
			// TODO reset everything!

			bool firstTime = (null == this.dataSource);

			if (null != dataSource) {
				DetachDataSource (dataSource);
				dataSource.Dispose ();
			}

			dataSource = CreateDataSource ();
			AttachDataSource (dataSource);


			if (firstTime) {
				// First time
				PathSelector pathSel = dataSource.PathSelector;
				if (null == pathSel.Path) {
					// Use the local Path componenent or any Path in parent objects:
					Path path = GetComponentInParent<Path> ();
					if (null != path) {
						dataSource.PathSelector = pathSel.WithPath (path).WithDataSetId (0).WithUseSnapshot (false);
						//						pathSel.Path = path;
						//						pathSel.DataSetId = 0; // the default
						//						pathSel.UseSnapshot = false;
					}
				}
			}

			if (null == meshCollidersContainer) {
				meshCollidersContainer = new HierarchicalObjectContainer ();
			}
			meshCollidersContainer.Reset ();
			meshCollidersContainer.childContainerName = "_MeshColliders";
			meshCollidersContainer.ContainerHideFlags = (HideFlags.HideInHierarchy | HideFlags.NotEditable);
			meshCollidersContainer.LeafHideFlags = (HideFlags.HideInHierarchy | HideFlags.NotEditable);


			if (null == shapesContainer) {
				shapesContainer = new HierarchicalObjectContainer ();
			}
			shapesContainer.Reset ();
			shapesContainer.childContainerName = "_Shapes";
			shapesContainer.ContainerHideFlags = (HideFlags.HideInHierarchy | HideFlags.NotEditable);
			shapesContainer.LeafHideFlags = (HideFlags.HideInHierarchy | HideFlags.NotEditable);

			autoUpdateWithPath = false;
			autoUpdateMesh = false;
			
//			updateMeshFilter = true;
//			createMeshFilter = true;
//			createMeshRenderer = true;
//			
//			updateMeshCollider = true;
//			createMeshCollider = true;
			createShapes = true;
			createMeshColliders = false;

			// TODO should we reset meshgenerator? Maybe yes!
		}
		
		public void OnEnable ()
		{
//			Debug.Log ("OnEnable: " + this);
			if (null != dataSource) {
				AttachDataSource (dataSource);
			}

			if (null != MeshGeneratorInstance) {
				CallMeshGeneratorLifecycle (MeshGeneratorInstance.OnEnable, false);
			}
		}

		public void OnDisable ()
		{
			if (null != dataSource) {
				DetachDataSource (dataSource);
			}
			if (null != MeshGeneratorInstance) {
				CallMeshGeneratorLifecycle (MeshGeneratorInstance.OnDisable, false);
			}

		}

		public void OnDestory ()
		{
			// TODO we should destroy all related EditorPrefs!

			// OnDisable() is already called, so no need to do anything in here
//			primaryDataSource.OnDisable (this);

			if (null != dataSource) {
				dataSource.GetPathModifierContainer ().RemoveAllPathModifiers ();
			}
			if (null != _meshGeneratorInstance) {
				CallMeshGeneratorLifecycle (_meshGeneratorInstance.OnDestroy, false);
				try {
					_meshGeneratorInstance.RemoveMeshGeneratorEventHandler (MeshGeneratorChanged);
				} catch (Exception ex) {
					// NOP
					// TODO this smells
				}
				this._meshGeneratorInstance = null;
			}
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
//			if (null != generatedMesh) {
//				Gizmos.color = Color.green;
//				//Gizmos.DrawWireMesh(generatedMesh);
//			}
		}

#endregion Lifecycle Events


#region Serialization
		public void OnBeforeSerialize ()
		{
//			ParameterStore.OnBeforeSerialize ();
		}




		public void OnAfterDeserialize ()
		{
//			ParameterStore.OnAfterDeserialize ();

			if (null == dataSource) {
				dataSource = CreateDataSource ();
			}
			AttachDataSource (dataSource);

//			GetPathModifierContainer ().LoadConfiguration ();
			
			//			if (null != primaryDataSource) {
			//				primaryDataSource.OnEnable ();
			//			}


		}

	

		public void SaveMeshGeneratorParameters ()
		{
			IMeshGenerator mg = MeshGeneratorInstance;
			if (null != mg) {
				ParameterStore store = GetParameterStoreForMG (mg);
				mg.SaveParameters (store, GetReferenceContainer ());
			}
		}

#endregion Serialization


#region Event Handlers

		// Called by our PathDataSource
		private void OnDataSourceDataChanged (PathDataChangedEventArgs e)
		{
			// TODO what if we're destroyed?
			if (e.Stage == PathDataStage.Unprocessed) {
				// Source data has changed
				if (autoUpdateWithPath) {
					// Invalidate our processed data to trigger its reprocessing
					e.DataSource.InvalidateProcessedData (e.MetadataChanged, true);
				}
			} else if (e.Stage == PathDataStage.Processed) {
				MarkSlicesDirty ();
				if (autoUpdateMesh) {
					// TODO How dow we know if this is Primary or Colliders?
//					GenerateMesh (MeshGeneratorTarget.Primary);
					GenerateMesh ();
				}
			}
		}

		public void MeshGeneratorModified ()
		{
			MarkSlicesDirty ();
			if (autoUpdateMesh) {
				GenerateMesh ();
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


//		// Called when the attached TrackGenerator is changed
//		private void MeshGeneratorTypeChanged (MeshGeneratorTarget target)
//		{
//			// Force to create a new instance:
//			if (this._meshGeneratorInstances.ContainsKey (target)) {
//				this._meshGeneratorInstances.Remove (target);
//			}
//		}

#endregion Event Handlers

//		private MeshGeneratorContainer GetMeshGeneratorContainer (MeshGeneratorTarget target, bool createIfRequired)
//		{
//			MeshGeneratorContainer mgc;
//			if (meshGeneratorContainerMap.ContainsKey (target)) {
//				mgc = meshGeneratorContainerMap [target];
//			} else if (createIfRequired) {
//				mgc = new MeshGeneratorContainer (this, target);
//				meshGeneratorContainerMap [target] = mgc;
//			} else {
//				mgc = null;
//			}
//			return mgc;
//		}
		private static PathDataSource CreateDataSource ()
		{
			PathDataSource ds = new PathDataSource (1);
			ds.Name = "Default";
			return ds;
		}

		private static MethodInfo FindMethod (object obj, string name, params Type[] types)
		{
			return obj.GetType ().GetMethod (
				name, 
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, types, null);
		}



		private void AttachDataSource (PathDataSource ds)
		{

			MethodInfo m = FindMethod (ds, "OnAttach", typeof(IPathDataSourceContainer));
			if (null == m) {
				Debug.LogWarning ("No lifecycle method OnAttach(IPathDataSourceContainer) found on PathDataSource: " + ds);
			} else {
				m.Invoke (ds, new object[] {new PathDataSourceContainer (this)});

				ds.DataChanged -= OnDataSourceDataChanged;
				ds.DataChanged += OnDataSourceDataChanged;

			}
		}

		private void DetachDataSource (PathDataSource ds)
		{
			MethodInfo m = FindMethod (ds, "OnDetach", typeof(IPathDataSourceContainer));
			if (null == m) {
				Debug.LogWarning ("No lifecycle method OnDetach(IPathDataSourceContainer) found on PathDataSource: " + ds);
			} else {
				m.Invoke (ds, new object[] {new PathDataSourceContainer (this)});

				ds.DataChanged -= OnDataSourceDataChanged;

			}
		}



		public IReferenceContainer GetReferenceContainer ()
		{
			return this.referenceContainer;
		}




//		public PathMeshSlice[] GetTrackSlices ()
//		{
//			return null;
////			MeshGeneratorContainer mgc = GetMeshGeneratorContainer (target, true);
////			IMeshGenerator mg = mgc.MeshGeneratorInstance;
////
////			generatedSlices = mg.CreateSlices (MeshDataSource, false);
////			}
////			TrackSlice[] arr = new TrackSlice[generatedSlices.Length];
////			Array.Copy (generatedSlices, arr, generatedSlices.Length);
////			return arr;
//		}

		private void MarkSlicesDirty ()
		{
//			this.generatedSlices = null;
		}


//		private IPathInfo DoGetPathInfo ()
//		{
//			IPathData d = primaryDataSource.PathSelector.PathData;
//			return (null != d) ? d.GetPathInfo () : null;
//		}



//		public DefaultPathModifierContainer GetPathModifierContainer ()
//		{
//			if (null == pathModifierContainer) {
//				pathModifierContainer = CreatePathModifierContainer ();
//			}
//			return pathModifierContainer;
//		}
		

		

//		internal ParameterStore GetPrimaryMeshGeneratorParameterStore ()
//		{
//			return DoGetMeshGeneratorParameterStore ("Primary");
//		}
//		internal ParameterStore GetColliderMeshGeneratorParameterStore ()
//		{
//			return DoGetMeshGeneratorParameterStore ("Colliders");
//		}




		
//		public bool GenerateTrackColliders ()
//		{
//			IMeshGenerator mg = GetMeshGeneratorInstance (UsePrimaryMeshGeneratorForColliders ? MeshGeneratorTarget.Primary : MeshGeneratorTarget.Colliders);
//			bool generated = 
//				DoGenerateMesh (mg, ref generatedColliderMesh, ColliderDataSource, this.name + "ColliderMesh");
//			if (generated) {
//				// Upate Mesh Collider component
//				MeshCollider mc = GetComponent<MeshCollider> ();
//				if (null != mc) {
//					mc.sharedMesh = generatedColliderMesh;
//					// disable / re-enable to force changes to be applied
//					if (mc.enabled) {
//						mc.enabled = false;
//						mc.enabled = true;
//					}
//				}
//				
//				//			MeshCollider mc = track.GetComponent<MeshCollider> ();
//				//			if (null == mc) {
//				//				mc = track.gameObject.AddComponent<MeshCollider> ();
//				//			}
//				//
//				//			xxx ();
//				//			mc.sharedMesh = m;
//			}
//			return generated;
//		}
		
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
		

		//		public void OnAfterDeserialize ()
		//		{
		//			// Force reload of MeshGeneratorInstances:
		//			this._meshGeneratorInstance = null;
		//		}
		//		
		//		private IMeshGenerator CreateMeshGeneratorInstance ()
		//		{
		//			IMeshGenerator mg;
		//			if (null != meshGeneratorType && meshGeneratorType.Length > 0) {
		//				// Create the instance
		//				mg = (IMeshGenerator)Activator.CreateInstance (Type.GetType (meshGeneratorType));
		//				// Load parameters
		//				// Load params:
		//				//					parameters.OnAfterDeserialize ();
		//				// TODO we must prefix the store!
		//				mg.LoadParameters (GetParameterStoreForMG (mg));
		//				_meshGeneratorInstance = mg;
		//			} else {
		//				mg = null;
		//			}
		//			return mg;
		//		}
		//		
		//		public void xSaveMeshGeneratorParameters (MeshGeneratorTarget target)
		//		{
		//			IMeshGenerator mg = MeshGeneratorInstance;
		//			if (null != mg) {
		//				ParameterStore store = GetParameterStoreForMG (mg);
		//				mg.SaveParameters (store);
		//			}
		//		}
		//
		//
		//
		//		private bool DoGenerateMesh (out Mesh mesh)
		//		{
		//			string initialMeshName = GetObjectName () + this.Name + "Mesh";
		//			
		//			IMeshGenerator mg = MeshGeneratorInstance;
		//			
		//			mesh = this.generatedMesh;
		//			
		//			
		//			if (null == mg) {
		//				Debug.LogError ("MeshGenerator is null");
		//				return false;
		//			} else {
		//				if (mesh == null) {
		//					Debug.Log ("Creating new Mesh instance");
		//					mesh = new Mesh ();
		//					
		//				} else {
		//					Debug.Log ("Updating existing Mesh instance");
		//					mesh.Clear ();
		//				}
		//				
		//				// Create initial Mesh name:
		//				if (StringUtil.IsEmpty (mesh.name)) {
		//					mesh.name = initialMeshName;
		//				}
		//				mesh = mg.CreateMesh (DataSource, mesh);
		//				this.generatedMesh = mesh;
		//				return true;
		//			}
		//		}
		//		
		//		
		//		/// <summary>
		//		/// Generates a Mesh with the current TrackGeneratorInstance and assigns it to 
		//		/// "generatedMesh" property.
		//		/// </summary>
		public bool GenerateMesh ()
		{
					
			string initialMeshName = string.Format ("{0}Mesh", this.name);
						
			IMeshGenerator mg = MeshGeneratorInstance;
						
						
			if (null == mg) {
				Debug.LogError ("MeshGenerator is null");
				return false;
			} else {
//				if (generatedMesh == null) {
//					Debug.Log ("Creating new Mesh instance");
//					generatedMesh = new Mesh ();
//								
//				} else {
//					Debug.Log ("Updating existing Mesh instance");
//					generatedMesh.Clear ();
//				}
				int meshCount = mg.GetMeshCount ();
				if (null == generatedMeshes) {
					generatedMeshes = new Mesh[meshCount];
				} else if (generatedMeshes.Length != meshCount) {
					// Trim / resice
					Array.Resize (ref generatedMeshes, meshCount);
				}
				int newMeshesCount = 0;
				for (int i = 0; i < meshCount; i++) {
					if (generatedMeshes [i] == null) {
						generatedMeshes [i] = new Mesh ();
						newMeshesCount++;
					} else {
						generatedMeshes [i].Clear ();
					}

					// Create initial Mesh name:
					if (StringUtil.IsEmpty (generatedMeshes [i].name)) {
						generatedMeshes [i].name = initialMeshName + "_" + i;
					}
				}
				if (newMeshesCount > 0) {
					Debug.LogFormat ("Creating {0} new and updating {1} existing Mesh instance(s)", 
					                 newMeshesCount, meshCount - newMeshesCount);
				} else {
					Debug.LogFormat ("Updating {0} existing Mesh instance(s)", 
					                 meshCount - newMeshesCount);
				}
								

				mg.CreateMeshes (DataSource, generatedMeshes);

				if (createShapes) {
					UpdateShapes ();
				}

				if (createMeshColliders) {
					UpdateMeshColliders ();
				}

				return true;
			}
		}

		private static void DoUpdateMeshRenderer (int meshIndex, GameObject childObj, object context)
		{
			PathMeshGenerator obj = (PathMeshGenerator)context;
			MeshFilter mf = childObj.GetComponent<MeshFilter> ();
			if (null == mf) {
				mf = childObj.AddComponent<MeshFilter> ();
			}
			mf.sharedMesh = obj.generatedMeshes [meshIndex];

			MeshRenderer mr = childObj.GetComponent<MeshRenderer> ();
			if (null == mr) {
				mr = childObj.AddComponent<MeshRenderer> ();
			}

			// Assign materials
			Material[] materials = obj.ShapeMaterials;
			int matCount = materials.Length;
			IMeshGenerator mg = obj.MeshGeneratorInstance;
			int[] submeshIndex = new int[matCount];
			int maxSubmeshIndex = 0;
			for (int i = 0; i < matCount; i++) {
				submeshIndex [i] = mg.GetMaterialSlotSubmeshIndex (i);
				if (submeshIndex [i] > maxSubmeshIndex) {
					maxSubmeshIndex = submeshIndex [i];
				}
			}
			int sharedMatCount = maxSubmeshIndex + 1;
			Material[] sharedMats = new Material[sharedMatCount];
			for (int i = 0; i < submeshIndex.Length; i++) {
				sharedMats [submeshIndex [i]] = materials [i];
			}
			mr.sharedMaterials = sharedMats;

		}

		private void UpdateShapes ()
		{
			shapesContainer.CreateChildren (
				gameObject,
				() => generatedMeshes.Length,
				DoUpdateMeshRenderer,
				this);

		}

		private static void DoUpdateMeshCollider (int i, GameObject childObj, object context)
		{
			PathMeshGenerator obj = (PathMeshGenerator)context;
			MeshCollider mc = childObj.GetComponent<MeshCollider> ();
			if (null == mc) {
				mc = childObj.AddComponent<MeshCollider> ();
			}
			if (null != mc) {
				mc.sharedMesh = obj.generatedMeshes [i];
				// Force refresh:
				bool mcWasenabled = mc.enabled;
				if (mcWasenabled) {
					mc.enabled = false;
					mc.enabled = true;
				}
			}
		}
		private void UpdateMeshColliders ()
		{
			meshCollidersContainer.CreateChildren (
				gameObject,
				() => generatedMeshes.Length,
				DoUpdateMeshCollider,
				this);

		}
	}
	public class MeshObjectVisibility
	{
		public HierarchicalObjectContainer container;
		public MeshObjectVisibility (HierarchicalObjectContainer container)
		{
			this.container = container;
		}
		private void DoSetLeafHideFlags (HideFlags flags, bool value)
		{
			if (value) {
				container.LeafHideFlags &= ~flags;
			} else {
				container.LeafHideFlags |= flags;
			}
		}
		private void DoSetContainerHideFlags (HideFlags flags, bool value)
		{
			if (value) {
				container.ContainerHideFlags &= ~flags;
			} else {
				container.ContainerHideFlags |= flags;
			}
		}
		
		public bool ContainerVisibleInHierarchy {
			get {
				return 0 == (container.ContainerHideFlags & HideFlags.HideInHierarchy);
			}
			set {
				DoSetContainerHideFlags (HideFlags.HideInHierarchy, value);
			}
		}
		public bool LeafsVisibleInHierarchy {
			get {
				return 0 == (container.LeafHideFlags & HideFlags.HideInHierarchy);
			}
			set {
				DoSetLeafHideFlags (HideFlags.HideInHierarchy, value);
			}
		}
		public bool ContainerEditable {
			get {
				return 0 == (container.ContainerHideFlags & HideFlags.NotEditable);
			}
			set {
				DoSetContainerHideFlags (HideFlags.NotEditable, value);
			}
		}
		public bool LeafsEditable {
			get {
				return 0 == (container.LeafHideFlags & HideFlags.NotEditable);
			}
			set {
				DoSetLeafHideFlags (HideFlags.NotEditable, value);
			}
		}
		
	}
}
