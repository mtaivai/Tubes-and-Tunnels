using UnityEngine;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Paths
{
	[CodeQuality.Experimental]
	[PathModifier(requiredInputFlags=PathPoint.POSITION, 
                  processCaps=PathPoint.POSITION, 
                  passthroughCaps=PathPoint.DIRECTION | PathPoint.DISTANCES,
                  generateCaps=PathPoint.DIRECTION | PathPoint.DISTANCES)]
	public class NoisePathModifier : AbstractPathModifier
	{
		// TODO change to use ConfigurableProcessPathModifier!
		public enum DirAndDistOutputType
		{
			Passthrough,
			Remove
		}

		public enum PositionOutputType
		{
			Process,
			Passthrough
		}

		public Vector3 NoiseVolume { get; set; }

		public int Seed { get; set; }

		public PositionOutputType positionOutput = PositionOutputType.Process;
		public DirAndDistOutputType directionOutput = DirAndDistOutputType.Passthrough;
		public DirAndDistOutputType distanceOutput = DirAndDistOutputType.Passthrough;

		public override int GetGenerateFlags (PathModifierContext context)
		{
			int f = 0;
			if (positionOutput == PositionOutputType.Process) {
				f |= PathPoint.POSITION;
			}
			return f;
		}
        
		public override int GetPassthroughFlags (PathModifierContext context)
		{
			int f = 0;
			if (positionOutput == PositionOutputType.Passthrough) {
				f |= PathPoint.POSITION;
			}
			if (directionOutput == DirAndDistOutputType.Passthrough) {
				f |= PathPoint.DIRECTION;
			}
			if (distanceOutput == DirAndDistOutputType.Passthrough) {
				f |= PathPoint.DISTANCE_FROM_BEGIN | PathPoint.DISTANCE_FROM_PREVIOUS;
			}
			return f;
		}

		public NoisePathModifier ()
		{
			Seed = 0;
		}
		// TODO we need to recalculate directions and distances!
		// TODO limit the amplitude between pathpoints!
		protected override PathPoint[] DoGetModifiedPoints (PathPoint[] points)
		{
			System.Random rnd = new System.Random (Seed);

			int ppFlags = GetOutputFlags (context);

			for (int i = 0; i < points.Length; i++) {

				Vector3 pos;

				if (positionOutput == PositionOutputType.Process) {
					Vector3 displacement = NoiseVolume * (float)rnd.Next (100) / 100.0f;
					pos = points [i].Position + displacement;
				} else {
					pos = points [i].Position;
				}

				points [i].Position = pos;
				points [i].Flags = ppFlags;
			} 
			return points;
		}
        
		public override void OnSerialize (Serializer store)
		{
			NoiseVolume = store.ReturnProperty ("NoiseVolume", NoiseVolume);
			Seed = store.ReturnProperty ("Seed", Seed);
			store.EnumProperty ("positionOutput", ref positionOutput);
			store.EnumProperty ("directionOutput", ref directionOutput);
			store.EnumProperty ("distanceOutput", ref distanceOutput);

		}

        
	}

}
