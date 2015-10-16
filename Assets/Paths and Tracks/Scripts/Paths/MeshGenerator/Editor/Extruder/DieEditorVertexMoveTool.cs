// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

using Paths.MeshGenerator.Extruder;
using Paths.MeshGenerator.Extruder.Model;

namespace Paths.MeshGenerator.Extruder.Editor
{
// TODO maybe this whole thing could be refactored to DieEditorMultiTool?
	public class DieEditorVertexMoveTool : DieEditorTool
	{
		private bool drawingMarqueSelRect = false;
		private Rect marqueSelRect = new Rect ();
		private HashSet<int> selectedVerticesBeforeMarqueSel = new HashSet<int> ();
		private HashSet<int> selectedEdgesBeforeMarqueSel = new HashSet<int> ();

		protected int draggingVertexIndex = -1; 
		protected int mergeDraggedVertexToIndex = -1;
		private Dictionary<int, Vector3> draggingVertexPositions = new Dictionary<int, Vector3> ();

		public DieEditorVertexMoveTool () : base()
		{
		}


		public override DieEditorSelectMode GetSupportedVertexSelectMode ()
		{
			return DieEditorSelectMode.Multi;
		}
//	public override SelectMode GetSupportedEdgeSelectMode ()
//	{
//		return SelectMode.Multi;
//	}

		public override void OnGUI (IDieEditorToolContext context)
		{
			if (drawingMarqueSelRect) {
				Color col = Color.cyan;
				Color bgCol = col;
				bgCol.a = 0.2f;
				Handles.color = col;
				Handles.DrawSolidRectangleWithOutline (
				new Vector3[] {
					new Vector3 (marqueSelRect.xMin, marqueSelRect.yMin),
					new Vector3 (marqueSelRect.xMax, marqueSelRect.yMin),
					new Vector3 (marqueSelRect.xMax, marqueSelRect.yMax),
					new Vector3 (marqueSelRect.xMin, marqueSelRect.yMax),
				}, 
				bgCol, 
				col);
			}
		}

