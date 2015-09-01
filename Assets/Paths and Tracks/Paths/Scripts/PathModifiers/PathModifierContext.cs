// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Paths
{

	public class PathModifierContext
	{
		private IPathModifierContainer pathModifierContainer;
		private int inputFlags;
		private IPathInfo pathInfo;

		private Dictionary<string, object> parameters = new Dictionary<string, object> ();
//		private ParameterStore parameters;

		public PathModifierContext (IPathInfo pathInfo, IPathModifierContainer pathModifierContainer, int inputFlags, Dictionary<string, object> parameters)
		{
			this.pathInfo = pathInfo;
			this.pathModifierContainer = pathModifierContainer;
			this.inputFlags = inputFlags;
			this.parameters = parameters;
		}
		public PathModifierContext (IPathInfo pathInfo, IPathModifierContainer pathModifierContainer, int inputFlags)
		: this(pathInfo, pathModifierContainer, inputFlags, new Dictionary<string, object>())
		{
		}

		public IPathInfo PathInfo {
			get {
				return pathInfo;
			}
		}

		public IPathModifierContainer PathModifierContainer {
			get {
				return pathModifierContainer;
			}
		}

		public int InputFlags {
			get {
				return inputFlags;
			}
		}

		public Dictionary<string, object> Parameters {
			get {
				return parameters;
			}
		}
	}

}
