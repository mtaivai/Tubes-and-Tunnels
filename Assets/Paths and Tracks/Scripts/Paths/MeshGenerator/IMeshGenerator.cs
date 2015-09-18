// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using Util;
using Paths;

namespace Paths.MeshGenerator
{

	public interface IMeshGenerator
	{
		void LoadParameters (ParameterStore store);

		void SaveParameters (ParameterStore store);

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
