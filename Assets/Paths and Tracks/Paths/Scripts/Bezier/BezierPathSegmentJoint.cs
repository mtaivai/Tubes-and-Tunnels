using UnityEngine;
using System.Collections;

// TODO XXX this is not really used!
namespace Paths.Bezier
{
    [System.Serializable]
    public class BezierPathSegmentJoint
    {
        
        public BezierJointMode controlPointMode;
        
        public void Reset()
        {
            controlPointMode = BezierJointMode.Free;
        }
    }
}
