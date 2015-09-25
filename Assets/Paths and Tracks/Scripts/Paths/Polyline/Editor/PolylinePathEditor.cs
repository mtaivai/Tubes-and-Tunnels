using UnityEngine;
using UnityEditor;

using System.Collections;
using System.Collections.Generic;
using System;

using Paths;
using Paths.Editor;
using Util.Editor;

namespace Paths.Polyline.Editor
{
	public class PolylinePathMenu
	{
		[MenuItem("Paths/Create/Create Polyline Path")]
		private static void CreatePolylinePathOption ()
		{
			new GameObject ("Polyline Path", typeof(PolylinePath));
		}

//		[MenuItem("Paths/Create/Create Polyline Path with Track")]
//		private static void CreatePolylinePathWithTrackOption ()
//		{
//			GameObject go = new GameObject ("Polyline Path", typeof(PolylinePath), typeof(Tracks.Track), typeof(MeshFilter), typeof(MeshRenderer));
//			Tracks.Track track = go.GetComponent<Tracks.Track> ();
//			track.Path = go.GetComponent<PolylinePath> ();
//			track.TrackGeneratorType = typeof(Tracks.Tube.TubeGenerator).FullName;
//			track.GenerateTrackMesh ();
//
//		}
//
//		[MenuItem("CONTEXT/PolylinePath/Create Track")]
//		private static void CreateTrackOption (MenuCommand menuCommand)
//		{
//			Path path = (Path)menuCommand.context;
//			GameObject go = path.gameObject;
//			go.AddComponent<Tracks.Track> ();
//
//		}
	}

	[CustomEditor(typeof(PolylinePath))]
	public class PolylinePathEditor : AbstractPathEditor<PolylinePath, PolylinePathData>
	{

//		private GUIStyle labelStyle;

//      private DictionaryEditorItemPrefs pathModifierPrefs = new DictionaryEditorItemPrefs();

		private bool controlPointsVisible = true;
		private bool currentPointVisible = true;
		private bool currentPointWeightsVisible = false;

		private int selectedNewWeightIndex = -1;
//		private int selectedControlPointEditView = 0;
//		private int selectedWeightEditingIndex = -1;

		private GUIStyle selectedPointRowStyle = null;
		private GUIStyle normalPointRowStyle = null;

		private int previousSelectedPointIndex = -1;

		public PolylinePathEditor ()
		{
//			// TODO we should have common / shared styles!
//			labelStyle = new GUIStyle ();
//			Texture2D labelBgTexture = new Texture2D (1, 1, TextureFormat.RGBA32, false);
//			labelBgTexture.SetPixel (0, 0, new Color (0.3f, 0.3f, 0.3f, 0.5f));
//			labelBgTexture.Apply ();
//            
//			labelStyle.alignment = TextAnchor.UpperLeft;
//			labelStyle.normal = new GUIStyleState ();
//			labelStyle.normal.background = labelBgTexture;
//			labelStyle.normal.textColor = Color.white;
		}

		protected override void OnInitInspectorGUI (bool firstTime)
		{
			if (firstTime) {
				normalPointRowStyle = GUIStyle.none;
//				normalPointRowStyle.border = new RectOffset ();

				selectedPointRowStyle = new GUIStyle (GUIStyle.none);
				Texture2D bgTexture = new Texture2D (1, 1, TextureFormat.RGBA32, false);

				// Bluish background
				bgTexture.SetPixel (0, 0, new Color (0.0f, 0.0f, 1.0f, 0.2f));
				bgTexture.Apply ();
				selectedPointRowStyle.normal = new GUIStyleState ();
				selectedPointRowStyle.normal.background = bgTexture;

			}
		}

		PolylinePathData GetPathData ()
		{
			return (PolylinePathData)pathData;
		}
    


		protected override void DrawGeneralInspectorGUI ()
		{
			DrawDefaultGeneralInspectorGUI ();
		}


