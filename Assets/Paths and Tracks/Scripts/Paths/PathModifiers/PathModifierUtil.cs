using UnityEngine;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Paths
{
	public static class PathModifierUtil
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

//		public static PathPoint[] RunPathModifiers (PathModifierContext context, PathPoint[] _pathPoints, bool protectInputPoints, ref int flags, bool fixResultFlags)
//		{
//			IPathModifier[] modifiers = context.PathModifierContainer.GetPathModifiers ();
//
//			long startTicks = System.DateTime.Now.Ticks;
//
//			PathPoint[] processedPoints;
//
//			if (protectInputPoints) {
//				processedPoints = new PathPoint[_pathPoints.Length];
//				// We cant't just Array.Copy(...) the array because PathPoints are mutable and
//				// any changes would still reflect the input array. We need to clone each point
//				// as well...
////				Array.Copy (_pathPoints, processedPoints, processedPoints.Length);
//				int c = _pathPoints.Length;
//				for (int i = 0; i < c; i++) {
//					processedPoints [i] = new PathPoint (_pathPoints [i]);
//				}
//			} else {
//				processedPoints = _pathPoints;
//			}
//
//			bool processingAborted = false;
//			foreach (IPathModifier mod in modifiers) {
//				if (processingAborted) {
//					break;
//				}
//				if (!mod.IsEnabled ()) {
//					continue;
//				}
//				PathModifierContext pmc = new PathModifierContext (context.PathInfo, context.PathModifierContainer, flags, context.Parameters);
//
//				// Timing:
//
//				long thisStartTicks = System.DateTime.Now.Ticks;
//
//				// Do run the path modifier
//				try {
//					processedPoints = mod.GetModifiedPoints (processedPoints, pmc);
//				
//					if (fixResultFlags) {
//						bool gotNulls = false;
//						int outputFlags = mod.GetOutputFlags (pmc);
//						for (int i = 0; i < processedPoints.Length; i++) {
//							PathPoint pp = processedPoints [i];
//							if (null == pp) {
//								gotNulls = true;
//								processedPoints [i] = new PathPoint ();
//								//pathPoints [i].Flags = outputFlags;
//							} else {
//								if (pp.Flags != outputFlags) {
//									//								pathPoints [i].Flags = outputFlags;
//									outputFlags &= processedPoints [i].Flags;
//								}
//							}
//						}
//						flags = outputFlags;
//						if (gotNulls) {
//							// TODO add to context warnings!
//							Debug.LogWarning ("PathModifier " + GetDisplayName (mod) + " (" + mod.GetType ().FullName + ") returned null point(s)");
//						}
//					
//					}
//					// input & (process | passthrough) | generate
//					flags = (flags & (mod.GetPassthroughFlags (pmc) | mod.GetProcessFlags (pmc))) | mod.GetGenerateFlags (pmc);
//					
//					long thisEndTicks = System.DateTime.Now.Ticks;
//					float thisDeltaTimeMs = (float)(thisEndTicks - thisStartTicks) / (float)System.TimeSpan.TicksPerMillisecond;
//					
//					Debug.Log ("Running PathModifier " + PathModifierUtil.GetDisplayName (mod) + " took " + thisDeltaTimeMs + " ms");
//				} catch (CircularPathReferenceException e) {
//					// TODO add errors to context!
//					string logMsg = string.Format ("Circular Path cross reference was detected while running PathModifier {0}; exception catched: {1} ", mod, e);
//					Debug.LogError (logMsg);
//					processingAborted = true;
//
//					string errorMsg = logMsg;
//					context.Errors.Add (errorMsg);
//
//					continue;
//				}
//
//
//			}
//			if (processingAborted) {
//				Debug.LogError ("Processing of PathModifiers was aborted due to previous errors");
//				processedPoints = new PathPoint[0];
//				flags = 0;
//			}
//
//			long endTicks = System.DateTime.Now.Ticks;
//			float deltaTimeMs = (float)(endTicks - startTicks) / (float)System.TimeSpan.TicksPerMillisecond;
//			Debug.Log ("Running " + modifiers.Length + " PathModifiers took " + deltaTimeMs + " ms");
//
//			return processedPoints;
//		}

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
				ParameterStore pmstore = parameterStore.ChildWithPrefix ("pathModifiers[" + i + "]");
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
				ParameterStore pmstore = parameterStore.ChildWithPrefix ("pathModifiers[" + i + "]");
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
