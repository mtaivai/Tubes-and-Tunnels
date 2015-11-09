// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

using Paths.MeshGenerator.Extruder;
using Paths.MeshGenerator.Extruder.Model;

namespace Paths.MeshGenerator.Extruder.Editor
{
	public class DieEditorMultiTool : DieEditorVertexMoveTool
	{
		protected const int ActionGroupGeneral = 0;
		protected const int ActionGroupVertex = 1;
		protected const int ActionGroupEdge = 2;

		private bool buttonlessDrag = false;
		private HashSet<int> tempVertices = new HashSet<int> ();
		private HashSet<int> tempEdges = new HashSet<int> ();


		public DieEditorMultiTool () : base()
		{
		}


		public override DieEditorSelectMode GetSupportedEdgeSelectMode ()
		{
			return DieEditorSelectMode.Multi;
		}




		protected static bool RequireSupportedModelOps (IDieEditorToolContext context, SupportedModelOps requiredOps)
		{
			if (context.IsMutableModel ()) {
				IMutableDieModel model = context.GetMutableDieModel ();
				SupportedModelOps ops = model.GetSupportedModelOps ();
				return (ops & requiredOps) == requiredOps;
			} else {
				return false;
			}
		}
		protected static bool RequireFocusedVertex (IDieEditorToolContext context)
		{
			return context.GetFocusedVertexIndex () >= 0;
		}
		protected static bool RequireFocusedVertexWithAddVertexAndEdgeSupport (IDieEditorToolContext context)
		{
			return RequireFocusedVertex (context) && RequireSupportedModelOps (context, SupportedModelOps.AddVertex | SupportedModelOps.AddEdge);
		}

		protected static bool RequireFocusedEdge (IDieEditorToolContext context)
		{
			return context.GetFocusedEdgeIndex () >= 0;
		}
		protected static  bool RequireFocusedVertexOrEdge (IDieEditorToolContext context)
		{
			return context.GetFocusedEdgeIndex () >= 0 || context.GetFocusedVertexIndex () >= 0;
		}

		protected static  bool RequireSupportsSplitVertices (IDieEditorToolContext context)
		{
			IDieModel model = context.GetDieModel ();
			return model.SupportsSplitVertices ();
		}
		protected static  bool RequireSupportsSplitVerticesAndMutableModel (IDieEditorToolContext context)
		{
			IDieModel model = context.GetDieModel ();
			return model.SupportsSplitVertices () && context.IsMutableModel ();
		}

		[EditorAction(ActionGroupGeneral, "Delete _x")]
		protected static void DeleteSelected (IDieEditorToolContext context)
		{
			// TODO implement this
//		model.RemoveVerticesAt (selectedVertexIndices);
//		selectedVertexIndices.Clear ();
//		
//		model.RemoveEdgesAt (false, selectedEdgeIndices);
//		selectedEdgeIndices.Clear ();
		}
		// NOTE: automatically mapped to DeleteSelected action by method name! Don't rename!
		protected static bool ValidateDeleteSelected (IDieEditorToolContext context)
		{
			return false; // TODO uncomment following, delete this line
			//return context.GetFocusedVertexIndex () >= 0 || context.GetSelectedVertices ().Length > 0 || context.GetFocusedEdgeIndex () >= 0 || context.GetSelectedEdges ().Length > 0;

		}


		[EditorAction(ActionGroupVertex, "Detach Vertex", "RequireFocusedVertex")]
		protected static void DetachVertex (IDieEditorToolContext context)
		{
			throw new NotImplementedException ("DetachVertex is not implemented (I'm not sure if it ever will be...)");
		}

		[EditorAction(ActionGroupVertex, "Mark Seam", "ValidateMarkSeam")]
		protected static void MarkSeam (IDieEditorToolContext context)
		{
			context.GetMutableDieModel ().SetSeamAt (context.GetFocusedVertexIndex (), true);
		}
		protected static bool ValidateMarkSeam (IDieEditorToolContext context)
		{
			IDieModel model = context.GetDieModel ();
			return RequireSupportsSplitVerticesAndMutableModel (context) && RequireFocusedVertex (context) && !model.IsSeamAt (context.GetFocusedVertexIndex ());
		}

		[EditorAction(ActionGroupVertex, "Clear Seam", "ValidateClearSeam")]
		protected static void ClearSeam (IDieEditorToolContext context)
		{
			context.GetMutableDieModel ().SetSeamAt (context.GetFocusedVertexIndex (), false);
		}
		protected static bool ValidateClearSeam (IDieEditorToolContext context)
		{
			IDieModel model = context.GetDieModel ();
			return RequireSupportsSplitVerticesAndMutableModel (context) && RequireFocusedVertex (context) && model.IsSeamAt (context.GetFocusedVertexIndex ());
		}


