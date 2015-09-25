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

	// TODO refactor this class; it's getting too complex

	class PathEditorMetadataPart<TPath, TPathData> where TPath: Path where TPathData: IPathData
	{
		private Dictionary<string, string> _newWeightName = new Dictionary<string, string> ();
		private Dictionary<string, float> _previousWeightMinValue = new Dictionary<string, float> ();
		private Dictionary<string, float> _previousWeightMaxValue = new Dictionary<string, float> ();
		private Dictionary<string, string> _weightRenameErrors = new Dictionary<string, string> ();

		private AbstractPathEditor<TPath, TPathData> pathEditor;

		public PathEditorMetadataPart (AbstractPathEditor<TPath, TPathData> pathEditor)
		{
			this.pathEditor = pathEditor;
		}
		private void Repaint ()
		{
			pathEditor.Repaint ();
		}
		public void DrawInspectorGUI ()
		{
			this.DrawWeightsInspectorGUI ();
		}
		protected void DrawWeightsInspectorGUI ()
		{
			IPathData pathData = pathEditor.PathData;
			UnityEngine.Object target = pathEditor.Path;

			//EditorGUILayout.LabelField ("Weights");
			if (pathData.IsPathMetadataSupported ()) {
				EditorGUILayout.HelpBox (
					"Following is list of available weight parameters for points in this data set." + 
					" Default value can be used to specify the value that will be used for points without defined weight.", MessageType.Info, true);
				
				Rect controlRect = EditorGUILayout.BeginHorizontal (GUILayout.ExpandWidth (true));
				//				Debug.Log ("CTRL: " + controlRect);
				float defaultValueFieldWidth = 40;
				float minValueFieldWidth = 40;
				float maxValueFieldWidth = 40;
				float actionsFieldWidth = 40;
				//				float nameFieldWidth = controlRect.width - defaultValueFieldWidth - minValueFieldWidth - maxValueFieldWidth - actionsFieldWidth;
				//				if (nameFieldWidth < 0) {
				//					nameFieldWidth = 80;
				//				}
				//				Debug.Log ("nw: " + nameFieldWidth);
				GUILayout.Label ("Name", GUILayout.ExpandWidth (true));
				GUILayout.Label ("Default", GUILayout.Width (defaultValueFieldWidth));
				GUILayout.Label ("Min", GUILayout.Width (minValueFieldWidth));
				GUILayout.Label ("Max", GUILayout.Width (maxValueFieldWidth));
				GUILayout.Label ("Actions", GUILayout.Width (actionsFieldWidth));
				
				EditorGUILayout.EndHorizontal ();
				
				IPathMetadata md = pathData.GetPathMetadata ();
				IEditablePathMetadata emd = md as IEditablePathMetadata;
				int count = md.GetWeightDefinitionCount ();
				for (int i = 0; i < count; i++) {
					WeightDefinition wd = md.GetWeightDefinitionAtIndex (i);
					string weightId = wd.WeightId;
					
					// Name (id)
					EditorGUI.BeginDisabledGroup (null == emd);
					EditorGUILayout.BeginHorizontal (GUILayout.ExpandWidth (true));
					
					EditorGUI.BeginChangeCheck ();
					if (!_newWeightName.ContainsKey (weightId)) {
						_newWeightName [weightId] = weightId;
					}
					_newWeightName [weightId] = EditorGUILayout.TextField (_newWeightName [weightId]);
					if (EditorGUI.EndChangeCheck () && null != emd) {
						//emd.SetDefinedWeightName (weightId, name);
						
						//						EditorUtility.SetDirty (target);
					}
					
					bool renaming = _newWeightName [weightId] != weightId;
					
					if (renaming) {
						// Enter: U+2386 ⎆
						// Cancel: U+2418 ␘
						
						Event ev = Event.current;
						if (ev.keyCode == KeyCode.Return
							|| ev.keyCode == KeyCode.KeypadEnter
							|| GUILayout.Button ("OK", EditorStyles.miniButtonLeft, GUILayout.ExpandWidth (false))) {
							try {
								emd.RenameWeightDefinition (weightId, _newWeightName [weightId]);
								_newWeightName.Remove (weightId);
								if (_weightRenameErrors.ContainsKey (weightId)) {
									_weightRenameErrors.Remove (weightId);
								}
								EditorUtility.SetDirty (target);
							} catch (ArgumentException e) {
								_weightRenameErrors [weightId] = e.Message;
							}
							
						}
						if (ev.keyCode == KeyCode.Escape
							|| GUILayout.Button ("Cancel", EditorStyles.miniButtonRight, GUILayout.ExpandWidth (false))) {
							_newWeightName.Remove (weightId);
							this.Repaint ();
						}
					} else {
						if (_weightRenameErrors.ContainsKey (weightId)) {
							_weightRenameErrors.Remove (weightId);
						}
						
						// Default Value
						//					EditorGUILayout.BeginHorizontal ();
						EditorGUI.BeginChangeCheck ();
						bool hasDefault = wd.HasDefaultValue;
						hasDefault = EditorGUILayout.Toggle (hasDefault, GUILayout.Width (10f));
						if (EditorGUI.EndChangeCheck ()) {
							wd = emd.ModifyWeightDefinition (wd.WithHasDefaultValue (hasDefault));
							EditorUtility.SetDirty (target);
						}
						EditorGUI.BeginDisabledGroup (!hasDefault);
						EditorGUI.BeginChangeCheck ();
						float defaultValue = wd.DefaultValue;
						defaultValue = EditorGUILayout.FloatField (defaultValue, GUILayout.Width (defaultValueFieldWidth - 10f));
						if (EditorGUI.EndChangeCheck ()) {
							wd = emd.ModifyWeightDefinition (wd.WithDefaultValue (defaultValue));
							EditorUtility.SetDirty (target);
						}
						EditorGUI.EndDisabledGroup ();
						//					EditorGUILayout.EndHorizontal ();
						
						// Min Value
						//					EditorGUILayout.BeginHorizontal (GUILayout.Width (minValueFieldWidth));
						float min = wd.MinValue;
						if (DrawWeightMinMaxField (weightId, ref min, float.NegativeInfinity, _previousWeightMinValue, minValueFieldWidth)) {
							wd = emd.ModifyWeightDefinition (wd.WithMinValue (min));
						}
						//					EditorGUILayout.EndHorizontal ();
						
						// Max Value
						//					EditorGUILayout.BeginHorizontal ();
						float max = wd.MaxValue;
						if (DrawWeightMinMaxField (weightId, ref max, float.PositiveInfinity, _previousWeightMaxValue, maxValueFieldWidth)) {
							wd = emd.ModifyWeightDefinition (wd.WithMaxValue (max));
						}
						//					EditorGUILayout.EndHorizontal ();
					}
					// Actions
					EditorGUILayout.BeginHorizontal (GUILayout.Width (actionsFieldWidth)); // actions field
					
					EditorGUI.BeginDisabledGroup (i == 0); // move up
					if (GUILayout.Button ("^", EditorStyles.miniButtonLeft, GUILayout.ExpandWidth (false))) {
						emd.SetWeightDefinitionListIndex (weightId, i - 1);
					}
					EditorGUI.EndDisabledGroup (); // move up
					
					EditorGUI.BeginDisabledGroup (i == count - 1); // move down
					if (GUILayout.Button ("v", EditorStyles.miniButtonMid, GUILayout.ExpandWidth (false))) {
						emd.SetWeightDefinitionListIndex (weightId, i + 1);
					}
					EditorGUI.EndDisabledGroup (); // move down
					
					if (GUILayout.Button ("-", EditorStyles.miniButtonRight, GUILayout.ExpandWidth (false))) {
						int answer = EditorUtility.DisplayDialogComplex (
							"Delete Weight Parameter", 
							"You're about to delete a weight parameter '" + weightId + "'." + 
							" Do you want to delete just the parameter definition or all the currently assigned values too?",
							"Delete but keep values",
							"Cancel",
							"Delete values too");
						switch (answer) {
						case 0:
							// Delete definition but keep values
							emd.RemoveWeightDefinition (weightId);
							break;
						case 1:
							// Cancel
							break;
						case 2:
							// Delete everything, including current values!
							// TODO we can't do this
							if (EditorUtility.DisplayDialog (
								"Delete Weight Parameter", 
								"It seems like I can't delete currently assigned value." + 
								" Do you still wish to delete the parameter definition?",
								"Yes",
								"No")) {
								emd.RemoveWeightDefinition (weightId);
							}
							break;
						}
					}
					EditorGUILayout.EndHorizontal (); // actions field
					
					EditorGUILayout.EndHorizontal (); // row
					
					if (renaming && _weightRenameErrors.ContainsKey (weightId)) {
						EditorGUILayout.HelpBox (_weightRenameErrors [weightId], MessageType.Error);
					}
					
					EditorGUI.EndDisabledGroup (); // null == emd
				}
				
				EditorGUI.BeginDisabledGroup (null == emd);
				if (GUILayout.Button ("Add Weight", GUILayout.ExpandWidth (false))) {
					emd.AddWeightDefinition ();
					//					emd.SetDefinedWeightName (newId, "weight_" + newId);
					EditorUtility.SetDirty (target);
				}
				EditorGUI.EndDisabledGroup ();
				
			} else {
				EditorGUILayout.HelpBox ("Path Metadata is not supported", MessageType.Info);
			}
			
		}
		
		private bool DrawWeightMinMaxField (string weightId, ref float value, float undefValue, Dictionary<string, float> previousValueMap, float width)
		{
			//			EditorGUILayout.BeginHorizontal ();
			bool changed = false;
			
			bool hasValue = value != undefValue;
			
			EditorGUI.BeginChangeCheck ();
			hasValue = EditorGUILayout.Toggle (hasValue, GUILayout.Width (10f));
			if (EditorGUI.EndChangeCheck ()) {
				changed = true;
				if (!hasValue) {
					previousValueMap [weightId] = value;
					value = undefValue;
				} else {
					if (previousValueMap.ContainsKey (weightId)) {
						value = previousValueMap [weightId];
					} else {
						value = 0f;
					}
				}
			}
			if (hasValue) {
				EditorGUI.BeginChangeCheck ();
				value = EditorGUILayout.FloatField (value, GUILayout.Width (width - 10f));
				if (EditorGUI.EndChangeCheck ()) {
					changed = true;
					EditorUtility.SetDirty (pathEditor.Path);
				}
			} else {
				EditorGUI.BeginDisabledGroup (true);
				EditorGUILayout.TextField ("", GUILayout.Width (width - 10f));
				EditorGUI.EndDisabledGroup ();
			}
			//					EditorGUILayout.EndHorizontal ();
			return changed;
		}
	}
    
}
