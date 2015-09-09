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
		private int
			flags;

		[SerializeField]
		private bool
			pointsValid;

		public PathDataCache ()
		{
			
		}
		
		public PathPoint[] Points {
			get {
				return this.points;
			}
			set {
				this.points = value;
				Valid = true;
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
				return pointsValid;
			}
			set {
				if (value == false) {
					Invalidate ();
				} else {
					this.pointsValid = true;
				}
			}
		}
		
		public void Invalidate ()
		{
			this.points = null;
			this.flags = 0;
			this.pointsValid = false;
		}
		
		public delegate PathPoint[] GetPointsAndFlagsDelegate (out int flags);

		public PathPoint[] GetPointsAndValidate (GetPointsAndFlagsDelegate getPointsAndFlagsFunc)
		{
			return GetPointsAndValidate (true, getPointsAndFlagsFunc);
		}
		public PathPoint[] GetPointsAndValidate (bool validateOnNullPoints, GetPointsAndFlagsDelegate getPointsAndFlagsFunc)
		{
			if (!Valid) {
				points = getPointsAndFlagsFunc (out flags);
				Valid = validateOnNullPoints || (null != points);
			}
			return points;
		}

		public PathPoint[] GetPointsAndValidate (Func<PathPoint[]> getPointsFunc)
		{
			return GetPointsAndValidate (true, getPointsFunc);
		}
		public PathPoint[] GetPointsAndValidate (bool validateOnNullPoints, Func<PathPoint[]> getPointsFunc)
		{
			if (!Valid) {
				// Our GetPointsAndValidate function also fetches flags
				// TODO we don't fetch flags, i.e. flags are NOT valid after this call!
				points = getPointsFunc ();
				Valid = validateOnNullPoints || (null != points);
			}
			return points;
		}
		public int GetFlagsAndValidate (GetPointsAndFlagsDelegate getPointsAndFlagsFunc)
		{
			return GetFlagsAndValidate (true, getPointsAndFlagsFunc);
		}
		public int GetFlagsAndValidate (bool validateOnNullPoints, GetPointsAndFlagsDelegate getPointsAndFlagsFunc)
		{
			if (!Valid) {
				// GetPointsAndFlagsDelegate function also fetches flags:
				GetPointsAndValidate (validateOnNullPoints, getPointsAndFlagsFunc);
			}
			return flags;
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

}
