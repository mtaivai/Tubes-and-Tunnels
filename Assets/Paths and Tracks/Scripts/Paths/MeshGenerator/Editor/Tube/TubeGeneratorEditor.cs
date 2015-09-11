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
