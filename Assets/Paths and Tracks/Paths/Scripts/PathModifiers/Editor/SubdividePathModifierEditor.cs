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


    [CustomToolEditor(typeof(SubdividePathModifier))]
    public class SubdividePathModifierEditor : AbstractPathModifierEditor
    {

        public override void DrawInspectorGUI(PathModifierEditorContext context)
        {
            SubdividePathModifier pm = (SubdividePathModifier)context.PathModifier;

            EditorGUI.BeginChangeCheck();
            pm.SubdivideSegmentsMin = EditorGUILayout.IntSlider("Min Subdivisions", pm.SubdivideSegmentsMin, 0, 20);
            if (EditorGUI.EndChangeCheck())
            {
                if (pm.SubdivideSegmentsMin > pm.SubdivideSegmentsMax)
                {
                    pm.SubdivideSegmentsMax = pm.SubdivideSegmentsMin;
                }
                //EditorUtility.SetDirty(context.Target);
                context.TargetModified();
//              trackInspector.TrackGeneratorModified();
            }
            EditorGUI.BeginChangeCheck();
            pm.SubdivideSegmentsMax = EditorGUILayout.IntSlider("Max Subdivisions", pm.SubdivideSegmentsMax, 0, 20);
            if (EditorGUI.EndChangeCheck())
            {
                if (pm.SubdivideSegmentsMax < pm.SubdivideSegmentsMin)
                {
                    pm.SubdivideSegmentsMin = pm.SubdivideSegmentsMax;
                }
//              EditorUtility.SetDirty(context.Target);
                context.TargetModified();
//              trackInspector.TrackGeneratorModified();
            }
            EditorGUI.BeginChangeCheck();
            pm.SubdivideTreshold = EditorGUILayout.FloatField("Subdivision Target Length", pm.SubdivideTreshold);
            if (EditorGUI.EndChangeCheck())
            {
//              EditorUtility.SetDirty(context.Target);
                context.TargetModified();
//              trackInspector.TrackGeneratorModified();
            }
            
            //      Track track = trackInspector.target as Track;
            //      Path path = track.Path;

//          EditorGUI.BeginChangeCheck();
//      usePathResolution = EditorGUILayout.ToggleLeft("Use Path Resolution (" + path.GetResolution() + ")", usePathResolution);
//      if (EditorGUI.EndChangeCheck()) {
//          EditorUtility.SetDirty(trackInspector.target);
//          trackInspector.TrackGeneratorModified();
//      }
//      
//      EditorGUI.BeginDisabledGroup(usePathResolution);
//      EditorGUI.BeginChangeCheck();
//      customResolution = EditorGUILayout.IntSlider("Custom Resolution", customResolution, 1, 100);
//      if (EditorGUI.EndChangeCheck()) {
//          EditorUtility.SetDirty(trackInspector.target);
//          trackInspector.TrackGeneratorModified();
//      }
//      EditorGUI.EndDisabledGroup();
        }
    
    }

}
