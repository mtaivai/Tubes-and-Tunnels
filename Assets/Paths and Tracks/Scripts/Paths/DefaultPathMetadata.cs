// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

using Util;

namespace Paths
{

	public class PathMetadataChangedEventArgs : EventArgs
	{
		public PathMetadataChangedEventArgs ()
		{
			
		}
	}
	public delegate void PathMetadataChangedHandler (object sender,PathMetadataChangedEventArgs e);

	[Serializable]
	public class DefaultPathMetadata : IEditablePathMetadata, ISerializationCallbackReceiver
	{
		[Serializable]
		class InterpolatedWeightData
		{
			[SerializeField]
			public string
				weightId;

			[SerializeField]
			public PathDataScope
				scope;

			[SerializeField]
			public List<float>
				interpolatedValues;
		}

//		[NonSerialized]
//		private IPathData
//			pathData;

		[SerializeField]
		private List<WeightDefinition>
			weightDefinitions;

		[SerializeField]
		private int
			nextWeightId = 1;

		[SerializeField]
		private List<InterpolatedWeightData>
			interpolatedData = new List<InterpolatedWeightData> ();

		public event PathMetadataChangedHandler
			MetadataChanged;
//		[NonSerialized]
//		private Dictionary<string, DefinedWeight> _idToDefinedWeightMap;

		public DefaultPathMetadata ()
		{
			weightDefinitions = new List<WeightDefinition> ();
//			_idToDefinedWeightMap = new Dictionary<string, DefinedWeight>();
		}

		private void FireChangedEvent ()
		{
			if (null != MetadataChanged) {
				PathMetadataChangedEventArgs e = new PathMetadataChangedEventArgs ();
				try {
					MetadataChanged (this, e);
				} catch (Exception ex) {
					Debug.LogError ("Catched an exception from PathMetadataChangedHandler: " + ex);
				}
			}
		}


//		// TODO do we need this?
//		public void AttachToPathData (IPathData pathData)
//		{
//			if (null == pathData) {
//				throw new ArgumentException ("AttachToPathData requires valid IPathData instance; got null", "pathData");
//			} else if (this.pathData != null && this.pathData != pathData) {
//				throw new ArgumentException ("Already attached to different pathData: " + this.pathData, "pathData");
//			}
//
//			this.pathData = pathData;
//		}
//		// TODO do we need this?
//		public void DetachFromPathData (IPathData pathData)
//		{
//			if (null == pathData) {
//				throw new ArgumentException ("DetachFromPathData requires valid IPathData instance; got null", "pathData");
//			} else if (this.pathData != pathData) {
//				throw new ArgumentException ("Attached to different pathData: " + this.pathData, "pathData");
//			}
//			this.pathData = null;
//		}

		public void OnBeforeSerialize ()
		{
//			if (null == _definedWeights) {
//				_definedWeights = new List<DefinedWeight>();
//			} else {
//				_definedWeights.Clear();
//			}
//			foreach (KeyValuePair<string, DefinedWeight> kvp in _idToDefinedWeightMap) {
//				kvp.Value.id = kvp.Key;
//				_definedWeights.Add (kvp.Value);
//			}
		}

		public void OnAfterDeserialize ()
		{
//			if (null == _idToDefinedWeightMap) {
//				_idToDefinedWeightMap = new Dictionary<string, DefinedWeight>();
//			}
			if (null != weightDefinitions) {
//				foreach (DefinedWeight dw in _definedWeights) {
//					_idToDefinedWeightMap[dw.id] = dw;
//				}
			} else {
				weightDefinitions = new List<WeightDefinition> ();
			}
			if (null == interpolatedData) {
				interpolatedData = new List<InterpolatedWeightData> ();
			}
		}

		// IPathMetadata
		public int GetWeightDefinitionCount ()
		{
			return weightDefinitions.Count;
		}

		// IPathMetadata
		public WeightDefinition GetWeightDefinitionAtIndex (int index)
		{
			return weightDefinitions [index];
			//return weightIds.Count;
		}

		// IPathMetadata
		public bool ContainsWeightDefinition (string weightId)
		{
			return IndexOfWeightDefinition (weightId, false) >= 0;
		}

		// IPathMetadata
		public WeightDefinition GetWeightDefinition (string weightId)
		{
			return GetWeightDefinition (weightId, true);
		}