		[EditorAction(ActionGroupVertex, "Mark Sharp Edge", "ValidateMarkSharpEdge")]
		protected static void MarkSharpEdge (IDieEditorToolContext context)
		{
			context.GetMutableDieModel ().SetSharpVertexAt (context.GetFocusedVertexIndex (), true);
		}
		protected static bool ValidateMarkSharpEdge (IDieEditorToolContext context)
		{
			IDieModel model = context.GetDieModel ();
			return RequireFocusedVertex (context) && RequireSupportsSplitVerticesAndMutableModel (context) && !model.IsSharpVertexAt (context.GetFocusedVertexIndex ());
		}

		[EditorAction(ActionGroupVertex, "Clear Sharp Edge", "ValidateClearSharpEdge")]
		protected static void ClearSharpEdge (IDieEditorToolContext context)
		{
			context.GetMutableDieModel ().SetSharpVertexAt (context.GetFocusedVertexIndex (), false);
		}
		protected static bool ValidateClearSharpEdge (IDieEditorToolContext context)
		{
			return RequireFocusedVertex (context) && RequireSupportsSplitVerticesAndMutableModel (context) && context.GetDieModel ().IsSharpVertexAt (context.GetFocusedVertexIndex ());
		}


		[EditorAction(ActionGroupVertex, "Slide Vertex", "RequireFocusedVertex")]
		protected static void SlideVertex (IDieEditorToolContext context)
		{
			throw new NotImplementedException ("SlideVertex is not yet implemented. Sorry.");
		}

		[EditorAction(ActionGroupVertex, "Extrude Vertex _e", "RequireFocusedVertexWithAddVertexAndEdgeSupport")]
		protected void ExtrudeVertex (IDieEditorToolContext context)
		{
			BeginExtrude (context);
		}

		[EditorAction(ActionGroupVertex, "Insert Vertex _i")]
		protected static void InsertVertex (IDieEditorToolContext context)
		{
			Vector3 pos = context.GetCursorModelPos ();
			context.GetMutableDieModel ().AddVertex (pos);
		}
		protected static bool ValidateInsertVertex (IDieEditorToolContext context)
		{
			return context.GetFocusedVertexIndex () < 0 && RequireSupportedModelOps (context, SupportedModelOps.AddVertex);
		}

		//BeginExtrude (context);

		[EditorAction(ActionGroupEdge, "Split Edge Here", "RequireFocusedEdge")]
		protected static void SplitEdgeHere (IDieEditorToolContext context)
		{


			context.GetMutableDieModel ().BatchOperation ("Split Edge", (model) => {
				int edgeIndex = context.GetFocusedEdgeIndex ();

				Edge edge = model.GetEdgeAt (edgeIndex);
				int fromIndex = edge.GetFromVertexIndex ();
				int toIndex = edge.GetToVertexIndex ();


				// 1. Add a new vertex
				// 2. Remap the edge to end to the new vertex
				// 3. Insert new edge after the remapped edge

				// Remove the edge
				//model.RemoveEdgeAt (edgeIndex, false);

				IMutableDieModel mutableModel = (IMutableDieModel)model;

				// Add new Vertex
				Vector3 splitPt = context.InverseTransformModelPoint (context.GetProjectedPointOnFocusedEdge ());
				int newVertexIndex = mutableModel.AddVertex (splitPt);

				// Reroute the split edge:
				edge.SetToVertexIndex (newVertexIndex);
				mutableModel.SetEdgeAt (edgeIndex, edge);

				// Add new edge from the split point:
				mutableModel.InsertEdge (edgeIndex + 1, newVertexIndex, toIndex);
//
//				int newBeforeEdgeIndex = model.AddEdge (fromIndex, newVertexIndex);
//				int newAfterEdgeIndex = model.AddEdge (newVertexIndex, toIndex);
//
				// Calculate distance from the begin of the (old) edge to the split point
				Vector3 p0 = model.GetVertexAt (fromIndex);
				Vector3 p1 = model.GetVertexAt (newVertexIndex);
				float distFromBeginToSplit = (p1 - p0).magnitude;

				// Total edge length:
				float edgeLength = (p1 - p0).magnitude;

				// Interpolate stuff
				Vector2 uv0 = model.GetUvAt (fromIndex, edgeIndex);
				Vector2 uv1 = model.GetUvAt (toIndex, edgeIndex + 1);
				Vector2 uv = (uv1 - uv0) * distFromBeginToSplit / edgeLength;
				mutableModel.SetUvAt (newVertexIndex, uv);

			});
		}



