// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using System;
using System.Collections.Generic;

namespace Util
{

	[System.Serializable]
	public class Parameter
	{
		[SerializeField]
		private string
			name;
		[SerializeField]
		private string
			value;
		
		public string Name {
			get {
				return name;
			}
			set {
				this.name = value;
			}
		}
		
		public string Value {
			get {
				return value;
			}
			set {
				this.value = value;
			}
		}
		
		public Parameter ()
		{
		}
		
		public Parameter (string name) : this()
		{
			this.name = name;
		}
		
		public Parameter (string name, string value) : this(name)
		{
			this.value = value;
		}
		
	}
	public abstract class ParameterStoreBase : ISerializationCallbackReceiver
	{
		private const string VALUE_ENTRY_PREFIX = "values.";
		private const string ARRAY_ENTRY_PREFIX = "arrays.";

		[NonSerialized]
		private ParameterStoreBase
			parent;

		[NonSerialized]
		private String
			prefix = "";

		[SerializeField]
		private List<Parameter>
			parameterList;
		
		[NonSerialized]
		private Dictionary<string, string>
			_values;

		[NonSerialized]
		private Dictionary<string, string[]>
			_arrays;
		
		//[Serializable]
		//private int modificationCount = 0;

		[NonSerialized]
		private int
			_modificationCount = 0;
		
		//[SerializeField]
		//private List<ArrayParameter> arrays = new List<ArrayParameter> ();
		
		protected ParameterStoreBase ()
		{
			prefix = "";
			parameterList = new List<Parameter> ();
			_values = new Dictionary<string, string> ();
			_arrays = new Dictionary<string, string[]> ();
		}
		
		protected ParameterStoreBase (ParameterStoreBase parent, string prefix)
		{
			//this.parameterList = parent.parameterList;
			this.parent = parent;
			this.prefix = prefix;
			this._values = null;
			this._arrays = null;
		}
		
		public string Prefix {
			get {
				return prefix;
			}
		}
		
		public void OnBeforeSerialize ()
		{
			if (null != parent) {
				throw new NotSupportedException ("Child ParameterStores can't be serialized");
			} else {
				
				if (_modificationCount > 0) {
					
					//Debug.Log("ParameterStore.OnBeforeSerialize");
					
					// Convert dictionaries to list
					this.parameterList.Clear ();
					foreach (KeyValuePair<string, string> kvp in _values) {
						Parameter p = new Parameter (VALUE_ENTRY_PREFIX + kvp.Key, kvp.Value);
						parameterList.Add (p);
					}
					
					// Arrays:
					foreach (KeyValuePair<string, string[]> kvp in _arrays) {
						string n = ARRAY_ENTRY_PREFIX + kvp.Key;
						string[] arr = kvp.Value;
						parameterList.Add (new Parameter (n + ".Length", arr.Length.ToString ()));
						
						for (int i = 0; i < arr.Length; i++) {
							parameterList.Add (new Parameter (n + "[" + i + "]", arr [i]));
							
						}
					}
					parameterList.Sort ((x, y) => x.Name.CompareTo (y.Name));

					_modificationCount = 0;
				}
			}
		}
		
		public void OnAfterDeserialize ()
		{
			//Debug.Log("ParameterStore.OnAfterDeserialize");
			if (null != parent) {
				throw new NotSupportedException ("Child ParameterStores can't be serialized");
			} else {
				// Populate dictionaries from the list
				this._values.Clear ();
				this._arrays.Clear ();
				
				// First pass populates an arrays building dictionary
				// and the second pass creates actual arrays
				Dictionary<string, Dictionary<int, string>> arrayDict = 
					new Dictionary<string, Dictionary<int, string>> ();
				
				foreach (Parameter p in parameterList) {
					string n = p.Name;
					if (n.StartsWith (VALUE_ENTRY_PREFIX)) {
						n = n.Substring (VALUE_ENTRY_PREFIX.Length);
						_values [n] = p.Value;
					} else if (n.StartsWith (ARRAY_ENTRY_PREFIX)) {
						n = n.Substring (ARRAY_ENTRY_PREFIX.Length);
						if (n.EndsWith (".Length")) {
							// array length
						} else {
							int indexStartPos = n.LastIndexOf ('[');
							if (indexStartPos > 0) {
								int indexEndPos = n.IndexOf (']', indexStartPos);
								if (indexEndPos > indexStartPos + 1) {
									int index = int.Parse (n.Substring (indexStartPos + 1, indexEndPos - indexStartPos - 1));
									n = n.Substring (0, indexStartPos);
									if (!arrayDict.ContainsKey (n)) {
										arrayDict.Add (n, new Dictionary<int, string> ());
									}
									arrayDict [n] [index] = p.Value;
								}
							}
						}
					}
				}
				foreach (KeyValuePair<string, Dictionary<int, string>> kvp in arrayDict) {
					string n = kvp.Key;
					int len = kvp.Value.Count;
					string[] arr = new string[len];
					foreach (KeyValuePair<int, string> kvp2 in kvp.Value) {
						arr [kvp2.Key] = kvp2.Value;
					}
					_arrays [n] = arr;
				}
			}
		}
		
