using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using Util;
using Paths;

namespace Paths.MeshGenerator
{

	public enum MeshFaceDir
	{
		Up, // In
		Down, // Out
		Both, // Both
	}


	// TODO this should be AbstractTubeGenerator?
	public abstract class AbstractMeshGenerator : IMeshGenerator
	{

		private string previousMeshAssetPath;

		// TODO what's this?
		private Dictionary<string, string> editorPrefs = new Dictionary<string, string> ();


		protected AbstractMeshGenerator ()
		{
		}

		/// <summary>
		/// Finds track generator types, i.e. all non-abstract types implementing the ITrackGenerator
		/// interface.
		/// </summary>
		/// <returns>An array of track generator types in no particular order.</returns>
		public static Type[] FindMeshGeneratorTypes ()
		{
			return Util.TypeUtil.FindImplementingTypes (typeof(IMeshGenerator));
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

		private bool _inLoadParameters;
		public void LoadParameters (ParameterStore store)
		{
			if (!_inLoadParameters) {
				_inLoadParameters = true;
				try {
					previousMeshAssetPath = store.GetString ("previousMeshAssetPath", previousMeshAssetPath);

					ParameterStore eps = store.ChildWithPrefix ("editor");
					string[] editorParams = eps.FindParametersStartingWith ("");
					this.editorPrefs.Clear ();
					foreach (string n in editorParams) {
						this.editorPrefs [n] = eps.GetString (n, null); 
					}
					OnLoadParameters (store);
				} finally {
					_inLoadParameters = false;
				}
			} else {
				Debug.LogWarning ("AbstractMeshGenerator.LoadParameters() called from OnLoadParameters() - ignoring");
			}
		}
		public virtual void OnLoadParameters (ParameterStore store)
		{

		}

		private bool _inSaveParameters;
		public void SaveParameters (ParameterStore store)
		{
			if (!_inSaveParameters) {
				_inSaveParameters = true;
				try {
					store.SetString ("previousMeshAssetPath", previousMeshAssetPath);

					ParameterStore eps = store.ChildWithPrefix ("editor");
					foreach (KeyValuePair<string, string> kvp in editorPrefs) {
						eps.SetString (kvp.Key, kvp.Value);
					}
					OnSaveParameters (store);
				} finally {
					_inSaveParameters = false;
				}
			} else {
				Debug.LogWarning ("AbstractMeshGenerator.SaveParameters() called from OnSaveParameters() - ignoring");
			}
		}
		public virtual void OnSaveParameters (ParameterStore store)
		{
			
		}

//		public void SetEditorPref (string key, string value)
//		{
//			this.editorPrefs [key] = value;
//		}
//
//		public string GetEditorPref (string key, string defaultValue)
//		{
//			if (editorPrefs.ContainsKey (key)) {
//				return editorPrefs [key];
//			} else {
//				return defaultValue;
//			}
//		}

		public string GetSavedMeshAssetPath ()
		{
			return previousMeshAssetPath;
		}

		public void SetSavedMeshAssetPath (string path)
		{
			this.previousMeshAssetPath = path;
		}

		public Mesh[] CreateMeshes (PathDataSource dataSource)
		{
			int meshCount = GetMeshCount ();
			Mesh[] meshes = new Mesh[meshCount];
			for (int i = 0; i < meshCount; i++) {
				meshes [i] = new Mesh ();
			}
			CreateMeshes (dataSource, meshes);
			return meshes;
		}

		public virtual int GetMeshCount ()
		{
			return 1;
		}
		public abstract int GetMaterialSlotCount ();
		public virtual string GetMaterialSlotName (int i)
		{
			return "Material " + i;
		}

		public virtual int GetMaterialSlotSubmeshIndex (int i)
		{
			return i;
		}


		public abstract void CreateMeshes (PathDataSource dataSource, Mesh[] existingMeshes);


	}

	// TODO this should be AbstractTubeGenerator?
}
