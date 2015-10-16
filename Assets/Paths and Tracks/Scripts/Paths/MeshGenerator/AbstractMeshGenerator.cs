using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

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

		private event MeshGeneratorEventHandler EventHandler;

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


		public virtual void OnCreate ()
		{
			Debug.Log ("OnCreate: " + this);
		}
		public virtual void OnEnable ()
		{
			Debug.Log ("OnEnable: " + this);
		}
		public virtual void OnDisable ()
		{
			Debug.Log ("OnDisable: " + this);
		}
		public virtual void OnDestroy ()
		{
			Debug.Log ("OnDestroy: " + this);
		}

		public void AddMeshGeneratorEventHandler (MeshGeneratorEventHandler handler)
		{
			EventHandler -= handler;
			EventHandler += handler;
		}
		public void RemoveMeshGeneratorEventHandler (MeshGeneratorEventHandler handler)
		{
			EventHandler -= handler;
		}
		protected void FireMeshGeneratorEvent (MeshGeneratorEventArgs e)
		{
			if (null != EventHandler) {
				try {
					EventHandler (e);
				} catch (Exception ex) {
					Debug.LogErrorFormat ("Catched an exception while firing MeshGeneratorEventArgs {0}: {1}", e, ex);
				}
			}
		}

		private bool _inLoadParameters;
		public void LoadParameters (ParameterStore store, IReferenceContainer refContainer)
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
					OnLoadParameters (store, refContainer);
				} finally {
					_inLoadParameters = false;
				}
			} else {
				Debug.LogWarning ("AbstractMeshGenerator.LoadParameters() called from OnLoadParameters() - ignoring");
			}
		}
		public virtual void OnLoadParameters (ParameterStore store, IReferenceContainer refContainer)
		{

		}

		private bool _inSaveParameters;
		public void SaveParameters (ParameterStore store, IReferenceContainer refContainer)
		{
			if (!_inSaveParameters) {
				_inSaveParameters = true;
				try {
					store.SetString ("previousMeshAssetPath", previousMeshAssetPath);

					ParameterStore eps = store.ChildWithPrefix ("editor");
					foreach (KeyValuePair<string, string> kvp in editorPrefs) {
						eps.SetString (kvp.Key, kvp.Value);
					}
					OnSaveParameters (store, refContainer);
				} finally {
					_inSaveParameters = false;
				}
			} else {
				Debug.LogWarning ("AbstractMeshGenerator.SaveParameters() called from OnSaveParameters() - ignoring");
			}
		}
		public virtual void OnSaveParameters (ParameterStore store, IReferenceContainer refContainer)
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
