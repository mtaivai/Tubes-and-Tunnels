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

using Paths.MeshGenerator.SliceStrip;
using Paths.MeshGenerator.Editor;

namespace Paths.MeshGenerator.SliceStrip.Editor
{

	public abstract class AbstractSliceStripGeneratorEditor<T> : AbstractMeshGeneratorEditor<T> where T : AbstractSliceStripGenerator
	{
		public override sealed void DrawInspectorGUI ()
		{
			DrawSliceConfigurationGUI ();

			DrawMeshConfigurationGUI ();

		}

		protected void DrawSliceConfigurationGUI ()
		{
			DrawDefaultSliceConfigurationGUI ();
		}
		protected void DrawDefaultSliceConfigurationGUI ()
		{
			EditorGUI.BeginChangeCheck ();
			bool sliceConfigVisible = EditorGUILayout.Foldout (editorState.GetBool ("SliceConfigExpanded", true), "Slice Configuration");
			if (EditorGUI.EndChangeCheck ()) {
				editorState.SetBool ("SliceConfigExpanded", sliceConfigVisible);
			}
			if (sliceConfigVisible) {
				EditorGUI.indentLevel++;
				

				
//				EditorGUI.BeginChangeCheck ();
//				target.SliceRotation = EditorGUILayout.Slider ("Slice Rotation", target.SliceRotation, 0f, 360f);
//				if (EditorGUI.EndChangeCheck ()) {
//					//EditorUtility.SetDirty(trackInspector.target);
//					editorContext.TargetModified ();
//				}

				DrawCustomSliceConfigurationGUI ();
				
				EditorGUI.indentLevel--;
			}
		}
		protected virtual void DrawCustomSliceConfigurationGUI ()
		{

		}

		protected void DrawMeshConfigurationGUI ()
		{
			DrawDefaultMeshConfigurationGUI ();
		}
		protected void DrawDefaultMeshConfigurationGUI ()
		{
			EditorGUI.BeginChangeCheck ();

			bool meshConfigVisible = EditorGUILayout.Foldout (editorState.GetBool ("MeshConfigExpanded", true), "Mesh Configuration");
			if (EditorGUI.EndChangeCheck ()) {
				editorState.SetBool ("MeshConfigExpanded", meshConfigVisible);
			}
			if (meshConfigVisible) {
				EditorGUI.indentLevel++;

				EditorGUI.BeginChangeCheck ();
				target.SplitMeshCount = EditorGUILayout.IntSlider ("Split Mesh Count", target.SplitMeshCount, 1, 32);
				if (EditorGUI.EndChangeCheck ()) {
					editorContext.TargetModified ();
				}
				
				EditorGUI.BeginChangeCheck ();
				target.FacesDir = (MeshFaceDir)EditorGUILayout.EnumPopup ("Faces Direction", target.FacesDir);
				if (EditorGUI.EndChangeCheck ()) {
					editorContext.TargetModified ();
				}
				
				EditorGUI.BeginDisabledGroup (target.FacesDir != MeshFaceDir.Both);
				EditorGUI.BeginChangeCheck ();
				target.PerSideVertices = EditorGUILayout.Toggle ("Vertex per side", target.PerSideVertices);
				if (EditorGUI.EndChangeCheck ()) {
					editorContext.TargetModified ();
				}
				EditorGUI.BeginChangeCheck ();
				target.PerSideSubmeshes = EditorGUILayout.Toggle ("Submesh per side", target.PerSideSubmeshes);
				if (EditorGUI.EndChangeCheck ()) {
					editorContext.TargetModified ();
				}
				EditorGUI.EndDisabledGroup ();
				
				EditorGUI.indentLevel--;
			}
		}
	}

}
