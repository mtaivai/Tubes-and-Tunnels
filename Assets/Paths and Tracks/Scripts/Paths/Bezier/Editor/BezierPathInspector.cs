using UnityEngine;
using UnityEditor;

using System.Collections;
using System.Collections.Generic;
using System;

using Paths;
using Paths.Editor;

namespace Paths.Bezier.Editor
{
	[CustomEditor(typeof(BezierPath))]
	public class BezierPathInspector : AbstractPathEditor<BezierPath, AbstractPathData>
	{
		private Transform transform;
		private GUIStyle labelStyle;
		int selectedControlPointIndex = -1;
		//int stepsPerSegment = 10;

		private bool controlPointsVisible = true;

		void OnSceneGUI ()
		{
			Tools.hidden = false;
            
			this.path = target as BezierPath;
			this.transform = path.transform;

			// TODO don't create this every time!
			if (null == labelStyle) {
				labelStyle = new GUIStyle ();
				Texture2D labelBgTexture = new Texture2D (1, 1, TextureFormat.RGBA32, false);
				labelBgTexture.SetPixel (0, 0, new Color (0.3f, 0.3f, 0.3f, 0.5f));
				labelBgTexture.Apply ();
                
				labelStyle.alignment = TextAnchor.UpperLeft;
				labelStyle.normal = new GUIStyleState ();
				labelStyle.normal.background = labelBgTexture;
				labelStyle.normal.textColor = Color.white;
			}

			DrawPath ();
		}
        
		protected override void DrawGeneralInspectorGUI ()
		{
			DrawDefaultGeneralInspectorGUI ();

			this.path = target as BezierPath;
			this.transform = path.transform;
            
			EditorGUI.BeginChangeCheck ();
			int pointsPerSegment = EditorGUILayout.IntField ("Points/segment", path.PointsPerSegment);
			if (EditorGUI.EndChangeCheck ()) {
				Undo.RecordObject (path, "Change Points/segment");
				path.PointsPerSegment = pointsPerSegment;
				EditorUtility.SetDirty (path);
				path.SetPointsDirty ();
			}
            
			EditorGUI.BeginChangeCheck ();
			bool loop = EditorGUILayout.Toggle ("Loop", path.Loop);
			if (EditorGUI.EndChangeCheck ()) {
				Undo.RecordObject (path, "Toggle Loop");
				path.Loop = loop;
				EditorUtility.SetDirty (path);
				path.SetPointsDirty ();
			}
            
			if (selectedControlPointIndex >= 0 && selectedControlPointIndex < path.ControlPointCount) {
				DrawSelectedPointInspector ();
			}
			//DrawDefaultInspector();
            
			if (GUILayout.Button ("Clear Caches")) {
				path.SetPointsDirty ();
			}
            
			if (GUILayout.Button ("Add Segment")) {
				Undo.RecordObject (path, "Add Segment");
				path.AddSegment ();
				EditorUtility.SetDirty (path);
			}
            
			if (GUILayout.Button ("Remove Segment")) {
				Undo.RecordObject (path, "Remove Segment");
				path.RemoveSegment (path.SegmentCount - 1);
				EditorUtility.SetDirty (path);
			}
		}

		protected override void DrawPathModifiersInspectorGUI ()
		{
			base.DrawPathModifiersInspectorGUI ();
		}

		protected override void DrawSettingsInspectorGUI ()
		{
			base.DrawSettingsInspectorGUI ();
		}

		protected override void DrawDebugInspectorGUI ()
		{
			base.DrawDebugInspectorGUI ();
		}
        
