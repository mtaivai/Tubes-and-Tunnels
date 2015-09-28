using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Paths
{
	[CodeQuality.Experimental]
	[PathModifier(requiredInputFlags=PathPoint.POSITION,
                  processCaps=PathPoint.NONE,
	              passthroughCaps=PathPoint.DIRECTION | PathPoint.UP,
                  generateCaps=PathPoint.POSITION | PathPoint.DIRECTION | PathPoint.DISTANCES)]
	public class SubdividePathModifier : AbstractPathModifier
	{
		// TODO VERSION INFORMATION!

		internal int subdivideSegmentsMin = 0;
		internal int subdivideSegmentsMax = 4;

		// TODO rename to "subdivideTargetLength"
		internal float subdivideTreshold = 1.0f;

		internal bool interpolateWeights = false;
		internal bool outputGeneratedDirections = false;

        
		public SubdividePathModifier ()
		{
            
		}

		public override int GetGenerateFlags (PathModifierContext context)
		{
			int f = base.GetGenerateFlags (context);
			if (outputGeneratedDirections) {
				f |= PathPoint.DIRECTION;
			} else {
				f &= ~PathPoint.DIRECTION;
			}
			return f;
		}
        
		// TODO check if values are rational!
		public int SubdivideSegmentsMin {
			get {
				return this.subdivideSegmentsMin;
			}
			set {
				subdivideSegmentsMin = value;
			}
		}
        
		public int SubdivideSegmentsMax {
			get {
				return this.subdivideSegmentsMax;
			}
			set {
				subdivideSegmentsMax = value;
			}
		}
        
		public float SubdivideTreshold {
			get {
				return this.subdivideTreshold;
			}
			set {
				subdivideTreshold = value;
			}
		}
		public bool OutputGeneratedDirections {
			get {
				return this.outputGeneratedDirections;
			}
			set {
				this.outputGeneratedDirections = value;
			}
		}
		public bool InterpolateWeights {
			get {
				return this.interpolateWeights;
			}
			set {
				this.interpolateWeights = value;
			}
		}

		public override void OnSerialize (Serializer store)
		{
			store.Property ("subdivideSegmentsMin", ref subdivideSegmentsMin);
			store.Property ("subdivideSegmentsMax", ref subdivideSegmentsMax);
			store.Property ("subdivideTreshold", ref subdivideTreshold);
			store.Property ("interpolateWeights", ref interpolateWeights);
			store.Property ("outputGeneratedDirections", ref outputGeneratedDirections);

		}

		/*
        public void DrawInspectorGUI(TrackInspector trackInspector) {
            Track target = (Track)trackInspector.target;
            
            EditorGUI.BeginChangeCheck();
            subdividePath = EditorGUILayout.Toggle("Subdivide Path", subdividePath);
            if (EditorGUI.EndChangeCheck()) {
                EditorUtility.SetDirty(target);
                trackInspector.TrackGeneratorModified();
            }
            
            EditorGUI.BeginDisabledGroup(!subdividePath);
            EditorGUI.BeginChangeCheck();
            subdivideSegmentsMin = EditorGUILayout.IntSlider("Min Subdivisions", subdivideSegmentsMin, 0, 20);
            if (EditorGUI.EndChangeCheck()) {
                EditorUtility.SetDirty(target);
                trackInspector.TrackGeneratorModified();
            }
            EditorGUI.BeginChangeCheck();
            subdivideSegmentsMax = EditorGUILayout.IntSlider("Max Subdivisions", subdivideSegmentsMax, 0, 20);
            if (EditorGUI.EndChangeCheck()) {
                EditorUtility.SetDirty(target);
                trackInspector.TrackGeneratorModified();
            }
            EditorGUI.BeginChangeCheck();
            subdivideTreshold = EditorGUILayout.FloatField("Subdivision Target Length", subdivideTreshold);
            if (EditorGUI.EndChangeCheck()) {
                EditorUtility.SetDirty(target);
                trackInspector.TrackGeneratorModified();
            }
            
            EditorGUI.EndDisabledGroup();
            
            //      Track track = trackInspector.target as Track;
            //      Path path = track.Path;

//          EditorGUI.BeginChangeCheck();
//      usePathResolution = EditorGUILayout.ToggleLeft("Use Path Resolution (" + path.GetResolution() + ")", usePathResolution);
//      if (EditorGUI.EndChangeCheck()) {
//          EditorUtility.SetDirty(trackInspector.target);
//          trackInspector.TrackGeneratorModified();
//      }
//      
//      EditorGUI.BeginDisabledGroup(usePathResolution);
//      EditorGUI.BeginChangeCheck();
//      customResolution = EditorGUILayout.IntSlider("Custom Resolution", customResolution, 1, 100);
//      if (EditorGUI.EndChangeCheck()) {
//          EditorUtility.SetDirty(trackInspector.target);
//          trackInspector.TrackGeneratorModified();
//      }
//      EditorGUI.EndDisabledGroup();
        }
    */
        
		protected override PathPoint[] DoGetModifiedPoints (PathPoint[] points)
		{
			return Subdivide (points, context.PathInfo.IsLoop (), this.SubdivideTreshold, this.SubdivideSegmentsMin, this.SubdivideSegmentsMax, this.interpolateWeights, this.outputGeneratedDirections);
		}
		public static PathPoint[] Subdivide (PathPoint[] points, bool loop, float targetSegmentLength, int minSubdivisions, int maxSubdivisions, bool interpolateWeights, bool outputGeneratedDirections)
		{
			List<PathPoint> results = new List<PathPoint> ();

			float distFromBegin = 0.0f;
			int pointCount = loop ? points.Length : points.Length - 1;
			int lastPointIndex = points.Length - 1;

			// If interpolateWeights == true, we collect all defined weights as we are iterating
			// the points array

			HashSet<string> weightIds = new HashSet<string> ();

			for (int i = 0; i < pointCount; i++) {

				PathPoint pp = points [i];

				if (interpolateWeights) {
					foreach (string wid in pp.GetWeightIds()) {
						weightIds.Add (wid);
					}
				}

				// Insert the original point
				results.Add (pp);

				// Insert subdivisions
				PathPoint nextPp;

				bool updateNextPpDistance = false;
				if (loop && i == lastPointIndex) {
					// Loop path; last point is the first point!
					nextPp = points [0];
					updateNextPpDistance = false;
				} else {
					nextPp = points [i + 1];
					updateNextPpDistance = true;
				}
				float distToNext;

				if (pp.HasDistanceFromBegin) {
					distFromBegin = pp.DistanceFromBegin;
				}

				if (nextPp.HasDistanceFromPrevious) {
					distToNext = nextPp.DistanceFromPrevious;
				} else {
					// Calculate distance (and store in the point)
					distToNext = PathPoint.Distance (pp, nextPp);
					if (updateNextPpDistance) {
						nextPp.DistanceFromPrevious = distToNext;
					}
				}
				if (updateNextPpDistance && !nextPp.HasDistanceFromBegin) {
					nextPp.DistanceFromBegin = distFromBegin + distToNext;
				}

				// Direction is just a linear interpolation; we don't want to use
				// possibly previously calculated directions since they might not
				// be result of linear interpolation between points!
				Vector3 dirToNext = (nextPp.Position - pp.Position).normalized;
				if (outputGeneratedDirections && !pp.HasDirection) {
					pp.Direction = dirToNext;
				}

				int combinedFlags = pp.Flags & nextPp.Flags;
				bool hasAngle = PathPoint.IsAngle (combinedFlags);
				bool hasUp = PathPoint.IsUp (combinedFlags);
				bool hasDir = PathPoint.IsDirection (combinedFlags);

				// x . x . x

				int divs = Mathf.RoundToInt (distToNext / targetSegmentLength) - 1;
				if (divs > maxSubdivisions) {
					divs = maxSubdivisions;
				}
				if (divs < minSubdivisions) {
					divs = minSubdivisions;
				}
				float divSegments = divs + 1;
				for (int j = 0; j < divs; j++) {
					float t = (float)(j + 1) / divSegments;
					float divDist = distToNext * t;

					Vector3 pos = pp.Position + dirToNext * divDist;
					float angle = hasAngle ? Mathf.Lerp (pp.Angle, nextPp.Angle, t) : 0f;
					Vector3 up = hasUp ? Vector3.Lerp (pp.Up, nextPp.Up, t) : Vector3.zero;


					// If original points include directions, we use linear interpolation for
					// subdivision dirs. Otherwise we use the segment direction before subdivision
					Vector3 dir = hasDir ? Vector3.Lerp (pp.Direction, nextPp.Direction, t) : dirToNext;

					// Construct a new pathpoint with interpolated position, direction, up, angle and
					// distances but without possible weights!
					PathPoint divPp = new PathPoint (
						pos, 
						dir, 
						up, 
						angle, 
						divDist, 
						pp.DistanceFromBegin + divDist, 
						combinedFlags);
					results.Add (divPp);
				}

			}
			PathPoint[] resultArray = results.ToArray ();
			if (interpolateWeights) {
				foreach (string weightId in weightIds) {
					InterpolateWeightsPathModifier.InterpolateWeight (weightId, resultArray, loop);
				}
			}
			return resultArray;
		}
        
		private PathPoint[] XXXOLD_DoSubdividePath (PathPoint[] points, PathModifierContext context)
		{
			if (!enabled || subdivideSegmentsMax < 0 || null == points || points.Length == 0) {
				return points;
			}

			int ppFlags = GetOutputFlags (context);

			List<PathPoint> newPoints = new List<PathPoint> ();
            
			// 0 1 2 3 4 5 6 7 8 9
			// 0.1.2.3.4.5.6.7.8.9

			float distFromBegin = 0.0f;
			for (int i = 1; i < points.Length; i++) {
				float distFromPrev;
				if (points [i].HasDistanceFromPrevious) {
					distFromPrev = points [i].DistanceFromPrevious;
				} else {
					distFromPrev = (points [i].Position - points [i - 1].Position).magnitude;
				}
				distFromBegin += distFromPrev;

				int subdivideCount = subdivideSegmentsMin;
				if (subdivideCount != subdivideSegmentsMax) {
                    
					if (distFromPrev > subdivideTreshold) {
						subdivideCount = (int)(distFromPrev / subdivideTreshold);
					} else if (distFromPrev < subdivideTreshold) {
						subdivideCount = 0;
					}
                    
					if (subdivideCount > subdivideSegmentsMax) {
						subdivideCount = subdivideSegmentsMax;
					} else if (subdivideCount < subdivideSegmentsMin) {
						subdivideCount = subdivideSegmentsMin;
					}
				}
				// We don't use the PathPoint.Direction because it's the "point" direction, i.e. an average
				// between incoming and outgoing directions!
				Vector3 segmentDir = (points [i].Position - points [i - 1].Position).normalized;
                
				float divisionDist = distFromPrev / (float)subdivideCount;
				float currentDistInDiv = 0.0f;
                
				for (int j = 0; j < subdivideCount; j++) {
					// TODO also interpolate the direction
					float divDistFromPrev = (i > 1 && j > 0) ? divisionDist : 0.0f;
                    
					Vector3 currentDir;
					bool interpolateDirections = true;
					if (j == 0) {
						currentDir = points [i - 1].Direction;
					} else {
						if (interpolateDirections) {
							float t = ((float)j / (float)subdivideCount);
							/*if (t < 0.5f) {
                            // Segments before the segment middle:
                            // Rotate segments towards the segment direction (the slice at the middle should
                            // be in line with the path)
                            currentDir = Vector3.Lerp(points[i - 1].Direction, segmentDir, t * 2.0f);
                        } else if (t > 0.5f) {
                            // Segments after the segment middle:
                            // Rotate segments towards the next path point direction

                            currentDir = Vector3.Lerp(segmentDir, points[i].Direction, (t - 0.5f) * 2.0f);
                        } else {
                            // Middle point!
                            currentDir = segmentDir;
                        }*/
							currentDir = Vector3.Lerp (points [i - 1].Direction, points [i].Direction, t * 1.0f);
							//currentDir = segmentDir;//((points[i - 1].Direction + points[i - 0].Direction) / 2.0f).normalized;
						} else {
							currentDir = segmentDir;
						}
                        
					}
                    
					PathPoint pp = new PathPoint (points [i - 1]);
					pp.Position = pp.Position + segmentDir * currentDistInDiv;
					pp.DistanceFromPrevious = divDistFromPrev;
					pp.DistanceFromBegin = distFromBegin + currentDistInDiv;

					newPoints.Add (pp);
					currentDistInDiv += divisionDist;
				}
                
				//          newPoints[i * 4 - 4] = new PathPoint(points[i - 1].Position, dir);
				//          newPoints[i * 4 - 2] = new PathPoint(points[i - 1].Position + dir * (divisionDist * 2.0f), dir);
				//          newPoints[i * 4 - 1] = new PathPoint(points[i - 1].Position + dir * (divisionDist * 3.0f), dir);
                
			}
			// Add Last point
			if (points.Length > 0) {
				newPoints.Add (points [points.Length - 1]);
			}
            
			return newPoints.ToArray ();
		}
	}
}
