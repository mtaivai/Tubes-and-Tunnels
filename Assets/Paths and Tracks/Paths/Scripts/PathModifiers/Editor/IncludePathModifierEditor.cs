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
	[CustomToolEditor(typeof(IncludePathModifier))]
	public class IncludePathModifierEditor : AbstractPathModifierEditor
	{
		public override void DrawInspectorGUI (PathModifierEditorContext context)
		{
			IncludePathModifier pm = (IncludePathModifier)context.PathModifier;
            
			Path includedPath = pm.GetIncludedPath (context.PathModifierContainer.GetReferenceContainer ());
			EditorGUI.BeginChangeCheck ();
			Path newPath = (Path)EditorGUILayout.ObjectField ("Included Path", includedPath, typeof(Path), true);
			if (EditorGUI.EndChangeCheck ()) {
				// TODO Undo.RecordObject
				if (newPath == context.Path) {
					EditorUtility.DisplayDialog ("Recursive Include", "Path can't be included recursively to itself!", "Got it!");
				} else if (IsPathIncludedIn (context.Path, newPath)) {
					EditorUtility.DisplayDialog (
                        "Recursive Include", 
                        "Path '" + newPath.name + "' already includes '" + context.Path.name + "'", 
                        "Got it!");
				} else {
					pm.SetIncludedPath (context.PathModifierContainer.GetReferenceContainer (), newPath);
				}
				//EditorUtility.SetDirty(context.Target);
				context.TargetModified ();
				//              trackInspector.TrackGeneratorModified();
			}
            
			int inputPointCount = pm.GetCurrentInputPointCount ();
			int sliderPos = pm.includePosition;
			if (sliderPos < 0) {
				sliderPos = inputPointCount;
			}
			EditorGUI.BeginChangeCheck ();
			sliderPos = EditorGUILayout.IntSlider ("Position", sliderPos, 0, inputPointCount);
			if (EditorGUI.EndChangeCheck ()) {
				// TODO record UNDO!
				pm.includePosition = sliderPos;
				context.TargetModified ();
			}

			EditorGUI.BeginChangeCheck ();
			pm.removeDuplicates = EditorGUILayout.Toggle ("Smart Include", pm.removeDuplicates);
			if (EditorGUI.EndChangeCheck ()) {
				// TODO record UNDO!
				context.TargetModified ();
			}

			EditorGUI.BeginChangeCheck ();
			EditorGUI.BeginDisabledGroup (pm.includePosition == 0);
			pm.alignFirstPoint = EditorGUILayout.Toggle ("Align First Point", pm.alignFirstPoint);
			EditorGUI.EndDisabledGroup ();
			if (EditorGUI.EndChangeCheck ()) {
				// TODO record UNDO!
				context.TargetModified ();
			}

			if (pm.alignFirstPoint) {
				EditorGUI.BeginDisabledGroup (true);
				EditorGUILayout.Vector3Field ("Position Offset", pm.GetCurrentIncludedPathPosOffset ());
				EditorGUI.EndDisabledGroup ();
			} else {
				EditorGUI.BeginChangeCheck ();
				pm.includedPathPosOffset = EditorGUILayout.Vector3Field ("Position Offset", pm.includedPathPosOffset);
				if (EditorGUI.EndChangeCheck ()) {
					// TODO record UNDO!
					context.TargetModified ();
				}
			}

			;
		}

		static bool IsPathIncludedIn (Path path, Path containerPath)
		{
			if (containerPath == path) {
				return true;
			}
			IReferenceContainer refContainer = containerPath.GetPathModifierContainer ().GetReferenceContainer ();
			IPathModifier[] pathModifiers = containerPath.GetPathModifierContainer ().GetPathModifiers ();
			foreach (IPathModifier pm in pathModifiers) {
				if (!(pm is IncludePathModifier)) {
					continue;
				}
				IncludePathModifier ipm = (IncludePathModifier)pm;
				if (ipm.GetIncludedPath (refContainer) == path) {
					return true;
				} else {
					// Recursive lookup
					if (IsPathIncludedIn (path, ipm.GetIncludedPath (refContainer))) {
						return true;
					}
				}

			}
			return false;
		}
	}
}
