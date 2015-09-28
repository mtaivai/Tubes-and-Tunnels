// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Util.Editor
{

	public class ContexteEditorState
	{
		// TODO implement this
	}

	// TODO EditorState and EditorPrefs should be separate concepts I.E. DO NOT USE THIS CLASS!
	// TODO move ContextEditorPrefs to its own file
	// TODO ContextEditorPrefs should keep track of plugin editor prefs and
	// be able to delete them on request!
	public class ContextEditorPrefs
	{
		private string contextPrefix;
		private bool addDotAfterPrefix;
		public ContextEditorPrefs (string prefix)
		{
			// Remove trailing '.' if any given
			while (prefix.EndsWith(".")) {
				prefix = prefix.Substring (0, prefix.Length - 1);
			}
			this.contextPrefix = prefix;
			int prefixLength = prefix.Length;
			if (prefixLength > 0) {
				char lastChar = contextPrefix [prefixLength - 1];
				addDotAfterPrefix = true;//(lastChar != ']' && lastChar != ')');
			} else {
				addDotAfterPrefix = false;
			}
		}

		public string Prefix {
			get {
				return contextPrefix;
			}
		}

		public ContextEditorPrefs WithPrefix (string prefix)
		{
			return new ContextEditorPrefs (PrefixKey (prefix, false));
		}

		private string PrefixKey (string key, bool record)
		{
			string prefixKey;
			int prefixLength = contextPrefix.Length;

			if (prefixLength == 0) {
				prefixKey = key;
			} else if (addDotAfterPrefix && key.Length > 0 && key [0] != '.') {
				prefixKey = string.Format ("{0}.{1}", contextPrefix, key);
			} else {
				prefixKey = contextPrefix + key;
			}
			if (record) {
				// TODO Record the key!
//				Debug.LogFormat ("ContextEditorPrefs '{0}' has key '{1}' ==> '{2}'", contextPrefix, key, prefixKey);
			}
			return prefixKey;
		}

		public bool HasKey (string key)
		{
			key = PrefixKey (key, false);
			return UnityEditor.EditorPrefs.HasKey (key);
		}
		public void DeleteKey (string key)
		{
			key = PrefixKey (key, false);
			UnityEditor.EditorPrefs.DeleteKey (key);
		}

		public string GetString (string key, string defaultValue = "")
		{
			key = PrefixKey (key, true);
			return UnityEditor.EditorPrefs.GetString (key, defaultValue);
		}
		public void SetString (string key, string value)
		{
			key = PrefixKey (key, true);
			UnityEditor.EditorPrefs.SetString (key, value);
		}
		public bool GetBool (string key, bool defaultValue = false)
		{
			key = PrefixKey (key, true);
			return UnityEditor.EditorPrefs.GetBool (key, defaultValue);
		}
		public void SetBool (string key, bool value)
		{
			key = PrefixKey (key, true);
			UnityEditor.EditorPrefs.SetBool (key, value);
		}
		public int GetInt (string key, int defaultValue = 0)
		{
			key = PrefixKey (key, true);
			return UnityEditor.EditorPrefs.GetInt (key, defaultValue);
		}
		public void SetInt (string key, int value)
		{
			key = PrefixKey (key, true);
			UnityEditor.EditorPrefs.SetInt (key, value);
		}
		public float GetFloat (string key, float defaultValue = 0f)
		{
			key = PrefixKey (key, true);
			return UnityEditor.EditorPrefs.GetFloat (key, defaultValue);
		}
		public void SetFloat (string key, float value)
		{
			key = PrefixKey (key, true);
			UnityEditor.EditorPrefs.SetFloat (key, value);
		}
	}
	
}
