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
        public override void DrawInspectorGUI(PathModifierEditorContext context)
        {
            IncludePathModifier pm = (IncludePathModifier)context.PathModifier;
            
            EditorGUI.BeginChangeCheck();
            Path includedPath = pm.GetIncludedPath(context.PathModifierContainer.GetReferenceContainer());

//            pm.Get
            Path newPath = (Path)EditorGUILayout.ObjectField("Included Path", includedPath, typeof(Path), true);
            if (EditorGUI.EndChangeCheck())
            {
                if (newPath == context.Path)
                {
                    EditorUtility.DisplayDialog("Recursive Include", "Path can't be included recursively to itself!", "Got it!");
                } else if (IsPathIncludedIn(context.Path, newPath))
                {
                    EditorUtility.DisplayDialog(
                        "Recursive Include", 
                        "Path '" + newPath.name + "' already includes '" + context.Path.name + "'", 
                        "Got it!");
                } else
                {
                    pm.SetIncludedPath(context.PathModifierContainer.GetReferenceContainer(), newPath);
                }
                //EditorUtility.SetDirty(context.Target);
                context.TargetModified();
                //              trackInspector.TrackGeneratorModified();
            }
            
        }

        static bool IsPathIncludedIn(Path path, Path containerPath)
        {
            if (containerPath == path)
            {
                return true;
            }
            IReferenceContainer refContainer = containerPath.GetPathModifierContainer().GetReferenceContainer();
            IPathModifier[] pathModifiers = containerPath.GetPathModifierContainer().GetPathModifiers();
            foreach (IPathModifier pm in pathModifiers)
            {
                if (!(pm is IncludePathModifier))
                {
                    continue;
                }
                IncludePathModifier ipm = (IncludePathModifier)pm;
                if (ipm.GetIncludedPath(refContainer) == path)
                {
                    return true;
                } else
                {
                    // Recursive lookup
                    if (IsPathIncludedIn(path, ipm.GetIncludedPath(refContainer)))
                    {
                        return true;
                    }
                }

            }
            return false;
        }
    }
}
