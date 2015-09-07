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
