// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using Util;
using Paths;

namespace Paths.MeshGenerator
{

	public class MeshGeneratorEventArgs : EventArgs
	{
		public enum Reason
		{
			MeshChanged,
		}
		private IMeshGenerator meshGenerator;
		private Reason reason;
		public MeshGeneratorEventArgs (IMeshGenerator mg, Reason r)
		{
			this.meshGenerator = mg;
			this.reason = r;
		}
		public Reason EventReason {
			get {
				return reason;
			}
		}
		public IMeshGenerator MeshGenerator {
			get {
				return meshGenerator;
			}
		}
		public override string ToString ()
		{
			return string.Format ("[MeshGeneratorEventArgs: meshGenerator={0}, reason={1}]", meshGenerator, reason);
		}
		

	}
	public delegate void MeshGeneratorEventHandler (MeshGeneratorEventArgs e);


	public interface IMeshGenerator
	{
		void OnCreate ();
		void OnEnable ();
		void OnDisable ();
		void OnDestroy ();

		void AddMeshGeneratorEventHandler (MeshGeneratorEventHandler handler);
		void RemoveMeshGeneratorEventHandler (MeshGeneratorEventHandler handler);

		void LoadParameters (ParameterStore store, IReferenceContainer refContainer);

		void SaveParameters (ParameterStore store, IReferenceContainer refContainer);

//		void SetEditorPref (string key, string value);
//
//		string GetEditorPref (string key, string defaultValue);

//		PathMeshSlice[] CreateSlices (PathDataSource dataSource);
//
//		PathMeshSlice[] CreateSlices (PathDataSource dataSource, bool repeatFirstInLoop);

		int GetMeshCount ();

		int GetMaterialSlotCount ();
		string GetMaterialSlotName (int i);
		int GetMaterialSlotSubmeshIndex (int i);

		Mesh[] CreateMeshes (PathDataSource dataSource);

		void CreateMeshes (PathDataSource dataSource, Mesh[] existingMeshes);

		string GetSavedMeshAssetPath ();

		void SetSavedMeshAssetPath (string path);

	}

	// TODO this should be AbstractTubeGenerator?

	// TODO this should be AbstractTubeGenerator?
}
