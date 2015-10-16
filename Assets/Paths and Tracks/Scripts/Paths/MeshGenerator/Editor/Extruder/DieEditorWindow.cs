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

//public static class EventExtensions
//{
//	public static bool HasModifier (this Event e, EventModifiers modifiers)
//	{
//		return modifiers == (modifiers & e.modifiers);
//	}
//}

namespace Paths.MeshGenerator.Extruder.Editor
{


// TODO tool support:
/*
- When enabled, store the previous Tools.current settings (maybe Tools.viewTool too)
- When disabled, restore the previously stored Tool
- Implement own tool selection
 - Q = pan
 - W = move
 - others
 Set mouse cursor based on the current tool
 */
	public class DieEditorWindow : DrawingEditorWindow
	{
		[MenuItem ("Window/Die Editor")]
		public static void MenuShowDieEditorWindow ()
		{
			EditorWindow.GetWindow<DieEditorWindow> ();
		}

#if !FOOBAR
		private IDieModel model;
		private UnityEngine.Object modelContainer;

		private static float vertexHandlePickSize = 0.75f;
		private static float vertexHandleSize = 0.5f;

		private static float faceNormalVectorLength = 1.0f;
		private static Color faceNormalVectorColor = Color.cyan;

		private static float vertexNormalVectorLength = 1.0f;
		private static Color vertexNormalVectorColor = new Color (0.7f, 0f, 0.7f, 0.7f);
		private static Color focusedVertexNormalVectorColor = new Color (1f, 0.2f, 1f, 1.0f);

		private static Color seamVertexColor = Color.red;
		private static Color sharpVertexColor = Color.cyan;

		private static Color vertexColor = new Color (0.9f, 0.9f, 0.9f, 1.0f);
		private static Color focusedVertexColor = new Color (0.7f, 0.7f, 1.0f, 1.0f);
		private static Color selectedVertexColor = Handles.selectedColor;

		private static Color edgeColor = vertexColor;
		private static Color focusedEdgeColor = focusedVertexColor;
		private static Color selectedEdgeColor = selectedVertexColor;


		private static float edgeHitThreshold = 1.5f;

//		private int focusedVertexIndex = -1;
//		private HashSet<int> selectedVertexIndices = new HashSet<int> ();

//		private int focusedEdgeIndex = -1;
//		private HashSet<int> selectedEdgeIndices = new HashSet<int> ();
		private Vector3 projectedPointOnFocusedEdge;
		private Vector3 cursorToProjectedPointOnFocusedEdgeVector; 

		private bool drawingNewEdge = false;
		private Vector3 newEdgeStartPos;
		private Vector3 newEdgeEndPos;

		//private IDieModelSelectionSupport selectionSupport = new DefaultDieModelSelectionSupport ();

		public DieEditorWindow ()
		{
			CurrentModelTool = new DieEditorMultiTool ();
		}

		protected IDieModel DieModel {
			get {
				return model;
			}
		}

		protected new DieEditorTool CurrentModelTool {
			get {
				IDrawingEditorTool t = base.CurrentModelTool;
				if (t == NoDrawingEditorTool.Instance) {
					t = DieEditorTool.None;
				}
				return (DieEditorTool)t;
			}
			set {
				base.CurrentModelTool = value;
			}
		}

		protected virtual IDieModelSelectionSupport SelectionSupport {
			get {
				//return selectionSupport;
				IDieModel model = this.DieModel;
				if (null != model && model is IDieModelEditorSupport) {
					return ((IDieModelEditorSupport)model).GetDieModelSelectionSupport ();
				} else {
					return NoDieModelSelectionSupport.Instance;
				}
			}
		}


		private void DieModelChangedEvent (DieModelChangedEventArgs e)
		{
			if (e.IsBeforeEvent) {
				string edgesTitle = e.Indices.Length == 1 ? "Edge" : "Edges";
				string verticesTitle = e.Indices.Length == 1 ? "Vertex" : "Vertices";

				string reasonTitle;
				switch (e.Reason) {
				case DieModelChangedEventArgs.EventReason.RefreshModel:
					reasonTitle = "Refresh Model";
					break;
				case DieModelChangedEventArgs.EventReason.BatchOperation:
					reasonTitle = e.BatchOperationName;
					break;
				case DieModelChangedEventArgs.EventReason.EdgesAdded:
					reasonTitle = "Add " + edgesTitle;
					break;
				case DieModelChangedEventArgs.EventReason.EdgesModified:
					reasonTitle = "Modify " + edgesTitle;
					break;
				case DieModelChangedEventArgs.EventReason.EdgesRemoved:
					reasonTitle = "Remove " + edgesTitle;
					break;
				case DieModelChangedEventArgs.EventReason.VerticesAdded:
					reasonTitle = "Add " + verticesTitle;
					break;
				case DieModelChangedEventArgs.EventReason.VerticesModified:
					reasonTitle = "Modify " + verticesTitle;
					break;
				case DieModelChangedEventArgs.EventReason.VerticesRemoved:
					reasonTitle = "Remove " + verticesTitle;
					break;
				default:
					reasonTitle = "Modify Die Model";
					break;
				}
				if (e.IsUndoable) {
					Undo.RecordObject (modelContainer, reasonTitle);
					EditorUtility.SetDirty (modelContainer);
				}
				OnDieModelChanging (e);
			} else {
				if (e.Reason == DieModelChangedEventArgs.EventReason.EdgesRemoved) {
					foreach (int edgeIndex in e.Indices) {
						CurrentModelTool.EdgeDeleted (edgeIndex);
					}
				} else if (e.Reason == DieModelChangedEventArgs.EventReason.VerticesRemoved) {
					foreach (int vertexIndex in e.Indices) {
						CurrentModelTool.VertexDeleted (vertexIndex);
					}
				}
				OnDieModelChanged (e);
				Repaint ();
			}
		}
		protected virtual void OnDieModelChanging (DieModelChangedEventArgs e)
		{

		}

		protected virtual void OnDieModelChanged (DieModelChangedEventArgs e)
		{
			
		}

		private void DieModelSelectionChanged (DieModelSelectionEventArgs e)
		{
			OnDieModelSelectionChanged (e);
		}
		protected virtual void OnDieModelSelectionChanged (DieModelSelectionEventArgs e)
		{
			Repaint ();
		}

		protected virtual IDieModel GetDieModelFromContainer (IDieModelContainer container)
		{
			return container.GetDieModel ();
		}

		private void ModelAttached (IDieModel model)
		{
			if (null != model) {
				if (model is IMutableDieModel) {
					((IMutableDieModel)model).AddDieModelChangeHandler (DieModelChangedEvent);
				}
				try {
					SelectionSupport.AttachModel (model);
				} catch (Exception e) {
					Debug.LogError ("An exception in SelectionSupport.AttachModel: " + e);
				}
				try {
					OnModelAttached (model);
				} catch (Exception e) {
					Debug.LogError ("An exception in OnModelAtached: " + e);
				}
			}
			Repaint ();
		}

		protected virtual void OnModelAttached (IDieModel model)
		{

		}

		private void ModelDetached (IDieModel model)
		{
			if (null != model) {
				if (model is IMutableDieModel) {
					((IMutableDieModel)model).RemoveDieModelChangeHandler (DieModelChangedEvent);
				}
				try {
					SelectionSupport.DetachModel (model);
				} catch (Exception e) {
					Debug.LogError ("An exception in SelectionSupport.DetachModel: " + e);
				}
				try {
					OnModelDetached (model);
				} catch (Exception e) {
					Debug.LogError ("An exception in OnModelDetached: " + e);
				}

			}
			Repaint ();
		}
		protected virtual void OnModelDetached (IDieModel model)
		{
			
		}


		void OnSelectionChange ()
		{
			SetModelBySelection ();
			Repaint ();
		}

		private void SetModelBySelection ()
		{
			IDieModel prevModel = this.model;
			GameObject gameObject = Selection.activeGameObject;
			if (null == gameObject) {
				model = null;
				modelContainer = null;
			} else {
				IDieModelContainer dmc = gameObject.GetComponent<IDieModelContainer> ();
				if (null != dmc) {
					model = GetDieModelFromContainer (dmc);
					if (null != model) {
						modelContainer = (UnityEngine.Object)dmc;
					} else {
						modelContainer = null;
					}
				} else {
					model = null;
					modelContainer = null;
				}
			}
			if (null != model) {
				// Got a model from selection
				if (prevModel != model) {
					// Selection changed
					ModelDetached (prevModel);
				}
				ModelAttached (model);
			}
		}

		protected override void OnEnabled ()
		{
			SelectionSupport.AddSelectionChangedChangeHandler (DieModelSelectionChanged);
			Undo.undoRedoPerformed += UndoRedoPerformed;

			SetModelBySelection ();

		}
		protected override void OnDisabled ()
		{
			SelectionSupport.RemoveSelectionChangedChangeHandler (DieModelSelectionChanged);
			Undo.undoRedoPerformed -= UndoRedoPerformed;

			try {
				if (null != model) {
					ModelDetached (model);
				}
			} finally {
				model = null;
				modelContainer = null;
			}
		}

		private void UndoRedoPerformed ()
		{
			// Fire event
			if (null != model && model is IDieModelEditorSupport) {
				((IDieModelEditorSupport)model).FireRefreshEvent ();
			}
		}

