using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using System;
using System.Collections.Generic;

using Util;
using Util.Editor;
using Paths.Editor;
using Paths.MeshGenerator;

namespace Paths.MeshGenerator.Editor
{

	[CustomEditor(typeof(PathMeshGenerator))]
	public class PathMeshGeneratorEditor : UnityEditor.Editor, IPluginEditorHost
	{
		private new PathMeshGenerator target;

		private Type[] availableMeshGeneratorTypes;
		private string[] availableMeshGeneratorDisplayNames;

		// TODO is this used?
		private bool meshDirty;

		// Type to editor instance
//		private Dictionary<string, IMeshGeneratorEditor> meshGeneratorEditorMap;

		private IMeshGeneratorEditor meshGeneratorEditor;

		ContextEditorPrefs editorPrefs;

//		private SerializedProperty meshGeneratorTypeProperty;
//
//		private SerializedProperty updateMeshFilterProperty;
//		private SerializedProperty createMeshFilterProperty;
//		private SerializedProperty createMeshRenderedProperty;
//		private SerializedProperty updateMeshColliderProperty;
//		private SerializedProperty createMeshColliderProperty;

		private Dictionary<object, IPluginEditor> _cachedPluginEditors = new Dictionary<object, IPluginEditor> ();

		public PathMeshGeneratorEditor ()
		{
		}

		void OnEnable ()
		{
			this.target = base.target as PathMeshGenerator;

			// TODO should we share editor preferences between instances? Maybe not!
			this.editorPrefs = 
				new ContextEditorPrefs ("PathMeshGenerator[" + target.GetInstanceID () + "]");

//			meshGeneratorTypeProperty = serializedObject.FindProperty ("meshGeneratorType");
//
//			updateMeshFilterProperty = serializedObject.FindProperty ("updateMeshFilter");
//			createMeshFilterProperty = serializedObject.FindProperty ("createMeshFilter");
//			createMeshRenderedProperty = serializedObject.FindProperty ("createMeshRendered");
//			updateMeshColliderProperty = serializedObject.FindProperty ("updateMeshCollider");
//			createMeshColliderProperty = serializedObject.FindProperty ("createMeshCollider");


			InitMeshGeneratorTypes ();
		}

		void OnDisable ()
		{
		}


		public void SetEditorFor (object pluginInstance, IPluginEditor editor)
		{
			if (null == editor) {
				if (_cachedPluginEditors.ContainsKey (pluginInstance)) {
					_cachedPluginEditors.Remove (pluginInstance);
				}
			} else {
				_cachedPluginEditors [pluginInstance] = editor;
			}
		}
		
		public IPluginEditor GetEditorFor (object pluginInstance)
		{
			if (_cachedPluginEditors.ContainsKey (pluginInstance)) {
				return _cachedPluginEditors [pluginInstance];
			} else {
				return null;
			}
		}

		public override void OnInspectorGUI ()
		{
			//EditorUtility.
			DrawDefaultInspectorGUI ();
//			serializedObject.ApplyModifiedProperties ();
		}


		private void MeshGeneratorModified ()
		{
			// TODO refactor the SetMeshDirty(). ... ConfigurationChanged --- PathModifiersChanged etc system
			EditorUtility.SetDirty (target);
			//SliceConfigurationChanged();
			SetMeshDirty ();

			// Save the configuration
			target.SaveMeshGeneratorParameters ();

			// Notify about the change
			target.MeshGeneratorModified ();

//			track.ConfigurationChanged (true, false);
			SceneView.RepaintAll ();
		}



		void SetMeshDirty ()
		{
			this.meshDirty = true;
		}

		private void InitMeshGeneratorTypes ()
		{
			this.availableMeshGeneratorTypes = AbstractMeshGenerator.FindMeshGeneratorTypes ();
			this.availableMeshGeneratorDisplayNames = new string[availableMeshGeneratorTypes.Length];
			for (int i = 0; i < availableMeshGeneratorTypes.Length; i++) {
				// Form generator name from the type name by removing "TrackGenerator" suffix (if any)
				string n = StringUtil.RemoveStringTail (availableMeshGeneratorTypes [i].Name, "Generator", 1);
				n = StringUtil.RemoveStringTail (n, "Mesh", 1);
				availableMeshGeneratorDisplayNames [i] = n;
			}
		}