		protected override void DrawPathPointsInspectorGUI ()
		{
            
			BezierPath path = (BezierPath)target;


			EditorGUILayout.Foldout (true, "Segments");
			for (int i = 0; i < path.SegmentCount; i++) {
				BezierPathSegment s = path.GetSegment (i);
               
				EditorGUILayout.LabelField ("Segment " + i);
				s.name = EditorGUILayout.TextField ("Name", s.name);
				s.upVector = EditorGUILayout.Vector3Field ("Up Vector", s.upVector);
			}

            
			if (GUILayout.Button ("Add Control Point")) {
//                InsertControlPoint(0);
			}


			int cpCount = path.ControlPointCount;
			controlPointsVisible = EditorGUILayout.Foldout (controlPointsVisible, "Control Points (" + cpCount + ")");
			if (controlPointsVisible) {
                
				for (int i = 0; i < cpCount; i++) {
                    
                    
                    
					Vector3 pt = path.GetControlPoint (i);
					EditorGUI.BeginChangeCheck ();
					EditorGUILayout.BeginHorizontal ();
					GUI.SetNextControlName ("ControlPoint_" + i);
					pt = EditorGUILayout.Vector3Field ("[" + i + "]", pt);
					if (EditorGUI.EndChangeCheck ()) {
						path.SetControlPoint (i, pt);
						EditorUtility.SetDirty (path);
					}
					if (GUILayout.Button (new GUIContent ("-", "Delete Control Point"), GUILayout.Width (32))) {
//                        DeleteControlPoint(i);
//                        cpCount = path.GetControlPointCount();
					}
					if (GUILayout.Button (new GUIContent ("+", "Insert Control Point"), GUILayout.Width (32))) {
//                        InsertControlPoint(i + 1);
//                        cpCount = path.GetControlPointCount();
					}
					EditorGUILayout.EndHorizontal ();
                    
				}
				if (selectedControlPointIndex >= 0) {
					//GUI.FocusControl("ControlPoint_" + selectedControlPointIndex);
				}
				string focusedControl = GUI.GetNameOfFocusedControl ();
				Debug.Log ("Focus: " + focusedControl);
				if (focusedControl.IndexOf ("ControlPoint_") >= 0) {
					int selPoint;
					if (int.TryParse (focusedControl.Substring ("ControlPoint_".Length), out selPoint)) {
						if (selPoint != this.selectedControlPointIndex) {
							this.selectedControlPointIndex = selPoint;
							EditorUtility.SetDirty (path);
						}
					}
                    
				}
                
			}
            
            
			if (GUILayout.Button ("Add Control Point")) {
//                InsertControlPoint(cpCount);
			}
            
			DrawDefaultPathDataInspector ();
		}
        
		private void DrawSelectedPointInspector ()
		{
			GUILayout.Label ("Selected Point");
            
//          BezierPathSegment selectedSegment = path.GetSegment(selectedControlPointIndex);
            
			BezierPathSegmentJoint selectedSegmentJoint = path.GetSegmentJoint (selectedControlPointIndex);
            
            
			EditorGUI.BeginChangeCheck ();
			Vector3 point = EditorGUILayout.Vector3Field ("Position", path.GetControlPoint (selectedControlPointIndex));
			if (EditorGUI.EndChangeCheck ()) {
				Undo.RecordObject (path, "Move Control Point");
				EditorUtility.SetDirty (path);
				path.SetControlPoint (selectedControlPointIndex, point);
			}
            
			EditorGUI.BeginChangeCheck ();
			BezierJointMode mode = (BezierJointMode)
                EditorGUILayout.EnumPopup ("Joint Mode", selectedSegmentJoint.controlPointMode);
			if (EditorGUI.EndChangeCheck ()) {
				Undo.RecordObject (path, "Change Joint Mode");
				selectedSegmentJoint.controlPointMode = mode;
				EditorUtility.SetDirty (path);
			}
		}
        
