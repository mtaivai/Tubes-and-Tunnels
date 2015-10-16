// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using UnityEditor;

using System;
using System.Collections;
using System.Collections.Generic;

using Util;
using Util.Editor;
using Paths;
using Paths.Editor;

using Paths.MeshGenerator.SliceStrip.Editor;
using Paths.MeshGenerator.Editor;
using Paths.MeshGenerator.Extruder;
using Paths.MeshGenerator.Extruder.Model;

namespace Paths.MeshGenerator.Extruder.Editor
{

	[PluginEditor(typeof(ExtruderGenerator))]
	public class ExtruderGeneratorEditor : IMeshGeneratorEditor
	{

//		public override void DrawInspectorGUI ()
//		{
////			throw new NotImplementedException ();
//		}
		public void DrawInspectorGUI (PluginEditorContext context)
		{
			PathMeshGenerator target = (PathMeshGenerator)context.Target;
			ExtruderGenerator egen = (ExtruderGenerator)context.PluginInstance;

			EditorGUI.BeginChangeCheck ();
			egen.DieModel = (DieModel)EditorGUILayout.ObjectField ("Die Model", egen.DieModel, typeof(DieModel), true);
			if (EditorGUI.EndChangeCheck ()) {
				Undo.RecordObject (target, "Select Die Model");
				context.TargetModified ();
				EditorUtility.SetDirty (target);
			}
			if (GUILayout.Button ("Foo")) {
				DieModel model = egen.DieModel;
				int i = 0;
				foreach (List<int> edgesStrip in model.FindConnectedEdgeGraphs(true, true)) {
					Debug.LogFormat ("Strip {0}: {1}", i++, EdgeStripToString (edgesStrip));
					//model.DistributeUOnEdgeGraph (edgesStrip);
				}			//egen.DieModel.FindConnectedEdgeGraphs ();
			}

		}

		static string EdgeStripToString (List<int> edgeIndices)
		{
			string s = "";
			foreach (int ei in edgeIndices) {
				if (s.Length > 0) {
					s += ", ";
				}
				s += ei;
			}
			return s;
		}

		protected void DrawCustomSliceConfigurationGUI ()
		{
//			base.DrawCustomSliceConfigurationGUI ();

		}
		public void DrawSceneGUI (MeshGeneratorEditorContext context)
		{
			//throw new NotImplementedException ();
		}

	}
}