		private int FindCurrentMeshGeneratorTypeIndex ()
		{
			PathMeshGenerator track = target as PathMeshGenerator;
        
			// Find selected index
			int selectedTgIndex = -1;
			for (int i = 0; i < availableMeshGeneratorTypes.Length; i++) {
				string n = availableMeshGeneratorTypes [i].FullName;
				if (n == track.MeshGeneratorType) {
					selectedTgIndex = i;
					break;
				}
			}
			return selectedTgIndex;
		}

//		static Type[] FindMeshGeneratorEditorTypes ()
//		{
//			Type[] editorTypes = Util.TypeUtil.FindTypesHavingAttribute (typeof(MeshGeneratorCustomEditor));
//			return editorTypes;
//		}

//		static Type FindMeshGeneratorEditorType (Type meshGeneratorType)
//		{
//			Type[] editorTypes = FindMeshGeneratorEditorTypes ();
//			for (int i = 0; i < editorTypes.Length; i++) {
//				object[] attrs = editorTypes [i].GetCustomAttributes (typeof(MeshGeneratorCustomEditor), true);
//				for (int j = 0; j < attrs.Length; j++) {
//					MeshGeneratorCustomEditor tge = (MeshGeneratorCustomEditor)attrs [j];
//					if (meshGeneratorType == tge.InspectedType) {
//						return editorTypes [i];
//					}
//				}
//			}
//			// Look for base type:
//			Type baseType = meshGeneratorType.BaseType;
//			return (null != baseType) ? FindMeshGeneratorEditorType (baseType) : null;
//		}

//		private IMeshGeneratorEditor GetMeshGeneratorEditor (IMeshGenerator mg)
//		{
//
//			IMeshGeneratorEditor cachedEditor;
//			if (null != mg) {
//				string typeName = mg.GetType ().FullName;
//				cachedEditor = meshGeneratorEditorMap.ContainsKey (typeName) ? meshGeneratorEditorMap [typeName] : null;
//
//				if (null == cachedEditor) {
//					cachedEditor = DoCreateMeshGeneratorEditor (mg);
//					meshGeneratorEditorMap [typeName] = cachedEditor;
//				}
//			} else {
//				cachedEditor = null;
//			}
//			return cachedEditor;
//		}
//	
//
//
//		private IMeshGeneratorEditor DoCreateMeshGeneratorEditor (IMeshGenerator mg)
//		{
//			IMeshGeneratorEditor editorInstance;
//			Type tgEditorType = FindMeshGeneratorEditorType (mg.GetType ());
//			if (null != tgEditorType) {
//				if (!typeof(IMeshGeneratorEditor).IsAssignableFrom (tgEditorType)) {
//					Debug.LogError ("Class '" + tgEditorType + "' has attribute TrackGeneratorCustomEditor but it doesn't implement the ITrackGeneratorEditor interface");
//					editorInstance = null;
//				} else if (typeof(ScriptableObject).IsAssignableFrom (tgEditorType)) {
//					editorInstance = (IMeshGeneratorEditor)ScriptableObject.CreateInstance (tgEditorType);
//				} else {
//					editorInstance = (IMeshGeneratorEditor)Activator.CreateInstance (tgEditorType);
//				}
//				editorInstance.OnEnable (new MeshGeneratorEditorContext (mg, track, this));
//			} else {
//				editorInstance = null;
//			}
//			return editorInstance;
//		}

		private void PathModifiersChanged ()
		{
			// TODO invalidate the processed data
			//			track.ConfigurationChanged (false, true);

//			track.GetPathModifierContainer ().ConfigurationChanged ();
			target.DataSource.GetPathModifierContainer ().ConfigurationChanged ();

			EditorUtility.SetDirty (target);
			//			track.OnPathModifiersChanged ();
		}


