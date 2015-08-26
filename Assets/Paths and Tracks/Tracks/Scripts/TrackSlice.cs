using UnityEngine;
using System.Collections;
using System;

namespace Tracks
{
    public abstract class TrackSlice
    {

        //private Vector3[] points;

        private Vector3[] transformedPoints;
        private Vector3 center;
        private Quaternion rotation;
        protected float circumference;

        public Vector3 Center
        {
            get
            {
                return center;
            }
        }

        public Vector3[] Points
        {
            get
            {
                if (null == transformedPoints)
                {
                    Vector3[] localPoints = GetLocalPoints();
                    transformedPoints = new Vector3[localPoints.Length];
                    for (int i = 0; i < localPoints.Length; i++)
                    {
                        transformedPoints [i] = rotation * localPoints [i] + center;
                    }
                }
                return transformedPoints;
            }
        }

        public float Circumference
        {
            get
            {
                return circumference;
            }
        }

        public Vector3 Direction
        {
            get
            {
                return rotation * Vector3.forward;
            }
        }

        protected TrackSlice(Vector3 center, Quaternion rotation)
        {
            this.center = center;
            this.rotation = rotation;
        }

        protected abstract Vector3[] GetLocalPoints();
    }
}
