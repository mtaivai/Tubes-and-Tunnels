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
			GameObject go = new GameObject ("Polyline Path", typeof(PolylinePath));
		}

		[MenuItem("Paths/Create/Create Polyline Path with Track")]
		private static void CreatePolylinePathWithTrackOption ()
		{
			GameObject go = new GameObject ("Polyline Path", typeof(PolylinePath), typeof(Tracks.Track), typeof(MeshFilter), typeof(MeshRenderer));
			Tracks.Track track = go.GetComponent<Tracks.Track> ();
			track.Path = go.GetComponent<PolylinePath> ();
			track.TrackGeneratorType = typeof(Tracks.Tube.TubeGenerator).FullName;
			track.GenerateTrackMesh ();

		}

		[MenuItem("CONTEXT/PolylinePath/Create Track")]
		private static void CreateTrackOption (MenuCommand menuCommand)
		{
			Path path = (Path)menuCommand.context;
			GameObject go = path.gameObject;
			go.AddComponent<Tracks.Track> ();

		}
	}

	[CustomEditor(typeof(PolylinePath))]
	public class PolylinePathEditor : PathEditor
	{

		private GUIStyle labelStyle;


//      private DictionaryEditorItemPrefs pathModifierPrefs = new DictionaryEditorItemPrefs();

		public PolylinePathEditor ()
		{
			// TODO we should have common / shared styles!
			labelStyle = new GUIStyle ();
			Texture2D labelBgTexture = new Texture2D (1, 1, TextureFormat.RGBA32, false);
			labelBgTexture.SetPixel (0, 0, new Color (0.3f, 0.3f, 0.3f, 0.5f));
			labelBgTexture.Apply ();
            
			labelStyle.alignment = TextAnchor.UpperLeft;
			labelStyle.normal = new GUIStyleState ();
			labelStyle.normal.background = labelBgTexture;
			labelStyle.normal.textColor = Color.white;
		}
    
		protected override void DrawGeneralInspectorGUI ()
		{
			DrawDefaultGeneralInspectorGUI ();

			PolylinePath path = target as PolylinePath;

			EditorGUI.BeginChangeCheck ();
			path.SetLoop (EditorGUILayout.Toggle ("Loop", path.IsLoop ()));
			if (EditorGUI.EndChangeCheck ()) {
				Undo.RecordObject (path, "Toggle Path Loop Mode");
				EditorUtility.SetDirty (path);
			}
		}

		protected override void DrawPathPointsInspector (ref bool expanded)
		{
        
			PolylinePath path = (PolylinePath)target;


			if (GUILayout.Button ("Add Control Point")) {
				InsertControlPoint (0);
			}

			int cpCount = path.GetControlPointCount ();
			expanded = EditorGUILayout.Foldout (expanded, "Control Points (" + cpCount + ")");
			if (expanded) {

				for (int i = 0; i < cpCount; i++) {

					Vector3 pt = path.GetControlPointAtIndex (i);
					EditorGUI.BeginChangeCheck ();
					EditorGUILayout.BeginHorizontal ();
					GUI.SetNextControlName ("ControlPoint_" + i);
					pt = EditorGUILayout.Vector3Field ("[" + i + "]", pt);
					if (EditorGUI.EndChangeCheck ()) {
						SetControlPoint (i, pt);
					}
					if (GUILayout.Button (new GUIContent ("-", "Delete Control Point"), GUILayout.Width (32))) {
						DeleteControlPoint (i);
						cpCount = path.GetControlPointCount ();
					}
					if (GUILayout.Button (new GUIContent ("+", "Insert Control Point"), GUILayout.Width (32))) {
						InsertControlPoint (i + 1);
						cpCount = path.GetControlPointCount ();
					}
					EditorGUILayout.EndHorizontal ();
                    
				}
				if (SelectedControlPointIndex >= 0) {
					//GUI.FocusControl("ControlPoint_" + selectedControlPointIndex);
				}
				string focusedControl = GUI.GetNameOfFocusedControl ();
				if (focusedControl.IndexOf ("ControlPoint_") >= 0) {
					int selPoint;
					if (int.TryParse (focusedControl.Substring ("ControlPoint_".Length), out selPoint)) {
						if (selPoint != this.SelectedControlPointIndex) {
							this.SelectedControlPointIndex = selPoint;
							EditorUtility.SetDirty (path);
						}
					}

				}

			}

            
			if (GUILayout.Button ("Add Control Point")) {
				InsertControlPoint (cpCount);
			}

			DrawDefaultPathPointsInspector ();
		}

		void SetControlPoint (int index, Vector3 pt)
		{
			PolylinePath path = (PolylinePath)target;
			Undo.RecordObject (path, "Modify Control Point");
			path.SetControlPointAtIndex (index, pt);
			EditorUtility.SetDirty (path);
		}

		void DeleteControlPoint (int index)
		{
			PolylinePath path = (PolylinePath)target;
			Undo.RecordObject (path, "Delete Control Point");
			path.RemoveControlPointAtIndex (index);
			EditorUtility.SetDirty (path);
		}

		void InsertControlPoint (int index)
		{
			PolylinePath path = (PolylinePath)target;

			int cpCount = path.GetControlPointCount ();
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
				prevPt = path.GetControlPointAtIndex (0);
				if (cpCount > 1) {
					dir = (prevPt - path.GetControlPointAtIndex (1));
					distFromPrev = dir.magnitude;
					dir.Normalize ();
				} else {
					dir = Vector3.back;
					distFromPrev = tailOrHeadDistFromPrev;
				}
			} else if (index == cpCount) {
				// Insert as last point
				// Extrapolate forward from the previous last point
				prevPt = path.GetControlPointAtIndex (cpCount - 1);
				if (cpCount > 1) {
					dir = (prevPt - path.GetControlPointAtIndex (cpCount - 2));
					distFromPrev = dir.magnitude;
					dir.Normalize ();
				} else {
					dir = Vector3.forward;
					distFromPrev = tailOrHeadDistFromPrev;
				}
			} else {
				// Insert between existing points
				prevPt = path.GetControlPointAtIndex (index - 1);
				Vector3 nextPt = path.GetControlPointAtIndex (index);
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

			Vector3 newPt = prevPt + dir * distFromPrev;

			path.InsertControlPoint (index, newPt);
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

		void OnSceneGUI ()
		{
//            if (SelectedControlPointIndex >= 0)
//            {
//                Tools.hidden = true;
//            } else
//            {
//                Tools.hidden = false;
//            }
			//PolylinePath path = target as PolylinePath;
        
			//DrawPath();

			// Draw something interesting
			//DrawControlPointHandles();
			DrawDefaultSceneGUI ();
		}





	}
    
}
