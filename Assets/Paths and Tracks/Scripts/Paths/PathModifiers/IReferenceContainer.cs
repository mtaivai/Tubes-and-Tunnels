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

	public interface IReferenceContainer
	{
		bool ContainsReferentObject (string id);
		UnityEngine.Object GetReferentObject (string id);
		void SetReferentObject (string id, UnityEngine.Object obj);
		string AddReferent (UnityEngine.Object obj);
		UnityEngine.Object RemoveReferent (string id);
	}

	[Serializable]
	public class SimpleReferenceContainer : IReferenceContainer, ISerializationCallbackReceiver
	{
		[SerializeField]
		private List<UnityEngine.Object>
			referents = new List<UnityEngine.Object> ();

		[SerializeField]
		private List<string>
			ids = new List<string> ();

		[SerializeField]
		private int
			nextId = 1;

		[NonSerialized]
		private Dictionary<string, UnityEngine.Object>
			_referentMap = new Dictionary<string, UnityEngine.Object> ();


		public SimpleReferenceContainer ()
		{

		}
		public void OnBeforeSerialize ()
		{
			this.referents = new List<UnityEngine.Object> ();
			this.ids = new List<string> ();

			foreach (KeyValuePair<string, UnityEngine.Object> kvp in _referentMap) {
				string id = kvp.Key;
				UnityEngine.Object obj = kvp.Value;
				referents.Add (obj);
				ids.Add (id);
			}
		}
		
		public void OnAfterDeserialize ()
		{
			this._referentMap.Clear ();

			if (null != referents && referents.Count > 0) {
				// TODO REMOVE FOLLOWING AFTER MIGRATION PHASE:
				if (null == ids || ids.Count < 1) {
					ids = new List<string> ();
					for (int i = 0; i < referents.Count; i++) {
						ids.Add (string.Format ("{0}", i));
					}
					nextId = referents.Count;
				}
				for (int i = 0; i < referents.Count; i++) {
					UnityEngine.Object obj = referents [i];
					string id = ids [i];
					_referentMap.Add (id, obj);
				}

			}
		}
		public bool ContainsReferentObject (string id)
		{
			return _referentMap.ContainsKey (id);
		}

		public UnityEngine.Object GetReferentObject (string id)
		{
			if (_referentMap.ContainsKey (id)) {
				return _referentMap [id];
			} else {
				return null;
			}
		}

		public void SetReferentObject (string id, UnityEngine.Object obj)
		{
			_referentMap [id] = obj;
		}


		public string AddReferent (UnityEngine.Object obj)
		{
			string id = string.Format ("{0}", nextId++);
			_referentMap.Add (id, obj);
			return id;
		}

		public UnityEngine.Object RemoveReferent (string id)
		{
			UnityEngine.Object obj;
			if (_referentMap.ContainsKey (id)) {
				obj = _referentMap [id];
				_referentMap.Remove (id);
			} else {
				obj = null;
			}
			return obj;
		}





	}
}
