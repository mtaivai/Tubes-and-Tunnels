using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

using Util;

namespace Paths
{
    public delegate void PathChangedEventHandler (object sender,EventArgs e);

    public class PathGizmoPrefs
    {
        public static readonly Color ControlPointConnectionLineColor = Color.gray;
        public static readonly Color ControlPointMarkerColor = Color.gray;
        public static readonly Color FinalPathLineColor = Color.cyan;
        public static readonly Color FinalPathPointMarkerColor = Color.cyan;
        public static readonly Color FinalPathFirstPointMarkerColor = Color.yellow;
        public static readonly Color UpVectorColor = Color.green;
        public static readonly Color DirVectorColor = Color.blue;
        public static readonly Color RightVectorColor = Color.red;
        public static readonly float UpVectorLength = 1.0f;
        public static readonly float DirVectorLength = 1.0f;
        public static readonly float RightVectorLength = 1.0f;
        public static readonly float FinalPathPointMarkerSize = 0.1f;
        public static readonly float FinalPathFirstPointMarkerSize = 0.2f;
    }

    // TODO we don't really need the IPath interface since we're always referring to Path
    // (which is a GameObject)
//  public interface IPath
//  {
//      bool IsLoop ();
//
//  }
    public interface IPathInfo
    {
        bool IsLoopPath();
//      int GetPointCount();
    }

    public class PathInfo : IPathInfo
    {
        private Path path;

        public PathInfo (Path path)
        {
            this.path = path;
        }

        public bool IsLoopPath()
        {
            return path.IsLoop();
        }
    }

    public abstract class Path : MonoBehaviour, ISerializationCallbackReceiver, IReferenceContainer
    {

        public enum PathStatus
        {
            Dynamic,
            ManualRefresh,
            Frozen
        }

        public event PathChangedEventHandler Changed;

        [SerializeField]
        private ParameterStore
            parameterStore = new Util.ParameterStore();

//      // don't serialize!
//      private List<IPathModifier> pathModifierInstances = new List<IPathModifier>();
        private DefaultPathModifierContainer pathModifierContainer = null;
        [SerializeField]
        private List<PathPoint>
            pathPoints;
        [SerializeField]
        private int
            pathPointFlags = 0;
        [SerializeField]
        private int
            rawPathPointFlags = 0;
        [SerializeField]
        private bool
            pathPointsDirty = true;
        [SerializeField]
        private PathStatus
            frozenStatus = PathStatus.Dynamic;
        [SerializeField]
        private List<UnityEngine.Object>
            referents = new List<UnityEngine.Object>();

//        private PathEditorPrefs editorPrefs = PathEditorPrefs.Defaults;
//
//        public PathEditorPrefs EditorPrefs
//        {
//            get
//            {
//                return editorPrefs;
//            }
//        }


        private IPathInfo pathInfo;

        public IPathInfo GetPathInfo()
        {
            if (null == pathInfo)
            {
                pathInfo = new PathInfo(this);
            }
            return pathInfo;
        }

        // TODO how to clean up references???
        public int GetReferentCount()
        {
            return referents.Count;
        }

        public UnityEngine.Object GetReferent(int index)
        {
            return referents [index];
        }

        public void SetReferent(int index, UnityEngine.Object obj)
        {
            referents [index] = obj;
        }

        public int AddReferent(UnityEngine.Object obj)
        {
            referents.Add(obj);
            return referents.Count - 1;
        }

        public void RemoveReferent(int index)
        {
            referents.RemoveAt(index);
        }

        public bool PointsDirty {
            get {
                return pathPointsDirty;
            }
        }

        public bool Frozen {
            get {
                return this.frozenStatus == PathStatus.Frozen;
            }
        }

        public PathStatus FrozenStatus {
            set {
                this.frozenStatus = value;
            }
            get {
                return this.frozenStatus;
            }
        }

