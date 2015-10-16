// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using System;
using UnityEngine;
using System.Collections.Generic;

using Util;
using Paths;
using Paths.MeshGenerator;
using Paths.MeshGenerator.Extruder.Model.Internal;

namespace Paths.MeshGenerator.Extruder.Model
{
	public class DieModelSelectionEventArgs : System.EventArgs
	{
		public enum SelectionTarget
		{
			FocusedVertex,
			FocusedEdge,
			SelectedVertices,
			SelectedEdges,
		}

		private IDieModelSelectionSupport selectionSupport;
		private SelectionTarget target;
		private int[] indices;

		public DieModelSelectionEventArgs (IDieModelSelectionSupport selectionSupport, SelectionTarget target, int[] indices)
		{
			this.selectionSupport = selectionSupport;
			this.target = target;
			this.indices = new int[indices.Length];
			Array.Copy (indices, this.indices, indices.Length);
		}

		public IDieModelSelectionSupport SelectionSupport {
			get {
				return selectionSupport;
			}
		}
		public SelectionTarget Target {
			get {
				return target;
			}
		}
		public int[] Indices {
			get {
				return indices;
			}
		}
		public int FocusedVertexIndex {
			get {
				return selectionSupport.GetFocusedVertexIndex ();
			}
		}
		public int[] SelectedVertexIndices {
			get {
				return selectionSupport.GetSelectedVertexIndices ();
			}
		}
		public int FocusedEdgeIndex {
			get {
				return selectionSupport.GetFocusedEdgeIndex ();
			}
		}
		public int[] SelectedEdgeIndices {
			get {
				return selectionSupport.GetSelectedEdgeIndices ();
			}
		}
	}
	
	public delegate void DieModelSelectionChangedHandler (DieModelSelectionEventArgs e);

	public interface IDieModelSelectionSupport
	{
		void AttachModel (IDieModel model);
		void DetachModel (IDieModel model);

		void AddSelectionChangedChangeHandler (DieModelSelectionChangedHandler h);
		void RemoveSelectionChangedChangeHandler (DieModelSelectionChangedHandler h);

		int GetFocusedVertexIndex ();
		void SetFocusedVertexIndex (int index);
		int GetFocusedEdgeIndex ();
		void SetFocusedEdgeIndex (int index);

		int[] GetSelectedVertexIndices ();
		bool IsVertexSelected (int index);
		void SetVertexSelected (int index, bool selected);
		void ClearSelectedVertices ();
		void SetSelectedVertexIndices (int[] indices);
		bool ToggleVertexSelected (int index);

		int[] GetSelectedEdgeIndices ();
		bool IsEdgeSelected (int index);
		void SetEdgeSelected (int index, bool selected);
		void ClearSelectedEdges ();
		void SetSelectedEdgeIndices (int[] indices);
		bool ToggleEdgeSelected (int index);

	}

	public class DefaultDieModelSelectionSupport : IDieModelSelectionSupport
	{
		private event DieModelSelectionChangedHandler SelectionChanged;
		[NonSerialized]
		private IDieModel
			dieModel;

		[NonSerialized]
		private int
			focusedVertexIndex = -1;
		
		[NonSerialized]
		private int
			focusedEdgeIndex = -1;
		
		[NonSerialized]
		private List<int>
			selectedVertexIndices = new List<int> ();
		
		[NonSerialized]
		private List<int>
			selectedEdgeIndices = new List<int> ();

		public void AttachModel (IDieModel model)
		{
			this.dieModel = model;
			if (null != this.dieModel && this.dieModel is IMutableDieModel) {
				// Add listener
				((IMutableDieModel)this.dieModel).AddDieModelChangeHandler (DieModelChanged);
			}
		}
		public void DetachModel (IDieModel model)
		{
			if (null != this.dieModel && this.dieModel is IMutableDieModel) {
				// Add listener
				((IMutableDieModel)this.dieModel).AddDieModelChangeHandler (DieModelChanged);
			}
			this.dieModel = null;
		}

