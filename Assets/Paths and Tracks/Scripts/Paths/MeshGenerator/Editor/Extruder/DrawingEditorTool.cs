// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Paths.MeshGenerator.Extruder.Editor
{

	public interface IDrawingEditorToolContext
	{
		object GetModel ();
		DrawingEditorWindow GetEditor ();
	
		Vector3 GetCursorCanvasPos ();
		Vector3 GetCursorModelPos ();
	
		Vector3 TransformModelPoint (Vector3 pt);
		Vector3 TransformModelVector (Vector3 v);
		Vector3 InverseTransformModelPoint (Vector3 pt);
		Vector3 InverseTransformModelVector (Vector3 v);
	}

// TODO this must be an interface!
	public interface IDrawingEditorTool
	{
		string GetToolId ();

		// TODO replace with MouseEvent, KeyEvent?
		bool BeginDrag (IDrawingEditorToolContext context);
		void Drag (IDrawingEditorToolContext context);
		void EndDrag (IDrawingEditorToolContext context);
		void Cancel (IDrawingEditorToolContext context);

		bool MouseDown (IDrawingEditorToolContext context);
		void MouseUp (IDrawingEditorToolContext context);
		void MouseMove (IDrawingEditorToolContext context);
		bool Key (IDrawingEditorToolContext context);

		void OnGUI (IDrawingEditorToolContext context);
	}


	public abstract class AbstractDrawingEditorTool : IDrawingEditorTool
	{
		private IDrawingEditorToolContext toolContext;

		public abstract string GetToolId ();

		public virtual bool BeginDrag (IDrawingEditorToolContext context)
		{
			return false;
		}
	
		public virtual void Drag (IDrawingEditorToolContext context)
		{
		}
	
		public virtual void EndDrag (IDrawingEditorToolContext context)
		{
		}
	
		public virtual void Cancel (IDrawingEditorToolContext context)
		{
		}
	
		public virtual bool MouseDown (IDrawingEditorToolContext context)
		{
			return false;
		}
	
		public virtual void MouseUp (IDrawingEditorToolContext context)
		{
		}
	
		public virtual void MouseMove (IDrawingEditorToolContext context)
		{
		}
	
		public virtual bool Key (IDrawingEditorToolContext context)
		{
			return false;
		}
	
		public virtual void OnGUI (IDrawingEditorToolContext context)
		{
		}

//	protected IDrawingEditorToolContext context {
//		get {
//			return toolContext;
//		}
//	} 
//	
//	private void WithContext (IDrawingEditorToolContext context, System.Action a)
//	{
//		IDrawingEditorToolContext prevContext = this.toolContext;
//		this.toolContext = context;
//		try {
//			a ();
//		} finally {
//			this.toolContext = prevContext;
//		}
//	}
//	private T WithContextReturn<T> (IDrawingEditorToolContext context, System.Func<T> f)
//	{
//		IDrawingEditorToolContext prevContext = this.toolContext;
//		this.toolContext = context;
//		try {
//			return f ();
//		} finally {
//			this.toolContext = prevContext;
//		}
//	}
//	public override sealed bool BeginDrag (IDrawingEditorToolContext context)
//	{
//		return WithContextReturn<bool> (context, BeginDrag);
//	}
//	
//	public override sealed void Drag (IDrawingEditorToolContext context)
//	{
//		WithContext (context, Drag);
//	}
//	
//	public override sealed void EndDrag (IDrawingEditorToolContext context)
//	{
//		WithContext (context, EndDrag);
//	}
//	
//	public override sealed void Cancel (IDrawingEditorToolContext context)
//	{
//		WithContext (context, Cancel);
//	}
//
//	public override sealed bool MouseDown (IDrawingEditorToolContext context)
//	{
//		return WithContextReturn<bool> (context, MouseDown);
//	}
//
//	public override sealed void MouseUp (IDrawingEditorToolContext context)
//	{
//		WithContext (context, MouseUp);
//	}
//
//	public override sealed void MouseMove (IDrawingEditorToolContext context)
//	{
//		WithContext (context, MouseMove);
//	}
//
//	public override sealed bool Key (IDrawingEditorToolContext context)
//	{
//		return WithContextReturn<bool> (context, Key);
//	}
//
//	public override sealed void OnGUI (IDrawingEditorToolContext context)
//	{
//		WithContext (context, OnGUI);
//	}
//	
//	/* Contextless versions, context is already stored in 'toolContext' field */
//	public virtual bool BeginDrag ()
//	{
//		return false;
//	}
//	
//	public virtual void Drag ()
//	{
//	}
//	
//	public virtual void EndDrag ()
//	{
//	}
//	
//	public virtual void Cancel ()
//	{
//	}
//	
//	public virtual bool MouseDown ()
//	{
//		return false;
//	}
//	
//	public virtual void MouseUp ()
//	{
//	}
//
//	public virtual void MouseMove ()
//	{
//	}
//	
//	public virtual bool Key ()
//	{
//		return false;
//	}
//	
//	public virtual void OnGUI ()
//	{
//	}
	}

	public sealed class NoDrawingEditorTool : AbstractDrawingEditorTool
	{
		private static NoDrawingEditorTool _instance = new NoDrawingEditorTool ();
		public static NoDrawingEditorTool Instance {
			get {
				return _instance;
			}
		}
		private NoDrawingEditorTool ()
		{
		}
		public override string GetToolId ()
		{
			return typeof(NoDrawingEditorTool).FullName;
		}
	}
}