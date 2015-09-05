using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

using Util;
using Paths;

using Tracks;
using Tracks.Editor;


namespace Tracks.Tube.Editor
{
	[TrackGeneratorCustomEditor(typeof(TubeGenerator))]
	public class TubeGeneratorEditor : DefaultTrackGeneratorEditor
	{

		bool sliceConfigVisible = true;
		bool meshConfigVisible = true;

		public override void OnEnable ()
		{
			base.OnEnable ();
			this.sliceConfigVisible = bool.Parse (target.GetEditorPref ("sliceConfigVisible", sliceConfigVisible.ToString ()));
			this.meshConfigVisible = bool.Parse (target.GetEditorPref ("meshConfigVisible", meshConfigVisible.ToString ()));
		}
    
		public override void DrawInspectorGUI ()
		{

			base.DrawInspectorGUI ();

			TubeGenerator target = (TubeGenerator)this.target;

			//      Track track = trackInspector.target as Track;
			//      Path path = track.Path;
        
			EditorGUI.BeginChangeCheck ();
			sliceConfigVisible = EditorGUILayout.Foldout (sliceConfigVisible, "Slice Configuration");
			if (EditorGUI.EndChangeCheck ()) {
				target.SetEditorPref ("sliceConfigVisible", sliceConfigVisible.ToString ());
			}
			if (sliceConfigVisible) {
				EditorGUI.indentLevel++;

				EditorGUI.BeginChangeCheck ();
				target.SliceEdges = EditorGUILayout.IntSlider ("Slice Edges", target.SliceEdges, 3, 64);
				if (EditorGUI.EndChangeCheck ()) {
					//EditorUtility.SetDirty(trackInspector.target);
					trackInspector.TrackGeneratorModified ();
				}
            
				EditorGUI.BeginChangeCheck ();
				target.SliceRotation = EditorGUILayout.Slider ("Slice Rotation", target.SliceRotation, 0f, 360f);
				if (EditorGUI.EndChangeCheck ()) {
					//EditorUtility.SetDirty(trackInspector.target);
					trackInspector.TrackGeneratorModified ();
				}

            
				EditorGUI.BeginChangeCheck ();
				target.SliceSize = EditorGUILayout.Vector2Field ("Slice Size", target.SliceSize);
				if (EditorGUI.EndChangeCheck ()) {
					//EditorUtility.SetDirty(trackInspector.target);
					trackInspector.TrackGeneratorModified ();
				}



				EditorGUI.indentLevel--;
			}


			// On Mesh!
			EditorGUI.BeginChangeCheck ();
			meshConfigVisible = EditorGUILayout.Foldout (meshConfigVisible, "Mesh Configuration");
			if (EditorGUI.EndChangeCheck ()) {
				target.SetEditorPref ("meshConfigVisible", meshConfigVisible.ToString ());
			}
			if (meshConfigVisible) {
				EditorGUI.indentLevel++;

				EditorGUI.BeginChangeCheck ();
				target.FacesDir = (TubeGenerator.FaceDir)EditorGUILayout.EnumPopup ("Faces Direction", target.FacesDir);
				if (EditorGUI.EndChangeCheck ()) {
					trackInspector.TrackGeneratorModified ();
				}
            
				EditorGUI.BeginDisabledGroup (target.FacesDir != TubeGenerator.FaceDir.Both);
				EditorGUI.BeginChangeCheck ();
				target.PerSideVertices = EditorGUILayout.Toggle ("Vertex per side", target.PerSideVertices);
				if (EditorGUI.EndChangeCheck ()) {
					trackInspector.TrackGeneratorModified ();
				}
				EditorGUI.BeginChangeCheck ();
				target.PerSideSubmeshes = EditorGUILayout.Toggle ("Submesh per side", target.PerSideSubmeshes);
				if (EditorGUI.EndChangeCheck ()) {
					trackInspector.TrackGeneratorModified ();
				}
				EditorGUI.EndDisabledGroup ();

				EditorGUI.indentLevel--;
			}

		}
    
		public override void DrawSceneGUI ()
		{
			//throw new NotImplementedException ();
		}
	}
}