        public ParameterStore EditorParameters {
            get {
                return new ParameterStore(this.parameterStore, "Editor.");
            }
        }

        private void FireChangedEvent()
        {
            if (null != Changed)
            {
                Debug.Log("FireChangedEvent: " + Changed.GetInvocationList().Length);

                Changed(this, EventArgs.Empty);
            }
        }

        //void PathGeneratorModified(IPathGenerator pathGenerator);
        //IPathGenerator GetPathGenerator();
        
        public abstract bool IsLoop();
        
        //int GetSegmentCount();
        //int GetResolution();
        
//      public abstract PathPoint[] GetAllPoints();
//      public abstract int GetPointCount();
//      public abstract PathPoint GetPointAtIndex(int index);

        protected abstract PathPoint[] DoGetPathPoints(out int outputFlags);

        public void ForceUpdatePathPoints()
        {
            this.pathPointsDirty = true;

            UpdatePathPoints(true);
        }

        public void UpdatePathPoints(bool manualRefresh)
        {
            bool doRefresh = frozenStatus == PathStatus.Dynamic || (manualRefresh && frozenStatus == PathStatus.ManualRefresh);
            if (doRefresh && (null == pathPoints || pathPointsDirty))
            {

                int flags;
                PathPoint[] pp = DoGetPathPoints(out flags);
                this.rawPathPointFlags = flags;

                pp = PathModifierUtil.RunPathModifiers(new PathModifierContext(this, flags), 
                                                       pp, ref flags, true);


                this.pathPointFlags = flags;
                this.pathPoints = new List<PathPoint>(pp);
                pathPointsDirty = false;
                FireChangedEvent();

            } else if (pathPoints == null)
            {
                pathPoints = new List<PathPoint>();
            }

        }

        public PathPoint[] GetAllPoints()
        {
            UpdatePathPoints(false);
            return pathPoints.ToArray();
        }
        
        public int GetPointCount()
        {
            UpdatePathPoints(false);
            return pathPoints.Count;
        }
        
        public PathPoint GetPointAtIndex(int index)
        {
            UpdatePathPoints(false);
            return pathPoints [index];
        }

        public int GetOutputFlags()
        {
            UpdatePathPoints(false);
            return pathPointFlags;
        }

        public int GetOutputFlagsBeforeModifiers()
        {
            UpdatePathPoints(false);
            return rawPathPointFlags;
        }

        public ParameterStore GetParameterStore()
        {
            return parameterStore;
        }

        public void OnBeforeSerialize()
        {
            parameterStore.OnBeforeSerialize();
        }

        public void OnAfterDeserialize()
        {
            parameterStore.OnAfterDeserialize();

            // Materialize PathModifiers
            GetPathModifierContainer().LoadPathModifiers(parameterStore);


        }

        public void PathPointsChanged()
        {
            pathPointsDirty = true;
            OnPathPointsChanged();
            FireChangedEvent();
        }

        public void PathModifiersChanged()
        {
            pathPointsDirty = true;
            OnPathModifiersChanged();
            GetPathModifierContainer().SavePathModifiers(parameterStore);


            FireChangedEvent();

        }

        public virtual void OnPathModifiersChanged()
        {

        }

        public virtual void OnPathPointsChanged()
        {

        }

        protected virtual DefaultPathModifierContainer CreatePathModifierContainer()
        {
            return CreatePathModifierContainer(null);
        }

        protected DefaultPathModifierContainer CreatePathModifierContainer(DefaultPathModifierContainer.SetPathPointsDelegate setPathPointsFunc)
        {
            DefaultPathModifierContainer pmc = new DefaultPathModifierContainer(
                GetPathInfo,
                PathModifiersChanged,
                PathPointsChanged,
                DoGetPathPoints,
                () => {
                this.pathPointsDirty = true;},
                setPathPointsFunc,
                 this);
       
            return pmc;
        }

