using UnityEngine;
using System;
using System.Reflection;

using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using Util;
using Util.Editor;
using Paths;

namespace Paths.Editor
{


	// TODO this class is getting quite complicated; please consider about refactoring
	public class PathModifierEditorUtil
	{



		public static void DrawPathModifiersInspector (
			Path path, IPathData pathData, 
			UnityEditor.Editor editor, UnityEngine.Object dirtyObject, 
			CustomToolEditorContext.TargetModifiedFunc modifiedCallback)
		{
//			Path path = pathData.GetPath ();
			IPathModifierContainer pmc = pathData.GetPathModifierContainer ();
			ParameterStore pmcParams = pmc.GetParameterStore ();

			ContextEditorPrefs prefs = new ContextEditorPrefs (pmcParams.Prefix);
			// Path Modifiers
			PathModifierEditorContext context = new PathModifierEditorContext (
				pathData, path, editor, modifiedCallback, prefs);
            
//			if (pmc.HasErrors()) {
//				context.
//			}

			DrawPathModifiersInspector (context, dirtyObject);
		}

		public static void DrawPathModifiersInspector (PathModifierEditorContext context, UnityEngine.Object dirtyObject, Action headerAction = null)
		{
			IPathModifierContainer container = context.PathModifierContainer;
			IPathModifier[] pathModifiers = container.GetPathModifiers ();

			IPathData pathData = context.PathData;

			bool haveErrors = container.HasMessages (PathModifierMessageType.Error);


			//container.HasErrors();

			// TODO is prefs already prefixed????
//			PrefixCustomToolEditorPrefs editorPrefs = new PrefixCustomToolEditorPrefs (
//                context.CustomToolEditorPrefs, "PathModifiers");
//
//
//          // Make sure that we have one NewPathModifier in the list
//          bool createNew = true;
//          foreach (IPathModifier pm in pathModifiers) {
//              if (pm is NewPathModifier) {
//                  createNew = false;
//                  break;
//              }
//          }
//          if (createNew) {
//              container.AddPathModifer(new NewPathModifier());
//              // Fetch new list of PathModifiers: (see #2)
//              pathModifiers = container.GetPathModifiers();
//
//              // The "new" item is hidden by default
//              visibilityPrefs.SetBool(pathModifiers.Length, false);
//          }
            
			EditorGUI.BeginChangeCheck ();
			int enabledPathModifierCount = 0;
			int totalPathModifierCount = 0;
			for (int i = 0; i < pathModifiers.Length; i++) {
				IPathModifier pm = pathModifiers [i];
				if (pm is NewPathModifier) {
					continue;
				}
				totalPathModifierCount++;
				if (pm.IsEnabled ()) {
					enabledPathModifierCount++;
				}

			}
			ContextEditorPrefs editorPrefs = context.ContextEditorPrefs;

			bool pathModifiersVisible = EditorGUILayout.Foldout (
				editorPrefs.GetBool (".Visible", true), "Path Modifiers (" + enabledPathModifierCount + "/" + totalPathModifierCount + ")" + (haveErrors ? " *** ERRORS ***" : ""));
			if (EditorGUI.EndChangeCheck ()) {
				editorPrefs.SetBool (".Visible", pathModifiersVisible);
                
			}


			Dictionary<string, object> pmParams = new Dictionary<string,object> ();
			if (pathModifiersVisible) {
				EditorGUI.indentLevel++;

				if (null != headerAction) {
					headerAction ();
				}
//				EditorGUI.indentLevel++;

				// TODO Foldout is bad GUI here
				EditorGUI.BeginChangeCheck ();
				bool messagesVisible = EditorGUILayout.Foldout (
					editorPrefs.GetBool (".MessagesVisible", true), "Messages" + (haveErrors ? " *** ERRORS ***" : ""));
				if (EditorGUI.EndChangeCheck ()) {
					editorPrefs.SetBool (".MessagesVisible", messagesVisible);
				}
				if (messagesVisible) {
					if (haveErrors) {
						// Collect all errors
						string errorsString = "Processing of PathModifier was aborted due to error(s): ";
						foreach (IPathModifier pm in pathModifiers) {
							foreach (string errorMsg in container.GetCurrentMessages(PathModifierMessageType.Error, pm)) {
								errorsString += "\n- " + errorMsg;
							}
						}
						EditorGUILayout.HelpBox (errorsString, MessageType.Error);
					}
				}
//				EditorGUI.indentLevel--;

				int combinedOutputFlags = CalculateCombinedOutputFlags (pathData, pmParams);
				// TODO use RED if some flag is missing!
				PathEditor.DrawPathPointMask ("Output Caps", combinedOutputFlags);


				int currentInputFlags = pathData.GetOutputFlagsBeforeModifiers ();

				bool hasPreviousErrors = false;

				bool hasNewPathModifier = false;


				for (int i = 0; i < pathModifiers.Length; i++) {
					IPathModifier pm = pathModifiers [i];
					if (pm is NewPathModifier) {
						hasNewPathModifier = true;
					}

					bool thisHasErrors = container.HasMessages (PathModifierMessageType.Error, pm);
					if (thisHasErrors) {
						haveErrors = true;
					}
					bool thisHasWarnings = container.HasMessages (PathModifierMessageType.Warning, pm);
					bool thisHasInfo = container.HasMessages (PathModifierMessageType.Info, pm);

					IPathInfo pathInfo = pathData.GetPathInfo ();

					PathModifierContext pmc = 
						new PathModifierContext (pathInfo, container, currentInputFlags, pmParams);

					pmc.Errors.AddRange (container.GetCurrentMessages (PathModifierMessageType.Error, pm));
					pmc.Warnings.AddRange (container.GetCurrentMessages (PathModifierMessageType.Warning, pm));
					pmc.Info.AddRange (container.GetCurrentMessages (PathModifierMessageType.Info, pm));


					if (pm.IsEnabled ()) {
						// TODO could we use pm.GetOutputFlags(pmc) in here?
						currentInputFlags = ((pm.GetPassthroughFlags (pmc) | pm.GetProcessFlags (pmc)) & currentInputFlags) | pm.GetGenerateFlags (pmc);
					}

					ContextEditorPrefs pmEditorPrefs = new ContextEditorPrefs (editorPrefs.Prefix + "[" + i + "]");

					PathModifierEditorContext pmec = new PathModifierEditorContext (
						context.PathData, pmc, pm, context.Path, context.EditorHost, context.TargetModified, pmEditorPrefs);

					DoDrawPathModifierInspector (pmec, hasPreviousErrors);
					if (thisHasErrors) {
						hasPreviousErrors = true;
					}

					// Add known context parameter names:
					string[] knownContextParams = pm.GetProducedContextParameters ();
					foreach (string ctxParam in knownContextParams) {
						if (!pmParams.ContainsKey (ctxParam)) {
							pmParams.Add (ctxParam, null);
						}
					}
				}
				EditorGUI.indentLevel--;

				
				if (!hasNewPathModifier) {
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("");
					if (GUILayout.Button ("Add New Path Modifier...", EditorStyles.miniButton, GUILayout.ExpandWidth (false))) {
						// TODO don't add if we already have one!
						// Add the "New" item:
						container.AddPathModifer (new NewPathModifier ());
						// Fetch new list of PathModifiers: (see #2)
						pathModifiers = container.GetPathModifiers ();
						
						// The "new" item is visible by default
						// TODO reimplement followin:
						//					visibilityPrefs.SetBool (pathModifiers.Length, true);
					}
					EditorGUILayout.EndHorizontal ();
				}
				

			}

//			EditorGUILayout.Separator ();
		}

