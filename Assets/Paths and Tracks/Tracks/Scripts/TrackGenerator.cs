using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using Util;
using Paths;

namespace Tracks
{
	public interface ITrackGenerator
	{
		void LoadParameters (ParameterStore store);

		void SaveParameters (ParameterStore store);

		void SetEditorPref (string key, string value);

		string GetEditorPref (string key, string defaultValue);

		TrackSlice[] CreateSlices (Track track);

		TrackSlice[] CreateSlices (Track track, bool repeatFirstInLoop);

		Mesh CreateMesh (Track track);

		Mesh CreateMesh (Track track, Mesh mesh);

		string GetSavedMeshAssetPath ();

		void SetSavedMeshAssetPath (string path);


	}

	// TODO rename to AbstractTrackGenerator?
	public abstract class TrackGenerator : ITrackGenerator
	{

		private bool usePathResolution = true;

		// TODO is this used any more? Probably not, so remove this!
		private int customResolution = 10;
		private string previousMeshAssetPath;
		private Dictionary<string, string> editorPrefs = new Dictionary<string, string> ();

		/// <summary>
		/// Finds track generator types, i.e. all non-abstract types implementing the ITrackGenerator
		/// interface.
		/// </summary>
		/// <returns>An array of track generator types in no particular order.</returns>
		public static Type[] FindTrackGeneratorTypes ()
		{
			return Util.TypeUtil.FindImplementingTypes (typeof(ITrackGenerator));
		}

		public TrackGenerator ()
		{
		}

		public virtual string Name {
			get {
				return GetType ().Name;
			}
		}

		public virtual string DisplayName {
			get {
				return Name;
			}
		}

		public virtual void LoadParameters (ParameterStore store)
		{

			usePathResolution = store.GetBool ("usePathResolution", usePathResolution);
			customResolution = store.GetInt ("customResolution", customResolution);
			previousMeshAssetPath = store.GetString ("previousMeshAssetPath", previousMeshAssetPath);

			ParameterStore eps = new ParameterStore (store, "editor");
			string[] editorParams = eps.FindParametersStartingWith ("");
			this.editorPrefs.Clear ();
			foreach (string n in editorParams) {
				this.editorPrefs [n] = eps.GetString (n, null); 
			}
		}

		public virtual void SaveParameters (ParameterStore store)
		{
			store.SetBool ("usePathResolution", usePathResolution);
			store.SetInt ("customResolution", customResolution);
			store.SetString ("previousMeshAssetPath", previousMeshAssetPath);


			ParameterStore eps = new ParameterStore (store, "editor");
			foreach (KeyValuePair<string, string> kvp in editorPrefs) {
				eps.SetString (kvp.Key, kvp.Value);
			}
		}

		public void SetEditorPref (string key, string value)
		{
			this.editorPrefs [key] = value;
		}

		public string GetEditorPref (string key, string defaultValue)
		{
			if (editorPrefs.ContainsKey (key)) {
				return editorPrefs [key];
			} else {
				return defaultValue;
			}
		}

		public string GetSavedMeshAssetPath ()
		{
			return previousMeshAssetPath;
		}

		public void SetSavedMeshAssetPath (string path)
		{
			this.previousMeshAssetPath = path;
		}

		public TrackSlice[] CreateSlices (Track track)
		{
			return CreateSlices (track, false);
		}
    
		protected abstract TrackSlice CreateSlice (Vector3 center, Quaternion sliceRotation);



		public TrackSlice[] CreateSlices (Track track, bool repeatFirstInLoop)
		{
			long startTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
        
			// Create slices
			//int slicesPerSgement = path.PointsPerSegment;

			PathPoint[] points;

			// TODO usePAthResolution is not used any longer!
			int ppFlags;
			if (usePathResolution) {
				points = track.PrimaryDataSource.GetProcessedPoints (out ppFlags);
			} else {
				// TODO path should provide "lowres" and "highres" paths!
				Debug.LogError ("Custom resolution is no longer supported");
				return null;
//          points = path.GetPathGenerator().GeneratePoints(customResolution);
			}

			//Debug.Log ("dirs: " + directions.Length + "; pts: " + points.Length);
			int sliceCount = points.Length;
			bool isLoop = false; // TODO IMPLEMENT THIS FOR REAL?
			if (isLoop && !repeatFirstInLoop) {
				sliceCount -= 1;
			}
        
			TrackSlice[] slices = new TrackSlice[sliceCount];
        
			// TODO split long segments to shorter
			bool usingGeneratedDirections = false;
			//int lastIndex = points.Length - 1;
			for (int i = 0; i < slices.Length; i++) {
				Vector3 pt0 = points [i].Position;
				Vector3 dir;
				if (PathPoint.IsDirection (ppFlags) && points [i].HasDirection) {
					dir = points [i].Direction;
				} else {
					usingGeneratedDirections = true;
					if (i < slices.Length - 1) {
						// Calculate direction from this to next
						dir = (points [i + 1].Position - pt0).normalized;
					} else if (i > 0) {
						// Last point; calculate direction from previous point to this
						dir = (pt0 - points [i - 1].Position).normalized;
					} else {
						// Unknown direction, set to "forward"
						dir = Vector3.forward;
					}
				}

				Vector3 up;
				if (PathPoint.IsUp (ppFlags) && points [i].HasUp) {
					up = points [i].Up;
				} else {
					up = Vector3.up;
				}

            
				//Quaternion sliceRot = Quaternion.LookRotation(dir);

				Quaternion sliceRot = Quaternion.LookRotation (dir, up);
				//Quaternion sliceRot = Quaternion.FromToRotation(Vector3.forward, dir);
            
				slices [i] = CreateSlice (pt0, sliceRot);
			}
			if (usingGeneratedDirections) {
				Debug.LogWarning ("Using calculated directions while generating Track; expect some inaccurancy!");
			}
			if (isLoop && repeatFirstInLoop) {
				slices [sliceCount - 1] = slices [0];
			}
			long endTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
        
			long deltaTime = endTime - startTime;
			Debug.Log ("Creating " + slices.Length + " slices took " + deltaTime + " ms");
        
			return slices;

		}

		public Mesh CreateMesh (Track track)
		{
			Mesh mesh = new Mesh ();
			return CreateMesh (track, mesh);
		}

		public abstract Mesh CreateMesh (Track track, Mesh mesh);


	}
}
