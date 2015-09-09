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



	[PathModifier(requiredInputFlags=PathPoint.NONE, 
                  processCaps=PathPoint.POSITION,
                  passthroughCaps=PathPoint.ALL, 
                  generateCaps=PathPoint.NONE)]
	public class DeformPathModifier : ConfigurableProcessPathModifier
	{
//        public static readonly int ORDER_INDEX_POSITION = 0;
//        public static readonly int ORDER_INDEX_ROTATE = 1;
//        public static readonly int ORDER_INDEX_SCALE = 2;

		public Vector3 translateStart;
		public Vector3 translateEnd;
		public Vector3 rotateStart;
		public Vector3 rotateEnd;
		public Vector3 scaleStart;
		public Vector3 scaleEnd;
		public int translateOrder;
		public int rotateOrder;
		public int scaleOrder;


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
			positionMask = MaskProcess;
			directionMask = MaskNone;
			distanceFromBeginMask = distanceFromPreviousMask = MaskNone;//MaskGenerate | MaskRemove;
			upVectorMask = MaskNone;
			angleMask = MaskNone;//MaskGenerate | MaskRemove;
            
		}

		protected override void OnResetConfiguration ()
		{
			translateStart = Vector3.zero;
			translateEnd = Vector3.zero;
			rotateStart = Vector3.zero;
			rotateEnd = Vector3.zero;
			scaleStart = Vector3.one;
			scaleEnd = Vector3.one;

			translateOrder = 3;
			rotateOrder = 2;
			scaleOrder = 1;
            
			SetInputFilter (new IndexRangePathModifierInputFilter ());
		}

		protected override void OnSerializeCustom (Serializer ser)
		{
			ser.Property ("translateStart", ref translateStart);
			ser.Property ("translateEnd", ref translateEnd);
			ser.Property ("rotateStart", ref rotateStart);
			ser.Property ("rotateEnd", ref rotateEnd);
			ser.Property ("scaleStart", ref scaleStart);
			ser.Property ("scaleEnd", ref scaleEnd);
			ser.Property ("translateOrder", ref translateOrder);
			ser.Property ("rotateOrder", ref rotateOrder);
			ser.Property ("scaleOrder", ref scaleOrder);


		}


		protected override PathPoint[] DoGetModifiedPoints (PathPoint[] points)
		{

//			int ppFlags = GetOutputFlags (context);
			// Linear shift
			// Scaling

//			bool scaleBeforeTranslate = false;
           
//			float len = points.Length;

			Quaternion rotStart = Quaternion.Euler (rotateStart);
			Quaternion rotEnd = Quaternion.Euler (rotateEnd);



			for (int i = 0; i < points.Length; i++) {
				float t = points.Length > 1 ? ((float)i / ((float)points.Length - 1)) : 0.0f;
				Vector3 posOffs = Vector3.Lerp (translateStart, translateEnd, t);
				Vector3 scaling = Vector3.Lerp (scaleStart, scaleEnd, t); 
				Quaternion rot = Quaternion.Lerp (rotStart, rotEnd, t);

				Vector3 pos = points [i].Position;

				for (int j = 1; j <= 3; j++) {
					if (j == scaleOrder) {
						pos.Scale (scaling);
					}
					if (j == rotateOrder) {
						pos = rot * pos;
					}
					if (j == translateOrder) {
						pos += posOffs;
					}
				}

				points [i].Position = pos;
			}
			return points;
		}
	}

	// TODO currently we can only generate a straight line
	// TODO should we have other algorithms as well?
    
}
