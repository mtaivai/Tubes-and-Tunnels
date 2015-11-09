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
					DrawEdge (canvasPt0, 
					          canvasPt1, 
					          SelectionSupport.IsEdgeSelected (i), 
					          i == SelectionSupport.GetFocusedEdgeIndex (), 
					          true, 
					          elabel);
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
		public void DrawEdge (Vector3 pt0, Vector3 pt1, bool selected, bool focused, bool drawUpVector, string label)
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

			bool drawLabel = null != label && label.Length > 0;
			Vector3 mid, up;
			if (drawUpVector || drawLabel) {
				// TODO we should use metrics from DieModel.Edge if available!

				Vector3 v = (pt1 - pt0);
				float vMagnitude = v.magnitude;
				Vector3 dir = v.normalized;
				up = Vector3.Cross (dir, Vector3.forward).normalized;
				mid = pt0 + dir * vMagnitude / 2f;
				
				up *= GetDirectionVectorLength (faceNormalVectorLength, mid);
			} else {
				mid = Vector3.zero;
				up = Vector3.zero;
			}

			if (drawUpVector) {
				Handles.color = faceNormalVectorColor;
				Handles.DrawAAPolyLine (mid, mid + up);
			}


			if (drawLabel) {
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

	}
}