		void DrawPath ()
		{
//            path.editorPrefs;
			Color lineColor = PathEditorPrefs.ControlPointConnectionLineColor;
			Color selectedLineColor = PathEditorPrefs.SelectedControlPointConnectionLineColor;

//          float directionVectorLength = 0.5f;
			int pathSteps = path.PointsPerSegment * path.SegmentCount;
            
			int pointCount = path.ControlPointCount;
            
			DoControlPointHandles ();
            
			// Connect first and last control points by straight lines:
			//Handles.color = Color.red;
			//Handles.DrawLine(p[0], p[pointCount - 1]);
            
//          Vector3[] ctrlPoints = path.GetControlPoints(0, pointCount, transform);
            
			// Draw nice Bezier using Handles utility:
			for (int i = 1; i < pointCount; i += 3) {
                
				/*Handles.DrawBezier(
                ctrlPoints[i - 1], ctrlPoints[i + 2], 
                ctrlPoints[i], ctrlPoints[i + 1], 
                Color.white, null, 2.0f);*/
			}
            
			int selectedSegmentIndex = path.GetSegmentIndex (selectedControlPointIndex);
            
			// Connect control points:
			Vector3 startPoint = path.GetPoint (0.0f);
			for (int i = 1; i <= pathSteps; i++) {
				float t = (float)i / (float)pathSteps;
                
				Vector3 endPoint = path.GetPoint (t);
                
				// Connection between path steps:
                
				int segmentIndex = path.GetSegmentIndex (t);
                
				if (segmentIndex == selectedSegmentIndex) {
					Handles.color = selectedLineColor;
				} else {
					Handles.color = lineColor;
				}
                
				Handles.DrawLine (startPoint, endPoint);
                
                
				// Tangent direction:
				//Handles.color = Color.green;
				//Handles.DrawLine(endPoint, endPoint + path.GetTangentDirection(t) * directionVectorLength);
                
				startPoint = endPoint;
			}
			DrawDirectionVectors ();
            
		}
        
		void DrawDirectionVectors ()
		{
			// TODO load colors from prefs
			Color selectedTangentColor = Color.magenta;
			Color tangentColor = Color.magenta;

			float directionVectorLength = 5.0f;
            
			int pathSteps = path.PointsPerSegment * path.SegmentCount;
            
//          Vector3 startPoint = path.GetPoint (0.0f);
            
			int selectedSegmentIndex = path.GetSegmentIndex (selectedControlPointIndex);
            
            
//            Vector3 prevPt = Vector3.zero;
			for (int i = 0; i <= pathSteps; i++) {
				if (i == pathSteps && path.Loop) {
					break;
				}
				float t = (float)i / (float)pathSteps;
                
				Vector3 pt = path.GetPoint (t);
                
				// Tangent direction:
				Vector3 dir = path.GetTangentDirection (t);
                
				int segmentIndex = path.GetSegmentIndex (t);
                
				if (segmentIndex == selectedSegmentIndex) {
					Handles.color = selectedTangentColor;
				} else {
					Handles.color = tangentColor;
				}
                
				//Handles.DrawSolidArc(pt, dir, transform.right, 360f, 0.1f);
				Handles.DrawLine (pt, pt + dir * directionVectorLength);
                
			}
		}
        
