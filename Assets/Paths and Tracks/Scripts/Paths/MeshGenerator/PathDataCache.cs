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


	[Serializable]
	public class PathDataCache
	{
		[SerializeField]
		private PathPoint[]
			points;

		[SerializeField]
		private bool
			loop;
		
		[SerializeField]
		private int
			flags;

		[SerializeField]
		private PathMetadataCache
			metadata;

		[SerializeField]
		private bool
			metadataValid;

		[SerializeField]
		private bool
			pointsValid;

//		[SerializeField]
//		private long
//			statusToken;


		public PathDataCache ()
		{
		}
		
		public PathPoint[] Points {
			get {
				return this.points;
			}
			set {
				this.points = value;
				DataValid = true;
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

		public bool Loop {
			get {
				return this.loop;
			}
			set {
				this.loop = value;
			}
		}
		
		public bool DataValid {
			get {
				return pointsValid;
			}
			set {
				if (value == false) {
					InvalidateData ();
				} else {
					this.pointsValid = true;
				}
			}
		}
		public bool MetadataValid {
			get {
				return metadataValid;
			}
			set {
				if (value == false) {
					InvalidateMetadata ();
				} else {
					this.metadataValid = true;
				}
			}
		}

//		public long StatusToken {
//			get {
//				return statusToken;
//			}
//		}

		public void InvalidateData ()
		{
			this.points = null;
			this.flags = 0;
			this.loop = false;
			this.pointsValid = false;
//			this.statusToken = System.DateTime.Now.Ticks;
		}
		public void InvalidateMetadata ()
		{
			this.metadataValid = false;
			this.metadata = null;
		}
		public delegate PathPoint[] GetPointsAndFlagsDelegate (out int flags);

		public PathPoint[] GetPointsAndValidate (GetPointsAndFlagsDelegate getPointsAndFlagsFunc, Func<IPathInfo> getPathInfoFunc)
		{
			return GetPointsAndValidate (true, getPointsAndFlagsFunc, getPathInfoFunc);
		}
		public PathPoint[] GetPointsAndValidate (bool validateOnNullPoints, GetPointsAndFlagsDelegate getPointsAndFlagsFunc, Func<IPathInfo> getPathInfoFunc)
		{
			if (!DataValid) {
				points = getPointsAndFlagsFunc (out flags);
				ProcessPathInfo (getPathInfoFunc);
				DataValid = validateOnNullPoints || (null != points);

			}
			return points;
		}
		private void ProcessPathInfo (Func<IPathInfo> getPathInfoFunc)
		{
			if (null != getPathInfoFunc) {
				IPathInfo pathInfo = getPathInfoFunc ();
				this.loop = (null != pathInfo) ? pathInfo.IsLoop () : false;
			}
		}
		public PathPoint[] GetPointsAndValidate (Func<PathPoint[]> getPointsFunc, Func<IPathInfo> getPathInfoFunc)
		{
			return GetPointsAndValidate (true, getPointsFunc, getPathInfoFunc);
		}
		public PathPoint[] GetPointsAndValidate (bool validateOnNullPoints, Func<PathPoint[]> getPointsFunc, Func<IPathInfo> getPathInfoFunc)
		{
			if (!DataValid) {
				// Our GetPointsAndValidate function also fetches flags
				// TODO we don't fetch flags, i.e. flags are NOT valid after this call!
				points = getPointsFunc ();
				ProcessPathInfo (getPathInfoFunc);
				DataValid = validateOnNullPoints || (null != points);
			}
			return points;
		}
		public int GetFlagsAndValidate (GetPointsAndFlagsDelegate getPointsAndFlagsFunc, Func<IPathInfo> getPathInfoFunc)
		{
			return GetFlagsAndValidate (true, getPointsAndFlagsFunc, getPathInfoFunc);
		}
		public int GetFlagsAndValidate (bool validateOnNullPoints, GetPointsAndFlagsDelegate getPointsAndFlagsFunc, Func<IPathInfo> getPathInfoFunc)
		{
			if (!DataValid) {
				// GetPointsAndFlagsDelegate function also fetches flags:
				GetPointsAndValidate (validateOnNullPoints, getPointsAndFlagsFunc, getPathInfoFunc);
			}
			return flags;
		}

		public IPathMetadata GetPathMetadataAndValidate (Func<IPathMetadata> getPathMetadataFunc)
		{
			if (!MetadataValid) {
				IPathMetadata md = getPathMetadataFunc ();
				this.metadata = new PathMetadataCache ();
				this.metadata.Import (md, false);
				this.metadataValid = true;
			}
			return this.metadata;
		}

//		public int GetFlagsAndValidate (Func<int> getFlagsFunc)
//		{
//			if (!Valid) {
//				// TODO points are NOT valid after this call!
//				flags = getFlagsFunc ();
//				Valid = true;
//			}
//			return flags;
//		}
//		//
//		//		public PathPoint[] GetPointsAndValidate(Func<PathPoint[]> fetchPointsFunc) {
//		//			if (!Valid) {
//		//				points = fetchPointsFunc();
//		//				if (null != points) {
//		//					Valid = true;
//		//				}
//		//			}
//		//			return points;
//		//		}
//		//		public PathPoint[] GetFlagsAndValidate(Func<int> fetchFlagsFunc) {
//		//			if (!Valid) {
//		//				flags = fetchFlagsFunc();
//		//				Valid = true;
//		//			}
//		//			return flags;
//		//		}
		
	}

	public class PathMetadataCache : DefaultPathMetadata
	{
		public PathMetadataCache ()
		{
		}

	}

}
