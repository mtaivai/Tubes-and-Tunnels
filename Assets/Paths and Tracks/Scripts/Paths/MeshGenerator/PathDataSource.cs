// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Paths.MeshGenerator
{
	public delegate void PathDataChangedHandler (PathDataChangedEventArgs e);

	public enum PathDataStage
	{
		Processed,
		Unprocessed
	}

	public class PathDataChangedEventArgs : EventArgs
	{
		private object source;
		private PathDataSource dataSource;
		private PathDataStage stage;
		
		public PathDataChangedEventArgs (object source, PathDataSource dataSource, PathDataStage stage)
		{
			this.source = source;
			this.dataSource = dataSource;
			this.stage = stage;
		}
		public object Source {
			get {
				return source;
			}
		}
		public PathDataSource DataSource {
			get {
				return dataSource;
			}
		}
		public PathDataStage Stage {
			get {
				return stage;
			}
		}
		public override string ToString ()
		{
			return string.Format ("[PathDataChangedEventArgs: source={0}, dataSource={1}, stage={2}, Source={3}, DataSource={4}, Stage={5}]", source, dataSource, stage, Source, DataSource, Stage);
		}
		
	}

	public interface IPathDataSourceContainer
	{
		IReferenceContainer GetReferenceContainer ();
		ParameterStore GetParameterStore (PathDataSource ds);
	}

	[Serializable]
	public class PathDataSource : ISerializationCallbackReceiver
	{

		public event PathDataChangedHandler DataChanged;

		[SerializeField]
		private int
			id;

		[SerializeField]
		private string
			name;

		[SerializeField]
		private PathSelector
			pathSelector = new PathSelector ();

		// Don't serialize: (see notes on setter of PathData property)
		[NonSerialized]
		private PathSelector
			_previousKnownPathSelector = null;

		[SerializeField]
		PathDataCache
			processedDataCache = new PathDataCache ();

		[SerializeField]
		PathDataCache
			unprocessedDataCache = new PathDataCache ();


		// Don't serialize:
//		private Func<IPathModifierContainer> getPathModifierContainerFunc;
		[NonSerialized]
		private DefaultPathModifierContainer
			pathModifierContainer;

		[NonSerialized]
		private IReferenceContainer
			referenceContainer;

		[NonSerialized]
		private ParameterStore
			parameterStore;

		public PathDataSource (int id)
		{
			this.id = id;
		}

		void OnAttach (IPathDataSourceContainer container)
		{
			this.referenceContainer = container.GetReferenceContainer ();
			this.parameterStore = container.GetParameterStore (this);

			// HACK: first unregister listeners to make sure that we have only one listener!
			this.DoUnregisterPathChangedListeners ();
			this.DoRegisterPathChangedListeners ();

			if (null == pathModifierContainer) {
				pathModifierContainer = CreatePathModifierContainer ();
				pathModifierContainer.LoadConfiguration ();
			}
		}
		// TODO rename to Detach?
		void OnDetach (IPathDataSourceContainer container)
		{
			this.referenceContainer = null;
			this.parameterStore = null;
			this.pathModifierContainer = null;
			this.DoUnregisterPathChangedListeners ();
		}
		public void OnBeforeSerialize ()
		{
		}
		
		public void OnAfterDeserialize ()
		{
			if (null == processedDataCache) {
				processedDataCache = new PathDataCache ();
			}
			if (null == unprocessedDataCache) {
				unprocessedDataCache = new PathDataCache ();
			}
			if (null != pathModifierContainer) {
				this.pathModifierContainer.LoadConfiguration ();
			}

			this.DoUnregisterPathChangedListeners ();
			this.DoRegisterPathChangedListeners ();
			
		}

//		public override bool Equals (object obj)
//		{
//			if (obj == null)
//				return false;
//			if (ReferenceEquals (this, obj))
//				return true;
//			if (obj.GetType () != typeof(TrackDataSource))
//				return false;
//			TrackDataSource other = (TrackDataSource)obj;
//			return pathData == other.pathData;
//		}
//
//		public override int GetHashCode ()
//		{
//			unchecked {
//				return (pathData != null ? pathData.GetHashCode () : 0);
//			}
//		}
//		public static bool operator == (TrackDataSource a, TrackDataSource b)
//		{
//			if (ReferenceEquals (null, a)) {
//				return false;
//			} else {
//				return a.Equals (b);
//			}
//		}
//		public static bool operator != (TrackDataSource a, TrackDataSource b)
//		{
//			return ! (a == b);
//		}
		public override string ToString ()
		{
			return string.Format ("[PathDataSource: id={0}, name={1}, pathSelector={2}]", id, name, pathSelector);
		}
		

		public int Id {
			get {
				return id;
			}
		}
		public string Name {
			get {
				return name;
			}
			set {
				this.name = value;
			}
		}

		public PathSelector PathSelector {
			get {
				UpdatePathSelectorState ();
				return pathSelector;
			}
			set {
//				PathSelector prevState = this.pathSelector;
//				bool succeeded = false;
				try {
					if (this.pathSelector != value) {
						this.pathSelector = new PathSelector (value);
						UpdatePathSelectorState ();
//						succeeded = true;
					}
				} finally {
//					if (!succeeded) {
//						this.pathSelector = prevState;
//						// Still mark dirty
//						//DoInvalidateData (TrackDataStage.Unprocessed, false);
//					}
				}
			}
		}

		public void UpdatePathSelectorState ()
		{
			if (_previousKnownPathSelector != this.pathSelector) {
				if (null != _previousKnownPathSelector) {
					DoUnregisterPathChangedListeners (_previousKnownPathSelector);
				}

				// Store the known PathWithDataId so that we know if it has
				// been manipulated since last set here. Although PathWithDataId objects
				// are immutable, some editors may still directly manipulate their
				// fields and we need to have a mechanism to detect such manipulations.
				this._previousKnownPathSelector = new PathSelector (this.pathSelector);
				
				DoRegisterPathChangedListeners ();
				
				DoInvalidateData (PathDataStage.Unprocessed);
				DoInvalidateData (PathDataStage.Processed);
			}
		}

//		private PathPoint[] FetchPoints(out int flags) {
//			return pathData.GetPathPoints(out flags);
//
//		}
		public PathPoint[] UnprocessedPoints {
			get {
				UpdatePathSelectorState ();
				return unprocessedDataCache.GetPointsAndValidate (true, pathSelector.GetPathPoints);
			}
		}
		public int UnprocessedFlags {
			get {
				UpdatePathSelectorState ();
				return unprocessedDataCache.GetFlagsAndValidate (pathSelector.GetPathPoints);
			}
		}
		public PathPoint[] GetUnprocessedPoints (out int flags)
		{
			UpdatePathSelectorState ();
			PathPoint[] points = unprocessedDataCache.GetPointsAndValidate (pathSelector.GetPathPoints);
			flags = unprocessedDataCache.Flags;
			return (unprocessedDataCache.Valid) ? points : null;
		}

		private PathPoint[] ProcessPoints (out int flags)
		{
			PathPoint[] processedPoints;
			int unprocessedFlags;
			PathPoint[] unprocessedPoints = GetUnprocessedPoints (out unprocessedFlags);
			if (null == unprocessedPoints) {
				// TODO should we log an error?
				processedPoints = null;
				flags = 0;
			} else {
				IPathModifierContainer pmc = this.pathModifierContainer;
				if (null == pmc) {
					// Can't process, no IPathModifierContainer available
					Debug.LogWarning ("No IPathModifierContainer available; not processing Path data");
					// Create copy
					processedPoints = new PathPoint[unprocessedPoints.Length];
					Array.Copy (unprocessedPoints, processedPoints, processedPoints.Length);
					flags = unprocessedFlags;

				} else {
					IPathData data = pathSelector.PathData;
					if (null == data) {
						// TODO should we log an error?
						processedPoints = null;
						flags = 0;
					} else {
						IPathInfo pathInfo = data.GetPathInfo ();
						PathModifierContext context = new PathModifierContext (pathInfo, pmc, unprocessedFlags);

						flags = unprocessedFlags;
						// We need to create a deep clone of unprocessedPoints because PathPoints are mutable:
						PathPoint[] copyOfUnprocessedPoints = new PathPoint[unprocessedPoints.Length];
						for (int i = 0; i < copyOfUnprocessedPoints.Length; i++) {
							copyOfUnprocessedPoints [i] = new PathPoint (unprocessedPoints [i]);
						}
						processedPoints = pmc.xxxRunPathModifiers (context, copyOfUnprocessedPoints, ref flags);
//						processedPoints = PathModifierUtil.RunPathModifiers (context, unprocessedPoints, true, ref flags, true);
					}
				}
			}
			return processedPoints;

		}

		public PathPoint[] GetProcessedPoints (out int flags)
		{
			UpdatePathSelectorState ();
			PathPoint[] points = processedDataCache.GetPointsAndValidate (ProcessPoints);
			flags = processedDataCache.Flags;
			return (processedDataCache.Valid) ? points : null;
		}

		public PathPoint[] ProcessedPoints {
			get {
				int flags = 0;
				return GetProcessedPoints (out flags);
			}
		}
		public int ProcessedFlags {
			get {
				int flags = 0;
				GetProcessedPoints (out flags);
				return flags;
			}
		}
//

		public void InvalidateProcessedData (bool fireEvents = true)
		{
			UpdatePathSelectorState ();
			DoInvalidateData (PathDataStage.Processed);
		}

		public void InvalidateUnprocessedData (bool fireEvents = true)
		{
			UpdatePathSelectorState ();
			DoInvalidateData (PathDataStage.Unprocessed);
		}
		
		private void DoInvalidateData (PathDataStage stage, bool fireEvents = true)
		{
			bool wasValid;
			switch (stage) {
			case PathDataStage.Processed:
				wasValid = processedDataCache.Valid;
				processedDataCache.Invalidate ();
				break;
			case PathDataStage.Unprocessed:
				wasValid = unprocessedDataCache.Valid;
				unprocessedDataCache.Invalidate ();
				break;
			default:
				wasValid = false;
				break;
			}
			if (fireEvents && wasValid) {
				FireEvent (new PathDataChangedEventArgs (this, this, stage));
			}
		}

		private void FireEvent (PathDataChangedEventArgs ev)
		{
			if (null != DataChanged) {
				DataChanged (ev);
			}
		}


		public void InvalidateAll (bool fireEvents = true)
		{
			InvalidateUnprocessedData (fireEvents);
			InvalidateProcessedData (fireEvents);
		}
		private void DoRegisterPathChangedListeners ()
		{
			//          Debug.Log ("Registering PathChangedEventHandler on '" + path + "': " + this.gameObject.name);
			PathChangedEventHandler d = PathChanged;
			Path path = pathSelector.Path;
			if (null != path) {
				path.Changed -= d;
				path.Changed += d;
			}
		}
		
		private void DoUnregisterPathChangedListeners ()
		{
			//          Debug.Log ("Unregistering PathChangedEventHandler on '" + path + "': " + this.gameObject.name);
			DoUnregisterPathChangedListeners (pathSelector);
		}
		private void DoUnregisterPathChangedListeners (PathSelector pathSelector)
		{
			//          Debug.Log ("Unregistering PathChangedEventHandler on '" + path + "': " + this.gameObject.name);
			if (null != pathSelector) {
				Path path = pathSelector.Path;
				if (null != path) {
					path.Changed -= PathChanged;
				}
			}
		}

		// Receives PathChangedEvent from one of our configured Path instances
		private void PathChanged (object sender, PathChangedEvent e)
		{
//			Debug.LogFormat ("{0} received PathChangedEvent from {1}: {2}", this, sender, e);
			// Ignore snapshot specifications in comparision:
			if (pathSelector.WithoutSnapshot () == e.ChangedData.WithoutSnapshot ()) {
				// Our data has changed
//				Debug.LogFormat ("{0}: my path data has changed: {1}", this, e.ChangedData);

				InvalidateUnprocessedData (true);
//				InvalidateProcessedData(true);

			}        
			
		}

		// Called from our DefaultPathModifierContainer when PathModifiers have changed (added/removed/edited)
		private void PathModifiersChanged (PathModifierContainerEvent e)
		{
			//			Debug.LogFormat ("PathModifiersChanged: {0}", e);
			InvalidateProcessedData ();
			
		}

		public IPathModifierContainer GetPathModifierContainer ()
		{
			if (null == pathModifierContainer) {
				pathModifierContainer = CreatePathModifierContainer ();
			}
			return pathModifierContainer;
		}


		public IPathInfo GetPathInfo ()
		{
			IPathData pathData = this.PathSelector.PathData;
			return (null != pathData) ? pathData.GetPathInfo () : EmptyPathInfo.Instance;
		}

		private DefaultPathModifierContainer CreatePathModifierContainer ()
		{
			DefaultPathModifierContainer pmc = new DefaultPathModifierContainer (
				GetPathInfo,
				GetUnprocessedPoints,
				null,
				() => referenceContainer, 
				() => null, 
				() => parameterStore);
			
			pmc.PathModifiersChanged += this.PathModifiersChanged;
			// TODO should "parameters" above be a child ParamaterStore with prefix "pathModifiers."?
			
			
			return pmc;
		}

	}


}
