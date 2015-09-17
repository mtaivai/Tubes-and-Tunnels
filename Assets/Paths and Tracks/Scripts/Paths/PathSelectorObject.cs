// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

using Util;

// TODO we need a GetPoint(float t) method, either directly in a path
// or as a separate utility. For pure bezier paths it would be easy to implement,
// but for polyline or composite paths it woudl need some more effort by
// implementing a smart smoothing / interpolating algorithm. One possiblity would
// be to convert polyline path to bezier path on-the-fly.

namespace Paths
{

	// TODO name of this class ... seriously?
	//public class UnambiguousPath : ScriptableObject
	public class PathSelectorObject : ScriptableObject
	{
		public PathSelector pathSelector;

//		public IPathData Data {
//			get {
//				return pathSelector.PathData;
//			}
//		}
	}

	// TODO Should this be renamed to AbstractPath?

}