		public void AddSelectionChangedChangeHandler (DieModelSelectionChangedHandler h)
		{
			this.SelectionChanged -= h;
			this.SelectionChanged += h;
		}
		public void RemoveSelectionChangedChangeHandler (DieModelSelectionChangedHandler h)
		{
			this.SelectionChanged -= h;
		}

		private void DieModelChanged (DieModelChangedEventArgs e)
		{
			if (e.IsAfterEvent) {
				if (e.Reason == DieModelChangedEventArgs.EventReason.EdgesRemoved) {
					// Remove from selected edges
					foreach (int edgeIndex in e.Indices) {
						if (edgeIndex == GetFocusedEdgeIndex ()) {
							SetFocusedEdgeIndex (-1);
						}
						SetEdgeSelected (edgeIndex, false);
					}
				} else if (e.Reason == DieModelChangedEventArgs.EventReason.VerticesRemoved) {
					// Remove from selected vertices
					foreach (int vertexIndex in e.Indices) {
						if (vertexIndex == GetFocusedVertexIndex ()) {
							SetFocusedVertexIndex (-1);
						}
						SetVertexSelected (vertexIndex, false);
					}
				}
			}
		}


		private void FireSelectionChangedEvent (DieModelSelectionEventArgs.SelectionTarget target, params int[] indices)
		{
			if (null != SelectionChanged) {
				try {
					DieModelSelectionEventArgs e = new DieModelSelectionEventArgs (this, target, indices);
					SelectionChanged (e);
				} catch (Exception ex) {
					Debug.LogError ("An exception in SelectionChanged handler: " + ex);
				}
			}
			
		}


		public int GetFocusedVertexIndex ()
		{
			return focusedVertexIndex;
		}
		public void SetFocusedVertexIndex (int index)
		{
			bool changed = this.focusedVertexIndex != index;
			this.focusedVertexIndex = index;
			if (changed) {
				FireSelectionChangedEvent (DieModelSelectionEventArgs.SelectionTarget.FocusedVertex, focusedVertexIndex);
			}
		}
		public int GetFocusedEdgeIndex ()
		{
			return focusedEdgeIndex;
		}
		public void SetFocusedEdgeIndex (int index)
		{
			bool changed = this.focusedEdgeIndex != index;
			this.focusedEdgeIndex = index;
			if (changed) {
				FireSelectionChangedEvent (DieModelSelectionEventArgs.SelectionTarget.FocusedEdge, focusedEdgeIndex);
			}
		}
		
		public int[] GetSelectedVertexIndices ()
		{
			return selectedVertexIndices.ToArray ();
		}
		public bool IsVertexSelected (int index)
		{
			return selectedVertexIndices.Contains (index);
		}
		public void SetVertexSelected (int index, bool selected)
		{
			bool changed;
			if (!selected && selectedVertexIndices.Contains (index)) {
				changed = true;
				selectedVertexIndices.Remove (index);
			} else if (selected) {
				changed = true;
				selectedVertexIndices.Add (index);
			} else {
				changed = false;
			}
			if (changed) {
				FireSelectionChangedEvent (DieModelSelectionEventArgs.SelectionTarget.SelectedVertices, this.selectedVertexIndices.ToArray ());
			}
		}
		public void ClearSelectedVertices ()
		{
			bool changed = selectedVertexIndices.Count > 0;
			this.selectedVertexIndices.Clear ();
			if (changed) {
				FireSelectionChangedEvent (DieModelSelectionEventArgs.SelectionTarget.SelectedVertices, new int[0]);
			}
		}
		public void SetSelectedVertexIndices (int[] indices)
		{
			selectedVertexIndices.Clear ();
			selectedVertexIndices.AddRange (indices);
			FireSelectionChangedEvent (DieModelSelectionEventArgs.SelectionTarget.SelectedVertices, this.selectedVertexIndices.ToArray ());
		}
		public bool ToggleVertexSelected (int index)
		{
			bool selected;
			if (IsVertexSelected (index)) {
				SetVertexSelected (index, false);
				selected = false;
			} else {
				SetVertexSelected (index, true);
				selected = true;
			}
			return selected;
		}
		