		protected class DieEditorToolContext : AbstractDrawingEditorToolContext, IDieEditorToolContext
		{
			private DieEditorTool tool;
			public DieEditorToolContext (DieEditorWindow editor, DieEditorTool tool) : base(editor)
			{
				this.tool = tool;
			}
			public DieEditorWindow GetDieEditor ()
			{
				return (DieEditorWindow)base.GetEditor ();
			}
			public override object GetModel ()
			{
				return GetDieEditor ().model;
			}
			public IDieModel GetDieModel ()
			{
				return (IDieModel)GetModel ();
			}
			public bool IsMutableModel ()
			{
				return GetModel () is IMutableDieModel;
			}
			public IMutableDieModel GetMutableDieModel ()
			{
				return (IMutableDieModel)GetModel ();
			}

			public int GetFocusedVertexIndex ()
			{
				return GetDieEditor ().SelectionSupport.GetFocusedVertexIndex ();
			}

			public int GetFocusedEdgeIndex ()
			{
				return GetDieEditor ().SelectionSupport.GetFocusedEdgeIndex ();
			}
			public Vector3 GetProjectedPointOnFocusedEdge ()
			{
				return GetDieEditor ().projectedPointOnFocusedEdge;
			}
			public bool IsVertexSelected (int index)
			{
				return GetDieEditor ().SelectionSupport.IsVertexSelected (index);
			}

			public void SetVertexSelected (int index, bool value, bool allowMultiSel)
			{
				DieEditorWindow editor = GetDieEditor ();
				DieEditorSelectMode sm = tool.GetSupportedVertexSelectMode ();

				if (value && index >= 0 && index < editor.model.GetVertexCount ()) {
					if (!allowMultiSel || sm == DieEditorSelectMode.Single) {
						editor.SelectionSupport.ClearSelectedVertices ();
					}
					if (sm != DieEditorSelectMode.None) {
						editor.SelectionSupport.SetVertexSelected (index, true);
					}
				} else {
					editor.SelectionSupport.SetVertexSelected (index, false);
				}
			}

			public int[] GetSelectedVertices ()
			{
				return GetDieEditor ().SelectionSupport.GetSelectedVertexIndices ();
			}

			public bool IsEdgeSelected (int index)
			{
				return GetDieEditor ().SelectionSupport.IsEdgeSelected (index);
			}

			public void SetEdgeSelected (int index, bool value, bool allowMultiSel)
			{
				DieEditorWindow editor = GetDieEditor ();
				DieEditorSelectMode sm = tool.GetSupportedEdgeSelectMode ();
				if (value && index >= 0 && index < editor.model.GetEdgeCount ()) {
					if (!allowMultiSel || sm == DieEditorSelectMode.Single) {
						editor.SelectionSupport.ClearSelectedEdges ();
					}
					if (sm != DieEditorSelectMode.None) {
						editor.SelectionSupport.SetEdgeSelected (index, true);
					}
				} else {
					editor.SelectionSupport.SetEdgeSelected (index, false);
				}
			}

			public int[] GetSelectedEdges ()
			{
				return GetDieEditor ().SelectionSupport.GetSelectedEdgeIndices ();
			}
		}


		protected override IDrawingEditorToolContext CreateToolContext ()
		{
			return new DieEditorToolContext (this, (DieEditorTool)CurrentModelTool);
		}


		protected override void OnMouseDown ()
		{
			SelectFocused ();
		}

		private void SelectFocused ()
		{
			bool singleSelMode = !(Event.current.control || Event.current.shift);
			
			DieEditorSelectMode supportedVertexSel = CurrentModelTool.GetSupportedVertexSelectMode ();
			DieEditorSelectMode supportedEdgeSel = CurrentModelTool.GetSupportedEdgeSelectMode ();

			int focusedVertex = SelectionSupport.GetFocusedVertexIndex ();
			int focusedEdge = SelectionSupport.GetFocusedEdgeIndex ();

			if (singleSelMode) {
				SelectionSupport.ClearSelectedVertices ();
				SelectionSupport.ClearSelectedEdges ();
				if (supportedVertexSel != DieEditorSelectMode.None && focusedVertex >= 0) {
					SelectionSupport.SetVertexSelected (focusedVertex, true);
				} else if (supportedEdgeSel != DieEditorSelectMode.None && focusedEdge >= 0) {
					SelectionSupport.SetEdgeSelected (focusedEdge, true);
				}
			} else if (supportedVertexSel == DieEditorSelectMode.Multi && focusedVertex >= 0) {
				SelectionSupport.ToggleVertexSelected (focusedVertex);
			} else if (supportedEdgeSel == DieEditorSelectMode.Multi && focusedEdge >= 0) {
				SelectionSupport.ToggleEdgeSelected (focusedEdge);
			}
			Repaint ();
		
		}

		protected override bool OnKeyEvent (Event e)
		{
			if (e.keyCode == KeyCode.X || e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace) {
				DeleteSelected ();
				return true;
			} else {
				return false;
			}
		}

		private void DeleteSelected ()
		{
			if (null != model && model is IMutableDieModel) {
				((IMutableDieModel)model).RemoveVerticesAt (SelectionSupport.GetSelectedVertexIndices ());
				SelectionSupport.ClearSelectedVertices ();

				((IMutableDieModel)model).RemoveEdgesAt (false, SelectionSupport.GetSelectedEdgeIndices ());
				SelectionSupport.ClearSelectedEdges ();
			}

		}
		protected override void OnDrawModel ()
		{

			if (null != model) {
				DoEdges ();
				DoPoints ();
			}
		}
		protected virtual bool IsVertexVisible (int vertexIndex)
		{
			return DefaultIsVertexVisible (vertexIndex);
		}

		protected bool DefaultIsVertexVisible (int vertexIndex)
		{
			bool visible;
			// Should we draw the vertex?
			// Vertex should be visible if any of its connected edges is visible:

			int[] directlyConnectedEdges = model.GetConnectedEdgeIndices (vertexIndex);
			if (directlyConnectedEdges.Length > 0) {
				visible = false; // hidden if all edges are hidden
				foreach (int ei in directlyConnectedEdges) {
					if (IsEdgeVisible (ei)) {
						visible = true;
						break;
					}
				}
			} else {
				// orphan vertices are always visible
				visible = true;
			}
			return visible;
		}

