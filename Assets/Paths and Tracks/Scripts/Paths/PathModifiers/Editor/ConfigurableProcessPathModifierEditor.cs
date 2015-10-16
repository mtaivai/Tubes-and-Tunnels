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
	[PluginEditor(typeof(ConfigurableProcessPathModifier))]
	public class FallbackConfigurableProcessPathModifierEditor : ConfigurableProcessPathModifierEditor<ConfigurableProcessPathModifier>
	{
		protected override void DrawGeneralConfigurationGUI (PathModifierEditorContext context)
		{
			FallbackCustomToolEditor.DoDrawInspectorGUI (context, 
                                                         "PositionFunction", "DirectionFunction", "UpVectorFunction", 
                                                         "DistanceFromPreviousFunction", "DistanceFromBeginFunction", "AngleFunction");
		}
//        public override void OnDrawInspectorGUI(PathModifierEditorContext context)
//        {
//            //base.OnDrawInspectorGUI(context);
//        }
	}

	public abstract class ConfigurableProcessPathModifierEditor<T> : AbstractPathModifierEditor<T> where T : ConfigurableProcessPathModifier
	{
		delegate void DrawFunctionOptionsFunc (PathModifierEditorContext context);

		protected override sealed void OnDrawConfigurationGUI ()
		{
			OnDrawConfigurableInspectorGUI ();
		}




		public virtual void OnDrawConfigurableInspectorGUI ()
		{
			DrawDefaultConfigurableInspectorGUI ();
		}

		public void DrawDefaultConfigurableInspectorGUI ()
		{
			//            ConfigurableProcessPathModifier pm = (ConfigurableProcessPathModifier)context.PathModifier;
			DrawFunctionSelections (context);
			DrawGeneralConfigurationGUI (context);
			//            
			//            EditorGUI.BeginChangeCheck();
			//
			//            if (EditorGUI.EndChangeCheck())
			//            {
			//                Undo.RecordObject(context.Target, "GenerateComponents Configuration");
			//                context.TargetModified();
			//                EditorUtility.SetDirty(context.Target);
			//            }
		}

		static string[] ToStringArray<K> (K[] objects)
		{
			string[] strs = new string[objects.Length];
			for (int i = 0; i < objects.Length; i++) {
				strs [i] = objects [i].ToString ();
			}
			return strs;
		}
        
		PathModifierFunction FunctionSelection (PathModifierEditorContext context, string label, PathModifierFunction[] allowedFunctions, PathModifierFunction currentFunction, DrawFunctionOptionsFunc drawOptionsFunc)
		{
			string[] allowedValues = ToStringArray (allowedFunctions);
			int currentFunctionIndex = -1;
			for (int i = 0; i < allowedFunctions.Length; i++) {
				if (allowedFunctions [i] == currentFunction) {
					currentFunctionIndex = i;
					break;
				}
			}
			EditorGUI.BeginDisabledGroup (allowedValues.Length < 1);
			if (allowedValues.Length == 1) {
				//EditorGUILayout.LabelField(label, allowedValues [0], GUI.skin.textField);
				currentFunctionIndex = 0;
			}
			currentFunctionIndex = EditorGUILayout.Popup (label, currentFunctionIndex, allowedValues);
			EditorGUI.EndDisabledGroup ();

			if (currentFunctionIndex >= 0 && currentFunctionIndex < allowedFunctions.Length) {
				currentFunction = allowedFunctions [currentFunctionIndex];
			} else {
				currentFunction = default(PathModifierFunction);
			}
            
			if (null != drawOptionsFunc) {
				if (currentFunction == PathModifierFunction.Generate || currentFunction == PathModifierFunction.Process) {
					EditorGUI.indentLevel++;
					drawOptionsFunc (context);
					EditorGUI.indentLevel--;
				}
			}
            
			return currentFunction;
		}
		private int DrawCustomProcessFuncToolbar ()
		{
			ConfigurableProcessPathModifier pm = (ConfigurableProcessPathModifier)context.PathModifier;

			int selIndex = editorState.GetInt ("SelectedPMProcessFunctionIndex", 0);

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label (" ", GUILayout.Width (EditorGUI.indentLevel * 12f));
			selIndex = DrawCustomProcessFuncToolbarButton (0, selIndex, "Position", pm.AllowedPositionFunctions.Length > 0, false);
			selIndex = DrawCustomProcessFuncToolbarButton (1, selIndex, "Direction", pm.AllowedDirectionFunctions.Length > 0, false);
			selIndex = DrawCustomProcessFuncToolbarButton (2, selIndex, "UpV", pm.AllowedUpVectorFunctions.Length > 0, false);
			selIndex = DrawCustomProcessFuncToolbarButton (3, selIndex, "Angle", pm.AllowedAngleFunctions.Length > 0, false);
			selIndex = DrawCustomProcessFuncToolbarButton (4, selIndex, "Dist-1", pm.AllowedDistanceFromPreviousFunctions.Length > 0, false);
			selIndex = DrawCustomProcessFuncToolbarButton (5, selIndex, "Dist-0", pm.AllowedDistanceFromBeginFunctions.Length > 0, true);
			EditorGUILayout.EndHorizontal ();

			editorState.SetInt ("SelectedPMProcessFunctionIndex", selIndex);
			return selIndex;
		}
		private static int DrawCustomProcessFuncToolbarButton (int index, int currentSelectedIndex, string label, bool enabled, bool last)
		{
			EditorGUI.BeginDisabledGroup (!enabled);
			GUIStyle style;
			if (index == 0) {
				style = last ? EditorStyles.miniButton : EditorStyles.miniButtonLeft;
			} else if (last) {
				style = EditorStyles.miniButtonRight;
			} else {
				style = EditorStyles.miniButtonMid;
			}
			if (GUILayout.Toggle (index == currentSelectedIndex, label, style, GUILayout.ExpandWidth (true))) {
				currentSelectedIndex = index;
			}
			EditorGUI.EndDisabledGroup ();
			return currentSelectedIndex;
		}
		public void DrawFunctionSelections (PathModifierEditorContext context)
		{
			ConfigurableProcessPathModifier pm = (ConfigurableProcessPathModifier)context.PathModifier;
            

			int selFnIndex = DrawCustomProcessFuncToolbar ();

			EditorGUI.BeginChangeCheck ();
			switch (selFnIndex) {
			case 0:
				pm.PositionFunction = FunctionSelection (context, "Position Fn", pm.AllowedPositionFunctions, pm.PositionFunction, DrawPositionFunctionGUI);
				break;
			case 1:
				pm.DirectionFunction = FunctionSelection (context, "Direction Fn", pm.AllowedDirectionFunctions, pm.DirectionFunction, DrawDirectionFunctionGUI);
				break;
			case 2:
				pm.UpVectorFunction = FunctionSelection (context, "Up Vector Fn", pm.AllowedUpVectorFunctions, pm.UpVectorFunction, DrawUpVectorFunctionGUI);
				break;
			case 3:
				pm.AngleFunction = FunctionSelection (context, "Angle Fn", pm.AllowedAngleFunctions, pm.AngleFunction, DrawAngleFunctionGUI);
				break;
			case 4:
				pm.DistanceFromPreviousFunction = FunctionSelection (context, "Dist[prev] Fn", pm.AllowedDistanceFromPreviousFunctions, pm.DistanceFromPreviousFunction, DrawDistanceFunctionGUI);
				break;
			case 5:
				pm.DistanceFromBeginFunction = FunctionSelection (context, "Dist[begin] Fn", pm.AllowedDistanceFromBeginFunctions, pm.DistanceFromBeginFunction, DrawDistanceFunctionGUI);
				break;
			default:
				// NOP;
				break;
			}
//			
//			
//			
//			
			if (EditorGUI.EndChangeCheck ()) {
				//                Undo.RecordObject(context.Target, "GenerateComponents Configuration");
				context.TargetModified ();
				EditorUtility.SetDirty (context.Target);
			}
            
		}

		protected virtual void DrawPositionFunctionGUI (PathModifierEditorContext context)
		{
		}

		protected virtual void DrawDirectionFunctionGUI (PathModifierEditorContext context)
		{
		}

		protected virtual void DrawUpVectorFunctionGUI (PathModifierEditorContext context)
		{
		}

		protected virtual void DrawAngleFunctionGUI (PathModifierEditorContext context)
		{
		}

		protected virtual void DrawDistanceFunctionGUI (PathModifierEditorContext context)
		{
		}

		protected virtual void DrawGeneralConfigurationGUI (PathModifierEditorContext context)
		{

		}

	}
    
}
