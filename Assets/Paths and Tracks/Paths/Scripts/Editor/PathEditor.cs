using UnityEngine;
using UnityEditor;

using System.Collections;
using System.Collections.Generic;
using System;

using Paths;
using Util.Editor;
using Util;

namespace Paths.Editor
{
	public class PathMenu
	{
//        [MenuItem("Paths")]
//        private static void PathsMenuOption()
//        {
//        }
	}


	[CustomEditor(typeof(Path), true)]
	public class PathEditor : AbstractPathEditor<Path, IPathData>
	{

	}


	// TODO refactor this class; it's getting too complex
	public class AbstractPathEditor<TPath, TPathData> : UnityEditor.Editor, ICUstomToolEditorHost where TPath: Path where TPathData: IPathData
	{

		public enum ToolbarSheet : int
		{
			General,
			Points,
			Modifiers,
			DataSets,
			Settings,
			Debug,
		}


//		private static string[] TB_TEXTS = {
//            "Path",
//            "Points",
//            "Modifiers",
//            "Settings",
//            "Debug"
//        };
		private Dictionary<int, bool> pointExpanded = new Dictionary<int, bool> ();
		private int _selectedControlPointIndex = -1;

		private Dictionary<object, ICustomToolEditor> _cachedCustomToolEditors = new Dictionary<object, ICustomToolEditor> ();

		protected TPath path;
		protected TPathData pathData;
		protected ParameterStore editorParams;

		protected AbstractPathEditor ()
		{

		}

		protected void InitGUI ()
		{
			this.path = target as TPath;
			this.editorParams = path.EditorParameters;
			UpdateDataSetSelection ();
		}
		public void SetEditorFor (object customToolInstance, ICustomToolEditor editor)
		{
			if (null == editor) {
				if (_cachedCustomToolEditors.ContainsKey (customToolInstance)) {
					_cachedCustomToolEditors.Remove (customToolInstance);
				}
			} else {
				_cachedCustomToolEditors [customToolInstance] = editor;
			}
		}

		public ICustomToolEditor GetEditorFor (object customToolInstance)
		{
			if (_cachedCustomToolEditors.ContainsKey (customToolInstance)) {
				return _cachedCustomToolEditors [customToolInstance];
			} else {
				return null;
			}
		}


		private void UpdateDataSetSelection ()
		{
			pathData = (TPathData)PathEditorUtil.GetSelectedDataSet (path, editorParams, true);
		}
		protected string GetToolbarSheetLabel (ToolbarSheet sheet)
		{
			return Enum.GetName (typeof(ToolbarSheet), sheet);
		}
		protected string[] GetToolbarSheetLabels ()
		{
			return Enum.GetNames (typeof(ToolbarSheet));
		}

		protected int SelectedControlPointIndex {
			get {
				return _selectedControlPointIndex;
			}
			set {
				this._selectedControlPointIndex = value;
			}
		}


		private void PathModifiersChanged ()
		{
			EditorUtility.SetDirty (target);
			UnityEditor.SceneView.RepaintAll ();

			pathData.GetPathModifierContainer ().ConfigurationChanged ();

		}

