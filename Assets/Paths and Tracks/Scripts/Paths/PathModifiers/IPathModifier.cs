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

	public interface IPathModifier
	{
		bool IsEnabled ();

		void SetEnabled (bool value);
		//void SetEnabled(bool enabled);

		void Attach (IPathModifierContainer container);
		
		void Detach ();

		void Reset ();

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



	[Serializable]
	public class PathDataSnapshot
	{
		[SerializeField]
		private string
			name;

		[SerializeField]
		private PathPoint[]
			points;

		[SerializeField]
		private int
			flags;

		public PathDataSnapshot (string name, PathPoint[] points, int flags)
		{
			if (null == name) {
				throw new ArgumentException ("Mandatory argument 'name' is not specified (is null)");
			}
			if (null == points) {
				throw new ArgumentException ("Mandatory argument 'points' is not specified (is null)");
			}
			this.name = name;
			this.points = points;
			this.flags = flags;
		}
		/// <summary>
		/// Construct a deep clone.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="points">Points.</param>
		/// <param name="flags">Flags.</param>
		public PathDataSnapshot (PathDataSnapshot src)
		{
			this.name = src.name;
			this.points = new PathPoint[src.points.Length];
			for (int i = 0; i < this.points.Length; i++) {
				this.points [i] = new PathPoint (src.points [i]);
			}
			this.flags = src.flags;
		}
		public string Name {
			get {
				return name;
			}
		}
		public PathPoint[] Points {
			get {
				return points;
			}
		}
		public int Flags {
			get {
				return flags;
			}
		}
	}


	public enum PathModifierMessageType
	{
		Info,
		Warning,
		Error
	}

	public interface IPathModifierContainer
	{

		IPathModifier[] GetPathModifiers ();

		void AddPathModifer (IPathModifier pm);

		void InsertPathModifer (int index, IPathModifier pm);

		void RemovePathModifer (int index);

		int IndexOf (IPathModifier pm);

		bool IsSupportsApplyPathModifier ();

		void ApplyPathModifier (int index);

		IReferenceContainer GetReferenceContainer ();

		IPathSnapshotManager GetPathSnapshotManager ();

		// TODO is this required?
		ParameterStore GetParameterStore ();

		//void SaveConfiguration ();
		void ConfigurationChanged ();

		PathPoint[] xxxRunPathModifiers (PathModifierContext context, PathPoint[] pp, ref int flags);

//		bool HasErrors ();
//		string[] GetCurrentErrors ();
//
		bool HasMessages (PathModifierMessageType messageType);
		bool HasMessages (PathModifierMessageType messageType, IPathModifier pm);
		string[] GetCurrentMessages (PathModifierMessageType messageType, IPathModifier pm);
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

	public abstract class AbstractPathModifier : IPathModifier, IPathModifierInputFilterSupport
	{


		internal bool enabled = true;
		private bool _inLoadParameters = false;
		private bool _inSaveParameters = false;
		protected int inputCaps;
		protected int processCaps;
		protected int passthroughCaps;
		protected int generateCaps;
		protected string instanceName;
		protected string instanceDescription;
		private string name;
		private IPathModifierContainer container;

		private PathModifierInputFilter inputFilter;

		private List<string> initialProducedContextParameters = new List<string> ();
		private List<string> producedContextParameters = new List<string> ();

		private PathModifierContext _context;

		public AbstractPathModifier ()
		{
			this.name = GetDisplayName (GetType ());
			Reset ();
		}

		protected PathModifierContext context {
			get {
				return this._context;
			}
			private set {
				this._context = value;
			}
		}

		private bool _onReset;

		public void Reset ()
		{
			if (!_onReset) {
				_onReset = true;
				try {
					enabled = true;
					PathModifierUtil.GetPathModifierCapsFromAttributes (GetType (), out inputCaps, out processCaps, out passthroughCaps, out generateCaps);
					instanceName = "";
					instanceDescription = "";
					inputFilter = null;
					// Don't reset the container!
					//container = null;

					ResolveInitialProducedContextParameters ();
					this.producedContextParameters = new List<string> (initialProducedContextParameters);

					OnReset ();
				} finally {
					_onReset = false;
				}
			}

		}

		protected virtual void OnReset ()
		{

		}

		private void ResolveInitialProducedContextParameters ()
		{
			initialProducedContextParameters = new List<string> ();
			// TODO read from Annotation
			object[] attrs = GetType ().GetCustomAttributes (typeof(ProducesContextParams), true);
			foreach (object attrObj in attrs) {
				ProducesContextParams pcp = attrObj as ProducesContextParams;
				if (null != pcp) {
					initialProducedContextParameters.AddRange (pcp.ContextParams);
				}
			}
		}


		private bool _onAttach;

		public void Attach (IPathModifierContainer container)
		{
			if (!_onAttach) {
				this.container = container;
				try {
					_onAttach = true;
					OnAttach ();
				} finally {
					_onAttach = false;
				}
			} else {
				Debug.LogWarning ("Attach() called from OnAttach() - ignoring");
			}
		}

		private bool _onDetach;

		public void Detach ()
		{
			if (!_onDetach) {

				try {
					_onDetach = true;
					OnDetach ();
				} finally {
					_onDetach = false;
					this.container = null;
				}
			} else {
				Debug.LogWarning ("Detach() called from OnDetach() - ignoring");
			}
		}

		public virtual void OnAttach ()
		{
		}

		public virtual void OnDetach ()
		{
		}

		protected IPathModifierContainer GetContainer ()
		{
			return container;
		}

		public static string GetDisplayName (Type toolType)
		{
			string name = CustomTool.GetToolName (toolType);
			if (StringUtil.IsEmpty (name)) {
				name = GetFallbackDisplayName (toolType);
			}
			return name;
		}

		private static string GetFallbackDisplayName (Type toolType)
		{
			return StringUtil.RemoveStringTail (StringUtil.RemoveStringTail (toolType.Name, "Modifier", 1), "Path", 1);
		}
        
		public virtual bool IsEnabled ()
		{
			return enabled;
		}

		public virtual void SetEnabled (bool value)
		{
			this.enabled = value;
		}

		public virtual int GetRequiredInputFlags ()
		{
			return inputCaps;
		}

		public virtual int GetProcessFlags (PathModifierContext context)
		{
			return processCaps & context.InputFlags;
		}

		public virtual int GetPassthroughFlags (PathModifierContext context)
		{
			return (passthroughCaps & context.InputFlags) & ~GetProcessFlags (context);
		}

		public virtual int GetGenerateFlags (PathModifierContext context)
		{
			return generateCaps;
		}

		public int GetOutputFlags (PathModifierContext context)
		{
			return (context.InputFlags & (GetProcessFlags (context) | GetPassthroughFlags (context))) | GetGenerateFlags (context);
		}

		public PathModifierInputFilter GetInputFilter ()
		{
			return inputFilter;
		}
		public void SetInputFilter (PathModifierInputFilter f)
		{
			this.inputFilter = f;
		}

		public void LoadParameters (ParameterStore store)
		{
			// TODO should we serialize inputCaps, outputCaps etc.?
			if (!_inLoadParameters) {

				try {
					_inLoadParameters = true;
					OnLoadParameters (store);
				} finally {
					_inLoadParameters = false;
				}
			} else {
				Debug.LogWarning ("LoadParameters() called from OnLoadParameters() - ignoring");
			}
            
		}

		public void OnLoadParameters (ParameterStore store)
		{
			Serializer ser = new Serializer (store, false);
			Serialize (ser);
		}

		public void SaveParameters (ParameterStore store)
		{
			// TODO should we serialize inputCaps, outputCaps etc.?
			if (!_inSaveParameters) {

				try {
					_inSaveParameters = true;
					OnSaveParameters (store);
				} finally {
					_inSaveParameters = false;
				}
			} else {
				Debug.LogWarning ("SaveParameters() called from OnSaveParameters() - ignoring");
			}
		}
		
		public void OnSaveParameters (ParameterStore store)
		{
			Serializer ser = new Serializer (store, true);
			Serialize (ser);
		}



		private void Serialize (Serializer ser)
		{
			ser.Property ("enabled", ref enabled);
			ser.Property ("instanceName", ref instanceName);
			ser.Property ("instanceDescription", ref instanceDescription);
			SerializeInputFilters (ser.WithPrefix ("inputFilters"));
			OnSerialize (ser);
		}

		public virtual void OnSerialize (Serializer ser)
		{
		}

		private void SerializeInputFilters (Serializer ser)
		{
			int count;
			if (ser.Saving) {
				// Saving

				// Clean up unused configuration
				foreach (string inputFilterParam in ser.ParameterStore.FindParametersStartingWith("")) {
					ser.ParameterStore.RemoveParameter (inputFilterParam);
				}

				if (null != inputFilter) {
					count = 1;
				} else {
					count = 0;
				}
				ser.ParameterStore.SetInt ("Count", count);

			} else {
				// Loading
				count = ser.ParameterStore.GetInt ("Count", -1);
				if (count == 0) {
					this.inputFilter = null;
				}
			}
			if (count > 0) {
				inputFilter = SerializeInputFilter (inputFilter, ser.WithPrefix ("Items[0]"));
			}
		}
		private PathModifierInputFilter SerializeInputFilter (PathModifierInputFilter f, Serializer ser)
		{
			if (ser.Saving) {
				string type = f.GetType ().FullName;
				ser.ParameterStore.SetString ("Type", type);
			} else {
				// LOADING
				string type = ser.ParameterStore.GetString ("Type", null);
//				if (type == null) {
//					throw new Exception ("Failed to read 'Type' of PathModifierInputFilter");
//				}
				if (null == f || (null != type && f.GetType ().FullName != type)) {
					f = (PathModifierInputFilter)Activator.CreateInstance (Type.GetType (type));
				}
			}
			f.Serialize (ser);
			return f;
		}


		private bool _inGetModifiedPoints;

		public PathPoint[] GetModifiedPoints (PathPoint[] points, PathModifierContext context)
		{
			if (!_inGetModifiedPoints) {
				_inGetModifiedPoints = true;
				try {
					producedContextParameters.Clear ();
					PathModifierInputFilter f = GetInputFilter ();
					if (null != f) {
						// Run filtered:
						points = f.Filter (points, context, DoGetModifiedPoints);
					} else {
						// Unfiltered:
						points = DoGetModifiedPoints (points, context);
					}
				} finally {

					_inGetModifiedPoints = false;
				}
			} else {
				throw new Exception ("GetModifiedPoints called from DoGetModifiedPoints. Ignoring.");
			}
			return points;
		}

		private PathPoint[] DoGetModifiedPoints (PathPoint[] points, PathModifierContext context)
		{
			try {
				this.context = context;
				return DoGetModifiedPoints (points);
			} finally {
				this.context = null;
			}
		}

		protected abstract PathPoint[] DoGetModifiedPoints (PathPoint[] points);

		public virtual Path[] GetPathDependencies ()
		{
			return new Path[0];
		}

		public string GetName ()
		{
			return name;
		}

		public virtual string GetDescription ()
		{
			return "";
		}

		public string GetInstanceName ()
		{
			return instanceName;
		}

		public void SetInstanceName (string name)
		{
			this.instanceName = name;
		}

		public string GetInstanceDescription ()
		{
			return instanceDescription;
		}

		public void SetInstanceDescription (string description)
		{
			this.instanceDescription = description;
		}


		public string[] GetProducedContextParameters ()
		{
			return producedContextParameters.ToArray ();
		}

		protected void AddContextParameter (string name, object value)
		{
			if (context.Parameters.ContainsKey (name)) {
				context.Parameters [name] = value;
			} else {
				context.Parameters.Add (name, value);
			}
			if (!producedContextParameters.Contains (name)) {
				producedContextParameters.Add (name);
			}
		}
	}

}