		private void DoPoints ()
		{

			bool testMousePos = Event.current.isMouse && CurrentModelTool.GetSupportedVertexSelectMode () != DieEditorSelectMode.None;
			int vertexCount = model.GetVertexCount ();
		
			float nearestSqrMagnitude = float.PositiveInfinity;
			int newNearestVertexIndex = -1;



			int focusedVertexIndex = SelectionSupport.GetFocusedVertexIndex ();

			for (int i = 0; i < vertexCount; i++) {
//			if (i == draggingVertexIndex) {
//				continue;
//			}
				if (!IsVertexVisible (i)) {
					continue;
				}

				Vector3 canvasPt;
				if (CurrentModelTool.IsVertexInToolContext (i)) {
					canvasPt = TransformModelPoint (CurrentModelTool.GetVertexToolContextPosition (i));
					DoVertexHandle (canvasPt, i, false, true, focusedVertexIndex == i);
				} else {
					canvasPt = TransformModelPoint (model.GetVertexAt (i));
					if (DoVertexHandle (canvasPt, i, testMousePos, SelectionSupport.IsVertexSelected (i), focusedVertexIndex == i)) {
						// Box is under cursor; check if closer than previously matched
						float sqrMagnitude = (CursorCanvasPos - canvasPt).sqrMagnitude;
						if (focusedVertexIndex < 0 || sqrMagnitude < nearestSqrMagnitude) {
							newNearestVertexIndex = i;
							nearestSqrMagnitude = sqrMagnitude;
						}
					}
				}

			}
			// Draw the vertex being dragged
//		if (draggingVertexIndex >= 0) {
//			DoVertexHandle (CursorCanvasPos, false, selectedVertexIndices.Contains (draggingVertexIndex), true);
//			Repaint ();
//		}

			if (testMousePos && focusedVertexIndex != newNearestVertexIndex) {
				//needsRepaint = true;
				Repaint ();
				focusedVertexIndex = newNearestVertexIndex;
				SelectionSupport.SetFocusedVertexIndex (focusedVertexIndex);
			}
	
		}

	
		protected bool DoVertexHandle (Vector3 canvasPt, int vertexIndex, bool testMousePos, bool selected, bool highlight)
		{
			float handleSize = HandleUtility.GetHandleSize (canvasPt) * vertexHandleSize;
			float pickSize = HandleUtility.GetHandleSize (canvasPt) * vertexHandlePickSize;
			//float handleSizeDiv2 = handleSize / 2f;
			float vertexSymbolSize = handleSize / 2.0f;
			float selectedVertexSymbolSize = pickSize / 2.0f;

			Rect handleRect = new Rect ();
			handleRect.size = new Vector3 (handleSize, handleSize);
			handleRect.center = canvasPt;

			Rect pickRect = new Rect ();
			pickRect.size = new Vector3 (pickSize, pickSize);
			pickRect.center = canvasPt;

			bool insideBox = testMousePos && pickRect.Contains (CursorCanvasPos);

			// We have N outline layers:
			// 1. Basic shape: handleRect
			// 2. First emphasis outline (seam/sharp)
			// 3. Second emphasis outline (sharp if fseam)
			// 4. Focused outline
			Rect[] outlineRects = new Rect[4];
			Vector3[][] outlineRectVertices = new Vector3[outlineRects.Length][];
			for (int i = 0; i < outlineRects.Length; i++) {
				outlineRects [i] = new Rect ();
				outlineRects [i].size = handleRect.size + Vector2.one * ((float)i * 2f);
				outlineRects [i].center = canvasPt;
				outlineRectVertices [i] = new Vector3[] {
					new Vector3 (outlineRects [i].xMin, outlineRects [i].yMin),
					new Vector3 (outlineRects [i].xMax, outlineRects [i].yMin),
					new Vector3 (outlineRects [i].xMax, outlineRects [i].yMax),
					new Vector3 (outlineRects [i].xMin, outlineRects [i].yMax),
					new Vector3 (outlineRects [i].xMin, outlineRects [i].yMin),
				};
			}
		
			if (highlight) {
				Handles.color = focusedVertexColor;
				Handles.DrawAAConvexPolygon (outlineRectVertices [outlineRectVertices.Length - 1]);
			}

			int nextShapeLayer = 1;


			bool isSeam;
			bool isSharp;

			if (vertexIndex >= 0) {
				isSeam = model.IsSeamAt (vertexIndex);
				isSharp = model.IsSharpVertexAt (vertexIndex);
			} else {
				isSeam = false;
				isSharp = false;
			}

			// Seam?
			if (isSeam) {
				Handles.color = seamVertexColor;
				Handles.DrawAAPolyLine (2f, outlineRectVertices [nextShapeLayer++]);
			}
			// Sharp?
			if (isSharp) {
				Handles.color = sharpVertexColor;
				Handles.DrawAAPolyLine (2f, outlineRectVertices [nextShapeLayer++]);
			}
			// Sharp?
			if (selected) {
				Handles.color = selectedVertexColor;
				Handles.DrawAAPolyLine (1f, outlineRectVertices [nextShapeLayer++]);
			}


			// Draw the basic shape:
			Handles.color = selected ? selectedVertexColor : vertexColor;
			Handles.DrawAAConvexPolygon (outlineRectVertices [0]);
//
//			if (vertexIndex >= 0 && model.IsSeamAt (vertexIndex)) {
//				Handles.color = Color.cyan;
//			} else {
//				Handles.color = selected ? selectedVertexColor : vertexColor;
//			}
//			Handles.DrawAAConvexPolygon (selected ? selectedVertexDiamondVertices : vertexDiamondVertices);
//
			int focusedEdgeIndex = SelectionSupport.GetFocusedEdgeIndex ();

			if (vertexIndex >= 0 && model is IDieModelGraphSupport) {
				// Draw normal(s)

				List<Vector3> normals = new List<Vector3> ();
				List<Vector3> highLightedNormals = new List<Vector3> ();
				if (model.IsSharpVertexAt (vertexIndex)) {
					// Draw normals for all edge ends
					foreach (int ei in ((IDieModelGraphSupport)model).FindConnectedEdgeIndices(vertexIndex, FindEdgesFlags.None)) {
						if (focusedEdgeIndex == ei) {
							highLightedNormals.Add (model.GetNormalAt (vertexIndex, ei));
						} else {
							normals.Add (model.GetNormalAt (vertexIndex, ei));
						}
					}

				} else {
					// Just single normal
					if (highlight) {
						highLightedNormals.Add (model.GetNormalAt (vertexIndex));
					} else {
						normals.Add (model.GetNormalAt (vertexIndex));
					}
				}

				float len = GetDirectionVectorLength (vertexNormalVectorLength, canvasPt);

				foreach (Vector3 normal in highLightedNormals) {
					Handles.color = focusedEdgeColor;
					Handles.DrawAAPolyLine (7.0f, canvasPt, canvasPt + TransformModelDirection (normal) * len);
					Handles.color = focusedVertexNormalVectorColor;
					Handles.DrawAAPolyLine (5.0f, canvasPt, canvasPt + TransformModelDirection (normal) * len);
				}

				Handles.color = vertexNormalVectorColor;
				foreach (Vector3 normal in normals) {
					Handles.DrawAAPolyLine (3.0f, canvasPt, canvasPt + TransformModelDirection (normal) * len);
				}


				// Draw label(s)

				string label = string.Format ("{0}", vertexIndex);
				Vector3 labelPos = new Vector3 (canvasPt.x, canvasPt.y - EditorGUIUtility.singleLineHeight);
				Handles.Label (labelPos, label, canvasLabelStyle);

			}

			return insideBox;
		}

		protected virtual bool IsEdgeVisible (int edgeIndex)
		{
			return true;
		}

		private void DoEdges ()
		{
			int prevNearestEdgeIndex = SelectionSupport.GetFocusedEdgeIndex ();
			Vector3 prevCursorToProjectedPointOnNearestEdge = cursorToProjectedPointOnFocusedEdgeVector;

			bool edgeFocusSet = false;
			bool trackFocus = Event.current.isMouse;
			bool edgeFocusSupported = CurrentModelTool.GetSupportedEdgeSelectMode () != DieEditorSelectMode.None;

			int previousFocusedEdgeIndex = SelectionSupport.GetFocusedEdgeIndex ();
			int focusedVertexIndex = SelectionSupport.GetFocusedVertexIndex ();

			if (null != model) {
				int edgeCount = model.GetEdgeCount ();
				float nearestSqrDist = float.PositiveInfinity;
//				Vector3 focusedEdgePt0 = Vector3.zero;
//				Vector3 focusedEdgePt1 = Vector3.zero;

				for (int i = 0; i < edgeCount; i++) {
					if (!IsEdgeVisible (i)) {
						continue;
					}
					Edge edge = model.GetEdgeAt (i);
					int[] vi = edge.GetVertexIndices ();

					bool edgeDragged = false;
					// Edge start and end in model coordinates
					Vector3[] pt = new Vector3[2];
					for (int j = 0; j < 2; j++) {
						if (CurrentModelTool.IsVertexInToolContext (vi [j])) {
							// dragging this vertex
							pt [j] = CurrentModelTool.GetVertexToolContextPosition (vi [j]);
							edgeDragged = true;
						} else {
							pt [j] = model.GetVertexAt (vi [j]);
						}
					}

					Vector3 canvasPt0 = TransformModelPoint (pt [0]);
					Vector3 canvasPt1 = TransformModelPoint (pt [1]);

					if (trackFocus && edgeFocusSupported && !edgeDragged && focusedVertexIndex < 0) {
						// Test if cursor is over the edge (or if near enough)
						Vector3 projectedPoint;
						float sqrDistToEdge;
						bool isNearestEdge = ProjectPointOnEdge (CursorCanvasPos, canvasPt0, canvasPt1, out projectedPoint, out sqrDistToEdge);
						if (isNearestEdge) {
							// Projection hits the edge, check if it's close enough:
							if (sqrDistToEdge < nearestSqrDist) {
								nearestSqrDist = sqrDistToEdge;
								SelectionSupport.SetFocusedEdgeIndex (i);
								edgeFocusSet = true;

								// Store for later:
//								focusedEdgePt0 = canvasPt0;
//								focusedEdgePt1 = canvasPt1;

								projectedPointOnFocusedEdge = projectedPoint;
								cursorToProjectedPointOnFocusedEdgeVector = projectedPoint - CursorCanvasPos;
							} else {
								isNearestEdge = false;
							}
						}

					}
					string elabel = string.Format ("{0}", i);
					DrawEdge (canvasPt0, canvasPt1, SelectionSupport.IsEdgeSelected (i), i == SelectionSupport.GetFocusedEdgeIndex (), elabel);
				}

				int focusedEdgeIndex = SelectionSupport.GetFocusedEdgeIndex ();

				// Highlight the focused edge
//				if (edgeFocusSupported && focusedEdgeIndex >= 0 && focusedVertexIndex < 0) {
//					// Highlight the edge selection target
//
//					DrawEdge (focusedEdgePt0, focusedEdgePt1, 
//					          SelectionSupport.IsEdgeSelected (focusedEdgeIndex), 
//				          true, "");
//
//				}
				// TODO is this used?
				if (drawingNewEdge) {
					Handles.color = Color.white;
					Vector3 pt0 = TransformModelPoint (newEdgeStartPos);
					Vector3 pt1 = TransformModelPoint (newEdgeEndPos);
					Handles.color = focusedEdgeColor;
					Handles.DrawAAPolyLine (2f, pt0, pt1);
					//
					//				if (newEdgeFromIndex >= 0 && newEdgeFromIndex < lastVertexIndex) {
					//					// Inserting between existing points; draw line from the new point to 
					//					// the existing next point
					//					Vector3 pt2 = CanvasTransformPoint (model.GetVertexAt (newEdgeFromIndex + 1));
					//					Handles.color = Color.cyan;
					//					Handles.DrawLine (pt1, pt2);
					//					//					Handles.DrawDottedLine (pt1, pt2, HandleUtility.GetHandleSize (pt1) * 10f);
					//				}
				} /*else if (draggingPointIndex >= 0) {
				List<int> edgeIndices = model.FindConnectedEdgeIndices (draggingPointIndex);
				foreach (int ei in edgeIndices) {

					Vector3 pt0Screen = CanvasTransformPoint (currentDragPos);
					Vector3 pt1Screen;

					DieModel.Edge e = model.GetEdgeAt (ei);
					int fromIndex = e.GetFromVertexIndex ();
					int toIndex = e.GetToVertexIndex ();

					if (draggingPointIndex == fromIndex) {
						// Draw temp line from current toIndex to the drag point
						pt1Screen = CanvasTransformPoint (model.GetVertexAt (toIndex));
					} else {
						// Draw temp line from current fromIndex to the drag point
						pt1Screen = CanvasTransformPoint (model.GetVertexAt (fromIndex));
					}

					Handles.color = Color.yellow;
					Handles.DrawAAPolyLine (2f, pt0Screen, pt1Screen);
				}
			}*/
			
			
//				if (focusedEdgeIndex != prevNearestEdgeIndex || 
//					(focusedEdgeIndex >= 0 && cursorToProjectedPointOnFocusedEdgeVector != prevCursorToProjectedPointOnNearestEdge)) {
//					Repaint ();
//				}

				if (DieModel is IMutableDieModel) {
					SupportedModelOps supportedOps = ((IMutableDieModel)DieModel).GetSupportedModelOps ();
					bool supportEdgeSplit = SupportedModelOps.AddVertex == (supportedOps & SupportedModelOps.AddVertex);
					if (supportEdgeSplit && focusedEdgeIndex >= 0) {

						Handles.color = Color.red;
					
						Handles.DrawLine (projectedPointOnFocusedEdge - cursorToProjectedPointOnFocusedEdgeVector, 
					                  projectedPointOnFocusedEdge + cursorToProjectedPointOnFocusedEdgeVector);
						Repaint ();
					} else {
					
					}
				}
				if (trackFocus && !edgeFocusSet && edgeFocusSupported) {
					SelectionSupport.SetFocusedEdgeIndex (-1);
				}
				if (previousFocusedEdgeIndex != SelectionSupport.GetFocusedEdgeIndex ()) {
					Repaint ();
				}
			}

		}
		private float GetDirectionVectorLength (float length, Vector3 canvasPt)
		{
			// Option1 : "up" is normalized; scale it to canvas:
			//up *= TransformModelVector (up * faceNormalVectorLength).magnitude;
			
			// Option 2: 1/10 of the edge length
			//up *= vMagnitude / 10f;
			
			// Option 3: constant screen size
			float handleSize = HandleUtility.GetHandleSize (canvasPt);
			return handleSize * length;
		}
		public void DrawEdge (Vector3 pt0, Vector3 pt1, bool selected, bool focused, string label)
		{
			if (focused) {
				Handles.color = focusedEdgeColor;
				Handles.DrawAAPolyLine (9f, pt0, pt1);
			}
			if (selected) {
				Handles.color = selectedEdgeColor;
				Handles.DrawAAPolyLine (5f, pt0, pt1);
			}
			Handles.color = selected ? selectedEdgeColor : edgeColor;
			Handles.DrawAAPolyLine (3f, pt0, pt1);


			// TODO we should use metrics from DieModel.Edge if available!
			Vector3 v = (pt1 - pt0);
			float vMagnitude = v.magnitude;
			Vector3 dir = v.normalized;
			Vector3 up = Vector3.Cross (dir, Vector3.forward).normalized;
			Vector3 mid = pt0 + dir * vMagnitude / 2f;

			up *= GetDirectionVectorLength (faceNormalVectorLength, mid);

			Handles.color = faceNormalVectorColor;
			Handles.DrawAAPolyLine (mid, mid + up);


			if (null != label && label.Length > 0) {
				Vector3 labelPos = mid + up;
				Handles.Label (labelPos, label, canvasLabelStyle);
			}

//		private static float faceNormalVectorLength = 0.25f;
//		private static Color faceNormalVectorColor = Color.cyan;
//		
//		private static float vertexNormalVectorLength = 0.25f;
//		private static Color vertexNormalVectorColor = Color.magenta;

		}