		public override sealed void OnInspectorGUI ()
		{
			InitGUI ();

//          bool disableEditing = path.FrozenStatus == Path.PathStatus.Frozen;


			EditorGUILayout.BeginHorizontal ();
			if (path.GetDataSetCount () > 1) {
				DrawDataSetSelection ();
//				DrawSceneViewDataSetSelection ();
			}

			//EditorGUI.BeginDisabledGroup(path.PointsDirty == false);
			if (GUILayout.Button ("Refresh" + (pathData.IsUpToDate () ? "" : " *"))) {
				path.ForceUpdatePathData (pathData);
				EditorUtility.SetDirty (path);
			}
			//EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal ();

			string sceneViewDsName = "";
			GUIStyle sceneViewDsNameStyle;
			string sceneViewDsNameTooltip = "Data set edited in the Scene View.";

			if (path.IsEditorSceneViewDataSetLocked ()) {
				sceneViewDsName = path.GetEditorSceneViewDataSet ().GetName ();
				sceneViewDsNameStyle = EditorStyles.boldLabel;
				sceneViewDsNameTooltip += " The Scene View Editor data set is currently locked to this data set; it can be changed in DataSets tab (the small 'E' button in the list).";
			} else {
				sceneViewDsName = pathData.GetName ();
				sceneViewDsNameStyle = EditorStyles.label;
			}

			EditorGUILayout.LabelField (new GUIContent ("Scene View Data", sceneViewDsNameTooltip), 
			                            new GUIContent (sceneViewDsName, sceneViewDsNameTooltip), sceneViewDsNameStyle);

			if (path.IsEditorSceneViewDataSetLocked ()) {

			}

			int pointCount = pathData.GetPointCount ();

			string dataSetNameSuffix;
			if (path.GetDataSetCount () > 1) {
				dataSetNameSuffix = " (" + pathData.GetName () + ")";
			} else {
				dataSetNameSuffix = "";
			}

			EditorGUILayout.LabelField ("Points" + dataSetNameSuffix, pointCount.ToString ());

			string totalDistance = "";
			totalDistance = pathData.GetTotalDistance ().ToString ("f1");
			EditorGUILayout.LabelField ("Total Length" + dataSetNameSuffix, totalDistance);


			ToolbarSheet tbSheet = (ToolbarSheet)editorParams.GetInt ("ToolbarSelection", 0);
			EditorGUI.BeginChangeCheck ();
			tbSheet = (ToolbarSheet)GUILayout.Toolbar ((int)tbSheet, GetToolbarSheetLabels ());
			if (EditorGUI.EndChangeCheck ()) {
				editorParams.SetInt ("ToolbarSelection", (int)tbSheet);
			}

			switch (tbSheet) {
			case ToolbarSheet.General:
				DrawGeneralInspectorGUI ();
				break;
			case ToolbarSheet.Points:
                // TODO why do we have the "expanded" pref here? Why not in the DrawPathPointsInspector fn itself?
				bool pointsExpanded = editorParams.GetBool ("PathPointsExpanded", true);
				DrawPathPointsInspector (ref pointsExpanded);
				editorParams.SetBool ("PathPointsExpanded", pointsExpanded);
				break;
			case ToolbarSheet.Modifiers:
				DrawPathModifiersInspectorGUI ();
				break;
			case ToolbarSheet.DataSets:
				DrawDataSetsInspectorGUI ();
				break;
			case ToolbarSheet.Settings:
				DrawSettingsInspectorGUI ();
				break;
			case ToolbarSheet.Debug:
				DrawDebugInspectorGUI ();
				break;
			}

			EditorGUILayout.Separator ();

			//DrawDefaultInspector();
		}

		protected void DrawDataSetSelection ()
		{
			IPathData ds = PathEditorUtil.GetSelectedDataSet (path, editorParams, true);
			
			int dataSetIndex = path.IndexOfDataSet (ds);
			
			List<string> dataSetNames;
			List<int> dataSetIds;
			PathEditorUtil.FindAvailableDataSets (path, out dataSetNames, out dataSetIds, " (default)");
			
			
			EditorGUILayout.BeginHorizontal ();
			EditorGUI.BeginChangeCheck ();
			dataSetIndex = EditorGUILayout.Popup ("Data Set", dataSetIndex, dataSetNames.ToArray ());
			if (EditorGUI.EndChangeCheck ()) {
				int dataSetId = dataSetIds [dataSetIndex];
				PathEditorUtil.SetSelectedDataSet (dataSetId, editorParams);
				ds = path.FindDataSetById (dataSetId);
			}
			
			EditorGUI.BeginChangeCheck ();
			Color color = ds.GetColor ();
			color = EditorGUILayout.ColorField (color, GUILayout.MaxWidth (40));
			if (EditorGUI.EndChangeCheck ()) {
				Undo.RecordObject (path, "Set Data Set color");
				ds.SetColor (color);
				EditorUtility.SetDirty (path);
			}
			EditorGUILayout.EndHorizontal ();
		
			UpdateDataSetSelection ();
		}