		protected override void DrawPathPointsInspectorGUI ()
		{
        
//			EditorGUILayout.HelpBox ("Control Points are shared by all data sets having Input Source set to 'Self'.", MessageType.Info);

			const float pointRowLabelWidth = 50f;

			string[] definedWeightIds = WeightEditorUtil.GetDefinedWeightIds (pathData);

			EditorGUI.BeginChangeCheck ();
			pathData.SetLoop (EditorGUILayout.Toggle ("Loop", pathData.IsLoop ()));
			if (EditorGUI.EndChangeCheck ()) {
				Undo.RecordObject (path, "Toggle Path Loop Mode");
				EditorUtility.SetDirty (path);
			}
			int selectedPointIndex = SelectedControlPointIndex;


			// Currently selected point
			currentPointVisible = EditorGUILayout.Foldout (currentPointVisible, "Selected Point");
			if (currentPointVisible) {
				if (SelectedControlPointIndex >= 0) {
					EditorGUI.indentLevel++;

					PathPoint pp = pathData.GetControlPointAtIndex (selectedPointIndex);

					EditorGUI.BeginChangeCheck ();
					selectedPointIndex = EditorGUILayout.IntField ("Index", selectedPointIndex, GUILayout.ExpandWidth (false));
					if (EditorGUI.EndChangeCheck ()) {
						selectedPointIndex = Mathf.Clamp (selectedPointIndex, 0, pathData.GetControlPointCount () - 1);
						SelectedControlPointIndex = selectedPointIndex;
						EditorUtility.SetDirty (target);
					}
					EditorGUI.BeginChangeCheck ();
					pp.Position = EditorGUILayout.Vector3Field ("Position", pp.Position);
					if (EditorGUI.EndChangeCheck ()) {
						pathData.SetControlPointAtIndex (selectedPointIndex, pp);
						EditorUtility.SetDirty (target);

					}

					// Available weights
//					if (pathData.IsPathMetadataSupported ()) {
//						IPathMetadata md = pathData.GetPathMetadata ();
//						int wc = md.GetDefinedWeightCount();
//						for (int i = 0; i < wc; i++) {
//							string wid = md.GetDefin
//						}
//					}
//					pathData.GetPathMetadata ();
					currentPointWeightsVisible = EditorGUILayout.Foldout (currentPointWeightsVisible, "Weights");
					if (currentPointWeightsVisible) {
						EditorGUI.indentLevel++;
						string[] weightIds = pp.GetWeightIds ();
						// TODO Sort by defined weights
						for (int i = 0; i < weightIds.Length; i++) {
							string weightId = weightIds [i];

							EditorGUILayout.BeginHorizontal (GUILayout.ExpandWidth (false));

							WeightDefinition wd = WeightEditorUtil.GetWeightDefinition (pathData, weightId);

							EditorGUI.BeginChangeCheck ();
							float value = pp.GetWeight (weightId);
							value = WeightEditorUtil.WeightEditField (wd, value);
							if (EditorGUI.EndChangeCheck ()) {
								WeightEditorUtil.SetControlPointWeight (pathData, selectedPointIndex, weightId, value);
								EditorUtility.SetDirty (target);
							}

							if (GUILayout.Button ("-", EditorStyles.miniButton, GUILayout.Width (20f))) {
								pp.RemoveWeight (weightId);
								pathData.SetControlPointAtIndex (selectedPointIndex, pp);
								EditorUtility.SetDirty (target);
							}
							EditorGUILayout.EndHorizontal ();

						}
						EditorGUILayout.BeginHorizontal (GUILayout.ExpandWidth (false));

						// Get list of available weights; don't include those already used!
						List<string> unusedWeightIds = WeightEditorUtil.GetAvailableWeightIds (pathData, weightIds);
						EditorGUI.BeginDisabledGroup (unusedWeightIds.Count == 0);
						unusedWeightIds.Insert (0, "");
						selectedNewWeightIndex = EditorGUILayout.Popup (selectedNewWeightIndex, unusedWeightIds.ToArray (), GUILayout.ExpandWidth (false), GUILayout.MaxWidth (EditorGUIUtility.labelWidth));
						EditorGUI.EndDisabledGroup ();
						selectedNewWeightIndex = Mathf.Clamp (selectedNewWeightIndex, 0, unusedWeightIds.Count - 1);

						EditorGUILayout.LabelField ("", GUILayout.ExpandWidth (true));

						EditorGUI.BeginDisabledGroup (selectedNewWeightIndex <= 0);
						if (GUILayout.Button ("+", EditorStyles.miniButton, GUILayout.Width (20f))) {
							WeightEditorUtil.AddWeightToControlPoint (pathData, selectedPointIndex, unusedWeightIds [selectedNewWeightIndex]);
							EditorUtility.SetDirty (target);
							selectedNewWeightIndex = -1;
						}
						EditorGUI.EndDisabledGroup ();

						EditorGUILayout.EndHorizontal ();

						EditorGUI.indentLevel--;
					}

					EditorGUI.indentLevel--;

				} else {

				}
			}


			int cpCount = pathData.GetControlPointCount ();
			controlPointsVisible = EditorGUILayout.Foldout (controlPointsVisible, "Control Points (" + cpCount + ")");
			if (controlPointsVisible) {


				int selectedControlPointEditView = editorParams.GetInt ("PathPointsToolbarSelection", 0);
				EditorGUI.BeginChangeCheck ();
				selectedControlPointEditView = GUILayout.Toolbar (selectedControlPointEditView, new string[] {"Position", "Weight"});
				if (EditorGUI.EndChangeCheck ()) {
					editorParams.SetInt ("PathPointsToolbarSelection", selectedControlPointEditView);
				}

				bool showInterpolatedWeightsForMissing = false;
				bool showDefaultWeightsForMissing = true;
				WeightDefinition selectedWeightDefinition = null;

				switch (selectedControlPointEditView) {
				case 0:
					// Points editor
					EditorGUILayout.BeginHorizontal (normalPointRowStyle);
					EditorGUILayout.LabelField ("Insert new point to the beginning -->");
					if (GUILayout.Button (new GUIContent ("+", "Add new point to the beginning"), 
					                      EditorStyles.miniButton, GUILayout.ExpandWidth (false))) {
						InsertControlPoint (0);
						cpCount = pathData.GetControlPointCount ();
					}
					EditorGUILayout.EndHorizontal ();
					break;
				case 1:
					// Weights editor
					// Weight selector

					int selectedWeightEditingIndex = editorParams.GetInt ("PathPointsSelectedWeightEditingIndex", 0);
					EditorGUI.BeginChangeCheck ();
					selectedWeightEditingIndex = EditorGUILayout.Popup (
						"Weight Param", selectedWeightEditingIndex, 
						definedWeightIds, 
						GUILayout.ExpandWidth (true));
					if (EditorGUI.EndChangeCheck ()) {
						editorParams.SetInt ("PathPointsSelectedWeightEditingIndex", selectedWeightEditingIndex);
					}
					if (selectedWeightEditingIndex >= 0 && selectedWeightEditingIndex < definedWeightIds.Length) {
						string weightId = definedWeightIds [selectedWeightEditingIndex];
						selectedWeightDefinition = WeightEditorUtil.GetWeightDefinition (pathData, weightId);
					} else {
						selectedWeightDefinition = null;
					}
					showInterpolatedWeightsForMissing = editorParams.SetBool ("showInterpolatedWeightsForMissing", 
					                                                          EditorGUILayout.Toggle ("Show interpolated values for missing", 
					                        editorParams.GetBool ("showInterpolatedWeightsForMissing", false)));

					showDefaultWeightsForMissing = editorParams.SetBool (
						"showDefaultWeightsForMissing", 
						EditorGUILayout.Toggle (
							"Show default values for missing", 
							editorParams.GetBool ("showDefaultWeightsForMissing", true)));


					// TODO interpolation settings!
					// TODO read interpolation settings from path modifier if any present!


					break;
				}

//				cpCount = 0;

				// Calculate interpolated values; 
				PathPoint[] interpolatedWeightPoints;

				if (showInterpolatedWeightsForMissing) {
					interpolatedWeightPoints = new PathPoint[cpCount];
					for (int i = 0; i < cpCount; i++) {
						interpolatedWeightPoints [i] = new PathPoint (pathData.GetControlPointAtIndex (i));
					}
					InterpolateWeightsPathModifier.InterpolateWeight (selectedWeightDefinition.WeightId, interpolatedWeightPoints, pathData.GetPathInfo ().IsLoop ());
				} else {
					interpolatedWeightPoints = null;
				}

				for (int i = 0; i < cpCount; i++) {

					PathPoint pp = pathData.GetControlPointAtIndex (i);


					GUIStyle style = selectedPointIndex == i ? selectedPointRowStyle : normalPointRowStyle;
					EditorGUILayout.BeginHorizontal (style, GUILayout.ExpandWidth (true));

					GUI.SetNextControlName ("ControlPoint_" + i);
					string label = String.Format ("[{0}]", i);

//					EditorGUIUtility.labelWidth = pointRowLabelWidth;
//					EditorGUILayout.PrefixLabel (label);



					if (selectedControlPointEditView == 0) {
						// Position editing mode
						EditorGUI.BeginChangeCheck ();
						pp.Position = DrawCustomVector3Field (label, pointRowLabelWidth, pp.Position);
						if (EditorGUI.EndChangeCheck ()) {
							SetControlPoint (i, pp);
						}
						if (GUILayout.Button (new GUIContent ("-", "Delete this point"), 
						                      EditorStyles.miniButtonLeft, GUILayout.ExpandWidth (false))) {
							DeleteControlPoint (i);
							cpCount = pathData.GetControlPointCount ();
						}
						if (GUILayout.Button (new GUIContent ("+", "Insert new point after this point"), 
						                      EditorStyles.miniButtonRight, GUILayout.ExpandWidth (false))) {
							InsertControlPoint (i + 1);
							cpCount = pathData.GetControlPointCount ();
						}

					} else if (selectedControlPointEditView == 1) {
						// Weight editing mode

						float prevLabelWidth = EditorGUIUtility.labelWidth;
						EditorGUIUtility.labelWidth = pointRowLabelWidth;

						if (null != selectedWeightDefinition) {
							// Draw the weight editor
							EditorGUILayout.BeginHorizontal ();
							if (pp.HasWeight (selectedWeightDefinition.WeightId)) {
								// We have the weight defined for this point
								float value = pp.GetWeight (selectedWeightDefinition.WeightId);
								EditorGUI.BeginChangeCheck ();
								value = WeightEditorUtil.WeightEditField (selectedWeightDefinition, value, label);
								if (EditorGUI.EndChangeCheck ()) {
									WeightEditorUtil.SetControlPointWeight (pathData, i, selectedWeightDefinition.WeightId, value);
									EditorUtility.SetDirty (target);
								}
								if (GUILayout.Button ("Del", EditorStyles.miniButtonRight, GUILayout.Width (30f))) {
									pp.RemoveWeight (selectedWeightDefinition.WeightId);
									pathData.SetControlPointAtIndex (i, pp);
									EditorUtility.SetDirty (target);
								}
							} else {
								// No weight defined for this point... use interpolated value
								float value;

								if (showInterpolatedWeightsForMissing && interpolatedWeightPoints [i].HasWeight (selectedWeightDefinition.WeightId)) {
									value = interpolatedWeightPoints [i].GetWeight (selectedWeightDefinition.WeightId);
								} else if (showDefaultWeightsForMissing && selectedWeightDefinition.HasDefaultValue) {
									value = selectedWeightDefinition.DefaultValue;
								} else {
									value = float.NaN;
								}

								//float value = WeightEditorUtil.GetInterpolatedWeightValue (pathData, selectedWeightDefinition, i);
								EditorGUI.BeginDisabledGroup (true);
								WeightEditorUtil.WeightEditField (selectedWeightDefinition, value, label);
								EditorGUI.EndDisabledGroup ();
//								string addLabel = "Add '" + selectedWeightDefinition.WeightId + "'";
								if (GUILayout.Button (
									"Add", 
									EditorStyles.miniButton, GUILayout.Width (30f))) {
									const float NaN = float.NaN;
									if (NaN == value) {
										value = selectedWeightDefinition.DefaultValue;
										if (NaN == value) {
											value = 0f;
										}
									}
									WeightEditorUtil.SetControlPointWeight (
										pathData, i, selectedWeightDefinition.WeightId, value);
									EditorUtility.SetDirty (target);
								}

							}

							EditorGUILayout.EndHorizontal ();
						} else {
							EditorGUILayout.LabelField (label, "");
						}

						EditorGUIUtility.labelWidth = prevLabelWidth;
					}


					EditorGUILayout.EndHorizontal ();

				}

//				if (previousSelectedPointIndex != selectedPointIndex) {
//					// Selection changed outside the inspector; update focus
//					GUI.FocusControl ("ControlPoint_" + selectedPointIndex);
//					previousSelectedPointIndex = selectedPointIndex;
//				} else {
//					// Set selection based on focused control:
//					string focusedControl = GUI.GetNameOfFocusedControl ();
//					if (focusedControl.IndexOf ("ControlPoint_") >= 0) {
//						int selPoint;
//						if (int.TryParse (focusedControl.Substring ("ControlPoint_".Length), out selPoint)) {
//							if (selPoint != this.SelectedControlPointIndex) {
//								this.SelectedControlPointIndex = selPoint;
//								EditorUtility.SetDirty (path);
//							}
//						}
//					}
//				}


			} 

			DrawDefaultPathPointsInspectorGUI ();
		}

