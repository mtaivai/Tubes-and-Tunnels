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
    [CustomToolEditor(typeof(RepeatPathModifier))]
    public class RepeatPathModifierEditor : AbstractPathModifierEditor
    {
        
        public override void DrawInspectorGUI(PathModifierEditorContext context)
        {
            RepeatPathModifier pm = (RepeatPathModifier)context.PathModifier;
            
            EditorGUI.BeginChangeCheck();
            pm.repeatCount = EditorGUILayout.IntSlider("Repeat Count", pm.repeatCount, 1, 100);
            if (EditorGUI.EndChangeCheck())
            {

                //EditorUtility.SetDirty(context.Target);
                context.TargetModified();
                //              trackInspector.TrackGeneratorModified();
            }

        }
        
    }

}
