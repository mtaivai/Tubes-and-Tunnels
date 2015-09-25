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
using Paths.MeshGenerator.FlatQuad;

namespace Paths.MeshGenerator.FlatQuad.Editor
{

	[PluginEditor(typeof(FlatQuadStripeGenerator))]
	public class FlatQuadStripeGeneratorEditor : AbstractSliceStripGeneratorEditor<FlatQuadStripeGenerator>
	{
		
		protected override void DrawCustomSliceConfigurationGUI ()
		{

			if (PathEditorUtil.DynParamField ("Width", target.Width)) {
				Debug.Log ("Changed: " + target.Width);
				editorContext.TargetModified ();
			}

//			target.Width = EditorGUILayout.FloatField ("Slice Width", target.Width);
//			EditorGUILayout.BeginHorizontal ();
//			EditorGUILayout.EnumPopup ("Width", target.Width.ValueSource, GUILayout.ExpandWidth (false));
//			switch (target.Width.ValueSource) {
//			case DynParamSource.Constant:
//				EditorGUILayout.FloatField (target.Width.Value);
//				break;
//			case DynParamSource.WeightParam:
//				EditorGUILayout.TextField ("");
//				break;
//			}
//			EditorGUILayout.EndHorizontal ();
		}
		
		public override void DrawSceneGUI ()
		{
			//throw new NotImplementedException ();
		}
	}

}