		public override bool BeginDrag (IDieEditorToolContext context)
		{
			int focusedVertex = context.GetFocusedVertexIndex ();
			if (!BeginDrag (context, focusedVertex, false)) {
				// Begin marque selection
				drawingMarqueSelRect = true;
				marqueSelRect.size = Vector2.zero;
				marqueSelRect.position = context.GetCursorCanvasPos ();

				selectedVerticesBeforeMarqueSel.Clear ();
				foreach (int vi in context.GetSelectedVertices()) {
					selectedVerticesBeforeMarqueSel.Add (vi);
				}

				selectedEdgesBeforeMarqueSel.Clear ();
				foreach (int ei in context.GetSelectedEdges()) {
					selectedEdgesBeforeMarqueSel.Add (ei);
				}

				return true;
			} else {
				return true;
			}
		}
		protected bool BeginDrag (IDieEditorToolContext context, int focusedVertex, bool forceMultiSel)
		{
			draggingVertexPositions.Clear ();

			if (focusedVertex >= 0) {
				context.SetVertexSelected (focusedVertex, true, forceMultiSel || Event.current.control | Event.current.shift);
			
				//			selectedVertexIndices.Add (focusedVertexIndex);
				this.draggingVertexIndex = focusedVertex;
				mergeDraggedVertexToIndex = -1;
			
				// Create drag positions for selected vertices
				foreach (int selIndex in context.GetSelectedVertices()) {
					draggingVertexPositions [selIndex] = context.GetDieModel ().GetVertexAt (selIndex);
				}
				return true;
			} else {
				draggingVertexIndex = -1;
				mergeDraggedVertexToIndex = -1;
				return false;
			}
		}
		public override void Drag (IDieEditorToolContext context)
		{
			if (drawingMarqueSelRect) {
				Vector3 cursor = context.GetCursorCanvasPos ();
				marqueSelRect.xMax = cursor.x;
				marqueSelRect.yMax = cursor.y;

				Rect normalRect = new Rect ();
				normalRect.x = Mathf.Min (marqueSelRect.xMin, marqueSelRect.xMax);
				normalRect.y = Mathf.Min (marqueSelRect.yMin, marqueSelRect.yMax);
				normalRect.width = Mathf.Abs (marqueSelRect.width);
				normalRect.height = Mathf.Abs (marqueSelRect.height);

				Rect modelRect = new Rect ();
				modelRect.min = context.InverseTransformModelPoint (new Vector3 (normalRect.xMin, normalRect.yMax));
				modelRect.size = context.InverseTransformModelVector (new Vector3 (normalRect.width, -normalRect.height));

				IDieModel model = context.GetDieModel ();
				int vcount = model.GetVertexCount ();
				for (int i = 0; i < vcount; i++) {
					Vector3 vpos = model.GetVertexAt (i);
					if (modelRect.Contains (vpos)) {
						context.SetVertexSelected (i, true, true);
					} else if (!selectedVerticesBeforeMarqueSel.Contains (i)) {
						context.SetVertexSelected (i, false, true);
					}
				}

				int ecount = model.GetEdgeCount ();
				for (int i = 0; i < ecount; i++) {
					Vector3[] ev = model.GetEdgeVertices (i);
					if (modelRect.Contains (ev [0]) && modelRect.Contains (ev [1])) {
						context.SetEdgeSelected (i, true, true);
					} else if (!selectedEdgesBeforeMarqueSel.Contains (i)) {
						context.SetEdgeSelected (i, false, true);
					}
				}

				context.GetEditor ().Repaint ();
			} else if (this.draggingVertexIndex >= 0) {
				// Delta
				IDieModel model = context.GetDieModel ();

				Vector3 newRefPos;
				if (context.GetFocusedVertexIndex () >= 0) {
					// Merge with the focused vertex
					mergeDraggedVertexToIndex = context.GetFocusedVertexIndex ();
					newRefPos = model.GetVertexAt (mergeDraggedVertexToIndex);
					context.GetEditor ().Repaint ();
				} else {
					newRefPos = context.GetCursorModelPos ();
					mergeDraggedVertexToIndex = -1;
				}
				Vector3 delta = newRefPos - model.GetVertexAt (draggingVertexIndex);
				List<int> draggedIndices = new List<int> (draggingVertexPositions.Keys);
				foreach (int draggedIndex in draggedIndices) {
					//model.SetVertexAt (kvp.Key, kvp.Value);
					Vector3 thisRefPos = model.GetVertexAt (draggedIndex);
					draggingVertexPositions [draggedIndex] = thisRefPos + delta;
					//				draggingVertexPositions [draggingVertexIndex] = newRefPos;
				
				}
			}
		
			context.GetEditor ().Repaint ();
		}
		public override void EndDrag (IDieEditorToolContext context)
		{
			if (drawingMarqueSelRect) {
				drawingMarqueSelRect = false;
				marqueSelRect = new Rect ();
				context.GetEditor ().Repaint ();
			} else if (draggingVertexIndex >= 0 && context.IsMutableModel ()) {
				IMutableDieModel model = context.GetMutableDieModel ();

				int[] indices = new int[draggingVertexPositions.Count];
				Vector3[] positions = new Vector3[indices.Length];
				int i = 0;
				foreach (KeyValuePair<int, Vector3> kvp in draggingVertexPositions) {
					indices [i] = kvp.Key;
					positions [i] = kvp.Value;
					i++;
				}
//			model.SetVertexAt (kvp.Key, kvp.Value);
				model.SetVerticesAt (indices, positions);

				if (mergeDraggedVertexToIndex >= 0 && model is IDieModelGraphSupport) {
					// Remove the dragged vertex, merge it with the target vertex

					// Reconnect edges to/from dragged
					List<int> connectedEdgesIndices = ((IDieModelGraphSupport)model).FindConnectedEdgeIndices (draggingVertexIndex, FindEdgesFlags.None);
					foreach (int edgeIndex in connectedEdgesIndices) {
						Edge e = model.GetEdgeAt (edgeIndex);
						if (e.GetFromVertexIndex () == draggingVertexIndex) {
							e.SetFromVertexIndex (mergeDraggedVertexToIndex);
						}
						if (e.GetToVertexIndex () == draggingVertexIndex) {
							e.SetToVertexIndex (mergeDraggedVertexToIndex);
						}
						model.SetEdgeAt (edgeIndex, e);
					}
					model.RemoveVertexAt (draggingVertexIndex);

				}
			}
			draggingVertexPositions.Clear ();
			draggingVertexIndex = -1;
			mergeDraggedVertexToIndex = -1;
			context.GetEditor ().Repaint ();
		}
		public override void Cancel (IDieEditorToolContext context)
		{
			//drawingNewEdge = false;
			draggingVertexIndex = -1;
			draggingVertexPositions.Clear ();
			context.GetEditor ().Repaint ();
		}
	
		public override void VertexDeleted (int vertexIndex)
		{
			if (draggingVertexIndex == vertexIndex) {
				draggingVertexIndex = -1;
				draggingVertexPositions.Clear ();
			} else if (draggingVertexPositions.ContainsKey (vertexIndex)) {
				draggingVertexPositions.Remove (vertexIndex);
			}
		}
		public override bool IsVertexInToolContext (int vertexIndex)
		{
			return draggingVertexIndex >= 0 && draggingVertexPositions.ContainsKey (vertexIndex);
		}
		public override Vector3 GetVertexToolContextPosition (int vertexIndex)
		{
			return draggingVertexPositions [vertexIndex];
		}


	}
}

