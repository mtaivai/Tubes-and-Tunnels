using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Paths
{
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class PathModifier : System.Attribute
	{
		public string displayName;

		/// <summary>
		/// Flags for required flags on input PathPoints.
		/// </summary>
		public int requiredInputFlags = PathPoint.NONE;
		public int processCaps = PathPoint.NONE;

		/// <summary>
		/// Specifies the mask for output properties; properties of 
		/// input PathPoints will be included in the output masked
		/// by this mask.
		/// </summary>
		public int passthroughCaps = PathPoint.NONE;

		/// <summary>
		/// Specifies properties that this modifier can generate
		/// for output PathPoints even if they are missing from
		/// input PathPoints.
		/// </summary>
		public int generateCaps = PathPoint.NONE;


		public PathModifier ()
		{
		}
	}

	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ProducesContextParams : System.Attribute
	{
		private string[] contextParams;
		public ProducesContextParams (params string[] contextParams)
		{
			this.contextParams = new string[contextParams.Length];
			for (int i = 0; i < contextParams.Length; i++) {
				this.contextParams [i] = contextParams [i];
			}
		}
		public string[] ContextParams {
			get {
				string[] arr = new string[contextParams.Length];
				for (int i = 0; i < arr.Length; i++) {
					arr [i] = contextParams [i];
				}
				return arr;
			}
		}
	}


	public enum PathModifierLifecyclePhase
	{
		//Create,
		Attach,
		Detach,
		Destroy
	}

	public class PathModifierLifecycleEventArgs : EventArgs
	{
		private PathModifierLifecyclePhase phase;
		private IPathModifier pathModifier;
		private IPathModifierContainer container;
		public PathModifierLifecycleEventArgs (IPathModifierContainer container, IPathModifier pm, PathModifierLifecyclePhase phase)
		{
			this.container = container;
			this.pathModifier = pm;
			this.phase = phase;
		}
		public PathModifierLifecyclePhase Phase {
			get {
				return this.phase;
			}
		}

		public IPathModifier PathModifier {
			get {
				return this.pathModifier;
			}
		}

		public IPathModifierContainer Container {
			get {
				return this.container;
			}
		}
	}

	public interface IPathModifierLifecycleAware
	{
		void HandleLifecycleEvent (PathModifierLifecycleEventArgs e);
	}

	public interface IPathModifier
	{
		bool IsEnabled ();

		void SetEnabled (bool value);
		//void SetEnabled(bool enabled);
//
//		void Attach (IPathModifierContainer container);
//		
//		void Detach ();
//
		void Reset ();
//
//		void Destroy();


		void LoadParameters (ParameterStore store);

		void SaveParameters (ParameterStore store);

		PathPoint[] GetModifiedPoints (PathPoint[] points, PathModifierContext context);

		int GetRequiredInputFlags ();

		int GetProcessFlags (PathModifierContext context);

		int GetPassthroughFlags (PathModifierContext context);

		int GetGenerateFlags (PathModifierContext context);

		int GetOutputFlags (PathModifierContext context);

		//void DrawInspectorGUI(TrackInspector trackInspector);

		Path[] GetPathDependencies ();

		string GetName ();

		string GetDescription ();

		string GetInstanceName ();

		void SetInstanceName (string name);

		string GetInstanceDescription ();

		void SetInstanceDescription (string description);

		string[] GetProducedContextParameters ();

	}






	public enum PathModifierMessageType
	{
		Info,
		Warning,
		Error
	}


	public abstract class PathModifierInputFilter
	{

		public delegate PathPoint[] ProcessFilteredPointsDelegate (PathPoint[] points,PathModifierContext context);
		
		public abstract PathPoint[] Filter (PathPoint[] points, PathModifierContext context, ProcessFilteredPointsDelegate processCallback);

		public abstract void Serialize (Serializer ser);


//		public abstract PathPoint[] Filter (PathPoint[] points, PathModifierContext context);
	}
	public class IndexRangePathModifierInputFilter : PathModifierInputFilter
	{
		private int firstPointIndex = 0;
		private int pointCount = -1;

		public int FirstPointIndex {
			get {
				return firstPointIndex;
			}
			set {
				firstPointIndex = value;
				if (firstPointIndex < 0) {
					firstPointIndex = 0;
				}
			}
		}
		public int PointCount {
			get {
				return pointCount;
			}
			set {
				pointCount = value;
				if (pointCount < -1) {
					pointCount = -1;
				}
			}
		}
		public override void Serialize (Serializer ser)
		{
//			base.Serialize (ser);
			ser.Property ("firstPointIndex", ref firstPointIndex);
			ser.Property ("pointCount", ref pointCount);

			SerializeConfigParam (firstPointIndexParam, ser);

		}

		protected void SerializeConfigParam (ConfigParam cp, Serializer ser)
		{
			ser.Property (cp.Name + ".fromContext", ref cp.fromContext);
			ser.Property (cp.Name + ".contextParamName", ref cp.contextParamName);
			ser.EnumProperty (cp.Name + ".contextParamOperator", ref cp.paramOperator);
			ser.Property (cp.Name + ".contextParamOperand", ref cp.paramOperand);
			ser.Property (cp.Name + ".value", ref cp.value);
		}

		public class ConfigParam
		{
			public enum ParamOperator : int
			{
				None = 0,
				Plus = 1,
				Minus = 2,
			}
			private string name;
			private int defaultValue;

			public bool fromContext;
			public string contextParamName;
			public int value;
			public ParamOperator paramOperator = ParamOperator.None;
			public int paramOperand;

			public ConfigParam (string name, int defaultValue)
			{
				this.name = name;
				this.value = this.defaultValue = defaultValue;
			}
			public string Name {
				get {
					return name;
				}
			}
			public int DefaultValue {
				get {
					return defaultValue;
				}
			}

			private static int GetInt (Dictionary<string, object> dict, string key, int defaultValue)
			{
				return dict.ContainsKey (key) ? TypeUtil.ToInt (dict [key], defaultValue) : defaultValue;
			}

			public int GetInt (PathModifierContext context)
			{
				if (fromContext) {
					int v = contextParamName != null ? GetInt (context.Parameters, contextParamName, defaultValue) : defaultValue;
					switch (paramOperator) {
					case ParamOperator.Plus:
						v += paramOperand;
						break;
					case ParamOperator.Minus:
						v -= paramOperand;
						break;

					}
					return v;
				} else {
					return value;
				}
			}
		}
		public ConfigParam firstPointIndexParam = new ConfigParam ("firstPointIndex", 0);
		public ConfigParam pointCountParam = new ConfigParam ("pointCount", -1);


		public override PathPoint[] Filter (PathPoint[] points, PathModifierContext context, ProcessFilteredPointsDelegate processCallback)
		{
			PathPoint[] results;
			if (points.Length == 0) {
				results = new PathPoint[0];
			} else {
				int adjustedFirstPointIndex;
				int adjustedPointCount;


				this.firstPointIndex = firstPointIndexParam.GetInt (context);
				this.pointCount = pointCountParam.GetInt (context);
				if (pointCount < 0) {
					// Negative value == include all
					adjustedPointCount = points.Length;
				} else {
					adjustedPointCount = Mathf.Clamp (pointCount, 0, points.Length);
					// TODO should we update the configuration setting here:
					//				this.pointCount = adjustedPointCount;
				}

				adjustedFirstPointIndex = Mathf.Clamp (firstPointIndex, 0, points.Length - 1);

				int filteredCount = Mathf.Clamp (adjustedPointCount, 0, points.Length - adjustedFirstPointIndex);

				// Now run the PathModifier with filtered points:

				PathPoint[] pmInputPoints = new PathPoint[filteredCount];
				for (int i = 0; i < filteredCount; i++) {
					pmInputPoints [i] = points [i + adjustedFirstPointIndex];
				}

				PathPoint[] pmResults = processCallback (pmInputPoints, context);

				int resultsCount = points.Length - filteredCount + pmResults.Length;

				// Combine results to input:
				results = new PathPoint[resultsCount];

				// First copy original points before filtering (the head):
				int headSize = adjustedFirstPointIndex;
				for (int i = 0; i < headSize; i++) {
					results [i] = points [i];
				}
				// Then copy modified points:
				for (int i = 0; i < pmResults.Length; i++) {
					results [i + headSize] = pmResults [i];
				}
				// And finally the tail:
				int tailSize = points.Length - pmResults.Length - headSize;
				int tailOffset = adjustedFirstPointIndex + pmResults.Length;
				for (int i = 0; i < tailSize; i++) {
					results [i + tailOffset] = points [i + headSize + pmResults.Length];
				}
			}

			return results;
		}
	}

	public interface IPathModifierInputFilterSupport
	{

		PathModifierInputFilter GetInputFilter ();
		void SetInputFilter (PathModifierInputFilter f);
	}


}
