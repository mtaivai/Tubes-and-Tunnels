using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Paths
{

	[PathModifier(requiredInputFlags=PathPoint.NONE, 
                  processCaps=PathPoint.NONE,
                  passthroughCaps=PathPoint.NONE, 
                  generateCaps=PathPoint.POSITION | PathPoint.DIRECTION | PathPoint.UP | PathPoint.DISTANCES)]
	public class GeneratePathModifier : ConfigurableProcessPathModifier
	{
		// TODO this is always discarding the existing path! Add behaviour like in IncludePathModifer
		// or even better, make a common subclass for both. Yes, that's what we'll do!
		public enum GenerateFunction
		{
			Straight,
			Ellipse,
		}
		public GenerateFunction function;
		public bool closeEllipse;
		public int pointCount;
		public Vector3 direction;
		public float sizeMagnitude;
		public Vector3 sizeVector;
		public CoordinatePlane plane;

		protected override void GetAllowedFunctions (out int positionMask, out int directionMask, 
                                                     out int distanceFromPreviousMask, out int distanceFromBeginMask,
                                                     out int upVectorMask, out int angleMask)
		{
			positionMask = MaskGenerate;
			directionMask = MaskGenerate | MaskRemove;
			distanceFromBeginMask = distanceFromPreviousMask = MaskGenerate | MaskRemove;

			upVectorMask = MaskGenerate | MaskRemove;
			angleMask = 0;//MaskGenerate | MaskRemove;

		}

		public override string GetDescription ()
		{
			return "Generate different paths or path segments";
		}

		protected override void OnResetConfiguration ()
		{
			function = GenerateFunction.Straight;
			closeEllipse = false;
			pointCount = 10;
			direction = Vector3.forward;
			sizeMagnitude = 1.0f;
			sizeVector = new Vector3 (1f, 1f, 1f);
			plane = CoordinatePlane.XZ;

		}

		protected override void OnSerializeCustom (Serializer store)
		{
			store.EnumProperty ("function", ref function);
			store.Property ("closeEllipse", ref closeEllipse);
			store.Property ("pointCount", ref pointCount);
			store.Property ("direction", ref direction);
			store.Property ("sizeMagnitude", ref sizeMagnitude);
			store.Property ("sizeVector", ref sizeVector);
			store.EnumProperty ("plane", ref plane);

		}

		protected override PathPoint[] DoGetModifiedPoints (PathPoint[] points)
		{
			int ppFlags = GetOutputFlags (context);

			bool loopPath = context.PathInfo.IsLoop ();

			Vector3[] positions;

			switch (function) {
			case GenerateFunction.Straight:
			case GenerateFunction.Ellipse:


				positions = CreateEllipse ();
				break;
			default:
				positions = new Vector3[0];
				break;

			}

			points = new PathPoint[positions.Length];



			float distFromBegin = 0.0f;
			for (int i = 0; i < points.Length; i++) {
				Vector3 pos = positions [i];

				float distFromPrev = 0.0f;
				bool distFromPrevKnown = false;
				switch (DistanceFromPreviousFunction) {
				case PathModifierFunction.Generate:
					distFromPrev = GetDistanceFromPrev (positions, i);
					distFromPrevKnown = true;
					break;
				}
				switch (DistanceFromBeginFunction) {
				case PathModifierFunction.Generate:
					if (!distFromPrevKnown) {
						distFromPrev = GetDistanceFromPrev (positions, i);
					}
					distFromBegin += distFromPrev;
					break;
				}

				points [i] = new PathPoint (pos, Vector3.zero, Vector3.zero, 0f, distFromPrev, distFromBegin, ppFlags);
			}

			// Second pass for directions (required to calculate them correctly for looped paths)
			for (int i = 0; i < points.Length; i++) {
				Vector3 pos = points [i].Position;
				Vector3 dir, up;
				Vector3 prevDir;
				Vector3 nextDir;

				bool dirKnown = false;
				bool prevAndNextDirKnown = false;

				switch (DirectionFunction) {
				case PathModifierFunction.Generate:
					dir = PathUtil.GetPathDirectionAtPoint (positions, i, loopPath, out prevDir, out nextDir);
					dirKnown = true;
					prevAndNextDirKnown = true;
					break;
				default:
					dir = points [i].Direction;
					dirKnown = points [i].HasDirection;
					break;
				}
                
				// Cross product things
                
				switch (UpVectorFunction) {
				case PathModifierFunction.Generate:
                    
                    // Do we have dir?
                    // TODO get Up vector algorithm from GenerateComponentsPathModifier
                    
                    //up = Vector3.up;
                    
					if (!(dirKnown && prevAndNextDirKnown)) {

						dir = PathUtil.GetPathDirectionAtPoint (positions, i, loopPath, out prevDir, out nextDir);
						dirKnown = prevAndNextDirKnown = true;
					}
					Vector3 cross = Vector3.Cross (-prevDir, nextDir).normalized;
					Vector3 right = cross;

					up = Quaternion.AngleAxis (-90, right) * dir;
                    //Vector3.OrthoNormalize(ref dir, ref right, ref up);
                    //up = Vector3.up;
					break;
				default:
					up = Vector3.zero;
					break;
				}

                
				// TODO change the following if we ever make PathPoint mutable:
                
				points [i] = new PathPoint (pos, dir, up, 0f, 
                                            points [i].DistanceFromPrevious, 
                                            points [i].DistanceFromBegin, ppFlags);
			}

			return points;
		}

		private float GetDistanceFromPrev (Vector3[] points, int index)
		{
			if (index == 0) {
				return 0.0f;
			} else {
				return (points [index] - points [index - 1]).magnitude;
			}
		}

		Vector3[] CreateEllipse ()
		{
			float sliceRotation = 0f;


			Vector2 sz = CoordinateUtil.ToVector2 (sizeVector, plane) * sizeMagnitude;

			Vector3[] points = new Vector3[pointCount];
			if ((closeEllipse && pointCount > 3) || (!closeEllipse && pointCount > 2)) {
            
				// Rotate to keep ceiling up and floor down:
				float rotation = (180.0f + sliceRotation) * Mathf.Deg2Rad;
				//rotation = 0.0f;

				// TODO What's this????
//				Quaternion rot = Quaternion.AngleAxis (-90, Vector3.up);

				// TODO add configuration for "dir"????
				float dir = 1.0f;


				int edges = closeEllipse ? pointCount - 1 : pointCount;

				for (int i = 0; i < edges; i++) {
					Vector3 pt;
                    
					float t = ((float)i / (float)edges) * Mathf.PI * 2 + rotation;
					float x = sz.x * Mathf.Cos (t * dir);
					float y = sz.y * Mathf.Sin (t * dir);
					//                pt = new Vector3(x, y, 0);
					//                points [i] = rot * pt;
					pt = CoordinateUtil.ToVector3 (new Vector2 (x, y), plane);
					points [i] = pt;

				}
				if (closeEllipse) {
					points [edges] = points [0];
				}
			}
			return points;
		}

	}


	// TODO currently we can only generate a straight line
	// TODO should we have other algorithms as well?
	[PathModifier(requiredInputFlags=PathPoint.POSITION, 
                  processCaps=PathPoint.ALL,
                  passthroughCaps=PathPoint.ALL, 
                  generateCaps=PathPoint.ALL)]
	public class OldGeneratePathModifier : AbstractPathModifier
	{

		public int pointCount = 3;
		public Vector3 dir = Vector3.forward;
		public float magnitude = 1.0f;
		public bool replaceExisting = false;

		protected override PathPoint[] DoGetModifiedPoints (PathPoint[] points)
		{

			int ppFlags = (GetPassthroughFlags (context) & context.InputFlags) | GetGenerateFlags (context);

			Vector3 normalDir = dir.normalized;

			PathPoint[] results;
			int resultIndexOffset;
			int totalPointCount;
			float pathLength = 0.0f;
			Vector3 ptOffs;
			if (replaceExisting) {
				totalPointCount = pointCount;
				resultIndexOffset = 0;
				results = new PathPoint[totalPointCount];
				ptOffs = Vector3.zero;
			} else {
				totalPointCount = points.Length + pointCount;
				resultIndexOffset = points.Length;
				results = new PathPoint[totalPointCount];
				// Copy original points to beginning:
				for (int i = 0; i < points.Length; i++) {
					results [i] = points [i];
					pathLength += results [i].DistanceFromPrevious;
				}
				if (points.Length > 0) {
					ptOffs = results [points.Length - 1].Position + (normalDir * magnitude);
				} else {
					ptOffs = Vector3.zero;
				}
			}

			// Generate new points
            
			for (int i = 0; i < pointCount; i++) {
				int resultIndex = i + resultIndexOffset;
                
				float distFromPrev = magnitude;
				float distFromBegin = (i > 0) ? results [i - 1].DistanceFromBegin + magnitude : 0.0f;
				//Vector3 dir = points[i].Direction;

//              if (i > 0) {
//                  ptOffs = results[i - 1].Position;
//              }
				Vector3 pos = ptOffs + (normalDir * (magnitude * (float)i));
                
				results [resultIndex] = new PathPoint (pos, dir, Vector3.zero, 0f, distFromPrev, distFromBegin, ppFlags);
			} 

			return results;
		}
        
		public override void OnSerialize (Serializer store)
		{
			store.Property ("pointCount", ref pointCount);
			store.Property ("dir", ref dir);
			store.Property ("magnitude", ref magnitude);
			store.Property ("replaceExisting", ref replaceExisting);
		}
    
	}
    
}