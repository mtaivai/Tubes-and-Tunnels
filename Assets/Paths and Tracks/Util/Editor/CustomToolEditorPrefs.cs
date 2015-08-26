using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Util.Editor
{
    public interface CustomToolEditorPrefs
    {
        bool ContainsEditorPrefKey(string key);

        string GetEditorPrefValue(string key, string defaultValue = null);

        void SetEditorPrefValue(string key, string value);

        void RemoveEditorPrefValue(string key);
    }

    public abstract class TypedCustomToolEditorPrefs : CustomToolEditorPrefs
    {
        public abstract bool ContainsEditorPrefKey(string key);
        
        public abstract string GetEditorPrefValue(string key, string defaultValue = null);
        
        public abstract void SetEditorPrefValue(string key, string value);
        
        public abstract void RemoveEditorPrefValue(string key);
        
        private string StringKey(object key)
        {
            return null != key ? key.ToString() : null;
        }
        
        public bool ContainsKey(object key)
        {
            return ContainsEditorPrefKey(StringKey(key));
        }

        public void SetString(object key, string value)
        {
            SetEditorPrefValue(StringKey(key), value);
        }

        public string GetString(object key, string defaultValue = null)
        {
            string skey = StringKey(key);
            if (ContainsEditorPrefKey(skey))
            {
                return GetEditorPrefValue(skey, defaultValue);
            } else
            {
                return defaultValue;
            }
        }
        
        public void SetBool(object key, bool value)
        {
            SetString(key, value.ToString());
        }

        public bool GetBool(object key, bool defaultValue = false)
        {
            string s = GetString(key, defaultValue.ToString());
            
            bool result;
            if (!bool.TryParse(s, out result))
            {
                result = defaultValue;
            }
            return result;
            
        }

        public void SetInt(object key, int value)
        {
            SetString(key, value.ToString());
        }

        public int GetInt(object key, int defaultValue = 0)
        {
            string s = GetString(key, defaultValue.ToString());
            
            int result;
            if (!int.TryParse(s, out result))
            {
                result = defaultValue;
            }
            return result;
            
        }
    }
    
    public class PrefixCustomToolEditorPrefs : TypedCustomToolEditorPrefs
    {
        private CustomToolEditorPrefs prefs;
        private string prefix;
        private string suffix;

        public PrefixCustomToolEditorPrefs(CustomToolEditorPrefs prefs, string prefix, string suffix = "")
        {
            this.prefs = prefs;
            this.prefix = prefix;
            this.suffix = suffix;
        }

        private string WrapKey(string key)
        {
            return prefix + key + suffix;
        }
        
        public override bool ContainsEditorPrefKey(string key)
        {
            return prefs.ContainsEditorPrefKey(WrapKey(key));
        }
        
        public override string GetEditorPrefValue(string key, string defaultValue)
        {
            return prefs.GetEditorPrefValue(WrapKey(key), defaultValue);
        }
        
        public override void SetEditorPrefValue(string key, string value)
        {
            prefs.SetEditorPrefValue(WrapKey(key), value);
        }
        
        public override void RemoveEditorPrefValue(string key)
        {
            prefs.RemoveEditorPrefValue(WrapKey(key));
        }
        
        
    }
    
    public class DictionaryCustomToolEditorPrefs : TypedCustomToolEditorPrefs
    {
        public Dictionary<string, string> dictionary;
        
        public DictionaryCustomToolEditorPrefs() : this (new Dictionary<string, string>())
        {
        }
        
        public DictionaryCustomToolEditorPrefs(Dictionary<string, string> dictionary)
        {
            this.dictionary = dictionary;
        }
        
        public override bool ContainsEditorPrefKey(string key)
        {
            return dictionary.ContainsKey(key);
        }
        
        public override string GetEditorPrefValue(string key, string defaultValue)
        {
            return dictionary.ContainsKey(key) ? dictionary [key] : defaultValue;
        }
        
        public override void SetEditorPrefValue(string key, string value)
        {
            dictionary [key] = value;
        }
        
        public override void RemoveEditorPrefValue(string key)
        {
            dictionary.Remove(key);
        }   
    }
    
    public class ParameterStoreCustomToolEditorPrefs : TypedCustomToolEditorPrefs
    {
        Util.ParameterStore store;

        public ParameterStoreCustomToolEditorPrefs(Util.ParameterStore store)
        {
            this.store = store;
        }

        public override bool ContainsEditorPrefKey(string key)
        {
            return store.HasParameter(key);
        }
        
        public override string GetEditorPrefValue(string key, string defaultValue)
        {
            return store.GetString(key, defaultValue);
        }
        
        public override void SetEditorPrefValue(string key, string value)
        {
            store.SetString(key, value);
        }
        
        public override void RemoveEditorPrefValue(string key)
        {
            store.RemoveParameter(key);
        }
    }
}