		private void Modified ()
		{
			_modificationCount++;
			if (null != parent) {
				parent.Modified ();
			}
		}
		
		private string AddNamePrefix (string name)
		{
			
			if (prefix.Length > 0) {
				name = prefix + "." + name;
			}
//			if (null != parent) {
//				name = parent.AddNamePrefix (name);
//			}
			return name;
		}
		
		// TODO refactor this to GetValue()???
		protected string DoGetParameterValue (string name, string defaultValue = null)
		{
			name = AddNamePrefix (name);
			if (null != parent) {
				return parent.DoGetParameterValue (name, defaultValue);
			} else if (_values.ContainsKey (name)) {
				return _values [name];
			} else {
				return defaultValue;
			}
		}
		
		protected void DoSetParameterValue (string name, string value)
		{
			// Find existing and replace
			name = AddNamePrefix (name);
			if (null != parent) {
				parent.DoSetParameterValue (name, value);
			} else if (null != value) {
				if (_values.ContainsKey (name)) {
					string prevValue = _values [name];
					if (prevValue != value) {
						_values [name] = value;
						Modified ();
					}
				} else {
					_values [name] = value;
					Modified ();
					//Debug.Log("New value added");
				}
			} else if (_values.ContainsKey (name)) {
				_values.Remove (name);
				Modified ();
			}
			
		}
		
		protected string[] DoGetArrayParameterValue (string name, string[] defaultValue = null)
		{
			name = AddNamePrefix (name);
			if (null != parent) {
				return parent.DoGetArrayParameterValue (name, defaultValue);
			} else if (_arrays.ContainsKey (name)) {
				return _arrays [name];
			} else {
				return defaultValue;
			}
			
		}
		
		protected void DoSetArrayParameterValue (string name, string[] value)
		{
			// Find existing and replace
			name = AddNamePrefix (name);
			if (null != parent) {
				parent.DoSetArrayParameterValue (name, value);
			} else {
				bool changed = false;
				if (null != value) {
					// Compare arrays!
					if (_arrays.ContainsKey (name)) {
						// Existing value
						string[] prevValue = _arrays [name];
						if (prevValue.Length != value.Length) {
							// Length doesn't match
							changed = true;
						} else {
							// Check if contents differ
							changed = false;
							for (int i = 0; i < prevValue.Length; i++) {
								if (prevValue [i] != value [i]) {
									changed = true;
									break;
								}
							}
						}
						if (changed) {
							_arrays [name] = value;
						}
					} else {
						// New value
						_arrays [name] = value;
						changed = true;
					}
				} else if (_values.ContainsKey (name)) {
					// Null value, remove
					_arrays.Remove (name);
					changed = true;
				}
				if (changed) {
					Modified ();
				}
			}
		}
		public bool ContainsParameter (string name)
		{
			name = AddNamePrefix (name);
			if (null != parent) {
				return parent.ContainsParameter (name);
			} else {
				return _values.ContainsKey (name) || _arrays.ContainsKey (name);
			}
		}
		
		public void RemoveParameter (string name)
		{
			if (null != parent) {
				parent.RemoveParameter (name);
			} else {
				DoSetParameterValue (name, null);
				DoSetArrayParameterValue (name, null);
			}
		}
		
		public String[] FindParametersStartingWith (string namePrefix)
		{
			namePrefix = AddNamePrefix (namePrefix);
			
			if (null != parent) {
				return parent.FindParametersStartingWith (namePrefix);
			} else {
				List<string> results = new List<string> ();
				foreach (string k in _values.Keys) {
					if (k.StartsWith (namePrefix)) {
						results.Add (k);
					}
				}
				foreach (string k in _arrays.Keys) {
					if (k.StartsWith (namePrefix)) {
						results.Add (k);
					}
				}
				
				// Remove prefix from results:
				string[] arr = results.ToArray ();
				string removePrefix = AddNamePrefix ("");
				for (int i = 0; i < arr.Length; i++) {
					if (arr [i].StartsWith (removePrefix)) {
						arr [i] = arr [i].Substring (removePrefix.Length);
					}
				}
				
				return arr;
			}
		}
	}
	
}
