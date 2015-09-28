using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Util.Editor
{


	[AttributeUsage(AttributeTargets.Class)]
	public sealed class PluginEditor : System.Attribute
	{
        
		private Type pluginType;

		public PluginEditor (Type pluginType)
		{
			this.pluginType = pluginType;
		}
		public Type PluginType {
			get {
				return pluginType;
			}
		}

	}

	public interface IPluginEditor
	{
		void DrawInspectorGUI (PluginEditorContext context);
//		void DrawSceneGUI (PluginEditorContext context);

//		void DeleteEditorPrefs ();
		// Messages, no static API for these:
		// void OnDestroyPlugin()

//		void OnPluginLifecycle(PluginEditorContext context);

	}

	// TODO what's this and where it's used?
	public interface IPluginEditorHost
	{
		void SetEditorFor (object pluginInstance, IPluginEditor editor);
		IPluginEditor GetEditorFor (object pluginInstance);

	}
    



	public class PluginEditorContext
	{
		public delegate void TargetModifiedFunc ();
        
		//      private CustomToolResolver toolResolver;

		private object plugin;
		private UnityEditor.Editor editorHost;
		private UnityEngine.Object target;
		private TargetModifiedFunc targetModifiedFunc;
		private ParameterStore editorParams;
		private CodeQuality _codeQuality;

//		public CustomToolEditorContext (object customTool, UnityEngine.Object target, UnityEditor.Editor e, TargetModifiedFunc targetModifiedFunc)
//			: this(customTool, target, e, targetModifiedFunc, new ContextEditorPrefs(""))
//		{
//
//		}
//		public PluginEditorContext (object plugin, UnityEngine.Object target, UnityEditor.Editor e, TargetModifiedFunc targetModifiedFunc, string editorPrefsPrefix)
//			: this(plugin, target, e, targetModifiedFunc, new ContextEditorPrefs(editorPrefsPrefix))
//		{
//		}
		public PluginEditorContext (object plugin, UnityEngine.Object target, UnityEditor.Editor e, TargetModifiedFunc targetModifiedFunc, ParameterStore editorParams)
		{
			this.plugin = plugin;
			this.target = target;
			this.editorHost = e;
			//          this.toolResolver = toolResolver;
			this.targetModifiedFunc = targetModifiedFunc;
			this.editorParams = editorParams;


			if (null != plugin) {
				this._codeQuality = Plugin.GetPluginCodeQuality (plugin.GetType ());
			}
			if (null == _codeQuality) {
				_codeQuality = new CodeQuality.Unknown ();
			}
		}
        
		public object PluginInstance {
			get {
				return plugin;
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

		public ParameterStore EditorParameters {
			get {
				return editorParams;
			}
		}
		public CodeQuality PluginCodeQuality {
			get {
				return _codeQuality;
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
	public static class PluginEditorUtil
	{
		public static void DeletePluginEditorPrefs (object pluginInstance)
		{
			if (Application.isEditor) {
				// This is a HACK...
				// ... let our (possible) editor clean up its stuff (such as EditorPrefs)

//				IPluginEditor editorInstance = PluginResolver.DefaultResolver.CreatePluginEditorInstance (pluginInstance);
//				if (null != editorInstance) {
//					editorInstance.DeleteEditorPrefs ();
//				}
			}
		}
	}
}
