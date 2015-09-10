using UnityEngine;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using Util;
using Util.Editor;
using Paths;
using Paths.Editor;

namespace Paths.MeshGenerator.Editor
{
	[CustomToolEditor(typeof(IMeshGenerator))]
	public class FallbackMeshGeneratorEditor : AbstractMeshGeneratorEditor<AbstractMeshGenerator>
	{
		//bool pathModifiersVisible = false;

		// TODO should we persist "prefs"?
//        Dictionary<string, string> prefs = new Dictionary<string, string>();



		public override void DrawInspectorGUI ()
		{
//            DictionaryCustomToolEditorPrefs editorPrefs = new DictionaryCustomToolEditorPrefs(prefs);

//            GUIContent[] tbContents = new GUIContent[] {
//                new GUIContent("General", "Track Parameters"),
//                new GUIContent("Path", "Path Parameters"),
//                new GUIContent("Settings", "Track Settings"),
//            };
			//GUILayout.Toolbar(0, tbContents);



		}

		public override void DrawSceneGUI ()
		{
			throw new NotImplementedException ();
		}
	}

}
