// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

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
	[CustomToolEditor(typeof(IPathModifier))]
	public class FallbackPathModifierEditor : AbstractPathModifierEditor
	{
		protected override void OnDrawConfigurationGUI ()
		{
			DrawDefaultConfigurationGUI ();
		}
	}
	// TODO refactor the editor system:
	//  FooEditorBinding : UnityEditor.Editor
	//   + fooEditor : FooEditor
	//
	//  - The FooEditorBinding class implemetns UnityEditor.Editor
	//  - FooEditor implements IPathModifiedEditor
	//  - This makes the code base cleaner!

	public abstract class AbstractPathModifierEditor : IPathModifierEditor
	{
		protected PathModifierEditorContext context;
		protected ContextEditorPrefs editorPrefs;

		private bool messagesVisible = false;
		private bool newMessages = false;

		protected AbstractPathModifierEditor ()
		{
		}

		public void DrawInspectorGUI (CustomToolEditorContext context)
		{
			DrawInspectorGUI ((PathModifierEditorContext)context);
		}

		private bool _inDrawInspectorGUI;
		public void DrawInspectorGUI (PathModifierEditorContext context)
		{
			if (!_inDrawInspectorGUI) {
				
				try {
					_inDrawInspectorGUI = true;
					this.context = context;
					editorPrefs = context.ContextEditorPrefs;
					OnDrawInspectorGUI ();
				} finally {
					_inDrawInspectorGUI = false;
				}
			} else {
				Debug.LogWarning ("DrawInspectorGUI called from OnDrawInspectorGUI. Ignoring to prevent infinite loop.");
			}

		}
		protected virtual void OnDrawInspectorGUI ()
		{
			DrawDefaultInspectorGUI ();
		}

		protected abstract void OnDrawConfigurationGUI ();
		
		protected virtual void OnDrawHeader ()
		{
			DrawDefaultHeader ();
		}
		protected virtual void OnDrawInputFilters ()
		{
			DrawDefaultInputFilters ();
		}
		protected virtual void OnDrawProcessMatrix ()
		{
			DrawDefaultProcessMatrix ();
		}
		
		protected virtual void OnDrawCommonInspector ()
		{
			DrawDefaultCommonInspector ();
		}
		protected virtual void OnDrawNotes ()
		{
			DrawDefaultNotes ();
		}

		private void DrawConfigurationGUI ()
		{
			OnDrawConfigurationGUI ();
		}

		private void DrawHeader ()
		{
			OnDrawHeader ();
		}
		private void DrawInputFilters ()
		{
			OnDrawInputFilters ();
		}
		private void DrawProcessMatrix ()
		{
			OnDrawProcessMatrix ();
		}

		private void DrawCommonInspector ()
		{
			OnDrawCommonInspector ();
		}
		private void DrawNotes ()
		{
			OnDrawNotes ();
		}

		protected void DrawDefaultInspectorGUI ()
		{
//			Vertical (() => {
			//GUILayout.Toolbar(0, new string[] {"Move Up", "Move Down"});
				
			DrawHeader ();

			DrawCommonInspector ();

			DrawInputFilters ();

			// Draw process matrix (input --> process --> output)
			DrawProcessMatrix ();

			DrawConfigurationGUI ();

			DrawNotes ();
				
//			}/*, GUI.skin.label*/);


		}

		protected void DrawDefaultConfigurationGUI ()
		{
			FallbackCustomToolEditor.DoDrawInspectorGUI (context);
		}

		protected void DrawDefaultHeader ()
		{
			
			IPathModifier pm = context.PathModifier;
			IPathData pathData = context.PathData;

			PathModifierContext pmContext = context.PathModifierContext;

			List<string> errors = pmContext.Errors;
			List<string> warnings = pmContext.Warnings;
			List<string> info = pmContext.Info;


			string messagesLabel = "Messages";
			string messagesInfo = "";
			if (errors.Count > 0) {
				messagesInfo += string.Format ("{0} Errors", errors.Count);
			}
			if (warnings.Count > 0) {
				if (messagesInfo.Length > 0) {
					messagesInfo += ", ";
				}
				messagesInfo += string.Format ("{0} Warnings", warnings.Count);
			}
			if (info.Count > 0) {
				if (messagesInfo.Length > 0) {
					messagesInfo += ", ";
				}
				messagesInfo += string.Format ("{0} Info", info.Count);
			}
			if (messagesInfo.Length > 0) {
				messagesLabel += " (" + messagesInfo + ")";
			}

			if (messagesInfo.Length > 0) {
				if (newMessages) {
					messagesVisible = true;
					newMessages = false;
				}

				EditorGUI.indentLevel++;
				messagesVisible = EditorGUILayout.Foldout (messagesVisible, messagesLabel);
				if (messagesVisible) {
					if (errors.Count > 0) {

						string errorsString = "";
						foreach (string errorMsg in errors) {
							errorsString += "\n- " + errorMsg;
						}
						EditorGUILayout.HelpBox ("*** ERROR ***: Processing of PathModifier was aborted due to error(s): " + errorsString, MessageType.Error, true);
					}
					if (warnings.Count > 0) {
						
						string warningsString = "";
						foreach (string warningMsg in warnings) {
							warningsString += "\n- " + warningMsg;
						}
						EditorGUILayout.HelpBox ("Warning: " + warningsString, MessageType.Warning, true);
					}
					if (info.Count > 0) {
						
						string infoString = "";
						foreach (string msg in info) {
							infoString += "\n- " + msg;
						}
						EditorGUILayout.HelpBox (infoString, MessageType.Info, true);
					}
				}
				EditorGUI.indentLevel--;
			}


			EditorGUI.BeginChangeCheck ();
			string displayName = PathModifierUtil.GetDisplayName (pm);
				
			pm.SetEnabled (EditorGUILayout.ToggleLeft (displayName, pm.IsEnabled ()));
			if (EditorGUI.EndChangeCheck ()) {
				if (null != context.Target) {
					EditorUtility.SetDirty (context.Target);
				}
				context.TargetModified ();
			}
			if (true)
				EditorLayout.Horizontal.WithOptions (GUILayout.ExpandWidth (false)).Draw (() =>
				{
					IPathModifierContainer pmc = pathData.GetPathModifierContainer ();
					int pmIndex = pmc.IndexOf (pm);
					int pmCount = pmc.GetPathModifiers ().Length;


					EditorGUILayout.PrefixLabel ("Actions");

					if (pmc.IsSupportsApplyPathModifier ()) {

						EditorGUI.BeginDisabledGroup (pmIndex < 0 || !pm.IsEnabled () || !pmc.IsSupportsApplyPathModifier () || (pm is NewPathModifier));
						if (GUILayout.Button ("Apply", EditorStyles.miniButton)) {
							if (EditorUtility.DisplayDialog ("Apply", "Do you want to Apply this modifier AND all the modifiers in the chain before it?", "Yes", "No")) {
							
								Undo.RecordObject (context.Target, "Apply Path Modifiers");
								pmc.ApplyPathModifier (pmIndex);
							
								EditorUtility.SetDirty (context.Target);
							}
						}
						EditorGUI.EndDisabledGroup ();
					}
				
					EditorGUILayout.Separator ();
				
					EditorGUI.BeginDisabledGroup (pmIndex <= 0);
					{
						if (GUILayout.Button ("Up", EditorStyles.miniButtonLeft, GUILayout.ExpandWidth (false))) {
							pmc.RemovePathModifer (pmIndex);
							pmc.InsertPathModifer (pmIndex - 1, pm);
							// TODO reimplement following:
							//						visibilityPrefs.SetBool (pmIndex, visibilityPrefs.GetBool (pmIndex - 1));
							//						visibilityPrefs.SetBool (pmIndex - 1, true);
							if (null != context.Target) {
								EditorUtility.SetDirty (context.Target);
							}
							context.TargetModified ();
						}
					}
					EditorGUI.EndDisabledGroup ();
				
					EditorGUI.BeginDisabledGroup (pmIndex < 0 || pmIndex == pmCount - 1);
					{
						if (GUILayout.Button ("Down", EditorStyles.miniButtonMid, GUILayout.ExpandWidth (false))) {
							pmc.RemovePathModifer (pmIndex);
							pmc.InsertPathModifer (pmIndex + 1, pm);
							// TODO reimplement following:
							//						visibilityPrefs.SetBool (pmIndex, visibilityPrefs.GetBool (pmIndex + 1));
							//						visibilityPrefs.SetBool (pmIndex + 1, true);
							if (null != context.Target) {
								EditorUtility.SetDirty (context.Target);
							}
						
							context.TargetModified ();
						}
					}
					EditorGUI.EndDisabledGroup ();
					if (GUILayout.Button (((pm is NewPathModifier) ? "Cancel" : "Remove"), EditorStyles.miniButtonRight, GUILayout.ExpandWidth (false))) {
						pmc.RemovePathModifer (pmIndex);
						if (null != context.Target) {
							EditorUtility.SetDirty (context.Target);
						}
						context.TargetModified ();
					}
				});

			EditorGUILayout.Separator ();
			
//			string name = PathModifierUtil.GetDisplayName (pm);
//			EditorGUI.BeginChangeCheck ();
//			name = EditorGUILayout.TextField ("Name", name);
//			if (EditorGUI.EndChangeCheck ()) {
//				if (name == pm.GetName ()) {
//					name = null;
//				}
//				pm.SetInstanceName (name);
//				EditorUtility.SetDirty (context.Target);
//				context.TargetModified ();
//			}
//			
//			EditorGUILayout.LabelField ("Type", pm.GetName () + " (" + pm.GetType ().FullName + ")");
//
		}

		protected void DrawDefaultInputFilters ()
		{
			IPathModifier pm = context.PathModifier;
			
			if (pm is IPathModifierInputFilterSupport) {
				IPathModifierInputFilterSupport ifs = (IPathModifierInputFilterSupport)pm;
				PathModifierInputFilter f = ifs.GetInputFilter ();
				
				if (null != f) {
					EditorGUILayout.LabelField ("Input Filter", f.GetType ().Name);
					// TODO CACHE THE EDITOR INSTANCE!!!

					ICustomToolEditor editor = (ICustomToolEditor)PathModifierInputFilterResolver.Instance.CreateToolEditorInstance (f);
					if (null != editor) {
						bool configVisible = editorPrefs.GetBool ("InputFilterConfigVisible", false);
						configVisible = EditorGUILayout.Foldout (configVisible, "Filter Configuration");
						editorPrefs.SetBool ("InputFilterConfigVisible", configVisible);
						if (configVisible) {
							PathModifierInputFilterEditorContext ctx = 
								new PathModifierInputFilterEditorContext (f, context);
							EditorLayout.Indent (() => editor.DrawInspectorGUI (ctx));
						}
					}
				}
			}
		}

		protected void DrawDefaultProcessMatrix ()
		{
			DoDrawMasks (true, true, true);
		}

		protected void DrawDefaultCommonInspector ()
		{
			IPathModifier pm = context.PathModifier;
			EditorGUILayout.LabelField ("Type", pm.GetName () + " (" + pm.GetType ().FullName + ")");

			string name = PathModifierUtil.GetDisplayName (pm);
			EditorGUI.BeginChangeCheck ();
			name = EditorGUILayout.TextField ("Name", name);
			if (EditorGUI.EndChangeCheck ()) {
				if (name == pm.GetName ()) {
					name = null;
				}
				pm.SetInstanceName (name);
				EditorUtility.SetDirty (context.Target);
				context.TargetModified ();
			}

		}
		protected void DrawDefaultNotes ()
		{
			IPathModifier pm = context.PathModifier;

			bool notesVisible = editorPrefs.GetBool ("NotesEditing", false);
			EditorGUI.indentLevel++;
			notesVisible = EditorGUILayout.Foldout (notesVisible, "Notes");
			editorPrefs.SetBool ("NotesEditing", notesVisible);
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
					EditorUtility.SetDirty (context.Target);
				}
			}
			EditorGUI.indentLevel--;
		}
		protected virtual void DoDrawMasks (bool input, bool process, bool output)
		{
//			IPathModifierContainer pmc = context.PathModifierContainer;
			IPathModifier pm = context.PathModifier;
			PathModifierContext pmContext = context.PathModifierContext;
			
			bool drawDisabledAsPassthrough = true;
			
			int outputFlags;
			if (pm.IsEnabled () || !drawDisabledAsPassthrough) {
				outputFlags = pm.GetOutputFlags (pmContext);
			} else {
				outputFlags = pmContext.InputFlags;
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
				PathEditor.DrawPathPointMask ("Input", pmContext.InputFlags);
			}
			
			if (process) {
				EditorLayout.Horizontal.Draw (() => {
					EditorGUILayout.PrefixLabel ("Process");
					if (pm.IsEnabled () || !drawDisabledAsPassthrough) {
						GUILayout.Button (PathModifierEditorUtil.GetProcessImage (PathPoint.POSITION, pm, pmContext), boxStyle, boxOptions);
					
						// TODO should this be implemented? If yes, copy to all functions!
						if (GUILayout.Button (PathModifierEditorUtil.GetProcessImage (PathPoint.DIRECTION, pm, pmContext), boxStyle, boxOptions)) {
							if (pm is ConfigurableProcessPathModifier) {
								ConfigurableProcessPathModifier cppm = (ConfigurableProcessPathModifier)pm;
								PathModifierFunction f = cppm.DirectionFunction;
								cppm.DirectionFunction = PathModifierEditorUtil.NextPathModifierFunction (f, cppm.AllowedDirectionFunctions);
								// TODO context.targetmodified!
							}
						
						}
						GUILayout.Button (PathModifierEditorUtil.GetProcessImage (PathPoint.UP, pm, pmContext), boxStyle, boxOptions);
						GUILayout.Button (PathModifierEditorUtil.GetProcessImage (PathPoint.ANGLE, pm, pmContext), boxStyle, boxOptions);
						GUILayout.Button (PathModifierEditorUtil.GetProcessImage (PathPoint.DISTANCE_FROM_PREVIOUS, pm, pmContext), boxStyle, boxOptions);
						if (GUILayout.Button (PathModifierEditorUtil.GetProcessImage (PathPoint.DISTANCE_FROM_BEGIN, pm, pmContext), boxStyle, boxOptions)) {
						
						}
					} else {
						GUILayout.Button (PathModifierEditorUtil.GetProcessImage (PathPoint.POSITION, pmContext.InputFlags, outputFlags, 0, 0, outputFlags), boxStyle, boxOptions);
						GUILayout.Button (PathModifierEditorUtil.GetProcessImage (PathPoint.DIRECTION, pmContext.InputFlags, outputFlags, 0, 0, outputFlags), boxStyle, boxOptions);
						GUILayout.Button (PathModifierEditorUtil.GetProcessImage (PathPoint.UP, pmContext.InputFlags, outputFlags, 0, 0, outputFlags), boxStyle, boxOptions);
						GUILayout.Button (PathModifierEditorUtil.GetProcessImage (PathPoint.ANGLE, pmContext.InputFlags, outputFlags, 0, 0, outputFlags), boxStyle, boxOptions);
						GUILayout.Button (PathModifierEditorUtil.GetProcessImage (PathPoint.DISTANCE_FROM_PREVIOUS, pmContext.InputFlags, outputFlags, 0, 0, outputFlags), boxStyle, boxOptions);
						GUILayout.Button (PathModifierEditorUtil.GetProcessImage (PathPoint.DISTANCE_FROM_BEGIN, pmContext.InputFlags, outputFlags, 0, 0, outputFlags), boxStyle, boxOptions);
					
					}
				});
			}
			
			if (output) {
				PathEditor.DrawPathPointMask ("Output", outputFlags);
			}
		}


	}

}
