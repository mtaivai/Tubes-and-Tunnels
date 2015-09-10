using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using Util;
using Util.Editor;
using Paths;

namespace Paths.MeshGenerator.Editor
{


	/// <summary>
	/// Marks the target type as an ITrackGeneratorEditor implementation for
	/// the specified target type.
	/// </summary>
//	[AttributeUsage(AttributeTargets.All)]
//	public sealed class MeshGeneratorCustomEditor : System.Attribute
//	{
//		private Type inspectedType;
//
//		public Type InspectedType {
//			get {
//				return inspectedType;
//			}
//		}
//
//		public MeshGeneratorCustomEditor (Type inspectedType)
//		{
//			this.inspectedType = inspectedType;
//		}
//	}


	public interface IMeshGeneratorEditor : ICustomToolEditor
	{
//		void OnEnable (MeshGeneratorEditorContext context);

//		void DrawInspectorGUI (MeshGeneratorEditorContext context);
//
		void DrawSceneGUI (MeshGeneratorEditorContext context);
	}

}
