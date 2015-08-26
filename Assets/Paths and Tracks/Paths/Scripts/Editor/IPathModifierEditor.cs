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



    /// <summary>
    /// Marks the target type as an IPathModifierEditor implementation for
    /// the specified target (IPathModifer) type.
    /// </summary>
    public class PathModifierEditorContext : CustomToolEditorContext
    {
        private IPathModifierContainer pathModifierContainer;

        public PathModifierEditorContext(IPathModifierContainer container, IPathModifier customTool, Path target, UnityEditor.Editor e, TargetModifiedFunc targetModifiedFunc)
            : base(customTool, target, e, targetModifiedFunc)
        {
            this.pathModifierContainer = container;
        }

        public PathModifierEditorContext(IPathModifierContainer container, IPathModifier customTool, Path target, UnityEditor.Editor e, TargetModifiedFunc targetModifiedFunc, CustomToolEditorPrefs prefs)
            : base(customTool, target, e, targetModifiedFunc, prefs)
        {
            this.pathModifierContainer = container;
        }

        public IPathModifierContainer PathModifierContainer
        {
            get
            {
                return pathModifierContainer;
            }
        }

        public IPathModifier PathModifier
        {
            get
            {
                return (IPathModifier)CustomTool;
            }
        }
        
        public Path Path
        {
            get
            {
                return (Path)Target;
            }
        }


    }

    public interface IPathModifierEditor : ICustomToolEditor
    {
    }

    public abstract class AbstractPathModifierEditor : IPathModifierEditor
    {
        public void DrawInspectorGUI(CustomToolEditorContext context)
        {
            DrawInspectorGUI((PathModifierEditorContext)context);
        }

        public abstract void DrawInspectorGUI(PathModifierEditorContext context);
    }




}
