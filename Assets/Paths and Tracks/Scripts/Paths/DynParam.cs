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
	public enum DynParamSource
	{
		Constant,
		WeightParam,
	}
	public enum DynParamExprOp
	{
		Nop,
		Plus,
		Minus,
		Multiply,
		Divide,
		ValueOrDefault,

	}

	public class DynParamExpr
	{

		public static class Symbols
		{
			private static readonly string[] _All = new string[] {
				Nop, Plus, Minus, Multiply, Divide, ValueOrDefault,
			};
			private static readonly string[] _AllAlt = new string[] {
				Nop, Plus, Minus, Multiply, DivideAlt, ValueOrDefault,
			};
			public const string Nop = " ";
			public const string Plus = "+";
			public const string Minus = "-";
			public const string Multiply = "*";
			public const string Divide = "/";
			public const string DivideAlt = "\u2215";
			public const string ValueOrDefault = "?:";

			public static string[] All {
				get {
					string[] arr = new string[_All.Length];
					Array.Copy (_All, arr, _All.Length);
					return arr;
				}
			}
			public static string[] AllAlt {
				get {
					string[] arr = new string[_AllAlt.Length];
					Array.Copy (_AllAlt, arr, _AllAlt.Length);
					return arr;
				}
			}
			public static string GetSymbol (DynParamExprOp op)
			{
				switch (op) {
				case DynParamExprOp.Nop:
					return Symbols.Nop;
				case DynParamExprOp.Plus:
					return Symbols.Plus;
				case DynParamExprOp.Minus:
					return Symbols.Minus;
				case DynParamExprOp.Multiply:
					return Symbols.Multiply;
				case DynParamExprOp.Divide:
					return Symbols.Divide;
				case DynParamExprOp.ValueOrDefault:
					return Symbols.ValueOrDefault;
				default:
					return "";
				}
			}
			public static DynParamExprOp GetOp (string symbol)
			{
				DynParamExprOp op;
				symbol = (null == symbol) ? "" : symbol.Trim ();
				switch (symbol) {
				case Symbols.Plus:
					op = DynParamExprOp.Plus;
					break;
				case Symbols.Minus:
					op = DynParamExprOp.Minus;
					break;
				case Symbols.Multiply:
					op = DynParamExprOp.Multiply;
					break;
				case Symbols.Divide:
				case Symbols.DivideAlt:
					op = DynParamExprOp.Divide;
					break;
				case Symbols.ValueOrDefault:
					op = DynParamExprOp.ValueOrDefault;
					break;
				default:
					op = DynParamExprOp.Nop;
					break;
				}
				return op;
			}
		}



		[SerializeField]
		private DynParamExprOp
			op;
		[SerializeField]
		private float
			rhs;

		public DynParamExpr () : this(DynParamExprOp.Nop, 0f)
		{
		}
		public DynParamExpr (DynParamExprOp op, float rhs)
		{
			this.op = op;
			this.rhs = rhs;
		}
		public DynParamExprOp Op {
			get {
				return op;
			}
			set {
				this.op = value;
			}
		}
		public float Rhs {
			get {
				return rhs;
			}
			set {
				this.rhs = value;
			}
		}
		public string OpSymbol {
			get {
				return Symbols.GetSymbol (op);
			}
			set {
				op = Symbols.GetOp (value);
			}
		}

		public float? Apply (float? value)
		{
			if (op == DynParamExprOp.ValueOrDefault) {
				value = (null != value) ? value : this.rhs;
			} else if (null != value) {
				switch (op) {
				case DynParamExprOp.Nop:
					break;
				case DynParamExprOp.Plus:
					value += rhs;
					break;
				case DynParamExprOp.Minus:
					value -= rhs;
					break;
				case DynParamExprOp.Multiply:
					value *= rhs;
					break;
				case DynParamExprOp.Divide:
					value /= rhs;
					break;
				default:
					throw new Exception ("Huh, unexpected DynParamExprOp: " + op);
				}
			}
			return value;
		}
		public void Save (ParameterStore store, string name)
		{
			store = store.ChildWithPrefix (name);
			store.SetEnum ("op", op);
			store.SetFloat ("rhs", rhs);
		}
		protected void DoLoad (ParameterStore store, string name)
		{
			store = store.ChildWithPrefix (name);
			
			op = store.GetEnum ("op", op);
			rhs = store.GetFloat ("rhs", rhs);
		}
		public static DynParamExpr Load (ParameterStore store, string name, DynParamExpr defaultValue)
		{
			if (null == defaultValue) {
				defaultValue = new DynParamExpr ();
			}
			defaultValue.DoLoad (store, name);
			return defaultValue;
		}
	}

	public class ReadOnlyParamException : Exception
	{
		
	}
	
	[Serializable]
	public class DynParam
	{
		[SerializeField]
		private DynParamSource
			valueSource;
		
		[SerializeField]
		private string
			weightId;
		
		[SerializeField]
		private float
			value; // Also used as 'default value'

		[SerializeField]
		private List<DynParamExpr>
			expressions = new List<DynParamExpr> ();

	
//		[SerializeField]
//		private bool
//			overrideWeightDefDefault;

		public DynParam ()
		{
		}
		public DynParam (DynParamSource source)
		{
			this.valueSource = source;
		}
		public DynParam (float value) : this(DynParamSource.Constant)
		{
			this.value = value;
		}
		public DynParamSource ValueSource {
			get {
				return valueSource;
			}
			set {
				this.valueSource = value;
			}
		}
		public float ConstantValue {
			get {
				return value;
			}
			set {
				this.value = value;
			}
		}
		public string WeightId {
			get {
				return weightId;
			}
			set {
				weightId = value;
			}
		}

		public DynParamExpr[] Expressions {
			get {
				return expressions.ToArray ();
			}
//			set {
//				exprOp = value;
//			}
		}

		public override string ToString ()
		{
			return string.Format ("[DynParam: valueSource={0}, weightId={1}, value={2}, expressions={3}]", valueSource, weightId, value, expressions);
		}
		

		public DynParamExpr AddExpression (DynParamExpr expr = null)
		{
			return InsertExpression (expressions.Count, expr);
		}
		public DynParamExpr InsertExpression (int index, DynParamExpr expr = null)
		{
			if (null == expr) {
				expr = new DynParamExpr ();
			}
			expressions.Insert (index, expr);
			return expr;
		}
		public void RemoveExpressionAt (int index)
		{
			expressions.RemoveAt (index);
		}

//		public bool OverrideWeightDefDefault {
//			get {
//				return overrideWeightDefDefault;
//			}
//			set {
//				overrideWeightDefDefault = value;
//			}
//		}
		public float GetRequiredValue (PathPoint pp, IPathMetadata pathMetadata)
		{
			float? val = GetValue (pp, pathMetadata);
			if (null == val) {
				throw new Exception ("Required value not available: " + ToString ());
			}
			return (float)val;
		}
		public float? GetValue (PathPoint pp, IPathMetadata pathMetadata)
		{
			float? value;
			switch (valueSource) {
			case DynParamSource.Constant:
				value = this.value;
				break;
			case DynParamSource.WeightParam:
				value = GetWeightValue (pp, pathMetadata);
				break;
			default:
				throw new Exception ("Invalid valueSource: " + valueSource);
			}
			if (null == value) {
				// TODO should we really do this?
				value = this.value;
			}
			return value;
		
		}
		public void SetValue (float value)
		{
			switch (valueSource) {
			case DynParamSource.Constant:
				this.value = value;
				break;
			default:
				throw new ReadOnlyParamException ();
			}
		}
//		private float GetWeightValue (IPathData pathData, int pointIndex)
		private float? GetWeightValue (PathPoint pp, IPathMetadata pathMetadata)
		{
			float? weightValue;
			if (pp.HasWeight (weightId)) {
				weightValue = pp.GetWeight (weightId);
			} else {
				weightValue = null;
			}
			float? defaultValue = null;
			if (null == weightValue) {
				// No weight defined for the point; check from metadata:
				if (null != pathMetadata && pathMetadata.ContainsWeightDefinition (weightId)) {
					WeightDefinition wd = pathMetadata.GetWeightDefinition (weightId);
					if (wd.HasDefaultValue) {
						defaultValue = wd.DefaultValue;
						weightValue = defaultValue;
					}
				}
			}

			// Apply expressions
			foreach (DynParamExpr expr in expressions) {
				weightValue = expr.Apply (weightValue);
			}

			if (null == weightValue) {
				// Still null after expressions; use the value from metadata, if any
				weightValue = defaultValue;

			}

			return weightValue;
		}

		public void Save (ParameterStore store, string name)
		{
			store = store.ChildWithPrefix (name);
			
			store.SetEnum ("valueSource", valueSource);
			store.SetString ("weightId", weightId);
			store.SetFloat ("value", value);
//			store.SetBool ("overrideWeightDefDefault", overrideWeightDefDefault);

			// Expressions
			int exprCount = (null != expressions ? expressions.Count : 0);
			store.SetInt ("expressions.Count", exprCount);
			// Remove old:
			foreach (string toRemove in store.FindParametersStartingWith("expressions[")) {
				store.RemoveParameter (toRemove);
			}
			for (int i = 0; i < exprCount; i++) {
				DynParamExpr expr = expressions [i];
				expr.Save (store, "expressions[" + i + "]");
			}


		}
		protected void DoLoad (ParameterStore store, string name)
		{
			store = store.ChildWithPrefix (name);
			
			valueSource = store.GetEnum ("valueSource", valueSource);
			weightId = store.GetString ("weightId", weightId);
			value = store.GetFloat ("value", value);
//			overrideWeightDefDefault = store.GetBool ("overrideWeightDefDefault", overrideWeightDefDefault);

			// Expressions
			int exprCount = store.GetInt ("expressions.Count", 0);
			expressions.Clear ();
			for (int i = 0; i < exprCount; i++) {
				expressions.Add (DynParamExpr.Load (store, "expressions[" + i + "]", null));
			}


		}
		public static DynParam Load (ParameterStore store, string name, DynParam param = null)
		{
			if (null == param) {
				param = new DynParam ();
			}
			param.DoLoad (store, name);
			return param;
		}
	}

	// TODO Should this be renamed to AbstractPath?

}
