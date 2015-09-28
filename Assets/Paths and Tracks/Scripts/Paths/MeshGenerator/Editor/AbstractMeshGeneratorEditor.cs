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


	public abstract class AbstractMeshGeneratorEditor<T> : IMeshGeneratorEditor where T: IMeshGenerator
	{
		protected MeshGeneratorEditorContext editorContext;
		protected T target;
		protected PathMeshGenerator pathMeshGenerator;
		protected PathMeshGeneratorEditor editorHost;
		protected ParameterStore editorState;
    
		private void SetContext (MeshGeneratorEditorContext context)
		{
			this.editorContext = context;
			this.target = (T)context.PluginInstance;
			this.pathMeshGenerator = (PathMeshGenerator)context.Target;
			this.editorHost = (PathMeshGeneratorEditor)context.EditorHost;
			this.editorState = context.EditorParameters;
//			this.track = context.Track;
//			this.trackInspector = (PathMeshGeneratorEditor)context.TrackEditor;
		}
    
//		public void OnEnable (MeshGeneratorEditorContext context)
//		{
//			SetContext (context);
//			this.OnEnable ();
//		}

//		public abstract void OnEnable ();

		public void DeleteEditorPrefs ()
		{

		}

		public void DrawInspectorGUI (PluginEditorContext context)
		{
			SetContext ((MeshGeneratorEditorContext)context);
			this.DrawInspectorGUI ();
		}
    
		public abstract void DrawInspectorGUI ();
    
		public void DrawSceneGUI (MeshGeneratorEditorContext context)
		{
			SetContext (context);
			this.DrawSceneGUI ();
		}
    
		public abstract void DrawSceneGUI ();
    
	}


}
