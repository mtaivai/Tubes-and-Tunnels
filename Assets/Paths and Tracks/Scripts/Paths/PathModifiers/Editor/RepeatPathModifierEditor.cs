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
	[PluginEditor(typeof(RepeatPathModifier))]
	public class RepeatPathModifierEditor : AbstractPathModifierEditor
	{
        
		protected override void OnDrawConfigurationGUI ()
		{
			RepeatPathModifier pm = (RepeatPathModifier)context.PathModifier;
            
			EditorGUI.BeginChangeCheck ();
			pm.repeatCount = EditorGUILayout.IntSlider ("Repeat Count", pm.repeatCount, 1, 100);
			if (EditorGUI.EndChangeCheck ()) {

				//EditorUtility.SetDirty(context.Target);
				context.TargetModified ();
				//              trackInspector.TrackGeneratorModified();
			}

		}
        
	}

}
