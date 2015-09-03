using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

using Util;
using Util.Editor;
using Paths;
using Paths.Editor;

namespace Tracks
{
	[CustomEditor(typeof(Track))]
	public class TrackEditor : Editor
	{

		//private int slicesPerSegment = 10;

		SerializedProperty pathProp;
		//SerializedProperty trackGeneratorProp;

		private Type[] trackGeneratorTypes;
		private string[] trackGeneratorDisplayNames;
		private bool meshDirty;
		private ITrackGeneratorEditor trackGeneratorEditorInstance;
		public const int SHEET_GENERAL = 0;
		public const int SHEET_PATH = 1;
		public const int SHEET_MESH = 2;
		public const int SHEET_SETTINGS = 3;
		private static GUIContent[] TB_CONTENTS = new GUIContent[] {
            new GUIContent ("General", "Track Parameters"),
            new GUIContent ("Path", "Path Parameters"),
            new GUIContent ("Mesh", "Mesh Configuration"),
            new GUIContent ("Settings", "Track Settings"),
        };
		TypedCustomToolEditorPrefs editorPrefs;
		private Track track;

		public TrackEditor ()
		{
			PopulateTrackGenerators ();
		}

		void OnEnable ()
		{
			this.track = target as Track;
			pathProp = serializedObject.FindProperty ("path");
            
			// Load editor settings
			this.editorPrefs = 
                new PrefixCustomToolEditorPrefs (new ParameterStoreCustomToolEditorPrefs (track.ParameterStore), "Editor.");


		}

		void OnDisable ()
		{
		}

		public void TrackGeneratorModified ()
		{
			Track track = target as Track;
			EditorUtility.SetDirty (track);
			//SliceConfigurationChanged();
			SetMeshDirty ();
			track.ConfigurationChanged ();
			SceneView.RepaintAll ();
		}

		public void PathModifiersChanged ()
		{
			Track track = target as Track;
			EditorUtility.SetDirty (track);
//			track.OnPathModifiersChanged ();
		}

		void SetMeshDirty ()
		{
			this.meshDirty = true;
		}

		private void PopulateTrackGenerators ()
		{
			this.trackGeneratorTypes = TrackGenerator.FindTrackGeneratorTypes ();
			this.trackGeneratorDisplayNames = new string[trackGeneratorTypes.Length];
			for (int i = 0; i < trackGeneratorTypes.Length; i++) {
				// Form generator name from the type name by removing "TrackGenerator" suffix (if any)
				string n = StringUtil.RemoveStringTail (trackGeneratorTypes [i].Name, "Generator", 1);
				n = StringUtil.RemoveStringTail (n, "Track", 1);
				trackGeneratorDisplayNames [i] = n;
			}
		}

		private int FindCurrentTrackGeneratorIndex ()
		{
			Track track = target as Track;
        
			// Find selected index
			int selectedTgIndex = -1;
			for (int i = 0; i < trackGeneratorTypes.Length; i++) {
				string n = trackGeneratorTypes [i].FullName;
				if (n == track.TrackGeneratorType) {
					selectedTgIndex = i;
					break;
				}
			}
			return selectedTgIndex;
		}

		static Type[] FindTrackGeneratorEditorTypes ()
		{
			Type[] editorTypes = Util.TypeUtil.FindTypesHavingAttribute (typeof(TrackGeneratorCustomEditor));
			return editorTypes;
		}

		static Type FindTrackGeneratorEditorType (Type trackGeneratorType)
		{
			Type[] editorTypes = FindTrackGeneratorEditorTypes ();
			for (int i = 0; i < editorTypes.Length; i++) {
				object[] attrs = editorTypes [i].GetCustomAttributes (typeof(TrackGeneratorCustomEditor), true);
				for (int j = 0; j < attrs.Length; j++) {
					TrackGeneratorCustomEditor tge = (TrackGeneratorCustomEditor)attrs [j];
					if (trackGeneratorType == tge.InspectedType) {
						return editorTypes [i];
					}
				}
			}
			// Look for base type:
			Type baseType = trackGeneratorType.BaseType;
			return (null != baseType) ? FindTrackGeneratorEditorType (baseType) : null;
		}

