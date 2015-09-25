#undef PATHPOINT_WITH_SQUARE_DISTANCES

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Paths
{
	public interface IWithPosition
	{
		Vector3 GetPosition ();
	}

	// TODO should we make PathPoint mutable? Vector3 itself is mutable...
	[Serializable]
	public class PathPoint : ICloneable, IWithPosition, ISerializationCallbackReceiver
	{

		/// <summary>
		///  Position.
		/// </summary>
		public const int POSITION = 0x001;

		/// <summary>
		/// Direction of the path in the current point, i.e. the forward vector, the local z-axis.
		/// </summary>
		public const int DIRECTION = 0x002;

		/// <summary>
		/// Distance from the previous point to this point.
		/// </summary>
		public const int DISTANCE_FROM_PREVIOUS = 0x004;

		/// <summary>
		/// Distance from beginning of the path to this point.
		/// </summary>
		public const int DISTANCE_FROM_BEGIN = 0x008;

		/// <summary>
		/// Mask for all Distances (DISTANCE_FROM_PREVIOUS, DISTANCE_FROM_BEGIN).
		/// </summary>
		public const int DISTANCES = 0x00c;

		/// <summary>
		/// The Up vector (local y-axis)
		/// </summary>
		public const int UP = 0x010;

		/// <summary>
		/// The right vector (local x-axis)
		/// </summary>
		private const int RIGHT_CALCULATED = 0x020;
		public const int ANGLE = 0x040;

		/// <summary>
		/// All flags.
		/// </summary>
		public const int ALL = 0x07f;

		/// <summary>
		/// No flags
		/// </summary>
		public const int NONE = 0x000;

		[SerializeField]
		private Vector3
			position;

		[SerializeField]
		private float
			distanceFromPrevious;

		[SerializeField]
		private float
			distanceFromBegin;

#if PATHPOINT_WITH_SQUARE_DISTANCES
		[SerializeField]
		private bool
			distanceFromPreviousIsSquare = false;
		[SerializeField]
		private bool
			distanceFromBeginIsSquare = false;
#endif

		[SerializeField]
		private Vector3
			direction; // direction from this to next point

		/// <summary>
		/// The up vector
		/// </summary>
		[SerializeField]
		private Vector3
			up;
		[SerializeField]
		private Vector3
			right;
		[SerializeField]
		private float
			angle;
		[SerializeField]
		private int
			flags;

		// Only for serialization purposes; live objects use _weightIdToValueMap
		[SerializeField]
		private List<float>
			weightValues;

		// Only for serialization purposes; live objects use _weightIdToValueMap
		[SerializeField]
		private List<string>
			weightIds;

		[NonSerialized]
		private Dictionary<string, float>
			_weightIdToValueMap;

#if PATHPOINT_WITH_SQUARE_DISTANCES
		public PathPoint (Vector3 pos, Vector3 dir, Vector3 up, float angle, float distFromPrev, float distFromBegin, int flags)
			: this(pos, dir, up, angle, distFromPrev, false, distFromBegin, false, flags)
		{
		}
		public PathPoint (Vector3 pos, Vector3 dir, Vector3 up, float angle, float distFromPrev, bool distFromPrevIsSquare, float distFromBegin, bool distFromBeginIsSquare, int flags)
		{
			this.position = pos;
			this.direction = dir;
			this.up = up;
			this.angle = angle;
			this.distanceFromPrevious = distFromPrev;
			this.distanceFromPreviousIsSquare = distFromPrevIsSquare;
			
			this.distanceFromBegin = distFromBegin;
			this.distanceFromBeginIsSquare = distFromBeginIsSquare;
			
			this.flags = (flags & ~RIGHT_CALCULATED);
		}
		public PathPoint (Vector3 pos, Vector3 dir)
			: this(pos, dir, Vector3.zero, 0f, 0f, false, 0f, false, POSITION | DIRECTION)
		{
		}
		
		public PathPoint (Vector3 pos)
			: this(pos, Vector3.zero, Vector3.zero, 0f, 0f, false, 0f, false, POSITION)
		{
		}
		public PathPoint (PathPoint src, int flags)
			: this(src.position, src.direction, src.up, src.angle, src.distanceFromPrevious, src.distanceFromPreviousIsSquare, src.distanceFromBegin, src.distanceFromBeginIsSquare, flags)
		{
			this.right = src.right;
			ClearUnusedComponents ();
		}
#else
		public PathPoint (Vector3 pos, Vector3 dir, Vector3 up, float angle, float distFromPrev, float distFromBegin, int flags)
		{
			this.position = pos;
			this.direction = dir;
			this.up = up;
			this.angle = angle;
			this.distanceFromPrevious = distFromPrev;
			this.distanceFromBegin = distFromBegin;
			this.flags = (flags & ~RIGHT_CALCULATED);
		}
		public PathPoint (Vector3 pos, Vector3 dir)
			: this(pos, dir, Vector3.zero, 0f, 0f, 0f, POSITION | DIRECTION)
		{
		}
		
		public PathPoint (Vector3 pos)
			: this(pos, Vector3.zero, Vector3.zero, 0f, 0f, 0f, POSITION)
		{
		}
		public PathPoint (PathPoint src, int flags)
			: this(src.position, src.direction, src.up, src.angle, src.distanceFromPrevious, src.distanceFromBegin, flags)
		{
			this.right = src.right;
			if (null != src._weightIdToValueMap) {
				this._weightIdToValueMap = new Dictionary<string, float> ();
				foreach (KeyValuePair<string, float> kvp in src._weightIdToValueMap) {
					this._weightIdToValueMap [kvp.Key] = kvp.Value;
				}
			}
			ClearUnusedComponents ();
		}
#endif

//        


		public PathPoint (PathPoint src)
        : this(src, src.flags)
		{
		}



		public PathPoint ()
		{
			this.flags = 0;
		}

#region Serialization
		public void OnBeforeSerialize ()
		{
			if (null != _weightIdToValueMap) {
				this.weightIds = new List<string> (_weightIdToValueMap.Count);
				this.weightValues = new List<float> ();
				foreach (KeyValuePair<string, float> kvp in _weightIdToValueMap) {
					weightIds.Add (kvp.Key);
					weightValues.Add (kvp.Value);
				}
			} else {
				weightIds = null;
				weightValues = null;
			}
		}

		public void OnAfterDeserialize ()
		{
			int weightCount = (null != weightIds ? weightIds.Count : 0);
			if (weightCount > 0) {
				_weightIdToValueMap = new Dictionary<string, float> ();
				for (int i = 0; i < weightCount; i++) {
					_weightIdToValueMap [weightIds [i]] = weightValues [i];
				}
			} else {
				_weightIdToValueMap = null;
			}
		}
#endregion

		public override bool Equals (object obj)
		{
			// Generated by MonoDevelop
			if (obj == null)
				return false;
			if (ReferenceEquals (this, obj))
				return true;
			if (obj.GetType () != typeof(PathPoint))
				return false;
			PathPoint other = (PathPoint)obj;

			if (this.flags != other.flags) {
				return false;
			} 
			if (HasFlag (ANGLE) && this.angle != other.angle) {
				return false;
			}
			if (HasFlag (POSITION) && this.position != other.position) {
				return false;
			}
			if (HasFlag (DIRECTION) && this.direction != other.direction) {
				return false;
			}
			if (HasFlag (UP) && this.up != other.up) {
				return false;
			}

#if PATHPOINT_WITH_SQUARE_DISTANCES
			if (HasFlag (DISTANCE_FROM_PREVIOUS)) {

				if (this.distanceFromPreviousIsSquare == other.distanceFromPreviousIsSquare
					&& this.distanceFromPrevious != other.distanceFromPrevious) {
					return false;
				} else if (this.distanceFromPreviousIsSquare && this.GetDistanceFromPrevious () != other.distanceFromPrevious) {
					// Get square root of our distanceFromPrevious by using method  GetDistanceFromPrevious()^
					// HACK this code smells a bit - our Equals() implementation has a side effect
					return false;
				} else if (other.GetDistanceFromPrevious () != this.distanceFromPrevious) {
					/// NOTE: we get the other.distanceFromPrevious by using method GetDistanceFromPrevious()
					return false;
				}
			}
			if (HasFlag (DISTANCE_FROM_BEGIN)) {
				if (this.distanceFromBeginIsSquare == other.distanceFromBeginIsSquare
				    && this.distanceFromBegin != other.distanceFromBegin) {
					return false;
				} else if (this.distanceFromBeginIsSquare && this.GetDistanceFromBegin () != other.distanceFromBegin) {
					// Get square root of our distanceFromBegin by using GetDistanceFromBegin() ^
					// HACK this code smells a bit - our Equals() implementation has a side effect
					return false;
				} else if (other.GetDistanceFromBegin () != this.distanceFromBegin) {
					// NOTE: we get the other.distanceFromBegin by using method GetDistanceFromBegin()
					return false;
				} 
			}
#else
			if (HasFlag (DISTANCE_FROM_PREVIOUS) && this.distanceFromPrevious != other.distanceFromPrevious) {
				return false;
			}
			if (HasFlag (DISTANCE_FROM_BEGIN) && this.distanceFromBegin != other.distanceFromBegin) {
				return false;
			}
#endif
//			// TODO if weights are stored in different order, this won't work!
//			// Weights: should these be considered in comparision?
//			if (!Util.MiscUtil.ArrayEquals (this.weightIds, other.weightIds)) {
//				return false;
//			}
//			if (!Util.MiscUtil.ArrayEquals (this.weightValues, other.weightValues)) {
//				return false;
//			}
			if (this._weightIdToValueMap != other._weightIdToValueMap) {
				if (null == this._weightIdToValueMap || null == other._weightIdToValueMap) {
					return false;
				} else {
					// Contains all keys
					foreach (KeyValuePair<string, float> kvp in this._weightIdToValueMap) {
						string wid = kvp.Key;
						float wv = kvp.Value;
						if (!other._weightIdToValueMap.ContainsKey (wid)) {
							return false;
						} else if (other._weightIdToValueMap [wid] != wv) {
							return false;
						}
					}
				}
			}
			

			return true;
		}

		

		public override int GetHashCode ()
		{
			// TODO this is calculated wrong, we do have to take distance...IsSquare flags in account
			// Generated by MonoDevelop
			unchecked {
				int weightHash = 0;
				if (null != _weightIdToValueMap) {
					foreach (KeyValuePair<string, float> kvp in _weightIdToValueMap) {
						weightHash = weightHash ^ kvp.Key.GetHashCode () ^ kvp.Value.GetHashCode ();
					}
				}
				return position.GetHashCode () ^ distanceFromPrevious.GetHashCode () ^ distanceFromBegin.GetHashCode () ^ direction.GetHashCode () ^ up.GetHashCode () ^ right.GetHashCode () ^ angle.GetHashCode () ^ flags.GetHashCode () ^ weightHash;
			}
		}


		public object Clone ()
		{
			return new PathPoint (this);
		}

		

		public void ClearUnusedComponents ()
		{
			if (!IsFlag (flags, ALL)) {
				if (!IsPosition (flags)) {
					this.position = Vector3.zero;
				}
				if (!IsDirection (flags)) {
					this.direction = Vector3.zero;
				}
				if (!IsUp (flags)) {
					this.up = Vector3.zero;
				}
				if (!IsDistanceFromPrevious (flags)) {
					this.distanceFromPrevious = 0.0f;
#if PATHPOINT_WITH_SQUARE_DISTANCES
					this.distanceFromPreviousIsSquare = false;
#endif
				}
				if (!IsDistanceFromBegin (flags)) {
					this.distanceFromBegin = 0.0f;
#if PATHPOINT_WITH_SQUARE_DISTANCES
					this.distanceFromBeginIsSquare = false;
#endif
				}
				if (!IsAngle (flags)) {
					this.angle = 0.0f;
				}
			}
		}

		public Vector3 Position {
			get {
				return this.position;
			}
			set {
				this.position = value;
				// TODO should we automatically update flags?
				this.flags |= POSITION;
			}
		}

		// IWithPosition.GetPosition()
		public Vector3 GetPosition ()
		{
			return this.position;
		}

		public Vector3 Direction {
			get {
				return this.direction;
			}
			set {
				this.direction = value;
				// TODO should we automatically update flags?
				SetFlag (DIRECTION, true);
				SetFlag (RIGHT_CALCULATED, false);
			}
		}

		public Vector3 Up {
			get {
				return this.up;
			}
			set {
				this.up = value;
				SetFlag (UP, true);
				SetFlag (RIGHT_CALCULATED, false);
			}
		}

		// TODO this might be wrong?
		public Vector3 Right {
			get {
				if (!IsFlag (flags, RIGHT_CALCULATED) && HasUp && HasDirection) {
					right = Quaternion.AngleAxis (-90.0f, direction) * up;
					flags |= RIGHT_CALCULATED;
				}
				return this.right;
			}
		}

		// TODO we may need a property to define the plane of the angle?
		public float Angle {
			get {
				return this.angle;
			}
			set {
				this.angle = value;
				this.flags |= ANGLE;
			}
		}

		public float DistanceFromPrevious {
			get {
				return distanceFromPrevious;
			}
			set {
				this.distanceFromPrevious = value;
				this.flags |= DISTANCE_FROM_PREVIOUS;
			}
		}
		public float DistanceFromBegin {
			get {
				return distanceFromBegin;
			}
			set {
				this.distanceFromBegin = value;
				this.flags |= DISTANCE_FROM_BEGIN;
			}
		}


#if PATHPOINT_WITH_SQUARE_DISTANCES
		public float DistanceFromPrevious {
			get {
				return GetDistanceFromPrevious ();
			}
			protected set {
				this.distanceFromPrevious = value;
				this.distanceFromPreviousIsSquare = false;
				this.flags |= DISTANCE_FROM_PREVIOUS;
			}
		}
		public float SquareDistanceFromPrevious {
			set {
				this.distanceFromPrevious = value;
				this.distanceFromPreviousIsSquare = true;
				this.flags |= DISTANCE_FROM_PREVIOUS;
			}
		}
		protected float GetDistanceFromPrevious ()
		{
			if (this.distanceFromPreviousIsSquare) {
				this.distanceFromPrevious = Mathf.Sqrt (this.distanceFromPrevious);
				this.distanceFromPreviousIsSquare = false;
			}
			return this.distanceFromPrevious;
		}

		public float DistanceFromBegin {
			get {
				return GetDistanceFromBegin ();
			}
			protected set {
				this.distanceFromBegin = value;
				this.distanceFromBeginIsSquare = false;
				this.flags |= DISTANCE_FROM_BEGIN;
			}
		}
		public float SquareDistanceFromBegin {
			set {
				this.distanceFromBegin = value;
				this.distanceFromBeginIsSquare = true;
				this.flags |= DISTANCE_FROM_BEGIN;
			}
		}

		protected float GetDistanceFromBegin ()
		{
			if (this.distanceFromBeginIsSquare) {
				this.distanceFromBegin = Mathf.Sqrt (this.distanceFromBegin);
				this.distanceFromBeginIsSquare = false;
			}
			return this.distanceFromBegin;
		}
#endif

		public int Flags {
			get {
				return this.flags;
			}
			set {
				// TODO should we validate flags?
				// TODO should we reset components that are not included by flags?
				this.flags = value;
			}
		}

//		public void InitWeights (IPathData pathData)
//		{
//			int weightCount = 0;
//			if (null != pathData && pathData.IsPathMetadataSupported ()) {
//				IPathMetadata md = pathData.GetPathMetadata ();
//				if (null != md) {
//					weightCount = md.GetDefinedWeightCount ();
//				}
//			}
//			if (null == this.weights) {
//				this.weights = new float?[weightCount];
//			} else {
//				Array.Resize (ref this.weights, weightCount);
//			}
//		}

		// Weights
		public string[] GetWeightIds ()
		{
			if (null != _weightIdToValueMap && _weightIdToValueMap.Count > 0) {
				List<string> ids = new List<string> ();
				foreach (string id in _weightIdToValueMap.Keys) {
					ids.Add (id);
				}
				return ids.ToArray ();
			} else {
				return new string[0];
			}
		}

		public bool HasWeight (string weightId)
		{
			return null != _weightIdToValueMap && _weightIdToValueMap.ContainsKey (weightId);
		}

		public float GetWeight (string weightId, float defaultValue)
		{
			if (null != _weightIdToValueMap && _weightIdToValueMap.ContainsKey (weightId)) {
				return _weightIdToValueMap [weightId];
			} else {
				return defaultValue;
			}
		}
		public float GetWeight (string weightId)
		{
			if (null != _weightIdToValueMap && _weightIdToValueMap.ContainsKey (weightId)) {
				return _weightIdToValueMap [weightId];
			} else {
				string msg = string.Format ("Requested weight with id {0} is not defined in this PathPoint", weightId);
				throw new ArgumentException (msg, "weightId");
			}
		}

		public void SetWeight (string weightId, float value)
		{
			if (null == _weightIdToValueMap) {
				_weightIdToValueMap = new Dictionary<string, float> ();
			}
			_weightIdToValueMap [weightId] = value;
		}
		public void RemoveWeight (string weightId)
		{
			if (null != _weightIdToValueMap && _weightIdToValueMap.ContainsKey (weightId)) {
				_weightIdToValueMap.Remove (weightId);
				if (_weightIdToValueMap.Count == 0) {
					_weightIdToValueMap = null;
				}
			}
		}

		public bool SetFlag (int flag, bool value)
		{
			bool prevValue = IsFlag (this.flags, flag);
			if (value) {
				this.flags = this.flags & ~flag;
			} else {
				this.flags = this.flags | flag;
			}
			return prevValue;
		}

		public bool HasPosition {
			get {
				return HasFlag (POSITION);
			}
			set {
				SetFlag (POSITION, value);
			}
		}

		public bool HasDirection {
			get {
				return HasFlag (DIRECTION);
			}
			set {
				SetFlag (DIRECTION, value);
			}
		}

		public bool HasUp {
			get {
				return HasFlag (UP);
			}
			set {
				SetFlag (UP, value);
			}
		}

		public bool HasRight {
			get {
				return HasFlag (UP | DIRECTION);
			}
		}

		public bool HasAngle {
			get {
				return HasFlag (ANGLE);
			}
			set {
				SetFlag (ANGLE, value);
			}
		}

		public bool HasDistanceFromPrevious {
			get {
				return HasFlag (DISTANCE_FROM_PREVIOUS);
			}
			set {
				SetFlag (DISTANCE_FROM_PREVIOUS, value);
			}
		}

		public bool HasDistanceFromBegin {
			get {
				return HasFlag (DISTANCE_FROM_BEGIN);
			}
			set {
				SetFlag (DISTANCE_FROM_BEGIN, value);
			}
		}
	
		public bool HasFlag (int flag)
		{
			return flag == (this.flags & flag);
		}
		public static bool IsFlag (int flags, int flag)
		{
			return flag == (flags & flag);
		}

		public static bool IsPosition (int flags)
		{
			return IsFlag (flags, POSITION);
		}

		public static bool IsDirection (int flags)
		{
			return IsFlag (flags, DIRECTION);
		}

		public static bool IsUp (int flags)
		{
			return IsFlag (flags, UP);
		}
//        private static bool IsRight(int flags)
//        {
//            return IsFlag(flags, RIGHT);
//        }

		public static bool IsAngle (int flags)
		{
			return IsFlag (flags, ANGLE);
		}

		public static bool IsDistanceFromPrevious (int flags)
		{
			return IsFlag (flags, DISTANCE_FROM_PREVIOUS);
		}

		public static bool IsDistanceFromBegin (int flags)
		{
			return IsFlag (flags, DISTANCE_FROM_BEGIN);
		}
	}

	public class PathNode : IWithPosition
	{
		private PathPoint point;
		private PathNode previous;
		private PathNode next;
		private bool isFirst; // Needed for circular paths!
		private bool isLast; // Needed for circular paths!
		
		private PathNode (PathPoint pp)
		{
			this.point = pp;
		}
		public PathNode[] ToArray ()
		{
			List<PathNode> l = new List<PathNode> ();
			PathNode node = this;
			l.Add (node);
			while (null != (node = node.NextIfNotLast)) {
				l.Add (node);
			}
			return l.ToArray ();
		}
		public static PathNode BuildLinkedList (IPathData pathData)
		{
			return BuildLinkedList (pathData.GetAllPoints (), pathData.GetPathInfo ().IsLoop ());
		}
		public static PathNode BuildLinkedList (PathPoint[] pathPoints, bool loop)
		{
			return BuildLinkedList ((IList<PathPoint>)pathPoints, loop);
		}
		public static PathNode BuildLinkedList (IList<PathPoint> pathPoints, bool loop)
		{
			PathNode first;
			int count = pathPoints.Count;
			if (count > 0) {
				first = new PathNode (pathPoints [0]);
				first.isFirst = true;
				PathNode prev = first;
				for (int i = 1; i < count; i++) {
					PathNode lpp = new PathNode (pathPoints [i]);
					lpp.previous = prev;
					prev.next = lpp;
					prev = lpp;
				}
				prev.isLast = true;

				if (loop) {
					first.previous = prev;
					prev.next = first;
				}
			} else {
				first = null;
			}
			return first;
		}

		public static implicit operator PathPoint (PathNode node)
		{
			return node.point;
		}

		public Vector3 GetPosition ()
		{
			return point.GetPosition ();
		}
		
		public PathPoint Point {
			get {
				return point;
			}
		}
		
		public bool HasNext {
			get {
				return null != next;
			}
		}
		
		public PathNode Next {
			get {
				return next;
			}
		}
		
		public PathNode NextIfNotLast {
			get {
				return isLast ? null : next;
			}
		}
		public bool HasPrevious {
			get {
				return null != previous;
			}
		}
		public PathNode Previous {
			get {
				return previous;
			}
		}
		public PathNode PreviousIfNotFirst {
			get {
				return isFirst ? null : previous;
			}
		}
		public bool IsFirst {
			get {
				return isFirst;
			}
		}
		public bool IsLast {
			get {
				return isLast;
			}
		}
		
		

	}
}