		public int[] GetSelectedEdgeIndices ()
		{
			return selectedEdgeIndices.ToArray ();
		}
		public bool IsEdgeSelected (int index)
		{
			return selectedEdgeIndices.Contains (index);
		}
		public void SetEdgeSelected (int index, bool selected)
		{
			bool changed;
			if (!selected && selectedEdgeIndices.Contains (index)) {
				selectedEdgeIndices.Remove (index);
				changed = true;
			} else if (selected) {
				selectedEdgeIndices.Add (index);
				changed = true;
			} else {
				changed = false;
			}
			if (changed) {
				FireSelectionChangedEvent (DieModelSelectionEventArgs.SelectionTarget.SelectedEdges, this.selectedEdgeIndices.ToArray ());
			}
		}
		public void ClearSelectedEdges ()
		{
			bool changed = selectedEdgeIndices.Count > 0;
			this.selectedEdgeIndices.Clear ();
			if (changed) {
				FireSelectionChangedEvent (DieModelSelectionEventArgs.SelectionTarget.SelectedEdges, new int[0]);
			}
		}
		public void SetSelectedEdgeIndices (int[] indices)
		{
			selectedEdgeIndices.Clear ();
			selectedEdgeIndices.AddRange (indices);
			FireSelectionChangedEvent (DieModelSelectionEventArgs.SelectionTarget.SelectedEdges, this.selectedEdgeIndices.ToArray ());
		}

		public bool ToggleEdgeSelected (int index)
		{
			bool selected;
			if (IsEdgeSelected (index)) {
				SetEdgeSelected (index, false);
				selected = false;
			} else {
				SetEdgeSelected (index, true);
				selected = true;
			}
			return selected;
		}
	}
	public sealed class NoDieModelSelectionSupport : IDieModelSelectionSupport
	{
		private static readonly NoDieModelSelectionSupport _instance = new NoDieModelSelectionSupport ();
		public static IDieModelSelectionSupport Instance {
			get {
				return _instance;
			}
		}
		public void AttachModel (IDieModel model)
		{
		}
		public void DetachModel (IDieModel model)
		{
		}

		public void AddSelectionChangedChangeHandler (DieModelSelectionChangedHandler h)
		{
		}
		public void RemoveSelectionChangedChangeHandler (DieModelSelectionChangedHandler h)
		{
		}
	
//			public void FireRefreshEvent ()
//			{
//	
//			}
	
		public int GetFocusedVertexIndex ()
		{
			return -1;
		}
		public void SetFocusedVertexIndex (int index)
		{
		}
		public int GetFocusedEdgeIndex ()
		{
			return -1;
		}
		public void SetFocusedEdgeIndex (int index)
		{
		}
			
		public int[] GetSelectedVertexIndices ()
		{
			return new int[0];
		}
		public bool IsVertexSelected (int index)
		{
			return false;
		}
		public void SetVertexSelected (int index, bool selected)
		{
		}
		public void ClearSelectedVertices ()
		{
		}
		public void SetSelectedVertexIndices (int[] indices)
		{
		}
		public bool ToggleVertexSelected (int index)
		{
			return false;
		}
			
		public int[] GetSelectedEdgeIndices ()
		{
			return new int[0];
		}
		public bool IsEdgeSelected (int index)
		{
			return false;
		}
	
		public void SetEdgeSelected (int index, bool selected)
		{
		}
		public void ClearSelectedEdges ()
		{
		}
		public void SetSelectedEdgeIndices (int[] indices)
		{
		}
		public bool ToggleEdgeSelected (int index)
		{
			return false;
		}
	
	}
}
