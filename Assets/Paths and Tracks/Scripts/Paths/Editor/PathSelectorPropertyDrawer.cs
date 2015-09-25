// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using UnityEditor;

using System.Collections;
using System.Collections.Generic;
using System;

using Paths;
using Util.Editor;
using Util;

namespace Paths.Editor
{
	[CustomPropertyDrawer(typeof(DynParam))]
	public class DynParamPropertyDrawer : PropertyDrawer
	{

	}

	[CustomPropertyDrawer(typeof(PathSelector))]
	public class PathSelectorPropertyDrawer : PropertyDrawer
	{
		private string snapshotNameFromList = null;
		
		public override float GetPropertyHeight (SerializedProperty property, GUIContent label)
		{
			float singleLineHeight = base.GetPropertyHeight (property, label);
			return singleLineHeight * 3f;
		}
		
		public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
		{
			// Using BeginProperty / EndProperty on the parent property means that
			// prefab override logic works on the entire property.
			EditorGUI.BeginProperty (position, label, property);
			
			float singleHeight = base.GetPropertyHeight (property, label);
			
			SerializedProperty pathProperty = property.FindPropertyRelative ("path");
			SerializedProperty dataSetIdProperty = property.FindPropertyRelative ("dataSetId");
			SerializedProperty useSnapshotProperty = property.FindPropertyRelative ("useSnapshot");
			SerializedProperty snapshotNameProperty = property.FindPropertyRelative ("snapshotName");
			
			PathEditorUtil.DoDrawPathSelector (position, singleHeight, label, pathProperty, dataSetIdProperty, useSnapshotProperty, snapshotNameProperty, 
			                                   (snapshotName) => snapshotNameFromList = snapshotName);
			
			if (null != snapshotNameFromList) {
				snapshotNameProperty.stringValue = snapshotNameFromList;
				snapshotNameFromList = null;
			}
			
			EditorGUI.EndProperty ();
			
		}
	}
    
}
