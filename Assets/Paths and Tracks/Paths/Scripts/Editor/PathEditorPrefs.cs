using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

using Util;

namespace Paths
{
  
    public class PathEditorPrefs
    {
        public static readonly Color ControlPointConnectionLineColor = Color.gray;
        public static readonly Color SelectedControlPointConnectionLineColor = Color.white;
        public static readonly Color ControlPointHandleColor = Color.gray;
        public static readonly Color SelectedControlPointHandleColor = Color.white;

        public static readonly float ControlPointHandleSize = 0.05f;
        public static readonly float FirstControlPointHandleSize = 0.10f;
        public static readonly float ControlPointPickSize = 0.075f;
        public static readonly float FirstControlPointPickSize = 0.20f;

        public static readonly Color UpVectorColor = PathGizmoPrefs.UpVectorColor;
        public static readonly Color DirVectorColor = PathGizmoPrefs.DirVectorColor;
        public static readonly Color RightVectorColor = PathGizmoPrefs.RightVectorColor;
        
        public static readonly float UpVectorLength = PathGizmoPrefs.UpVectorLength;
        public static readonly float DirVectorLength = PathGizmoPrefs.DirVectorLength;
        public static readonly float RightVectorLength = PathGizmoPrefs.RightVectorLength;
    }


    public class BezierPathEditorPrefs : PathEditorPrefs
    {

        public static readonly Color AlignedJointHandleColor = Color.white;
        public static readonly Color MirroredJointHandleColor = Color.yellow;
        public static readonly Color FreeJointHandleColor = Color.red;

    }

    public enum OLD_PathEditorPref
    {
        CPConnectionLineColor, // Control Point Connection Line Color
        SelectedCPConnectionLineColor, // Control Point Connection Line Color
        FinalPathLineColor,
        PointMarkerColor,

        DirVectorColor,
        UpVectorColor,
        OrthoVectorColor,

        JointHandleColor,
        SelectedJointHandleColor,

        CPHandleSize,
        FirstCPHandleSize,
        CPPickSize,
        FirstCPPickSize,

        DirVectorLength,
        UpVectorLength,
        OrthoVectorLength, // i.e. the "right" vector


        // TODO move following under Bezier path:
        AlignedJointHandleColor,
        MirroredJointHandleColor,
        FreeJointHandleColor,


    }

    internal class OLD_DefaultPathEditorPrefs : OLD_PathEditorPrefs
    {
        internal OLD_DefaultPathEditorPrefs()
        {
            this [OLD_PathEditorPref.CPConnectionLineColor] = Color.gray;
            this [OLD_PathEditorPref.SelectedCPConnectionLineColor] = Color.white;
            this [OLD_PathEditorPref.FinalPathLineColor] = Color.cyan;
            this [OLD_PathEditorPref.PointMarkerColor] = Color.cyan;

            this [OLD_PathEditorPref.DirVectorColor] = Color.blue;
            this [OLD_PathEditorPref.UpVectorColor] = Color.green;
            this [OLD_PathEditorPref.OrthoVectorColor] = Color.red;

            this [OLD_PathEditorPref.JointHandleColor] = Color.gray;
            this [OLD_PathEditorPref.SelectedJointHandleColor] = Color.white;

            this [OLD_PathEditorPref.CPHandleSize] = 0.05f;
            this [OLD_PathEditorPref.FirstCPHandleSize] = 0.10f;
            this [OLD_PathEditorPref.CPPickSize] = 0.075f;
            this [OLD_PathEditorPref.FirstCPPickSize] = 0.15f;

            this [OLD_PathEditorPref.DirVectorLength] = 1.0f;
            this [OLD_PathEditorPref.UpVectorLength] = 1.0f;
            this [OLD_PathEditorPref.OrthoVectorLength] = 1.0f;



            // TODO move following under Bezier path:
            this [OLD_PathEditorPref.AlignedJointHandleColor] = Color.white;
            this [OLD_PathEditorPref.MirroredJointHandleColor] = Color.yellow;
            this [OLD_PathEditorPref.FreeJointHandleColor] = Color.red;


        }
    }

    // NOTE: this is not part of the Editor package because we need to access preferences
    // while drawing Gizmos!
    public class OLD_PathEditorPrefs
    {
        public static readonly OLD_PathEditorPrefs Defaults = new OLD_DefaultPathEditorPrefs();

        private OLD_PathEditorPrefs fallbackPrefs;
        private Dictionary<string, string> store = new Dictionary<string, string>();

        private Dictionary<string, object> cache = new Dictionary<string, object>();

        public OLD_PathEditorPrefs()
            : this(null)
        {
        }

        public OLD_PathEditorPrefs(OLD_PathEditorPrefs fallbackPrefs)
        {
            this.fallbackPrefs = fallbackPrefs;

        }

        private static string ToString(object o)
        {
            if (o == null)
            {
                return null;
            } else
            {
                if (o is Color)
                {
                    return ((Color)o).ToHexStringRGBA();
                } else
                {
                    return o.ToString();
                }
            }
        }
       


        public class PrefValue
        {
            internal object cachedValue;
            internal string rawValue;
            internal bool valid;
            internal PrefValue(string rawValue, object cachedValue)
            {
                this.rawValue = rawValue;
                this.cachedValue = cachedValue;
                this.valid = true;
            }
            internal PrefValue(string rawValue) : this(rawValue, null)
            {
            }