		static int CalculateCombinedOutputFlags (IPathData pathData, Dictionary<string, object> pmParams)
		{
			// Calculate combined output of all PM's:
			IPathModifierContainer container = pathData.GetPathModifierContainer ();
			IPathModifier[] pathModifiers = container.GetPathModifiers ();

			int combinedOutputFlags = pathData.GetOutputFlags ();

			for (int i = 0; i < pathModifiers.Length; i++) {
				IPathModifier pm = pathModifiers [i];

				if (!pm.IsEnabled ()) {
					continue;
				}
				
				//int inputCaps, passthroughCaps, generateCaps;
				//PathModifierUtil.GetPathModifierCaps(pm.GetType(), out inputCaps, out passthroughCaps, out generateCaps);
				//int inputCaps, passthroughCaps, generateCaps;
				IPathInfo pathInfo = pathData.GetPathInfo ();
				PathModifierContext pmc = 
					new PathModifierContext (pathInfo, container, combinedOutputFlags, pmParams);
				// TODO could we use pm.GetOutputFlags(pmc) in here?
				combinedOutputFlags = ((pm.GetProcessFlags (pmc) | pm.GetPassthroughFlags (pmc)) & combinedOutputFlags) | pm.GetGenerateFlags (pmc);
			}
			return combinedOutputFlags;
		}