		Vector3 DrawCustomVector3Field (string label, float labelWidth, Vector3 pt)
		{
			float prevLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = labelWidth;
			pt = DrawCustomVector3Field (label, pt);
			EditorGUIUtility.labelWidth = prevLabelWidth;
			return pt;
		}
		Vector3 DrawCustomVector3Field (string label, Vector3 pt)
		{
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.PrefixLabel (label);
			pt = DrawCustomVector3Field (pt);
			EditorGUILayout.EndHorizontal ();
			return pt;
		}

		Vector3 DrawCustomVector3Field (Vector3 pt)
		{
			float prevLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 10f;
			EditorGUILayout.BeginHorizontal ();
			pt.x = EditorGUILayout.FloatField ("X", pt.x, GUILayout.MinWidth (20f));
			pt.y = EditorGUILayout.FloatField ("Y", pt.y, GUILayout.MinWidth (20f));
			pt.z = EditorGUILayout.FloatField ("Z", pt.z, GUILayout.MinWidth (20f));
			EditorGUILayout.EndHorizontal ();
			EditorGUIUtility.labelWidth = prevLabelWidth;
			return pt;
		}

		void SetControlPoint (int index, PathPoint pp)
		{
			PolylinePath path = (PolylinePath)target;
			Undo.RecordObject (path, "Modify Control Point");
			pathData.SetControlPointAtIndex (index, pp);
			EditorUtility.SetDirty (path);
		}

