// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Paths.MeshGenerator.Extruder.Editor
{

	public abstract class DrawingEditorWindowBase : EditorWindow
	{
		protected abstract void OnEnable ();
		protected abstract void OnDisable ();
		protected abstract void OnDestroy ();
		protected abstract void Update ();
		protected abstract void OnGUI ();
	}





	public abstract class DrawingEditorWindow : DrawingEditorWindowBase
	{

		int selStyleIndex = -1;
	
		private class GridConfig
		{
			private string name;
			private float div;
			private Color color;
			//		public bool enabled;
			public bool visible;
		
			public GridConfig (string name, float div, Color color/*, bool enabled*/)
			{
				this.name = name;
				this.div = div;
				this.color = color;
				//			this.enabled = enabled;
			}
			public string Name {
				get {
					return name;
				}
			}
			public float Div {
				get {
					return div;
				}
			}
			public Color Color {
				get {
					return color;
				}
			}
			public bool Visible {
				get {
					return visible;
				}
				set {
					this.visible = value;
				}
			}
		}

		private enum MouseToolState
		{
			None,
			Drag,
		}
		private enum DragMode
		{
			None,
			Pan,
			Zoom,
			ModelMove,
		}

		private List<GridConfig> grids = new List<GridConfig> ();
		private int enabledGridsMask;
		private bool gridsEnabled = true;
		private bool forceSnapToGrid = false;
		private bool showOrigo = true;

		protected GUIStyle canvasBgStyle;
		protected GUIStyle linerStyle;
		protected Color canvasBgColor;
		protected GUIStyle canvasLabelStyle;

		private Vector3 cursorCanvasPos;
		private Vector3 freeCursorCanvasPos; // unsnapped!
		private bool showSnapTarget;
	
		protected Rect canvasRect;

		private float canvasScaleFactorMult = 0.2f; // For nice scaling? TODO what's this?
		private float canvasScaleFactor = 1.0f;
		private float canvasScaleAspectRatio = 1.0f; // x / y
		private Vector2 canvasOffset = new Vector2 (200f, 0f);

		// TODO get rid of this!
		private bool needsRepaint = false;

		private bool mouseDownSentToModel = false;

		private MouseToolState mouseToolState = MouseToolState.None;
		private DragMode dragMode = DragMode.None;

		private DrawingModel drawingModel = EmptyDrawingModel.Instance;

		private IDrawingEditorTool currentModelTool = NoDrawingEditorTool.Instance;

		protected DrawingEditorWindow ()
		{
		}

		protected float CanvasScaleAspectRatio {
			get {
				return canvasScaleAspectRatio;
			}
			set {
				if (value <= 0.0f) {
					throw new ArgumentOutOfRangeException ("CanvasScaleAspectRatio", value, "CanvasScaleAspectRatio must be greater than zero");
				}
				bool changed = this.canvasScaleAspectRatio != value;
				this.canvasScaleAspectRatio = value;
				if (changed) {
					Repaint ();
				}

			}
		}

		protected Color CanvasBackgroundColor {
			get {
				return canvasBgColor;
			}
		}

		protected Vector3 CursorCanvasPos {
			get {
				return cursorCanvasPos;
			}
		}
		protected Vector3 FreeCursorCanvasPos {
			get {
				return freeCursorCanvasPos;
			}
		}

		protected IDrawingEditorTool CurrentModelTool {
			get {
				return currentModelTool;
			}
			set {
				if (null == value) {
					value = NoDrawingEditorTool.Instance;
				}
				this.currentModelTool = value;
				Repaint ();// TODO is Repaint() required in here?
			}
		}
		//private bool showSnapTarget;

	
		protected override sealed void OnEnable ()
		{
			//		Texture icon = new Texture();
			//		Resources.Load("", typeof(Texture));
			//
			//		titleContent = new GUIContent ("Die Profile", icon);
			//
			this.wantsMouseMove = true;
		
			//		EditorApplication.modifierKeysChanged = SendRepaintEvent;
		
			Texture2D canvasBgTexture = new Texture2D (1, 1, TextureFormat.RGBA32, false);
			canvasBgColor = new Color (0.34f, 0.34f, 0.34f, 1.0f);
			canvasBgTexture.SetPixel (0, 0, canvasBgColor);
			canvasBgTexture.Apply ();
		
			canvasBgStyle = new GUIStyle ();
			canvasBgStyle.normal.background = canvasBgTexture;
		
			Texture2D linerBgTexture = new Texture2D (1, 1, TextureFormat.RGBA32, false);
			linerBgTexture.SetPixel (0, 0, new Color (0.4f, 0.4f, 0.4f, 1.0f));
			linerBgTexture.Apply ();
		
			linerStyle = new GUIStyle ();
			linerStyle.normal.background = linerBgTexture;

			Color canvasLabelBgColor = canvasBgColor;
			canvasLabelBgColor.a = 0.5f;

			Texture2D canvasLabelBg = new Texture2D (1, 1, TextureFormat.RGBA32, false);
			canvasLabelBg.alphaIsTransparency = true;
			canvasLabelBg.SetPixel (0, 0, canvasLabelBgColor);
			canvasLabelBg.Apply ();
			canvasLabelStyle = new GUIStyle ();
			canvasLabelStyle.normal.textColor = new Color (1f, 1f, 1f, 0.7f);
			canvasLabelStyle.normal.background = canvasLabelBg;
			canvasLabelStyle.fontSize = 8;
			canvasLabelStyle.alignment = TextAnchor.MiddleCenter;
		

			grids.Add (new GridConfig ("10.0", 10f, new Color (0.8f, 0.8f, 0.8f, 1.0f)));
			grids.Add (new GridConfig ("1.0", 1f, new Color (0.7f, 0.7f, 0.8f, 0.6f)));
			grids.Add (new GridConfig ("0.5", 0.5f, new Color (0.6f, 0.6f, 0.6f, 0.4f)));
			grids.Add (new GridConfig ("0.1", 0.1f, new Color (0.5f, 0.5f, 0.5f, 0.2f)));
			enabledGridsMask = 0x01 | 0x02 | 0x04 | 0x08;

			OnEnabled ();
		}

		protected virtual void OnEnabled ()
		{

		}

		protected override sealed void OnDisable ()
		{
			OnDisabled ();
		}
		protected virtual void OnDisabled ()
		{

		}

		protected override sealed void OnDestroy ()
		{
		
		}
	
		protected override sealed void Update ()
		{
		}
	
		protected override sealed void OnGUI ()
		{
			needsRepaint = false;
		
		
//		UnityEngine.GameObject selGameObject = Selection.activeGameObject;

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
		
			Rect unclippedCanvasRect = canvasRect;
			try {
				GUI.BeginClip (canvasRect);
				Vector2 offs = canvasRect.position;
				canvasRect.position = Vector3.zero;
				DoCanvas ();
			} finally {
				GUI.EndClip ();
				this.canvasRect = unclippedCanvasRect;
			}

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
//	class ToolInfo {
//		public string name;
//		public Type toolClass;
//		public ToolInfo(Types toolClass, string name) {
//			this.toolClass = toolClass;
//			this.name = name;
//		}
//	}
//	class ToolFactory {
//		public ToolInfo[] GetToolInfos() {
//			return new ToolInfo[] {
//				new ToolInfo(typeof(DieEditorVertexMoveTool), "Sel"),
//				new ToolInfo(typeof(DieEditorInsertVerticesTool), "Ins"),
//			};
//		}
//		public DrawingEditorTool GetToolInstance(Types toolType) {
//			return null;
//		}
//	}

		public class ModelToolInfo
		{
			private string name;
			private string id;
			public ModelToolInfo (string id, string name)
			{
				this.id = id;
				this.name = name;
			}
			public string Id {
				get {
					return id;
				}
			}
			public string Name {
				get {
					return name;
				}
			}
		}
		public interface IModelToolFactory
		{
			ModelToolInfo[] GetToolInfos ();
			IDrawingEditorTool CreateToolInstance (string toolId);
		}

		protected virtual IModelToolFactory GetModelToolFactory ()
		{
			return null;
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


			// Tool Selection:
			// Available tools:


			IModelToolFactory mtf = GetModelToolFactory ();
			if (null != mtf) { 
				IDrawingEditorTool currentModelTool = CurrentModelTool;
				ModelToolInfo[] toolInfos = mtf.GetToolInfos ();
				for (int i = 0; i < toolInfos.Length; i++) {
					ModelToolInfo mti = toolInfos [i];
					bool active = CurrentModelTool.GetToolId () == mti.Id;

					bool newActive = GUILayout.Toggle (active, mti.Name, EditorStyles.toolbarButton);
					if (active != newActive) {
						if (newActive) {
							// Set the new tool
							CurrentModelTool = mtf.CreateToolInstance (mti.Id);
						} else {
							// Set to empty tool
							CurrentModelTool = NoDrawingEditorTool.Instance;
						}
						Repaint ();
					}
				}
			}
			GUILayout.Space (8f);
			//GUILayout.BeginHorizontal ();
			DrawCustomToolbar ();
			//GUILayout.EndHorizontal ();

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
				gridNames.Add (gc.Name);
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

		protected virtual void DrawCustomToolbar ()
		{

		}

		// TODO fix this!
		private string currentMouseHelpText = "";
	
		private void DoStatusBar (Rect rect)
		{
			GUILayout.BeginArea (rect);
			GUI.Box (rect, GUIContent.none, EditorStyles.toolbar);
			EditorGUILayout.BeginHorizontal ();
		
			string keyHelpText = "";
			bool showSnapToGrid = true;
			bool showCancel = false;//drawingNewEdge;
		
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
			
				for (int i = grids.Count - 1; i >= 0; i--) {
					int maskValue = 0x01 << i;
					GridConfig grid = grids [i];
					if (maskValue == (enabledGridsMask & maskValue)) {
						grid.Visible = DrawGrid (grid.Div, grid.Color);
					} else {
						grid.Visible = false;
					}
				}
			}
		
			DrawGridOrigo ();
		

			//		float handleSize = HandleUtility.GetHandleSize (boxPos);
			//		Rect boxRect = new Rect (boxPos.x, boxPos.y, handleSize, handleSize);
			//		GUI.Box (boxRect, GUIContent.none);
		
			//		Debug.Log ("E: " + Event.current.type);

			HandleInputEvent ();

			if (showSnapTarget) {
				//DoVertexHandle (cursorCanvasPos, false);
			}


			DrawModel ();

			CurrentModelTool.OnGUI (CreateToolContext ());
			//DoPoints ();
			//DoEdges ();


		}

		protected abstract class AbstractDrawingEditorToolContext : IDrawingEditorToolContext
		{
			private DrawingEditorWindow editor;
			protected AbstractDrawingEditorToolContext (DrawingEditorWindow editor)
			{
				this.editor = editor;
			}

			public abstract object GetModel ();

			public DrawingEditorWindow GetEditor ()
			{
				return editor;
			}

			public Vector3 GetCursorCanvasPos ()
			{
				return editor.CursorCanvasPos;
			}
			public Vector3 GetCursorModelPos ()
			{
				return InverseTransformModelPoint (GetCursorCanvasPos ());
			}

			public Vector3 TransformModelPoint (Vector3 pt)
			{
				return editor.TransformModelPoint (pt);
			}

			public Vector3 TransformModelVector (Vector3 v)
			{
				return editor.TransformModelVector (v);
			}

			public Vector3 InverseTransformModelPoint (Vector3 pt)
			{
				return editor.InverseTransformModelPoint (pt);
			}

			public Vector3 InverseTransformModelVector (Vector3 v)
			{
				return editor.InverseTransformModelVector (v);
			}
		}
		protected abstract IDrawingEditorToolContext CreateToolContext ();

		private void CancelCurrentTool ()
		{
			MouseToolState cancelledToolState = this.mouseToolState;
			DragMode cancelledDragMode = this.dragMode;

			this.mouseToolState = MouseToolState.None;
			this.dragMode = DragMode.None;

//		if (cancelledToolState == MouseToolState.Drag && cancelledDragMode == DragMode.Move) {
//			OnEndMouseDrag (false);
//		}
			IDrawingEditorToolContext context = CreateToolContext ();
			currentModelTool.Cancel (context);

		}

		private DragMode BeginMouseDrag ()
		{
			DragMode mode;
			Event e = Event.current;
			if (e.button == 1) {
				// Secondary (right) mouse button
				mode = DragMode.Pan;
			} else if (e.button == 2) {
				// Tertiary (middle) mouse button
				mode = DragMode.Zoom;
			} else {
				// Primary button drag
				IDrawingEditorToolContext context = CreateToolContext ();
				if (currentModelTool.BeginDrag (context)) {
					mode = DragMode.ModelMove;
				} else {
					mode = DragMode.None;
				}
			}

			return mode;
		}



		private void EndMouseDrag ()
		{
			mouseToolState = MouseToolState.None;
			DragMode prevDragMode = dragMode;
			dragMode = DragMode.None;
			if (prevDragMode == DragMode.ModelMove) {
				IDrawingEditorToolContext context = CreateToolContext ();
				currentModelTool.EndDrag (context);
			}

		}

		private void MouseDrag ()
		{
			IDrawingEditorToolContext context = CreateToolContext ();
			currentModelTool.Drag (context);
		}


		private void MouseDown ()
		{

			if (!currentModelTool.MouseDown (CreateToolContext ())) {
				OnMouseDown ();
			} else {
				mouseDownSentToModel = true;
			}
		}
		protected virtual void OnMouseDown ()
		{

		}

		private void MouseUp ()
		{
			if (mouseDownSentToModel) {
				mouseDownSentToModel = false;
				currentModelTool.MouseUp (CreateToolContext ());
			} else {
				OnMouseUp ();
			}
		}
		protected virtual void OnMouseUp ()
		{
		
		}

		private bool HandleKeyEvent (Event e)
		{
			if (!CurrentModelTool.Key (CreateToolContext ())) {
				return OnKeyEvent (e);
			} else {
				return true;
			}
		}
		protected virtual bool OnKeyEvent (Event e)
		{
			return false;
		}
		private void HandleInputEvent ()
		{
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
				mouseLocalPos = InverseTransformModelPoint (mousePos);
			
				snappedMouseLocalPos = mouseLocalPos;
				if (SnapToGrid (ref snappedMouseLocalPos)) {
					this.showSnapTarget = true;
					this.cursorCanvasPos = TransformModelPoint (snappedMouseLocalPos);
					this.freeCursorCanvasPos = TransformModelPoint (mouseLocalPos);
				} else {
					this.showSnapTarget = false;
					this.cursorCanvasPos = mousePos;
					this.freeCursorCanvasPos = mousePos;
				}
			
			} else {
				mousePos = Vector3.zero;
				mouseLocalPos = mousePos;
				snappedMouseLocalPos = mouseLocalPos;
			}
		
			if (e.isKey) {
				bool use;
				if (e.keyCode == KeyCode.Escape) {
					CancelCurrentTool ();
					use = true;
					needsRepaint = true;
				} else {
					use = HandleKeyEvent (e);
				}
				if (use) {
					e.Use ();
				}
			}

			switch (e.type) {
			case EventType.MouseMove:
				CurrentModelTool.MouseMove (CreateToolContext ());
				break;
			case EventType.ScrollWheel:
			// Zoom
				DoZoomByMouseEvent (e);
				needsRepaint = true;
				break;
			case EventType.MouseDown:

//			if (e.button == 1) {
//				// Invoke context menu
//				Rect menuRect = new Rect (mousePos, new Vector2 (0, 0));
//				EditorUtility.DisplayCustomMenu (
//					menuRect, 
//					new GUIContent[] {
//					new GUIContent ("Add Edge from Here"), 
//					new GUIContent ("Delete Vertex")
//				}, 
//				-1, 
//				(a,b,c) => {}, 
//				null);
//			}

			// Alt+0 == pan
			// Alt+1 == zoom
			// Don't route alt+MouseDown to model!
				if (!e.alt) {
					MouseDown ();
				}

				break;

			case EventType.MouseUp:
			// End drag?
				if (this.mouseToolState == MouseToolState.Drag) {
					EndMouseDrag ();
					this.dragMode = DragMode.None;
				} else {
					MouseUp ();
				}
				break;

			case EventType.MouseDrag:
			// dragging: 
			// Alt+0 = Pan
			// Alt+1 = Zoom!

			// Begin drag?
			
				if (this.mouseToolState == MouseToolState.None) {
					this.dragMode = BeginMouseDrag ();
					this.mouseToolState = MouseToolState.Drag;//(this.dragMode != DragMode.None) ? MouseToolState.Drag : MouseToolState.None;
				}

				if (this.mouseToolState == MouseToolState.Drag) {

					DragMode tempMode;
					if (e.button == 0 && e.alt) {
						tempMode = DragMode.Pan;
					} else {
						tempMode = this.dragMode;
					}

					switch (tempMode) {
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
						DoZoomByMouseEvent (e);
						needsRepaint = true;
						break;
					case DragMode.ModelMove:
						MouseDrag ();
					//				if (draggingPointIndex >= 0) {
					//					currentDragPos += InverseCanvasTransformVector (e.delta);
					//					currentDragPos = SnapToGrid (currentDragPos);
					//					
					//					//					// TODO DON't set the vertex before drag ends!
					//					//					model.SetVertexAt (draggingPointIndex, pt);
					//					
					//					needsRepaint = true;
					//					
					//				} else if (drawingNewEdge) {
					//					newEdgeEndPos = SnapToGrid (mouseLocalPos);
					//					//Debug.LogFormat ("New: {0} --> {1}", newEdgeStartPos, newEdgeEndPos);
					//					needsRepaint = true;
					//				}
						break;
					}
				}
			
				break;
			}
		}
		private void DoZoomByMouseEvent (Event e)
		{
			float scaleFactor = this.canvasScaleFactor;
			float speed = e.shift ? 0.02f : 0.005f;
			scaleFactor *= (1f + e.delta.y * speed);
			this.canvasScaleFactor = Mathf.Clamp (scaleFactor, 0.01f, 10f);
		}


		private Vector3 SnapToGrid (Vector3 pt)
		{
			Vector3 snappedPt = pt;
			SnapToGrid (ref snappedPt);
			return snappedPt;
		}
		private bool SnapToGrid (ref Vector3 pt)
		{
			bool snapped = false;
			// TODO make modifier actions configurable (currently Cmd=Snap)
			if (forceSnapToGrid || Event.current.command) {
				// Snap to grid
				if (Event.current.command && drawingModel.HasCurrentSnapToTarget ()) {
					// Snap to nearest vertex
					snapped = true;
					pt = drawingModel.GetCurrentSnapToTargetPosition ();
				} else if (gridsEnabled) {
					bool gridFound = false;
					float densestGridDiv = 0f;
					for (int i = 0; i < grids.Count; i++) {
						GridConfig grid = grids [i];
						if (grid.Visible && (!gridFound || grid.Div < densestGridDiv)) {
							densestGridDiv = grid.Div;
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
		/// Transform point in local coordinates to canvas screen coordinates.
		/// </summary>
		/// <returns><c>true</c> if this instance canvas transform point the specified pt; otherwise, <c>false</c>.</returns>
		/// <param name="pt">Point.</param>
		protected Vector3 TransformModelPoint (Vector3 pt)
		{
			// Lower left = 0,0
			// Upper right = 1,1 / canvasScaleFactor
			Vector2 offs = canvasRect.position + canvasOffset;
			float scaleAxis = canvasRect.width;
			Vector2 scale = new Vector2 (scaleAxis * canvasScaleAspectRatio, scaleAxis) * canvasScaleFactor * canvasScaleFactorMult;
			return new Vector3 (pt.x * scale.x + offs.x, pt.y * -scale.y + offs.y + canvasRect.height, pt.z);
		}
		protected Vector3 TransformModelVector (Vector3 v)
		{
			float scaleAxis = canvasRect.width;
			Vector2 scale = new Vector2 (scaleAxis * canvasScaleAspectRatio, scaleAxis) * canvasScaleFactor * canvasScaleFactorMult;
			return new Vector3 (v.x * scale.x, v.y * -scale.y, v.z);
		}
		protected Vector3 TransformModelDirection (Vector3 v)
		{
			return new Vector3 (v.x, -v.y, v.z);
		}
	
		protected Vector3 InverseTransformModelPoint (Vector2 pt)
		{
			Vector2 offs = canvasRect.position + canvasOffset;
			;
			float scaleAxis = canvasRect.width;
			Vector2 scale = new Vector2 (scaleAxis * canvasScaleAspectRatio, scaleAxis) * canvasScaleFactor * canvasScaleFactorMult;
			return new Vector2 ((pt.x - offs.x) / scale.x, (pt.y - offs.y - canvasRect.height) / -scale.y);
		}
		protected Vector3 InverseTransformModelVector (Vector2 v)
		{
			float scaleAxis = canvasRect.width;
			Vector2 scale = new Vector2 (scaleAxis * canvasScaleAspectRatio, -scaleAxis) * canvasScaleFactor * canvasScaleFactorMult;
			return new Vector3 (v.x / scale.x, v.y / scale.y);
		}
	
		private void DrawGridOrigo ()
		{
			if (showOrigo) {
				Vector2 origo = TransformModelPoint (Vector3.zero);
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
			if (TransformModelVector (new Vector2 (gridDiv, gridDiv)).x >= skipThreshold) {
			
				Vector3 topLeftLocal = InverseTransformModelPoint (canvasRect.position);
				Vector3 bottomRightLocal = InverseTransformModelPoint (new Vector2 (canvasRect.x + canvasRect.width, canvasRect.y + canvasRect.height));
				Handles.color = color;
				float gridXLocal = Mathf.Ceil (topLeftLocal.x / gridDiv) * gridDiv;
				while (gridXLocal <= bottomRightLocal.x) {
					float gridXScreen = TransformModelPoint (new Vector3 (gridXLocal, 0f)).x;
					Vector3 pt0 = new Vector3 (gridXScreen, canvasRect.y);
					Vector3 pt1 = new Vector3 (pt0.x, canvasRect.y + canvasRect.height);
					Handles.DrawLine (pt0, pt1);
					//Handles.DrawAAPolyLine (2.0f, pt0, pt1);
					gridXLocal += gridDiv;
				}
				float gridYLocal = Mathf.Ceil (bottomRightLocal.y / gridDiv) * gridDiv;
				while (gridYLocal <= topLeftLocal.y) {
					float gridYScreen = TransformModelPoint (new Vector3 (0f, gridYLocal)).y;
					Vector3 pt0 = new Vector3 (canvasRect.x, gridYScreen);
					Vector3 pt1 = new Vector3 (canvasRect.x + canvasRect.width, pt0.y);
					Handles.DrawLine (pt0, pt1);
					//Handles.DrawAAPolyLine (2.0f, pt0, pt1);
					gridYLocal += gridDiv;
				
				}
				return true;
			} else {
				return false;
			}
		}

		private void DrawModel ()
		{
			OnDrawModel ();
		}
		protected abstract void OnDrawModel ();


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
	}
}