		/// <summary>
		/// Project a point to an edge, all in screen coordinates
		/// </summary>
		/// <returns><c>true</c>, if point on edge was projected, <c>false</c> otherwise.</returns>
		/// <param name="cursorPos">Cursor position.</param>
		/// <param name="edgePt0">Edge pt0.</param>
		/// <param name="edgePt1">Edge pt1.</param>
		/// <param name="projectedPoint">Projected point.</param>
		/// <param name="distThreshold">Dist threshold.</param>
		bool ProjectPointOnEdge (Vector3 cursorPos, Vector3 edgePt0, Vector3 edgePt1, out Vector3 projectedPoint, out float sqrDistanceFromCursorToProjected)
		{
			// Normal and projection in screen coordinates:
		
			float distThreshold = edgeHitThreshold * HandleUtility.GetHandleSize (edgePt0);
			float sqrDistThreshold = distThreshold * distThreshold;
		
			Vector3 edge = (edgePt1 - edgePt0);
			float edgeSqrMagnitude = edge.sqrMagnitude;
			Vector3 edgeNormal = edge.normalized;
			Vector3 projection = Vector3.Project (cursorPos - edgePt0, edgeNormal);
			float projectionSqrMagnitude = projection.sqrMagnitude;
		
			projectedPoint = edgePt0 + projection;
			Vector3 cursorToProjected = (projectedPoint - CursorCanvasPos);
			sqrDistanceFromCursorToProjected = cursorToProjected.sqrMagnitude;
		
			bool insideEdge;
			if (sqrDistanceFromCursorToProjected > sqrDistThreshold) {
				// Too far
				insideEdge = false;
			} else if (projectionSqrMagnitude > edgeSqrMagnitude) {
				insideEdge = false;
			} else if (Vector3.Dot (edgeNormal, projection) < 0f) {
				insideEdge = false;
			} else {
				insideEdge = true;
			}
		
			return insideEdge;
		}

#else

//	[MenuItem ("CONTEXT/MyObject/Add Profile Edge")]
//	public static void MenuAddProfileEdge (MenuCommand command)
//	{
//		MyObject target = command.context as MyObject;
//		if (null != target) {
//			target.points.Add (new Vector3 ());
//		}
//		//EditorWindow.GetWindow<MyEditorWindow> ();
//	}

	int selStyleIndex = -1;

	private class GridConfig
	{
		public string name;
		public float div;
		public Color color;
//		public bool enabled;
		public bool visible;

		public GridConfig (string name, float div, Color color/*, bool enabled*/)
		{
			this.name = name;
			this.div = div;
			this.color = color;
//			this.enabled = enabled;
		}
		
	}

	private enum DragMode
	{
		None,
		Pan,
		Zoom,
		MoveVertex,
		
	}

	private List<GridConfig> grids = new List<GridConfig> ();
	private int enabledGridsMask;
	private bool gridsEnabled = true;
	private bool forceSnapToGrid = false;
	private bool showOrigo = true;
	
	//	Vector3 boxPos = new Vector3 (50, 50);
	
	private GUIStyle canvasBgStyle;
	private GUIStyle linerStyle;
	
	private int draggingPointIndex = -1;
	private Vector3 currentDragPos;

	private bool drawingNewEdge = false;
	private int newEdgeFromIndex = -1;
	private Vector3 newEdgeStartPos;
	private Vector3 newEdgeEndPos;

	private Vector3 cursorCanvasPos;
	private Vector3 freeCursorCanvasPos; // unsnapped!
	private bool showSnapTarget;

	//	bool dragging = false;
	//	Vector3 dragStartPos;
	//
	protected DieModel model;
	protected Rect canvasRect;
//	protected Rect statusBarRect;

	private float canvasScaleFactorMult = 0.2f;
	private float canvasScaleFactor = 1.0f;
	private Vector2 canvasOffset = new Vector2 (200f, 0f);

	private static float edgeHitThreshold = 1.5f;

	// TODO rename to edgeUnderCursor / vertexUnderCursor ?
	private int nearestVertexIndex = -1;

	private bool needsRepaint = false;

	private int nearestEdgeIndex = -1;
	private Vector3 projectedPointOnNearestEdge;
	private Vector3 cursorToProjectedPointOnNearestEdge;

	private Texture2D edgeSplitCursor;

	public DieEditorWindow ()
	{
	}


	void OnEnable ()
	{
//		Texture icon = new Texture();
//		Resources.Load("", typeof(Texture));
//
//		titleContent = new GUIContent ("Die Profile", icon);
//
		this.wantsMouseMove = true;

//		EditorApplication.modifierKeysChanged = SendRepaintEvent;

		Texture2D canvasBgTexture = new Texture2D (1, 1, TextureFormat.RGBA32, false);
		canvasBgTexture.SetPixel (0, 0, new Color (0.34f, 0.34f, 0.34f, 1.0f));
		canvasBgTexture.Apply ();
		
		canvasBgStyle = new GUIStyle ();
		canvasBgStyle.normal.background = canvasBgTexture;

		Texture2D linerBgTexture = new Texture2D (1, 1, TextureFormat.RGBA32, false);
		linerBgTexture.SetPixel (0, 0, new Color (0.4f, 0.4f, 0.4f, 1.0f));
		linerBgTexture.Apply ();
		
		linerStyle = new GUIStyle ();
		linerStyle.normal.background = linerBgTexture;

		edgeSplitCursor = new Texture2D (0, 0, TextureFormat.RGBA32, false);
		edgeSplitCursor.Apply ();

		grids.Add (new GridConfig ("10.0", 10f, new Color (0.8f, 0.8f, 0.8f, 0.6f)));
		grids.Add (new GridConfig ("1.0", 1f, new Color (0.7f, 0.7f, 0.7f, 0.5f)));
		grids.Add (new GridConfig ("0.5", 0.5f, new Color (0.6f, 0.6f, 0.6f, 0.4f)));
		grids.Add (new GridConfig ("0.1", 0.1f, new Color (0.5f, 0.5f, 0.5f, 0.4f)));
		enabledGridsMask = 0x01 | 0x02 | 0x04;

	}
	void OnDisable ()
	{

	}
	void OnDestroy ()
	{

	}

