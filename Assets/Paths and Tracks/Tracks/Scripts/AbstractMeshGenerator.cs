using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using Util;
using Paths;

namespace Tracks
{
	public interface IMeshGenerator
	{
		void LoadParameters (ParameterStore store);

		void SaveParameters (ParameterStore store);

		void SetEditorPref (string key, string value);

		string GetEditorPref (string key, string defaultValue);

		TrackSlice[] CreateSlices (TrackDataSource dataSource);

		TrackSlice[] CreateSlices (TrackDataSource dataSource, bool repeatFirstInLoop);

		Mesh CreateMesh (TrackDataSource dataSource);

		Mesh CreateMesh (TrackDataSource dataSource, Mesh mesh);

		string GetSavedMeshAssetPath ();

		void SetSavedMeshAssetPath (string path);


	}

	public abstract class AbstractMeshGenerator : IMeshGenerator
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
		public static Type[] FindMeshGeneratorTypes ()
		{
			return Util.TypeUtil.FindImplementingTypes (typeof(IMeshGenerator));
		}

		public AbstractMeshGenerator ()
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

			ParameterStore eps = store.ChildWithPrefix ("editor");
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


			ParameterStore eps = store.ChildWithPrefix ("editor");
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

		public TrackSlice[] CreateSlices (TrackDataSource dataSource)
		{
			return CreateSlices (dataSource, false);
		}
    
		protected abstract TrackSlice CreateSlice (Vector3 center, Quaternion sliceRotation);



		public TrackSlice[] CreateSlices (TrackDataSource dataSource, bool repeatFirstInLoop)
		{
			TrackSlice[] slices;
			long startTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
        
			// Create slices
			//int slicesPerSgement = path.PointsPerSegment;

			if (null == dataSource) {
				Debug.LogError ("No Data Source configured; not creating any slices");
				slices = new TrackSlice[0];
			} else {
				slices = DoCreateSlices (dataSource, repeatFirstInLoop);
			}

			long endTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
			
			long deltaTime = endTime - startTime;
			Debug.Log ("Creating " + slices.Length + " slices took " + deltaTime + " ms");

			return slices;
		}
		private TrackSlice[] DoCreateSlices (TrackDataSource dataSource, bool repeatFirstInLoop)
		{
			int ppFlags;
			PathPoint[] points = dataSource.GetProcessedPoints (out ppFlags);
			if (null == points) {
				throw new Exception ("No data available in data source: " + dataSource.PathSelector);
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

			
			return slices;
		}

		public Mesh CreateMesh (TrackDataSource dataSource)
		{
			Mesh mesh = new Mesh ();
			return CreateMesh (dataSource, mesh);
		}

		public abstract Mesh CreateMesh (TrackDataSource dataSource, Mesh mesh);


	}
}
