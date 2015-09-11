using UnityEngine;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using Util;

namespace Util.Editor
{

	// TODO implement editing of all supported types!
	[PluginEditor(typeof(object))]
	public class FallbackCustomToolEditor : IPluginEditor
	{
        
		public void DeleteEditorPrefs ()
		{

		}

		public void DrawInspectorGUI (PluginEditorContext context)
		{
			DoDrawInspectorGUI (context);
		}

		public static void DoDrawInspectorGUI (PluginEditorContext context, params string[] excludedFields)
		{
			//          PathModifierEditorContext pmeContext = (PathModifierEditorContext)context;



			// Collect all read/write fields:
			List<MemberInfo> inspectedMembers = new List<MemberInfo> ();
            
			foreach (MemberInfo mi in context.PluginInstance.GetType().GetMembers(BindingFlags.Instance|BindingFlags.Public)) {
				bool excludeThis = false;
				foreach (string excluded in excludedFields) {
					if (excluded == mi.Name) {
						excludeThis = true;
						break;
					}
				}
				if (excludeThis) {
					continue;
				}
				switch (mi.MemberType) {
				case MemberTypes.Field:
					inspectedMembers.Add (mi);
					break;
				case System.Reflection.MemberTypes.Property:
					PropertyInfo pi = (PropertyInfo)mi;
					if (pi.CanWrite && pi.CanRead) {
						inspectedMembers.Add (pi);
					}
					break;
				}
			}
            
			foreach (MemberInfo mi in inspectedMembers) {
				Field (context, mi);
			}
		}
        
		static void SetFieldValue (object obj, MemberInfo mi, object value)
		{
			if (mi is PropertyInfo) {
				((PropertyInfo)mi).SetValue (obj, value, new object[0]);
			} else if (mi is FieldInfo) {
				((FieldInfo)mi).SetValue (obj, value);
			} else {
				throw new NotSupportedException ("Can't set value of member " + mi.Name + " because it's " + mi.MemberType);
			}
		}
        
		static object GetFieldValue (object obj, MemberInfo mi)
		{
			if (mi is PropertyInfo) {
				return ((PropertyInfo)mi).GetValue (obj, new object[0]);
			} else if (mi is FieldInfo) {
				return ((FieldInfo)mi).GetValue (obj);
			} else {
				throw new NotSupportedException ("Can't get value of member " + mi.Name + " because it's " + mi.MemberType);
			}
		}

		static Type GetFieldType (object obj, MemberInfo mi)
		{
			if (mi is PropertyInfo) {
				return ((PropertyInfo)mi).PropertyType;
			} else if (mi is FieldInfo) {
				return ((FieldInfo)mi).FieldType;
			} else {
				throw new NotSupportedException ("Can't get value of member " + mi.Name + " because it's " + mi.MemberType);
			}
		}
        
		static void Field (PluginEditorContext context, MemberInfo mi)
		{
			object obj = context.PluginInstance;
            
			EditorGUI.BeginChangeCheck ();
            
			Type type = GetFieldType (obj, mi);
			// TODO is supported type?
			object currentValue = GetFieldValue (obj, mi);
			object newValue = currentValue;
            
			EditorGUI.BeginChangeCheck ();
			string labelText = FieldLabel (mi.Name);
			if (typeof(Vector3).IsAssignableFrom (type)) {
				// Vector3
				newValue = EditorGUILayout.Vector3Field (mi.Name, (Vector3)currentValue);
			} else if (typeof(bool).IsAssignableFrom (type)) {
				// bool
				newValue = EditorGUILayout.Toggle (labelText, (bool)currentValue);
			} else if (typeof(int).IsAssignableFrom (type)) {
				// int
				newValue = EditorGUILayout.IntField (labelText, (int)currentValue);
			} else if (typeof(long).IsAssignableFrom (type)) {
				// long
				newValue = EditorGUILayout.LongField (labelText, (long)currentValue);
			} else if (typeof(float).IsAssignableFrom (type)) {
				// float
				newValue = EditorGUILayout.FloatField (labelText, (float)currentValue);
                
			} else if (typeof(string).IsAssignableFrom (type)) {
				// string
				newValue = EditorGUILayout.TextField (labelText, (string)currentValue);
			} else if (typeof(Enum).IsAssignableFrom (type)) {
				// Enum
				newValue = EditorGUILayout.EnumPopup (labelText, (Enum)currentValue);
			} else {
				// Unsupported type; nop!
				Debug.LogWarning ("Unsupported property type: " + type + " in field " + mi.Name);
			}
            
			if (EditorGUI.EndChangeCheck ()) {
				SetFieldValue (obj, mi, newValue);
				context.TargetModified ();
			}
		}

		static string FieldLabel (string name)
		{
			string label = System.Text.RegularExpressions.Regex.Replace (name, "([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.Compiled).Trim ();
			return label.Length > 0 ? label.Substring (0, 1).ToUpper () + label.Substring (1) : "";
		}
	}
    
}