		private static readonly GUIContent[] ToolbarContents = new GUIContent[] {
			new GUIContent ("General", "Track Parameters"),
			new GUIContent ("Mesh", "Mesh Configuration"),
			new GUIContent ("Settings", "Track Settings"),


		};

		private enum ToolbarSheet : int
		{
			General = 0,
			Mesh,
			Settings,
		};

		public void DrawDefaultInspectorGUI ()
		{

			ToolbarSheet selectedSheet = (ToolbarSheet)editorPrefs.GetInt ("selectedSheet", (int)ToolbarSheet.General);
			selectedSheet = (ToolbarSheet)GUILayout.Toolbar ((int)selectedSheet, ToolbarContents);


			if ((int)selectedSheet < 0 || (int)selectedSheet >= ToolbarContents.Length) {
				Debug.LogError ("Unknown ToolbarSheet selected: " + selectedSheet);
				// Revert to first:
				selectedSheet = (int)0;
			}
			editorPrefs.SetInt ("selectedSheet", (int)selectedSheet);


			// DRAW COMMON HEADER
//			if (GUILayout.Button ("Data Sources", EditorStyles.miniButton)) {
//				dataSourcesVisible.target = !dataSourcesVisible.value;
//			}
//			
//			if (EditorGUILayout.BeginFadeGroup (dataSourcesVisible.faded)) {
//				DrawDataSourcesInspectorSheet ();
//			}
//			EditorGUILayout.EndFadeGroup ();


			switch (selectedSheet) {
			case ToolbarSheet.General:
				DrawGeneralInspectorSheet ();
				break;
			case ToolbarSheet.Mesh:
				DrawMeshInspectorSheet ();
				break;
			case ToolbarSheet.Settings:
				DrawSettingsInspectorSheet ();
				break;
			}

			// TODO why do we always save TG params?
//			track.SaveMeshGeneratorParameters (Track.MeshGeneratorTarget.Primary);
//			track.SaveMeshGeneratorParameters (Track.MeshGeneratorTarget.Colliders);

		}


		void DrawGeneralInspectorSheet ()
		{
			
		}

		private void DrawDataSourceConfigurationGUI ()
		{
			PathDataSource ds = target.DataSource;
			PathSelector pathSelector = ds.PathSelector;
			if (PathEditorUtil.DrawPathDataSelection ("Path Selection", ref pathSelector, (snapshotName) => {
				ds.PathSelector = ds.PathSelector.WithSnapshotName (snapshotName);})) {
				ds.PathSelector = pathSelector;
			}
					
			pathSelector = ds.PathSelector;
					

			Path path = pathSelector.Path;
			PathDataSourceWrapper pathDataWrapper = new PathDataSourceWrapper (ds);
			PathModifierEditorContext context = new PathModifierEditorContext (
						pathDataWrapper, path, this, PathModifiersChanged, editorPrefs.WithPrefix ("PathModifiers"));
			PathModifierEditorUtil.DrawPathModifiersInspector (context, target, 
				() => EditorGUILayout.HelpBox ("Track's Path Modifiers can be used to modify the path before it's feed to the Track Generator. Modifiers will not modify the original Path.", MessageType.Info));
		}

		IMeshGeneratorEditor GetMeshGeneratorEditor ()
		{
			if (null == meshGeneratorEditor) {
				IMeshGenerator mg = target.MeshGeneratorInstance;
				
				PluginResolver ctr = PluginResolver.ForPluginType (typeof(IMeshGenerator), typeof(IMeshGeneratorEditor));
				meshGeneratorEditor = (IMeshGeneratorEditor)ctr.CreatePluginEditorInstance (mg);
			}
			return meshGeneratorEditor;
		}

