using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Paths
{
	public class PathModifierUtil
	{

		public static string GetDisplayName (IPathModifier pm)
		{
			string n = pm.GetInstanceName ();
			if (StringUtil.IsEmpty (n)) {
				n = pm.GetName ();
				if (StringUtil.IsEmpty (n)) {
					n = AbstractPathModifier.GetDisplayName (pm.GetType ());
				}
			}
			return n;
		}

		public static PathPoint[] RunPathModifiers (PathModifierContext context, PathPoint[] pathPoints, ref int flags, bool fixResultFlags)
		{
			IPathModifier[] modifiers = context.PathModifierContainer.GetPathModifiers ();

			foreach (IPathModifier mod in modifiers) {
				if (!mod.IsEnabled ()) {
					continue;
				}
				PathModifierContext pmc = new PathModifierContext (context.PathInfo, context.PathModifierContainer, flags, context.Parameters);

				pathPoints = mod.GetModifiedPoints (pathPoints, pmc);

				if (fixResultFlags) {
					bool gotNulls = false;
					int outputFlags = mod.GetOutputFlags (pmc);
					for (int i = 0; i < pathPoints.Length; i++) {
						PathPoint pp = pathPoints [i];
						if (null == pp) {
							gotNulls = true;
							pathPoints [i] = new PathPoint ();
							//pathPoints [i].Flags = outputFlags;
						} else {
							if (pp.Flags != outputFlags) {
//								pathPoints [i].Flags = outputFlags;
								outputFlags &= pathPoints [i].Flags;
							}
						}
					}
					flags = outputFlags;
					if (gotNulls) {
						Debug.LogWarning ("PathModifier " + GetDisplayName (mod) + " (" + mod.GetType ().FullName + ") returned null point(s)");
					}
				}
				// input & (process | passthrough) | generate
				flags = (flags & (mod.GetPassthroughFlags (pmc) | mod.GetProcessFlags (pmc))) | mod.GetGenerateFlags (pmc);
                
			}
			return pathPoints;
		}

		public static void GetPathModifierCapsFromAttributes (Type pmType, out int inputFlags, out int processFlags, out int passtroughFlags, out int generateFlags)
		{
			inputFlags = PathPoint.NONE;
			processFlags = PathPoint.NONE;
			passtroughFlags = PathPoint.NONE;
			generateFlags = PathPoint.NONE;
			object[] attrs = pmType.GetCustomAttributes (typeof(PathModifier), false);
			foreach (object attr in attrs) {
				PathModifier pmAttr = (PathModifier)attr;
				inputFlags |= pmAttr.requiredInputFlags;
				processFlags |= pmAttr.processCaps;
				passtroughFlags |= pmAttr.passthroughCaps;
				generateFlags |= pmAttr.generateCaps;
			}
		}

		public static void SavePathModifiers (ParameterStore parameterStore, List<IPathModifier> pathModifiers)
		{
			// Remove unused PathModifier configurations:
			string[] pmParams = parameterStore.FindParametersStartingWith ("pathModifiers[");
			foreach (string n in pmParams) {
				//Debug.Log ("Removing: " + n);
				parameterStore.RemoveParameter (n);
			}
//            Debug.Log("Serializing " + pathModifiers.Count + " PathModifiers");
			parameterStore.SetInt ("pathModifiers.Count", pathModifiers.Count);
			for (int i = 0; i < pathModifiers.Count; i++) {
                
				IPathModifier pm = pathModifiers [i];
				ParameterStore pmstore = new ParameterStore (parameterStore, "pathModifiers[" + i + "]");
				pmstore.SetString ("Type", pm.GetType ().FullName);
				pm.SaveParameters (pmstore);
			}
		}

		public static List<IPathModifier> LoadPathModifiers (ParameterStore parameterStore)
		{
			// Materialize PathModifiers
			List<IPathModifier> pathModifiers = new List<IPathModifier> ();
			int pmCount = parameterStore.GetInt ("pathModifiers.Count");
//            Debug.Log("Materializing " + pmCount + " PathModifiers");
			for (int i = 0; i < pmCount; i++) {
				ParameterStore pmstore = new ParameterStore (parameterStore, "pathModifiers[" + i + "]");
				string pmTypeName = pmstore.GetString ("Type");
				Type pmType = Type.GetType (pmTypeName);
				if (null != pmType) {
					IPathModifier pm = (IPathModifier)Activator.CreateInstance (pmType);
					pm.LoadParameters (pmstore);
					pathModifiers.Add (pm);
				}
			}
			return pathModifiers;
		}
	}

    
}