		protected virtual void DrawGeneralInspectorGUI ()
		{
			DrawDefaultGeneralInspectorGUI ();
		}

		protected void DrawDefaultGeneralInspectorGUI ()
		{
//          Path path = target as Path;
//          EditorGUILayout.LabelField ("", path.PointsDirty ? "*Needs Refresh*" : "Points are up to date");
//			DrawInputSourceSelection ();
		}

		protected virtual void DrawPathModifiersInspectorGUI ()
		{
			DrawDefaultPathModifiersInspectorGUI ();
		}

		protected void DrawDefaultPathModifiersInspectorGUI ()
		{
			Path path = target as Path;

			PathModifierEditorUtil.DrawPathModifiersInspector (path, pathData, this, path, PathModifiersChanged);
		}

//		protected void DrawInputSourceSelection ()
//		{
//			PathEditorUtil.DrawInputSourceSelection (path, pathData);
//		}



		protected virtual void DrawSettingsInspectorGUI ()
		{
			DrawDefaultSettingsInspectorGUI ();
		}

		protected void DrawDefaultSettingsInspectorGUI ()
		{
			Path path = target as Path;
			EditorGUI.BeginChangeCheck ();
			path.FrozenStatus = (Path.PathStatus)EditorGUILayout.EnumPopup ("Update Status", path.FrozenStatus);

			//path.Frozen = EditorGUILayout.Toggle("Frozen", path.Frozen);
			if (EditorGUI.EndChangeCheck ()) {
                
			}
		}
		protected virtual void DrawDataSetsInspectorGUI ()
		{
			DrawDefaultDataSetsInspectorGUI ();
		}

//		class PathDataTreeNode
//		{
//			public IPathData data;
//			public PathDataTreeNode parent;
//			public List<PathDataTreeNode> children = new List<PathDataTreeNode>();
//
//
//		}

