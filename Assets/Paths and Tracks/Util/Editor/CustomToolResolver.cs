using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Util.Editor
{

	// TODO add documentation (briefly)
	public class CustomToolResolver
	{
        
		//private Type toolBaseType;
		//private Type toolAttribute;
        
		//private Type toolEditorBaseType;
		//private Type toolEditorAttribute;

		private const string CACHE_KEY_TOOL_TYPES = "ToolTypes";
		private const string CACHE_KEY_EDITOR_TYPES = "EditorTypes";
		private const string CACHE_KEY_EDITOR_TYPE_PREFIX = "EditorType.";
		private const string CACHE_KEY_DISPLAY_NAME_PREFIX = "DisplayName.";
		private const string CACHE_KEY_DESCRIPTION_PREFIX = "Description.";

		// TODO Currently the cache scope is not configurable (instance vs static vs custom)?
		private TypeCache cache = new TypeCache ();
        
		public delegate Type[] FindTypesFunc ();

		public delegate Type[] FindEditorTypesFunc ();
        
		public delegate int ToolEditorMatcherFunc (Type toolType,Type editorTypeCandidate);

		public delegate string DisplayNameResolverFunc (Type toolType);
        
		private FindTypesFunc toolTypesFinder;
		private FindTypesFunc editorTypesFinder;
		private ToolEditorMatcherFunc editorMatcher;
		private DisplayNameResolverFunc displayNameResolver;
		private DisplayNameResolverFunc fallbackDisplayNameResolver;

		public ToolEditorMatcherFunc DefaultEditorMatcher {
			get {
				return DoMatchToolEditorByCustomToolEditorAttribute;
			}
		}

		public DisplayNameResolverFunc DefaultDisplayNameResolver {
			get {
				return CustomTool.GetToolName;
			}
		}

		public CustomToolResolver ()
		{
			this.editorMatcher = DefaultEditorMatcher;
			this.displayNameResolver = DefaultDisplayNameResolver;
		}

		public CustomToolResolver (FindTypesFunc toolTypesFinder, FindTypesFunc editorTypesFinder)
        : this()
		{
			this.toolTypesFinder = toolTypesFinder;
			this.editorTypesFinder = editorTypesFinder;
		}
        
		public CustomToolResolver (FindTypesFunc toolTypesFinder, FindTypesFunc editorTypesFinder, 
                                  ToolEditorMatcherFunc toolEditorMatcher, DisplayNameResolverFunc displayNameResolver) 
        : this(toolTypesFinder, editorTypesFinder)
		{
			this.editorMatcher = toolEditorMatcher;
			this.displayNameResolver = displayNameResolver;
		}

		public FindTypesFunc ToolTypesFinder {
			get {
				return this.toolTypesFinder;
			}
			set {
				toolTypesFinder = value;
			}
		}

		public FindTypesFunc EditorTypesFinder {
			get {
				return this.editorTypesFinder;
			}
			set {
				editorTypesFinder = value;
			}
		}

		public ToolEditorMatcherFunc EditorMatcher {
			get {
				return this.editorMatcher;
			}
			set {
				editorMatcher = value;
			}
		}

		public DisplayNameResolverFunc DisplayNameResolver {
			get {
				return this.displayNameResolver;
			}
			set {
				displayNameResolver = value;
			}
		}

		public DisplayNameResolverFunc FallbackDisplayNameResolver {
			get {
				return this.fallbackDisplayNameResolver;
			}
			set {
				fallbackDisplayNameResolver = value;
			}
		}
        
		//      private static Type[] FindToolTypesFunc() {
		//          return Util.TypeUtil.FindTypesHavingAttribute(toolAttribute, toolBaseType);
		//      }

		public delegate Type GetInspectedTypeFunc (object attr);
        
		protected static int DoMatchToolEditorByAttribute (Type toolType, Type editorTypeCandidate, Type attrType, GetInspectedTypeFunc getInspectedTypeFunc)
		{
			int bestMatch = -1;
			object[] attributes = editorTypeCandidate.GetCustomAttributes (attrType, false);
			if (attributes.Length == 0) {
				attributes = editorTypeCandidate.GetCustomAttributes (attrType, true);
			}
			foreach (object attrObj in attributes) {
				//object attr = (CustomToolEditor)attrObj;
				int match = Util.TypeUtil.HierarchyDistance (toolType, getInspectedTypeFunc (attrObj));
				if (match >= 0 && (bestMatch < 0 || match < bestMatch)) {
					bestMatch = match;
				}
			}
			return bestMatch;
		}
        
		protected static int DoMatchToolEditorByCustomToolEditorAttribute (Type toolType, Type editorTypeCandidate)
		{
			int bestMatch = -1;
			object[] attributes = editorTypeCandidate.GetCustomAttributes (typeof(CustomToolEditor), false);
			if (attributes.Length == 0) {
				attributes = editorTypeCandidate.GetCustomAttributes (typeof(CustomToolEditor), true);
			}
			foreach (object attrObj in attributes) {
				CustomToolEditor cte = (CustomToolEditor)attrObj;
				int match = Util.TypeUtil.HierarchyDistance (toolType, cte.inspectedType);
				if (match >= 0 && (bestMatch < 0 || match < bestMatch)) {
					bestMatch = match;
					if (bestMatch == 0) {
						break;
					}
				}
			}
			return bestMatch;
		}

		protected static string DefaultFallbackDisplayNameResolver (Type toolType)
		{
			return toolType.Name;
		}

		private static string CacheTypeKey (string prefix, Type type)
		{
			return prefix + type.FullName;
		}

		// Cached types
		//private Type[] toolTypes;
		//private Dictionary<Type, Type> editorTypes = new Dictionary<Type, Type>();
        
		/// <summary>
		/// Find all types implementing or extending the configured "toolBaseType" and 
		/// (optionally) having the specified "toolAttribute" attribute.
		/// </summary>
		/// <returns>The tool types.</returns>
		public Type[] FindToolTypes ()
		{
			Type[] types;
			if (cache.ContainsKey (CACHE_KEY_TOOL_TYPES)) {
				types = cache.GetValue<Type[]> (CACHE_KEY_TOOL_TYPES);
			} else {
				types = toolTypesFinder ();
				cache.AddValue (CACHE_KEY_TOOL_TYPES, types);
			}
			return types;
		}
        
		public Type[] FindEditorTypes ()
		{
			Type[] types;
			if (cache.ContainsKey (CACHE_KEY_EDITOR_TYPES)) {
				types = cache.GetValue<Type[]> (CACHE_KEY_EDITOR_TYPES);
			} else {
				types = editorTypesFinder ();
				cache.AddValue (CACHE_KEY_EDITOR_TYPES, types);
			}
			return types;
		}

		public Type FindEditorType (Type toolType)
		{
			Type bestEditorType;
			string cacheKey = CacheTypeKey (CACHE_KEY_EDITOR_TYPE_PREFIX, toolType);
			if (cache.ContainsKey (cacheKey)) {
				bestEditorType = cache.GetValue<Type> (cacheKey);
			} else {
				Type[] editorTypes = FindEditorTypes ();
				int bestEditorTypeMatch = int.MaxValue;
				bestEditorType = null;
				for (int i = 0; i < editorTypes.Length; i++) {
					Type et = editorTypes [i];
					int match = editorMatcher (toolType, et);
					if (match >= 0 && match < bestEditorTypeMatch) {
						bestEditorTypeMatch = match;
						bestEditorType = et;
					}
				}
				cache.AddValue (cacheKey, bestEditorType);
			}
			return bestEditorType;
		}
        
		public string GetToolDisplayName (Type toolType)
		{
			string dn;
			string cacheKey = CacheTypeKey (CACHE_KEY_DISPLAY_NAME_PREFIX, toolType);
			if (cache.ContainsKey (cacheKey)) {
				dn = cache.GetValue<string> (cacheKey);
			} else {
				dn = displayNameResolver (toolType);
				if (null == dn) {
					dn = (null != fallbackDisplayNameResolver) ? fallbackDisplayNameResolver (toolType) : DefaultFallbackDisplayNameResolver (toolType);
				}
				cache.AddValue (cacheKey, dn);
			}
			return dn;
		}

		public object CreateToolEditorInstance (Type toolType)
		{
			object editor;
			Type editorType = FindEditorType (toolType);
			if (null != editorType) {
				if (typeof(ScriptableObject).IsAssignableFrom (editorType)) {
					editor = ScriptableObject.CreateInstance (editorType);
				} else {
					editor = Activator.CreateInstance (editorType);
				}
			} else {
				editor = null;
			}
			return editor;
		}

		public object CreateToolEditorInstance (object toolInstance)
		{
			object editor;
			if (null != toolInstance) {
				editor = CreateToolEditorInstance (toolInstance.GetType ());
			} else {
				editor = null;
			}
			return editor;
		}
	}
}