		[EditorAction(ActionGroupEdge, "Switch Direction of Edge(s)", "RequireFocusedEdge")]
		protected static void SwitchEdgeDirection (IDieEditorToolContext context)
		{
			int ei = context.GetFocusedEdgeIndex ();
			if (ei >= 0) {
				IMutableDieModel model = context.GetMutableDieModel ();
				Edge edge = model.GetEdgeAt (ei);
				int from = edge.GetFromVertexIndex ();
				int to = edge.GetToVertexIndex ();
				edge.SetFromVertexIndex (to);
				edge.SetToVertexIndex (from);
				model.SetEdgeAt (ei, edge);
			}
		}


		[EditorAction(ActionGroupEdge, "Select Connected Edge(s)", "RequireFocusedVertexOrEdge")]
		protected static void SelectConnectedEdges (IDieEditorToolContext context)
		{
			DoSelectConnectedEdges (context, true, true, true);
			context.GetEditor ().Repaint ();
		}


		[EditorAction(ActionGroupEdge, "Select Connected Edge(s) From Here", "RequireFocusedVertexOrEdge")]
		protected static void SelectConnectedEdgesFromHere (IDieEditorToolContext context)
		{
			DoSelectConnectedEdges (context, true, false, true);
			context.GetEditor ().Repaint ();
		}

		[EditorAction(ActionGroupEdge, "Select Connected Edge(s) To Here", "RequireFocusedVertexOrEdge")]
		protected static void SelectConnectedEdgesToHere (IDieEditorToolContext context)
		{
			DoSelectConnectedEdges (context, true, true, false);
			context.GetEditor ().Repaint ();
		}

		private static void DoSelectConnectedEdges (IDieEditorToolContext context, bool stopOnSeams, bool backwards, bool forwards)
		{
			FindEdgesFlags findFlags = FindEdgesFlags.IncludeDistantEdges;
			if (stopOnSeams) {
				findFlags |= FindEdgesFlags.StopOnSplitVertices;
			}
			if (!backwards) {
				findFlags |= FindEdgesFlags.DontWalkBackwards;
			}
			if (!forwards) {
				findFlags |= FindEdgesFlags.DontWalkForwards;
			}
			int focusedVertex = context.GetFocusedVertexIndex ();
			if (focusedVertex >= 0) {
				IDieModel model = context.GetDieModel ();
				if (model is IDieModelGraphSupport) {
					List<int> connectedEdges = ((IDieModelGraphSupport)model).FindConnectedEdgeIndices (focusedVertex, findFlags);
					foreach (int ei in connectedEdges) {
						Edge e = model.GetEdgeAt (ei);
						context.SetEdgeSelected (ei, true, true);
						context.SetVertexSelected (e.GetFromVertexIndex (), true, true);
						context.SetVertexSelected (e.GetToVertexIndex (), true, true);

					}
					context.GetEditor ().Repaint ();
				}
			}
			int focusedEdge = context.GetFocusedEdgeIndex ();
			if (focusedEdge >= 0) {
				IDieModel model = context.GetDieModel ();
				if (model is IDieModelGraphSupport) {
					Edge e = model.GetEdgeAt (focusedEdge);
					int vFrom = backwards ? e.GetFromVertexIndex () : e.GetToVertexIndex ();
					//				int vTo = e.GetToVertexIndex ();
					List<int> connectedEdges = new List<int> ();
					connectedEdges.AddRange (((IDieModelGraphSupport)model).FindConnectedEdgeIndices (vFrom, findFlags));
					//				connectedEdges.AddRange (model.FindConnectedEdgeIndices (vTo, true));
					
					foreach (int ei in connectedEdges) {
						context.SetEdgeSelected (ei, true, true);
						Edge e2 = model.GetEdgeAt (ei);
						context.SetVertexSelected (e2.GetFromVertexIndex (), true, true);
						context.SetVertexSelected (e2.GetToVertexIndex (), true, true);
					}
					context.GetEditor ().Repaint ();
				}
			}
		}