        public DefaultPathModifierContainer GetPathModifierContainer()
        {
            if (null == pathModifierContainer)
            {
                pathModifierContainer = CreatePathModifierContainer();
            }
            return pathModifierContainer;
        }
       
        public abstract int GetControlPointCount();

        public abstract Vector3 GetControlPointAtIndex(int index);

        public abstract void SetControlPointAtIndex(int index, Vector3 pt);

        void OnDrawGizmos()
        {
//            Debug.Log("Path: OnDrawGizmos");
            // Draw the actual path:
            PathPoint[] pp = GetAllPoints();
            //Vector3[] transformedPoints = new Vector3[pp.Length];


            // Draw the actual (final) path:
            Gizmos.color = PathGizmoPrefs.FinalPathLineColor;
            for (int i = 1; i < pp.Length; i++)
            {
                Vector3 pt0 = transform.TransformPoint(pp [i - 1].Position);
                Vector3 pt1 = transform.TransformPoint(pp [i].Position);
                Gizmos.DrawLine(pt0, pt1);
            }
            if (IsLoop() && pp.Length > 1)
            {
                // Connect last and first points:
                Vector3 pt0 = transform.TransformPoint(pp [0].Position);
                Vector3 pt1 = transform.TransformPoint(pp [pp.Length - 1].Position);
                Gizmos.DrawLine(pt0, pt1);
            }

            // Direction Vectors (Forward, Right and Up) and point markers
            Color upVectorColor = PathGizmoPrefs.UpVectorColor;
            Color dirVectorColor = PathGizmoPrefs.DirVectorColor;
            Color rightVectorColor = PathGizmoPrefs.RightVectorColor;

            Color pointMarkerColor = PathGizmoPrefs.FinalPathPointMarkerColor;
            Color firstPointMarkerColor = PathGizmoPrefs.FinalPathFirstPointMarkerColor;

            float upVectorLength = PathGizmoPrefs.UpVectorLength;
            float dirVectorLength = PathGizmoPrefs.DirVectorLength;
            float rightVectorLength = PathGizmoPrefs.RightVectorLength;

            float pointMarkerSize = PathGizmoPrefs.FinalPathPointMarkerSize;
            float firstPointMarkerSize = PathGizmoPrefs.FinalPathFirstPointMarkerSize;

            // TODO transform directions etc!
            for (int i = 0; i < pp.Length; i++)
            {
                Vector3 pt = transform.TransformPoint(pp [i].Position);

                // Draw dir vector
                if (pp [i].HasDirection)
                {
                    Gizmos.color = dirVectorColor;
                    Gizmos.DrawLine(pt, pt + pp [i].Direction * dirVectorLength);
                }

                // Draw up vector
                if (pp [i].HasUp)
                {
                    Gizmos.color = upVectorColor;
                    Gizmos.DrawLine(pt, pt + pp [i].Up * upVectorLength);
                }

                // Draw ortho (right) vector
                if (pp [i].HasRight)
                { 
                    Gizmos.color = rightVectorColor;
                    Gizmos.DrawLine(pt, pt + pp [i].Right * rightVectorLength);
                }



                Gizmos.color = (i == 0) ? firstPointMarkerColor : pointMarkerColor;
                Gizmos.DrawSphere(pt, (i == 0) ? firstPointMarkerSize : pointMarkerSize);

            }
            
            
            //          // Draw handles
            //          
            //          for (int i = 0; i < pp.Length; i++) {
            //              float worldHandleSize = HandleUtility.GetHandleSize(transformedPoints[i]);
            //              float handleSize, pickSize;
            //              
            //              handleSize = Constants.controlPointHandleSize * worldHandleSize;
            //              pickSize = Constants.controlPointPickSize * worldHandleSize;
            //              
            //              Handles.Button(transformedPoints[i], transform.rotation, 
            //                             handleSize, pickSize, 
            //                             Handles.DotCap);
            //              
            //          }
        }
    }


}