		static void DoDrawPathModifierInspector (PathModifierEditorContext context, bool skippedDueToErrors)
		{
			IPathModifier pm = context.PathModifier;
			ContextEditorPrefs prefs = context.ContextEditorPrefs;

			if (!prefs.HasKey ("Visible")) {
				prefs.SetBool ("Visible", true);
			}
			string pmTitle = PathModifierUtil.GetDisplayName (pm);
			if (!pm.IsEnabled ()) {
				pmTitle += " [disabled]";
			}

			if (context.PathModifierContext.Errors.Count > 0) {
				pmTitle += " [*** ERROR ***]";
			} else if (skippedDueToErrors && pm.IsEnabled ()) {
				pmTitle += " [*** skipped due to previous errors ***]";
			}

			bool itemVisible = EditorGUILayout.Foldout (prefs.GetBool ("Visible"), pmTitle);
			prefs.SetBool ("Visible", itemVisible);
			if (itemVisible) {
//				EditorGUI.indentLevel++;


				IPathModifierEditor pme = GetEditorForPathModifier (pm, context);


//				TypedCustomToolEditorPrefs pmPrefs = new PrefixCustomToolEditorPrefs (prefs, "[" + index + "].");
//
//				PathModifierEditorContext pmeCtx = new PathModifierEditorContext (
//                    dc.context.PathData, null, pm, dc.context.Path, dc.context.EditorHost, dc.context.TargetModified, pmPrefs);
//
				pme.DrawInspectorGUI (context);

				EditorGUILayout.Separator ();
//				EditorGUI.indentLevel--;
			}
                

		}

		public static IPathModifierEditor GetEditorForPathModifier (IPathModifier pm, PathModifierEditorContext context)
		{
			IPathModifierEditor pme;
			
			ICUstomToolEditorHost cteh = context.EditorHost as ICUstomToolEditorHost;
			if (null != cteh) {
				ICustomToolEditor cte = cteh.GetEditorFor (pm);
				pme = cte as IPathModifierEditor;
				if (null == pme && null != cte) {
					Debug.LogWarning ("Failed to get IPathModifierEditor for '" + pm + "' from ICustomToolEditorHost: " + cteh);
				}
			} else {
				pme = null;
			}
			
			if (null == pme) {
				pme = PathModifierResolver.Instance.CreateToolEditorInstance (pm) as IPathModifierEditor;
				if (null == pme) {
					Debug.LogWarning ("No IPathModifierEditor found for PathModifier '" + pm + "'; using FallbackPathModifierEditor.");
					pme = new FallbackPathModifierEditor ();
				}
				if (null != cteh) {
					cteh.SetEditorFor (pm, pme);
				}
			}
			return pme;
		}

		public static PathModifierFunction NextPathModifierFunction (PathModifierFunction f)
		{
			PathModifierFunction[] values = (PathModifierFunction[])Enum.GetValues (typeof(PathModifierFunction));
			return NextPathModifierFunction (f, values);
		}

		public static PathModifierFunction NextPathModifierFunction (PathModifierFunction f, PathModifierFunction[] allowedFunctions)
		{
			int currentIndex = 0;
			for (int i = 0; i < allowedFunctions.Length; i++) {
				if (f == allowedFunctions [i]) {
					currentIndex = i;
					break;
				}
			}
			int nextIndex = (currentIndex < allowedFunctions.Length - 1) ? currentIndex + 1 : 0;
			return allowedFunctions [nextIndex];
		}

