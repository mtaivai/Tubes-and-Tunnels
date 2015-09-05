// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Util.Editor
{

	public class EditorLayout
	{
		protected GUILayoutOption[] options = new GUILayoutOption[0];
		protected GUIStyle style = GUIStyle.none;
		protected bool hidden = false;

		private Action<EditorLayout> beforeDraw;
		private Action<EditorLayout> afterDraw;

		private EditorLayout (Action<EditorLayout> beforeDrawAction, Action<EditorLayout> afterDrawAction)
		{
			this.beforeDraw = beforeDrawAction;
			this.afterDraw = afterDrawAction;
		}
		private EditorLayout (EditorLayout src)
		{
			this.beforeDraw = src.beforeDraw;
			this.afterDraw = src.afterDraw;
			this.options = new GUILayoutOption[src.options.Length];
			Array.Copy (src.options, this.options, src.options.Length);
			this.style = src.style;
			this.hidden = src.hidden;
		}
		
		public static EditorLayout None {
			get {
				return new EditorLayout (
					(EditorLayout l) => {},
					(EditorLayout l) => {});
			}
		}
		public static EditorLayout Vertical {
			get {
				return new EditorLayout (
					(EditorLayout l) => (EditorGUILayout.BeginVertical (l.style, l.options)),
					(EditorLayout l) => (EditorGUILayout.EndVertical ()));
			}
		}
		public static EditorLayout Horizontal {
			get {
				return new EditorLayout (
					(EditorLayout l) => (EditorGUILayout.BeginHorizontal (l.style, l.options)),
					(EditorLayout l) => (EditorGUILayout.EndHorizontal ()));
			}
		}
		public static void Indent (Action a)
		{
			Indent (1, a);
		}
		public static void Indent (int depth, Action a)
		{
			None.WithIndent (depth).Draw (a);
		}



		public EditorLayout WithOptions (params GUILayoutOption[] options)
		{
			EditorLayout l = new EditorLayout (this);
			l.options = new GUILayoutOption[options.Length];
			Array.Copy (options, l.options, options.Length);
			return l;
		}
		public EditorLayout WithStyle (GUIStyle style)
		{
			EditorLayout l = new EditorLayout (this);
			l.style = style;
			return l;
		}

		public EditorLayout WithIndent (int depth = 1)
		{
			EditorLayout l = new EditorLayout (this);

			Action<EditorLayout> oldBeforeDraw = l.beforeDraw;
			Action<EditorLayout> oldAfterDraw = l.afterDraw;

			l.beforeDraw = (EditorLayout l2) => {
				EditorGUI.indentLevel += depth;
				oldBeforeDraw (l2);
			};
			l.afterDraw = (EditorLayout l2) => {
				oldAfterDraw (l2);
				EditorGUI.indentLevel -= depth;
			};
			return l;
		}
		public EditorLayout Hidden (bool hidden = true)
		{
			EditorLayout l = new EditorLayout (this);
			l.hidden = hidden;
			return l;
		}

		public void Draw (Action a)
		{
			if (!hidden) {
				beforeDraw (this);
				a ();
				afterDraw (this);
			}
		}
	}
	
}