		private ITrackGeneratorEditor GetTrackGeneratorEditor ()
		{
			if (null == this.trackGeneratorEditorInstance) {
				Track track = target as Track;
				ITrackGenerator tg = track.TrackGeneratorInstance;
				if (null != tg) {
					Type tgEditorType = FindTrackGeneratorEditorType (tg.GetType ());
					if (null != tgEditorType) {
						if (!typeof(ITrackGeneratorEditor).IsAssignableFrom (tgEditorType)) {
							Debug.LogError ("Class '" + tgEditorType + "' has attribute TrackGeneratorCustomEditor but it doesn't implement the ITrackGeneratorEditor interface");
						} else if (typeof(ScriptableObject).IsAssignableFrom (tgEditorType)) {
							this.trackGeneratorEditorInstance = (ITrackGeneratorEditor)ScriptableObject.CreateInstance (tgEditorType);
						} else {
							this.trackGeneratorEditorInstance = (ITrackGeneratorEditor)Activator.CreateInstance (tgEditorType);
						}
						trackGeneratorEditorInstance.OnEnable (new TrackGeneratorEditorContext (tg, track, this));
					}

				}
			}
			return this.trackGeneratorEditorInstance;
        
		}




//      public const int TB_SHEET_SETTINGS = 0;
//      public const int TB_SHEET_SETTINGS = 0;
//      public const int TB_SHEET_SETTINGS = 0;


		private class TrackPathData : IPathData
		{
			private Track track;

			public TrackPathData (Track track)
			{
				this.track = track;
			}
			public IPathInfo GetPathInfo ()
			{
				throw new NotImplementedException ();
			}
			public int GetId ()
			{
				return 0;
			}

			public string GetName ()
			{
				return "Default";
			}
			public Color GetColor ()
			{
				return PathGizmoPrefs.FinalPathLineColor;
			}
			public void SetColor (Color value)
			{
				throw new NotImplementedException ();
			}
			public bool IsDrawGizmos ()
			{
				return true;
			}

			public void SetDrawGizmos (bool value)
			{
				throw new NotImplementedException ();
			}
			public PathDataInputSource GetInputSource ()
			{
				return PathDataInputSourceSelf.Instance;
			}
			public IPathSnapshotManager GetPathSnapshotManager ()
			{
				return UnsupportedSnapshotManager.Instance;
			}
			public IPathModifierContainer GetPathModifierContainer ()
			{
				return track.GetPathModifierContainer ();
			}

			public PathPoint[] GetAllPoints ()
			{
				throw new NotImplementedException ();
			}

			public int GetPointCount ()
			{
				throw new NotImplementedException ();
			}

			public PathPoint GetPointAtIndex (int index)
			{
				throw new NotImplementedException ();
			}

			public int GetOutputFlags ()
			{
				throw new NotImplementedException ();
			}

			public int GetOutputFlagsBeforeModifiers ()
			{
				throw new NotImplementedException ();
			}

			public float GetTotalDistance ()
			{
				throw new NotImplementedException ();
			}


		
			public int GetControlPointCount ()
			{
				throw new NotImplementedException ();
			}

			public Vector3 GetControlPointAtIndex (int index)
			{
				throw new NotImplementedException ();
			}

			public void SetControlPointAtIndex (int index, Vector3 pt)
			{
				throw new NotImplementedException ();
			}

			public bool IsUpToDate ()
			{
				throw new NotImplementedException ();
			}

			public long GetStatusToken ()
			{
				throw new NotImplementedException ();
			}
		}

		public override void OnInspectorGUI ()
		{
			this.track = target as Track;

			int selectedSheet = editorPrefs.GetInt ("selectedSheet", 0);
			selectedSheet = GUILayout.Toolbar (selectedSheet, TB_CONTENTS);
			editorPrefs.SetInt ("selectedSheet", selectedSheet);

			if (selectedSheet == SHEET_GENERAL) {


				DrawGeneralInspectorSheet ();

			} else if (selectedSheet == SHEET_PATH) {

				if (null != track.Path) {
					EditorGUILayout.HelpBox ("Track's Path Modifiers can be used to modify the path before it's feed to the Track Generator. Modifiers will not modify the original Path.", MessageType.Info);

					IPathData pathData = new TrackPathData (track);
					PathModifierEditorContext context = new PathModifierEditorContext (
                        pathData, track.Path, this, PathModifiersChanged, editorPrefs);

					// 
//                  this.target, null, track.Path, this, TrackGeneratorModified, editorPrefs

					PathModifierEditorUtil.DrawPathModifiersInspector (context, track);
				}
			} else if (selectedSheet == SHEET_MESH) {
				DrawMeshInspectorSheet ();

				EditorGUILayout.Separator ();
				EditorGUILayout.Separator ();

				if (GUILayout.Button ("Generate Colliders")) {

					MeshCollider mc = track.GetComponent<MeshCollider> ();
					if (null == mc) {
						mc = track.gameObject.AddComponent<MeshCollider> ();
					}

					track.GenerateTrackColliders ();



				}
            
			} else if (selectedSheet == SHEET_SETTINGS) {
				EditorGUI.BeginChangeCheck ();
				track.AutomaticUpdateWithPath = EditorGUILayout.Toggle ("Automatic Update with Path", track.AutomaticUpdateWithPath);
				if (EditorGUI.EndChangeCheck ()) {
					EditorUtility.SetDirty (track);
				}

				//EditorGUI.BeginDisabledGroup (track.AutomaticUpdateWithPath == false);
				EditorGUI.BeginChangeCheck ();
				track.AutomaticMeshUpdate = EditorGUILayout.Toggle ("Automatic Mesh Update", track.AutomaticMeshUpdate);
				if (EditorGUI.EndChangeCheck ()) {
					EditorUtility.SetDirty (track);
				}
			}

			//EditorGUI.EndDisabledGroup ();



			//Debug.Log ("Save Parameters");
			track.SaveTrackGeneratorParameters ();
			//trackGenerator.SaveParameters (track.GetTrackGeneratorParameterStore ());


			//DrawDefaultInspector ();

			serializedObject.ApplyModifiedProperties ();
		}

