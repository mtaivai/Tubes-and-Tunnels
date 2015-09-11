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
//			EditorGUI.BeginChangeCheck ();
			target.Width = EditorGUILayout.FloatField ("Slice Width", target.Width);
//			if (EditorGUI.EndChangeCheck ()) {
//				//EditorUtility.SetDirty(trackInspector.target);
//				editorContext.TargetModified ();
//			}
		}
		
		public override void DrawSceneGUI ()
		{
			//throw new NotImplementedException ();
		}
	}

}
