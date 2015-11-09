// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
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
	public interface IDieEditorToolContext : IDrawingEditorToolContext
	{
		DieEditorWindow GetDieEditor ();
		IDieModel GetDieModel ();
		IMutableDieModel GetMutableDieModel ();
		bool IsMutableModel ();

		int GetFocusedVertexIndex ();
		int GetFocusedEdgeIndex ();
		Vector3 GetProjectedPointOnFocusedEdge ();

		bool IsVertexSelected (int index);
		void SetVertexSelected (int index, bool value, bool allowMultiSel);
		int[] GetSelectedVertices ();
	
		bool IsEdgeSelected (int index);
		void SetEdgeSelected (int index, bool value, bool allowMultiSel);
		int[] GetSelectedEdges ();
			
	}

	public enum DieEditorSelectMode
	{
		None,
		Single,
		Multi,
	}


	[AttributeUsage(AttributeTargets.Method)]
	public class EditorAction : Attribute
	{
		public const int DefaultGroup = 0;
		public const int DefaultPriority = 0;

		private string name;
		private bool isValidateFunction;
		private int priority;
		private string validateFunc;
		private int group;


		public EditorAction (int group, string name) : this(group, name, false, DefaultPriority)
		{
		}
		public EditorAction (int group, string name, bool isValidateFunction, int priority)
		{
			this.group = group;
			this.name = name;
			this.isValidateFunction = isValidateFunction;
			this.priority = priority;
			this.validateFunc = null;
		}
		public EditorAction (int group, string name, int priority, string validateFunction) : this(group, name, false, priority)
		{
			this.validateFunc = validateFunction;
		}

		public EditorAction (int group, string name, string validateFunction) : this(group, name, DefaultPriority, validateFunction)
		{
		}

		/******** Constructors without group assignment ************/
		public EditorAction (string name) : this(DefaultGroup, name)
		{
		}
		public EditorAction (string name, bool isValidateFunction, int priority) : this(DefaultGroup, name, isValidateFunction, priority)
		{
		}
		public EditorAction (string name, int priority, string validateFunction) : this(DefaultGroup, name, priority, validateFunction)
		{
		}
		public EditorAction (string name, string validateFunction) : this(DefaultGroup, name, validateFunction)
		{
		}

		public int Group {
			get {
				return group;
			}
		}

		public string Name {
			get {
				return this.name;
			}
		}
		public bool IsValidateFunction {
			get {
				return this.isValidateFunction;
			}
		}
		public int Priority {
			get {
				return this.priority;
			}
		}
		public string ValidateFunc {
			get {
				return this.validateFunc;
			}
		}
	}

	public interface IDieEditorTool : IDrawingEditorTool
	{
		void VertexDeleted (int vertexIndex);
		void EdgeDeleted (int edgeIndex);
		bool IsVertexInToolContext (int vertexIndex);
	
		Vector3 GetVertexToolContextPosition (int vertexIndex);
		DieEditorSelectMode GetSupportedVertexSelectMode ();
		DieEditorSelectMode GetSupportedEdgeSelectMode ();
	}

	public abstract class DieEditorToolDrawingEditorToolAdapter : AbstractDrawingEditorTool
	{
		public override sealed bool BeginDrag (IDrawingEditorToolContext context)
		{
			return BeginDrag ((IDieEditorToolContext)context);
		}
	
		public override sealed void Drag (IDrawingEditorToolContext context)
		{
			Drag ((IDieEditorToolContext)context);
		}
	
		public override sealed void EndDrag (IDrawingEditorToolContext context)
		{
			EndDrag ((IDieEditorToolContext)context);
		}
	
		public override sealed void Cancel (IDrawingEditorToolContext context)
		{
			Cancel ((IDieEditorToolContext)context);
		}
	
		public override sealed bool MouseDown (IDrawingEditorToolContext context)
		{
			return MouseDown ((IDieEditorToolContext)context);
		}
	
		public override sealed void MouseUp (IDrawingEditorToolContext context)
		{
			MouseUp ((IDieEditorToolContext)context);
		}
	
		public override sealed void MouseMove (IDrawingEditorToolContext context)
		{
			MouseMove ((IDieEditorToolContext)context);
		}
	
		public override sealed bool Key (IDrawingEditorToolContext context)
		{
			return Key ((IDieEditorToolContext)context);
		}
	
		public override sealed void OnGUI (IDrawingEditorToolContext context)
		{
			OnGUI ((IDieEditorToolContext)context);
		}
	
		public virtual bool BeginDrag (IDieEditorToolContext context)
		{
			return false;
		}
	
		public virtual void Drag (IDieEditorToolContext context)
		{
		}
	
		public virtual void EndDrag (IDieEditorToolContext context)
		{
		}
	
		public virtual void Cancel (IDieEditorToolContext context)
		{
		}
	
		public virtual bool MouseDown (IDieEditorToolContext context)
		{
			return false;
		}
	
		public virtual void MouseUp (IDieEditorToolContext context)
		{
		}
	
		public virtual void MouseMove (IDieEditorToolContext context)
		{
		}
	
		public virtual bool Key (IDieEditorToolContext context)
		{
			return false;
		}
	
		public virtual void OnGUI (IDieEditorToolContext context)
		{
		}
	}

	public abstract class DieEditorTool : DieEditorToolDrawingEditorToolAdapter, IDieEditorTool
	{

		public delegate void ContextMenuAction0 ();
		public delegate void ContextMenuAction1 (IDieEditorToolContext context);
		public delegate bool ValidateContextMenuAction0 ();
		public delegate bool ValidateContextMenuAction1 (IDieEditorToolContext context);


		private sealed class NoTool : DieEditorTool
		{
			public NoTool () : base()
			{
			}
		
		}
		public static readonly DieEditorTool None = new NoTool ();



		protected DieEditorTool ()
		{

		}

		public override string GetToolId ()
		{
			return GetType ().FullName;
		}


		public virtual void VertexDeleted (int vertexIndex)
		{
		}
		public virtual void EdgeDeleted (int edgeIndex)
		{
		}
	
		public virtual bool IsVertexInToolContext (int vertexIndex)
		{
			return false;
		}
	
		public virtual Vector3 GetVertexToolContextPosition (int vertexIndex)
		{
			throw new System.NotSupportedException ();
		}
		public virtual DieEditorSelectMode GetSupportedVertexSelectMode ()
		{
			return DieEditorSelectMode.None;
		}
		public virtual DieEditorSelectMode GetSupportedEdgeSelectMode ()
		{
			return DieEditorSelectMode.None;
		}


		private class EditorActionInfo
		{
			public int group = EditorAction.DefaultGroup;
			public int priority = EditorAction.DefaultPriority;
			public string name;
			public Delegate action;
			public Delegate validateAction;
			public EditorAction eaAttr;
		}

		private static List<EditorActionInfo> CollectActions (object target)
		{
		
			// Find available Actions
			Dictionary<string, EditorActionInfo> actionInfos = new Dictionary<string, EditorActionInfo> ();

			BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | 
				BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

			MethodInfo[] allMethods = target.GetType ().GetMethods (bindingFlags);
			foreach (MethodInfo mi in allMethods) {
				object[] attrObjs = mi.GetCustomAttributes (typeof(EditorAction), true);
				if (attrObjs.Length > 0) {
					object delegateTarget = mi.IsStatic ? null : target;
					ParameterInfo[] pis = mi.GetParameters ();
					EditorAction ea = (EditorAction)attrObjs [0];

					Delegate actionDelegate;
					if (ea.IsValidateFunction) {
						if (mi.ReturnType != typeof(bool)) {
							// Not a valid validate function!
							// TODO should we log about this?
							actionDelegate = null;
						} else if (pis.Length == 0) {
							actionDelegate = Delegate.CreateDelegate (typeof(ValidateContextMenuAction0), delegateTarget, mi);
						} else if (pis.Length == 1 && pis [0].ParameterType.IsAssignableFrom (typeof(IDieEditorToolContext))) {
							actionDelegate = Delegate.CreateDelegate (typeof(ValidateContextMenuAction1), delegateTarget, mi);
						} else {
							// Not a valid validate function!
							// TODO should we log about this?
							actionDelegate = null;
						}
					} else {
						if (pis.Length == 0) {
							actionDelegate = Delegate.CreateDelegate (typeof(ContextMenuAction0), delegateTarget, mi);
						} else if (pis.Length == 1 && pis [0].ParameterType.IsAssignableFrom (typeof(IDieEditorToolContext))) {
							actionDelegate = Delegate.CreateDelegate (typeof(ContextMenuAction1), delegateTarget, mi);
						} else {
							// Not a valid action function!
							// TODO should we log about this?
							actionDelegate = null;
						}

					}

					if (null != actionDelegate) {
						EditorActionInfo eai;
						string name = ea.Name;
						if (actionInfos.ContainsKey (name)) {
							eai = actionInfos [name];
						} else {
							eai = new EditorActionInfo ();
							eai.name = name;
							actionInfos.Add (name, eai);
						}
						if (ea.IsValidateFunction) {
							eai.validateAction = actionDelegate;
						} else {
							eai.action = actionDelegate;
							eai.eaAttr = ea;
							eai.group = ea.Group;
							eai.priority = ea.Priority;
						}

					}
				}
			}

			// Try to find missing validate actions by method name
			foreach (EditorActionInfo eai in actionInfos.Values) {
				if (null == eai.validateAction) {

					bool validateMethodSpecified;
					string validateMethodName = eai.eaAttr.ValidateFunc;
					if (Util.StringUtil.IsEmpty (validateMethodName)) {
						validateMethodSpecified = false;
						string methodName = eai.action.Method.Name;
						validateMethodName = "Validate" + methodName;
					} else {
						validateMethodSpecified = true;
					}
					// First try a method with context:
					MethodInfo mi = target.GetType ().GetMethod (validateMethodName, bindingFlags, null, new Type[] {typeof(IDieEditorToolContext)}, null);
					if (null != mi && mi.ReturnType == typeof(bool)) {
						object delegateTarget = mi.IsStatic ? null : target;
						eai.validateAction = Delegate.CreateDelegate (typeof(ValidateContextMenuAction1), delegateTarget, mi);
					} else {
						// Alt version is one without context:
						mi = target.GetType ().GetMethod (validateMethodName, bindingFlags, null, new Type[0], null);
						if (null != mi && mi.ReturnType == typeof(bool)) {
							object delegateTarget = mi.IsStatic ? null : target;
							eai.validateAction = Delegate.CreateDelegate (typeof(ValidateContextMenuAction0), delegateTarget, mi);
						} 
					}
					if (validateMethodSpecified && null == eai.validateAction) {
						Debug.LogWarningFormat ("Specified EditorAction validate method '{0}' not found.", validateMethodName);
					}
				}
			}

			List<EditorActionInfo> actions = new List<EditorActionInfo> ();

			// Order by 1. group, 2. priority
			actions.Sort ((a, b) => {
				int dg = a.group - b.group;
				return (dg != 0) ? dg : a.priority - b.priority;});

			actions.AddRange (actionInfos.Values);
			return actions;
		
		}

		[EditorAction("//")]
		protected static void EmptyAction ()
		{
		
		}

		private static bool DefaultValidateEditorAction ()
		{
			return true;
		}


		private class ContextMenuData
		{
			public IDieEditorToolContext context;
			public Delegate[] actions;
		}

		private bool CallValidateAction (Delegate d, IDieEditorToolContext context)
		{
			if (null == d) {
				return true;
			} else if (d.Method.GetParameters ().Length == 0) {
				return (bool)d.DynamicInvoke ();
			} else {
				return (bool)d.DynamicInvoke (context);
			}
		}

		protected bool ShowContextMenu (IDieEditorToolContext context)
		{
			Rect rect = new Rect ();
			rect.position = context.GetCursorCanvasPos ();

			ContextMenuData data = new ContextMenuData ();
			data.context = context;

			List<EditorActionInfo> shownActions = new List<EditorActionInfo> ();
			List<Delegate> actionDelegates = new List<Delegate> ();

			List<EditorActionInfo> actions = CollectActions (this);
			foreach (EditorActionInfo eai in actions) {
				if (CallValidateAction (eai.validateAction, context)) {
					shownActions.Add (eai);
					actionDelegates.Add (eai.action);
				}
			}
	
	
			List<GUIContent> menuItems = new List<GUIContent> ();
			int prevGroup = -1;
			for (int i = 0; i < shownActions.Count; i++) {
				int group = shownActions [i].group;
				if (i > 0 && prevGroup != group) {
					// Add separator
					menuItems.Add (new GUIContent ("//"));
					actionDelegates.Insert (i, null);
				}
				menuItems.Add (new GUIContent (shownActions [i].name));
				prevGroup = group;
			}

			data.actions = actionDelegates.ToArray ();


			if (menuItems.Count > 0) {
				EditorUtility.DisplayCustomMenu (
				rect, 
				menuItems.ToArray (), 
				-1, 
				ContextMenuCallback, 
				data);
				return true;
			} else {
				return false;
			}

		}
		private void ContextMenuCallback (object userData, string[] options, int sel)
		{
			ContextMenuData data = (ContextMenuData)userData;
			Delegate[] actions = data.actions;
		
			if (sel >= 0 && sel < actions.Length) {
				Delegate d = actions [sel];
				if (d.Method.GetParameters ().Length == 0) {
					// Contextless action
					d.DynamicInvoke ();
				} else {
					d.DynamicInvoke (data.context);
				}
			}
		}
	

	}
}

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
