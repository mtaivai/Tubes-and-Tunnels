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
	public class DieEditorUVMappingMultiTool : DieEditorMultiTool
	{
		[EditorAction(ActionGroupVertex, "Clear UV", "RequireFocusedVertex")]
		private void ClearUV (IDieEditorToolContext context)
		{
			HashSet<int> selectedVertices = new HashSet<int> (context.GetSelectedVertices ());
			selectedVertices.Add (context.GetFocusedVertexIndex ());
			UVEditorDieModel model = context.GetDieModel () as UVEditorDieModel;
			model.BatchOperation ("Clear UV", (mdl) => {
				foreach (int vi in selectedVertices) {
					model.ClearUvAt (vi);
				}
			});

			context.GetDieEditor ().Repaint ();
		}
	}

	public class DieEditorUVMappingWindow : DieEditorWindow
	{
		//private UnwrappedDieModel _unwrappedModel;

		private Texture2D texture;
		//private float vScaling = 1.0f;


		[SerializeField]
		private bool
			showSelectedEdgesOnly = true;

		[SerializeField]
		private bool
			alsoShowEdgesOnSameStrip = true;


		[MenuItem ("Window/Die UV Editor")]
		public static void MenuShowDieUVEditorWindow ()
		{
			EditorWindow.GetWindow<DieEditorUVMappingWindow> ();
		}


		public DieEditorUVMappingWindow ()
		{
			CurrentModelTool = new DieEditorUVMappingMultiTool ();
		}

		protected new UVEditorDieModel DieModel {
			get {
				return (UVEditorDieModel)base.DieModel;
			}
		}

		protected override IDieModel GetDieModelFromContainer (IDieModelContainer container)
		{
			IDieModel rawModel = container.GetDieModel ();
			UnwrappedDieModel unwrappedModel = UnwrappedDieModel.Build (rawModel);
			UVEditorDieModel uvModel = UVEditorDieModel.Build (unwrappedModel);
			return uvModel;
		}

//		protected override void OnDieModelChanged (DieModelChangedEventArgs e)
//		{
//			((UnwrappedDieModel)DieModel).Rebuild ();
//			Repaint ();
//		}

		private int _changedEventDepth = 0;
		private void WrappedDieModelChanged (DieModelChangedEventArgs e)
		{
			if (e.IsAfterEvent && _changedEventDepth == 0) {
				// The original wrapped modle has changed; we need to rebuild our model!
				try {
					_changedEventDepth++;
					DieModel.Rebuild (true);
					Repaint ();
				} finally {
					_changedEventDepth --;
				}

			}
		}
		private void WrappedModelSelectionChanged (DieModelSelectionEventArgs e)
		{
			// Follow the focused vertex and edge

			UnwrappedDieModel unwrappedModel = DieModel.UnwrappedModel;
			IDieModelSelectionSupport selectionSupport = DieModel.GetDieModelSelectionSupport ();

			int focusedVertexIndex = unwrappedModel.GetUnwrappedVertexIndex (e.FocusedVertexIndex);
			selectionSupport.SetFocusedVertexIndex (focusedVertexIndex);

			int focusedEdgeIndex = unwrappedModel.GetUnwrappedEdgeIndex (e.FocusedEdgeIndex);
			selectionSupport.SetFocusedEdgeIndex (focusedEdgeIndex);
			Debug.LogFormat ("Focus: v={0}, e={1}", focusedVertexIndex, focusedEdgeIndex);

			Repaint ();
		}

		protected override void OnModelAttached (IDieModel model)
		{
			// Add changed listener to the original wrapped model:
			IDieModel wrappedModel = ((UVEditorDieModel)model).UnwrappedModel.WrappedModel;
			if (wrappedModel is IMutableDieModel) {
				((IMutableDieModel)wrappedModel).AddDieModelChangeHandler (WrappedDieModelChanged);
			}
			// Register selection changed handler on the wrapped model:
			if (wrappedModel is IDieModelEditorSupport) {
				((IDieModelEditorSupport)wrappedModel).GetDieModelSelectionSupport ().AddSelectionChangedChangeHandler (WrappedModelSelectionChanged);
			}
			((UVEditorDieModel)model).GetDieModelSelectionSupport ().AttachModel (model);
		}
		protected override void OnModelDetached (IDieModel model)
		{
			// Remove changed listener from the original wrapped model:
			IDieModel wrappedModel = ((UVEditorDieModel)model).UnwrappedModel.WrappedModel;
			if (wrappedModel is IMutableDieModel) {
				((IMutableDieModel)wrappedModel).RemoveDieModelChangeHandler (WrappedDieModelChanged);
			}
			// Unregister selection changed handler on the wrapped model:
			if (wrappedModel is IDieModelEditorSupport) {
				((IDieModelEditorSupport)wrappedModel).GetDieModelSelectionSupport ().RemoveSelectionChangedChangeHandler (WrappedModelSelectionChanged);
			}

			((UVEditorDieModel)model).GetDieModelSelectionSupport ().DetachModel (model);
		}

		private void Foo ()
		{
			// Initial U distribution // TODO do this in editor!
			//				float currentDist = 0.0f;
			//				foreach (Edge e in this.edges) {
			//					UnwrappedVertex v0 = this.vertices [e.GetFromVertexIndex ()];
			//					UnwrappedVertex v1 = this.vertices [e.GetToVertexIndex ()];
			//					//					float magnitude = 0.0f;
			//					float len = e.GetLength (this);
			//					v0.uv = new Vector2 (currentDist / totalLength, 0f);
			//					
			//					currentDist += len;
			//					v1.uv = new Vector2 (currentDist / totalLength, 0f);
			//					
			//				}
		}
//		public UnwrappedDieModel UnwrappedModel {
//			get {
//				if (null == _unwrappedModel) {
//					_unwrappedModel = UnwrappedDieModel.Build (DieModel);
//				}
//				return _unwrappedModel;
//			}
//		}

		protected override void DrawCustomToolbar ()
		{
			EditorGUI.BeginChangeCheck ();
			showSelectedEdgesOnly = GUILayout.Toggle (showSelectedEdgesOnly, "Filter", EditorStyles.toolbarButton);
			EditorGUI.BeginDisabledGroup (!showSelectedEdgesOnly);
			alsoShowEdgesOnSameStrip = GUILayout.Toggle (alsoShowEdgesOnSameStrip, "+Strip", EditorStyles.toolbarButton);
			EditorGUI.EndDisabledGroup ();
			if (EditorGUI.EndChangeCheck ()) {
				Repaint ();
			}

			EditorGUI.BeginChangeCheck ();
//			this.texture = (Texture2D)EditorGUI.ObjectField (rect, this.texture, typeof(Texture2D), true);
			this.texture = (Texture2D)EditorGUILayout.ObjectField (texture, typeof(Texture2D), true, GUILayout.ExpandWidth (false));
			if (EditorGUI.EndChangeCheck ()) {
				Repaint ();
			}

			if (null != texture) {
				int w = texture.width;
				int h = texture.height;
				

				float[] aspectRatios = new float[] {
					1.0f, (float)w / (float)h,
				};
				int selectedAspectRatioIndex = 0;

				string[] aspectRatioStrings = new string[aspectRatios.Length];
				for (int i = 0; i < aspectRatios.Length; i++) {
					float aspectRatio = aspectRatios [i];
					if (this.CanvasScaleAspectRatio == aspectRatio) {
						selectedAspectRatioIndex = i;
					}
					if (aspectRatio == Mathf.Round (aspectRatio)) {
						aspectRatioStrings [i] = string.Format ("{0:f0}:1", aspectRatio);
					} else {
						aspectRatioStrings [i] = string.Format ("{0:f2}:1", aspectRatio);
					}
				}


				selectedAspectRatioIndex = EditorGUILayout.Popup (selectedAspectRatioIndex, aspectRatioStrings, EditorStyles.toolbarDropDown, GUILayout.ExpandWidth (false));
				//this.vScaling = aspectRatios [selectedAspectRatioIndex];
				this.CanvasScaleAspectRatio = aspectRatios [selectedAspectRatioIndex];
			}

		}

		protected override bool IsEdgeVisible (int edgeIndex)
		{
			bool visible;
			if (showSelectedEdgesOnly) {
				UnwrappedDieModel unwrappedModel = DieModel.UnwrappedModel;
				IDieModelEditorSupport wrappedEditorSupport = unwrappedModel.WrappedModel as IDieModelEditorSupport;
				if (null != wrappedEditorSupport) {
					visible = false;



					int[] selectedEdges = wrappedEditorSupport.GetDieModelSelectionSupport ().GetSelectedEdgeIndices ();




					if (alsoShowEdgesOnSameStrip) {
						int stripIndex = unwrappedModel.GetEdgeStripIndex (edgeIndex);
						int[] stripEdges = unwrappedModel.GetStripAt (stripIndex);
						HashSet<int> wrappedStripEdgeIndices = new HashSet<int> ();
						foreach (int ei in stripEdges) {
							int wrappedEdgeIndex = unwrappedModel.GetWrappedEdgeIndex (ei);
							wrappedStripEdgeIndices.Add (wrappedEdgeIndex);
						}
						foreach (int ei in selectedEdges) {
							if (wrappedStripEdgeIndices.Contains (ei)) {
								visible = true;
								break;
							}
						}
					} else {
						// Selected edge indices are in wrapped model's nomenclature
						int wrappedEdgeIndex = unwrappedModel.GetWrappedEdgeIndex (edgeIndex);
						foreach (int ei in selectedEdges) {
							if (wrappedEdgeIndex == ei) {
								visible = true;
								break;
							}
						}
					}



				} else {
					visible = true;
				}
			} else {
				visible = true;
			}
			return visible;
		}

		protected override void OnDrawModel ()
		{
//			Texture txt = (Texture)Resources.Load ("tunnelslice01", typeof(Texture));

			if (null != texture) {




				Vector3 bottomLeft = TransformModelPoint (Vector3.zero);
				Vector3 topRight = TransformModelPoint (Vector3.one);
//				float textureAspectRatio = (float)texture.width / (float)texture.height;
//				Vector3 topRight = TransformModelPoint (new Vector3 (1.0f, 1.0f / (this.CanvasScaleAspectRatio / textureAspectRatio)));

				Rect rect = new Rect (
					bottomLeft.x,
					topRight.y,
					topRight.x - bottomLeft.x,
					bottomLeft.y - topRight.y);

				// TODO we need to manually clip the graphics
				//GUI.Box (rect, texture, GUIStyle.none);
				//Graphics.DrawTexture (rect, texture);
				GUI.DrawTexture (rect, texture);
				// Check if we need to repeat the texture:

				float minY = float.MaxValue;
				float maxY = float.MinValue;
				foreach (Vector3 v in DieModel.GetVertices()) {
					if (v.y < minY) {
						minY = v.y;
					}
					if (v.y > maxY) {
						maxY = v.y;
					}
				}
				int repeatBelow = -Mathf.RoundToInt (minY - 0.5f);
				for (int i = 0; i < repeatBelow; i++) {
					Rect r = new Rect (rect.position, rect.size);
					r.y += r.height * (float)(i + 1);
					GUI.DrawTexture (r, texture);
				}
			}
			//Resources.UnloadAsset (txt);
			base.OnDrawModel ();

			// Draw faces for an (imaginery) extruded segment:
//			DieModel.GetVertices();
			int vertexCount = DieModel.GetVertexCount ();
			int edgeCount = DieModel.GetEdgeCount ();


			Vector3 canvasUp1 = TransformModelVector (new Vector3 (0, 1, 0));

			for (int ei = 0; ei < edgeCount; ei++) {
				Edge e = DieModel.GetEdgeAt (ei);
				int fromVertex = e.GetFromVertexIndex ();
				Vector3 fromVertexPos;
				if (CurrentModelTool.IsVertexInToolContext (fromVertex)) {
					fromVertexPos = CurrentModelTool.GetVertexToolContextPosition (fromVertex);
				} else {
					fromVertexPos = DieModel.GetVertexAt (fromVertex);
				}

				int toVertex = e.GetToVertexIndex ();
				Vector3 toVertexPos;
				if (CurrentModelTool.IsVertexInToolContext (toVertex)) {
					toVertexPos = CurrentModelTool.GetVertexToolContextPosition (toVertex);
				} else {
					toVertexPos = DieModel.GetVertexAt (toVertex);
				}

				Vector3 pt0 = TransformModelPoint (fromVertexPos);
				Vector3 pt1 = pt0 + canvasUp1;
				Vector3 pt2 = TransformModelPoint (toVertexPos) + canvasUp1;
				DrawEdge (pt0, pt1, false, false, false, "");
				DrawEdge (pt1, pt2, false, false, false, "");

				if (ei == edgeCount - 1) {
					Vector3 pt3 = TransformModelPoint (toVertexPos);
					DrawEdge (pt2, pt3, false, false, false, "");
				}
			}
		}

//		protected override void OnDrawModel ()
//		{
//			UnwrappedDieModel mdl = UnwrappedModel;
//			if (null != mdl) {
//				int vcount = mdl.GetVertexCount ();
//				for (int i = 0; i < vcount; i++) {
//					if (1 != mdl.GetVertexUnwrappedStripIndex (i)) {
//						continue;
//					}
//					//Vector3 v = mdl.GetVertexAt (i);
//
//					// Ortho from above:
//					//v.y = -1f;
//
//					Vector3 v = mdl.GetUvAt (i);
////					v.y = -1f;
//
//					Vector3 pos = TransformModelPoint (v);
//
//					//bool selected
//					int wi = mdl.GetWrappedVertexIndex (i);
//
//					bool selected = SelectionSupport.IsVertexSelected (wi);
//					bool focused = SelectionSupport.GetFocusedVertexIndex () == wi;
//
//					DoVertexHandle (pos, wi, false, selected, focused);
//				}
//
//			}
//		}
	}
}
