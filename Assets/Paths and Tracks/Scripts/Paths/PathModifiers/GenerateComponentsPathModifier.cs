using UnityEngine;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Paths
{



	[CodeQuality.Experimental]
	// TODO consider implementing OutputType etc options in a common base class
	[PathModifier(requiredInputFlags=PathPoint.POSITION, 
                  passthroughCaps=PathPoint.POSITION | PathPoint.UP | PathPoint.ANGLE, 
                  generateCaps=PathPoint.DIRECTION | PathPoint.DISTANCES)]
	public class GenerateComponentsPathModifier : ConfigurableProcessPathModifier
	{

		public enum UpVectorAlgorithm
		{
			PlaneNormal,
			Bank, 
			Constant,
		}



		public CoordinatePlane generateAnglePlane = CoordinatePlane.XZ;
		public CoordinatePlane generateUpVectorPlane = CoordinatePlane.XZ;
		public UpVectorAlgorithm upVectorAlgorithm;
		public float bankFactor;
		public float bankFactorMultiplier;
		public float maxBankAngle;
		public float bankSmoothingFactor;
		public Vector3 constantUpVector;

		public override string GetDescription ()
		{
			return "This Path Modifier can (re)generate PathPoint components with different algorithms.";
		}

		protected override void OnResetConfiguration ()
		{

			PositionFunction = PathModifierFunction.Passthrough;
			DirectionFunction = PathModifierFunction.Generate;
			UpVectorFunction = PathModifierFunction.Passthrough;
			DistanceFromPreviousFunction = PathModifierFunction.Generate;
			DistanceFromBeginFunction = PathModifierFunction.Generate;
			AngleFunction = PathModifierFunction.Passthrough;

			// TODO we should have these as mandatory settings, e.g. in constructor args or
			// as an abstract method GetAllowedOutputMasks(out int positionMask, ....)

			generateAnglePlane = CoordinatePlane.XZ;
			generateUpVectorPlane = CoordinatePlane.XZ;
            
			upVectorAlgorithm = UpVectorAlgorithm.Constant;
			bankFactor = 1.0f;
			bankFactorMultiplier = 1.0f;
			maxBankAngle = 45.0f;
			bankSmoothingFactor = 0.5f;
			constantUpVector = Vector3.up;

		}

		protected override void GetAllowedFunctions (out int positionMask, out int directionMask, 
                                                    out int distanceFromPreviousMask, out int distanceFromBeginMask, 
                                                    out int upVectorMask, out int angleMask)
		{
			positionMask = MaskPassthrough;
			directionMask = MaskPassthrough | MaskGenerate | MaskRemove;
			upVectorMask = MaskPassthrough | MaskGenerate | MaskRemove;
			angleMask = MaskPassthrough | MaskGenerate | MaskRemove;
			distanceFromPreviousMask = MaskPassthrough | MaskGenerate | MaskRemove;
			distanceFromBeginMask = distanceFromPreviousMask;
		}

		protected override PathPoint[] DoGetModifiedPoints (PathPoint[] points)
		{

//          int ppFlags = PathPoint.POSITION;
//          if (directionOutput != OutputType.Remove) {
//              ppFlags |= PathPoint.DIRECTION;
//          }
//          if (distanceOutput != OutputType.Remove) {
//              ppFlags |= PathPoint.DISTANCE_FROM_BEGIN | PathPoint.DISTANCE_FROM_PREVIOUS;
//          }
			int ppFlags = GetOutputFlags (context);

			bool loopPath = context.PathInfo.IsLoop ();
			Vector3 normalizedConstantUpVector = constantUpVector.normalized;


			float distBegin = 0.0f;
			float currentBankAngle = 0.0f;
			for (int i = 0; i < points.Length; i++) {

				float distPrev = 0.0f;

				bool prevAndNextDirKnown = false;
				Vector3 prevDir = Vector3.zero, nextDir = Vector3.zero;

				Vector3 dir;
				if (DirectionFunction == PathModifierFunction.Generate) {
					dir = PathUtil.GetPathDirectionAtPoint (points, i, loopPath, Vector3.zero, out prevDir, out nextDir);
					points [i].Direction = dir;
					prevAndNextDirKnown = true;
                    
				} else if (DirectionFunction == PathModifierFunction.Passthrough) {
					dir = points [i].Direction;
				} else {
					dir = Vector3.zero;
				}


				float angle = 0.0f;
				bool angleKnown = false;
				if (AngleFunction == PathModifierFunction.Generate) {
					if (!prevAndNextDirKnown) {
						dir = PathUtil.GetPathDirectionAtPoint (points, i, false, Vector3.zero, out prevDir, out nextDir);
						prevAndNextDirKnown = true;
					}

					angle = CalculateAngle (points, i, prevDir, nextDir);
					points [i].Angle = angle;
					angleKnown = true;

				} else if (AngleFunction == PathModifierFunction.Passthrough) {

					angleKnown = points [i].HasAngle;
					if (angleKnown) {
						angle = points [i].Angle;
					} else {
						angle = 0.0f;
					}

				} else {
					angle = 0.0f;
				}


				Vector3 up;
				if (UpVectorFunction == PathModifierFunction.Generate) {

					if (!prevAndNextDirKnown) {
						dir = PathUtil.GetPathDirectionAtPoint (points, i, false, Vector3.zero, out prevDir, out nextDir);
						prevAndNextDirKnown = true;
					}

					// TODO rename UpVectorAlgorithms
					// TODO remove "Bank" algorithm as standalone algorithm; maybe 
					// we could have another PM like "CurvesPathModifier" that supports Camber
					// (yes, refactor term "Bank" to "Camber")
					if (upVectorAlgorithm == UpVectorAlgorithm.Constant) {
						up = normalizedConstantUpVector;
					} else if (upVectorAlgorithm == UpVectorAlgorithm.PlaneNormal) {
						//Vector3 cross = Vector3.Cross(-prevDir, nextDir).normalized;
						//Vector3 planeX = Quaternion.AngleAxis(90, Vector3.up) * nextDir;

						// TODO remove this:
						// Strip / Road / surface with fixed face target orientation:
						up = Vector3.up;

						// Tube with consistent face orientation (allows loop without twisting):
						Vector3 cross = Vector3.Cross (-prevDir, nextDir);
						up = cross.normalized;

						//Vector3 rotateAroundVector = GetRotateAroundVector (generateUpVectorPlane);
                       
						//up = Quaternion.AngleAxis (-90, rotateAroundVector) * dir;


					} else if (upVectorAlgorithm == UpVectorAlgorithm.Bank) {
						// TODO we need to have an option to override the angle and 
						// calculate it based on generateUpVectorPlane

						// Need angles
						if (!angleKnown) {
							angle = CalculateAngle (points, i, prevDir, nextDir);
							angleKnown = true;
						}
						float bankAngle = angle * (bankFactor * bankFactorMultiplier);

						bankAngle = Mathf.Clamp (bankAngle, -maxBankAngle, maxBankAngle);

						currentBankAngle = (i == 0) ? bankAngle : Mathf.LerpAngle (currentBankAngle, bankAngle, (1.0f - bankSmoothingFactor));


						// The unbanked vector first:
						up = GetUpVector (generateUpVectorPlane);

						//up = rotateAroundVector;//Vector3.up;
						//Quaternion.AngleAxis(-90, rotateAroundVector) * dir;

						//up = (Quaternion.AngleAxis(currentBankAngle, dir) * up).normalized;

					} else {
						Debug.LogWarning ("Unknown / unsupported upVectorAlgorithm: " + upVectorAlgorithm);
						up = Vector3.up;
					}

					points [i].Up = up;

				} 

				bool distPrevKnown = false;

				switch (DistanceFromPreviousFunction) {
				case PathModifierFunction.Generate:
					if (i == 0) {
						distPrev = 0f;
					} else {
						distPrev = DistanceFromPrevious (points, i);
					}
					points [i].DistanceFromPrevious = distPrev;
					distPrevKnown = true;
					break;
				case PathModifierFunction.Passthrough:
					distPrev = points [i].DistanceFromPrevious;
					distPrevKnown = PathPoint.IsDistanceFromPrevious (context.InputFlags) && points [i].HasDistanceFromPrevious;
					break;
				}

				switch (DistanceFromBeginFunction) {
				case PathModifierFunction.Generate:
					if (!distPrevKnown) {
						distPrev = DistanceFromPrevious (points, i);
						distPrevKnown = true;
					}
					distBegin += distPrev;
					points [i].DistanceFromBegin = distBegin;
					break;
				}


				points [i].Flags = ppFlags;
			}
			return points;

		}

		private static float DistanceFromPrevious (PathPoint[] points, int index)
		{
			float distPrev;
			if (index == 0) {
				distPrev = 0f;
			} else {
				distPrev = (points [index].Position - points [index - 1].Position).magnitude;
			}
			return distPrev;
		}

		// TODO what is this used for?
		static Vector3 GetRotateAroundVector (CoordinatePlane plane)
		{
			Vector3 rotateAroundVector;
			switch (plane) {
                
			case CoordinatePlane.XY:
				rotateAroundVector = Vector3.right; // Z
				break;
			case CoordinatePlane.XZ:
				rotateAroundVector = Vector3.up; // Y
				break;
			case CoordinatePlane.YZ:
				rotateAroundVector = Vector3.forward; // X
				break;
			case CoordinatePlane.XYZ:
			default:
                    // TODO what to do here, should we have user-defined Vector3?
				rotateAroundVector = Vector3.up;
				break;
			}
			return rotateAroundVector;
		}

		static Vector3 GetUpVector (CoordinatePlane plane)
		{
			Vector3 v;
			switch (plane) {
                
			case CoordinatePlane.XY:
				v = Vector3.forward;
				break;
			case CoordinatePlane.XZ:
				v = Vector3.up; // X
				break;
			case CoordinatePlane.YZ:
				v = Vector3.right; // Y
				break;
			case CoordinatePlane.XYZ:
			default:
                    // TODO what to do here, should we have user-defined Vector3?
				v = Vector3.up;
				break;
			}
			return v;
		}

//        private delegate float GetVectorAxisFn(Vector3 v);
//        private static float GetVectorX(Vector3 v)
//        {
//            return v.x;
//        }
//        private static float GetVectorY(Vector3 v)
//        {
//            return v.y;
//        }
//        private static float GetVectorZ(Vector3 v)
//        {
//            return v.z;
//        }
//        private static float GetZero(Vector3 v)
//        {
//            return 0.0f;
//        }
		float CalculateAngle (PathPoint[] points, int index, Vector3 prevDir, Vector3 nextDir)
		{
			int angleDirAxis = -1;
			switch (generateAnglePlane) {
			case CoordinatePlane.XY:
				prevDir = new Vector3 (prevDir.x, prevDir.y, 0f).normalized;
				nextDir = new Vector3 (nextDir.x, nextDir.y, 0f).normalized;
				angleDirAxis = 2; // Z
				break;
			case CoordinatePlane.XZ:
				prevDir = new Vector3 (prevDir.x, 0f, prevDir.z).normalized;
				nextDir = new Vector3 (nextDir.x, 0f, nextDir.z).normalized;
				angleDirAxis = 1; // Y
				break;
			case CoordinatePlane.YZ:
				prevDir = new Vector3 (0f, prevDir.y, prevDir.z).normalized;
				nextDir = new Vector3 (0f, nextDir.y, nextDir.z).normalized;
				angleDirAxis = 0; // X
				break;
			case CoordinatePlane.XYZ:
			default:
                    // Keep all axis
				angleDirAxis = -1;
				break;

			}
			float angle = Vector3.Angle (prevDir, nextDir);
			Vector3 cross = Vector3.Cross (prevDir, nextDir);
			if (angleDirAxis >= 0 && cross [angleDirAxis] < 0.0f) {
				angle = -angle;
			}
			return angle;
		}

		protected override void OnSerializeCustom (Serializer store)
		{
			store.EnumProperty ("generateAnglePlane", ref generateAnglePlane);
			store.EnumProperty ("generateUpVectorPlane", ref generateUpVectorPlane);
			store.EnumProperty ("upVectorAlgorithm", ref upVectorAlgorithm);
			store.Property ("bankFactor", ref bankFactor);
			store.Property ("bankFactorMultiplier", ref bankFactorMultiplier);
			store.Property ("maxBankAngle", ref maxBankAngle);
			store.Property ("bankSmoothingFactor", ref bankSmoothingFactor);
			store.Property ("constantUpVector", ref constantUpVector);
		}

	}


}
