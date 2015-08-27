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

		public static void DrawPathModifiersInspector (Path path, UnityEditor.Editor editor, UnityEngine.Object dirtyObject, CustomToolEditorContext.TargetModifiedFunc modifiedCallback)
		{
            
			Util.ParameterStore store = path.GetParameterStore ();
			CustomToolEditorPrefs prefs = new ParameterStoreCustomToolEditorPrefs (store);
            
			// Path Modifiers
			PathModifierEditorContext context = new PathModifierEditorContext (path.GetPathModifierContainer (), null, path, editor, modifiedCallback, prefs);
            
			DrawPathModifiersInspector (context, dirtyObject);
		}

		public static void DrawPathModifiersInspector (PathModifierEditorContext context, UnityEngine.Object dirtyObject)
		{


			IPathModifierContainer container = context.PathModifierContainer;
			IPathModifier[] pathModifiers = container.GetPathModifiers ();

			PrefixCustomToolEditorPrefs visibilityPrefs = new PrefixCustomToolEditorPrefs (
                context.CustomToolEditorPrefs, "PathModifier[", "].Visible");

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

			bool pathModifiersVisible = EditorGUILayout.Foldout (
                visibilityPrefs.GetBool (-1, true), "Path Modifiers (" + enabledPathModifierCount + "/" + totalPathModifierCount + ")");
			if (EditorGUI.EndChangeCheck ()) {
				visibilityPrefs.SetBool (-1, pathModifiersVisible);
                
			}

			ParameterStore pmParams = new ParameterStore ();
			if (pathModifiersVisible) {
				EditorGUI.indentLevel++;
				int currentInputFlags = context.Path.GetOutputFlagsBeforeModifiers ();


				for (int i = 0; i < pathModifiers.Length; i++) {
					IPathModifier pm = pathModifiers [i];

					IPathInfo pathInfo = context.Path.GetPathInfo ();
					PathModifierContext pmc = 
						new PathModifierContext (pathInfo, container, currentInputFlags, pmParams);
					if (pm.IsEnabled ()) {
						// TODO could we use pm.GetOutputFlags(pmc) in here?
						currentInputFlags = ((pm.GetPassthroughFlags (pmc) | pm.GetProcessFlags (pmc)) & currentInputFlags) | pm.GetGenerateFlags (pmc);
					}
					DrawContext dc = new DrawContext (pm, container, i, pathModifiers.Length, visibilityPrefs, context, dirtyObject);
					DoDrawPathModifierInspector (dc, pmc);
				}
				EditorGUI.indentLevel--;
			}
            
			//if (GUILayout.Button("Add Path Modifier")) {
			//  AddPathModifier();
			//}



			// Calculate combined output of all PM's:
			bool hasNewPathModifier = false;
			int combinedOutputFlags = context.Path.GetOutputFlags ();
			pmParams = new ParameterStore ();
			for (int i = 0; i < pathModifiers.Length; i++) {
				IPathModifier pm = pathModifiers [i];
				if (pm is NewPathModifier) {
					hasNewPathModifier = true;
					continue;
				}
				if (!pm.IsEnabled ()) {
					continue;
				}

				//int inputCaps, passthroughCaps, generateCaps;
				//PathModifierUtil.GetPathModifierCaps(pm.GetType(), out inputCaps, out passthroughCaps, out generateCaps);
				//int inputCaps, passthroughCaps, generateCaps;
				IPathInfo pathInfo = context.Path.GetPathInfo ();
				PathModifierContext pmc = 
					new PathModifierContext (pathInfo, container, combinedOutputFlags, pmParams);
				// TODO could we use pm.GetOutputFlags(pmc) in here?
				combinedOutputFlags = ((pm.GetProcessFlags (pmc) | pm.GetPassthroughFlags (pmc)) & combinedOutputFlags) | pm.GetGenerateFlags (pmc);
			}


			if (!hasNewPathModifier) {

				if (GUILayout.Button ("Add Path Modifier")) {
					// TODO don't add if we already have one!
					// Add the "New" item:
					container.AddPathModifer (new NewPathModifier ());
					// Fetch new list of PathModifiers: (see #2)
					pathModifiers = container.GetPathModifiers ();

					// The "new" item is visible by default
					visibilityPrefs.SetBool (pathModifiers.Length, true);
				}
			}

			// TODO use RED if some flag is missing!
			PathEditor.DrawPathPointMask ("Output Caps", combinedOutputFlags);

			EditorGUILayout.Separator ();
		}
		private class DrawContext
		{
			internal IPathModifier pm;
			internal IPathModifierContainer container;
			internal int index;
			internal int pmCount;
			internal TypedCustomToolEditorPrefs visibilityPrefs;
			internal PathModifierEditorContext context;
			internal UnityEngine.Object dirtyObject;

			public DrawContext (IPathModifier pm, IPathModifierContainer container, int index, int pmCount, TypedCustomToolEditorPrefs visibilityPrefs, PathModifierEditorContext context, UnityEngine.Object dirtyObject)
			{
				this.pm = pm;
				this.container = container;
				this.index = index;
				this.pmCount = pmCount;
				this.visibilityPrefs = visibilityPrefs;
				this.context = context;
				this.dirtyObject = dirtyObject;
			}
            
		}
		static void DoDrawPathModifierInspector (DrawContext dc, PathModifierContext pmc)
		{
			TypedCustomToolEditorPrefs visibilityPrefs = dc.visibilityPrefs;
			int index = dc.index;
			IPathModifier pm = dc.pm;


			if (!visibilityPrefs.ContainsKey (index)) {
				visibilityPrefs.SetBool (index, true);
			}
			string pmTitle = GetPathModifierDisplayName (pm); 
			if (!pm.IsEnabled ()) {
				pmTitle += " (disabled)";
			}
			bool itemVisible = EditorGUILayout.Foldout (visibilityPrefs.GetBool (index), pmTitle);

			visibilityPrefs.SetBool (index, itemVisible);
			if (itemVisible) {
				EditorGUI.indentLevel++;

				EditorGUILayout.BeginVertical (GUI.skin.label);
				{
					//GUILayout.Toolbar(0, new string[] {"Move Up", "Move Down"});
                    
					DrawActions (dc);

					EditorGUILayout.Separator ();

					DrawInputFilters (dc);

//                  DrawMasks(pm, pmc, true, false, false);

					DrawMasks (pm, pmc, true, true, true);

					DrawCommonInspector (dc, pmc);

					IPathModifierEditor pme = (IPathModifierEditor)PathModifierResolver.Instance.CreateToolEditorInstance (pm);
					PathModifierEditorContext pmeCtx = new PathModifierEditorContext (
                        dc.context.PathModifierContainer, pm, dc.context.Path, dc.context.EditorHost, dc.context.TargetModified);
					if (null != pme) {
						pme.DrawInspectorGUI (pmeCtx);
					} else {
						new FallbackCustomToolEditor ().DrawInspectorGUI (pmeCtx);
					}
					//                      IPathModifierEditor pme = PathModifierEditorUtil.FindToolEditorType();
					//                      if (null != pme) {
					//                          pme.DrawInspectorGUI(new PathModifierEditorContext(pm, track.Path, trackInspector));
					//                      }

//                  DrawMasks(pm, pmc, false, true, true);

					string editingNotesPrefKey = index + ".NotesEditing";
					bool notesVisible = visibilityPrefs.GetBool (editingNotesPrefKey, false);
					notesVisible = EditorGUILayout.Foldout (notesVisible, "Notes");
					visibilityPrefs.SetBool (editingNotesPrefKey, notesVisible);
					if (notesVisible) {
						if (!StringUtil.IsEmpty (pm.GetDescription ())) {
							EditorGUILayout.HelpBox (pm.GetDescription (), MessageType.Info);
						}

						EditorGUI.BeginChangeCheck ();

						string notes = StringUtil.Trim (pm.GetInstanceDescription ());
//                        EditorGUILayout.PrefixLabel("Notes");
						notes = EditorGUILayout.TextArea (notes);
						if (EditorGUI.EndChangeCheck ()) {
							pm.SetInstanceDescription (notes);
							EditorUtility.SetDirty (dc.dirtyObject);
						}
					}


					EditorGUILayout.Separator ();
				}

				EditorGUILayout.EndVertical ();
                
				EditorGUILayout.Separator ();
				EditorGUI.indentLevel--;
			}
                

		}

		static string GetPathModifierDisplayName (IPathModifier pm)
		{
			string n = pm.GetInstanceName ();
			if (StringUtil.IsEmpty (n)) {
				n = pm.GetName ();
			}
			return n;
		}

		static void DrawInputFilters (DrawContext dc)
		{
//			public CustomToolResolver (FindTypesFunc toolTypesFinder, FindTypesFunc editorTypesFinder, 
//			                           ToolEditorMatcherFunc toolEditorMatcher, DisplayNameResolverFunc displayNameResolver;

			IPathModifierContainer container = dc.container;
			IPathModifier pm = dc.pm;
			int index = dc.index;
			int pmCount = dc.pmCount;
			TypedCustomToolEditorPrefs visibilityPrefs = dc.visibilityPrefs;
			UnityEngine.Object dirtyObject = dc.dirtyObject;

			if (pm is IPathModifierInputFilterSupport) {
				IPathModifierInputFilterSupport ifs = (IPathModifierInputFilterSupport)pm;
				PathModifierInputFilter f = ifs.GetInputFilter ();

				if (null != f) {
					EditorGUILayout.LabelField ("Input Filter", f.GetType ().Name);
					ICustomToolEditor editor = (ICustomToolEditor)PathModifierInputFilterResolver.Instance.CreateToolEditorInstance (f);
					if (null != editor) {
						string prefKey = dc.index + ".InputFilterConfigVisible";
						bool configVisible = visibilityPrefs.GetBool (prefKey, false);
						configVisible = EditorGUILayout.Foldout (configVisible, "Filter Configuration");
						visibilityPrefs.SetBool (prefKey, configVisible);
						if (configVisible) {

							CustomToolEditorContext ctx = 
								new CustomToolEditorContext (f, dc.context.Target, dc.context.EditorHost, dc.context.TargetModified);
							EditorGUI.indentLevel++;
							editor.DrawInspectorGUI (ctx);
							EditorGUI.indentLevel--;
						}
					}
				}
			}
		}

		static void DrawActions (DrawContext dc)
		{
			IPathModifierContainer container = dc.container;
			IPathModifier pm = dc.pm;
			int index = dc.index;
			int pmCount = dc.pmCount;
			TypedCustomToolEditorPrefs visibilityPrefs = dc.visibilityPrefs;
			UnityEngine.Object dirtyObject = dc.dirtyObject;


			EditorGUILayout.BeginHorizontal ();
			{
				//EditorGUILayout.LabelField(GetPathModifierDisplayName(pm)); // TODO or GetInstanceName() ?

//            pm.SetInstanceName(EditorGUILayout.TextField(pm.GetInstanceName()));
				EditorGUI.BeginChangeCheck ();
				pm.SetEnabled (EditorGUILayout.ToggleLeft (GetPathModifierDisplayName (pm), pm.IsEnabled ()));
				if (EditorGUI.EndChangeCheck ()) {
					if (null != dc.dirtyObject) {
						EditorUtility.SetDirty (dc.dirtyObject);
					}
					dc.context.TargetModified ();
				}

				EditorGUI.BeginDisabledGroup (!pm.IsEnabled () || !dc.container.IsSupportsApplyPathModifier () || (pm is NewPathModifier));
				if (GUILayout.Button ("Apply")) {
					if (EditorUtility.DisplayDialog ("Apply", "Do you want to Apply this modifier AND all the modifiers in the chain before it?", "Yes", "No")) {
                        
						Undo.RecordObject (dc.dirtyObject, "Apply Path Modifiers");
						dc.container.ApplyPathModifier (dc.index);
                        
						EditorUtility.SetDirty (dc.dirtyObject);
					}
				}
				EditorGUI.EndDisabledGroup ();

				EditorGUILayout.Separator ();

				EditorGUI.BeginDisabledGroup (index == 0);
				{
					if (GUILayout.Button ("Up")) {
						container.RemovePathModifer (index);
						container.InsertPathModifer (index - 1, pm);
						visibilityPrefs.SetBool (index, visibilityPrefs.GetBool (index - 1));
						visibilityPrefs.SetBool (index - 1, true);
						if (null != dirtyObject) {
							EditorUtility.SetDirty (dirtyObject);
						}
						dc.context.TargetModified ();
					}
				}
				EditorGUI.EndDisabledGroup ();
                
				EditorGUI.BeginDisabledGroup (index == pmCount - 1);
				{
					if (GUILayout.Button ("Down")) {
						container.RemovePathModifer (index);
						container.InsertPathModifer (index + 1, pm);
						visibilityPrefs.SetBool (index, visibilityPrefs.GetBool (index + 1));
						visibilityPrefs.SetBool (index + 1, true);
						if (null != dirtyObject) {
							EditorUtility.SetDirty (dirtyObject);
						}
                        
						dc.context.TargetModified ();
					}
				}
				EditorGUI.EndDisabledGroup ();
                
				if (GUILayout.Button ((pm is NewPathModifier) ? "Cancel" : "Remove")) {
					container.RemovePathModifer (index);
					if (null != dirtyObject) {
						EditorUtility.SetDirty (dirtyObject);
					}
					dc.context.TargetModified ();
				}
			}
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.Separator ();

			string name = StringUtil.Trim (GetPathModifierDisplayName (pm));
			EditorGUI.BeginChangeCheck ();
			name = EditorGUILayout.TextField ("Name", name);
			if (EditorGUI.EndChangeCheck ()) {
				if (name == pm.GetName ()) {
					name = null;
				}
				pm.SetInstanceName (name);
				EditorUtility.SetDirty (dirtyObject);
				dc.context.TargetModified ();
			}

			EditorGUILayout.LabelField ("Type", pm.GetName () + " (" + pm.GetType ().FullName + ")");



//            EditorGUI.BeginChangeCheck();
//            pm.SetEnabled(EditorGUILayout.Toggle("Enabled", pm.IsEnabled()));
//            if (EditorGUI.EndChangeCheck())
//            {
//                if (null != dc.dirtyObject)
//                {
//                    EditorUtility.SetDirty(dc.dirtyObject);
//                }
//                dc.context.TargetModified();
//            }
		}

		static void DrawCommonInspector (DrawContext dc, PathModifierContext pmc)
		{
			IPathModifier pm = dc.pm;
			if (!(pm is NewPathModifier)) {

				// TODO do we really need the "Type" field?
				EditorGUILayout.LabelField ("Type", pm.GetName () + " (" + pm.GetType ().FullName + ")");
                
				//int inputCaps, outputCaps, generateCaps;
				//PathModifierUtil.GetPathModifierCaps(pm.GetType(), out inputCaps, out outputCaps, out generateCaps);
                
				//                          EditorGUILayout.MaskField("input", inputCaps, new string[]{"Pos", "Dir", "Dp", "Db"});


//              EditorGUILayout.LabelField("Requires", PathPointFlagsString(pm.GetRequiredInputFlags()));
//              EditorGUILayout.LabelField("Passthrough", PathPointFlagsString(pm.GetPassthroughFlags(pmc)));
//              EditorGUILayout.LabelField("Generates", PathPointFlagsString(pm.GetGenerateFlags(pmc)));


//


			}
		}

		static void DrawMasks (IPathModifier pm, PathModifierContext pmc, bool input, bool process, bool output)
		{

			bool drawDisabledAsPassthrough = true;

			int outputFlags;
			if (pm.IsEnabled () || !drawDisabledAsPassthrough) {
				outputFlags = pm.GetOutputFlags (pmc);
			} else {
				outputFlags = pmc.InputFlags;
			}
			//              EditorGUILayout.LabelField("Output", PathPointFlagsString(outputFlags));
			//              if (PathPoint.ALL != outputFlags) {
			//                  EditorGUILayout.Toggle("Generate Direction", true);
			//                  EditorGUILayout.Toggle("Generate Dist n-1", true);
			//                  EditorGUILayout.Toggle("Generate Dist 0", true);
			//              }
            
            

			int btnHeight = 24;
			int btnWidth = 24;
            
			// Input
            

//            GUIStyle btnStyle = GUIStyle.none;
			GUIStyle boxStyle = GUIStyle.none;
            
//            GUILayoutOption[] btnOptions = {GUILayout.Width(btnWidth), GUILayout.Height(btnHeight)};
			GUILayoutOption[] boxOptions = {
                GUILayout.Width (btnWidth),
                GUILayout.Height (btnHeight)
            };

			if (input) {
				PathEditor.DrawPathPointMask ("Input", pmc.InputFlags);
			}

			if (process) {
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.PrefixLabel ("Process");
				if (pm.IsEnabled () || !drawDisabledAsPassthrough) {
					GUILayout.Button (GetProcessImage (PathPoint.POSITION, pm, pmc), boxStyle, boxOptions);

					// TODO should this be implemented? If yes, copy to all functions!
					if (GUILayout.Button (GetProcessImage (PathPoint.DIRECTION, pm, pmc), boxStyle, boxOptions)) {
						if (pm is ConfigurableProcessPathModifier) {
							ConfigurableProcessPathModifier cppm = (ConfigurableProcessPathModifier)pm;
							PathModifierFunction f = cppm.DirectionFunction;
							cppm.DirectionFunction = NextPathModifierFunction (f, cppm.AllowedDirectionFunctions);
							// TODO context.targetmodified!
						}

					}
					GUILayout.Button (GetProcessImage (PathPoint.UP, pm, pmc), boxStyle, boxOptions);
					GUILayout.Button (GetProcessImage (PathPoint.ANGLE, pm, pmc), boxStyle, boxOptions);
					GUILayout.Button (GetProcessImage (PathPoint.DISTANCE_FROM_PREVIOUS, pm, pmc), boxStyle, boxOptions);
					if (GUILayout.Button (GetProcessImage (PathPoint.DISTANCE_FROM_BEGIN, pm, pmc), boxStyle, boxOptions)) {
                        
					}
				} else {
					GUILayout.Button (GetProcessImage (PathPoint.POSITION, pmc.InputFlags, outputFlags, 0, 0, outputFlags), boxStyle, boxOptions);
					GUILayout.Button (GetProcessImage (PathPoint.DIRECTION, pmc.InputFlags, outputFlags, 0, 0, outputFlags), boxStyle, boxOptions);
					GUILayout.Button (GetProcessImage (PathPoint.UP, pmc.InputFlags, outputFlags, 0, 0, outputFlags), boxStyle, boxOptions);
					GUILayout.Button (GetProcessImage (PathPoint.ANGLE, pmc.InputFlags, outputFlags, 0, 0, outputFlags), boxStyle, boxOptions);
					GUILayout.Button (GetProcessImage (PathPoint.DISTANCE_FROM_PREVIOUS, pmc.InputFlags, outputFlags, 0, 0, outputFlags), boxStyle, boxOptions);
					GUILayout.Button (GetProcessImage (PathPoint.DISTANCE_FROM_BEGIN, pmc.InputFlags, outputFlags, 0, 0, outputFlags), boxStyle, boxOptions);

				}
				EditorGUILayout.EndHorizontal ();
			}

			if (output) {
				PathEditor.DrawPathPointMask ("Output", outputFlags);
			}
		}

		static PathModifierFunction NextPathModifierFunction (PathModifierFunction f)
		{
			PathModifierFunction[] values = (PathModifierFunction[])Enum.GetValues (typeof(PathModifierFunction));
			return NextPathModifierFunction (f, values);
		}

		static PathModifierFunction NextPathModifierFunction (PathModifierFunction f, PathModifierFunction[] allowedFunctions)
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

		static Texture2D GetProcessImage (int componentType, IPathModifier pm, PathModifierContext pmc)
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