	void Update ()
	{
	}

	void OnGUI ()
	{
		needsRepaint = false;
		

		UnityEngine.GameObject selGameObject = Selection.activeGameObject;
		//target = (null != selGameObject) ? selGameObject.GetComponent<MyObject> () : null;
		if (null != selGameObject) {
			IDieModelContainer dmc = selGameObject.GetComponent<IDieModelContainer> ();
			this.model = (null != dmc) ? dmc.GetDieModel () : null;
		} else {
			this.model = null;
		}

		float height = position.height;
		float width = position.width;
		float lineHeight = EditorGUIUtility.singleLineHeight;
		float linerSize = 20f;
		Rect toolbarRect = new Rect (0, 0, width, lineHeight);
		Rect statusBarRect = new Rect (0, height - lineHeight, width, lineHeight);

		// Temporary canvas rect without toolbar and status bar:
		Rect totalCanvasRect = new Rect (0, toolbarRect.y + toolbarRect.height, width, height - toolbarRect.height - statusBarRect.height);
		Rect vertLinerRect = new Rect (totalCanvasRect.x, totalCanvasRect.y, linerSize, totalCanvasRect.height - linerSize);
		Rect horizLinerRect = new Rect (totalCanvasRect.x + linerSize, totalCanvasRect.y + totalCanvasRect.height - linerSize, totalCanvasRect.width - linerSize, linerSize);

		canvasRect = new Rect (
			totalCanvasRect.x + linerSize, 
			totalCanvasRect.y, 
			totalCanvasRect.width - linerSize, 
			totalCanvasRect.height - linerSize);

		//snapToGrid = false;

		DoCanvas ();

		// Liners and interconnection box:
		Rect linerJunctionBox = new Rect (
			vertLinerRect.x,
			horizLinerRect.y,
			vertLinerRect.width,
			horizLinerRect.height);

		GUI.Box (linerJunctionBox, GUIContent.none, linerStyle);

		Vector3 linerJunctionCenter = linerJunctionBox.center;
		Handles.color = Handles.yAxisColor;
		Handles.DrawLine (linerJunctionCenter, new Vector3 (linerJunctionCenter.x, linerJunctionBox.yMin));
		Handles.color = Handles.xAxisColor;
		Handles.DrawLine (linerJunctionCenter, new Vector3 (linerJunctionBox.xMax, linerJunctionCenter.y));

		GUI.Box (vertLinerRect, GUIContent.none, linerStyle);
		GUI.Box (horizLinerRect, GUIContent.none, linerStyle);


		DoToolbar (toolbarRect);
		DoStatusBar (statusBarRect);

//		Handles.color = Color.gray;
//		for (int i = 0; i < 100; i++) {
//			Vector2 pt = CanvasTransformPoint (new Vector2 ((float)i / 10f, 0));
//			Handles.DrawLine (new Vector3 (pt.x, horizLinerRect.y), new Vector3 (pt.x, horizLinerRect.y + horizLinerRect.height));
//		}

//		Debug.Log ("Scale: " + canvasScaleFactor);
//		List<string> styles = new List<string> ();
//		foreach (GUIStyle s in GUI.skin) {
//			if (s.name.ToLower ().Contains ("sel")) {
//				styles.Add (s.name);
//			}
//		}
//		styles.Sort ();
//		selStyleIndex = EditorGUI.Popup (toolbarRect, selStyleIndex, styles.ToArray ());
//		if (selStyleIndex >= 0 && selStyleIndex < styles.Count) {
//			linerStyle = GUI.skin.GetStyle (styles [selStyleIndex]);
//			Debug.Log ("" + linerStyle.normal.textColor);
//			//GUI.Box (new Rect (0, 40, rect.width, rect.height), GUIContent.none, GUI.skin.GetStyle (styles [selStyleIndex]));
//		}
		// AnimationTimelineTick
		// AnimationCurveEditorBackground

		if (needsRepaint) {
			Repaint ();
			needsRepaint = false;
		}
	}
	private void DoToolbar (Rect toolbarRect)
	{
		GUILayout.BeginArea (toolbarRect);
		GUI.Box (toolbarRect, GUIContent.none, EditorStyles.toolbar);
		EditorGUILayout.BeginHorizontal ();

//		Rect addButtonRect = EditorGUILayout.BeginHorizontal ();
//		if (GUILayout.Button ("One", EditorStyles.toolbarButton)) {
//			MenuCommand command = new MenuCommand (target);
//			EditorUtility.DisplayPopupMenu (addButtonRect, "CONTEXT/MyObject", command);
//		}
//		EditorGUILayout.EndHorizontal ();

		GUILayout.FlexibleSpace ();

		List<int> zoomValues = new List<int> (new int[] {10, 20, 50, 100, 200, 300, 400, 500});
		int currentZoomPct = (int)(canvasScaleFactor * 100f);
//		if (!zoomValues.Contains (currentZoomPct)) {
		zoomValues.Insert (0, currentZoomPct);
		zoomValues.Insert (1, -1);
//		}
		List<string> zoomLabels = new List<string> ();
		foreach (int zv in zoomValues) {
			if (zv >= 0) {
				zoomLabels.Add (string.Format ("{0:f0}%", zv));
			} else {
				zoomLabels.Add ("//");
			}
		}
		EditorGUI.BeginChangeCheck ();
		currentZoomPct = EditorGUILayout.IntPopup (currentZoomPct, zoomLabels.ToArray (), zoomValues.ToArray (), EditorStyles.toolbarPopup, GUILayout.Width (60f));
		if (EditorGUI.EndChangeCheck ()) {
			this.canvasScaleFactor = (float)currentZoomPct / 100f;
		}

		GUILayout.Space (8f);

		showOrigo = GUILayout.Toggle (showOrigo, "Origo", EditorStyles.toolbarButton);
		gridsEnabled = GUILayout.Toggle (gridsEnabled, "Grid", EditorStyles.toolbarButton);

		List<string> gridNames = new List<string> ();
		foreach (GridConfig gc in grids) {
			gridNames.Add (gc.name);
		}
		EditorGUI.BeginDisabledGroup (!gridsEnabled);
		EditorGUI.BeginChangeCheck ();
		this.enabledGridsMask = EditorGUILayout.MaskField (enabledGridsMask, gridNames.ToArray (), EditorStyles.toolbarDropDown, GUILayout.MaxWidth (16f));
		if (EditorGUI.EndChangeCheck ()) {
			Repaint ();
		}
		EditorGUI.EndDisabledGroup ();

		EditorGUI.BeginDisabledGroup (!gridsEnabled);
		forceSnapToGrid = GUILayout.Toggle (forceSnapToGrid, "Snap", EditorStyles.toolbarButton);
		EditorGUI.EndDisabledGroup ();

		GUILayout.Space (8f);

		EditorGUILayout.EndHorizontal ();
		GUILayout.EndArea ();
//
//		int sel = GUI.Toolbar (toolbarRect, 0, new string[] {"D", "Refresh", "+", "-", "1x", "0.5x", "2x"});
//		if (sel == 1) {
//			Repaint ();
//		} else if (sel == 2) {
//			canvasScaleFactor += 0.1f;
//		} else if (sel == 3) {
//			canvasScaleFactor -= 0.1f;
//		} else if (sel == 4) {
//			canvasScaleFactor = 1f;
//			Repaint ();
//		} else if (sel == 5) {
//			canvasScaleFactor = 0.5f;
//			Repaint ();
//		}
	}

	private string currentMouseHelpText = "";

	private void DoStatusBar (Rect rect)
	{
		GUILayout.BeginArea (rect);
		GUI.Box (rect, GUIContent.none, EditorStyles.toolbar);
		EditorGUILayout.BeginHorizontal ();

		string keyHelpText = "";
		bool showSnapToGrid = true;
		bool showCancel = drawingNewEdge;

		Event e = Event.current;
		if (e.isMouse) {
			currentMouseHelpText = (e.alt || e.button == 1) ? "" : "⌘ Snap to Grid";
			bool dragging = e.type == EventType.MouseDrag;
			if (dragging) {
				if (e.button == 0) {
					currentMouseHelpText += " ⌥ Pan";
				} else if (e.button == 1) {
					currentMouseHelpText += " ⌥ Zoom";
				}
				if (e.alt) {
					currentMouseHelpText += " ⇧ Faster";
				} else {
					currentMouseHelpText += " ⎋ Cancel";
				}
			}
		}
		// dragging: 
		// Alt+0 = Pan
		// Alt+1 = Zoom!
		// 
		// shift: Double speed!

		GUILayout.Label (currentMouseHelpText);

		EditorGUILayout.EndHorizontal ();
		GUILayout.EndArea ();
	}