            public static implicit operator PrefValue(string v)
            {
                return new PrefValue(v, v);
            }
            public static implicit operator PrefValue(int v)
            {
                return new PrefValue(OLD_PathEditorPrefs.ToString(v), v);
            }
            public static implicit operator PrefValue(float v)
            {
                return new PrefValue(OLD_PathEditorPrefs.ToString(v), v);
            }
            public static implicit operator PrefValue(Color v)
            {
                return new PrefValue(OLD_PathEditorPrefs.ToString(v), v);
            }
            public static implicit operator PrefValue(Vector3 v)
            {
                return new PrefValue(OLD_PathEditorPrefs.ToString(v), v);
            }

            public static implicit operator string(PrefValue pv)
            {
                if (null != pv.rawValue)
                {
                    return pv.rawValue.ToString();
                } else if (null != pv.cachedValue)
                {
                    return pv.cachedValue.ToString();
                } else
                {
                    return null;
                }
            }

            public static implicit operator bool(PrefValue pv)
            {
                if (null != pv.cachedValue)
                {
                    return (bool)pv.cachedValue;
                } else
                {
                    bool result;
                    pv.valid = bool.TryParse(pv.rawValue, out result);
                    if (!pv.valid)
                    {
                        // TODO should we throw an exception?
                        result = false;
                        pv.cachedValue = null;
                    } else
                    {
                        pv.cachedValue = result;
                    }
                    return result;
                }
            }

            public static implicit operator int(PrefValue pv)
            {
                if (null != pv.cachedValue)
                {
                    return (int)pv.cachedValue;
                } else
                {
                    int result;
                    pv.valid = int.TryParse(pv.rawValue, out result);
                    if (!pv.valid)
                    {
                        // TODO should we throw an exception?
                        result = 0;
                        pv.cachedValue = null;
                    } else
                    {
                        pv.cachedValue = result;
                    }
                    return result;
                }
            }

            public static implicit operator float(PrefValue pv)
            {
                if (null != pv.cachedValue)
                {
                    return (float)pv.cachedValue;
                } else
                {
                    float result;
                    pv.valid = float.TryParse(pv.rawValue, out result);
                    if (!pv.valid)
                    {
                        // TODO should we throw an exception?
                        result = 0f;
                        pv.cachedValue = null;
                    } else
                    {
                        pv.cachedValue = result;
                    }
                    return result;
                }
            }
            public static implicit operator Color(PrefValue pv)
            {
                if (null != pv.cachedValue)
                {
                    return (Color)pv.cachedValue;
                } else
                {
                    Color result;
                    pv.valid = Color.TryParseHexString(pv.rawValue, out result);
                    if (!pv.valid)
                    {
                        // TODO should we throw an exception?
                        result = default(Color);
                    }
                    pv.cachedValue = result;
                    return result;
                }
            }
            public static implicit operator Vector3(PrefValue pv)
            {
//                Vector3 result;
//                pv.valid = int.TryParse(pv.rawValue, out result);
//                if (!pv.valid)
//                {
//                    // TODO should we throw an exception?
//                    result = default(Vector3);
//                }
//                return result;
                throw new NotSupportedException("Parsing of Vector3 is not yet supported");
            }

        }

        public PrefValue this [string key]
        {
            get
            {
                object cached;
                if (cache.ContainsKey(key))
                {
                    cached = cache [key];
                } else
                {
                    cached = null;
                }
                return new PrefValue(null == cached ? GetValue(key, "") : null, cached);
            }
            set
            {
                SetValue(key, value.cachedValue);
            }
        }

        public PrefValue this [OLD_PathEditorPref key]
        {
            get
            {
                return this [key.ToString()];
            }
            set
            {
                this [key.ToString()] = value;
            }
        }

        internal static bool TryParse<T>(string s, out T result)
        {
            bool parseResult;
            Type t = typeof(T);
            if (typeof(bool).IsAssignableFrom(t))
            {
                PrefValue pv = new PrefValue(s);
                result = (T)(object)(bool)pv;
                parseResult = pv.valid;
            } else if (typeof(int).IsAssignableFrom(t))
            {
                PrefValue pv = new PrefValue(s);
                result = (T)(object)(int)pv;
                parseResult = pv.valid;
            } else if (typeof(float).IsAssignableFrom(t))
            {
                PrefValue pv = new PrefValue(s);
                result = (T)(object)(float)pv;
                parseResult = pv.valid;
            } else if (typeof(Color).IsAssignableFrom(t))
            {
                PrefValue pv = new PrefValue(s);
                result = (T)(object)(Color)pv;
                parseResult = pv.valid;
            } else
            {
                throw new NotSupportedException("Don't know how to parse " + t + " from a string: " + s);
            }
            return parseResult;
        }
       

        // TODO we should have caches per type to eliminate repeating calls to Parse
        public T GetValue<T>(string key, T defaultValue = default(T))
        {

            T val;
            if (cache.ContainsKey(key))
            {
                val = (T)cache [key];
            } else if (store.ContainsKey(key))
            {
                string s = store [key];
//                Type type = typeof(T);
                if (defaultValue is string)
                {
                    val = (T)(object)s;
                } else if (!TryParse(s, out val))
                {
                    // TODO should we throw an exception?
                    val = defaultValue;
                }

            } else if (null != fallbackPrefs)
            {
                // Look up defaults
                val = fallbackPrefs.GetValue(key, defaultValue);
            } else
            {
                // No such key
                val = defaultValue;
            }
            cache [key] = val;
            return val;

        }
        public void SetValue<T>(string key, T value)
        {
            store [key] = ToString(value);
            cache [key] = value;
        }


        public T GetValue<T>(OLD_PathEditorPref key, T defaultValue = default(T))
        {
            return GetValue(key.ToString(), defaultValue);
        }

        public void SetValue<T>(OLD_PathEditorPref key, T value)
        {
            this.SetValue(key.ToString(), value);
        }
    }

    // TODO we don't really need the IPath interface since we're always referring to Path
    // (which is a GameObject)

}
