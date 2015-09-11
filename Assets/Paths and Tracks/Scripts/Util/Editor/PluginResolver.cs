using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Util.Editor
{

	// TODO refactor the whole "customtool" system
	// We should have:
	// Attribute "Component" (or similar) annotates the class to be resolvable by CustomToolResolver
	// - Classes having attribute that is subclass of Component should also be resolved!
	//   - As an alternative the Component attribute class should be sealed; in such case components
	//     could have other attributes as well (such as PathModifier)
	// Attribute "ComponentEditor" to replace CustomToolEditor
	//


	// TODO add documentation (briefly)
	// TODO rename to ComponentResolver (or SubComponentResolver or so)
	public class PluginResolver
	{
		public static readonly PluginResolver DefaultResolver = new PluginResolver ();
        
		private static readonly Dictionary<int, PluginResolver> _cachedResolvers = new Dictionary<int, PluginResolver> ();

		//private Type toolBaseType;

		//private Type toolAttribute;
        
		//private Type toolEditorBaseType;
		//private Type toolEditorAttribute;

		private const string CACHE_KEY_PLUGIN_TYPES = "PluginTypes";
		private const string CACHE_KEY_EDITOR_TYPES = "EditorTypes";
		private const string CACHE_KEY_EDITOR_TYPE_PREFIX = "EditorType.";
		private const string CACHE_KEY_DISPLAY_NAME_PREFIX = "DisplayName.";
		private const string CACHE_KEY_DESCRIPTION_PREFIX = "Description.";

		// TODO Currently the cache scope is not configurable (instance vs static vs custom)?
		private TypeCache typeCache = new TypeCache ();
        
		public delegate int MatchPluginEditorTypeFunc (Type pluginType,Type editorTypeCandidate);

		private Func<Type[]> findPluginTypesFunc;
		private Func<Type[]> findEditorTypesFunc;

		private MatchPluginEditorTypeFunc matchPluginEditorTypeFunc;

		private Func<Type, string> getPluginDisplayNameFunc;

		protected PluginResolver () : this(DefaultFindPluginTypes, DefaultFindEditorTypes)
		{
		}

		protected PluginResolver (Func<Type[]> findToolTypesFunc, Func<Type[]> findEditorTypesFunc)
			:this(findToolTypesFunc, findEditorTypesFunc, DefaultMatchEditorType, DefaultGetPluginDisplayName) // TODO defaults for last two args
		{
		}
        
		protected PluginResolver (Func<Type[]> findToolTypesFunc, Func<Type[]> findEditorTypesFunc, 
		                       MatchPluginEditorTypeFunc matchPluginEditorTypeFunc, Func<Type, string> getPluginDisplayNameFunc)
		{
			this.findPluginTypesFunc = findToolTypesFunc;
			this.findEditorTypesFunc = findEditorTypesFunc;
			this.matchPluginEditorTypeFunc = matchPluginEditorTypeFunc;
			this.getPluginDisplayNameFunc = getPluginDisplayNameFunc;
		}

		protected PluginResolver (Type pluginBaseType) : this(FindTypesFuncForPluginBaseType(pluginBaseType), DefaultFindEditorTypes)
		{

		}
		protected PluginResolver (Type pluginBaseType, Func<Type, string> getPluginDisplayNameFunc)
			: this(FindTypesFuncForPluginBaseType(pluginBaseType), DefaultFindEditorTypes, DefaultMatchEditorType, getPluginDisplayNameFunc)
		{
			if (null == getPluginDisplayNameFunc) {
				this.getPluginDisplayNameFunc = DefaultGetPluginDisplayName;
			}
		}

		private static Func<Type[]> FindTypesFuncForPluginBaseType (Type pluginBaseType)
		{
			return () => {
				return Util.TypeUtil.FindTypesHavingAttribute (typeof(Plugin), pluginBaseType);};
		}

		public static PluginResolver ForPluginType (Type pluginType)
		{
			return ForPluginType (pluginType, typeof(IPluginEditor));
		}
		
		
		public static PluginResolver ForPluginType (Type pluginType, Type pluginEditorType)
		{
			PluginResolver resolver;

			int cacheKey;
			unchecked {
				cacheKey = pluginType.GetHashCode () ^ pluginEditorType.GetHashCode ();
			}

			lock (_cachedResolvers) {
				if (_cachedResolvers.ContainsKey (cacheKey)) {
					resolver = _cachedResolvers [cacheKey];
				} else {
					Func<Type[]> findPluginTypes = () => 
					{
						return Util.TypeUtil.FindTypesHavingAttribute (typeof(Plugin), pluginType);
					};
					Func<Type[]> findEditorTypes = () => 
					{
						return Util.TypeUtil.FindTypesHavingAttribute (typeof(PluginEditor), pluginEditorType);
					};
					// TODO implement static cache!
					resolver = new PluginResolver (findPluginTypes, findEditorTypes);
					_cachedResolvers [cacheKey] = resolver;
				}
			}

			return resolver;

		}
	
		public static Type[] DefaultFindPluginTypes ()
		{
			return Util.TypeUtil.FindTypesHavingAttribute (typeof(Plugin), typeof(object));
		}
		
		public static Type[] DefaultFindEditorTypes ()
		{
			return Util.TypeUtil.FindTypesHavingAttribute (typeof(PluginEditor), typeof(IPluginEditor));
		}

		public static int DefaultMatchEditorType (Type pluginType, Type editorTypeCandidate)
		{
			return DoMatchPluginEditorTypeByPluginEditorAttribute (pluginType, editorTypeCandidate);
		}

		private static int DoMatchPluginEditorTypeByPluginEditorAttribute (Type pluginType, Type editorTypeCandidate)
		{
			int bestMatch = -1;
			object[] attributes = editorTypeCandidate.GetCustomAttributes (typeof(PluginEditor), true);
			
			foreach (object attrObj in attributes) {
				PluginEditor cte = (PluginEditor)attrObj;
				int match = Util.TypeUtil.HierarchyDistance (pluginType, cte.PluginType);
				if (match >= 0 && (bestMatch < 0 || match < bestMatch)) {
					bestMatch = match;
					if (bestMatch == 0) {
						break;
					}
				}
			}
			return bestMatch;
		}


		public static string DefaultGetPluginDisplayName (Type pluginType)
		{
			return Plugin.GetPluginName (pluginType);
		}

		protected static string FallbackGetPluginDisplayName (Type toolType)
		{
			return toolType.Name;
		}
//
		// TODO what's this and why it's in here?
//		public delegate Type xxxGetInspectedTypeFunc (object attr);
        
//		protected static int DoMatchToolEditorByAttribute (Type toolType, Type editorTypeCandidate, Type attrType, GetInspectedTypeFunc getInspectedTypeFunc)
//		{
//			int bestMatch = -1;
//			object[] attributes = editorTypeCandidate.GetCustomAttributes (attrType, false);
//			if (attributes.Length == 0) {
//				attributes = editorTypeCandidate.GetCustomAttributes (attrType, true);
//			}
//			foreach (object attrObj in attributes) {
//				//object attr = (CustomToolEditor)attrObj;
//				int match = Util.TypeUtil.HierarchyDistance (toolType, getInspectedTypeFunc (attrObj));
//				if (match >= 0 && (bestMatch < 0 || match < bestMatch)) {
//					bestMatch = match;
//				}
//			}
//			return bestMatch;
//		}
        


		private static string CacheTypeKey (string prefix, Type type)
		{
			return prefix + type.FullName;
		}


		/// <summary>
		/// Find all types implementing or extending the configured "toolBaseType" and 
		/// (optionally) having the specified "toolAttribute" attribute.
		/// </summary>
		/// <returns>The tool types.</returns>
		public Type[] FindPluginTypes ()
		{
			Type[] types;
			if (typeCache.ContainsKey (CACHE_KEY_PLUGIN_TYPES)) {
				types = typeCache.GetValue<Type[]> (CACHE_KEY_PLUGIN_TYPES);
			} else {
				types = findPluginTypesFunc ();
				typeCache.AddValue (CACHE_KEY_PLUGIN_TYPES, types);
			}
			return types;
		}
        
		public Type[] FindEditorTypes ()
		{
			Type[] types;
			if (typeCache.ContainsKey (CACHE_KEY_EDITOR_TYPES)) {
				types = typeCache.GetValue<Type[]> (CACHE_KEY_EDITOR_TYPES);
			} else {
				types = findEditorTypesFunc ();
				typeCache.AddValue (CACHE_KEY_EDITOR_TYPES, types);
			}
			return types;
		}


		public Type FindEditorType (Type pluginType)
		{
			Type bestEditorType;
			string cacheKey = CacheTypeKey (CACHE_KEY_EDITOR_TYPE_PREFIX, pluginType);
			if (typeCache.ContainsKey (cacheKey)) {
				bestEditorType = typeCache.GetValue<Type> (cacheKey);
			} else {
				Type[] editorTypes = FindEditorTypes ();
				int bestEditorTypeMatch = int.MaxValue;
				bestEditorType = null;
				for (int i = 0; i < editorTypes.Length; i++) {
					Type et = editorTypes [i];
					int match = matchPluginEditorTypeFunc (pluginType, et);
					if (match >= 0 && match < bestEditorTypeMatch) {
						bestEditorTypeMatch = match;
						bestEditorType = et;
					}
				}
				typeCache.AddValue (cacheKey, bestEditorType);
			}
			return bestEditorType;
		}
        
		public string GetPluginDisplayName (Type pluginType)
		{
			string dn;
			string cacheKey = CacheTypeKey (CACHE_KEY_DISPLAY_NAME_PREFIX, pluginType);
			if (typeCache.ContainsKey (cacheKey)) {
				dn = typeCache.GetValue<string> (cacheKey);
			} else {
				dn = getPluginDisplayNameFunc (pluginType);
				if (null == dn) {
//					dn = (null != fallbackDisplayNameResolver) ? fallbackDisplayNameResolver (pluginType) : DefaultFallbackDisplayNameResolver (pluginType);
					dn = FallbackGetPluginDisplayName (pluginType);
				}
				typeCache.AddValue (cacheKey, dn);
			}
			return dn;
		}

		public IPluginEditor CreatePluginEditorInstance (Type pluginType)
		{
			IPluginEditor editor;
			Type editorType = FindEditorType (pluginType);
			if (null != editorType) {
				if (typeof(ScriptableObject).IsAssignableFrom (editorType)) {
					editor = (IPluginEditor)ScriptableObject.CreateInstance (editorType);
				} else {
					editor = (IPluginEditor)Activator.CreateInstance (editorType);
				}
			} else {
				editor = null;
			}
			return editor;
		}

		public IPluginEditor CreatePluginEditorInstance (object pluginInstance)
		{
			IPluginEditor editor;
			if (null != pluginInstance) {
				editor = CreatePluginEditorInstance (pluginInstance.GetType ());
			} else {
				editor = null;
			}
			return editor;
		}
	}
}
