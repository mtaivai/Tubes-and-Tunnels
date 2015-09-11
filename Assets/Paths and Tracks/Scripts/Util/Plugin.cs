using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Util
{
//	public enum PluginLifecycle {
//		Init,
//		Enable,
//		Disable,
//		Destroy,
//	}
//	public class PluginLifecyleEventArgs : EventArgs {
//		private PluginLifecycle lifecycle;
//		private object plugin;
//		private UnityEngine.Object containerObject;
//		public PluginLifecyleEventArgs(object plugin, PluginLifecycle lifecycle) {
//			this.plugin = plugin;
//			this.lifecycle = lifecycle;
//		}
//		public PluginLifecycle Lifecycle {
//			get {
//				return lifecycle;
//			}
//		}
//	}
//
//	public interface IPluginLifecycleAware {
//		void HandlePluginLifecycleEvent(PluginLifecyleEventArgs e);
//	}
//
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class Plugin : System.Attribute
	{
        
		public string name;
		public string description;
        
		public Plugin () : this(null)
		{
		}

		public Plugin (string name)
		{
			this.name = name;
		}
        
		public static string GetPluginName (Type pluginType)
		{
            
			object[] attrs = pluginType.GetCustomAttributes (typeof(Plugin), false);
			if (attrs.Length == 0) {
				attrs = pluginType.GetCustomAttributes (typeof(Plugin), true);
			}
			foreach (object attr in attrs) {
				Plugin pluginAttr = (Plugin)attr;
				if (!StringUtil.IsEmpty (pluginAttr.name)) {
					return pluginAttr.name;
				}
			}
			return null;
		}
	}

	public static class PluginUtil
	{

#if UNITY_EDITOR 
		public static void DeletePluginEditorPrefs (object pluginInstance)
		{
			
			if (Application.isEditor) {
				// This is a HACK...
				// ... let our (possible) editor clean up its stuff (such as EditorPrefs)
				
				// Call method DeletePluginEditorPrefs of class "Util.PluginEditorUtil:
				// public static void DeletePluginEditorPrefs (object pluginInstance)

				Type peuType = Type.GetType ("Util.PluginEditorUtil,Assembly-CSharp-Editor", false, false);
				if (null == peuType) {
					Debug.LogError ("Class Util.PluginEditorUtil is not available; can't fire DeletePluginEditorPrefs event to editor of " + pluginInstance);
				} else {
					System.Reflection.MethodInfo deletePrefsMethod = peuType.GetMethod ("DeletePluginEditorPrefs");
					if (null != deletePrefsMethod) {
						deletePrefsMethod.Invoke (null, new object[]{pluginInstance});
					} else {
						Debug.LogError ("Method Util.PluginEditorUtil.DeletePluginEditorPrefs is not available; can't fire DeletePluginEditorPrefs event to of: " + pluginInstance);
					}
				}
				
			}
		}
#else
		public static void DeletePluginEditorPrefs (object pluginInstance)
		{
			// Nothing to be done
		}
#endif

	}
    
}