		IMeshGenerator DrawMeshGeneratorSelectionGUI ()
		{
			EditorGUI.BeginChangeCheck ();
			int tgIndex = FindCurrentMeshGeneratorTypeIndex ();
			tgIndex = EditorGUILayout.Popup ("Mesh Generator", tgIndex, availableMeshGeneratorDisplayNames);
			if (EditorGUI.EndChangeCheck ()) {
				target.MeshGeneratorType = availableMeshGeneratorTypes [tgIndex].FullName;
//				meshGeneratorTypeProperty.stringValue = target.MeshGeneratorType;
				EditorUtility.SetDirty (target);

				// Force recreation of the editor
				this.meshGeneratorEditor = null;
				MeshGeneratorModified ();
			}

			IMeshGenerator mg = target.MeshGeneratorInstance;

//			IMeshGeneratorEditor mgEditor = GetMeshGeneratorEditor (mg);
//			if (null != mgEditor) {
//				
//				MeshGeneratorEditorContext ctx = new MeshGeneratorEditorContext (mg, track, this);
//				mgEditor.DrawInspectorGUI (ctx);
//				EditorGUILayout.Separator ();
//			}
			return mg;
		}

		void DrawMeshGeneratorConfigurationGUI ()
		{
			IMeshGenerator mg = target.MeshGeneratorInstance;
//			Debug.LogWarning ("MeshGeneratorInstance: " + mg);
			
			IMeshGeneratorEditor mge = GetMeshGeneratorEditor ();
			if (null != mge) {
				MeshGeneratorEditorContext mgeContext = new MeshGeneratorEditorContext (
					mg, target, this, MeshGeneratorModified, editorPrefs);
				mge.DrawInspectorGUI (mgeContext);
				EditorGUILayout.Separator ();
			}

			DrawMaterialsSelectionGUI ();

//			EditorGUILayout.ObjectField(

			//[SerializeField]

		}
		void DrawMaterialsSelectionGUI ()
		{
			IMeshGenerator mg = target.MeshGeneratorInstance;
			if (null != mg) {
				EditorGUILayout.Foldout (true, "Materials");
				EditorGUI.indentLevel++;

				int matCount = mg.GetMaterialSlotCount ();
				for (int i = 0; i < matCount; i++) {
					string matName = mg.GetMaterialSlotName (i);
					EditorGUI.BeginChangeCheck ();
					target.ShapeMaterials [i] = EditorGUILayout.ObjectField (matName, 
						target.ShapeMaterials [i], typeof(Material), true) as Material;
					if (EditorGUI.EndChangeCheck ()) {
						EditorUtility.SetDirty (target);

						MeshGeneratorModified ();
					}
				}

				EditorGUI.indentLevel--;
			}
		}

		void DrawToggle (string label, ref bool value)
		{
			//				EditorGUI.BeginDisabledGroup (null == target.GetComponent<MeshFilter> ());
			EditorGUI.BeginChangeCheck ();
			value = EditorGUILayout.Toggle (label, value);
			if (EditorGUI.EndChangeCheck ()) {
				Undo.RecordObject (target, "Toggle " + label);
				EditorUtility.SetDirty (target);
			}
			//				EditorGUILayout.PropertyField (updateMeshFilterProperty);
			//				EditorGUI.EndDisabledGroup ();
		}

		bool meshComponentsSectionExpanded = true;
		void DrawMeshInspectorSheet ()
		{
			DrawDataSourceConfigurationGUI ();
			IMeshGenerator mg = DrawMeshGeneratorSelectionGUI ();
			if (null != mg) {
				DrawMeshGeneratorConfigurationGUI ();
			}

			EditorGUILayout.Separator ();

			meshComponentsSectionExpanded = EditorGUILayout.Foldout (meshComponentsSectionExpanded, "Mesh Components");
			if (meshComponentsSectionExpanded) {
				EditorGUI.indentLevel++;

				DrawToggle ("Create Shapes", ref target.createShapes);
				DrawToggle ("Create Mesh Colliders", ref target.createMeshColliders);

				EditorGUI.indentLevel--;
			}

			EditorGUI.BeginDisabledGroup (null == mg);
			string generateMeshLabel = "Generate Mesh";
			if (meshDirty) {
				generateMeshLabel += " (*)";
			}
			
			if (GUILayout.Button (new GUIContent (generateMeshLabel, "Generate Mesh"))) {
				GenerateMesh ();
				
				//Undo.RecordObject(path, "Generate Tunnel Mesh");
				//path.AddSegment();
				//EditorUtility.SetDirty(path);
			}
			EditorGUI.EndDisabledGroup ();
			
			
//			EditorGUI.BeginDisabledGroup (null == track.generatedMesh);
//			if (GUILayout.Button ("Clear Mesh")) {
//				ClearTrackMesh ();
//				//Undo.RecordObject(path, "Generate Tunnel Mesh");
//				//path.AddSegment();
//				//EditorUtility.SetDirty(path);
//			}
//			
//			if (GUILayout.Button ("Save Mesh Asset")) {
//				SaveGeneratedMesh ();
//				//Undo.RecordObject(path, "Generate Tunnel Mesh");
//				//path.AddSegment();
//				//EditorUtility.SetDirty(path);
//			}
//			EditorGUI.EndDisabledGroup ();
		}




