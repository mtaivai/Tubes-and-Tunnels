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


	[CustomToolEditor(typeof(IndexRangePathModifierInputFilter))]
	public class IndexRangePathModifierInputFilterEditor : ICustomToolEditor
	{
		CustomToolEditorContext context;
		IndexRangePathModifierInputFilter filter;

		public void DrawInspectorGUI (CustomToolEditorContext context)
		{
			this.context = context;
			this.filter = (IndexRangePathModifierInputFilter)context.CustomTool;
			OnDrawInspectorGUI ();
		}

		protected void OnDrawInspectorGUI ()
		{

//			EditorGUI.BeginChangeCheck ();
//			filter.FirstPointIndex = EditorGUILayout.IntField ("Start Index", filter.FirstPointIndex);
//			if (EditorGUI.EndChangeCheck ()) {
//				context.TargetModified ();
//			}
//			EditorGUI.BeginChangeCheck ();
//			filter.PointCount = EditorGUILayout.IntField ("Point Count", filter.PointCount);
//			if (EditorGUI.EndChangeCheck ()) {
//				context.TargetModified ();
//			}
			ConfigParamField (filter.firstPointIndexParam, "Start Index");
			ConfigParamField (filter.pointCountParam, "Point Count");


		}

		void ConfigParamField (IndexRangePathModifierInputFilter.ConfigParam configParam, string label = null)
		{


			label = StringUtil.IsEmpty (label) ? configParam.Name : label;


			int selectedParamIndex = configParam.fromContext ? 1 : 0;
			string[] availableParams = {
				"[Value]",
				"[Parameter]",
				"Include.IncludedPathStartIndex",
				"Include.IncludedPathEndIndex",
				"Include.IncludedPathPointCount"
			};
			bool predefinedParameter = false;
			for (int i = 0; i < availableParams.Length; i++) {
				if (configParam.contextParamName == availableParams [i]) {
					selectedParamIndex = i;
					predefinedParameter = true;
					break;
				}
			}

			EditorGUILayout.BeginHorizontal ();
			EditorGUI.BeginChangeCheck ();
			selectedParamIndex = EditorGUILayout.Popup (label, 
				selectedParamIndex, availableParams, GUILayout.ExpandWidth (selectedParamIndex > 1));
			configParam.fromContext = (selectedParamIndex >= 1);
			if (EditorGUI.EndChangeCheck ()) {
				if (selectedParamIndex >= 2) {
					configParam.contextParamName = availableParams [selectedParamIndex];
				} else if (selectedParamIndex == 1 && predefinedParameter) {
					configParam.contextParamName = "";
				}
				context.TargetModified ();
			}
			if (selectedParamIndex == 1) {

				int indentLevel = EditorGUI.indentLevel;
				EditorGUI.indentLevel = 0;
							
				EditorGUI.BeginChangeCheck ();
				configParam.contextParamName = EditorGUILayout.TextField (configParam.contextParamName, GUILayout.ExpandWidth (true));
				if (EditorGUI.EndChangeCheck ()) {
					context.TargetModified ();
				}
				EditorGUI.indentLevel = indentLevel;

			}
			EditorGUILayout.EndHorizontal ();
//
//			int indentLevel = EditorGUI.indentLevel;
//			EditorGUI.indentLevel = 0;
//			EditorGUI.indentLevel = indentLevel;


//			EditorGUILayout.BeginHorizontal ();
//			int valueSource = configParam.fromContext ? 1 : 0;
//			EditorGUI.BeginChangeCheck ();
//			valueSource = EditorGUILayout.Popup (label, valueSource, new string[] {"Value", "Parameter"}, GUILayout.ExpandWidth (false));
//			if (EditorGUI.EndChangeCheck ()) {
//				configParam.fromContext = (valueSource == 1);
//				context.TargetModified ();
//			}


//			int prevIndentLevel = EditorGUI.indentLevel;
//			EditorGUI.indentLevel = 0;

			// Parameter selection
			// TODO implement selection popup - we need to have a way to collect available parameters
			// from all pathmodifiers before this! That list could be collected by the invoking GUI




//			EditorGUILayout.EndHorizontal ();

			if (configParam.fromContext) {
//				string[] operatorValues = new String[] {" ", "+", "-"};
//				EditorGUI.BeginChangeCheck ();
//				configParam.paramOperator = (IndexRangePathModifierInputFilter.ConfigParam.ParamOperator)EditorGUILayout.Popup (
//					(int)configParam.paramOperator, operatorValues, GUILayout.ExpandWidth (true), GUILayout.MaxWidth (40));
//				if (EditorGUI.EndChangeCheck ()) {
//					context.TargetModified ();
//				}
//				EditorGUILayout.EndHorizontal ();


//				EditorGUI.BeginDisabledGroup ((int)configParam.paramOperator == 0);
				EditorGUI.indentLevel++;
				EditorGUI.BeginChangeCheck ();
				configParam.paramOperand = EditorGUILayout.IntField ("Value Adjust", configParam.paramOperand, GUILayout.ExpandWidth (false));
				if (configParam.paramOperand == 0) {
					configParam.paramOperator = IndexRangePathModifierInputFilter.ConfigParam.ParamOperator.None;
				} else {
					configParam.paramOperator = IndexRangePathModifierInputFilter.ConfigParam.ParamOperator.Plus;
				}
				if (EditorGUI.EndChangeCheck ()) {
					context.TargetModified ();
				}
				EditorGUI.indentLevel--;
//				EditorGUI.EndDisabledGroup ();

				
				//EditorGUILayout.Popup (0, new string[] {"", "-", "+"});
				//EditorGUILayout.IntField (0);

			} else {
				// Static value
				EditorGUI.indentLevel++;
				EditorGUI.BeginChangeCheck ();
				configParam.value = EditorGUILayout.IntField ("Value", configParam.value, GUILayout.ExpandWidth (false));
				if (EditorGUI.EndChangeCheck ()) {
					context.TargetModified ();
				}
				EditorGUI.indentLevel--;
			}

//			EditorGUI.indentLevel = prevIndentLevel;
		}
	}
	[CustomToolEditor(typeof(IncludePathModifier))]
	public class IncludePathModifierEditor : AbstractPathModifierEditor
	{
		public override void DrawInspectorGUI (PathModifierEditorContext context)
		{
			IncludePathModifier pm = (IncludePathModifier)context.PathModifier;
            
			Path includedPath = pm.GetIncludedPath (context.PathModifierContainer.GetReferenceContainer ());
			EditorGUI.BeginChangeCheck ();
			Path newPath = (Path)EditorGUILayout.ObjectField ("Included Path", includedPath, typeof(Path), true);
			if (EditorGUI.EndChangeCheck ()) {
				// TODO Undo.RecordObject
				if (newPath == context.Path) {
					EditorUtility.DisplayDialog ("Recursive Include", "Path can't be included recursively to itself!", "Got it!");
				} else if (IsPathIncludedIn (context.Path, newPath)) {
					EditorUtility.DisplayDialog (
                        "Recursive Include", 
                        "Path '" + newPath.name + "' already includes '" + context.Path.name + "'", 
                        "Got it!");
				} else {
					pm.SetIncludedPath (context.PathModifierContainer.GetReferenceContainer (), newPath);
				}
				//EditorUtility.SetDirty(context.Target);
				context.TargetModified ();
				//              trackInspector.TrackGeneratorModified();
			}
            
			// TODO this is not up-to-date when we first draw the inspector!
			int inputPointCount = pm.GetCurrentInputPointCount ();
			int sliderPos = pm.includePosition;
			if (sliderPos < 0) {
				sliderPos = inputPointCount;
			}
			EditorGUI.BeginChangeCheck ();
			sliderPos = EditorGUILayout.IntSlider ("Insert At Index", sliderPos, 0, inputPointCount);
			if (EditorGUI.EndChangeCheck ()) {
				// TODO record UNDO!
				pm.includePosition = sliderPos;
				context.TargetModified ();
			}

			EditorGUI.BeginChangeCheck ();
			pm.removeDuplicates = EditorGUILayout.Toggle ("Smart Include", pm.removeDuplicates);
			if (EditorGUI.EndChangeCheck ()) {
				// TODO record UNDO!
				context.TargetModified ();
			}

			EditorGUI.BeginChangeCheck ();
			EditorGUI.BeginDisabledGroup (pm.includePosition == 0);
			pm.alignFirstPoint = EditorGUILayout.Toggle ("Align First Point", pm.alignFirstPoint);
			EditorGUI.EndDisabledGroup ();
			if (EditorGUI.EndChangeCheck ()) {
				// TODO record UNDO!
				context.TargetModified ();
			}

			if (pm.alignFirstPoint) {
				EditorGUI.BeginDisabledGroup (true);
				// TODO this is not up-to-date when we first draw the inspector!
				EditorGUILayout.Vector3Field ("Position Offset", pm.GetCurrentIncludedPathPosOffset ());
				EditorGUI.EndDisabledGroup ();
			} else {
				EditorGUI.BeginChangeCheck ();
				pm.includedPathPosOffset = EditorGUILayout.Vector3Field ("Position Offset", pm.includedPathPosOffset);
				if (EditorGUI.EndChangeCheck ()) {
					// TODO record UNDO!
					context.TargetModified ();
				}
			}
			// TODO these are not up-to-date when we first draw the inspector!
			EditorGUILayout.LabelField ("Included Index Offs", "" + pm.GetCurrentIncludedIndexOffset ());
			EditorGUILayout.LabelField ("Included Point Count", "" + pm.GetCurrentIncludedPointCount ());

		}

		static bool IsPathIncludedIn (Path path, Path containerPath)
		{
			if (containerPath == path) {
				return true;
			}
			IReferenceContainer refContainer = containerPath.GetPathModifierContainer ().GetReferenceContainer ();
			IPathModifier[] pathModifiers = containerPath.GetPathModifierContainer ().GetPathModifiers ();
			foreach (IPathModifier pm in pathModifiers) {
				if (!(pm is IncludePathModifier)) {
					continue;
				}
				IncludePathModifier ipm = (IncludePathModifier)pm;
				if (ipm.GetIncludedPath (refContainer) == path) {
					return true;
				} else {
					// Recursive lookup
					if (IsPathIncludedIn (path, ipm.GetIncludedPath (refContainer))) {
						return true;
					}
				}

			}
			return false;
		}
	}
}
