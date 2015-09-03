// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

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

	public sealed class PathEditorUtil
	{
		public static void FindAvailableDataSets (Path path, out List<string> dataSetNames, out List<int> dataSetIds, string addSuffixToDefaultName = "")
		{
			FindAvailableDataSets (path, null, out dataSetNames, out dataSetIds, addSuffixToDefaultName);
		}
		public static void FindAvailableDataSets (Path path, IPathData currentDataSet, out List<string> dataSetNames, out List<int> dataSetIds, string addSuffixToDefaultName = "")
		{
			int defaultId = path.GetDefaultDataSetId ();

			dataSetNames = new List<string> ();
			dataSetIds = new List<int> ();
			
			//			int selectedDsIndex = -1;
			//			int selectedDsId = source.GetDataSetId ();
			//			
			int dsCount = path.GetDataSetCount ();
			for (int i = 0; i < dsCount; i++) {
				IPathData data = path.GetDataSetAtIndex (i);
				int id = data.GetId ();
				
				if (null != currentDataSet && id == currentDataSet.GetId ()) {
					// Skip itself
					continue;
				}
				string name = data.GetName ();
				if (id == defaultId && !StringUtil.IsEmpty (addSuffixToDefaultName)) {
					name += addSuffixToDefaultName;
				}
				dataSetNames.Add (name);
				dataSetIds.Add (id);
				//				if (id == selectedDsId) {
				//					selectedDsIndex = dataSetIds.Count - 1;
				//				}
			}
			//			return selectedDsIndex;
		}

		public static IPathData GetSelectedDataSet (Path path, ParameterStore editorParams, bool fallbackToDefault)
		{
			int pathDefaultDataSetId = path.GetDefaultDataSetId ();
			int dataSetId = editorParams.GetInt ("currentDataSetId", pathDefaultDataSetId);
			IPathData ds = path.FindDataSetById (dataSetId);
			if (null == ds && fallbackToDefault) {
				ds = path.GetDefaultDataSet ();
			}
			return ds;
		}
		public static void SetSelectedDataSet (IPathData data, ParameterStore editorParams)
		{
			if (null == data) {
				editorParams.RemoveParameter ("currentDataSetId");
			} else {
				SetSelectedDataSet (data.GetId (), editorParams);
			}
		}
		public static void SetSelectedDataSet (int dataSetId, ParameterStore editorParams)
		{
			editorParams.SetInt ("currentDataSetId", dataSetId);
		}

		public static IPathData DrawDataSetSelection (Path path, ParameterStore editorParams)
		{
			IPathData ds = GetSelectedDataSet (path, editorParams, true);
		
			int dataSetIndex = path.IndexOfDataSet (ds);

			List<string> dataSetNames;
			List<int> dataSetIds;
			FindAvailableDataSets (path, out dataSetNames, out dataSetIds, " (default)");

			
			EditorGUILayout.BeginHorizontal ();
			EditorGUI.BeginChangeCheck ();
			dataSetIndex = EditorGUILayout.Popup ("Data Set", dataSetIndex, dataSetNames.ToArray ());
			if (EditorGUI.EndChangeCheck ()) {
				int dataSetId = dataSetIds [dataSetIndex];
				SetSelectedDataSet (dataSetId, editorParams);
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
			return ds;
		}


		public static void DrawInputSourceSelection (Path path, IPathData pathData)
		{

			UnityEngine.Object target = path;

			bool pathChanged = false;
			
			List<string> inputSourceNames = new List<string> ();
			List<PathDataInputSource.SourceType> inputSourceTypes = new List<PathDataInputSource.SourceType> ();
			
			string pathTypeName = path.GetType ().Name;
			
			inputSourceNames.Add ("Self (" + pathTypeName + ")");
			inputSourceTypes.Add (PathDataInputSource.SourceType.Self);
			
			inputSourceNames.Add ("None");
			inputSourceTypes.Add (PathDataInputSource.SourceType.None);
			
			inputSourceNames.Add ("Data Set");
			inputSourceTypes.Add (PathDataInputSource.SourceType.DataSet);
			
			Dictionary<PathDataInputSource.SourceType, int> sourceTypeToSelectionIndex = 
				new Dictionary<PathDataInputSource.SourceType, int> ();
			for (int i = 0; i < inputSourceTypes.Count; i++) {
				sourceTypeToSelectionIndex [inputSourceTypes [i]] = i;
			}
			
			//			// Data sets:
			//			List<string> dataSetNames;
			//			List<int> dataSetIds;
			//			FindAvailableDataSets (out dataSetNames, out dataSetIds);
			//
			//			Dictionary<int, int> dsIdToSelectionIndex = new Dictionary<int, int> ();
			//			for (int i = 0; i < dataSetNames.Count; i++) {
			//				string dsName = dataSetNames [i];
			//				inputSourceNames.Add (dsName);
			//				dsIdToSelectionIndex.Add (dataSetIds [i], i + firstDataSetSelectionIndex);
			//			}
			
			PathDataInputSource inputSource = pathData.GetInputSource ();
			int sourceTypeIndex = sourceTypeToSelectionIndex [inputSource.GetSourceType ()];
			
			EditorGUI.BeginChangeCheck ();
			sourceTypeIndex = EditorGUILayout.Popup ("Input Source", sourceTypeIndex, inputSourceNames.ToArray ());
			if (EditorGUI.EndChangeCheck ()) {
				Undo.RecordObject (target, "Change Path Input Source");
				
				PathDataInputSource.SourceType sourceType = inputSourceTypes [sourceTypeIndex];
				path.SetDataSetInputSourceType (pathData, sourceType);
				
				pathChanged = true;
			}
			
			// Refresh the inputSource:
			inputSource = pathData.GetInputSource ();
			
			EditorGUI.indentLevel++;
			
			if (inputSource.GetSourceType () == PathDataInputSource.SourceType.DataSet) {
				PathDataInputSourceDataSet dataSetSource = (PathDataInputSourceDataSet)inputSource;
				
				Path currentParentPath = Path.FindParentPathObject (path.transform);
				string currentParentPathName = (null != currentParentPath) ? currentParentPath.name : "none";
				
				List<string> sourcePathTypeNames = new List<String> ();
				sourcePathTypeNames.Add ("<Self> (" + pathTypeName + ")");
				sourcePathTypeNames.Add ("<Parent> (" + currentParentPathName + ")");
				sourcePathTypeNames.Add ("Specific Path...");
				
				Path sourcePath;
				int sourcePathSelIndex;
				if (dataSetSource.IsSourcePathSelf ()) {
					sourcePathSelIndex = 0;
					sourcePath = path;
				} else if (dataSetSource.IsSourcePathParent ()) {
					sourcePathSelIndex = 1;
					sourcePath = Path.FindParentPathObject (path.transform);
				} else {
					sourcePathSelIndex = 2;
					sourcePath = dataSetSource.GetSourcePath ();
					sourcePathTypeNames [sourcePathSelIndex] = "Specific Path";
				}
				
				EditorGUI.BeginChangeCheck ();
				sourcePathSelIndex = EditorGUILayout.Popup ("Source Path Selection", sourcePathSelIndex, sourcePathTypeNames.ToArray ());
				if (EditorGUI.EndChangeCheck ()) {
					Undo.RecordObject (path, "Change Source Path Selection");
					
					if (sourcePathSelIndex == 0) {
						// Self
						dataSetSource = dataSetSource.WithSourcePathIsSelf (true);
					} else if (sourcePathSelIndex == 1) {
						// Parent
						dataSetSource = dataSetSource.WithSourcePathIsSelf (false).WithSourcePathIsParent (true);
					} else if (sourcePathSelIndex == 2) {
						// Specific path
						dataSetSource = dataSetSource.WithSourcePathIsSelf (false).WithSourcePathIsParent (false);
					}
					path.SetDataSetInputSource (pathData, dataSetSource);
					
					pathChanged = true;
				}
				
				if (!(dataSetSource.IsSourcePathSelf () || dataSetSource.IsSourcePathParent ())) {
					// Source path selection
					EditorGUI.indentLevel++;
					
					EditorGUI.BeginChangeCheck ();
					sourcePath = (Path)EditorGUILayout.ObjectField ("Source Path", dataSetSource.GetSourcePath (), typeof(Path), true);
					if (EditorGUI.EndChangeCheck ()) {
						Undo.RecordObject (path, "Change Source Path");
						dataSetSource = dataSetSource.WithSourcePath (sourcePath);
						path.SetDataSetInputSource (pathData, dataSetSource);
						pathChanged = true;
					}
					EditorGUI.indentLevel--;
					
				}
				
				// Data set
				List<string> dataSetNames;
				List<int> dataSetIds;
				
				FindAvailableDataSets (sourcePath, (sourcePath == path) ? pathData : null, out dataSetNames, out dataSetIds);
				
				// Selection for the Default data set:
				string currentDefaultDataSetName;
				if (null != sourcePath) {
					int defaultDsId = sourcePath.GetDefaultDataSetId ();
					IPathData defaultDs = sourcePath.FindDataSetById (defaultDsId);
					currentDefaultDataSetName = "(" + defaultDs.GetName () + ")";
				} else {
					currentDefaultDataSetName = "";
				}
				dataSetNames.Insert (0, "<Default> " + currentDefaultDataSetName);
				dataSetIds.Insert (0, -1);

				int selectedDsId = dataSetSource.GetDataSetId ();

				if (!dataSetIds.Contains (selectedDsId)) {
					dataSetNames.Insert (1, "** Deleted Data Set **");
					dataSetIds.Insert (1, selectedDsId);
				}
				
				List<string> dataSetDisplayNames = new List<string> ();
				//				if (/*this.path != sourcePath && */sourcePath != null) {
				//					string sourcePathName = sourcePath.name;
				//					foreach (string dsName in dataSetNames) {
				//						dataSetDisplayNames.Add (sourcePathName + ":" + dsName);
				//					}
				//				} else {
				dataSetDisplayNames.AddRange (dataSetNames);
				//				}
				
				// Find selected index
				int selectedDsIndex = dataSetIds.IndexOf (selectedDsId);

				EditorGUI.BeginChangeCheck ();
				selectedDsIndex = EditorGUILayout.Popup ("Data Set", selectedDsIndex, dataSetDisplayNames.ToArray ());
				if (EditorGUI.EndChangeCheck ()) {
					Undo.RecordObject (path, "Change Source Path Data Set");
					
					selectedDsId = dataSetIds [selectedDsIndex];
					dataSetSource = dataSetSource.WithDataSetId (selectedDsId);
					path.SetDataSetInputSource (pathData, dataSetSource);
					pathChanged = true;

				}
				// Source path is self?
				//
				//
				// SNAPSHOT selection:
				
				EditorGUILayout.BeginHorizontal ();
				
				bool fromSnapshot = dataSetSource.IsFromSnapshot ();
				
				EditorGUI.BeginChangeCheck ();
				fromSnapshot = EditorGUILayout.Toggle ("From Snapshot", fromSnapshot, GUILayout.ExpandWidth (false));
				if (EditorGUI.EndChangeCheck ()) {
					Undo.RecordObject (path, "Changed Source DataSet/Snapshot");
					
					dataSetSource = path.SetDataSetInputSource (pathData, dataSetSource.WithFromSnapshot (fromSnapshot));
					
					pathChanged = true;
				}
				
				string snapshotName = dataSetSource.GetSnapshotName ();
				// TODO currently we have no easy way to find available snapshots - they are created 
				// when path modifiers of the source dataset are ran
				EditorGUI.BeginDisabledGroup (!fromSnapshot);
				EditorGUI.BeginChangeCheck ();
				snapshotName = GUILayout.TextField (snapshotName);
				if (EditorGUI.EndChangeCheck ()) {
					Undo.RecordObject (path, "Change Source DataSet/Snapshot");
					dataSetSource = path.SetDataSetInputSource (pathData, dataSetSource.WithSnapshotName (snapshotName));
					
					pathChanged = true;
				}
				EditorGUI.EndDisabledGroup ();
				EditorGUILayout.EndHorizontal ();
				
				//
			}
			EditorGUI.indentLevel--;
			
			if (pathChanged) {
				EditorUtility.SetDirty (path);
				// TODO should we notify the path?
				//				path.PathPointsChanged ();
			}
		}
	}

	// TODO refactor this class; it's getting too complex
    
}
