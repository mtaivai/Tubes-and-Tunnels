// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Paths.MeshGenerator.Extruder.Editor
{
	public class DieEditorInsertVerticesTool : DieEditorTool
	{
		private bool allowInsertEdges = true;

		private int edgeStartIndex = -1;
		private bool edgeStartIsNew = false;
		private Vector3 edgeStartPos = Vector3.zero;
		private bool drawingNewEdge = false;

		public DieEditorInsertVerticesTool () : base()
		{
		}


		public override DieEditorSelectMode GetSupportedVertexSelectMode ()
		{
			// Allow selection in edge drawing mode
			return allowInsertEdges ? DieEditorSelectMode.Single : DieEditorSelectMode.None;
		}

		public override void Cancel (IDieEditorToolContext context)
		{
			this.drawingNewEdge = false;
			this.edgeStartIndex = -1;
			context.GetEditor ().Repaint ();
		}

		public override bool MouseDown (IDieEditorToolContext context)
		{
			int focused = context.GetFocusedVertexIndex ();

			if (edgeStartIndex >= 0) {
				// Draw edge to here
			}

			if (focused >= 0) {

				if (allowInsertEdges) {
					//edgeStartIndex = focused;
					return true;
				} else {
					// let the editor handle selection
					return false;
				}
			} else {
				// Insert new vertex here
				//DieModel model = context.GetDieModel ();
				//int addedIndex = model.AddVertex (context.GetCursorModelPos ());
				//context.SetVertexSelected (addedIndex, true, Event.current.control);

				//edgeStartIndex = addedIndex;
//			editor.Repaint();
				return true;
			}
		}
		public override bool BeginDrag (IDieEditorToolContext context)
		{
			if (allowInsertEdges) {
			
				int focusedVertex = context.GetFocusedVertexIndex ();

				if (focusedVertex >= 0) {
					// Begin drawing a new edge from the selected point
					drawingNewEdge = true;
					edgeStartIndex = focusedVertex;
					edgeStartIsNew = false;
					edgeStartPos = context.GetDieModel ().GetVertexAt (edgeStartIndex);
					context.SetVertexSelected (edgeStartIndex, true, Event.current.control);
				} else {
					// Insert new vertex here and start drawing a new edge
					//DieModel model = context.GetDieModel ();
					//int addedIndex = model.AddVertex (context.GetCursorModelPos ());
					drawingNewEdge = true;
					edgeStartIndex = -1;
					edgeStartPos = context.GetCursorModelPos ();
				}

				return true;
			} else {
				return false;
			}
		}
		public override void EndDrag (IDieEditorToolContext context)
		{
			if (drawingNewEdge && context.IsMutableModel ()) {
				int focusedVertex = context.GetFocusedVertexIndex ();
				int edgeEndIndex;
				if (focusedVertex >= 0) {
					// Connect to the focused vertex
					edgeEndIndex = focusedVertex;
				} else {
					// Create a new vertex and connect the edge to it
					edgeEndIndex = -1;
				}

				if (edgeStartIndex < 0 && edgeEndIndex < 0 || edgeStartIndex != edgeEndIndex) {
					Debug.LogFormat ("Edge: {0} --> {1}", edgeStartIndex, edgeEndIndex);

					if (edgeStartIndex < 0) {
						// Create a new vertex in begin pos
						edgeStartIndex = context.GetMutableDieModel ().AddVertex (edgeStartPos);
					}
					if (edgeEndIndex < 0) {
						edgeEndIndex = context.GetMutableDieModel ().AddVertex (context.GetCursorModelPos ());
					}

					context.GetMutableDieModel ().AddEdge (edgeStartIndex, edgeEndIndex);

					context.SetVertexSelected (edgeEndIndex, true, Event.current.control);
				}

				edgeStartIndex = -1;
				drawingNewEdge = false;
			}
		}
		public override void Drag (IDieEditorToolContext context)
		{

		}
		public override void OnGUI (IDieEditorToolContext context)
		{
			if (drawingNewEdge) {
				Vector3 pt0 = context.TransformModelPoint (edgeStartPos);

				Vector3 pt1;
				int focused = context.GetFocusedVertexIndex ();
				if (focused >= 0) {
					pt1 = context.TransformModelPoint (context.GetDieModel ().GetVertexAt (focused));
				} else {
					pt1 = context.GetCursorCanvasPos ();
				}


				context.GetDieEditor ().DrawEdge (pt0, pt1, false, true, true, "");
				context.GetDieEditor ().Repaint ();
			}
		}
//	private void DrawNewEdge ()
//	{
//
//	}
	}
}
