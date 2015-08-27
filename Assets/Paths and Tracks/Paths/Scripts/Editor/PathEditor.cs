using UnityEngine;
using UnityEditor;

using System.Collections;
using System.Collections.Generic;
using System;

using Paths;
using Util.Editor;
using Util;

namespace Paths.Editor
{
	public class PathMenu
	{
//        [MenuItem("Paths")]
//        private static void PathsMenuOption()
//        {
//        }
	}

	public class PathEditor : UnityEditor.Editor
	{

		protected const int TB_SHEET_GENERAL = 0;
		protected const int TB_SHEET_POINTS = 1;
		protected const int TB_SHEET_MODIFIERS = 2;
		protected const int TB_SHEET_SETTINGS = 3;
		protected const int TB_SHEET_DEBUG = 4;
		private static string[] TB_TEXTS = {
            "Path",
            "Points",
            "Modifiers",
            "Settings",
            "Debug"
        };
		private Dictionary<int, bool> pointExpanded = new Dictionary<int, bool> ();
		private int selectedControlPointIndex = -1;

		protected int SelectedControlPointIndex {
			get {
				return selectedControlPointIndex;
			}
			set {
				this.selectedControlPointIndex = value;
			}
		}

		private void PathModifiersChanged ()
		{
			EditorUtility.SetDirty (target);
			UnityEditor.SceneView.RepaintAll ();
			((Path)target).PathModifiersChanged ();
		}

		public override sealed void OnInspectorGUI ()
		{
			DrawDefaultPathInspectorGUI ();
			//DrawDefaultInspector();
		}

		public int DrawDefaultPathInspectorGUI ()
		{
			Path path = target as Path;
//          bool disableEditing = path.FrozenStatus == Path.PathStatus.Frozen;


			ParameterStore editorParams = path.EditorParameters;

			int pointCount = path.GetPointCount ();
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField ("Points", pointCount.ToString ());


			//EditorGUI.BeginDisabledGroup(path.PointsDirty == false);
			if (GUILayout.Button ("Refresh" + (path.PointsDirty ? " *" : ""))) {
				path.ForceUpdatePathPoints ();
				EditorUtility.SetDirty (path);
			}
			//EditorGUI.EndDisabledGroup();

			EditorGUILayout.EndHorizontal ();

			string totalDistance = "";
			if (pointCount > 0) {
				PathPoint lastPoint = path.GetPointAtIndex (pointCount - 1);
				if (lastPoint.HasDistanceFromBegin) {
					totalDistance = lastPoint.DistanceFromBegin.ToString ();
				}
			}
			EditorGUILayout.LabelField ("Total Length", totalDistance);

           

			int tbSheet = editorParams.GetInt ("ToolbarSelection", 0);
			EditorGUI.BeginChangeCheck ();
			tbSheet = GUILayout.Toolbar (tbSheet, TB_TEXTS);
			if (EditorGUI.EndChangeCheck ()) {
				editorParams.SetInt ("ToolbarSelection", tbSheet);
			}

			switch (tbSheet) {
			case TB_SHEET_GENERAL:
				DrawGeneralInspectorGUI ();
				break;
			case TB_SHEET_POINTS:
                // TODO why do we have the "expanded" pref here? Why not in the DrawPathPointsInspector fn itself?
				bool pointsExpanded = editorParams.GetBool ("PathPointsExpanded", true);
				DrawPathPointsInspector (ref pointsExpanded);
				editorParams.SetBool ("PathPointsExpanded", pointsExpanded);
				break;
			case TB_SHEET_MODIFIERS:
				DrawPathModifiersInspectorGUI ();
				break;
			case TB_SHEET_SETTINGS:
				DrawSettingsInspectorGUI ();
				break;
			case TB_SHEET_DEBUG:
				DrawDebugInspectorGUI ();
				break;
			}

			EditorGUILayout.Separator ();


			return tbSheet;
			//DrawDefaultInspector();
		}

		protected virtual void DrawGeneralInspectorGUI ()
		{
			DrawDefaultGeneralInspectorGUI ();
		}

		protected void DrawDefaultGeneralInspectorGUI ()
		{
//          Path path = target as Path;
//          EditorGUILayout.LabelField ("", path.PointsDirty ? "*Needs Refresh*" : "Points are up to date");
		}

