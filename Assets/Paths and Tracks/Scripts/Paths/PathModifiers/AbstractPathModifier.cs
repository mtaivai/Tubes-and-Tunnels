// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Paths
{

	public abstract class AbstractPathModifier : IPathModifier, IPathModifierInputFilterSupport, IPathModifierLifecycleAware
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

//		public void HandlePluginLifecycleEvent (PluginLifecyleEventArgs e)
//		{
//			switch (e.
//		}

		private bool _inReset;

		public void Reset ()
		{
			if (!_inReset) {
				_inReset = true;
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
					_inReset = false;
				}
			} else {
				Debug.LogWarning ("AbstractPathModifier.Reset called back from OnReset - ignoring");
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

		private bool _inHandleLifecycleEvent;
		public void HandleLifecycleEvent (PathModifierLifecycleEventArgs e)
		{
			if (!_inHandleLifecycleEvent) {
				_inHandleLifecycleEvent = true;
				try {
					OnHandleLifecycleEvent (e);
				} finally {
					_inHandleLifecycleEvent = false;
				}
			} else {
				Debug.LogWarning ("AbstractPathModifier.HandleLifecycleEvent called back from delegate event handler - ignoring");
			}
		}

		protected void OnHandleLifecycleEvent (PathModifierLifecycleEventArgs e)
		{
			switch (e.Phase) {
//			case PathModifierLifecyclePhase.Create:
//				HandleCreateEvent (e);
//				break;
			case PathModifierLifecyclePhase.Attach:
				HandleAttachEvent (e);
				break;
			case PathModifierLifecyclePhase.Detach:
				HandleDetachEvent (e);
				break;
			case PathModifierLifecyclePhase.Destroy:
				HandleDestroyEvent (e);
				break;
			}
		}

//		private void HandleCreateEvent (PathModifierLifecycleEventArgs e)
//		{
//			OnCreate ();
//		}
//		
//		protected virtual void OnCreate ()
//		{
//		}

		private void HandleAttachEvent (PathModifierLifecycleEventArgs e)
		{
			this.container = e.Container;
			OnAttach ();
		}

		protected virtual void OnAttach ()
		{
		}

		private void HandleDetachEvent (PathModifierLifecycleEventArgs e)
		{
			try {
				OnDetach ();
			} finally {
				this.container = null;
			}
		}

		protected virtual void OnDetach ()
		{
		}

		private void HandleDestroyEvent (PathModifierLifecycleEventArgs e)
		{
			try {
				OnDestroy ();
			} finally {
				try {
					PostDestroy ();
				} catch (Exception ex) {
					Debug.LogError ("Catched an exception: " + ex);
					// Swallow the exception
				}
				this.container = null;
			}
		}
		protected virtual void OnDestroy ()
		{
		}

#if UNITY_EDITOR 
		private void PostDestroy ()
		{
			if (Application.isEditor) {
				PluginUtil.DeletePluginEditorPrefs (this);
			}
		}
#else
		private void PostDestroy () 
		{
			// NOP, really
		}
#endif


		protected IPathModifierContainer GetContainer ()
		{
			return container;
		}

		public static string GetDisplayName (Type toolType)
		{
			string name = Plugin.GetPluginName (toolType);
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
			SerializeInputFilters (ser.ChildWithPrefix ("inputFilters"));
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
				inputFilter = SerializeInputFilter (inputFilter, ser.ChildWithPrefix ("Items[0]"));
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
