using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Paths
{
	[PathModifier(requiredInputFlags=PathPoint.POSITION,
                  processCaps=PathPoint.NONE,
                  passthroughCaps=PathPoint.UP,
                  generateCaps=PathPoint.POSITION | PathPoint.DIRECTION | PathPoint.DISTANCES)]
	public class SubdividePathModifier : AbstractPathModifier
	{
		internal int subdivideSegmentsMin = 0;
		internal int subdivideSegmentsMax = 4;
		internal float subdivideTreshold = 1.0f;
        
		public SubdividePathModifier ()
		{
            
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

		public override void OnSerialize (Serializer store)
		{
			store.Property ("subdivideSegmentsMin", ref subdivideSegmentsMin);
			store.Property ("subdivideSegmentsMax", ref subdivideSegmentsMax);
			store.Property ("subdivideTreshold", ref subdivideTreshold);
            
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
        
		protected override PathPoint[] DoGetModifiedPoints (PathPoint[] points, PathModifierContext context)
		{
			return DoSubdividePath (points, context);
		}
        
		private PathPoint[] DoSubdividePath (PathPoint[] points, PathModifierContext context)
		{
			if (!enabled || subdivideSegmentsMax < 0 || null == points || points.Length == 0) {
				return points;
			}

			int ppFlags = GetOutputFlags (context);
			List<PathPoint> newPoints = new List<PathPoint> ();
            
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
                    
					PathPoint pp = new PathPoint (points [i - 1].Position + segmentDir * currentDistInDiv, 
                                                  currentDir, points [i - 1].Up, points [i - 1].Angle, divDistFromPrev, distFromBegin + currentDistInDiv, ppFlags);
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