		void OnSceneGUI ()
		{
			this.track = target as Track;
			if (!track.isActiveAndEnabled) {
				// TODO add a configuration parameter for this behaviour!
				return;
			}

			//DrawPath();
			ITrackGeneratorEditor tgEditor = GetTrackGeneratorEditor ();
			if (null != tgEditor) {
				tgEditor.DrawSceneGUI (new TrackGeneratorEditorContext (track.TrackGeneratorInstance, track, this));
			}
        
		}

		public void DrawDefaultGeneralInspectorSheet ()
		{

			EditorGUI.BeginChangeCheck ();
			EditorGUILayout.PropertyField (pathProp, new GUIContent ("Path"));
			if (EditorGUI.EndChangeCheck ()) {
				track.ConfigurationChanged ();
				EditorUtility.SetDirty (track);
			}
			EditorGUI.BeginChangeCheck ();
			int tgIndex = FindCurrentTrackGeneratorIndex ();
			tgIndex = EditorGUILayout.Popup ("Track Generator", tgIndex, trackGeneratorDisplayNames);
			if (EditorGUI.EndChangeCheck ()) {
				track.TrackGeneratorType = trackGeneratorTypes [tgIndex].FullName;
				EditorUtility.SetDirty (track);
				trackGeneratorEditorInstance = null;
				TrackGeneratorModified ();
			}
            
			ITrackGeneratorEditor tgEditor = GetTrackGeneratorEditor ();
			if (null != tgEditor) {
                
				TrackGeneratorEditorContext ctx = new TrackGeneratorEditorContext (track.TrackGeneratorInstance, track, this);
				tgEditor.DrawInspectorGUI (ctx);
				EditorGUILayout.Separator ();
			}
		}

		public virtual void DrawGeneralInspectorSheet ()
		{
			DrawDefaultGeneralInspectorSheet ();
		}

		public void DrawDefaultMeshInspectorSheet ()
		{
			ITrackGenerator trackGenerator = track.TrackGeneratorInstance;
			EditorGUI.BeginDisabledGroup (null == trackGenerator);
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
            
            
			EditorGUI.BeginDisabledGroup (null == track.generatedMesh);
			if (GUILayout.Button ("Clear Mesh")) {
				ClearTrackMesh ();
				//Undo.RecordObject(path, "Generate Tunnel Mesh");
				//path.AddSegment();
				//EditorUtility.SetDirty(path);
			}
            
			if (GUILayout.Button ("Save Mesh Asset")) {
				SaveGeneratedMesh ();
				//Undo.RecordObject(path, "Generate Tunnel Mesh");
				//path.AddSegment();
				//EditorUtility.SetDirty(path);
			}
			EditorGUI.EndDisabledGroup ();
		}

		public virtual void DrawMeshInspectorSheet ()
		{
			DrawDefaultMeshInspectorSheet ();
		}

