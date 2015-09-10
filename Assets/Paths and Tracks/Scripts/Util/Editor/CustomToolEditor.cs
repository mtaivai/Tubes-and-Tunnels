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
    

	// TODO EditorState and EditorPrefs should be separate concepts!

	// TODO move ContextEditorPrefs to its own file
	public class ContextEditorPrefs
	{
		private string contextPrefix;
		public ContextEditorPrefs (string prefix)
		{
			this.contextPrefix = prefix;
		}
		public string Prefix {
			get {
				return contextPrefix;
			}
		}

		private string PrefixKey (string key)
		{
			return contextPrefix + key;
		}

		public bool HasKey (string key)
		{
			key = PrefixKey (key);
			return UnityEditor.EditorPrefs.HasKey (key);
		}
		public void DeleteKey (string key)
		{
			key = PrefixKey (key);
			UnityEditor.EditorPrefs.DeleteKey (key);
		}

		public string GetString (string key, string defaultValue = "")
		{
			key = PrefixKey (key);
			return UnityEditor.EditorPrefs.GetString (key, defaultValue);
		}
		public void SetString (string key, string value)
		{
			key = PrefixKey (key);
			UnityEditor.EditorPrefs.SetString (key, value);
		}
		public bool GetBool (string key, bool defaultValue = false)
		{
			key = PrefixKey (key);
			return UnityEditor.EditorPrefs.GetBool (key, defaultValue);
		}
		public void SetBool (string key, bool value)
		{
			key = PrefixKey (key);
			UnityEditor.EditorPrefs.SetBool (key, value);
		}
		public int GetInt (string key, int defaultValue = 0)
		{
			key = PrefixKey (key);
			return UnityEditor.EditorPrefs.GetInt (key, defaultValue);
		}
		public void SetInt (string key, int value)
		{
			key = PrefixKey (key);
			UnityEditor.EditorPrefs.SetInt (key, value);
		}
		public float GetFloat (string key, float defaultValue = 0f)
		{
			key = PrefixKey (key);
			return UnityEditor.EditorPrefs.GetFloat (key, defaultValue);
		}
		public void SetFloat (string key, float value)
		{
			key = PrefixKey (key);
			UnityEditor.EditorPrefs.SetFloat (key, value);
		}
	}

	public class CustomToolEditorContext
	{
		public delegate void TargetModifiedFunc ();
        
		//      private CustomToolResolver toolResolver;

		private object customTool;
		private UnityEditor.Editor editorHost;
		private UnityEngine.Object target;
		private TargetModifiedFunc targetModifiedFunc;
		private ContextEditorPrefs editorPrefs;

//		public CustomToolEditorContext (object customTool, UnityEngine.Object target, UnityEditor.Editor e, TargetModifiedFunc targetModifiedFunc)
//			: this(customTool, target, e, targetModifiedFunc, new ContextEditorPrefs(""))
//		{
//
//		}
		public CustomToolEditorContext (object customTool, UnityEngine.Object target, UnityEditor.Editor e, TargetModifiedFunc targetModifiedFunc, string editorPrefsPrefix)
			: this(customTool, target, e, targetModifiedFunc, new ContextEditorPrefs(editorPrefsPrefix))
		{
		}
		public CustomToolEditorContext (object customTool, UnityEngine.Object target, UnityEditor.Editor e, TargetModifiedFunc targetModifiedFunc, ContextEditorPrefs editorPrefs)
		{
			this.customTool = customTool;
			this.target = target;
			this.editorHost = e;
			//          this.toolResolver = toolResolver;
			this.targetModifiedFunc = targetModifiedFunc;
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

		public ContextEditorPrefs ContextEditorPrefs {
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
