// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using UnityEditor;

using System.Collections;
using System.Collections.Generic;
using System;

using Paths;
using Paths.Editor;
using Util.Editor;

namespace Paths.Editor
{
	public static class WeightEditorUtil
	{
		private static readonly string[] EmptyStringArray = new string[0];

		public static string[] GetDefinedWeightIds (IPathData pathData)
		{

			if (pathData.IsPathMetadataSupported ()) {
				IPathMetadata md = pathData.GetPathMetadata ();
				int count = md.GetWeightDefinitionCount ();
				List<string> ids = new List<string> ();
				for (int i = 0; i < count; i++) {
					WeightDefinition wd = md.GetWeightDefinitionAtIndex (i);
					ids.Add (wd.WeightId);
				}
				return ids.ToArray ();
			} else {
				return EmptyStringArray;
			}
		}

		public static List<string> GetAvailableWeightIds (IPathData pathData, IEnumerable excludedWeightIds)
		{
			// Get list of available weights; don't include those already used!
			List<string> availableWeightIds = new List<string> (GetDefinedWeightIds (pathData));
			foreach (string excludedId in excludedWeightIds) {
				availableWeightIds.Remove (excludedId);
			}
			return availableWeightIds;
		}

		public static WeightDefinition GetWeightDefinition (IPathData pathData, string weightId)
		{
			WeightDefinition wd = new WeightDefinition (weightId);

			if (pathData.IsPathMetadataSupported ()) {
				IPathMetadata md = pathData.GetPathMetadata ();
				if (md.ContainsWeightDefinition (weightId)) {
					WeightDefinition wd2 = md.GetWeightDefinition (weightId);
					wd = wd.WithDefaultValue (wd2.DefaultValue)
						.WithHasDefaultValue (wd2.HasDefaultValue)
						.WithMinValue (wd2.MinValue)
						.WithMaxValue (wd2.MaxValue);
				}
			}
			return wd;
		}

//		public static float? GetDefaultWeightValue (IPathData pathData, string weightId)
//		{
//			WeightDefinition wd = GetWeightDefinition (pathData, weightId);
//			return wd.HasDefaultValue ? (float)wd.DefaultValue : (float?)null;
//		}

		public static void AddWeightToControlPoint (IPathData pathData, int pointIndex, string weightId)
		{
			float initialValue = 0f;
			if (pathData.IsPathMetadataSupported ()) {
				IPathMetadata md = pathData.GetPathMetadata ();
				if (md.ContainsWeightDefinition (weightId)) {
					WeightDefinition wd = md.GetWeightDefinition (weightId);
					if (wd.HasDefaultValue) {
						initialValue = wd.DefaultValue;
					}
				}
			}
			SetControlPointWeight (pathData, pointIndex, weightId, initialValue);
		}

		public static void SetControlPointWeight (IPathData pathData, int pointIndex, string weightId, float value)
		{
			PathPoint pp = pathData.GetControlPointAtIndex (pointIndex);
			// Check limits
			if (pathData.IsPathMetadataSupported ()) {
				IPathMetadata md = pathData.GetPathMetadata ();
				if (md.ContainsWeightDefinition (weightId)) {
					WeightDefinition wd = md.GetWeightDefinition (weightId);
					value = Mathf.Clamp (value, wd.MinValue, wd.MaxValue);
				}
			}
			pp.SetWeight (weightId, value);
			pathData.SetControlPointAtIndex (pointIndex, pp);
		}



//		public static void DisplayAddWeightMenu (Rect rect, IPathData pathData, int pointIndex, UnityEngine.Object target)
//		{
//			//			Rect rect = EditorGUILayout.BeginHorizontal ();
//			//			if (GUILayout.Button (new GUIContent ("+", "Add new weight"))) {
//			string[] weightIds = GetDefinedWeightIds (pathData);
//			GUIContent[] weightOptions = new GUIContent[weightIds.Length];
//			for (int i = 0; i < weightIds.Length; i++) {
//				weightOptions [i] = new GUIContent (weightIds [i]);
//			}
//			AddWeightCallbackData awd = new AddWeightCallbackData ();
//			awd.target = target;
//			awd.pathData = pathData;
//			awd.pointIndex = pointIndex;
//			EditorUtility.DisplayCustomMenu (rect, weightOptions, -1, AddWeightCallback, awd);
//			//			}
//			//			EditorGUILayout.EndHorizontal ();
//		}
//
//		private class AddWeightCallbackData
//		{
//			public UnityEngine.Object target;
//			public IPathData pathData;
//			public int pointIndex;
//			
//		}
//		private static void AddWeightCallback (object userData, string[] options, int selected)
//		{
//			AddWeightCallbackData awd = (AddWeightCallbackData)userData;
//			IPathData pathData = awd.pathData;
//			PathPoint pp = pathData.GetControlPointAtIndex (awd.pointIndex);
//			
//			string weightId = options [selected];
//			float initialValue = 0f;
//			if (pathData.IsPathMetadataSupported ()) {
//				IPathMetadata md = pathData.GetPathMetadata ();
//				if (md.ContainsWeightDefinition (weightId)) {
//					WeightDefinition wd = md.GetWeightDefinition (weightId);
//					if (wd.HasDefaultValue) {
//						initialValue = wd.DefaultValue;
//					}
//				}
//			}
//			pp.SetWeight (weightId, initialValue);
//			EditorUtility.SetDirty (awd.target);
//		}



		public static float WeightEditField (WeightDefinition wd, float value, string customLabel = null)
		{
			string weightId = wd.WeightId;
			string label = (null != customLabel) ? customLabel : weightId;
			if (wd.ValueRangeDefined) {
				// Slider
				value = EditorGUILayout.Slider (label, value, wd.MinValue, wd.MaxValue, GUILayout.ExpandWidth (true));
			} else {
				// Float field
				value = EditorGUILayout.FloatField (label, value, GUILayout.ExpandWidth (true));
			}
			return value;
		}


	}
    
}
