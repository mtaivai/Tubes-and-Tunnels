using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Paths
{
	public enum PathModifierFunction
	{
		None,
		Passthrough,
		Process,
		Generate,
		Remove
	}

	public abstract class ConfigurableProcessPathModifier : AbstractPathModifier
	{

		public const int MaskPassthrough = 0x01;
		public const int MaskProcess = 0x02;
		public const int MaskGenerate = 0x04;
		public const int MaskRemove = 0x08;
		public const int MaskAll = 0x0f;
		public const int MaskNone = 0x00;
		private PathModifierFunction positionFunction = PathModifierFunction.None;
		private PathModifierFunction directionFunction = PathModifierFunction.None;
		private PathModifierFunction upVectorFunction = PathModifierFunction.None;
		private PathModifierFunction distanceFromPreviousFunction = PathModifierFunction.None;
		private PathModifierFunction distanceFromBeginFunction = PathModifierFunction.None;
		private PathModifierFunction angleFunction = PathModifierFunction.None;
		private int allowedPositionFunctionsMask = MaskNone;
		private int allowedDirectionFunctionsMask = MaskNone;
		private int allowedUpVectorFunctionsMask = MaskNone;
		private int allowedDistanceFromPreviousFunctionsMask = MaskNone;
		private int allowedDistanceFromBeginFunctionsMask = MaskNone;
		private int allowedAngleFunctionsMask = MaskNone;

		public ConfigurableProcessPathModifier ()
		{
			Reset ();
			GetAllowedFunctions (
                out allowedPositionFunctionsMask, out allowedDirectionFunctionsMask, 
                out allowedDistanceFromPreviousFunctionsMask, out allowedDistanceFromBeginFunctionsMask, 
                out allowedUpVectorFunctionsMask, out allowedAngleFunctionsMask);
			// Initial values:
			if (!IsAllowedFunction (positionFunction, allowedPositionFunctionsMask)) {
				positionFunction = GetFirstAllowedFunction (AllowedPositionFunctions);
			}
			if (!IsAllowedFunction (directionFunction, allowedDirectionFunctionsMask)) {
				directionFunction = GetFirstAllowedFunction (AllowedDirectionFunctions);
			}
			if (!IsAllowedFunction (upVectorFunction, allowedUpVectorFunctionsMask)) {
				upVectorFunction = GetFirstAllowedFunction (AllowedUpVectorFunctions);
			}
			if (!IsAllowedFunction (distanceFromPreviousFunction, allowedDistanceFromPreviousFunctionsMask)) {
				distanceFromPreviousFunction = GetFirstAllowedFunction (AllowedDistanceFromPreviousFunctions);
			}
			if (!IsAllowedFunction (distanceFromBeginFunction, allowedDistanceFromBeginFunctionsMask)) {
				distanceFromBeginFunction = GetFirstAllowedFunction (AllowedDistanceFromBeginFunctions);
			}
			if (!IsAllowedFunction (angleFunction, allowedAngleFunctionsMask)) {
				angleFunction = GetFirstAllowedFunction (AllowedAngleFunctions);
			}


		}

		private static PathModifierFunction GetFirstAllowedFunction (PathModifierFunction[] functions)
		{
			if (functions.Length > 0) {
				return functions [0];
			} else {
				return PathModifierFunction.None;
			}
		}

		public abstract void Reset ();

		protected abstract void GetAllowedFunctions (out int positionMask, out int directionMask, 
                                                    out int distanceFromBeginMask, out int distanceFromPreviousMask,
                                                    out int upVectorMask, out int angleMask);

		public static bool IsAllowedFunction (PathModifierFunction function, int mask)
		{
			if (function == PathModifierFunction.Passthrough) {
				return (mask & MaskPassthrough) == MaskPassthrough;
			} else if (function == PathModifierFunction.Process) {
				return (mask & MaskProcess) == MaskProcess;
			} else if (function == PathModifierFunction.Generate) {
				return (mask & MaskGenerate) == MaskGenerate;
			} else if (function == PathModifierFunction.Remove) {
				return (mask & MaskRemove) == MaskRemove;
			} else {
				return false;
			}
		}

		public PathModifierFunction PositionFunction {
			get {
				return positionFunction;
			}
			set {

				if (IsAllowedFunction (value, allowedPositionFunctionsMask)) {
					this.positionFunction = value;
				}
			}
		}

		public PathModifierFunction DirectionFunction {
			get {
				return directionFunction;
			}
			set {
				if (IsAllowedFunction (value, allowedDirectionFunctionsMask)) {
					this.directionFunction = value;
				}
			}
		}

		public PathModifierFunction UpVectorFunction {
			get {
				return upVectorFunction;
			}
			set {
				if (IsAllowedFunction (value, allowedUpVectorFunctionsMask)) {
					this.upVectorFunction = value;
				}
			}
		}

		public PathModifierFunction DistanceFromPreviousFunction {
			get {
				return distanceFromPreviousFunction;
			}
			set {
				if (IsAllowedFunction (value, allowedDistanceFromPreviousFunctionsMask)) {
					this.distanceFromPreviousFunction = value;
				}
			}
		}

		public PathModifierFunction DistanceFromBeginFunction {
			get {
				return distanceFromBeginFunction;
			}
			set {
				if (IsAllowedFunction (value, allowedDistanceFromBeginFunctionsMask)) {
					this.distanceFromBeginFunction = value;
				}
			}
		}

		public PathModifierFunction AngleFunction {
			get {
				return angleFunction;
			}
			set {
				if (IsAllowedFunction (value, allowedAngleFunctionsMask)) {
					this.angleFunction = value;
				}
			}
		}

		public int AllowedPositionFunctionsMask {
			get {
				return allowedPositionFunctionsMask;
			}
		}

		public PathModifierFunction[] AllowedPositionFunctions {
			get {
				return GetAllowedOutputTypes (allowedPositionFunctionsMask);
			}
		}

		public PathModifierFunction[] AllowedDirectionFunctions {
			get {
				return GetAllowedOutputTypes (allowedDirectionFunctionsMask);
			}
		}

		public PathModifierFunction[] AllowedUpVectorFunctions {
			get {
				return GetAllowedOutputTypes (allowedUpVectorFunctionsMask);
			}
		}

		public PathModifierFunction[] AllowedAngleFunctions {
			get {
				return GetAllowedOutputTypes (allowedAngleFunctionsMask);
			}
		}

		public PathModifierFunction[] AllowedDistanceFromPreviousFunctions {
			get {
				return GetAllowedOutputTypes (allowedDistanceFromPreviousFunctionsMask);
			}
		}

		public PathModifierFunction[] AllowedDistanceFromBeginFunctions {
			get {
				return GetAllowedOutputTypes (allowedDistanceFromBeginFunctionsMask);
			}
		}

		private static PathModifierFunction[] CopyFunctions (PathModifierFunction[] arr)
		{
			PathModifierFunction[] cp = new PathModifierFunction[arr.Length];
			for (int i = 0; i < arr.Length; i++) {
				cp [i] = arr [i];
			}
			return cp;
		}

		private static PathModifierFunction[] GetAllowedOutputTypes (int allowedMask)
		{
			List<PathModifierFunction> l = new List<PathModifierFunction> ();
			foreach (PathModifierFunction t in Enum.GetValues(typeof(PathModifierFunction))) {
				if (IsAllowedFunction (t, allowedMask)) {
					l.Add (t);
				}
			}
			return l.ToArray ();
		}

		public override int GetProcessFlags (PathModifierContext context)
		{
			int f = 0;
			if (positionFunction == PathModifierFunction.Process) {
				f |= PathPoint.POSITION;
			}
			if (directionFunction == PathModifierFunction.Process) {
				f |= PathPoint.DIRECTION;
			}
			if (distanceFromPreviousFunction == PathModifierFunction.Process) {
				f |= PathPoint.DISTANCE_FROM_PREVIOUS;
			}
			if (distanceFromBeginFunction == PathModifierFunction.Process) {
				f |= PathPoint.DISTANCE_FROM_BEGIN;
			}
			if (upVectorFunction == PathModifierFunction.Process) {
				f |= PathPoint.UP;
			}
			if (angleFunction == PathModifierFunction.Process) {
				f |= PathPoint.ANGLE;
			}
			return f;
		}

		public override int GetGenerateFlags (PathModifierContext context)
		{
			int f = 0;
			if (positionFunction == PathModifierFunction.Generate) {
				f |= PathPoint.POSITION;
			}
			if (directionFunction == PathModifierFunction.Generate) {
				f |= PathPoint.DIRECTION;
			}
			if (distanceFromPreviousFunction == PathModifierFunction.Generate) {
				f |= PathPoint.DISTANCE_FROM_PREVIOUS;
			}
			if (distanceFromBeginFunction == PathModifierFunction.Generate) {
				f |= PathPoint.DISTANCE_FROM_BEGIN;
			}
			if (upVectorFunction == PathModifierFunction.Generate) {
				f |= PathPoint.UP;
			}
			if (angleFunction == PathModifierFunction.Generate) {
				f |= PathPoint.ANGLE;
			}
			return f;
		}
        
		public override int GetPassthroughFlags (PathModifierContext context)
		{
			int f = 0;
			if (positionFunction == PathModifierFunction.Passthrough) {
				f |= PathPoint.POSITION;
			}
			if (directionFunction == PathModifierFunction.Passthrough) {
				f |= PathPoint.DIRECTION;
			}
			if (upVectorFunction == PathModifierFunction.Passthrough) {
				f |= PathPoint.UP;
			}
			if (distanceFromPreviousFunction == PathModifierFunction.Passthrough) {
				f |= PathPoint.DISTANCE_FROM_PREVIOUS;
			}
			if (distanceFromBeginFunction == PathModifierFunction.Passthrough) {
				f |= PathPoint.DISTANCE_FROM_BEGIN;
			}
			if (angleFunction == PathModifierFunction.Passthrough) {
				f |= PathPoint.ANGLE;
			}
			return f;
		}

		private bool _onSerialize;

		public override sealed void OnSerialize (Serializer store)
		{
			if (!_onSerialize) {
				_onSerialize = true;
				try {
					PositionFunction = (PathModifierFunction)store.ReturnEnumProperty ("positionOutput", PositionFunction);
					DirectionFunction = (PathModifierFunction)store.ReturnEnumProperty ("directionOutput", DirectionFunction);
					UpVectorFunction = (PathModifierFunction)store.ReturnEnumProperty ("upVectorOutput", UpVectorFunction);
					DistanceFromPreviousFunction = (PathModifierFunction)store.ReturnEnumProperty ("distanceOutput", DistanceFromPreviousFunction);
					DistanceFromBeginFunction = (PathModifierFunction)store.ReturnEnumProperty ("distanceOutput", DistanceFromBeginFunction);
					AngleFunction = (PathModifierFunction)store.ReturnEnumProperty ("angleOutput", AngleFunction);

					OnSerializeCustom (store);
				} finally {
					_onSerialize = false;
				}
			}

     
		}

		protected virtual void OnSerializeCustom (Serializer store)
		{
		}

		// TODO consider implementing OutputType etc options in a common base class

	}
}