	private void DoCanvas ()
	{
//		Debug.Log ("DoCanvas " + System.DateTime.Now.Ticks);



//		bgStyle = GUI.skin.GetStyle ("AnimationCurveEditorBackground");
	
		GUI.Box (canvasRect, GUIContent.none, canvasBgStyle);
//		GUI.BeginClip (canvasRect);

		if (gridsEnabled) {
			
			for (int i = 0; i < grids.Count; i++) {
				int maskValue = 0x01 << i;
				GridConfig grid = grids [i];
				if (maskValue == (enabledGridsMask & maskValue)) {
					grid.visible = DrawGrid (grid.div, grid.color);
				} else {
					grid.visible = false;
				}
			}
		}

		DrawGridOrigo ();

		if (showSnapTarget) {
			DoVertexHandle (cursorCanvasPos, false);
		}


		DoPoints ();
		DoEdges ();

//		float handleSize = HandleUtility.GetHandleSize (boxPos);
//		Rect boxRect = new Rect (boxPos.x, boxPos.y, handleSize, handleSize);
//		GUI.Box (boxRect, GUIContent.none);

//		Debug.Log ("E: " + Event.current.type);

		Event e = Event.current;

		Vector3 mousePos;
		Vector3 mouseLocalPos;

		Vector3 snappedMouseLocalPos;

		bool prevShowSnapTarget = showSnapTarget;

//		bool isAltModifier = Event.current.HasModifier (EventModifiers.Alt);
//		bool isShiftModifier = Event.current.HasModifier (EventModifiers.Shift);
//

		if (e.isMouse) {
			mousePos = e.mousePosition;
			mouseLocalPos = InverseCanvasTransformPoint (mousePos);

			snappedMouseLocalPos = mouseLocalPos;
			if (SnapToGrid (ref snappedMouseLocalPos)) {
				showSnapTarget = true;
				cursorCanvasPos = CanvasTransformPoint (snappedMouseLocalPos);
				freeCursorCanvasPos = CanvasTransformPoint (mouseLocalPos);
			} else {
				showSnapTarget = false;
				cursorCanvasPos = mousePos;
				freeCursorCanvasPos = mousePos;
			}

		} else {
			mousePos = Vector3.zero;
			mouseLocalPos = mousePos;
			snappedMouseLocalPos = mouseLocalPos;
		}

		if (e.isKey) {
//			Repaint ();
//			Debug.Log ("Key: " + Event.current.keyCode);
			if (e.keyCode == KeyCode.Escape) {
				// TODO we need to collect the current tool state to one object!
				drawingNewEdge = false;
				needsRepaint = true;
			}
		}

		// TODO
		//
		// - Särmän voi katkaista kahdella tavalla:
		//   1. Jaa --> Uusi piste janaan
		//   2. Katkaise --> Kahdeksi erilliseksi janaksi
		// - Uuden pisteen janaan voi lisätä joko viimeisen pisteen jälkeen tai
		//   ennen ensimmäistä pistettä. Ei siis minne vaan kuten nyt


		switch (e.type) {
		case EventType.MouseMove:


			if (prevShowSnapTarget != showSnapTarget || showSnapTarget) {
				needsRepaint = true;
			}
			if (drawingNewEdge) {
				draggingPointIndex = -1;
				newEdgeEndPos = snappedMouseLocalPos;
				needsRepaint = true;
			}


			break;
		case EventType.ScrollWheel:
			if (e.delta.y != 0.0f) {
				canvasScaleFactor *= (1f + e.delta.y / 100f);
				canvasScaleFactor = Mathf.Clamp (canvasScaleFactor, 0.01f, 10f);
			}
			needsRepaint = true;
			break;
		case EventType.MouseDown:

			if (e.button == 0) {
				if (drawingNewEdge) {
					// Dont't do anything yet - add the edge only when the
					// user releases the button
					draggingPointIndex = -1;
				} else if (nearestVertexIndex >= 0) {
					currentDragPos = model.GetVertexAt (nearestVertexIndex);
					draggingPointIndex = nearestVertexIndex;
				} else {
					// Insert new edge starting from here
					drawingNewEdge = true;
					draggingPointIndex = -1;
					newEdgeFromIndex = -1;
					newEdgeStartPos = SnapToGrid (mouseLocalPos);
					newEdgeEndPos = newEdgeStartPos;
					needsRepaint = true;
				}
			} else if (e.button == 1) {
				if (nearestVertexIndex >= 0) {
					// Show popup menu
					Rect menuRect = new Rect (mousePos, new Vector2 (0, 0));
					EditorUtility.DisplayCustomMenu (
						menuRect, 
						new GUIContent[] {
							new GUIContent ("Add Edge from Here"), 
							new GUIContent ("Delete Vertex")
						}, 
						-1, 
						VertexContextMenuSelected, 
						nearestVertexIndex);
				}
			}
			break;
		case EventType.MouseUp:
			if (e.button == 0) {
				if (draggingPointIndex >= 0) {
					model.SetVertexAt (draggingPointIndex, currentDragPos);
					needsRepaint = true;
					draggingPointIndex = -1;
				} else if (drawingNewEdge) {
					drawingNewEdge = false;
					needsRepaint = true;

					// Do insert the point
					newEdgeEndPos = snappedMouseLocalPos;
					if (newEdgeFromIndex < 0) {
						newEdgeFromIndex = model.AddVertex (newEdgeStartPos);
					}

					if (newEdgeEndPos != newEdgeStartPos) {
						int newVertexToIndex;

						if (nearestVertexIndex >= 0 && nearestVertexIndex != newEdgeFromIndex
							&& model.GetVertexAt (nearestVertexIndex) == newEdgeEndPos) {
							// Connect to an existing vertex
							newVertexToIndex = nearestVertexIndex;
						} else {
							// Connect to a new vertex
							newVertexToIndex = model.AddVertex (newEdgeEndPos);
						}

						model.AddEdge (newEdgeFromIndex, newVertexToIndex);
					}
					newEdgeFromIndex = -1;
				}

			}
			break;
		case EventType.MouseDrag:
			// dragging: 
			// Alt+0 = Pan
			// Alt+1 = Zoom!

			DragMode dragMode = DragMode.None;
			if (e.button == 0) {
				// Primary (left) mouse button
				if (e.alt) {
					dragMode = DragMode.Pan;
				} else if (draggingPointIndex >= 0 || drawingNewEdge) {
					dragMode = DragMode.MoveVertex;
				} 
			} else if (e.button == 1) {
				// Secondary (right) mouse button
				dragMode = DragMode.Pan;
			} else if (e.button == 2) {
				// Tertiary (middle) mouse button
				dragMode = DragMode.Zoom;
			}

			switch (dragMode) {
			case DragMode.Pan:

				Vector2 delta = e.delta;
				if (e.shift) {
					// Double speed
					delta *= 2f;
				}
				this.canvasOffset += delta;
				needsRepaint = true;
				break;
			case DragMode.Zoom:
				// TODO implement double speed!
				if (e.delta.y != 0.0f) {
					canvasScaleFactor *= (1f + e.delta.y / 100f);
					canvasScaleFactor = Mathf.Clamp (canvasScaleFactor, 0.01f, 10f);
				}
				needsRepaint = true;
				break;
			case DragMode.MoveVertex:
				if (draggingPointIndex >= 0) {
					currentDragPos += InverseCanvasTransformVector (e.delta);
					currentDragPos = SnapToGrid (currentDragPos);

//					// TODO DON't set the vertex before drag ends!
//					model.SetVertexAt (draggingPointIndex, pt);

					needsRepaint = true;

				} else if (drawingNewEdge) {
					newEdgeEndPos = SnapToGrid (mouseLocalPos);
					//Debug.LogFormat ("New: {0} --> {1}", newEdgeStartPos, newEdgeEndPos);
					needsRepaint = true;
				}
				break;
			}

			break;
		}

	}


	private void VertexContextMenuSelected (object userData, string[] options, int selItem)
	{
		if (0 == selItem) {
			// Add Edge from Here
			int startVertexIndex = (int)userData;
			Vector3 startPos = model.GetVertexAt (startVertexIndex);
			newEdgeStartPos = startPos;
			newEdgeEndPos = newEdgeStartPos;
			newEdgeFromIndex = startVertexIndex;
			drawingNewEdge = true;
			Repaint ();
		} else if (1 == selItem) {
			// Delete vertex
			int vertexIndex = (int)userData;
			model.RemoveVertexAt (vertexIndex);
			Repaint ();
		}

	}

//	private void AddNewEdge (int newEdgeFromIndex, Vector3 newEdgeEndPos, bool repaint)
//	{
//		target.points.Insert (newEdgeFromIndex + 1, newEdgeEndPos);
//		if (repaint) {
//			Repaint ();
//		}
//	}
	private Vector3 SnapToGrid (Vector3 pt)
	{
		Vector3 snappedPt = pt;
		SnapToGrid (ref snappedPt);
		return snappedPt;
	}
	private bool SnapToGrid (ref Vector3 pt)
	{
		bool snapped = false;
		if (forceSnapToGrid || Event.current.command) {
			// Snap to grid
			if (Event.current.command && nearestVertexIndex >= 0) {
				// Snap to nearest vertex
				snapped = true;
				pt = model.GetVertexAt (nearestVertexIndex);
			} else if (gridsEnabled) {
				bool gridFound = false;
				float densestGridDiv = 0f;
				for (int i = 0; i < grids.Count; i++) {
					GridConfig grid = grids [i];
					if (grid.visible && (!gridFound || grid.div < densestGridDiv)) {
						densestGridDiv = grid.div;
						gridFound = true;
					}
				}
				if (gridFound) {
					pt = new Vector3 (
						Mathf.Round (pt.x / densestGridDiv) * densestGridDiv,
						Mathf.Round (pt.y / densestGridDiv) * densestGridDiv,
						pt.z);
					snapped = true;
				}
			}
		}
		return snapped;
	}

