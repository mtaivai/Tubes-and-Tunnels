using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using Util;
using Util.Editor;
using Paths;

namespace Paths.Editor
{


	public class PathModifierResolver : PluginResolver
	{
		public static readonly PathModifierResolver Instance = 
			new PathModifierResolver ();
//		private PathModifierResolver () : base(typeof(IPathModifier), AbstractPathModifier.GetDisplayName)
//		{
//		}
		private PathModifierResolver () : base(FindPathModifierTypes, DefaultFindEditorTypes, DefaultMatchEditorType, AbstractPathModifier.GetDisplayName)
		{
		}

		private static Type[] FindPathModifierTypes ()
		{
//			// The standard implementation by Plugin attribute:
//			Type[] typesByPluginAttr = TypeUtil.FindTypesHavingAttribute (typeof(Plugin), typeof(IPathModifier));
//
//			// Also include classes w/ PathModifier attribute even if they don't have the Plugin attr:
//			Type[] typesByPMAttr = TypeUtil.FindTypesHavingAttribute (typeof(PathModifier), typeof(IPathModifier));
//
//			List<Type> l = new List<Type> ();
//			l.AddRange (typesByPluginAttr);
//
//			foreach (Type t in typesByPMAttr) {
//				if (!l.Contains (t)) {
//					l.Add (t);
//				}
//			}
//			return l.ToArray ();
			return TypeUtil.FindTypesHavingAttribute (typeof(PathModifier), typeof(IPathModifier));
		}
	}

}
