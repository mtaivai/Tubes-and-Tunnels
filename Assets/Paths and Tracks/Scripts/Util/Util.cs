﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Util
{
	public enum CoordinatePlane
	{
		XYZ,
		XY,
		XZ,
		YZ,
	}

	public class CoordinateUtil
	{
		public static Vector2 ToVector2 (Vector3 v3, CoordinatePlane plane)
		{
			switch (plane) {
			case CoordinatePlane.XZ:
				return new Vector2 (v3.x, v3.z);
			case CoordinatePlane.YZ:
				return new Vector2 (v3.z, v3.y);
			case CoordinatePlane.XYZ:
			case CoordinatePlane.XY:
			default:
				return new Vector2 (v3.x, v3.y);
			}
		}

		public static Vector3 ToVector3 (Vector2 v2, CoordinatePlane plane)
		{
			switch (plane) {
			case CoordinatePlane.XZ:
				return new Vector3 (v2.x, 0f, v2.y);
			case CoordinatePlane.YZ:
				return new Vector3 (0f, v2.x, v2.y);
			case CoordinatePlane.XYZ:
			case CoordinatePlane.XY:
			default:
				return new Vector3 (v2.x, v2.y, 0f);
			}
		}
	}

	public class StringUtil
	{
		public static string RemoveStringTail (string str, string tail, int minLength)
		{
			if (str.EndsWith (tail) && (str.Length - tail.Length) > minLength) {
				str = str.Substring (0, str.Length - tail.Length);
			}
			return str;
		}

		public static bool IsEmpty (string s)
		{
			return (null == s || s.Trim ().Length == 0);
		}

		public static string Trim (string s, bool nullIsEmpty = true)
		{
			return (s == null) ? (nullIsEmpty ? "" : null) : s.Trim ();

		}
	}

	public static class MiscUtil
	{

		public static string[] GetFolderAndFileName (string path)
		{

			string[] pathParts = path.Split ('/');
			string fn = (pathParts.Length > 0) ? pathParts [pathParts.Length - 1] : path;
			string folder = (pathParts.Length > 1) ? path.Substring (0, path.Length - fn.Length - 1) : "";
			return new string[] {folder, fn};
		}
        
		public static string GetFolderName (string path)
		{
			return GetFolderAndFileName (path) [0];
		}

		public static string GetFileName (string path)
		{
			return GetFolderAndFileName (path) [1];
		}
	}

	public class TypeUtil
	{

		public static T Convert<T> (object val, T defaultValue)
		{
			if (typeof(T) == typeof(int)) {
				return (T)(object)ToInt (val, (int)(object)defaultValue);
			} else {
				throw new ArgumentException ("Unsupported target type: " + typeof(T));
			}
		}

		public static int ToInt (object val, int defaultValue)
		{
			if (null != val) {
				if (val is int) {
					return (int)val;
				} else if (val is short) { 
					return (short)val;
				} else if (val is long) {
					return (int)(long)val;
				} else if (val is byte) {
					return (int)(byte)val;
				} else if (val is float) {
					return (int)(float)val;
				} else if (val is double) {
					return (int)(double)val;
				} else if (val is bool) {
					return ((bool)val) ? 1 : 0;
				} else {
					int intVal;
					if (int.TryParse (val.ToString (), out intVal)) {
						return intVal;
					} else {
						return defaultValue;
					}
				}
			} else {
				return defaultValue;
			}
		}

		public static Type[] FindImplementingTypes (Type baseType)
		{
			List<Type> foundTypes = new List<Type> ();
			System.Reflection.Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies ();
			foreach (System.Reflection.Assembly assembly in assemblies) {
				Type[] types = assembly.GetTypes ();
				foreach (Type type in types) {
					if (baseType.IsAssignableFrom (type) && !type.IsAbstract) {
						foundTypes.Add (type);
					}
				}
			}
			return foundTypes.ToArray ();
		}

		public static Type[] FindTypesHavingAttribute (Type attrType)
		{
			return FindTypesHavingAttribute (attrType, typeof(object));
		}

		public static Type[] FindTypesHavingAttribute (Type attrType, Type baseType)
		{
			if (!typeof(Attribute).IsAssignableFrom (attrType)) {
				throw new ArgumentException ("attrType is not an Attribute type: " + attrType);
			}
			if (null == baseType) {
				baseType = typeof(object);
			}
			// TODO baseType can't be null on the following!
			Type[] types = (null != baseType) ? FindImplementingTypes (baseType) : FindImplementingTypes (typeof(object));
			return DoFindTypesHavingAttribute (types, attrType);
		}

		private static Type[] DoFindTypesHavingAttribute (Type[] types, Type attrType)
		{
			List<Type> foundTypes = new List<Type> ();
			foreach (Type type in types) {
				object[] attrs = type.GetCustomAttributes (attrType, true);
				if (attrs.Length > 0) {
					foundTypes.Add (type);
				}
			}
			return foundTypes.ToArray ();
		}

		public static int HierarchyDistance (Type type, Type superType)
		{
			int dist;

			if (type.Equals (superType)) {
				dist = 0;
			} else {
				dist = -1;
				if (superType.IsInterface) {
					int shortestDist = int.MaxValue;
					Type[] intfs = type.GetInterfaces ();
					foreach (Type intf in intfs) {
						dist = HierarchyDistance (intf, superType);
						if (dist < shortestDist) {
							shortestDist = dist;
							if (shortestDist == 0) {
								break;
							}
						}
					}
					if (shortestDist >= 0 && shortestDist < int.MaxValue) {
						dist = shortestDist + 1;
					} else {
						dist = -1;
					}
				} 

				if (dist < 0) {
					Type baseType = type.BaseType;
					if (null != baseType) {
						dist = HierarchyDistance (baseType, superType);
						if (dist >= 0) {
							dist += 1;
						}
					} else {
						dist = -1;
					}
				}

			}
			return dist;
		}

	}


}