		protected void DrawDefaultDataSetsInspectorGUI ()
		{

			int count = path.GetDataSetCount ();

//			PathDataTreeNode root = null;// = new PathDataTreeNode();
//
//			Dictionary
//			
//			for (int i = 0; i < count; i++) {
//				
//				IPathData ds = path.GetDataSetAtIndex (i);
//				List<IPathData> children = ((AbstractPathData)ds).FindDataTargets (path);
//				PathDataTreeNode node = new PathDataTreeNode();
//				node.data = ds;
//				if (null == root) {
//					root = node;
//				} else {
//					if (root.children.Contains(node)) {
//						node.parent = root;
//					} else {
//						// To orphans....
//					}
//				}
//				foreach (IPathData childData in children) {
//					// Find existing....
//					PathDataTreeNode childNode = new PathDataTreeNode();
//					childNode.data = childData;
//					childNode.parent = node;
//					node.children.Add(childNode);
//				}
//
//			}
//
//			EditorGUILayout.HelpBox ("Data Sets are used to generate variations of the path, for example different resolution versions. The default path data is selected by ticking a check box below.", MessageType.Info);
//
//
//
//			editorParams.SetBool ("ds[0].expanded", EditorGUILayout.Foldout (editorParams.GetBool ("ds[0].expanded"), "FooBar"));
//
//
//			EditorGUI.indentLevel++;
//			EditorGUILayout.Foldout (false, "Xsrew3");
//			EditorGUI.indentLevel++;
//			EditorGUILayout.Foldout (true, "Foobar of child");
//
//			EditorGUI.indentLevel -= 2;
//			EditorGUILayout.Foldout (true, "XXX");
//

		

			///////////// BEGIN REAL IMPL ////////////

//			int count = path.GetDataSetCount ();
			for (int i = 0; i < count; i++) {

				IPathData ds = path.GetDataSetAtIndex (i);

				/////////////// EXPERIMENTAL BEGIN ///////////////////////

			


				/////////////// EXPERIMENTAL END   ///////////////////////

				bool isDefault = path.IsDefaultDataSet (ds);

				EditorGUILayout.BeginHorizontal ();

//				if (isDefault) {
//					EditorGUILayout.LabelField ("[" + i.ToString () + "]", ds.GetName ());
//				} else {
				//EditorGUILayout.PrefixLabel ("[" + i.ToString () + "]");

				EditorGUI.BeginChangeCheck ();
				isDefault = GUILayout.Toggle (isDefault, new GUIContent ("Def", "Default dataset"), EditorStyles.miniButtonLeft, GUILayout.ExpandWidth (false));
				if (EditorGUI.EndChangeCheck () && isDefault) {
					Undo.RecordObject (target, "Set default data set to '" + ds.GetName () + "'");
					path.SetDefaultDataSetId (ds.GetId ());
					EditorUtility.SetDirty (target);
					isDefault = path.IsDefaultDataSet (ds);
				}

				EditorGUI.BeginChangeCheck ();
				bool drawGizmos = ds.IsDrawGizmos ();
				drawGizmos = GUILayout.Toggle (drawGizmos, new GUIContent ("Gz", "Draw Gizmos on Scene View"), EditorStyles.miniButtonMid, GUILayout.ExpandWidth (false));
				if (EditorGUI.EndChangeCheck ()) {
//					Undo.RecordObject (target, "Set default data set to '" + ds.GetName () + "'");
//					path.SetDefaultDataSetId (ds.GetId ());
					ds.SetDrawGizmos (drawGizmos);
					EditorUtility.SetDirty (target);
//					isDefault = path.IsDefaultDataSet (ds);
				}

				EditorGUI.BeginChangeCheck ();
				bool lockForEditor = path.IsEditorSceneViewDataSetLocked () && path.GetEditorSceneViewDataSet ().GetId () == ds.GetId ();
				lockForEditor = GUILayout.Toggle (lockForEditor, new GUIContent ("E", "Lock this data set for editor Scene View"), EditorStyles.miniButtonRight, GUILayout.ExpandWidth (false));
				if (EditorGUI.EndChangeCheck ()) {
					//					Undo.RecordObject (target, "Set default data set to '" + ds.GetName () + "'");
					//					path.SetDefaultDataSetId (ds.GetId ());
					path.SetEditorSceneViewDataSetId (ds.GetId ());
					path.SetEditorSceneViewDataSetLocked (lockForEditor);
					ds.SetDrawGizmos (drawGizmos);
					EditorUtility.SetDirty (target);
					//					isDefault = path.IsDefaultDataSet (ds);
				}


				Color color = ds.GetColor ();
				EditorGUI.BeginChangeCheck ();
				color = EditorGUILayout.ColorField (color, GUILayout.MaxWidth (60), GUILayout.ExpandWidth (false));
				if (EditorGUI.EndChangeCheck ()) {
					Undo.RecordObject (target, "Change Data Set '" + ds.GetName () + "' color");
					ds.SetColor (color);
					EditorUtility.SetDirty (target);
				}
				EditorGUI.BeginChangeCheck ();
				string newName = EditorGUILayout.TextField (ds.GetName ());
				if (EditorGUI.EndChangeCheck ()) {
					Undo.RecordObject (target, "Rename Data Set '" + ds.GetName () + "' to '" + newName + "'");
					path.SetDataSetName (ds, newName);
					EditorUtility.SetDirty (target);
				}
//				}
//				EditorGUILayout.Toggle (isDefault, GUILayout.ExpandWidth (false));

				EditorGUI.BeginDisabledGroup (i <= 0 || !path.IsSetDataSetIndexSupported ());
				if (GUILayout.Button (new GUIContent ("^", "Move this data set upwards"), EditorStyles.miniButtonLeft, GUILayout.ExpandWidth (false))) {
					path.SetDataSetIndex (ds, i - 1);
//					if (ds.GetId () == pathData.GetId ()) {
//						this.SetPathDataIndex (i - 1);
//					}
					EditorUtility.SetDirty (target);
				}
				EditorGUI.EndDisabledGroup ();

				EditorGUI.BeginDisabledGroup (i >= count - 1 || !path.IsSetDataSetIndexSupported ());
				if (GUILayout.Button (new GUIContent ("v", "Move this data set downwards"), EditorStyles.miniButtonRight, GUILayout.ExpandWidth (false))) {
					path.SetDataSetIndex (ds, i + 1);
//					if (ds.GetId () == pathData.GetId ()) {
//						this.SetPathDataIndex (i + 1);
//					}
					EditorUtility.SetDirty (target);
				}
				EditorGUI.EndDisabledGroup ();
//
//				EditorGUI.BeginDisabledGroup (isDefault);
//				if (GUILayout.Button ("Make Default", GUILayout.ExpandWidth (false))) {
//					Undo.RecordObject (target, "Remove Data Set " + i);
//					path.RemoveDataSetAtIndex (i);
//					EditorUtility.SetDirty (target);
//				}
//				EditorGUI.EndDisabledGroup ();


				// Is input to any other data set?


				EditorGUI.BeginDisabledGroup (isDefault);
				if (GUILayout.Button (new GUIContent ("X", "Permanently remove this data set"), EditorStyles.miniButton, GUILayout.ExpandWidth (false))) {

//					List<IPathData> targets = ((AbstractPathData)ds).FindDataTargets (path);


					int cpCount = ds.GetControlPointCount ();
					int pmCount = ds.GetPathModifierContainer ().GetPathModifiers ().Length;
					string message = "Do you want to permanently remove Path Data Set '" + ds.GetName () + "'?";

					if (cpCount > 0 || pmCount > 0 /*|| targets.Count > 0*/) {
						message += " The data set has " + cpCount + " control point(s), " + pmCount + " Path Modifier(s).";
//						message += " It's source for following data sets in this Path: ";
//						for (int ti = 0; ti < targets.Count; ti++) {
//							string targetName = targets [ti].GetName ();
//							if (ti > 0) {
//								message += ", ";
//							}
//							message += "'" + targetName + "'";
//						}
//						message += ".";
					} else {
						message += " The data set doesn't have any control points or path modifiers.";
					}

					if (EditorUtility.DisplayDialog ("Remove Data Set", message, "Sure!", "No way!")) {

						Undo.RecordObject (target, "Remove Data Set " + i);
						path.RemoveDataSetAtIndex (i);
						EditorUtility.SetDirty (target);
					}
				}
				EditorGUI.EndDisabledGroup ();

				EditorGUILayout.EndHorizontal ();
			}
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.PrefixLabel ("Actions");
			if (GUILayout.Button ("Add New", EditorStyles.miniButton)) {
				Undo.RecordObject (target, "Add Data Set");
				path.AddDataSet ();
				EditorUtility.SetDirty (target);
			}
			EditorGUILayout.EndHorizontal ();

		}



