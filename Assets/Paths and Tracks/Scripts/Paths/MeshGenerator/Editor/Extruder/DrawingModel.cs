// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Paths.MeshGenerator.Extruder.Editor
{
// TODO this is not used!
	public interface DrawingModel
	{

		bool HasCurrentSnapToTarget ();
		Vector3 GetCurrentSnapToTargetPosition ();

	}

	public class EmptyDrawingModel : DrawingModel
	{
		private static EmptyDrawingModel _instance = new EmptyDrawingModel ();

		public static EmptyDrawingModel Instance {
			get {
				return _instance;
			}
		}
		private EmptyDrawingModel ()
		{

		}

		public bool HasCurrentSnapToTarget ()
		{
			return false;
		}

		public Vector3 GetCurrentSnapToTargetPosition ()
		{
			throw new System.NotImplementedException ();
		}
	}
}
