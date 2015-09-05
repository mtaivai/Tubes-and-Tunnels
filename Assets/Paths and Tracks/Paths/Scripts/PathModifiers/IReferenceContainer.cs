// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Paths
{

	public interface IReferenceContainer
	{
		int GetReferentCount ();

		UnityEngine.Object GetReferent (int index);

		void SetReferent (int index, UnityEngine.Object obj);

		int AddReferent (UnityEngine.Object obj);

		void RemoveReferent (int index);
	}
	[Serializable]
	public class SimpleReferenceContainer : IReferenceContainer
	{
		[SerializeField]
		private List<UnityEngine.Object>
			referents = new List<UnityEngine.Object> ();
		
		// TODO how to clean up references???
		public int GetReferentCount ()
		{
			return referents.Count;
		}
		
		public UnityEngine.Object GetReferent (int index)
		{
			return referents [index];
		}
		
		public void SetReferent (int index, UnityEngine.Object obj)
		{
			referents [index] = obj;
		}
		
		public int AddReferent (UnityEngine.Object obj)
		{
			referents.Add (obj);
			return referents.Count - 1;
		}
		
		public void RemoveReferent (int index)
		{
			referents.RemoveAt (index);
		}
	}
}