		void DeleteControlPoint (int index)
		{
			PolylinePath path = (PolylinePath)target;
			Undo.RecordObject (path, "Delete Control Point");
			pathData.RemoveControlPointAtIndex (index);
			EditorUtility.SetDirty (path);
		}

		void InsertControlPoint (int index)
		{
			PolylinePath path = (PolylinePath)target;

			int cpCount = pathData.GetControlPointCount ();
			if (index > cpCount || index < 0) {
				throw new ArgumentOutOfRangeException ("index");
			}

			Undo.RecordObject (path, "Insert Control Point");

			float tailOrHeadDistFromPrev = 1.0f;

			Vector3 prevPt, dir;
			float distFromPrev;



			if (cpCount == 0) {
				// Insert as first (and only) point
				prevPt = Vector3.zero;
				dir = Vector3.zero;
				distFromPrev = 0.0f;

			} else if (index == 0) {
				// Insert as first point
				// Extrapolate backwards from the previous first point
				prevPt = pathData.GetControlPointAtIndex (0).Position;
				if (cpCount > 1) {
					dir = (prevPt - pathData.GetControlPointAtIndex (1).Position);
					distFromPrev = dir.magnitude;
					dir.Normalize ();
				} else {
					dir = Vector3.back;
					distFromPrev = tailOrHeadDistFromPrev;
				}
			} else if (index == cpCount) {
				// Insert as last point
				// Extrapolate forward from the previous last point
				prevPt = pathData.GetControlPointAtIndex (cpCount - 1).Position;
				if (cpCount > 1) {
					dir = (prevPt - pathData.GetControlPointAtIndex (cpCount - 2).Position);
					distFromPrev = dir.magnitude;
					dir.Normalize ();
				} else {
					dir = Vector3.forward;
					distFromPrev = tailOrHeadDistFromPrev;
				}
			} else {
				// Insert between existing points
				prevPt = pathData.GetControlPointAtIndex (index - 1).Position;
				Vector3 nextPt = pathData.GetControlPointAtIndex (index).Position;
				dir = (nextPt - prevPt);
				// Insert in middle:
				distFromPrev = dir.magnitude / 2.0f;
				dir.Normalize ();
			}

			Debug.Log ("Insert Control Point: index=" + index + "; prevPt=" + prevPt + "; dir=" + dir + "; distFromPrev=" + distFromPrev);


			// TODO: Configurable Add Control Point method (tail or head distance):
			// - Fixed distance (user defined)
			// - Average distance between path points
			// - Distance between next / previous points

			PathPoint newPp = new PathPoint (prevPt + dir * distFromPrev);

			pathData.InsertControlPoint (index, newPp);
			EditorUtility.SetDirty (path);
		}

		void OnEnable ()
		{
			Tools.hidden = false;
		}

		void OnDisable ()
		{
			Tools.hidden = false;
		}

	

	}
    
}
