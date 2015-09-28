// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using System;
using System.Reflection;

using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using Util;
using Util.Editor;
using Paths;

namespace Paths.Editor
{
	// TODO implement these:
//	public interface IPathModifierInputFilterEditor : ICustomToolEditor
//	{
//	}
//	public class PathModifierInputFilterEditorContext : CustomToolEditorContext
//	{
//	}


	[PluginEditor(typeof(StoreSnapshotPathModifier))]
	public class StoreSnapshotPathModifierEditor : AbstractPathModifierEditor<StoreSnapshotPathModifier>
	{

		protected override void OnDrawConfigurationGUI ()
		{
			StoreSnapshotPathModifier pm = context.PathModifier as StoreSnapshotPathModifier;

			EditorGUI.BeginChangeCheck ();
			pm.snapshotName = EditorGUILayout.TextField ("Snapshot Name", pm.snapshotName);
			if (EditorGUI.EndChangeCheck ()) {
				Undo.RecordObject (context.Target, "Change Snapshot Name to '" + pm.snapshotName + "'");
				context.TargetModified ();
				EditorUtility.SetDirty (context.Target);
			}
//			BranchToDataSetPathModifier pm = (BranchToDataSetPathModifier)context.PathModifier;
//			Path path = context.Path;
//			int thisPmIndex = context.PathModifierContainer.IndexOf (pm);
//
//			string targetName = pm.GetTargetDataSetName ();
//
//			// Collect list of target data set names
//			List<string> datasetNames = new List<string> ();
//			int dsCount = path.GetDataSetCount ();
//			int defaultDsIndex = path.GetDefaultDataSetIndex ();
//			bool currentExists = StringUtil.IsEmpty (targetName);
//			for (int i = 0; i < dsCount; i++) {
//				PathData data = path.GetDataSet (i);
//				if (i != defaultDsIndex && i != thisPmIndex) {
//					// Add to list of available data sets
//					string n = data.GetName ();
//					datasetNames.Add (n);
//					if (n == targetName) {
//						currentExists = true;
//					}
//				}
//			}
//
//
//			int selectedIndex; 
//			if (currentExists) {
//				selectedIndex = datasetNames.IndexOf (targetName);
//			} else {
//				datasetNames.Insert (0, "*MISSING* " + targetName);
//				selectedIndex = 0;
//			}
//
//			EditorGUI.BeginChangeCheck ();
//			selectedIndex = EditorGUILayout.Popup ("Target Dataset", selectedIndex, datasetNames.ToArray ());
//			if (EditorGUI.EndChangeCheck () && selectedIndex >= (currentExists ? 0 : 1)) {
//				Undo.RecordObject (context.Target, "Change Branch To Dataset Target");
//				pm.targetDataSetName = datasetNames [selectedIndex];
//				context.TargetModified ();
//			}

//			DrawDefaultConfigurationGUI ();
		}
		protected override void OnDrawProcessMatrix ()
		{
//			base.OnDrawProcessMatrix ();
		}
//		protected override void OnDrawInspectorGUI ()
//		{
//			DrawDefaultInspectorGUI();
//		}
	}
	
}