		protected virtual void DrawPathModifiersInspectorGUI ()
		{
			DrawDefaultPathModifiersInspectorGUI ();
		}

		protected void DrawDefaultPathModifiersInspectorGUI ()
		{
			Path path = target as Path;
			PathModifierEditorUtil.DrawPathModifiersInspector (path, this, path, PathModifiersChanged);
		}

		protected virtual void DrawSettingsInspectorGUI ()
		{
			DrawDefaultSettingsInspectorGUI ();
		}

		protected void DrawDefaultSettingsInspectorGUI ()
		{
			Path path = target as Path;
			EditorGUI.BeginChangeCheck ();
			path.FrozenStatus = (Path.PathStatus)EditorGUILayout.EnumPopup ("Update Status", path.FrozenStatus);

			//path.Frozen = EditorGUILayout.Toggle("Frozen", path.Frozen);
			if (EditorGUI.EndChangeCheck ()) {
                
			}
		}

		protected virtual void DrawDebugInspectorGUI ()
		{
			DrawDefaultDebugInspectorGUI ();
		}

		protected void DrawDefaultDebugInspectorGUI ()
		{
			DrawDefaultInspector ();
		}

		private bool IsPointExpanded (int index)
		{
			return pointExpanded.ContainsKey (index) ? pointExpanded [index] : false;
		}

		protected virtual void DrawPathPointsInspector (ref bool expanded)
		{
			DrawDefaultPathPointsInspector ();
		}

		protected void DrawDefaultPathPointsInspector ()
		{
			DrawGeneratedPathPointsInspector ();
		}

		protected void DrawGeneratedPathPointsInspector ()
		{

			Path path = target as Path;

			ParameterStore editorParams = path.EditorParameters;

			PathPoint[] points = path.GetAllPoints ();
			bool treeExpanded = EditorGUILayout.Foldout (editorParams.GetBool ("OutputPointsExpanded", false), "Output Points (" + points.Length + ")");
			editorParams.SetBool ("OutputPointsExpanded", treeExpanded);
			if (treeExpanded) {
				EditorGUI.indentLevel++;
				DrawPathPointMask ("Caps", path.GetOutputFlags ());

				for (int i = 0; i < points.Length; i++) {
					EditorGUILayout.BeginHorizontal ();
					pointExpanded [i] = EditorGUILayout.Foldout (IsPointExpanded (i), "[" + i + "]");
					//              EditorGUILayout.Vector3Field("", points[i].Position);
					EditorGUILayout.LabelField ("", points [i].Position.ToString () + "; " + points [i].Direction.ToString () + "; " + points [i].DistanceFromPrevious.ToString () + "; " + points [i].DistanceFromBegin.ToString ());
					EditorGUILayout.EndHorizontal ();
					if (pointExpanded [i]) {
						EditorGUI.indentLevel++;
						DrawPathPointMask ("Flags", points [i].Flags);

						//                  EditorGUILayout.Vector3Field("Position", points[i].Position);
						//                  EditorGUILayout.Vector3Field("Direction", points[i].Direction);
						//                  EditorGUILayout.FloatField("Dist -1", points[i].DistanceFromPrevious);
						//                  EditorGUILayout.FloatField("Total dist", points[i].DistanceFromBegin);

						EditorGUILayout.LabelField ("Position", points [i].Position.ToString ());
						EditorGUILayout.LabelField ("Forward", points [i].Direction.ToString ());
						EditorGUILayout.LabelField ("Up", points [i].Up.ToString ());
						EditorGUILayout.LabelField ("Right", points [i].Right.ToString ());
						EditorGUILayout.LabelField ("Angle", points [i].Angle.ToString ());
						EditorGUILayout.LabelField ("Dist-1", points [i].DistanceFromPrevious.ToString ());
						EditorGUILayout.LabelField ("Dist-0", points [i].DistanceFromBegin.ToString ());
						EditorGUI.indentLevel--;

					}
					//EditorGUILayout.Vector3Field("Dir", points[i].Direction);

				}
				EditorGUI.indentLevel--;
			}
		}

