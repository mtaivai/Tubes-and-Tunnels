using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Util
{
	[AttributeUsage(AttributeTargets.Class)]
	public class CodeQuality : Attribute
	{
		[AttributeUsage(AttributeTargets.Class)]
		public class Unknown : CodeQuality
		{
			public Unknown () : base(Threshold.Unknown)
			{
			}
		}

		[AttributeUsage(AttributeTargets.Class)]
		public class Beta : CodeQuality
		{
			public Beta () : base(Threshold.Beta)
			{
			}
		}
		[AttributeUsage(AttributeTargets.Class)]
		public class Experimental : CodeQuality
		{
			public Experimental () : base(Threshold.Experimental)
			{
			}
		}
		
		[AttributeUsage(AttributeTargets.Class)]
		public class Stable : CodeQuality
		{
			public Stable () : base(Threshold.Stable)
			{
			}
		}

		public enum Threshold : int
		{
			Unknown = 0,
			Experimental = 1,
			Beta = 50,
			Stable = 100,
		}
//		public static readonly int ExperimentalThreshold = 0;
//		public static readonly int BetaThreshold = 50;
//		public static readonly int StableThreshold = 100;
		private Threshold qualityLevel;
		private string name;
		public CodeQuality (Threshold qualityThreshold)
		{
			this.qualityLevel = qualityThreshold;

		}
		public string Name {
			get {
				if (null == name) {
					int[] thresholdValues = (int[])Enum.GetValues (typeof(Threshold));
					for (int i = 0; i < thresholdValues.Length; i++) {
						if ((int)qualityLevel == i) {
							name = Enum.GetName (typeof(Threshold), i);
							break;
						}
					}
					if (null == name) {
						if (IsStable) {
							name = "Stable";
						} else if (IsBeta) {
							name = "Beta";
						} else if (IsExperimental) {
							name = "Experimental";
						} else {
							name = "Unknown";
						}
					}
				}
				return name;
			}
		}
		public Threshold QualityThreshold {
			get {
				return this.qualityLevel;
			}
		}
		public bool IsExperimental {
			get {
				return qualityLevel >= Threshold.Experimental && qualityLevel < Threshold.Beta;
			}
		}
		public bool IsBeta {
			get {
				return qualityLevel >= Threshold.Beta && qualityLevel < Threshold.Stable;
			}
		}
		public bool IsStable {
			get {
				return qualityLevel >= Threshold.Stable;
			}
		}
	}




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
            
			object[] attrs = pluginType.GetCustomAttributes (typeof(Plugin), true);
			foreach (object attr in attrs) {
				Plugin pluginAttr = (Plugin)attr;
				if (!StringUtil.IsEmpty (pluginAttr.name)) {
					return pluginAttr.name;
				}
			}
			return null;
		}
		public static CodeQuality GetPluginCodeQuality (Type pluginType)
		{
			object[] attrs = pluginType.GetCustomAttributes (typeof(CodeQuality), true);
			foreach (object attr in attrs) {
				CodeQuality pluginAttr = attr as CodeQuality;
				if (null != pluginAttr) {
					return pluginAttr;
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