		protected virtual void DrawDebugInspectorGUI ()
		{
			DrawDefaultDebugInspectorGUI ();
		}

		protected void DrawDefaultDebugInspectorGUI ()
		{
			DrawDefaultInspector ();
		}

		private bool IsPointExpanded (int index)
		{
			return pointExpanded.ContainsKey (index) ? pointExpanded [index] : false;
		}

		protected virtual void DrawPathPointsInspector (ref bool expanded)
		{
			DrawDefaultPathPointsInspector ();
		}

		protected void DrawDefaultPathPointsInspector ()
		{
			DrawGeneratedPathPointsInspector ();
		}

		protected void DrawGeneratedPathPointsInspector ()
		{

			Path path = target as Path;

			ParameterStore editorParams = path.EditorParameters;
			// TODO implement other data sets!
			PathPoint[] points = pathData.GetAllPoints ();
			bool treeExpanded = EditorGUILayout.Foldout (editorParams.GetBool ("OutputPointsExpanded", false), "Output Points (" + points.Length + ") - " + pathData.GetName ());
			editorParams.SetBool ("OutputPointsExpanded", treeExpanded);
			if (treeExpanded) {
				EditorGUI.indentLevel++;
				DrawPathPointMask ("Caps", pathData.GetOutputFlags ());

				for (int i = 0; i < points.Length; i++) {
					EditorGUILayout.BeginHorizontal ();
					pointExpanded [i] = EditorGUILayout.Foldout (IsPointExpanded (i), "[" + i + "]");
					//              EditorGUILayout.Vector3Field("", points[i].Position);
					EditorGUILayout.LabelField ("", points [i].Position.ToString () + "; " + points [i].Direction.ToString () + "; " + points [i].DistanceFromPrevious.ToString () + "; " + points [i].DistanceFromBegin.ToString ());
					EditorGUILayout.EndHorizontal ();
					if (pointExpanded [i]) {
						EditorGUI.indentLevel++;
						DrawPathPointMask ("Flags", points [i].Flags);

						//                  EditorGUILayout.Vector3Field("Position", points[i].Position);
						//                  EditorGUILayout.Vector3Field("Direction", points[i].Direction);
						//                  EditorGUILayout.FloatField("Dist -1", points[i].DistanceFromPrevious);
						//                  EditorGUILayout.FloatField("Total dist", points[i].DistanceFromBegin);

						EditorGUILayout.LabelField ("Position", points [i].Position.ToString ());
						EditorGUILayout.LabelField ("Forward", points [i].Direction.ToString ());
						EditorGUILayout.LabelField ("Up", points [i].Up.ToString ());
						EditorGUILayout.LabelField ("Right", points [i].Right.ToString ());
						EditorGUILayout.LabelField ("Angle", points [i].Angle.ToString ());
						EditorGUILayout.LabelField ("Dist-1", points [i].DistanceFromPrevious.ToString ());
						EditorGUILayout.LabelField ("Dist-0", points [i].DistanceFromBegin.ToString ());
						EditorGUI.indentLevel--;

					}
					//EditorGUILayout.Vector3Field("Dir", points[i].Direction);

				}
				EditorGUI.indentLevel--;
			}
		}

