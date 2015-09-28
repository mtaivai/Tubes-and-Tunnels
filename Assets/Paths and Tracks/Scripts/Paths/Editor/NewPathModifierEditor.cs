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
	[PluginEditor(typeof(NewPathModifier))]
	internal class NewPathModifierEditor : AbstractPathModifierEditor<NewPathModifier>
	{


		protected override void OnDrawConfigurationGUI ()
		{

		}
		protected override void DrawTypeField ()
		{

			//PluginResolver toolResolver = PathModifierResolver.Instance;
			NewPathModifier pm = (NewPathModifier)context.PathModifier;
			IPathModifierContainer pmContainer = context.PathModifierContainer;
//          Path path = pmeContext.Path;
            
			// Show type selection


			Type[] pmTypes = PathModifierResolver.Instance.FindPluginTypes ();
			List<string> displayNameList = new List<string> ();
			for (int j = 0; j < pmTypes.Length; j++) {
				displayNameList.Add (PathModifierResolver.Instance.GetPluginDisplayName (pmTypes [j]));
			}

			string[] displayNames = displayNameList.ToArray ();

			// sort pmTypes and displayNames
			Array.Sort (displayNames, pmTypes);


			displayNameList.Insert (0, "<Please Select Type>");
			displayNames = displayNameList.ToArray ();

			EditorGUILayout.BeginHorizontal ();
			EditorGUI.BeginChangeCheck ();
			pm.pathModifierIndex = EditorGUILayout.Popup ("Type", pm.pathModifierIndex + 1, displayNames) - 1;
			if (EditorGUI.EndChangeCheck ()) {
				// Create instance already:
				if (pm.pathModifierIndex >= 0) {
					pm.pathModifier = (IPathModifier)Activator.CreateInstance (pmTypes [pm.pathModifierIndex]);
				} else {
					pm.pathModifier = null;
				}
			}

			EditorGUI.BeginDisabledGroup (pm.pathModifier == null || pm.pathModifierIndex < 0);
			if (GUILayout.Button ("Add", EditorStyles.miniButton, GUILayout.ExpandWidth (false)) && pm.pathModifier != null && pm.pathModifierIndex >= 0) {
				// TODO should we use "pmContainer" as target in here
				Undo.RecordObject (context.Target, "Add Path Modifier");

				int thisIndex = pmContainer.IndexOf (pm);
				pmContainer.RemovePathModifier (thisIndex);

				if (!StringUtil.IsEmpty (pm.GetInstanceName ()) && pm.GetName () != pm.GetInstanceName ()) {
					pm.pathModifier.SetInstanceName (pm.GetInstanceName ());
				}
				if (!StringUtil.IsEmpty (pm.GetInstanceDescription ())) {
					pm.pathModifier.SetInstanceDescription (pm.GetInstanceDescription ());
				}
				pmContainer.InsertPathModifier (thisIndex, pm.pathModifier);
				pm.pathModifier = null;

                
				if (pmContainer is UnityEngine.Object) {
					EditorUtility.SetDirty ((UnityEngine.Object)pmContainer);
				}
				context.TargetModified ();
			}
			EditorGUILayout.EndHorizontal ();

			EditorGUI.EndDisabledGroup ();
		}
	}

}