		void DrawSettingsInspectorSheet ()
		{
			EditorGUI.BeginChangeCheck ();
			target.AutomaticUpdateWithPath = EditorGUILayout.Toggle ("Automatic Update with Path", target.AutomaticUpdateWithPath);
			if (EditorGUI.EndChangeCheck ()) {
				EditorUtility.SetDirty (target);
			}
		
			//EditorGUI.BeginDisabledGroup (track.AutomaticUpdateWithPath == false);
			EditorGUI.BeginChangeCheck ();
			target.AutomaticMeshUpdate = EditorGUILayout.Toggle ("Automatic Mesh Update", target.AutomaticMeshUpdate);
			if (EditorGUI.EndChangeCheck ()) {
				EditorUtility.SetDirty (target);
			}

			EditorGUILayout.Separator ();

			DrawMeshObjectsVisibilityGUI (target.ShapeObjectsVisibility, "Shape", target.createShapes);
			DrawMeshObjectsVisibilityGUI (target.ColliderObjectsVisibility, "Collider", target.createMeshColliders);

		}

		void DrawMeshObjectsVisibilityGUI (MeshObjectVisibility visibility, string meshObjectName, bool areGenerated)
		{

			EditorGUILayout.LabelField (meshObjectName + " Objects Visiblity");

			EditorGUI.indentLevel++;

			if (!areGenerated) {
				EditorGUILayout.HelpBox ("Note: this Mesh Generator is not currently configured to generate " + meshObjectName + " objects.", MessageType.Info);
			}

			EditorGUI.BeginDisabledGroup (!areGenerated);

			// Collider Objects Visibility
			//  Show Container Object
			//    Container Object Editable
			//  Show Leaf Objects
			//    Leaf Objects Editable
			//
			EditorGUI.BeginChangeCheck ();
			bool containerVisible = visibility.ContainerVisibleInHierarchy;
			containerVisible = EditorGUILayout.Toggle ("Show Container Object", containerVisible);
			if (EditorGUI.EndChangeCheck ()) {
				visibility.ContainerVisibleInHierarchy = containerVisible;
				EditorUtility.SetDirty (target);
			}

			EditorGUI.BeginDisabledGroup (!containerVisible);

			EditorGUI.indentLevel++;
			EditorGUI.BeginChangeCheck ();
			bool containerEditable = visibility.ContainerEditable;
			containerEditable = EditorGUILayout.Toggle ("Container Object Editable", containerEditable);
			if (EditorGUI.EndChangeCheck ()) {
				visibility.ContainerEditable = containerEditable;
				EditorUtility.SetDirty (target);
			}
			EditorGUI.indentLevel--;

			EditorGUI.BeginChangeCheck ();
			bool leafsVisible = visibility.LeafsVisibleInHierarchy;
			leafsVisible = EditorGUILayout.Toggle ("Show Leaf Objects", leafsVisible);
			if (EditorGUI.EndChangeCheck ()) {
				visibility.LeafsVisibleInHierarchy = leafsVisible;
				EditorUtility.SetDirty (target);
			}
			EditorGUI.BeginDisabledGroup (!leafsVisible);
			EditorGUI.indentLevel++;
			EditorGUI.BeginChangeCheck ();
			bool leafsEditable = visibility.LeafsEditable;
			leafsEditable = EditorGUILayout.Toggle ("Leaf Objects Editable", leafsEditable);
			if (EditorGUI.EndChangeCheck ()) {
				visibility.LeafsEditable = leafsEditable;
				EditorUtility.SetDirty (target);
			}
			EditorGUI.indentLevel--;
			EditorGUI.EndDisabledGroup (); // !leafsVisible
			EditorGUI.EndDisabledGroup (); // !containerVisible

			EditorGUI.EndDisabledGroup ();

			EditorGUI.indentLevel--;

		}