		Color GetJointColor (int controlPointIndex)
		{
			Color color;
			// Handle color depends on the joint mode:
			BezierPathSegmentJoint joint = path.GetSegmentJoint (controlPointIndex);
			switch (joint.controlPointMode) {
			case BezierJointMode.Aligned:
				color = BezierPathEditorPrefs.AlignedJointHandleColor;
				break;
			case BezierJointMode.Mirrored:
				color = BezierPathEditorPrefs.MirroredJointHandleColor;
				break;
			default:
				color = BezierPathEditorPrefs.FreeJointHandleColor;
				break;
			}
			return color;
		}
		/*
    Vector3 DoControlPointHandles(int pointIndex) {
        return DoControlPointHandles(pointIndex, 1)[0];
    }

    Vector3[] DoControlPointHandles(int firstIndex, int count) {
        return DoControlPointHandles(firstIndex, count, null);
    }
    */
		Vector3[] DoControlPointHandles ()
		{
			int count = path.ControlPointCount;
			int firstIndex = 0;
            
			Vector3[] points = new Vector3[count];
            
			Quaternion rot;
			if (Tools.pivotRotation == PivotRotation.Local) {
				// Local pivot rotation
				rot = transform.rotation;
			} else {
				// Global rotation
				rot = Quaternion.identity;
			}
			// First pass: just transform points:
			for (int i = 0; i < count; i++) {
				int controlPointIndex = i + firstIndex;
				points [i] = transform.TransformPoint (path.GetControlPoint (controlPointIndex));
			}

			float firstControlPointHandleSize = PathEditorPrefs.FirstControlPointHandleSize;
			float controlPointHandleSize = PathEditorPrefs.ControlPointHandleSize;
			float firstControlPointPickSize = PathEditorPrefs.FirstControlPointPickSize;
			float controlPointPickSize = PathEditorPrefs.ControlPointPickSize;

			int controlPointCount = path.ControlPointCount;
			for (int i = 0; i < count; i++) {
				int controlPointIndex = i + firstIndex;
				if (path.Loop && controlPointIndex == controlPointCount - 1) {
					// Don't draw last cp in loop mode
					break;
				}
				float t = (float)controlPointIndex / (float)(controlPointCount - 1);
                
				float worldHandleSize = HandleUtility.GetHandleSize (points [i]);
                
				// Control point handles; first is bigger
				float handleSize;
				float pickSize;
				if (0 == controlPointIndex) {
					handleSize = firstControlPointHandleSize * worldHandleSize;
					pickSize = firstControlPointPickSize * worldHandleSize;
				} else {
					handleSize = controlPointHandleSize * worldHandleSize;
					pickSize = controlPointPickSize * worldHandleSize;
				}
                
				// Handle color depends on the joint mode:
				Handles.color = GetJointColor (controlPointIndex);
                
				// Draw line from the control point handle to the corresponding
				// path point:
				Handles.DrawDottedLine (points [i], path.GetPoint (t), 2.0f);
                
				Handles.DrawCapFunction capFn;
//              bool middlePoint;
				if (controlPointIndex % 3 == 0) {
					// Joint middle
//                  middlePoint = true;
					pickSize *= 1.0f;
					handleSize *= 4.0f;
					capFn = Handles.SphereCap;
                    
					// ; draw line to previous and next cp's
					// From this to next:
					if (i < controlPointCount - 2) {
						Handles.DrawLine (points [i], points [i + 1]);
					}
					// From this to previous:
					if (i > 0) {
						Handles.DrawLine (points [i], points [i - 1]);
					} else if (path.Loop) {
						Handles.DrawLine (points [0], points [controlPointCount - 2]);
					}
				} else {
//                  middlePoint = false;
					capFn = Handles.DotCap;
				}
                
				if (Handles.Button (points [i], rot, 
                                   handleSize, pickSize, 
                                   capFn)) {
					selectedControlPointIndex = i;
					Repaint ();
				}
				if (selectedControlPointIndex == i) {
					EditorGUI.BeginChangeCheck ();
					points [i] = Handles.DoPositionHandle (points [i], rot);
					if (EditorGUI.EndChangeCheck ()) {
						Undo.RecordObject (path, "Move Control Point");
						EditorUtility.SetDirty (path);
						path.SetControlPoint (i + firstIndex, transform.InverseTransformPoint (points [i]));
					}
				}
                
				Handles.Label (points [i], "" + controlPointIndex + "", labelStyle);
                
				/*
            EditorGUI.BeginChangeCheck();
            points[i] = Handles.DoPositionHandle(points[i], rot);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(path, "Move Control Point");
                EditorUtility.SetDirty(path);
                path.ControlPoints[i + firstIndex] = transform.InverseTransformPoint(points[i]);
            }*/
			}
            
			return points;
		}
	}
    
}
