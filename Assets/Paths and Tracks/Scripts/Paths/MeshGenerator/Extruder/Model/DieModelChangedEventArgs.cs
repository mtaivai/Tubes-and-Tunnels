// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using System;
using UnityEngine;
using System.Collections.Generic;

using Util;
using Paths;
using Paths.MeshGenerator;
using Paths.MeshGenerator.SliceStrip;

namespace Paths.MeshGenerator.Extruder.Model
{

	public enum EventPhase
	{
		Before,
		BeforeVetoable,
		After,
	}

	public class DieModelChangedEventArgs : System.EventArgs
	{

		public enum EventReason
		{
			RefreshModel,
			BatchOperation,
			VerticesAdded,
			VerticesRemoved,
			VerticesModified,
			EdgesAdded,
			EdgesRemoved,
			EdgesModified
		}
		//private System.Object source;
		private IDieModel model;
		private EventPhase phase;
		private EventReason reason;
		private int[] indices;
		private String batchOperationName;

		public static DieModelChangedEventArgs RefreshModel (IDieModel model)
		{
			return new DieModelChangedEventArgs (model, EventPhase.After, EventReason.RefreshModel, new int[0]);
		}
		public static DieModelChangedEventArgs VerticesAdded (IDieModel model, params int[] indices)
		{
			return new DieModelChangedEventArgs (model, EventReason.VerticesAdded, indices);
		}
		public static DieModelChangedEventArgs VerticesRemoved (IDieModel model, params int[] indices)
		{
			return new DieModelChangedEventArgs (model, EventReason.VerticesRemoved, indices);
		}
		public static DieModelChangedEventArgs VerticesModified (IDieModel model, params int[] indices)
		{
			return new DieModelChangedEventArgs (model, EventReason.VerticesModified, indices);
		}
		public static DieModelChangedEventArgs EdgesAdded (IDieModel model, params int[] indices)
		{
			return new DieModelChangedEventArgs (model, EventReason.EdgesAdded, indices);
		}
		public static DieModelChangedEventArgs EdgesRemoved (IDieModel model, params int[] indices)
		{
			return new DieModelChangedEventArgs (model, EventReason.EdgesRemoved, indices);
		}
		public static DieModelChangedEventArgs EdgesModified (IDieModel model, params int[] indices)
		{
			return new DieModelChangedEventArgs (model, EventReason.EdgesModified, indices);
		}

		public DieModelChangedEventArgs (DieModelChangedEventArgs src)
		{
			this.model = src.model;
			this.phase = src.phase;
			this.reason = src.reason;
			this.indices = src.indices;
			this.batchOperationName = src.batchOperationName;
		}
		public DieModelChangedEventArgs (DieModelChangedEventArgs src, EventPhase phase) : this(src)
		{
			this.phase = phase;
		}

		public DieModelChangedEventArgs (IDieModel model, EventPhase phase, EventReason reason, int[] indices)
		{
			this.model = model;
			this.phase = phase;
			this.reason = reason;
			this.indices = indices;
		}
		public DieModelChangedEventArgs (IDieModel model, EventReason reason, int[] indices)
			: this(model, EventPhase.After, reason, indices)
		{
		}
		public DieModelChangedEventArgs (IDieModel model, EventPhase phase, string batchOperationName)
		{
			this.model = model;
			this.phase = phase;
			this.reason = EventReason.BatchOperation;
			this.batchOperationName = batchOperationName;
			this.indices = new int[0];
		}
		public EventReason Reason {
			get {
				return reason;
			}
		}
		public EventPhase Phase {
			get {
				return phase;
			}
		}
		public bool IsBeforeEvent {
			get {
				return phase == EventPhase.Before || phase == EventPhase.BeforeVetoable;
			}
		}
		public bool IsAfterEvent {
			get {
				return phase == EventPhase.After;
			}
		}
		public bool IsUndoable {
			get {
				return reason != EventReason.RefreshModel;
			}
		}
		public IDieModel Model {
			get {
				return model;
			}
		}
		public int[] Indices {
			get {
				return indices;
			}
			set {
				this.indices = value;
			}
		}

		public string BatchOperationName {
			get {
				return batchOperationName;
			}
		}
		//private 
	}
	
//	[Serializable]

}
