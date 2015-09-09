using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Util.Editor
{


	[AttributeUsage(AttributeTargets.Class)]
	public class CustomToolEditor : System.Attribute
	{
        
		public Type inspectedType;

		public CustomToolEditor (Type inspectedType)
		{
			this.inspectedType = inspectedType;
		}

	}

	public interface ICustomToolEditor
	{
		void DrawInspectorGUI (CustomToolEditorContext context);
	}

	public interface ICUstomToolEditorHost
	{
		void SetEditorFor (object customToolInstance, ICustomToolEditor editor);
		ICustomToolEditor GetEditorFor (object customToolInstance);

	}
    
	public class CustomToolEditorContext
	{
		public delegate void TargetModifiedFunc ();
        
		//      private CustomToolResolver toolResolver;

		private object customTool;
		private UnityEditor.Editor editorHost;
		private UnityEngine.Object target;
		private TargetModifiedFunc targetModifiedFunc;
		private CustomToolEditorPrefs editorPrefs;

		public CustomToolEditorContext (object customTool, UnityEngine.Object target, UnityEditor.Editor e, TargetModifiedFunc targetModifiedFunc)
        : this(customTool, target, e, targetModifiedFunc, new DictionaryCustomToolEditorPrefs())
		{

		}

		public CustomToolEditorContext (object customTool, UnityEngine.Object target, UnityEditor.Editor e, TargetModifiedFunc targetModifiedFunc, CustomToolEditorPrefs editorPrefs)
		{
			this.customTool = customTool;
			this.target = target;
			this.editorHost = e;
			//          this.toolResolver = toolResolver;
			this.targetModifiedFunc = targetModifiedFunc;
			if (null == editorPrefs) {
				editorPrefs = new DictionaryCustomToolEditorPrefs ();
			}
			this.editorPrefs = editorPrefs;

		}
        
		public object CustomTool {
			get {
				return customTool;
			}
		}
        
		public UnityEditor.Editor EditorHost {
			get {
				return editorHost;
			}
		}

		public UnityEngine.Object Target {
			get {
				return target;
			}
		}

		public CustomToolEditorPrefs CustomToolEditorPrefs {
			get {
				return editorPrefs;
			}
		}

		//      public CustomToolResolver ToolResolver {
		//          get {
		//              return toolResolver;
		//          }
		//      }
		public void TargetModified ()
		{
			if (null != targetModifiedFunc) {
				targetModifiedFunc ();
			}
		}


	}
}