		public override bool MouseDown (IDieEditorToolContext context)
		{
			bool handled = false;
			Event e = Event.current;
			if (e.button == 0) {
				if (buttonlessDrag) {
					// End the "buttonless" drag mode
					EndDrag (context);
					buttonlessDrag = false;
					tempEdges.Clear ();
					tempVertices.Clear ();
					handled = true;
				}
			} else if (e.button == 1) {
				handled = ShowContextMenu (context);
			}
			if (!handled) {
				handled = base.MouseDown (context);
			}
			return handled;
		}
		public override void MouseMove (IDieEditorToolContext context)
		{
			if (buttonlessDrag) {
				Drag (context);
			} else {
				base.MouseMove (context);
			}
		}
		public override void MouseUp (IDieEditorToolContext context)
		{
			base.MouseUp (context);
		}
		public override bool Key (IDieEditorToolContext context)
		{
			bool used = false;
			Event e = Event.current;
			if (draggingVertexIndex < 0) {
				if (e.keyCode == KeyCode.G) {
					used = BeginGrab (context);

				} else if (e.keyCode == KeyCode.E) {
					// Extrude / Edge

					used = BeginExtrude (context);
				}
			} else {
				used = false;
			}
			if (!used) {
				used = base.Key (context);
			}
			return used;
		}

		public override void Cancel (IDieEditorToolContext context)
		{
			if (context.IsMutableModel ()) {
				IMutableDieModel model = context.GetMutableDieModel ();
				model.RemoveEdgesAt (false, tempEdges);
			
				tempEdges.Clear ();

				model.RemoveVerticesAt (tempVertices);
				tempVertices.Clear ();
			}

			buttonlessDrag = false;
			base.Cancel (context);
		}

		private bool BeginGrab (IDieEditorToolContext context)
		{
			bool begun;
			// Move / Grab
			List<int> selectedVertices = new List<int> (context.GetSelectedVertices ());
		
			int focusedVertex = context.GetFocusedVertexIndex ();
			if (focusedVertex >= 0 && !selectedVertices.Contains (focusedVertex)) {
				selectedVertices.Insert (0, focusedVertex);
			}
//			int focusedEdge = context.GetFocusedEdgeIndex ();
//			if (focusedEdge >= 0) {
//				// Edge grabbed; include both of its vertices:
//
//				Edge e = context.GetDieModel ().GetEdgeAt (focusedEdge);
//				if (!selectedVertices.Contains (e.GetFromVertexIndex ())) {
//					selectedVertices.Add (e.GetFromVertexIndex ());
//				}
//				if (!selectedVertices.Contains (e.GetToVertexIndex ())) {
//					selectedVertices.Add (e.GetToVertexIndex ());
//				}
//			}

			if (selectedVertices.Count > 0) {
				BeginDrag (context, selectedVertices [0], true);
			
				buttonlessDrag = true;
				begun = true;
				context.GetEditor ().Repaint ();
			} else {
				buttonlessDrag = false;
				begun = false;
			}
			return begun;
		}
		private bool BeginExtrude (IDieEditorToolContext context)
		{
			bool begun;

			tempEdges.Clear ();
			tempVertices.Clear ();

			List<int> selectedVertices = new List<int> (context.GetSelectedVertices ());
			int focusedVertex = context.GetFocusedVertexIndex ();
			if (focusedVertex >= 0 && !selectedVertices.Contains (focusedVertex)) {
				selectedVertices.Insert (0, focusedVertex);
			}

			if (selectedVertices.Count > 0 && context.IsMutableModel ()) {
				// Create new Vertex here
				IMutableDieModel model = context.GetMutableDieModel ();
			
				// Use focused vertex or the first selected vertex as a reference
				Vector3 refPos = model.GetVertexAt (selectedVertices [0]);
			
				// Create clone of all selected:
				Vector3 cursorPos = context.GetCursorModelPos ();
			
				int firstAddedIndex = -1;
				bool firstSel = true;
				foreach (int i in selectedVertices) {
					Vector3 pos = cursorPos + model.GetVertexAt (i) - refPos;
					int addedVertex = model.AddVertex (pos);
					tempVertices.Add (addedVertex);

					// Connect edge from selected vertices
					int addedEdge = model.AddEdge (i, addedVertex);
					tempEdges.Add (addedEdge);

					// Select newly added vertices, for first use 'single' selection model to
					// deselect all the previously selected items
					context.SetVertexSelected (addedVertex, true, firstSel == false);
					if (firstSel) {
						firstAddedIndex = addedVertex;
						firstSel = false;
					}
				
				}
			
				//					int addedVertex = model.AddVertex (context.GetCursorModelPos ());
			
				// Set focus to
				//context.GetDieEditor().SetFocusedVertexIndex(addedVertex);
			
				BeginDrag (context, firstAddedIndex, true);
			
				buttonlessDrag = true;
				begun = true;
			} else {
				begun = false;
			}
			return begun;
		}
	}
}