		protected void GenerateMesh ()
		{
			// Add MeshFilter and MeshRenderer if not already added
			MeshFilter mf = track.gameObject.GetComponent<MeshFilter> ();
			if (null == mf) {
				mf = track.gameObject.AddComponent<MeshFilter> ();
			}
			MeshRenderer mr = track.gameObject.GetComponent<MeshRenderer> ();
			if (null == mr) {
				mr = track.gameObject.AddComponent<MeshRenderer> ();
			}

			track.GenerateTrackMesh ();
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




		public void ClearTrackMesh ()
		{

			Track track = target as Track;
			//NewPath path = track.Path;
			track.generatedMesh = null;
			SceneView.RepaintAll ();

		}

		public void SaveGeneratedMesh ()
		{
        
			Track track = target as Track;
//      Path path = track.Path;
			ITrackGenerator tg = track.TrackGeneratorInstance;

			// Asset folder:
			// TODO use previously saved!

			string assetFolder;
			string filePath = tg.GetSavedMeshAssetPath ();
			if (StringUtil.IsEmpty (filePath)) {
				filePath = "";
				assetFolder = filePath;
			} else {

				if (!StringUtil.IsEmpty (filePath)) {
					// Remove extension
					int extBegin = filePath.LastIndexOf ('.');
					if (extBegin > 0) {
						filePath = filePath.Substring (0, extBegin);
					}

					assetFolder = MiscUtil.GetFolderName (filePath);
				} else {
					assetFolder = "Assets";
				}
			}

			if (!AssetDatabase.IsValidFolder (assetFolder)) {
				assetFolder = "Assets";
				if (!AssetDatabase.IsValidFolder (assetFolder)) {
					assetFolder = "";
				}
			}

			string assetFileName = MiscUtil.GetFileName (filePath);
			if (StringUtil.IsEmpty (assetFileName)) {
				assetFileName = track.generatedMesh.name;
			}

			filePath = EditorUtility.SaveFilePanelInProject (
            "Save Mesh", assetFileName, "asset", "Save generated Mesh asset", assetFolder);
			if (null == filePath || filePath.Length == 0) {
				return;
			}

			assetFolder = MiscUtil.GetFolderName (filePath);
			assetFileName = MiscUtil.GetFileName (filePath);

			if (tg.GetSavedMeshAssetPath () != filePath) {
				tg.SetSavedMeshAssetPath (filePath);
				EditorUtility.SetDirty (track);
			}

			/*
        Mesh mesh = (Mesh)AssetDatabase.LoadAssetAtPath (prefabPath, typeof(GameObject));
        if (!mesh) {
            mesh = new Mesh();
            AssetDatabase.AddObjectToAsset(mesh, prefabPath);
        } else {
            mesh.Clear();
        }*/
        
			Mesh mesh = (Mesh)AssetDatabase.LoadAssetAtPath (filePath, typeof(Mesh));
			if (null == mesh) {
				mesh = new Mesh ();
				AssetDatabase.CreateAsset (mesh, filePath);
			}
			//AssetDatabase.SaveAssets();
			mesh.Clear ();

			// copy vertices, normals, uv's and triangles:
			mesh.vertices = track.generatedMesh.vertices;
			mesh.normals = track.generatedMesh.normals;
			mesh.uv = track.generatedMesh.uv;
			mesh.triangles = track.generatedMesh.triangles;

			//SelectedTrackGenerator.CreateMesh(path, mesh);
        
			//AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(mesh));
			AssetDatabase.SaveAssets ();
			Debug.Log ("Saved Mesh to " + AssetDatabase.GetAssetPath (mesh));
		}

		private void DrawPath ()
		{

			Track track = target as Track;
			if (null == track) {
				return;
			}
			Path path = track.Path;
//            Transform transform = track.transform;

			if (null == path) {
				return;
			}
			//int pathSteps = path.GetResolution() * path.GetSegmentCount();

			//Debug.Log("PathSteps: " + path.Points.Length);

			// Connect control points:
			// TODO we should show points after all the modifiers (subdivide etc)
			/*PathPoint[] points = path.GetAllPoints ();
            for (int i = 1; i < points.Length; i++) {
                Vector3 startPoint = transform.TransformPoint (points [i - 1].Position);
                Vector3 endPoint = transform.TransformPoint (points [i].Position);
            
                // Connection between path steps:
            
                Handles.color = Color.red;
            
                Handles.DrawLine (startPoint, endPoint);

                startPoint = endPoint;
            }*/

			// Draw generated slices
			ITrackGenerator tg = track.TrackGeneratorInstance;
			if (null != tg) {
				MeshRenderer mr = track.GetComponent<MeshRenderer> ();
				if (null == mr || !mr.enabled) {

					TrackSlice[] slices = track.TrackSlices;

					for (int i = 0; i < slices.Length; i++) {
						TrackSlice slice = slices [i];
						DrawSlice (slice);
					}
				}
			}


		}

		private void DrawSlice (TrackSlice slice)
		{
			Track track = target as Track;
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
				Vector3 center = transform.TransformPoint (slice.Center);
				Handles.DrawLine (center, center + slice.Direction * 1.0f);
			}
		}
	}

}