	/// <summary>
	/// Project a point to an edge, all in screen coordinates
	/// </summary>
	/// <returns><c>true</c>, if point on edge was projected, <c>false</c> otherwise.</returns>
	/// <param name="cursorPos">Cursor position.</param>
	/// <param name="edgePt0">Edge pt0.</param>
	/// <param name="edgePt1">Edge pt1.</param>
	/// <param name="projectedPoint">Projected point.</param>
	/// <param name="distThreshold">Dist threshold.</param>
	bool ProjectPointOnEdge (Vector3 cursorPos, Vector3 edgePt0, Vector3 edgePt1, out Vector3 projectedPoint, out float sqrDistanceFromCursorToProjected)
	{
		// Normal and projection in screen coordinates:

		float distThreshold = edgeHitThreshold * HandleUtility.GetHandleSize (edgePt0);
		float sqrDistThreshold = distThreshold * distThreshold;

		Vector3 edge = (edgePt1 - edgePt0);
		float edgeSqrMagnitude = edge.sqrMagnitude;
		Vector3 edgeNormal = edge.normalized;
		Vector3 projection = Vector3.Project (cursorPos - edgePt0, edgeNormal);
		float projectionSqrMagnitude = projection.sqrMagnitude;

		projectedPoint = edgePt0 + projection;
		Vector3 cursorToProjected = (projectedPoint - cursorCanvasPos);
		sqrDistanceFromCursorToProjected = cursorToProjected.sqrMagnitude;

		bool insideEdge;
		if (sqrDistanceFromCursorToProjected > sqrDistThreshold) {
			// Too far
			insideEdge = false;
		} else if (projectionSqrMagnitude > edgeSqrMagnitude) {
			insideEdge = false;
		} else if (Vector3.Dot (edgeNormal, projection) < 0f) {
			insideEdge = false;
		} else {
			insideEdge = true;
		}

		return insideEdge;
	}
//	private class EdgeBounds
//	{
//		private Rect bounds;
//		private Vector3 pt0;
//		private Vector3 pt1;
//		private bool initialized = false;
//
//		public Rect GetBoundingRect (Vector3 pt0, Vector3 pt1)
//		{
//			UpdateBounds (pt0, pt1);
//			return bounds;
//
//		}
//
//		// In screen coords!
//		public void UpdateBounds (Vector3 pt0, Vector3 pt1)
//		{
//			if (!initialized || !this.pt0.Equals (pt0) || !this.pt1.Equals (pt1)) {
//				bounds = Rect.MinMaxRect (
//					Mathf.Min (pt0.x, pt1.x), 
//					Mathf.Min (pt0.y, pt1.y), 
//					Mathf.Max (pt0.x, pt1.x), 
//					Mathf.Max (pt0.y, pt1.y));
//				
//				float marginTopLeft = HandleUtility.GetHandleSize (bounds.position) * edgeHitThreshold;
//				float marginBottomRight = HandleUtility.GetHandleSize (bounds.position + bounds.size) * edgeHitThreshold;
//				bounds.x -= marginTopLeft;
//				bounds.width += (marginTopLeft + marginBottomRight);
//				bounds.y -= marginTopLeft;
//				bounds.height += (marginTopLeft + marginBottomRight);
//				initialized = true;
//			}
//		}
//	}


//	private Dictionary<int, EdgeBounds> _edgeBoundsMap = new Dictionary<int, EdgeBounds> ();
//	private EdgeBounds GetEdgeBoundsForEdgeAt (int index)
//	{
//		int lastIndex = target.points.Count - 1;
//
//		if (index < 0 || index >= lastIndex) {
//			throw new System.ArgumentOutOfRangeException ("index", index, "index < 0 || index >= " + lastIndex);
//		}
//		EdgeBounds bounds;
//		if (_edgeBoundsMap.ContainsKey (index)) {
//			bounds = _edgeBoundsMap [index];
//		} else {
//			bounds = new EdgeBounds ();
//			_edgeBoundsMap [index] = bounds;
//		}
//		Vector3 pt0 = target.points [index];
//		Vector3 pt1 = target.points [index + 1];
//
//		bounds.GetBoundingRect (pt0, pt1);
//		return bounds;
//	}

//	void Foo (Vector3 cursorPos)
//	{
//		// 
//		foreach (Rect rect in edgeBounds) {
//			Color outlinecolor = Color.white;
//			//Debug.Log (cursorCanvasPos);
//			if (rect.Contains (cursorCanvasPos)) {
//				outlinecolor = Color.red;
//				Repaint ();
//			}
//			Handles.color = outlinecolor;
//			Handles.DrawSolidRectangleWithOutline (
//				new Vector3[] {
//				new Vector3 (rect.xMin, rect.yMin),
//				new Vector3 (rect.xMax, rect.yMin),
//				new Vector3 (rect.xMax, rect.yMax),
//				new Vector3 (rect.xMin, rect.yMax)
//			}, Color.clear, outlinecolor);
//		}
//	}

	/// <summary>
	/// Transform point in local coordinates to canvas screen coordinates.
	/// </summary>
	/// <returns><c>true</c> if this instance canvas transform point the specified pt; otherwise, <c>false</c>.</returns>
	/// <param name="pt">Point.</param>
	private Vector3 CanvasTransformPoint (Vector3 pt)
	{
		// Lower left = 0,0
		// Upper right = 1,1 / canvasScaleFactor
		Vector2 offs = canvasRect.position + canvasOffset;
		float scaleAxis = canvasRect.width;
		Vector2 scale = new Vector2 (scaleAxis, scaleAxis) * canvasScaleFactor * canvasScaleFactorMult;
		return new Vector3 (pt.x * scale.x + offs.x, pt.y * -scale.y + offs.y + canvasRect.height, pt.z);
	}
	private Vector3 CanvasTransformVector (Vector3 v)
	{
		float scaleAxis = canvasRect.width;
		Vector2 scale = new Vector2 (scaleAxis, scaleAxis) * canvasScaleFactor * canvasScaleFactorMult;
		return new Vector3 (v.x * scale.x, v.y * -scale.y, v.z);
	}

	private Vector3 InverseCanvasTransformPoint (Vector2 pt)
	{
		Vector2 offs = canvasRect.position + canvasOffset;
		;
		float scaleAxis = canvasRect.width;
		Vector2 scale = new Vector2 (scaleAxis, scaleAxis) * canvasScaleFactor * canvasScaleFactorMult;
		return new Vector2 ((pt.x - offs.x) / scale.x, (pt.y - offs.y - canvasRect.height) / -scale.y);
	}
	private Vector3 InverseCanvasTransformVector (Vector2 v)
	{
		float scaleAxis = canvasRect.width;
		Vector2 scale = new Vector2 (scaleAxis, -scaleAxis) * canvasScaleFactor * canvasScaleFactorMult;
		return new Vector3 (v.x / scale.x, v.y / scale.y);
	}

	private void DrawGridOrigo ()
	{
		if (showOrigo) {
			Vector2 origo = CanvasTransformPoint (Vector3.zero);
			//if (canvasRect.Contains (origo)) {
			Handles.color = Handles.yAxisColor;
			Handles.DrawLine (new Vector3 (origo.x, canvasRect.y), new Vector3 (origo.x, canvasRect.y + canvasRect.height));
			Handles.color = Handles.xAxisColor;
			Handles.DrawLine (new Vector3 (canvasRect.x, origo.y), new Vector3 (canvasRect.x + canvasRect.width, origo.y));
		}
	}

	private bool DrawGrid (float gridDiv, Color color, float skipThreshold = 8.0f)
	{
		// Draw grid
		if (CanvasTransformVector (new Vector2 (gridDiv, gridDiv)).x >= skipThreshold) {

			Vector3 topLeftLocal = InverseCanvasTransformPoint (canvasRect.position);
			Vector3 bottomRightLocal = InverseCanvasTransformPoint (new Vector2 (canvasRect.x + canvasRect.width, canvasRect.y + canvasRect.height));
			Handles.color = color;
			float gridXLocal = Mathf.Ceil (topLeftLocal.x / gridDiv) * gridDiv;
			while (gridXLocal <= bottomRightLocal.x) {
				float gridXScreen = CanvasTransformPoint (new Vector3 (gridXLocal, 0f)).x;
				Vector3 pt0 = new Vector3 (gridXScreen, canvasRect.y);
				Vector3 pt1 = new Vector3 (pt0.x, canvasRect.y + canvasRect.height);
				Handles.DrawLine (pt0, pt1);
				gridXLocal += gridDiv;
			}
			float gridYLocal = Mathf.Ceil (bottomRightLocal.y / gridDiv) * gridDiv;
			while (gridYLocal <= topLeftLocal.y) {
				float gridYScreen = CanvasTransformPoint (new Vector3 (0f, gridYLocal)).y;
				Vector3 pt0 = new Vector3 (canvasRect.x, gridYScreen);
				Vector3 pt1 = new Vector3 (canvasRect.x + canvasRect.width, pt0.y);
				Handles.DrawLine (pt0, pt1);
				gridYLocal += gridDiv;
				
			}
			return true;
		} else {
			return false;
		}
	}

