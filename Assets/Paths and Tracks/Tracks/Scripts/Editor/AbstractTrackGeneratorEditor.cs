using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using Util;
using Paths;

namespace Tracks.Editor
{
	public abstract class AbstractTrackGeneratorEditor<T> : ITrackGeneratorEditor
	{
		protected TrackGeneratorEditorContext editorContext;
		protected T target;
		protected Track track;
		protected TrackEditor trackInspector;
    
		private void SetContext (TrackGeneratorEditorContext context)
		{
			this.editorContext = context;
			this.target = (T)context.TrackGenerator;
			this.track = context.Track;
			this.trackInspector = (TrackEditor)context.TrackEditor;
		}
    
		public void OnEnable (TrackGeneratorEditorContext context)
		{
			SetContext (context);
			this.OnEnable ();
		}

		public abstract void OnEnable ();

		public void DrawInspectorGUI (TrackGeneratorEditorContext context)
		{
			SetContext (context);
			this.DrawInspectorGUI ();
		}
    
		public abstract void DrawInspectorGUI ();
    
		public void DrawSceneGUI (TrackGeneratorEditorContext context)
		{
			SetContext (context);
			this.DrawSceneGUI ();
		}
    
		public abstract void DrawSceneGUI ();
    
	}


}