		protected void GenerateMesh ()
		{
			// Add MeshFilter and MeshRenderer if not already added
//			MeshFilter mf = target.gameObject.GetComponent<MeshFilter> ();
//			if (null == mf) {
//				mf = target.gameObject.AddComponent<MeshFilter> ();
//			}
//			MeshRenderer mr = target.gameObject.GetComponent<MeshRenderer> ();
//			if (null == mr) {
//				mr = target.gameObject.AddComponent<MeshRenderer> ();
//			}
			target.GenerateMesh ();
//			track.GenerateMesh (Track.MeshGeneratorTarget.Primary);
			this.meshDirty = false;
			SceneView.RepaintAll ();
		}

//  internal void SliceConfigurationChanged() {
//      this.slices = null;
//
//      SetMeshDirty();
//  }

		/*public void GenerateFooColiders() {
        Track track = target as Track;

        GameObject fooCollidersObj;
        Transform fooColliders = track.transform.FindChild("FooColliders");
        if (null == fooColliders) {
            fooCollidersObj = new GameObject("FooColliders");
            fooColliders = fooCollidersObj.transform;
            fooColliders.parent = track.transform;
        } else {
            fooCollidersObj = fooColliders.gameObject;
        }

        // TODO should we prompt the user about this?
        Collider[] oldColliders = fooCollidersObj.GetComponentsInChildren<Collider>();
        for (int i = oldColliders.Length - 1; i >= 0; i--) {
            Object.DestroyImmediate(oldColliders[i]);
        }

        Path path = track.Path;
        Vector3[] points = path.Points;
        for (int i = 1; i < points.Length; i++) {
            Vector3 pt0 = points[i - 1];
            Vector3 pt1 = points[i];
            float len = (pt1 - pt0).magnitude;

            BoxCollider c = fooCollidersObj.AddComponent<BoxCollider>();
            c.size = new Vector3(1f, 1f, len);

            c.center = points[i];
        }

    }*/




//		public void ClearTrackMesh ()
//		{
//
//			Track track = target as Track;
//			//NewPath path = track.Path;
//			track.generatedMesh = null;
//			SceneView.RepaintAll ();
//
//		}

//		public void SaveGeneratedMesh ()
//		{
//			// TODO THIS IS NOT WORKING ANY MORE?
//        
//			Track track = target as Track;
////      Path path = track.Path;
//			IMeshGenerator tg = track.GetMeshGeneratorInstance (Track.MeshGeneratorTarget.Primary);
//
//			// Asset folder:
//			// TODO use previously saved!
//
//			string assetFolder;
//			string filePath = tg.GetSavedMeshAssetPath ();
//			if (StringUtil.IsEmpty (filePath)) {
//				filePath = "";
//				assetFolder = filePath;
//			} else {
//
//				if (!StringUtil.IsEmpty (filePath)) {
//					// Remove extension
//					int extBegin = filePath.LastIndexOf ('.');
//					if (extBegin > 0) {
//						filePath = filePath.Substring (0, extBegin);
//					}
//
//					assetFolder = MiscUtil.GetFolderName (filePath);
//				} else {
//					assetFolder = "Assets";
//				}
//			}
//
//			if (!AssetDatabase.IsValidFolder (assetFolder)) {
//				assetFolder = "Assets";
//				if (!AssetDatabase.IsValidFolder (assetFolder)) {
//					assetFolder = "";
//				}
//			}
//
//			string assetFileName = MiscUtil.GetFileName (filePath);
//			if (StringUtil.IsEmpty (assetFileName)) {
//				assetFileName = track.generatedMesh.name;
//			}
//
//			filePath = EditorUtility.SaveFilePanelInProject (
//            "Save Mesh", assetFileName, "asset", "Save generated Mesh asset", assetFolder);
//			if (null == filePath || filePath.Length == 0) {
//				return;
//			}
//
//			assetFolder = MiscUtil.GetFolderName (filePath);
//			assetFileName = MiscUtil.GetFileName (filePath);
//
//			if (tg.GetSavedMeshAssetPath () != filePath) {
//				tg.SetSavedMeshAssetPath (filePath);
//				EditorUtility.SetDirty (track);
//			}
//
//			/*
//        Mesh mesh = (Mesh)AssetDatabase.LoadAssetAtPath (prefabPath, typeof(GameObject));
//        if (!mesh) {
//            mesh = new Mesh();
//            AssetDatabase.AddObjectToAsset(mesh, prefabPath);
//        } else {
//            mesh.Clear();
//        }*/
//        
//			Mesh mesh = (Mesh)AssetDatabase.LoadAssetAtPath (filePath, typeof(Mesh));
//			if (null == mesh) {
//				mesh = new Mesh ();
//				AssetDatabase.CreateAsset (mesh, filePath);
//			}
//			//AssetDatabase.SaveAssets();
//			mesh.Clear ();
//
//			// copy vertices, normals, uv's and triangles:
//			mesh.vertices = track.generatedMesh.vertices;
//			mesh.normals = track.generatedMesh.normals;
//			mesh.uv = track.generatedMesh.uv;
//			mesh.triangles = track.generatedMesh.triangles;
//
//			//SelectedTrackGenerator.CreateMesh(path, mesh);
//        
//			//AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(mesh));
//			AssetDatabase.SaveAssets ();
//			Debug.Log ("Saved Mesh to " + AssetDatabase.GetAssetPath (mesh));
//		}

