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
	[PluginEditor(typeof(DeformPathModifier))]
	public class DeformPathModifierEditor : ConfigurableProcessPathModifierEditor<DeformPathModifier>
	{

		protected override void DrawPositionFunctionGUI (PathModifierEditorContext context)
		{

			EditorGUI.BeginChangeCheck ();
			target.linearDisplaceEnabled = EditorGUILayout.Toggle ("Linear Displacement", target.linearDisplaceEnabled);
			if (EditorGUI.EndChangeCheck ()) {
				context.TargetModified ();
			}

			EditorGUI.indentLevel++;
			EditorGUI.BeginDisabledGroup (!target.linearDisplaceEnabled);
			EditorGUI.BeginChangeCheck ();
			target.linearDisplaceStart = EditorGUILayout.Vector3Field ("Linear Disp Begin", target.linearDisplaceStart);
			if (EditorGUI.EndChangeCheck ()) {
				context.TargetModified ();
			}

			EditorGUI.BeginChangeCheck ();
			target.linearDisplaceEnd = EditorGUILayout.Vector3Field ("Linear Disp End", target.linearDisplaceEnd);
			if (EditorGUI.EndChangeCheck ()) {
				context.TargetModified ();
			}
			EditorGUI.EndDisabledGroup ();
			EditorGUI.indentLevel--;

			DrawDisplacementCurvesGUI ();
		}

		protected override void DrawDirectionFunctionGUI (PathModifierEditorContext context)
		{
			base.DrawDirectionFunctionGUI (context);
		}

		protected override void DrawUpVectorFunctionGUI (PathModifierEditorContext context)
		{
			base.DrawUpVectorFunctionGUI (context);
		}

		protected override void DrawAngleFunctionGUI (PathModifierEditorContext context)
		{
			base.DrawAngleFunctionGUI (context);
		}

		protected override void DrawDistanceFunctionGUI (PathModifierEditorContext context)
		{
			base.DrawDistanceFunctionGUI (context);
		}
		protected override void DrawGeneralConfigurationGUI (PathModifierEditorContext c)
		{
		}

		void DrawDisplacementCurvesGUI ()
		{
			//DrawDefaultConfigurationGUI ("displacementCurves", "displacementCurveLimits");
			
			
			// Displacement curves
			bool showCurves = editorState.GetBool ("DisplacementCurvesVisible", false);
			showCurves = EditorGUILayout.Foldout (showCurves, "Displacement Curves");
			editorState.SetBool ("DisplacementCurvesVisible", showCurves);
			
			if (showCurves) {
				
				EditorGUI.indentLevel++;
				for (int i = 0; i < 3; i++) {
					DrawDisplacementCurveEditor (i);
				}
				EditorGUI.indentLevel--;
			}
		}
		void DrawDisplacementCurveEditor (int axis)
		{
			if (axis < 0 || axis > 2) {
				throw new ArgumentOutOfRangeException ("axis", axis, "axis != (0..2)");
			}


//
//			if (dcurve.limits.from > dcurve.limits.to) {
//				float swap = dcurve.limits.from;
//				dcurve.limits.from = dcurve.limits.to;
//				dcurve.limits.to = swap;
//				context.TargetModified ();
//			}

			string curveLabel;
			Color curveColor;
			switch (axis) {
			case 0:
				curveLabel = "X";
				curveColor = Color.red;
				break;
			case 1:
				curveLabel = "Y";
				curveColor = Color.green;
				break;
			case 2:
				curveLabel = "Z";
				curveColor = Color.blue;
				break;
			default:
				// HUH!
				curveLabel = "Curve";
				curveColor = Color.white;
				break;
			}

			bool showCurves = editorState.GetBool ("DisplacementCurvesVisible" + axis, false);
			showCurves = EditorGUILayout.Foldout (showCurves, curveLabel + " Displacement Curve");
			editorState.SetBool ("DisplacementCurvesVisible" + axis, showCurves);

			if (showCurves) {
				AnimationCurve ac = new AnimationCurve ();
				DeformPathModifier.DisplacementCurve dcurve = target.displacementCurves [axis];
				
				foreach (Keyframe kf in dcurve.keyframes) {
					ac.AddKey (kf);
				}

				EditorGUI.BeginChangeCheck ();
				dcurve.enabled = EditorGUILayout.Toggle ("Enabled", dcurve.enabled);
				if (EditorGUI.EndChangeCheck ()) {
					context.TargetModified ();
				}


				//			EditorGUILayout.BeginHorizontal ();
				Rect editRange = new Rect (
					dcurve.editorTRange.from, 
					dcurve.editorValueRange.from, 
					dcurve.editorTRange.to - dcurve.editorTRange.from, 
					dcurve.editorValueRange.to - dcurve.editorValueRange.from);

				EditorGUI.BeginChangeCheck ();
				ac = EditorGUILayout.CurveField ("Curve", ac, curveColor, editRange);
				//ac = EditorGUILayout.CurveField (curveLabel, ac);
				if (EditorGUI.EndChangeCheck ()) {
					// Reflect back
					dcurve.keyframes = new List<Keyframe> (ac.keys);
					context.TargetModified ();
				}
				
				//			if (target.displacementCurves [axis].Count == 0) {
				//				if (GUILayout.Button (
				//					new GUIContent ("+", "Create new curve"), EditorStyles.miniButton, GUILayout.Width (20f))) {
				//					ac = AnimationCurve.Linear (0f, 0f, 1f, 0f);
				//					target.displacementCurves [axis] = new List<Keyframe> (ac.keys);
				//					context.TargetModified ();
				//				}
				//			} else {
				//				if (GUILayout.Button (
				//					new GUIContent ("-", "Delete the curve"), EditorStyles.miniButton, GUILayout.Width (20f))) {
				//					target.displacementCurves [axis] = new List<Keyframe> (0);
				//					context.TargetModified ();
				//				}
				//			}
				//			EditorGUILayout.EndHorizontal ();


				RangeField ("Editor Value Range", dcurve.editorValueRange, ref dcurve.keepEditorValueRangeSymmetricalAroundZero);

				RangeField ("Editor T Range", dcurve.editorTRange);
			}

		}

		private void RangeField (string label, Range range)
		{
			bool fooSym = false;
			RangeField (label, range, ref fooSym, false);
		}

		private void RangeField (string label, Range range, ref bool keepSymAroundZero, bool showSymAroundZeroOption = true)
		{
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.PrefixLabel (label);
			float prevLabelWidth = EditorGUIUtility.labelWidth;
			int prevIndent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			EditorGUIUtility.labelWidth = 40f;
			
			// Lower value limit
			EditorGUI.BeginChangeCheck ();
			range.from = EditorGUILayout.FloatField ("From", range.from, GUILayout.ExpandWidth (false));
			if (EditorGUI.EndChangeCheck ()) {
				if (keepSymAroundZero) {
					range.from = Mathf.Min (0f, range.from);
					range.to = -range.from;
				} 
				context.TargetModified ();
			}
			
			// Upper value limit
			EditorGUI.BeginChangeCheck ();
			range.to = EditorGUILayout.FloatField ("To", range.to, GUILayout.ExpandWidth (false));
			if (EditorGUI.EndChangeCheck ()) {
				if (keepSymAroundZero) {
					range.to = Mathf.Max (0, range.to);
					range.from = -range.to;
				}
				context.TargetModified ();
			}

			if (showSymAroundZeroOption) {
				EditorGUI.BeginChangeCheck ();
				keepSymAroundZero = EditorGUILayout.Toggle ("Sym0", keepSymAroundZero);
				if (EditorGUI.EndChangeCheck ()) {
					if (keepSymAroundZero) {
						float dist = Mathf.Abs (range.to - range.from);
						range.from = -dist;
						range.to = dist;
					}
					context.TargetModified ();
				}
			}
			EditorGUIUtility.labelWidth = prevLabelWidth;
			EditorGUI.indentLevel = prevIndent;
			
			EditorGUILayout.EndHorizontal ();
		}

	}

}
