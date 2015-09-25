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
		private IPathMetadata pathMetadata;

		private Dictionary<string, object> parameters = new Dictionary<string, object> ();

		private List<string> info = new List<string> ();
		private List<string> errors = new List<string> ();
		private List<string> warnings = new List<string> ();
//		private ParameterStore parameters;

		public PathModifierContext (IPathInfo pathInfo, IPathModifierContainer pathModifierContainer, IPathMetadata pathMetadata, int inputFlags, Dictionary<string, object> parameters)
		{
			this.pathInfo = pathInfo;
			this.pathMetadata = pathMetadata;
			this.pathModifierContainer = pathModifierContainer;
			this.inputFlags = inputFlags;
			this.parameters = parameters;
		}
		public PathModifierContext (IPathInfo pathInfo, IPathModifierContainer pathModifierContainer, IPathMetadata pathMetadata, int inputFlags)
		: this(pathInfo, pathModifierContainer, pathMetadata, inputFlags, new Dictionary<string, object>())
		{
		}

		public static PathModifierContext ForPathData (IPathData pathData, int inputFlags)
		{
			return new PathModifierContext (pathData.GetPathInfo (), pathData.GetPathModifierContainer (), pathData.GetPathMetadata (), inputFlags);
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
		public IPathMetadata PathMetadata {
			get {
				return pathMetadata;
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

		public List<string> Errors {
			get {
				return errors;
			}
		}
		public bool HasErrors {
			get {
				return errors.Count > 0;
			}
		}
		public List<string> Warnings {
			get {
				return warnings;
			}
		}
		public bool HasWarnings {
			get {
				return warnings.Count > 0;
			}
		}
		public List<string> Info {
			get {
				return info;
			}
		}
		public bool HasInfo {
			get {
				return info.Count > 0;
			}
		}
	}

}
