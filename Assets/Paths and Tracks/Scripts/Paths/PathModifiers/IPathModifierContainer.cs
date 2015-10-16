// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Paths
{

	public interface IPathModifierContainer
	{

		IPathModifier[] GetPathModifiers ();

		void AddPathModifer (IPathModifier pm);

		void InsertPathModifier (int index, IPathModifier pm);

		void RemovePathModifier (int index);

		void RemoveAllPathModifiers ();

		int IndexOf (IPathModifier pm);

		bool IsSupportsApplyPathModifier ();

		void ApplyPathModifier (int index);

		IReferenceContainer GetReferenceContainer ();

		IPathSnapshotManager GetPathSnapshotManager ();

		// TODO is this required?
		ParameterStore GetParameterStore ();

		//void SaveConfiguration ();
		void ConfigurationChanged ();

		PathPoint[] RunPathModifiers (PathModifierContext context, PathPoint[] pp, ref int flags);

//		bool HasErrors ();
//		string[] GetCurrentErrors ();
//
		bool HasMessages (PathModifierMessageType messageType);
		bool HasMessages (PathModifierMessageType messageType, IPathModifier pm);
		string[] GetCurrentMessages (PathModifierMessageType messageType, IPathModifier pm);
	}

}
