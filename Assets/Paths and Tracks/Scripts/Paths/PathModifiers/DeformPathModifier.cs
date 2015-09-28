// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Paths
{


	[CodeQuality.Experimental]
	[PathModifier(requiredInputFlags=PathPoint.NONE, 
                  processCaps=PathPoint.POSITION,
                  passthroughCaps=PathPoint.ALL, 
                  generateCaps=PathPoint.NONE)]
	public class DeformPathModifier : ConfigurableProcessPathModifier
	{
//        public static readonly int ORDER_INDEX_POSITION = 0;
//        public static readonly int ORDER_INDEX_ROTATE = 1;
//        public static readonly int ORDER_INDEX_SCALE = 2;

		[Serializable]
		public class DisplacementCurve
		{
			[SerializeField]
			public List<Keyframe>
				keyframes = new List<Keyframe> ();

			[SerializeField]
			public Range
				editorValueRange = new Range (-10f, 10f);

			[SerializeField]
			public Range
				editorTRange = new Range (0f, 1f);

			[SerializeField]
			public bool
				keepEditorValueRangeSymmetricalAroundZero = true;

			[SerializeField]
			public bool
				enabled = false;

			public void Serialize (Serializer ser)
			{
				ParameterStore store = ser.ParameterStore;
				if (ser.Saving) {
					store.SetBool ("enabled", enabled);
					int keyframeCount = null != keyframes ? keyframes.Count : 0;
					store.SetInt ("keyframes.Count", keyframeCount);
					for (int i = 0; i < keyframeCount; i++) {
						store = store.ChildWithPrefix ("keyframes[" + i + "]");
						
						Keyframe kf = keyframes [i];
						store.SetFloat ("time", kf.time);
						store.SetFloat ("value", kf.value);
						store.SetFloat ("inTangent", kf.inTangent);
						store.SetFloat ("outTangent", kf.outTangent);
						store.SetInt ("tangentMode", kf.tangentMode);
					}
					store.SetRange ("editorValueRange", editorValueRange);
					store.SetRange ("editorTRange", editorTRange);
					store.SetBool ("keepEditorValueRangeSymmetricalAroundZero", keepEditorValueRangeSymmetricalAroundZero);
				} else {
					enabled = store.GetBool ("enabled", enabled);
					int keyframeCount = store.GetInt ("keyframes.Count", 0);
					keyframes = new List<Keyframe> ();
					for (int i = 0; i < keyframeCount; i++) {
						store = store.ChildWithPrefix ("keyframes[" + i + "]");
						Keyframe kf = new Keyframe ();
						kf.time = store.GetFloat ("time", kf.time);
						kf.value = store.GetFloat ("value", kf.value);
						kf.inTangent = store.GetFloat ("inTangent", kf.inTangent);
						kf.outTangent = store.GetFloat ("outTangent", kf.outTangent);
						kf.tangentMode = store.GetInt ("tangentMode", kf.tangentMode);
						keyframes.Add (kf);
					}
					editorValueRange = store.GetRange ("editorValueRange", editorValueRange);
					editorTRange = store.GetRange ("editorTRange", editorTRange);


					keepEditorValueRangeSymmetricalAroundZero = store.GetBool (
						"limits.keepEditorValueRangeSymmetricalAroundZero", keepEditorValueRangeSymmetricalAroundZero);
				}
			}
		}

		public bool linearDisplaceEnabled = false;
		public Vector3 linearDisplaceStart; 
		public Vector3 linearDisplaceEnd;

		public bool rotateEnabled = false;
		public Vector3 rotateStart;
		public Vector3 rotateEnd;

		public bool scaleEnabled = false;
		public Vector3 scaleStart;
		public Vector3 scaleEnd;

		public int translateOrder;
		public int rotateOrder;
		public int scaleOrder;

		public DisplacementCurve[] displacementCurves = new DisplacementCurve[3]; // 0 = x, 1 = y, 2 = z

//		private IndexRangePathModifierInputFilter f = new IndexRangePathModifierInputFilter ();
//		public int FirstIndex {
//			set {
//				GetInputFilter ().FirstPointIndex = value;
//			}
//			get {
//				return f.FirstPointIndex;
//			}
//		}
//		public int LastIndex {
//			set {
//				f.LastPointIndex = value;
//			}
//			get {
//				return f.LastPointIndex;
//			}
//		}

		protected override void GetAllowedFunctions (out int positionMask, out int directionMask, 
                                                     out int distanceFromPreviousMask, out int distanceFromBeginMask,
                                                     out int upVectorMask, out int angleMask)
		{
			positionMask = MaskProcess | MaskPassthrough;
			directionMask = MaskNone;
			distanceFromBeginMask = distanceFromPreviousMask = MaskNone;//MaskGenerate | MaskRemove;
			upVectorMask = MaskNone;
			angleMask = MaskNone;//MaskGenerate | MaskRemove;
            
		}

		protected override void OnResetConfiguration ()
		{
			linearDisplaceEnabled = false;
			linearDisplaceStart = Vector3.zero;
			linearDisplaceEnd = Vector3.zero;

			rotateEnabled = false;
			rotateStart = Vector3.zero;
			rotateEnd = Vector3.zero;

			scaleEnabled = false;
			scaleStart = Vector3.one;
			scaleEnd = Vector3.one;

			translateOrder = 3;
			rotateOrder = 2;
			scaleOrder = 1;
            
			for (int i = 0; i < 3; i++) {
				displacementCurves [i] = new DisplacementCurve ();
			}


			SetInputFilter (new IndexRangePathModifierInputFilter ());
		}

		protected override void OnSerializeCustom (Serializer ser)
		{
			ser.Property ("linearDisplaceEnabled", ref linearDisplaceEnabled);
			ser.Property ("translateStart", ref linearDisplaceStart);
			ser.Property ("translateEnd", ref linearDisplaceEnd);
			ser.Property ("rotateEnabled", ref rotateEnabled);
			ser.Property ("rotateStart", ref rotateStart);
			ser.Property ("rotateEnd", ref rotateEnd);
			ser.Property ("scaleEnabled", ref scaleEnabled);
			ser.Property ("scaleStart", ref scaleStart);
			ser.Property ("scaleEnd", ref scaleEnd);
			ser.Property ("translateOrder", ref translateOrder);
			ser.Property ("rotateOrder", ref rotateOrder);
			ser.Property ("scaleOrder", ref scaleOrder);

			for (int i = 0; i < 3; i++) {
				Serializer ser2 = ser.ChildWithPrefix ("displacementCurves[" + i + "]");
				displacementCurves [i].Serialize (ser2);
			}

		}

		protected override PathPoint[] DoGetModifiedPoints (PathPoint[] points)
		{

//			int ppFlags = GetOutputFlags (context);
			// Linear shift
			// Scaling

//			bool scaleBeforeTranslate = false;
           
//			float len = points.Length;
			if (PositionFunction == PathModifierFunction.Process) {

				Quaternion rotStart = Quaternion.Euler (rotateStart);
				Quaternion rotEnd = Quaternion.Euler (rotateEnd);

				// Construct AnimationCurves from stored displacement curves
				AnimationCurve[] displCurves = new AnimationCurve[3];
				for (int axis = 0; axis < 3; axis++) {
					displCurves [axis] = new AnimationCurve ();
					if (displacementCurves [axis].enabled) {
						List<Keyframe> keyframes = this.displacementCurves [axis].keyframes;
						for (int j = 0; j < keyframes.Count; j++) {
							displCurves [axis].AddKey (keyframes [j]);
						}
					}
				}

				for (int i = 0; i < points.Length; i++) {
					float t = points.Length > 1 ? ((float)i / ((float)points.Length - 1)) : 0.0f;
					Vector3 posOffs;
					if (linearDisplaceEnabled) {
						posOffs = Vector3.Lerp (linearDisplaceStart, linearDisplaceEnd, t);
					} else {
						posOffs = Vector3.zero;
					}
					Vector3 scaling = scaleEnabled ? Vector3.Lerp (scaleStart, scaleEnd, t) : Vector3.one; 
					Quaternion rot = rotateEnabled ? Quaternion.Lerp (rotStart, rotEnd, t) : Quaternion.identity;

					Vector3 pos = points [i].Position;

					for (int j = 1; j <= 3; j++) {
						if (scaleEnabled && j == scaleOrder) {
							pos.Scale (scaling);
						}
						if (rotateEnabled && j == rotateOrder) {
							pos = rot * pos;
						}
						if (j == translateOrder) {

							if (linearDisplaceEnabled) {
								pos += posOffs;
							}
							// displacement curves
							for (int axis = 0; axis < 3; axis++) {
								if (displacementCurves [axis].enabled) {
									float axisDpValue = displCurves [axis].Evaluate (t);
									pos [axis] = pos [axis] + axisDpValue;
								}
							}

						}
					}



					points [i].Position = pos;
				}
			}
			return points;
		}
	}

	// TODO currently we can only generate a straight line
	// TODO should we have other algorithms as well?
    
}