		void OnSceneGUI ()
		{
			if (!target.isActiveAndEnabled) {
				// TODO add a configuration parameter for this behaviour!
				return;
			}
		
			//DrawPath();
//			IMeshGenerator mg = track.GetMeshGeneratorInstance (Track.MeshGeneratorTarget.Primary);
//			IMeshGeneratorEditor tgEditor = GetMeshGeneratorEditor (mg, Track.MeshGeneratorTarget.Primary);
//			if (null != tgEditor) {
//
//				tgEditor.DrawSceneGUI (new MeshGeneratorEditorContext (mg, track, this));
//			}
		
		}

//		private void DrawTrackSlices ()
//		{
//			// Draw generated slices
//			IMeshGenerator mg = track.GetMeshGeneratorInstance (Track.MeshGeneratorTarget.Primary);
//			if (null != mg) {
//				MeshRenderer mr = track.GetComponent<MeshRenderer> ();
//				if (null == mr || !mr.enabled) {
//
//					TrackSlice[] slices = track.TrackSlices;
//
//					for (int i = 0; i < slices.Length; i++) {
//						TrackSlice slice = slices [i];
//						DrawSlice (slice);
//					}
//				}
//			}
//
//
//		}

		private void DrawSlice (SliceStrip.TransformedSlice slice)
		{
			PathMeshGenerator track = target as PathMeshGenerator;
			Transform transform = track.transform;

			int slicePoints = slice.Points.Length;
			for (int j = 0; j < slicePoints; j++) {
				int k = (j < slicePoints - 1) ? j + 1 : 0;
				float t = ((float)j / (float)slicePoints);
				if (t < 0.25f) {
					Handles.color = Color.yellow;
				} else if (t < 0.5f) {
					Handles.color = Color.green;
				} else if (t < 0.75f) {
					Handles.color = Color.blue;
				} else {
					Handles.color = Color.red;
				}
				Handles.DrawLine (transform.TransformPoint (slice.Points [j]), transform.TransformPoint (slice.Points [k]));

				Handles.color = Color.cyan;
				//Vector3 center = transform.TransformPoint (slice.Center);
				Vector3 center = slice.Center;
				Handles.DrawLine (center, center + slice.Direction * 1.0f);
			}
		}
	}

}

