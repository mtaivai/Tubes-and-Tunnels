using UnityEngine;
using System;
using System.Collections.Generic;

namespace Util
{



	[Serializable]
	public class ParameterStore : ParameterStoreBase
	{
		public ParameterStore () : base()
		{
		}
		
		protected ParameterStore (ParameterStore parent, string prefix) : base(parent, prefix)
		{
		}

		public ParameterStore ChildWithPrefix (string prefix)
		{
			return new ParameterStore (this, prefix);
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
			ParameterStore store2 = store.ChildWithPrefix (prefix);
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