	private void DoEdges ()
	{
		int prevNearestEdgeIndex = this.nearestEdgeIndex;
		Vector3 prevCursorToProjectedPointOnNearestEdge = cursorToProjectedPointOnNearestEdge;
		nearestEdgeIndex = -1;

		if (null != model) {
			int edgeCount = model.GetEdgeCount ();
			float nearestSqrDist = float.PositiveInfinity;

			for (int i = 0; i < edgeCount; i++) {

				DieModel.Edge edge = model.GetEdgeAt (i);
				int[] vi = edge.GetVertexIndices ();

				Vector3 pt0, pt1;

				bool edgeDragged;
				if (vi [0] == draggingPointIndex) {
					// dragging the "from" vertex (pt0)
					pt0 = currentDragPos;
					pt1 = model.GetVertexAt (vi [1]);
					edgeDragged = true;
				} else if (vi [1] == draggingPointIndex) {
					// dragging the "to" vertex (pt1)
					pt0 = model.GetVertexAt (vi [0]);
					pt1 = currentDragPos;
					edgeDragged = true;
				} else {
					pt0 = model.GetVertexAt (vi [0]);
					pt1 = model.GetVertexAt (vi [1]);
					edgeDragged = false;
				}
				Vector3 pt0Screen = CanvasTransformPoint (pt0);
				Vector3 pt1Screen = CanvasTransformPoint (pt1);

				if (!edgeDragged) {
					Vector3 projectedPoint;
					float sqrDistToEdge;
					bool isNearestEdge = ProjectPointOnEdge (cursorCanvasPos, pt0Screen, pt1Screen, out projectedPoint, out sqrDistToEdge);
					if (isNearestEdge) {
						// Projection hits the edge, check if it's close enough:
						if (sqrDistToEdge < nearestSqrDist) {
							nearestSqrDist = sqrDistToEdge;
							nearestEdgeIndex = i;
							projectedPointOnNearestEdge = projectedPoint;
							cursorToProjectedPointOnNearestEdge = projectedPoint - cursorCanvasPos;
						} else {
							isNearestEdge = false;
						}
					}
					// Draw the edge
					if (isNearestEdge) {
						// Highlight the edge selection target
						Handles.color = Color.cyan;
						Handles.DrawAAPolyLine (4f, pt0Screen, pt1Screen);
					}
				}
//				
				Handles.color = Color.white;
				Handles.DrawAAPolyLine (2f, pt0Screen, pt1Screen);


				//				Handles.DrawLine (cursorCanvasPos, projectedPoint);
			}

			if (drawingNewEdge) {
				Handles.color = Color.white;
				Vector3 pt0Screen = CanvasTransformPoint (newEdgeStartPos);
				Vector3 pt1Screen = CanvasTransformPoint (newEdgeEndPos);
				Handles.DrawAAPolyLine (2f, pt0Screen, pt1Screen);
//
//				if (newEdgeFromIndex >= 0 && newEdgeFromIndex < lastVertexIndex) {
//					// Inserting between existing points; draw line from the new point to 
//					// the existing next point
//					Vector3 pt2 = CanvasTransformPoint (model.GetVertexAt (newEdgeFromIndex + 1));
//					Handles.color = Color.cyan;
//					Handles.DrawLine (pt1, pt2);
//					//					Handles.DrawDottedLine (pt1, pt2, HandleUtility.GetHandleSize (pt1) * 10f);
//				}
			} /*else if (draggingPointIndex >= 0) {
				List<int> edgeIndices = model.FindConnectedEdgeIndices (draggingPointIndex);
				foreach (int ei in edgeIndices) {

					Vector3 pt0Screen = CanvasTransformPoint (currentDragPos);
					Vector3 pt1Screen;

					DieModel.Edge e = model.GetEdgeAt (ei);
					int fromIndex = e.GetFromVertexIndex ();
					int toIndex = e.GetToVertexIndex ();

					if (draggingPointIndex == fromIndex) {
						// Draw temp line from current toIndex to the drag point
						pt1Screen = CanvasTransformPoint (model.GetVertexAt (toIndex));
					} else {
						// Draw temp line from current fromIndex to the drag point
						pt1Screen = CanvasTransformPoint (model.GetVertexAt (fromIndex));
					}

					Handles.color = Color.yellow;
					Handles.DrawAAPolyLine (2f, pt0Screen, pt1Screen);
				}
			}*/


			if (nearestEdgeIndex != prevNearestEdgeIndex || 
				(nearestEdgeIndex >= 0 && cursorToProjectedPointOnNearestEdge != prevCursorToProjectedPointOnNearestEdge)) {
				needsRepaint = true;
			}
			if (nearestEdgeIndex >= 0) {
				//Rect hideCursorRect = new Rect ();
				//hideCursorRect.size = Vector2.one * 40f;
				//hideCursorRect.center = projectedPointOnNearestEdge;
				//Cursor.SetCursor (null, Vector2.zero, CursorMode.ForceSoftware);
				//EditorGUIUtility.AddCursorRect (hideCursorRect, MouseCursor.CustomCursor);
				//EditorGUI.DrawRect (hideCursorRect, Color.cyan);
				
				Handles.color = Color.yellow;
				
				Handles.DrawLine (projectedPointOnNearestEdge - cursorToProjectedPointOnNearestEdge, 
				                  projectedPointOnNearestEdge + cursorToProjectedPointOnNearestEdge);
				needsRepaint = true; 
				
			} else {
				
			}
		}
	}
	private void DoPoints ()
	{

//		Vector2 offs = canvasRect.position;
//		Vector2 scale = new Vector2 (canvasRect.width, canvasRect.height) * canvasScaleFactor;
		//nearestVertexIndex = -1;
		if (null != model) {
			bool testMousePos = Event.current.isMouse;

			int vertexCount = model.GetVertexCount ();

			float nearestSqrMagnitude = float.PositiveInfinity;
			int newNearestVertexIndex = -1;
			for (int i = 0; i < vertexCount; i++) {
				if (i == draggingPointIndex) {
					continue;
				}
				Vector3 canvasPt = CanvasTransformPoint (model.GetVertexAt (i));
				if (DoVertexHandle (canvasPt, testMousePos, nearestVertexIndex == i)) {
					// Box is under cursor; check if closer than previously matched
					float sqrMagnitude = (freeCursorCanvasPos - canvasPt).sqrMagnitude;
					if (nearestVertexIndex < 0 || sqrMagnitude < nearestSqrMagnitude) {
						newNearestVertexIndex = i;
						nearestSqrMagnitude = sqrMagnitude;
					}
				}
			}
			if (testMousePos && nearestVertexIndex != newNearestVertexIndex) {
				needsRepaint = true;
				nearestVertexIndex = newNearestVertexIndex;
			}
		}
	}

	bool DoVertexHandle (Vector3 canvasPt, bool testMousePos = false, bool highlight = false)
	{
		float handleSize = HandleUtility.GetHandleSize (canvasPt) * 0.75f;
		float handleSizeDiv2 = handleSize / 2f;
		Rect boxRect = new Rect (canvasPt.x - handleSizeDiv2, canvasPt.y - handleSizeDiv2, handleSize, handleSize);

		bool insideBox = testMousePos && boxRect.Contains (freeCursorCanvasPos);

		Vector3[] boxVertices = new Vector3[] {
			new Vector3 (boxRect.x, boxRect.y),
			new Vector3 (boxRect.x + boxRect.width, boxRect.y),
			new Vector3 (boxRect.x + boxRect.width, boxRect.y + boxRect.height),
			new Vector3 (boxRect.x, boxRect.y + boxRect.height),
		};


		Color boxFaceColor;
		Color boxOutlineColor;
		if (highlight) {
			boxFaceColor = new Color (0.7f, 0.7f, 0.7f, 0.2f);
			boxOutlineColor = Color.cyan;
		} else {
			boxFaceColor = new Color (0, 0, 0, 0.1f);
			boxOutlineColor = Color.white;
		}
		Handles.color = boxOutlineColor;
		Handles.DrawSolidRectangleWithOutline (boxVertices, boxFaceColor, boxOutlineColor);



		return insideBox;
	}

	// Translated from http://wiki.unity3d.com/index.php/PolyContainsPoint
//	private static bool PolygonContainsPoint (Vector2[] polyPoints, Vector2 p)
//	{ 
//		int j = polyPoints.Length - 1; 
//		bool inside = false; 
//		for (int i = 0; i < polyPoints.Length; j = i++) { 
//			if (((polyPoints [i].y <= p.y && p.y < polyPoints [j].y) || (polyPoints [j].y <= p.y && p.y < polyPoints [i].y)) && 
//				(p.x < (polyPoints [j].x - polyPoints [i].x) * (p.y - polyPoints [i].y) / (polyPoints [j].y - polyPoints [i].y) + polyPoints [i].x)) 
//				inside = !inside; 
//		} 
//		return inside; 
//	}
#endif
	}
}