		public static void DrawPathPointMask (string label, int flags)
		{
			Texture2D emptyGridImage = (Texture2D)Resources.Load ("btn-empty-grid", typeof(Texture2D));
			Texture2D posImage = (Texture2D)Resources.Load ("btn-position", typeof(Texture2D));
			Texture2D dirImage = (Texture2D)Resources.Load ("btn-direction", typeof(Texture2D));
			Texture2D upImage = (Texture2D)Resources.Load ("btn-up", typeof(Texture2D));
			Texture2D angleImage = (Texture2D)Resources.Load ("btn-angle", typeof(Texture2D));
			Texture2D d1Image = (Texture2D)Resources.Load ("btn-dist1", typeof(Texture2D));
			Texture2D d0Image = (Texture2D)Resources.Load ("btn-dist0", typeof(Texture2D));
            
            
            
			int btnHeight = 24;
			int btnWidth = 24;
			GUIStyle boxStyle = GUIStyle.none;
			GUILayoutOption[] boxOptions = {
                GUILayout.Width (btnWidth),
                GUILayout.Height (btnHeight)
            };
            
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.PrefixLabel (label);
			GUILayout.Box (PathPoint.IsPosition (flags) ? posImage : emptyGridImage, boxStyle, boxOptions);
			GUILayout.Box (PathPoint.IsDirection (flags) ? dirImage : emptyGridImage, boxStyle, boxOptions);
			GUILayout.Box (PathPoint.IsUp (flags) ? upImage : emptyGridImage, boxStyle, boxOptions);
			GUILayout.Box (PathPoint.IsAngle (flags) ? angleImage : emptyGridImage, boxStyle, boxOptions);
			GUILayout.Box (PathPoint.IsDistanceFromPrevious (flags) ? d1Image : emptyGridImage, boxStyle, boxOptions);
			GUILayout.Box (PathPoint.IsDistanceFromBegin (flags) ? d0Image : emptyGridImage, boxStyle, boxOptions);
			EditorGUILayout.EndHorizontal ();
		}

		void OnSceneGUI ()
		{
			DrawDefaultSceneGUI ();

		}

		protected void DrawDefaultSceneGUI ()
		{
			if (selectedControlPointIndex >= 0) {
				Tools.hidden = true;
			} else {
				Tools.hidden = false;
			}
			//
			//            DrawPath();
			DrawControlPointHandles ();
		}