		public static void DrawPathPointMask (string label, int flags)
		{
			Texture2D emptyGridImage = (Texture2D)Resources.Load ("btn-empty-grid", typeof(Texture2D));
			Texture2D posImage = (Texture2D)Resources.Load ("btn-position", typeof(Texture2D));
			Texture2D dirImage = (Texture2D)Resources.Load ("btn-direction", typeof(Texture2D));
			Texture2D upImage = (Texture2D)Resources.Load ("btn-up", typeof(Texture2D));
			Texture2D angleImage = (Texture2D)Resources.Load ("btn-angle", typeof(Texture2D));
			Texture2D d1Image = (Texture2D)Resources.Load ("btn-dist1", typeof(Texture2D));
			Texture2D d0Image = (Texture2D)Resources.Load ("btn-dist0", typeof(Texture2D));
            
            
            
			int btnHeight = 24;
			int btnWidth = 24;
			GUIStyle boxStyle = GUIStyle.none;
			GUILayoutOption[] boxOptions = {
                GUILayout.Width (btnWidth),
                GUILayout.Height (btnHeight)
            };
            
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.PrefixLabel (label);
			GUILayout.Box (PathPoint.IsPosition (flags) ? posImage : emptyGridImage, boxStyle, boxOptions);
			GUILayout.Box (PathPoint.IsDirection (flags) ? dirImage : emptyGridImage, boxStyle, boxOptions);
			GUILayout.Box (PathPoint.IsUp (flags) ? upImage : emptyGridImage, boxStyle, boxOptions);
			GUILayout.Box (PathPoint.IsAngle (flags) ? angleImage : emptyGridImage, boxStyle, boxOptions);
			GUILayout.Box (PathPoint.IsDistanceFromPrevious (flags) ? d1Image : emptyGridImage, boxStyle, boxOptions);
			GUILayout.Box (PathPoint.IsDistanceFromBegin (flags) ? d0Image : emptyGridImage, boxStyle, boxOptions);
			EditorGUILayout.EndHorizontal ();
		}

		protected void OnSceneGUI ()
		{
			InitGUI ();

			if (path.IsEditorSceneViewDataSetLocked ()) {
				pathData = (TPathData)path.GetEditorSceneViewDataSet ();
			} 

			DrawDefaultSceneGUI ();

		}

