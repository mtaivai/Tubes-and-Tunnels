// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using Util;
using Util.Editor;
using Paths;
using Paths.Editor;

using Paths.MeshGenerator.SliceStrip.Editor;
using Paths.MeshGenerator.Editor;
using Paths.MeshGenerator.Tube;

namespace Paths.MeshGenerator.Tube.Editor
{


	[PluginEditor(typeof(TubeGenerator))]
	public class TubeGeneratorEditor : AbstractSliceStripGeneratorEditor<TubeGenerator>
	{

		protected override void DrawCustomSliceConfigurationGUI ()
		{
			EditorGUI.BeginChangeCheck ();
			target.SliceEdges = EditorGUILayout.IntSlider ("Slice Edges", target.SliceEdges, 3, 64);
			if (EditorGUI.EndChangeCheck ()) {
				//EditorUtility.SetDirty(trackInspector.target);
				editorContext.TargetModified ();
			}



//			EditorGUI.BeginChangeCheck ();
//			target.StartAngle = EditorGUILayout.Slider ("Start Angle", target.StartAngle, -360f, 360f);
			if (PathEditorUtil.DynParamField ("Start Angle", target.StartAngle, -360f, 360f)) {
				editorContext.TargetModified ();
			}
//			if (EditorGUI.EndChangeCheck ()) {
//				editorContext.TargetModified ();
//			}

//			EditorGUI.BeginChangeCheck ();
//			target.ArcLength = EditorGUILayout.Slider ("Arc Length", target.ArcLength, 0f, 360f);
//			if (EditorGUI.EndChangeCheck ()) {
//				editorContext.TargetModified ();
//			}
			if (PathEditorUtil.DynParamField ("Arc Length", target.ArcLength, 0f, 360f)) {
				editorContext.TargetModified ();
			}



//			EditorGUI.BeginChangeCheck ();
//			float startAngle = target.StartAngle;
//			float endAngle = startAngle + target.ArcLength;
//			EditorGUILayout.MinMaxSlider (ref startAngle, ref endAngle, -360f, 360f + target.ArcLength);
//			if (EditorGUI.EndChangeCheck ()) {
//				target.StartAngle = Mathf.Round (startAngle);
//				target.ArcLength = Mathf.Round (endAngle - startAngle);
//				//target.EndAngle = endAngle;
//				editorContext.TargetModified ();
//			}

			EditorGUI.BeginChangeCheck ();
			target.SliceSize = EditorGUILayout.Vector2Field ("Slice Size", target.SliceSize);
			if (EditorGUI.EndChangeCheck ()) {
				//EditorUtility.SetDirty(trackInspector.target);
				editorContext.TargetModified ();
			}
		}
		
		public override void DrawSceneGUI ()
		{
			//throw new NotImplementedException ();
		}
	}

}
