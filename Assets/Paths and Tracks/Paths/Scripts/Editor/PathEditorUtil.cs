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

	[CustomPropertyDrawer(typeof(PathSelector))]
	public class PathWithDataIdPropertyDrawer : PropertyDrawer
	{
		private string snapshotNameFromList = null;
		
		public override float GetPropertyHeight (SerializedProperty property, GUIContent label)
		{
			float singleLineHeight = base.GetPropertyHeight (property, label);
			return singleLineHeight * 3f;
		}
		
		public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
		{
			// Using BeginProperty / EndProperty on the parent property means that
			// prefab override logic works on the entire property.
			EditorGUI.BeginProperty (position, label, property);
			
			float singleHeight = base.GetPropertyHeight (property, label);
			
			SerializedProperty pathProperty = property.FindPropertyRelative ("path");
			SerializedProperty dataSetIdProperty = property.FindPropertyRelative ("dataSetId");
			SerializedProperty useSnapshotProperty = property.FindPropertyRelative ("useSnapshot");
			SerializedProperty snapshotNameProperty = property.FindPropertyRelative ("snapshotName");
			
			
			PathEditorUtil.DoDrawPathSelector (position, singleHeight, label, pathProperty, dataSetIdProperty, useSnapshotProperty, snapshotNameProperty, 
			                                   (snapshotName) => snapshotNameFromList = snapshotName);
			
			if (null != snapshotNameFromList) {
				snapshotNameProperty.stringValue = snapshotNameFromList;
				snapshotNameFromList = null;
			}
			
			
			EditorGUI.EndProperty ();
			
		}
	}

	public sealed class PathEditorUtil
	{
		public static string FindDefaultDatasetName (Path path, string prefix = "", string suffix = "", string defaultValue = "", bool addPrefixAndSuffixToDefaultValue = false)
		{
			string defaultDsName = null;
			if (null != path) {
				IPathData defaultDs = path.GetDefaultDataSet ();
				if (null != defaultDs) {
					defaultDsName = prefix + defaultDs.GetName () + suffix;
				}
			}
			if (defaultDsName == null) {
				if (addPrefixAndSuffixToDefaultValue) {
					defaultDsName = prefix + defaultValue + suffix;
				} else {
					defaultDsName = defaultValue;
				}
			}
			return defaultDsName;
		}
		public static void FindAvailableDataSets (Path path, out List<string> dataSetNames, out List<int> dataSetIds, string addSuffixToDefaultName = "")
		{
			FindAvailableDataSets (path, new int[0], out dataSetNames, out dataSetIds, addSuffixToDefaultName);
		}

		public static void FindAvailableDataSets (Path path, IPathData excludedDataSet, out List<string> dataSetNames, out List<int> dataSetIds, string addSuffixToDefaultName = "")
		{
			FindAvailableDataSets (path, excludedDataSet != null ? new int[] {excludedDataSet.GetId () } : new int[0], out dataSetNames, out dataSetIds, addSuffixToDefaultName);
		}

		public static void FindAvailableDataSets (Path path, int[] excludedIds, out List<string> dataSetNames, out List<int> dataSetIds, string addSuffixToDefaultName = "")
		{

			dataSetNames = new List<string> ();
			dataSetIds = new List<int> ();

			if (null != path) {
				int defaultId = path.GetDefaultDataSetId ();


				int dsCount = path.GetDataSetCount ();
				for (int i = 0; i < dsCount; i++) {
					IPathData data = path.GetDataSetAtIndex (i);
					int id = data.GetId ();

					bool excluded = false;
					foreach (int excludedId in excludedIds) {
						if (id == excludedId) {
							excluded = true;
							break;
						}
					}
					if (!excluded) {
						string name = data.GetName ();
						if (id == defaultId && !StringUtil.IsEmpty (addSuffixToDefaultName)) {
							name += addSuffixToDefaultName;
						}
						dataSetNames.Add (name);
						dataSetIds.Add (id);
					}
				}
			}
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

		public static bool DrawPathDataSelection (ref PathSelector dataId, params int[] excludedDataSetIds)
		{
			return DrawPathDataSelection (ref dataId, true, excludedDataSetIds);
		}
		public static bool DrawPathDataSelection (GUIContent label, ref PathSelector dataId, params int[] excludedDataSetIds)
		{
			return DrawPathDataSelection (label, ref dataId, true, excludedDataSetIds);
		}
		public static bool DrawPathDataSelection (string label, ref PathSelector dataId, params int[] excludedDataSetIds)
		{
			return DrawPathDataSelection (new GUIContent (label), ref dataId, true, excludedDataSetIds);
		}

		public static bool DrawPathDataSelection (ref PathSelector dataId, bool showPathSelection, params int[] excludedDataSetIds)
		{
			return DrawPathDataSelection (GUIContent.none, ref dataId, showPathSelection, excludedDataSetIds);
		}
		public static bool DrawPathDataSelection (string label, ref PathSelector dataId, bool showPathSelection, params int[] excludedDataSetIds)
		{
			return DrawPathDataSelection (new GUIContent (label), ref dataId, showPathSelection, excludedDataSetIds);
		}
		public static bool DrawPathDataSelection (GUIContent label, ref PathSelector dataId, bool showPathSelection, params int[] excludedDataSetIds)
		{
			float singleLineHeight = 16f;

			Rect position = EditorGUILayout.GetControlRect (true, singleLineHeight * 3.0f);

			EditorGUI.BeginChangeCheck ();
			dataId = DoDrawPathSelector (position, singleLineHeight, label, dataId, (snapshotName) => {});
			return EditorGUI.EndChangeCheck ();

		}

//		static float GetSingleControlHeight ()
//		{
//			return 16f;
//		}

		class PropertyHolder<T>
		{
			public SerializedProperty property;
			public T value;
			public PropertyHolder (SerializedProperty property)
			{
				this.property = property;
				this.value = default(T);
			}
			public PropertyHolder (T value)
			{
				this.property = null;
				this.value = value;
			}
			public bool IsProperty ()
			{
				return null != property;
			}
		}

		static PathSelector DoDrawPathSelector (Rect position, float singleFieldHeight, GUIContent label, 
		                              PathSelector pathSelector,
		                              Action<string> setSnapshotNameCallback)
		{

			PropertyHolder<Path> pathProperty = new PropertyHolder<Path> (pathSelector.Path);
			PropertyHolder<int> dataSetIdProperty = new PropertyHolder<int> (pathSelector.DataSetId);
			PropertyHolder<bool> useSnapshotProperty = new PropertyHolder<bool> (pathSelector.UseSnapshot);
			PropertyHolder<string> snapshotNameProperty = new PropertyHolder<string> (pathSelector.SnapshotName);

			DoDrawPathSelector (position, singleFieldHeight, label, pathProperty, dataSetIdProperty, useSnapshotProperty, snapshotNameProperty, setSnapshotNameCallback);

			return new PathSelector (pathProperty.value, dataSetIdProperty.value, useSnapshotProperty.value, snapshotNameProperty.value);

		}

		public static void DoDrawPathSelector (Rect position, float singleFieldHeight, GUIContent label, 
		                              SerializedProperty pathProperty, 
		                              SerializedProperty dataSetIdProperty, 
		                              SerializedProperty useSnapshotProperty, 
		                              SerializedProperty snapshotNameProperty, 
		                              Action<string> setSnapshotNameCallback)
		{
			DoDrawPathSelector (position, singleFieldHeight, label, 
			          new PropertyHolder<Path> (pathProperty),
			          new PropertyHolder<int> (dataSetIdProperty),
			          new PropertyHolder<bool> (useSnapshotProperty),
			          new PropertyHolder<string> (snapshotNameProperty),
			          setSnapshotNameCallback);
		}

		static void DoDrawPathSelector (Rect position, float singleFieldHeight, GUIContent label, 
		                              PropertyHolder<Path> pathProperty, 
		                              PropertyHolder<int> dataSetIdProperty, 
		                              PropertyHolder<bool> useSnapshotProperty, 
		                              PropertyHolder<string> snapshotNameProperty, 
		                              Action<string> setSnapshotNameCallback)
		{

			// Draw label
			if (null != label && label != GUIContent.none) {
				position = EditorGUI.PrefixLabel (position, GUIUtility.GetControlID (FocusType.Passive), label);
			}
				
			// Don't make child fields be indented
			var indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			Rect pathRect = new Rect (position.x, position.y, position.width, singleFieldHeight);
			Rect dataSetIdRect = new Rect (position.x, pathRect.y + pathRect.height, position.width, singleFieldHeight);
			Rect snapshotRect = new Rect (position.x, dataSetIdRect.y + dataSetIdRect.height, position.width, singleFieldHeight);

			if (pathProperty.IsProperty ()) {
				EditorGUI.ObjectField (pathRect, pathProperty.property, GUIContent.none);
			} else {
				pathProperty.value = (Path)EditorGUI.ObjectField (pathRect, GUIContent.none, pathProperty.value, typeof(Path), true);
			}
				
				
			//		EditorGUI.PropertyField (dataSetIdRect, dataSetIdProperty, GUIContent.none);
				
			List<string> dataSetNames;
			List<int> dataSetIds;
			Path path = pathProperty.IsProperty () ? pathProperty.property.objectReferenceValue as Path : pathProperty.value;
			PathEditorUtil.FindAvailableDataSets (path, out dataSetNames, out dataSetIds, " (default)");
				
			string defaultDsName = PathEditorUtil.FindDefaultDatasetName (path, "<Default: ", ">", "", true);
			dataSetNames.Insert (0, defaultDsName);
			dataSetIds.Insert (0, 0);
				
			EditorGUI.BeginDisabledGroup (null == path);
			int currentDsId = dataSetIdProperty.IsProperty () ? dataSetIdProperty.property.intValue : dataSetIdProperty.value;
			int currentDsIndex = dataSetIds.IndexOf (currentDsId);
			//dataSetIdRect = EditorGUI.PrefixLabel (dataSetIdRect, new GUIContent ("Dataset"));
				
			Rect dataSetIdLabelRect = new Rect (dataSetIdRect.x, dataSetIdRect.y, 60f, dataSetIdRect.height);
			Rect dataSetPopupRect = new Rect (dataSetIdRect.x + 60f, dataSetIdRect.y, dataSetIdRect.width - 60f, dataSetIdRect.height);
			EditorGUI.HandlePrefixLabel (dataSetIdRect, dataSetIdLabelRect, new GUIContent ("Dataset"));
			int newDsIndex = EditorGUI.Popup (dataSetPopupRect, currentDsIndex, dataSetNames.ToArray ());
			if (newDsIndex != currentDsIndex) {
				currentDsIndex = newDsIndex;
				currentDsId = dataSetIds [newDsIndex];
				if (dataSetIdProperty.IsProperty ()) {
					dataSetIdProperty.property.intValue = currentDsId;
				} else {
					dataSetIdProperty.value = currentDsId;
				}
			}
			EditorGUI.EndDisabledGroup ();
				
			float xOffset = 0f;
			Rect snapshotLabelRect = new Rect (snapshotRect.x, snapshotRect.y, 60f, snapshotRect.height);
			xOffset += snapshotLabelRect.width;
				
			Rect snapshotToggleRect = new Rect (snapshotRect.x + xOffset, snapshotRect.y, 20f, snapshotRect.height);
			xOffset += snapshotToggleRect.width;
				
			Rect snapshotPopupRect = new Rect (snapshotRect.x + xOffset, snapshotRect.y, snapshotRect.width - xOffset, snapshotRect.height);
			//			xOffset += snapshotPopupRect.width;
				
			Rect snapshotNameRect = new Rect (snapshotPopupRect.x, snapshotPopupRect.y, snapshotPopupRect.width - 20, snapshotPopupRect.height);
			Rect snapshotBrowseButtonRect = new Rect (snapshotNameRect.x + snapshotNameRect.width, snapshotNameRect.y, 20, snapshotNameRect.height);
				
			IPathData pathData = null != path ? path.FindDataSetById (currentDsId) : null;
			IPathSnapshotManager ssm = null != pathData ? pathData.GetPathSnapshotManager () : UnsupportedSnapshotManager.Instance;
				
			EditorGUI.BeginDisabledGroup (!ssm.SupportsSnapshots ());
			{
				EditorGUI.HandlePrefixLabel (snapshotRect, snapshotLabelRect, new GUIContent ("Snapshot"));
				bool useSnapshot;
				if (useSnapshotProperty.IsProperty ()) {
					EditorGUI.PropertyField (snapshotToggleRect, useSnapshotProperty.property, GUIContent.none);
					useSnapshot = useSnapshotProperty.property.boolValue;
				} else {
					useSnapshot = EditorGUI.Toggle (snapshotToggleRect, GUIContent.none, useSnapshotProperty.value);
					useSnapshotProperty.value = useSnapshot;
				}
				EditorGUI.BeginDisabledGroup (!useSnapshot);
				{
					string currentSnapshotName = snapshotNameProperty.IsProperty () ? snapshotNameProperty.property.stringValue : snapshotNameProperty.value;
					List<string> snapshotNames = new List<string> (ssm.GetAvailableSnapshotNames ());
					int currentSnapshotIndex = snapshotNames.IndexOf (currentSnapshotName);
						
						
					//GUI.SetNextControlName("snapshotNameProperty");
					if (snapshotNameProperty.IsProperty ()) {
						EditorGUI.PropertyField (snapshotNameRect, snapshotNameProperty.property, GUIContent.none);
					} else {
						currentSnapshotName = EditorGUI.TextField (snapshotNameRect, GUIContent.none, currentSnapshotName);
					}						
					EditorGUI.BeginDisabledGroup (snapshotNames.Count < 1);
					if (GUI.Button (snapshotBrowseButtonRect, new GUIContent (""), EditorStyles.popup)) {
						// object userData, string[] options, int selected
						GUIContent[] snapshotNameContents = new GUIContent[snapshotNames.Count];
						for (int i = 0; i < snapshotNames.Count; i++) {
							snapshotNameContents [i] = new GUIContent (snapshotNames [i]);
						}
						// Remove focus from other controls (if the user was editing the snapshotName field, the focus
						// would prevent selection update
						GUI.FocusControl ("_BOGUS_");
						EditorUtility.DisplayCustomMenu (snapshotPopupRect, snapshotNameContents, currentSnapshotIndex, 
							                                 (userData, options, selected) => setSnapshotNameCallback (snapshotNames [selected]), snapshotNameProperty);
					}
					EditorGUI.EndDisabledGroup ();

				}
				EditorGUI.EndDisabledGroup ();
				//				
			}
			EditorGUI.EndDisabledGroup ();
				
			// Set indent back to what it was
			EditorGUI.indentLevel = indent;
		}



	}
    
}
