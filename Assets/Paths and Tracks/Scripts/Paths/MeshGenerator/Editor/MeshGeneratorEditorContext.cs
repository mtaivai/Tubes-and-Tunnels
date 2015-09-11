// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using Util;
using Util.Editor;
using Paths;

namespace Paths.MeshGenerator.Editor
{

	/// <summary>
	/// Marks the target type as an ITrackGeneratorEditor implementation for
	/// the specified target type.
	/// </summary>

	public class MeshGeneratorEditorContext : PluginEditorContext
	{

	
		public MeshGeneratorEditorContext (IMeshGenerator customTool, UnityEngine.Object target, UnityEditor.Editor e, 
		                                   TargetModifiedFunc targetModifiedFunc, ContextEditorPrefs editorPrefs)
			: base(customTool, target, e, targetModifiedFunc, editorPrefs)
		{
		}


//		private IMeshGenerator meshGenerator;
//		private PathMeshGenerator track; // TODO rename track to pathMeshGenerator
//		private PathMeshGeneratorEditor trackEditor; // TODO rename this to editorHost ?
//
//		public IMeshGenerator TrackGenerator {
//			get {
//				return meshGenerator;
//			}
//		}
//
//		public PathMeshGenerator Track {
//			get {
//				return track;
//			}
//		}
//
//		public PathMeshGeneratorEditor TrackEditor {
//			get {
//				return trackEditor;
//			}
//		}
//
//		public MeshGeneratorEditorContext (IMeshGenerator tg, PathMeshGenerator t, PathMeshGeneratorEditor e)
//		{
//			this.meshGenerator = tg;
//			this.track = t;
//			this.trackEditor = e;
//		}
    
	}

}
