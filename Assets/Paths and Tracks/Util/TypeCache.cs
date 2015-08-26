using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Util
{
    public class TypeCache
    {
        private Dictionary<string, object> cache = new Dictionary<string, object>();
    
        public bool ContainsKey(string key)
        {
            return cache.ContainsKey(key);
        }

        public void AddValue<T>(string key, T value)
        {
            cache.Add(key, value);
        }

        public T GetValue<T>(string key)
        {
            if (cache.ContainsKey(key))
            {
                return (T)cache [key];
            } else
            {
                return default(T);
            }
        }

    }
    
}
