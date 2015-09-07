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

	public class PathModifierInputFilterEditorContext : CustomToolEditorContext
	{
		private PathModifierEditorContext pathModifierEditorContext;

		public PathModifierInputFilterEditorContext (PathModifierInputFilter customTool, PathModifierEditorContext pmec)
		: base(customTool, pmec.Target, pmec.EditorHost, pmec.TargetModified, pmec.EditorPrefs)
		{
			this.pathModifierEditorContext = pmec;
		}
		public PathModifierEditorContext PathModifierEditorContext {
			get { return pathModifierEditorContext; }
		}
	}

	[CustomToolEditor(typeof(IndexRangePathModifierInputFilter))]
	public class IndexRangePathModifierInputFilterEditor : ICustomToolEditor
	{
		PathModifierInputFilterEditorContext context;
		IndexRangePathModifierInputFilter filter;

		public void DrawInspectorGUI (CustomToolEditorContext context)
		{
			this.context = (PathModifierInputFilterEditorContext)context;
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

			List<string> availableParams = new List<string> ();

			availableParams.Add ("[Value]");
			int valueSelectionIndex = availableParams.Count - 1;

			availableParams.Add ("[Parameter]");
			int customParamSelectionIndex = availableParams.Count - 1;

			int firstPredefParamSelectionindex = availableParams.Count;
			 
			Dictionary<string, object> p = context.PathModifierEditorContext.PathModifierContext.Parameters;
			foreach (string pn in p.Keys) {
				availableParams.Add (pn);
			}

			int selectedParamIndex;

			bool predefinedParameter = false;
			if (configParam.fromContext) {
				selectedParamIndex = customParamSelectionIndex;
				for (int i = 0; i < availableParams.Count; i++) {
					if (configParam.contextParamName == availableParams [i]) {
						selectedParamIndex = i;
						predefinedParameter = true;
						break;
					}
				}
			} else {
				selectedParamIndex = valueSelectionIndex;
			}


			EditorGUILayout.BeginHorizontal ();
			EditorGUI.BeginChangeCheck ();

			// Don't expand popup width if "value" is specified (to leave room for the input field)
			selectedParamIndex = EditorGUILayout.Popup (label, 
				selectedParamIndex, availableParams.ToArray (), GUILayout.ExpandWidth (selectedParamIndex != valueSelectionIndex));

			configParam.fromContext = (selectedParamIndex != valueSelectionIndex);
			if (EditorGUI.EndChangeCheck ()) {
				if (selectedParamIndex >= firstPredefParamSelectionindex) {
					configParam.contextParamName = availableParams [selectedParamIndex];
				} else if (selectedParamIndex == customParamSelectionIndex && predefinedParameter) {
					configParam.contextParamName = "";
				}
				context.TargetModified ();
			}
			if (selectedParamIndex == customParamSelectionIndex) {

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
		private string snapshotNameFromList = null;

		public class PathSelectorContainer
		{
			public PathSelector pathSelector;
		}
		protected override void OnDrawConfigurationGUI ()
		{
			//DrawDefaultInspectorGUI ();
			//return;
			IncludePathModifier pm = (IncludePathModifier)context.PathModifier;
			IReferenceContainer refContainer = context.PathModifierContainer.GetReferenceContainer ();

			Path includedPath = pm.GetIncludedPath (refContainer);

			int[] excludedDataSetIds;
			if (includedPath == context.Path) {
				excludedDataSetIds = new int[] {context.PathData.GetId ()};
			} else {
				excludedDataSetIds = new int[0];
			}

			// TODO make a utility method of this:
//			PathSelectorContainer psc = new PathSelectorContainer ();
//			psc.pathSelector = new PathSelector (includedPath, pm.includedPathDataSetId, pm.includedPathFromSnapshot, pm.includedPathSnapshotName);
//			SerializedProperty prop = new SerializedObject (psc).FindProperty ("pathSelector");
//			EditorGUILayout.PropertyField (prop);

			PathSelector dataId = new PathSelector (includedPath, pm.includedPathDataSetId, pm.includedPathFromSnapshot, pm.includedPathSnapshotName);

			if (snapshotNameFromList != null) {
				dataId = dataId.WithSnapshotName (snapshotNameFromList);
				pm.includedPathSnapshotName = dataId.SnapshotName;
				snapshotNameFromList = null;
				context.TargetModified ();
				EditorUtility.SetDirty (context.Path);
			}

			Action<string> setSnapshotNameCallback = (snapshotName) => {
				snapshotNameFromList = snapshotName;
			};

			if (PathEditorUtil.DrawPathDataSelection ("Included Path", ref dataId, true, setSnapshotNameCallback, excludedDataSetIds)) {
				// TODO RECORD UNDO
				pm.SetIncludedPath (context.PathModifierContainer.GetReferenceContainer (), dataId.Path);

				pm.includedPathDataSetId = dataId.DataSetId;
				pm.includedPathFromSnapshot = dataId.UseSnapshot;
				pm.includedPathSnapshotName = dataId.SnapshotName;

				context.TargetModified ();
				EditorUtility.SetDirty (context.Path);
			}
//


			// TODO this is not up-to-date when we first draw the inspector!
			int inputPointCount = pm.CurrentInputPointCount;
			int sliderPos = pm.includePosition;
			if (sliderPos < 0) {
				sliderPos = inputPointCount;
			}
			EditorGUI.BeginChangeCheck ();
			//sliderPos = EditorGUILayout.IntSlider ("Insert At Index", sliderPos, 0, inputPointCount);
			sliderPos = EditorGUILayout.IntField ("Insert at Index", sliderPos);
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
			pm.breakLoop = EditorGUILayout.Toggle ("Break Loop", pm.breakLoop);
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
//			


			if (pm.alignFirstPoint) {
				EditorGUI.BeginDisabledGroup (true);
				// TODO this is not up-to-date when we first draw the inspector!
				EditorGUILayout.Vector3Field ("Position Offset", pm.CurrentIncludedPathPosOffset);
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
//			EditorGUILayout.LabelField ("Included Index Offs", "" + pm.GetCurrentIncludedIndexOffset ());
//			EditorGUILayout.LabelField ("Included Point Count", "" + pm.GetCurrentIncludedPointCount ());

		}

		static bool IsPathIncludedIn (Path path, Path containerPath)
		{
			if (containerPath == path) {
				return true;
			}
			int dsCount = containerPath.GetDataSetCount ();

			for (int i = 0; i < dsCount; i++) {
				IPathModifierContainer pmc = containerPath.GetDataSetAtIndex (i).GetPathModifierContainer ();
				IPathModifier[] pathModifiers = pmc.GetPathModifiers ();
				IReferenceContainer refContainer = pmc.GetReferenceContainer ();

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

			}
			return false;
		}
	}
}
