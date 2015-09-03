using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using Util;
using Util.Editor;
using Paths;

namespace Paths.Editor
{
	// TODO Move PathModifierResolver class to its own file
	public class PathModifierInputFilterResolver : CustomToolResolver
	{
		private static PathModifierInputFilterResolver _instance;
		
		public static PathModifierInputFilterResolver Instance {
			get {
				if (null == _instance) {
					_instance = new PathModifierInputFilterResolver ();
				}
				return _instance;
			}
		}

		private static Type[] FindPathModifierInputFilterTypes ()
		{
			return Util.TypeUtil.FindTypesHavingAttribute (typeof(CustomTool), typeof(IPathModifier));
		}
		
		private static Type[] FindPathModifierInputFilterEditorTypes ()
		{
			return Util.TypeUtil.FindTypesHavingAttribute (typeof(CustomToolEditor), typeof(ICustomToolEditor));
		}
		
		//      private static int MatchPathModifierEditor (Type toolType, Type editorTypeCandidate)
		//      {
		//          return DoMatchToolEditorByAttribute (toolType, editorTypeCandidate, 
		//                                               typeof(CustomToolEditor), delegate(object attr) {
		//              return ((CustomToolEditor)attr).inspectedType;
		//          });
		//      }
		
		//      private static string GetPathModifierDisplayName (Type toolType)
		//      {
		//          
		//          object[] attrs = toolType.GetCustomAttributes (typeof(PathModifier), false);
		//          foreach (object attr in attrs) {
		//              PathModifier toolAttr = (PathModifier)attr;
		//              if (!StringUtil.IsEmpty (toolAttr.displayName)) {
		//                  return toolAttr.displayName;
		//              }
		//          }
		//          // Fallback:
		//          return StringUtil.RemoveStringTail (StringUtil.RemoveStringTail (toolType.Name, "Modifier", 1), "Path", 1);
		//      }
		
		//      private static string GetDefaultPathModifierDisplayName (Type toolType)
		//      {
		//            return AbstractPathModifier.GetFallbackDisplayName(toolType);
		//      }
		
		public PathModifierInputFilterResolver ()
			: base(FindPathModifierInputFilterTypes, FindPathModifierInputFilterEditorTypes)
		{
			//DisplayNameResolver = AbstractPathModifier.GetDisplayName;
			//FallbackDisplayNameResolver = null;
		}

	}

	public class PathModifierResolver : CustomToolResolver
	{
		private static PathModifierResolver _instance;

		public static PathModifierResolver Instance {
			get {
				if (null == _instance) {
					_instance = new PathModifierResolver ();
				}
				return _instance;
			}
		}
    
		private static Type[] FindPathModifierTypes ()
		{
			return Util.TypeUtil.FindTypesHavingAttribute (typeof(PathModifier), typeof(IPathModifier));
		}

		private static Type[] FindPathModifierEditorTypes ()
		{
			return Util.TypeUtil.FindTypesHavingAttribute (typeof(CustomToolEditor), typeof(IPathModifierEditor));
		}

//      private static int MatchPathModifierEditor (Type toolType, Type editorTypeCandidate)
//      {
//          return DoMatchToolEditorByAttribute (toolType, editorTypeCandidate, 
//                                               typeof(CustomToolEditor), delegate(object attr) {
//              return ((CustomToolEditor)attr).inspectedType;
//          });
//      }
        
//      private static string GetPathModifierDisplayName (Type toolType)
//      {
//          
//          object[] attrs = toolType.GetCustomAttributes (typeof(PathModifier), false);
//          foreach (object attr in attrs) {
//              PathModifier toolAttr = (PathModifier)attr;
//              if (!StringUtil.IsEmpty (toolAttr.displayName)) {
//                  return toolAttr.displayName;
//              }
//          }
//          // Fallback:
//          return StringUtil.RemoveStringTail (StringUtil.RemoveStringTail (toolType.Name, "Modifier", 1), "Path", 1);
//      }

//      private static string GetDefaultPathModifierDisplayName (Type toolType)
//      {
//            return AbstractPathModifier.GetFallbackDisplayName(toolType);
//      }

		public PathModifierResolver ()
        : base(FindPathModifierTypes, FindPathModifierEditorTypes)
		{
			DisplayNameResolver = AbstractPathModifier.GetDisplayName;
			FallbackDisplayNameResolver = null;
		}

    

	}

}
