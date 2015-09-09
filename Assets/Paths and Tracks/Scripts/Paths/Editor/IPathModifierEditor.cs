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
		private IPathData pathData;
		private PathModifierContext pathModifierContext;



		public PathModifierEditorContext (IPathData data, PathModifierContext pmContext, IPathModifier customTool, Path target, UnityEditor.Editor e, TargetModifiedFunc targetModifiedFunc, TypedCustomToolEditorPrefs prefs)
            : base(customTool, target, e, targetModifiedFunc, prefs)
		{
			this.pathData = data;
			this.pathModifierContext = pmContext;
			if (null == pmContext && null != customTool) {
				pmContext =
					new PathModifierContext (data.GetPathInfo (), data.GetPathModifierContainer (), data.GetOutputFlagsBeforeModifiers ());
			}
		}
		public PathModifierEditorContext (IPathData data, Path target, UnityEditor.Editor e, TargetModifiedFunc targetModifiedFunc, TypedCustomToolEditorPrefs prefs)
			: this(data, null, null, target, e, targetModifiedFunc, prefs)
		{
		}

		public IPathModifierContainer PathModifierContainer {
			get {
				return pathData.GetPathModifierContainer ();
			}
		}

		public IPathModifier PathModifier {
			get {
				return (IPathModifier)CustomTool;
			}
		}
		public IPathData PathData {
			get {
				return pathData;
			}
		}
		public Path Path {
			get {
				return (Path)Target;
			}
		}
		public PathModifierContext PathModifierContext {
			get {
				return pathModifierContext;
			}
		}
		public TypedCustomToolEditorPrefs EditorPrefs {
			get {
				return (TypedCustomToolEditorPrefs)CustomToolEditorPrefs;
			}
		}

	}

	public interface IPathModifierEditor : ICustomToolEditor
	{
	}



}