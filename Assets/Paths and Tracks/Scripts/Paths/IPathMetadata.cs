// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

using Util;

// TODO we need a GetPoint(float t) method, either directly in a path
// or as a separate utility. For pure bezier paths it would be easy to implement,
// but for polyline or composite paths it woudl need some more effort by
// implementing a smart smoothing / interpolating algorithm. One possiblity would
// be to convert polyline path to bezier path on-the-fly.

namespace Paths
{


	public enum PathDataScope
	{
		ControlPoints,
		FinalData,
		//All = 0x04,
	}

	public interface IPathMetadata
	{
		int GetWeightDefinitionCount ();
		WeightDefinition GetWeightDefinitionAtIndex (int index);

		bool ContainsWeightDefinition (string weightId);
		WeightDefinition GetWeightDefinition (string weightId);

		//float GetInterpolatedWeight (string weightId, int index, PathDataScope scope);
		//float[] GetInterpolatedWeights (string weightId, PathDataScope scope);

		void PathDataChanged (PathDataScope scope);

//		string GetDefinedWeightName (int weightId);

//		string[] GetWeightNames ();

//		float[] GetAllWeightsOfPoint (int pointIndex, float defaultWeightValue = 0.0f);
//		float[][] GetAllWeightsOfAllPoints (float defaultWeightValue = 0.0f);
//		float[] GetWeightOfAllPoints (int weightIndex, float defaultWeightValue = 0.0f);
//		float GetWeightOfPoint (int pointIndex, int weightIndex, float defaultWeightValue = 0.0f);
//		bool PointHasWeight (int pointIndex, int weightIndex);

//		string[] GetMarkers(int pointIndex);
//		string FindMarker();
	}

	public interface IEditablePathMetadata : IPathMetadata
	{
		WeightDefinition AddWeightDefinition ();
		WeightDefinition AddWeightDefinition (string weightId);
		void RemoveWeightDefinition (string weightId);

		WeightDefinition RenameWeightDefinition (string weightId, string newId);
		void SetWeightDefinitionListIndex (string weightId, int newListIndex);
		
		WeightDefinition ModifyWeightDefinition (WeightDefinition wd);

		void Import (IPathMetadata src, bool overwrite);
	}

	public sealed class UnsupportedPathMetadata : IPathMetadata
	{
		private static readonly UnsupportedPathMetadata _instance = 
			new UnsupportedPathMetadata ();

		public static UnsupportedPathMetadata Instance {
			get {
				return _instance;
			}
		}

		public int GetWeightDefinitionCount ()
		{
			return 0;
		}

		public WeightDefinition GetWeightDefinitionAtIndex (int index)
		{
			throw new ArgumentOutOfRangeException ("index", index, "index > 0");
		}

		public bool ContainsWeightDefinition (string weightId)
		{
			return false;
		}

		public WeightDefinition GetWeightDefinition (string weightId)
		{
			throw new ArgumentException ("No such weight definition: " + weightId, "weightId");
		}

		public void PathDataChanged (PathDataScope scope)
		{
			// NOP
		}
	}

	// Immutable
	[Serializable]
	public sealed class WeightDefinition
	{
		[SerializeField]
		private string
			weightId;
		
		[SerializeField]
		private bool
			hasDefaultValue;

		[SerializeField]
		private float
			defaultValue;
		
		[SerializeField]
		private float
			minValue;

		[SerializeField]
		private float
			maxValue;
		
		private WeightDefinition (WeightDefinition src)
		{
			this.weightId = src.weightId;
			this.hasDefaultValue = src.hasDefaultValue;
			this.defaultValue = src.defaultValue;
			this.minValue = src.minValue;
			this.maxValue = src.maxValue;
		}
		public WeightDefinition (string weightId, bool hasDefaultValue, float defaultValue, float minValue, float maxValue)
		{
			this.weightId = weightId;
			this.hasDefaultValue = hasDefaultValue;
			this.defaultValue = defaultValue;
			this.minValue = minValue;
			this.maxValue = maxValue;
		}
		
		public WeightDefinition (string weightId, float defaultValue)
			: this(weightId, true, defaultValue, float.NegativeInfinity, float.PositiveInfinity)
		{
		}
		
		public WeightDefinition (string weightId)
			: this(weightId, false, 0f, float.NegativeInfinity, float.PositiveInfinity)
		{
		}
		
		public string WeightId {
			get {
				return weightId;
			}
		}
		public bool HasDefaultValue {
			get {
				return hasDefaultValue;
			}
		}

		public float DefaultValue {
			get {
				return defaultValue;
			}
		}
		
		public bool MaxValueDefined {
			get {
				return maxValue < float.PositiveInfinity;
			}
		}
		public float MaxValue {
			get {
				return maxValue;
			}
		}
		public bool MinValueDefined {
			get {
				return minValue > float.NegativeInfinity;
			}
		}
		public float MinValue {
			get {
				return minValue;
			}
		}
		public bool ValueRangeDefined {
			get {
				return minValue > float.NegativeInfinity && maxValue < float.PositiveInfinity;
			}
		}
		
		public WeightDefinition WithDefaultValue (float defaultValue)
		{
			WeightDefinition wd = WithHasDefaultValue (true);
			wd.defaultValue = defaultValue;
			return wd;
		}
		public WeightDefinition WithHasDefaultValue (bool hasDefaultValue)
		{
			WeightDefinition wd = new WeightDefinition (this);
			wd.hasDefaultValue = hasDefaultValue;
			return wd;
		}
		public WeightDefinition WithoutDefaultValue ()
		{
			return WithHasDefaultValue (false);
		}
		public WeightDefinition WithValueRange (float min, float max)
		{
			WeightDefinition wd = new WeightDefinition (this);
			wd.minValue = min;
			wd.maxValue = max;
			return wd;
		}
		public WeightDefinition WithoutMinValue ()
		{
			return WithMinValue (float.NegativeInfinity);
		}
		public WeightDefinition WithMinValue (float min)
		{
			return WithValueRange (min, maxValue);
		}
		public WeightDefinition WithMaxValue (float max)
		{
			return WithValueRange (minValue, max);
		}
		public WeightDefinition WithoutMaxValue ()
		{
			return WithMaxValue (float.PositiveInfinity);
		}
	}
}
