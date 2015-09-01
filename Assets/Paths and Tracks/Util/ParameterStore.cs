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

	[System.Serializable]
	public class ParameterStore
	{
		private const string VALUE_ENTRY_PREFIX = "values.";
		private const string ARRAY_ENTRY_PREFIX = "arrays.";
		private String prefix = "";
		private ParameterStore parent;
		[SerializeField]
		private List<Parameter>
			parameterList = new List<Parameter> ();
		private Dictionary<string, string> values = new Dictionary<string, string> ();
		private Dictionary<string, string[]> arrays = new Dictionary<string, string[]> ();

		//[Serializable]
		//private int modificationCount = 0;

		private int _modificationCount = 0;

		//[SerializeField]
		//private List<ArrayParameter> arrays = new List<ArrayParameter> ();


		public ParameterStore ()
		{
			prefix = "";
		}

		public ParameterStore (ParameterStore parent, string prefix)
		{
			//this.parameterList = parent.parameterList;
			this.parent = parent;
			this.prefix = prefix;
			this.values = parent.values;
			this.arrays = parent.arrays;
		}


		public string Prefix {
			get {
				return prefix;
			}
		}

		public ParameterStore WithPrefix (string prefix)
		{
			return new ParameterStore (this, prefix);
		}

		public void OnBeforeSerialize ()
		{
			if (null != parent) {
				throw new NotSupportedException ("Child ParameterStores can't be serialized");
			}

			if (_modificationCount > 0) {
            
				//Debug.Log("ParameterStore.OnBeforeSerialize");

				// Convert dictionaries to list
				this.parameterList.Clear ();
				foreach (KeyValuePair<string, string> kvp in values) {
					Parameter p = new Parameter (VALUE_ENTRY_PREFIX + kvp.Key, kvp.Value);
					parameterList.Add (p);
				}

				// Arrays:
				foreach (KeyValuePair<string, string[]> kvp in arrays) {
					string n = ARRAY_ENTRY_PREFIX + kvp.Key;
					string[] arr = kvp.Value;
					parameterList.Add (new Parameter (n + ".Length", arr.Length.ToString ()));

					for (int i = 0; i < arr.Length; i++) {
						parameterList.Add (new Parameter (n + "[" + i + "]", arr [i]));

					}
				}
				_modificationCount = 0;
			}
		}
        
		public void OnAfterDeserialize ()
		{
			//Debug.Log("ParameterStore.OnAfterDeserialize");
			if (null != parent) {
				throw new NotSupportedException ("Child ParameterStores can't be serialized");
			}
			// Populate dictionaries from the list
			this.values.Clear ();
			this.arrays.Clear ();

			// First pass populates an arrays building dictionary
			// and the second pass creates actual arrays
			Dictionary<string, Dictionary<int, string>> arrayDict = 
                new Dictionary<string, Dictionary<int, string>> ();

			foreach (Parameter p in parameterList) {
				string n = p.Name;
				if (n.StartsWith (VALUE_ENTRY_PREFIX)) {
					n = n.Substring (VALUE_ENTRY_PREFIX.Length);
					values [n] = p.Value;
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
				arrays [n] = arr;
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
			if (null != parent) {
				name = parent.AddNamePrefix (name);
			}
			return name;
		}

		// TODO refactor this to GetValue()???
		protected string DoGetParameterValue (string name, string defaultValue = null)
		{
			name = AddNamePrefix (name);

			if (values.ContainsKey (name)) {
				return values [name];
			} else {
				return defaultValue;
			}
		}

		protected void DoSetParameterValue (string name, string value)
		{
			// Find existing and replace
			name = AddNamePrefix (name);

			if (null != value) {
				if (values.ContainsKey (name)) {
					string prevValue = values [name];
					if (prevValue != value) {
						values [name] = value;
						Modified ();
					}
				} else {
					values [name] = value;
					Modified ();
					//Debug.Log("New value added");
				}
			} else if (values.ContainsKey (name)) {
				values.Remove (name);
				Modified ();
			}

		}

		protected string[] DoGetArrayParameterValue (string name, string[] defaultValue = null)
		{
			name = AddNamePrefix (name);

			if (arrays.ContainsKey (name)) {
				return arrays [name];
			} else {
				return defaultValue;
			}

		}

		protected void DoSetArrayParameterValue (string name, string[] value)
		{
			// Find existing and replace
			name = AddNamePrefix (name);

			bool changed = false;
			if (null != value) {
				// Compare arrays!
				if (arrays.ContainsKey (name)) {
					// Existing value
					string[] prevValue = arrays [name];
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
						arrays [name] = value;
					}
				} else {
					// New value
					arrays [name] = value;
					changed = true;
				}
			} else if (values.ContainsKey (name)) {
				// Null value, remove
				arrays.Remove (name);
				changed = true;
			}
			if (changed) {
				Modified ();
			}
		}
		public bool ContainsParameter (string name)
		{
			name = AddNamePrefix (name);
			return values.ContainsKey (name) || arrays.ContainsKey (name);
		}

		public void RemoveParameter (string name)
		{
			SetString (name, null);
			SetStringArray (name, null);
		}

		public String[] FindParametersStartingWith (string namePrefix)
		{
			namePrefix = AddNamePrefix (namePrefix);

			List<string> results = new List<string> ();
			foreach (string k in values.Keys) {
				if (k.StartsWith (namePrefix)) {
					results.Add (k);
				}
			}
			foreach (string k in arrays.Keys) {
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


		public string GetString (string name, string defaultValue = null)
		{
			return DoGetParameterValue (name, defaultValue);
		}

		public void SetString (string name, string value)
		{
			DoSetParameterValue (name, value);
		}

		public int GetInt (string name, int defaultValue = 0)
		{
			string val = GetString (name);
			return (null == val) ? defaultValue : int.Parse (val);
		}

		public void SetInt (string name, int value)
		{
			SetString (name, value.ToString ());
		}

		public bool GetBool (string name, bool defaultValue = false)
		{
			string val = GetString (name);
			return (null == val) ? defaultValue : bool.Parse (val);
		}

		public void SetBool (string name, bool value)
		{
			SetString (name, value.ToString ());
		}

		private static float ParseFloat (string s, float defaultValue = 0.0f)
		{
			if (null == s) {
				return defaultValue;
			} else {
				s = s.Trim ();
				try {
					return (s.Length > 0) ? float.Parse (s) : defaultValue;
				} catch (FormatException e) {
					Debug.LogWarning ("Failed to Parse float: " + e.Message);
					return defaultValue;
				}
			}

		}

		public float GetFloat (string name, float defaultValue = 0.0f)
		{
			string val = GetString (name);
			return (null == val) ? defaultValue : ParseFloat (val, defaultValue);
		}

		public void SetFloat (string name, float value)
		{
			SetString (name, value.ToString ());
		}

		public T GetEnum<T> (string name, T defaultValue)
		{
			string val = GetString (name);
			if (null == val) {
				return defaultValue;
			} else {
				try {
					return (T)Enum.Parse (typeof(T), val);
				} catch (ArgumentException e) {
					Debug.LogWarning ("Failed to Parse Enum '" + typeof(T) + "' value '" + val + "': " + e.Message);
					return defaultValue;
				}
			}
		}

		public void SetEnum<T> (string name, T value)
		{
			string s = (null != value) ? value.ToString () : null;
			SetString (name, s);
		}

		private static Vector2 ParseVector2 (string s, Vector2 defaultValue)
		{
			string[] parts = s.Split (';');
			float x = (parts.Length > 0) ? ParseFloat (parts [0], defaultValue.x) : defaultValue.x;
			float y = (parts.Length > 1) ? ParseFloat (parts [1], defaultValue.y) : defaultValue.y;
			return new Vector2 (x, y);
		}

		private static string Vector2ToString (Vector2 value)
		{
			return value.x + ";" + value.y;
		}

		public Vector2 GetVector2 (string name)
		{
			return GetVector2 (name, Vector2.zero);
		}
        
		public Vector2 GetVector2 (string name, Vector2 defaultValue)
		{
			String s = DoGetParameterValue (name, null);
			if (null == s) {
				return defaultValue;
			} else {
				// Parse
				return ParseVector2 (s, defaultValue);
                
			}
		}
        
		public void SetVector2 (string name, Vector2 value)
		{
			DoSetParameterValue (name, Vector2ToString (value));
		}

		public Vector3 GetVector3 (string name)
		{
			return GetVector3 (name, Vector3.zero);
		}

		private static Vector3 ParseVector3 (string s, Vector3 defaultValue)
		{
			string[] parts = s.Split (';');
			float x = (parts.Length > 0) ? ParseFloat (parts [0], defaultValue.x) : defaultValue.x;
			float y = (parts.Length > 1) ? ParseFloat (parts [1], defaultValue.y) : defaultValue.y;
			float z = (parts.Length > 2) ? ParseFloat (parts [2], defaultValue.z) : defaultValue.z;
			return new Vector3 (x, y, z);
		}

		private static string Vector3ToString (Vector3 value)
		{
			return value.x + ";" + value.y + ";" + value.z;
		}

		public Vector3 GetVector3 (string name, Vector3 defaultValue)
		{
			String s = DoGetParameterValue (name, null);
			if (null == s) {
				return defaultValue;
			} else {
				// Parse
				return ParseVector3 (s, defaultValue);
                
			}
		}
        
		public void SetVector3 (string name, Vector3 value)
		{
			DoSetParameterValue (name, Vector3ToString (value));
		}

		public string[] GetStringArray (string name, string[] defaultValue)
		{
			return DoGetArrayParameterValue (name, defaultValue);
		}
        
		public void SetStringArray (string name, string[] value)
		{
			DoSetArrayParameterValue (name, value);
		}

		public int[] GetIntArray (string name, int[] defaultValue)
		{
			string[] strarr = GetStringArray (name, null);
			if (null == strarr) {
				return defaultValue;
			} else {
				int[] intArr;
				intArr = new int[strarr.Length];
				for (int i = 0; i < strarr.Length; i++) {
					intArr [i] = int.Parse (strarr [i]);
				} 
				return intArr;
			}
		}
        
		public void SetIntArray (string name, int[] value)
		{
			string[] strarr;
			if (value != null) {
				strarr = new string[value.Length];
				for (int i = 0; i < value.Length; i++) {
					strarr [i] = value [i].ToString ();
				}
			} else {
				strarr = null;
			}
			DoSetArrayParameterValue (name, strarr);
		}

		public float[] GetFloatArray (string name, float[] defaultValue)
		{
			string[] strarr = GetStringArray (name, null);
			if (null == strarr) {
				return defaultValue;
			} else {
				float[] arr;
				arr = new float[strarr.Length];
				for (int i = 0; i < strarr.Length; i++) {
					arr [i] = ParseFloat (strarr [i]);
				}
				return arr;
			}
		}
        
		public void SetFloatArray (string name, float[] value)
		{
			string[] strarr;
			if (value != null) {
				strarr = new string[value.Length];
				for (int i = 0; i < value.Length; i++) {
					strarr [i] = value [i].ToString ();
				}
			} else {
				strarr = null;
			}
			DoSetArrayParameterValue (name, strarr);
		}

		public Vector3[] GetVector3Array (string name, Vector3[] defaultValue = null)
		{
			string[] strvals = GetStringArray (name, null);
			if (null == strvals) {
				return defaultValue;
			} else {
				Vector3[] value = new Vector3[strvals.Length];
				for (int i = 0; i < strvals.Length; i++) {
					value [i] = ParseVector3 (strvals [i], Vector3.zero);
				}
				return value;
			}
		}
        
		public void SetVector3Array (string name, Vector3[] value)
		{
			string[] strarr = new string[value.Length];
			for (int i = 0; i < value.Length; i++) {
				strarr [i] = Vector3ToString (value [i]);
			}
			SetStringArray (name, strarr);
		}


	}

	public class Serializer
	{
		private ParameterStore store;
		private bool saving;

		public Serializer (ParameterStore store, bool saving)
		{
			this.store = store;
			this.saving = saving;
		}

		public bool Saving {
			get {
				return saving;
			}
		}
		public ParameterStore ParameterStore {
			get {
				return store;
			}
		}

		public Serializer WithPrefix (string prefix)
		{
			ParameterStore store2 = new ParameterStore (store, prefix);
			return new Serializer (store2, saving);
		}


		public void Property (string name, ref string value)
		{
			value = ReturnProperty (name, value);
		}

		public void Property (string name, ref bool value)
		{
			value = ReturnProperty (name, value);
		}

		public void Property (string name, ref int value)
		{
			value = ReturnProperty (name, value);
		}

		public void Property (string name, ref float value)
		{
			value = ReturnProperty (name, value);
		}
//        public void Property(string name, ref Enum value)
//        {
//            value = ReturnProperty(name, value);
//        }
		public void EnumProperty<T> (string name, ref T value) where T : struct, IConvertible
		{
//            if (!typeof(Enum).IsAssignableFrom(typeof(T)))
//            {
//                throw new ArgumentException("T must be an Enum type; was: " + typeof(T));
//            }
			//value = (T)(object)ReturnEnumProperty(name, (Enum)(object)value);
			value = ReturnEnumProperty (name, value);
		}

		public void Property (string name, ref Vector3 value)
		{
			value = ReturnProperty (name, value);
		}

		public string ReturnProperty (string name, string value)
		{
			if (saving) {
				store.SetString (name, value);
			} else {
				value = store.GetString (name, value);
			}
			return value;
		}

		public bool ReturnProperty (string name, bool value)
		{
			if (saving) {
				store.SetBool (name, value);
			} else {
				value = store.GetBool (name, value);
			}
			return value;
		}

		public int ReturnProperty (string name, int value)
		{
			if (saving) {
				store.SetInt (name, value);
			} else {
				value = store.GetInt (name, value);
			}
			return value;
		}

		public float ReturnProperty (string name, float value)
		{
			if (saving) {
				store.SetFloat (name, value);
			} else {
				value = store.GetFloat (name, value);
			}
			return value;
		}

		public T ReturnEnumProperty<T> (string name, T value) where T: struct, IConvertible
		{
			if (saving) {
				store.SetEnum (name, value);
			} else {
				value = store.GetEnum (name, value);
			}
			return value;
		}

		public Vector3 ReturnProperty (string name, Vector3 value)
		{
			if (saving) {
				store.SetVector3 (name, value);
			} else {
				value = store.GetVector3 (name, value);
			}
			return value;
		}
	}
}