		public void PathDataChanged (PathDataScope scope)
		{
			// Find all in scope and mark dirty
			foreach (InterpolatedWeightData iwd in interpolatedData) {
				if (iwd.scope == scope) {
					iwd.interpolatedValues = null;
				}
			}
		}

		private WeightDefinition GetWeightDefinition (string weightId, bool throwExceptionIfNotFound)
		{
			int index = IndexOfWeightDefinition (weightId, throwExceptionIfNotFound);
			if (index < 0) {
				return null;
			} else {
				return weightDefinitions [index];
			}
		}

		private int IndexOfWeightDefinition (string weightId, bool throwExceptionIfNotFound = false)
		{
			int index = -1;
			int c = weightDefinitions.Count;
			for (int i = 0; i < c; i++) {
				if (weightDefinitions [i].WeightId == weightId) {
					index = i;
					break;
				}
			}
			if (index == -1 && throwExceptionIfNotFound) {
				throw new ArgumentException ("No weight with id '" + weightId + "' defined", "weightId");
			}
			return index;
		}



		// IEditablePathMetadata
		public WeightDefinition AddWeightDefinition ()
		{
			// pattern = "Weight <n>"
			string nameCandidate;
			do {
				nameCandidate = string.Format ("Weight {0}", nextWeightId++);
			} while (IndexOfWeightDefinition(nameCandidate, false) >= 0);
			return AddWeightDefinition (nameCandidate);
		}

		// IEditablePathMetadata
		public WeightDefinition AddWeightDefinition (string weightId)
		{
			if (IndexOfWeightDefinition (weightId, false) >= 0) {
				// Already exists
				throw new ArgumentException ("Weight with id '" + weightId + "' already defined", "weightId");
			}
			WeightDefinition wd = new WeightDefinition (weightId);
			weightDefinitions.Add (wd);

			// Fire an event
			FireChangedEvent ();

			return wd;
		}


		// IEditablePathMetadata
		public void RemoveWeightDefinition (string weightId)
		{
			int index = IndexOfWeightDefinition (weightId, true);
			weightDefinitions.RemoveAt (index);

			// Fire an event
			FireChangedEvent ();
		}

		// IEditablePathMetadata
		public WeightDefinition RenameWeightDefinition (string weightId, string newId)
		{
			int index = IndexOfWeightDefinition (weightId, true);
			if (IndexOfWeightDefinition (newId, false) >= 0) {
				throw new ArgumentException ("Weight with id '" + newId + "' already defined", "newId");
			}
			//weightDefinitions [index].id = newId;
			WeightDefinition oldDef = weightDefinitions [index];
			WeightDefinition newDef = new WeightDefinition (
				newId, oldDef.HasDefaultValue, oldDef.DefaultValue, oldDef.MinValue, oldDef.MaxValue);
			weightDefinitions [index] = newDef;

			// Fire an event
			FireChangedEvent ();

			return newDef;
		}

		// IEditablePathMetadata
		public void SetWeightDefinitionListIndex (string weightId, int newListIndex)
		{
			int index = IndexOfWeightDefinition (weightId, true);
			// Swap
			WeightDefinition w0 = weightDefinitions [newListIndex];
			weightDefinitions [newListIndex] = weightDefinitions [index];
			weightDefinitions [index] = w0;
		}

		// IEditablePathMetadata
		public WeightDefinition ModifyWeightDefinition (WeightDefinition wd)
		{
			int index = IndexOfWeightDefinition (wd.WeightId, true);
			weightDefinitions [index] = wd;

			// Fire an event
			FireChangedEvent ();

			return wd;
		}

		// IEditablePathMetadata
		public void Import (IPathMetadata source, bool overwrite)
		{
			int wdCount = source.GetWeightDefinitionCount ();
			for (int i = 0; i < wdCount; i++) {
				WeightDefinition sourceWd = source.GetWeightDefinitionAtIndex (i);
				string weightId = sourceWd.WeightId;
				bool alreadyExists = this.ContainsWeightDefinition (weightId);
				if (!alreadyExists) {
					this.AddWeightDefinition (weightId);
				}
				if (!alreadyExists || overwrite) {
					this.ModifyWeightDefinition (sourceWd);
				}
			}
		}
	}
}
