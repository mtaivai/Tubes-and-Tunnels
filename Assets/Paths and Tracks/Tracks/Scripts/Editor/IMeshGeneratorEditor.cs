using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using Util;
using Paths;

namespace Tracks.Editor
{





	/// <summary>
	/// Marks the target type as an ITrackGeneratorEditor implementation for
	/// the specified target type.
	/// </summary>
	[AttributeUsage(AttributeTargets.All)]
	public sealed class MeshGeneratorCustomEditor : System.Attribute
	{
		private Type inspectedType;

		public Type InspectedType {
			get {
				return inspectedType;
			}
		}

		public MeshGeneratorCustomEditor (Type inspectedType)
		{
			this.inspectedType = inspectedType;
		}
	}

	public class MeshGeneratorEditorContext
	{
		private IMeshGenerator meshGenerator;
		private Track track;
		private TrackEditor trackEditor;

		public IMeshGenerator TrackGenerator {
			get {
				return meshGenerator;
			}
		}

		public Track Track {
			get {
				return track;
			}
		}

		public TrackEditor TrackEditor {
			get {
				return trackEditor;
			}
		}

		public MeshGeneratorEditorContext (IMeshGenerator tg, Track t, TrackEditor e)
		{
			this.meshGenerator = tg;
			this.track = t;
			this.trackEditor = e;
		}
    
	}

	public interface IMeshGeneratorEditor
	{
		void OnEnable (MeshGeneratorEditorContext context);

		void DrawInspectorGUI (MeshGeneratorEditorContext context);

		void DrawSceneGUI (MeshGeneratorEditorContext context);
	}

}