		void DrawPath ()
		{
			Path path = target as Path;
			Transform transform = path.transform;


			// Connect control points
			int cpCount = path.GetControlPointCount ();
			Vector3[] transformedPoints = new Vector3[cpCount];
            
			for (int i = 0; i < cpCount; i++) {
				transformedPoints [i] = transform.TransformPoint (path.GetControlPointAtIndex (i));
			}
			Handles.color = PathEditorPrefs.ControlPointConnectionLineColor;
			Handles.DrawPolyLine (transformedPoints);


			// Draw directions of Control Points
			Color dirVectorColor = PathEditorPrefs.DirVectorColor;
			float dirVectorLength = PathEditorPrefs.DirVectorLength;
            
			for (int i = 0; i < cpCount; i++) {
				Vector3 pt = transformedPoints [i];
                
				// Direction vector
				Vector3 prevDir, nextDir;
				Vector3 dir = PathUtil.IntersectDirection (transformedPoints, i, false, out prevDir, out nextDir);
                
				Handles.color = dirVectorColor;
				Handles.DrawLine (pt, pt + dir * dirVectorLength);
                
				// UP vector
				//                Vector3 upDir;
				//                if (i == 0) {
				//                    upDir = Vector3.up;
				//                } else {
				//                    upDir = Vector3.Cross(transformedPoints[i - 1], pt).normalized;
				//                }
                
				// Path angle:
				float angle = Vector3.Angle (prevDir, nextDir);
				//                float dot = Vector3.Dot(prevDir, nextDir);
				Vector3 cross = Vector3.Cross (prevDir, nextDir);
				if (cross.y < 0.0f) {
					angle = -angle;
				}
				if (i == 2) {
					//                    Debug.Log("Angle: " + angle + "; dot=" + dot + "; cross=" + cross);
				}
                
				Vector3 upDir = Vector3.up;
				upDir = Quaternion.AngleAxis (angle / 10f, dir) * upDir;
                
				//upDir = Vector3.Reflect(prevDir, Vector3.up);
				// Angle between 
                
				//                Vector3 planeDir;
				//                if (i > 0 && i < cpCount - 1) {
				//                    planeDir = (transformedPoints[i - 1] - transformedPoints[i + 1]).normalized;
				//                } else {
				//                    planeDir = dir;
				//                }
				//                upDir = Quaternion.AngleAxis(90, planeDir) * dir;
				//                //upDir = Quaternion.LookRotation(dir) * Vector3.up;
				//
				//
				//                if (i == 0) {
				//                    // First or only point
				//                    upDir = (cpCount > 1) ? (Vector3.Cross(transformedPoints[i + 1], pt).normalized) : Vector3.up;
				//                } else {
				//                    // Last or middle point
				//                    upDir = (Vector3.Cross(transformedPoints[i - 1], pt).normalized);
				//                }
				//
				//
				Handles.color = Color.green;
				//Handles.DrawLine(pt, pt + upDir * 5.0f);
			}
            
            
            
            
			// Draw the actual path:
			//          PathPoint[] pp = path.GetAllPoints();
			//          transformedPoints = new Vector3[pp.Length];
			//          for (int i = 0; i < pp.Length; i++) {
			//              transformedPoints[i] = transform.TransformPoint(pp[i].Position);
			//          }
			//          Handles.color = Color.cyan;
			//Handles.DrawPolyLine(transformedPoints);
            
			// Draw handles
            
			//          for (int i = 0; i < pp.Length; i++) {
			//              float worldHandleSize = HandleUtility.GetHandleSize(transformedPoints[i]);
			//              float handleSize, pickSize;
			//
			//              handleSize = Constants.controlPointHandleSize * worldHandleSize;
			//              pickSize = Constants.controlPointPickSize * worldHandleSize;
			//          
			//              Handles.Button(transformedPoints[i], transform.rotation, 
			//                             handleSize, pickSize, 
			//                             Handles.DotCap);
			//
			//          }
            
		}
        
		void DrawControlPointHandles ()
		{
			Path path = target as Path;
			Transform transform = path.transform;
			Quaternion rot;
			if (Tools.pivotRotation == PivotRotation.Local) {
				// Local pivot rotation
				rot = transform.rotation;
			} else {
				// Global rotation
				rot = Quaternion.identity;
			}
            
			float firstControlPointHandleSize = PathEditorPrefs.FirstControlPointHandleSize;
			float controlPointHandleSize = PathEditorPrefs.ControlPointHandleSize;
			float firstControlPointPickSize = PathEditorPrefs.FirstControlPointPickSize;
			float controlPointPickSize = PathEditorPrefs.ControlPointPickSize;
			Color jointHandleColor = PathEditorPrefs.ControlPointHandleColor;
            
			int cpCount = path.GetControlPointCount ();
			for (int i = 0; i < cpCount; i++) {
				Vector3 pt = path.GetControlPointAtIndex (i);
				pt = transform.TransformPoint (pt);
                
				float worldHandleSize = HandleUtility.GetHandleSize (pt);
				float handleSize, pickSize;
				if (i == 0) {
					handleSize = firstControlPointHandleSize * worldHandleSize;
					pickSize = firstControlPointPickSize * worldHandleSize;
				} else {
					handleSize = controlPointHandleSize * worldHandleSize;
					pickSize = controlPointPickSize * worldHandleSize;
				}
                
				// Selection button:
				Handles.color = jointHandleColor;
				if (Handles.Button (pt, rot, 
                                   handleSize, pickSize, 
                                   Handles.DotCap)) {
					selectedControlPointIndex = i;
                    
					Repaint ();
				}
                
                
				// Move handle for selected:
				if (i == selectedControlPointIndex) {
					EditorGUI.BeginChangeCheck ();
					pt = Handles.DoPositionHandle (pt, rot);
					if (EditorGUI.EndChangeCheck ()) {
						Undo.RecordObject (path, "Move Control Point");
						EditorUtility.SetDirty (path);
						path.SetControlPointAtIndex (i, transform.InverseTransformPoint (pt));
					}
				}
                
			}
		}
	}
    
}