		protected void DrawDefaultSceneGUI ()
		{
			if (SelectedControlPointIndex >= 0) {
				Tools.hidden = true;
			} else {
				Tools.hidden = false;
			}
			//
			DrawPath ();
			DrawControlPointHandles ();
		}

		void DrawPath ()
		{
			Transform transform = path.transform;


			// Connect control points
			int cpCount = pathData.GetControlPointCount ();
			Vector3[] transformedPoints = new Vector3[cpCount];
            
			for (int i = 0; i < cpCount; i++) {
				transformedPoints [i] = transform.TransformPoint (pathData.GetControlPointAtIndex (i));
			}
			Handles.color = PathEditorPrefs.ControlPointConnectionLineColor;
			Handles.DrawPolyLine (transformedPoints);

			// Draw directions of Control Points
			Color dirVectorColor = PathEditorPrefs.DirVectorColor;
			float dirVectorLength = PathEditorPrefs.DirVectorLength;
            
			for (int i = 0; i < cpCount; i++) {
				Vector3 pt = transformedPoints [i];
                
				// Direction vector
				Vector3 prevDir, nextDir;
				Vector3 dir = PathUtil.GetPathDirectionAtPoint (transformedPoints, i, false, out prevDir, out nextDir);
                
				Handles.color = dirVectorColor;
				Handles.DrawLine (pt, pt + dir * dirVectorLength);
			}
            
            
            
            
			// Draw the actual path:
			//          PathPoint[] pp = path.GetAllPoints();
			//          transformedPoints = new Vector3[pp.Length];
			//          for (int i = 0; i < pp.Length; i++) {
			//              transformedPoints[i] = transform.TransformPoint(pp[i].Position);
			//          }
			//          Handles.color = Color.cyan;
			//Handles.DrawPolyLine(transformedPoints);
            
			// Draw handles
            
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
        
		void DrawControlPointHandles ()
		{
			Path path = target as Path;
			Transform transform = path.transform;
			Quaternion rot;
			if (Tools.pivotRotation == PivotRotation.Local) {
				// Local pivot rotation
				rot = transform.rotation;
			} else {
				// Global rotation
				rot = Quaternion.identity;
			}
            
			float firstControlPointHandleSize = PathEditorPrefs.FirstControlPointHandleSize;
			float controlPointHandleSize = PathEditorPrefs.ControlPointHandleSize;
			float firstControlPointPickSize = PathEditorPrefs.FirstControlPointPickSize;
			float controlPointPickSize = PathEditorPrefs.ControlPointPickSize;
			Color jointHandleColor = PathEditorPrefs.ControlPointHandleColor;
            
			int cpCount = pathData.GetControlPointCount ();
			for (int i = 0; i < cpCount; i++) {
				Vector3 pt = pathData.GetControlPointAtIndex (i);
				pt = transform.TransformPoint (pt);
                
				float worldHandleSize = HandleUtility.GetHandleSize (pt);
				float handleSize, pickSize;
				if (i == 0) {
					handleSize = firstControlPointHandleSize * worldHandleSize;
					pickSize = firstControlPointPickSize * worldHandleSize;
				} else {
					handleSize = controlPointHandleSize * worldHandleSize;
					pickSize = controlPointPickSize * worldHandleSize;
				}
                
				// Selection button:
				Handles.color = jointHandleColor;
				if (Handles.Button (pt, rot, 
                                   handleSize, pickSize, 
                                   Handles.DotCap)) {
					SelectedControlPointIndex = i;
                    
					Repaint ();
				}
                
                
				// Move handle for selected:
				if (i == SelectedControlPointIndex) {
					EditorGUI.BeginChangeCheck ();
					pt = Handles.DoPositionHandle (pt, rot);
					if (EditorGUI.EndChangeCheck ()) {
						Undo.RecordObject (path, "Move Control Point");
						EditorUtility.SetDirty (path);
						pathData.SetControlPointAtIndex (i, transform.InverseTransformPoint (pt));
					}
				}
                
			}
		}
	}
    
}
