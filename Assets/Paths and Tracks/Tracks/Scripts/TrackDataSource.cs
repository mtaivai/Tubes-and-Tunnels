// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Tracks
{
	public delegate void TrackDataChangedHandler (TrackDataChangedEventArgs e);

	public enum TrackDataStage
	{
		Processed,
		Unprocessed
	}
	public class TrackDataChangedEventArgs : EventArgs
	{
		private object source;
		private TrackDataSource dataSource;
		private TrackDataStage stage;
		
		public TrackDataChangedEventArgs (object source, TrackDataSource dataSource, TrackDataStage stage)
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
		public TrackDataSource DataSource {
			get {
				return dataSource;
			}
		}
		public TrackDataStage Stage {
			get {
				return stage;
			}
		}
		public override string ToString ()
		{
			return string.Format ("[TrackDataChangedEventArgs: source={0}, dataSource={1}, stage={2}, Source={3}, DataSource={4}, Stage={5}]", source, dataSource, stage, Source, DataSource, Stage);
		}
		
	}


	[Serializable]
	public class TrackDataSource : ISerializationCallbackReceiver
	{

		public event TrackDataChangedHandler DataChanged;

		[SerializeField]
		private PathWithDataId
			pathData = new PathWithDataId ();

		[SerializeField]
		TrackDataCache
			processedDataCache = new TrackDataCache ();

		[SerializeField]
		TrackDataCache
			unprocessedDataCache = new TrackDataCache ();

		// Don't serialize:
		private Func<IPathModifierContainer> getPathModifierContainerFunc;

//		private DataChangedDelegate processedDataChangedCallback;
//		private DataChangedDelegate unprocessedDataChanged;

//		public TrackDataSource ()
//		{
//
//		}
		public TrackDataSource (Track track)
		{
			this.getPathModifierContainerFunc = track.GetPathModifierContainer;
		}

		public void OnEnable (Track track)
		{
			this.DoUnregisterPathChangedListeners ();
			this.DoRegisterPathChangedListeners ();
		}
		public void OnDisable (Track track)
		{
			this.DoUnregisterPathChangedListeners ();
			this.getPathModifierContainerFunc = null;
		}
		public void OnBeforeSerialize ()
		{
		}
		
		public void OnAfterDeserialize ()
		{

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
			return string.Format ("[TrackDataSource: pathData={0}]", pathData);
		}

		public PathWithDataId PathData {
			get {
				return pathData;
			}
			set {
				PathWithDataId prevDataId = this.pathData;
				bool succeeded = false;
				try {
					if (this.pathData != value) {
						DoUnregisterPathChangedListeners ();

						this.pathData = value;

						DoRegisterPathChangedListeners ();

						InvalidateUnprocessedData ();
						succeeded = true;
					}
				} finally {
					if (!succeeded) {
						this.pathData = prevDataId;
					}
				}
			}
		}

//		private PathPoint[] FetchPoints(out int flags) {
//			return pathData.GetPathPoints(out flags);
//
//		}
		public PathPoint[] UnprocessedPoints {
			get {
				return unprocessedDataCache.GetPointsAndValidate (pathData.GetPathPoints);
			}
		}
		public int UnprocessedFlags {
			get {
				return unprocessedDataCache.GetFlagsAndValidate (pathData.GetPathPoints);
			}
		}
		public PathPoint[] GetUnprocessedPoints (out int flags)
		{
			PathPoint[] points = unprocessedDataCache.GetPointsAndValidate (pathData.GetPathPoints);
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
				IPathModifierContainer pmc = (null != getPathModifierContainerFunc) ? getPathModifierContainerFunc () : null;
				if (null == pmc) {
					// Can't process, no IPathModifierContainer available
					Debug.LogWarning ("No IPathModifierContainer available; not processing Path data");
					// Create copy
					processedPoints = new PathPoint[unprocessedPoints.Length];
					Array.Copy (unprocessedPoints, processedPoints, processedPoints.Length);
					flags = unprocessedFlags;

				} else {
					IPathData data = pathData.PathData;
					if (null == data) {
						// TODO should we log an error?
						processedPoints = null;
						flags = 0;
					} else {
						IPathInfo pathInfo = data.GetPathInfo ();
						PathModifierContext context = new PathModifierContext (pathInfo, pmc, unprocessedFlags);

						flags = unprocessedFlags;
						processedPoints = PathModifierUtil.RunPathModifiers (context, unprocessedPoints, true, ref flags, true);
					}
				}
			}
			return processedPoints;

		}

		public PathPoint[] GetProcessedPoints (out int flags)
		{
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

		public void InvalidateUnprocessedData (bool fireEvents = true)
		{
			bool wasValid = unprocessedDataCache.Valid;
			unprocessedDataCache.Invalidate ();
			if (fireEvents && wasValid) {
				if (null != DataChanged) {
					DataChanged (new TrackDataChangedEventArgs (this, this, TrackDataStage.Unprocessed));
				}
			}
		}

		public void InvalidateProcessedData (bool fireEvents = true)
		{
			bool wasValid = processedDataCache.Valid;
			processedDataCache.Invalidate ();
			if (fireEvents && wasValid) {
				if (null != DataChanged) {
					DataChanged (new TrackDataChangedEventArgs (this, this, TrackDataStage.Processed));
				}
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
			Path path = pathData.Path;
			if (null != path) {
				path.Changed -= d;
				path.Changed += d;
			}
		}
		
		private void DoUnregisterPathChangedListeners ()
		{
			//          Debug.Log ("Unregistering PathChangedEventHandler on '" + path + "': " + this.gameObject.name);
			Path path = pathData.Path;
			if (null != path) {
				path.Changed -= PathChanged;
			}
		}

		// Receives PathChangedEvent from one of our configured Path instances
		private void PathChanged (PathChangedEvent e)
		{
			//NewPath path = (NewPath)sender;
			Debug.Log (ToString () + ": received PathChangdEvent: " + e);
			// Ignore snapshot specifications in comparision:
			if (pathData.WithoutSnapshot () == e.ChangedData.WithoutSnapshot ()) {
				// Our data has changed
				Debug.Log (ToString () + ": path data has changed: " + e.ChangedData);

				InvalidateUnprocessedData (true);
//				InvalidateProcessedData(true);

			}        
			
		}

	}

	[Serializable]
	public class TrackDataCache
	{
		[SerializeField]
		private PathPoint[]
			points;
		
		[SerializeField]
		private int
			flags;
		
		
		public TrackDataCache ()
		{
			
		}
		
		public PathPoint[] Points {
			get {
				return this.points;
			}
			set {
				this.points = value;
			}
		}
		
		public int Flags {
			get {
				return this.flags;
			}
			set {
				this.flags = value;
			}
		}
		
		
		public bool Valid {
			get {
				return null != points;
			}
			set {
				if (value == false) {
					Invalidate ();
				}
			}
		}
		
		public void Invalidate ()
		{
			this.points = null;
			this.flags = 0;
		}
		
		public delegate PathPoint[] GetPointsAndFlagsDelegate (out int flags);
		
		public PathPoint[] GetPointsAndValidate (GetPointsAndFlagsDelegate getPointsAndFlagsFunc)
		{
			if (!Valid) {
				points = getPointsAndFlagsFunc (out flags);
				if (null != points) {
					Valid = true;
				}
			}
			return points;
		}
		public PathPoint[] GetPointsAndValidate (Func<PathPoint[]> getPointsFunc)
		{
			if (!Valid) {
				// Our GetPointsAndValidate function also fetches flags:
				points = getPointsFunc ();
				Valid = (null != points);
			}
			return points;
		}
		public int GetFlagsAndValidate (GetPointsAndFlagsDelegate getPointsAndFlagsFunc)
		{
			if (!Valid) {
				// Our GetPointsAndValidate function also fetches flags:
				GetPointsAndValidate (getPointsAndFlagsFunc);
			}
			return flags;
		}
		public int GetFlagsAndValidate (Func<int> getFlagsFunc)
		{
			if (!Valid) {
				// Our GetPointsAndValidate function also fetches flags:
				flags = getFlagsFunc ();
				Valid = true;
			}
			return flags;
		}
		//
		//		public PathPoint[] GetPointsAndValidate(Func<PathPoint[]> fetchPointsFunc) {
		//			if (!Valid) {
		//				points = fetchPointsFunc();
		//				if (null != points) {
		//					Valid = true;
		//				}
		//			}
		//			return points;
		//		}
		//		public PathPoint[] GetFlagsAndValidate(Func<int> fetchFlagsFunc) {
		//			if (!Valid) {
		//				flags = fetchFlagsFunc();
		//				Valid = true;
		//			}
		//			return flags;
		//		}
		
	}

}