		public static Texture2D GetEmptyProcessImage ()
		{
			return (Texture2D)Resources.Load ("btn-empty", typeof(Texture2D));
		}

		public static Texture2D GetProcessImage (PathModifierFunction pmFunction)
		{
			Texture2D img;
			switch (pmFunction) {
			case PathModifierFunction.Process:
				img = (Texture2D)Resources.Load ("btn-pm-process", typeof(Texture2D));
				break;
			case PathModifierFunction.Passthrough:
				img = (Texture2D)Resources.Load ("btn-pm-passthrough", typeof(Texture2D));
				break;
			case PathModifierFunction.Generate:
				img = (Texture2D)Resources.Load ("btn-pm-generate", typeof(Texture2D));
				break;
			case PathModifierFunction.Remove:
				img = (Texture2D)Resources.Load ("btn-pm-delete", typeof(Texture2D));
				break;
			default:
				img = GetEmptyProcessImage ();
				break;
			}
			return img;
		}

		public static Texture2D GetProcessImage (int componentType, int inputFlags, int outputFlags, int processFlags, int generateFlags, int passthroughFlags)
		{

			PathModifierFunction pmFunc;
			bool unknownFunction = false;

			if (PathPoint.IsFlag (outputFlags, componentType)) {
				// dist0 is part of output...
				if (PathPoint.IsFlag (inputFlags, componentType)) {
					// Also part of input...
					if (PathPoint.IsFlag (processFlags, componentType)) {
						// Process
						pmFunc = PathModifierFunction.Process;
					} else if (PathPoint.IsFlag (generateFlags, componentType)) {
						// Generate
						pmFunc = PathModifierFunction.Generate;
					} else if (PathPoint.IsFlag (passthroughFlags, componentType)) {
						// is passthrough
						pmFunc = PathModifierFunction.Passthrough;
					} else {
						// none of them, hmmmm.... maybe process then!
						pmFunc = PathModifierFunction.Process;
					}
				} else {
					// not part of input
					pmFunc = PathModifierFunction.Generate;
				}
			} else if (PathPoint.IsFlag (inputFlags, componentType)) {
				// not part of output but is part of input
				pmFunc = PathModifierFunction.Remove;
			} else {
				// Not part of input, not part of output
				pmFunc = PathModifierFunction.Passthrough; 
				unknownFunction = true;
			}
			Texture2D img = unknownFunction ? GetEmptyProcessImage () : GetProcessImage (pmFunc);
			return img;
		}

		public static Texture2D GetProcessImage (int componentType, IPathModifier pm, PathModifierContext pmc)
		{

			return GetProcessImage (componentType, pmc.InputFlags, 
                                   pm.GetOutputFlags (pmc), 
                                   pm.GetProcessFlags (pmc), 
                                   pm.GetGenerateFlags (pmc), 
                                   pm.GetPassthroughFlags (pmc));

		}
//
//      public static string PathPointFlagsString(int flags) {
//          string s = "";
//          if ((flags & PathPoint.POSITION) == PathPoint.POSITION) {
//              s += "Pos";
//          } else {
//              s += "---";
//          }
//          s += ", ";
//
//          if ((flags & PathPoint.DIRECTION) == PathPoint.DIRECTION) {
//              s += "Dir";
//          } else {
//              s += "---";
//          }
//          s += ", ";
//
//          if ((flags & PathPoint.DISTANCE_FROM_PREVIOUS) == PathPoint.DISTANCE_FROM_PREVIOUS) {
//              s += "D-1";
//          } else {
//              s += "---";
//          }
//          s += ", ";
//
//          if ((flags & PathPoint.DISTANCE_FROM_BEGIN) == PathPoint.DISTANCE_FROM_BEGIN) {
//              s += "D_0";
//          } else {
//              s += "---";
//          }
//          return s;
//
//      }
	}
    
}
