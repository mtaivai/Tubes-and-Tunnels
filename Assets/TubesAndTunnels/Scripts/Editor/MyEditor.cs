using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;


[CustomEditor(typeof(MyObject))]
public class MyObjectEditor : Editor
{
	void OnEnable ()
	{
		Tools.hidden = true;
//		SceneView.currentDrawingSceneView.in2DMode = true;
//		SceneView sv = ScriptableObject.CreateInstance<SceneView> ();
//		sv.in2DMode = true;
//		sv.FrameSelected ();
//		sv.m_SceneLighting = false;
//		sv.m_AudioPlay = false;
//		sv.name = "FooBar";
//		sv.Show ();

	}
	void OnDisable ()
	{
		Tools.hidden = false;
		//Selection.activeObject = target;
	}
//	void OnSceneGUI ()
//	{
//
////		SceneView.currentDrawingSceneView.in2DMode = true;
////		SceneView.currentDrawingSceneView.titleContent = "Foo";
//
//		MyObject myObject = (MyObject)target;
////		List<Vector3> points = myObject.points;
//
////		// Find bounds:
////		Vector3 boundsFrom = new Vector3 (-1f, -1f);
////		Vector3 boundsTo = new Vector3 (1f, 1f);
////
////		foreach (Vector3 pt in points) {
////			for (int axis = 0; axis < 2; axis++) {
////				if (pt [axis] < boundsFrom [axis]) {
////					boundsFrom [axis] = pt [axis];
////				}
////				if (pt [axis] > boundsTo [axis]) {
////					boundsTo [axis] = pt [axis];
////				}
////			}
////		}
////
////		// Bounds:
////		//int controlId = GUIUtility.GetControlID (FocusType.Passive);
////		Vector3 boundsMargin = new Vector3 (0.2f, 0.2f);
////		Vector3[] boundsWithMarginVertices = new Vector3[] {
////			new Vector3 (boundsFrom.x - boundsMargin.x, boundsFrom.y - boundsMargin.y),
////			new Vector3 (boundsTo.x + boundsMargin.x, boundsFrom.y - boundsMargin.y),
////			new Vector3 (boundsTo.x + boundsMargin.x, boundsTo.y + boundsMargin.y),
////			new Vector3 (boundsFrom.x - boundsMargin.x, boundsTo.y + boundsMargin.y),
////		};
////		Vector3[] boundsVertices = new Vector3[] {
////			new Vector3 (boundsFrom.x, boundsFrom.y),
////			new Vector3 (boundsTo.x, boundsFrom.y),
////			new Vector3 (boundsTo.x, boundsTo.y),
////			new Vector3 (boundsFrom.x, boundsTo.y),
////			
////		};
////		Color bgColor = new Color (0f, 0f, 0f, 0.2f);
////		Handles.DrawSolidRectangleWithOutline (boundsWithMarginVertices, bgColor, Color.white);
////
//		//Handles.DrawSolidRectangleWithOutline (boundsVertices, bgColor, bgColor);
//
//		// Draw origo:
//		Handles.color = Handles.yAxisColor;
//		Handles.DrawLine (new Vector3 (0f, -1f), new Vector2 (0f, 1f));
//
//		Handles.color = Handles.xAxisColor;
//		Handles.DrawLine (new Vector3 (-1f, 0f), new Vector2 (1f, 0f));
//
//		float handleSize = 0.1f;
////		float pickSize = handleSize * 1.25f;
//
//		int vertexCount = model.GetVertexCount ();
//		int lastVertexIndex = vertexCount - 1;
//		for (int i = 0; i < vertexCount; i++) {
//			Vector3 pt = model.GetVertexAt (i);
//
//			// Connect from previous:
//			if (i < lastPointIndex) {
//				Vector3 nextPt = points [i + 1];
//				Handles.color = Color.white;
////				Handles.DrawDottedLine (pt, nextPt, 0.1f);
//				Handles.DrawLine (pt, nextPt);
//			}
//
//			float worldHandleSize = HandleUtility.GetHandleSize (pt);
////
////			if (Handles.Button (pt, Quaternion.identity,
////			                    worldHandleSize * handleSize, worldHandleSize * pickSize, 
////			                    Handles.DotCap)) {
////				Debug.Log (Event.current.mousePosition);
////			}
//
//			pt = Handles.FreeMoveHandle (
//				pt, Quaternion.identity, worldHandleSize * handleSize, Vector3.one,
//				Handles.CubeCap);
//			if (GUI.changed) {
//				myObject.points [i] = pt;
//				EditorUtility.SetDirty (target);
//			}
//		}
////		if (Event.current.type == EventType.MouseUp) {
////			EditorApplication.sel
////			//GUIUtility.hotControl = controlId;
////			//Event.current.Use ();
////		}
//
//	}
}

