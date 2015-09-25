// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

using Util;

// TODO we need a GetPoint(float t) method, either directly in a path
// or as a separate utility. For pure bezier paths it would be easy to implement,
// but for polyline or composite paths it woudl need some more effort by
// implementing a smart smoothing / interpolating algorithm. One possiblity would
// be to convert polyline path to bezier path on-the-fly.

namespace Paths
{
	// TODO do we really need separate IPathInfo? Maybe yes, because some path related
	// tools can be used without the actual Path object, for example PathModifiers don't
	// really need a reference to Path, the just need valid array of PathPoints. In some
	// cases we don't have the original Path instance available, for example Track has
	// its own set of PathModifiers.
	public interface IPathInfo
	{
		/// <summary>
		/// Determines whether this instance is loop path, i.e. the last point is connected
		/// to the first point. The returned last point has always the same position, direction,
		/// up vector etc as the first but its distance components are measured from the last
		/// actual point (or the last control point) to the first point.
		/// </summary>
		/// <returns><c>true</c> if this instance is loop path; otherwise, <c>false</c>.</returns>
		bool IsLoop ();
	}

	public sealed class EmptyPathInfo : IPathInfo
	{
		public static EmptyPathInfo Instance = new EmptyPathInfo ();
		private EmptyPathInfo ()
		{

		}
		public bool IsLoop ()
		{
			return false;
		}
	}


	public interface IPathData
	{
//		Path GetPath ();
//		bool IsEnabled();

		int GetId ();
		string GetName ();

		Color GetColor ();
		void SetColor (Color value);

		bool IsDrawGizmos ();
		void SetDrawGizmos (bool value);

		// TODO do we really need this?
		IPathInfo GetPathInfo ();

		IPathSnapshotManager GetPathSnapshotManager ();
		IPathModifierContainer GetPathModifierContainer ();

		// TODO should control point operations be in different interface?
		int GetControlPointCount ();
		PathPoint GetControlPointAtIndex (int index);
		void SetControlPointAtIndex (int index, PathPoint pt);

		PathPoint[] GetAllPoints ();
		int GetPointCount ();
		PathPoint GetPointAtIndex (int index);

		int GetOutputFlags ();
		int GetOutputFlagsBeforeModifiers ();
		float GetTotalDistance ();

		bool IsPathMetadataSupported ();
		IPathMetadata GetPathMetadata ();

		/// <summary>
		/// Determines whether data of this instance is up to date with the configuration.
		/// </summary>
		/// <returns><c>true</c> if this instance is up to date; otherwise, <c>false</c>.</returns>
		bool IsUpToDate ();


		/// <summary>
		/// Gets the status token that can be used later to determine if the path data has been
		/// modified since we got this status token.
		/// </summary>
		/// <returns>The status token.</returns>
		long GetStatusToken ();
		
	}


	public interface IAttachableToPath
	{
		void AttachToPath (Path path, PathChangedEventHandler dataChangedHandler);
		void DetachFromPath (Path path);
	}

	public class CircularPathReferenceException : Exception
	{
		public CircularPathReferenceException () : base()
		{

		}
		public CircularPathReferenceException (string message) : base(message)
		{
			
		}
	}

}
