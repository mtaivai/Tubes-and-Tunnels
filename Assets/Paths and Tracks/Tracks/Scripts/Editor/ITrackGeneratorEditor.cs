using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using Util;
using Paths;

namespace Tracks
{





    /// <summary>
    /// Marks the target type as an ITrackGeneratorEditor implementation for
    /// the specified target type.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class TrackGeneratorCustomEditor : System.Attribute
    {
        private Type inspectedType;

        public Type InspectedType
        {
            get
            {
                return inspectedType;
            }
        }

        public TrackGeneratorCustomEditor(Type inspectedType)
        {
            this.inspectedType = inspectedType;
        }
    }

    public class TrackGeneratorEditorContext
    {
        private ITrackGenerator trackGenerator;
        private Track track;
        private TrackEditor trackEditor;

        public ITrackGenerator TrackGenerator
        {
            get
            {
                return trackGenerator;
            }
        }

        public Track Track
        {
            get
            {
                return track;
            }
        }

        public TrackEditor TrackEditor
        {
            get
            {
                return trackEditor;
            }
        }

        public TrackGeneratorEditorContext(ITrackGenerator tg, Track t, TrackEditor e)
        {
            this.trackGenerator = tg;
            this.track = t;
            this.trackEditor = e;
        }
    
    }

    public interface ITrackGeneratorEditor
    {
        void OnEnable(TrackGeneratorEditorContext context);

        void DrawInspectorGUI(TrackGeneratorEditorContext context);

        void DrawSceneGUI(TrackGeneratorEditorContext context);
    }

}
