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
	[PluginEditor(typeof(GenerateComponentsPathModifier))]
	public class GenerateComponentsPathModifierEditor : ConfigurableProcessPathModifierEditor
	{
		protected override void DrawAngleFunctionGUI (PathModifierEditorContext context)
		{
//            generateAnglePlane
			GenerateComponentsPathModifier pm = (GenerateComponentsPathModifier)context.PathModifier;
			if (pm.AngleFunction == PathModifierFunction.Generate) {
				pm.generateAnglePlane = (CoordinatePlane)EditorGUILayout.EnumPopup ("Coordinate Plane", pm.generateAnglePlane);

			}
		}

		protected override void DrawUpVectorFunctionGUI (PathModifierEditorContext context)
		{
//            TypedCustomToolEditorPrefs editorPrefs = new TypedCustomToolEditorPrefs(context.CustomToolEditorPrefs);
			GenerateComponentsPathModifier pm = (GenerateComponentsPathModifier)context.PathModifier;

			if (pm.UpVectorFunction == PathModifierFunction.Generate) {
				pm.generateUpVectorPlane = (CoordinatePlane)EditorGUILayout.EnumPopup ("Path Plane", pm.generateUpVectorPlane);

				pm.upVectorAlgorithm = (GenerateComponentsPathModifier.UpVectorAlgorithm)EditorGUILayout.EnumPopup ("Up Vector Algorithm", pm.upVectorAlgorithm);
				EditorGUI.indentLevel++;
				if (pm.upVectorAlgorithm == GenerateComponentsPathModifier.UpVectorAlgorithm.Bank) {
					pm.bankFactor = EditorGUILayout.FloatField ("Banking Factor", pm.bankFactor);
					pm.maxBankAngle = EditorGUILayout.Slider ("Max Bank Angle", pm.maxBankAngle, 0f, 360.0f);
					pm.bankSmoothingFactor = EditorGUILayout.Slider ("Bank Smoothing Factor", pm.bankSmoothingFactor, 0.0f, 1.0f);

				} else if (pm.upVectorAlgorithm == GenerateComponentsPathModifier.UpVectorAlgorithm.Constant) {
					pm.constantUpVector = EditorGUILayout.Vector3Field ("Up Vector", pm.constantUpVector);
				}
				EditorGUI.indentLevel--;
			}

		}


	}
}
