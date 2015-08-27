using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

using Util;

namespace Paths
{

	// TODO we don't really need the IPath interface since we're always referring to Path
	// (which is a GameObject)
	public class SimplePathModifierContainer : IPathModifierContainer
	{
        
		private List<IPathModifier> pathModifiers = new List<IPathModifier> ();
		private IReferenceContainer referenceContainer;
        
		public SimplePathModifierContainer ()
		{
            
		}
        
		public IPathModifier[] GetPathModifiers ()
		{
			return pathModifiers.ToArray ();
		}
        
		public void AddPathModifer (IPathModifier pm)
		{
			pathModifiers.Add (pm);
			pm.Attach (this);
		}
        
		public void InsertPathModifer (int index, IPathModifier pm)
		{
			pathModifiers.Insert (index, pm);
		}
        
		public void RemovePathModifer (int index)
		{
			pathModifiers [index].Detach ();

			pathModifiers.RemoveAt (index);
		}
        
		public int IndexOf (IPathModifier pm)
		{
			return pathModifiers.IndexOf (pm);
		}
        
		public bool IsSupportsApplyPathModifier ()
		{
			return false;
		}
        
		public void ApplyPathModifier (int index)
		{
			throw new NotImplementedException ();
		}
        
		public IReferenceContainer GetReferenceContainer ()
		{
			return referenceContainer;
		}

		public void SetReferenceContainer (IReferenceContainer container)
		{
			this.referenceContainer = container;
		}
	}

	public class DefaultPathModifierContainer : IPathModifierContainer
	{
		private List<IPathModifier> pathModifierInstances = new List<IPathModifier> ();

		public delegate IPathInfo GetPathInfoDelegate ();

		public delegate void PathModifiersChangedDelegate ();

		public delegate void PathPointsChangedDelegate ();

		public delegate PathPoint[] DoGetPathPointsDelegate (out int ppFlags);

		public delegate void SetPathPointsDirtyDelegate ();

		public delegate void SetPathPointsDelegate (PathPoint[] points);

		// TODO maybe we should replace these delegates with an interface? (not SetPathPointsDelegate?)
		private GetPathInfoDelegate GetPathInfo;
		private PathModifiersChangedDelegate PathModifiersChanged;
		private PathPointsChangedDelegate PathPointsChanged;
		private DoGetPathPointsDelegate DoGetPathPoints;
		private SetPathPointsDirtyDelegate SetPathPointsDirty;
		private SetPathPointsDelegate SetPathPoints;
		private IReferenceContainer referenceContainer;

		//private Action<PathPoint[]> DoGetPathPoints;

		public DefaultPathModifierContainer (GetPathInfoDelegate getPathInfoFunc,
                                             PathModifiersChangedDelegate pathModifiersChangedFunc, 
                                            PathPointsChangedDelegate pathPointsChangedFunc,
                                            DoGetPathPointsDelegate doGetPathPointsFunc,
                                            SetPathPointsDirtyDelegate setPathPointsDirtyFunc,
                                            SetPathPointsDelegate setPathPointsFunc,
                                            IReferenceContainer referenceContainer)
		{
			this.GetPathInfo = getPathInfoFunc;
			this.PathModifiersChanged = pathModifiersChangedFunc;
			this.PathPointsChanged = pathPointsChangedFunc;
			this.DoGetPathPoints = doGetPathPointsFunc;
			this.SetPathPointsDirty = setPathPointsDirtyFunc;
			this.SetPathPoints = setPathPointsFunc;
			this.referenceContainer = referenceContainer;
		}

		public void LoadPathModifiers (ParameterStore parameterStore)
		{
			pathModifierInstances = PathModifierUtil.LoadPathModifiers (parameterStore);
			foreach (IPathModifier pm in pathModifierInstances) {
				pm.Attach (this);
			}
//            PathModifierContext pmc = new PathModifierContext(this, 0);
			RegisterListenerOnIncludedPaths ();
		}

		public void SavePathModifiers (ParameterStore parameterStore)
		{
			PathModifierUtil.SavePathModifiers (parameterStore, pathModifierInstances);
//            PathModifierContext pmc = new PathModifierContext(this, 0);
			RegisterListenerOnIncludedPaths ();

		}

		public PathPoint[] RunPathModifiers (PathModifierContext context, PathPoint[] pp, ref int flags)
		{
			return RunPathModifiers (context, pp, ref flags, true);
		}

		public PathPoint[] RunPathModifiers (PathModifierContext context, PathPoint[] pp, ref int flags, bool fixResultFlags)
		{
			return PathModifierUtil.RunPathModifiers (context, pp, ref flags, fixResultFlags);
		}

		void RegisterListenerOnIncludedPaths ()
		{
			// Do we have included paths?
			foreach (IPathModifier pm in pathModifierInstances) {
				if (!pm.IsEnabled ()) {
					continue;
				}
				Path[] depPaths = pm.GetPathDependencies ();
				foreach (Path p in depPaths) {
					RegisterListenerOnIncludedPath (p);
				}
			}
		}

		void IncludedPathChanged (object sender, EventArgs e)
		{
			Debug.Log ("Included Path Changed: sender=" + sender);
			this.PathPointsChanged ();
		}

		void RegisterListenerOnIncludedPath (Path p)
		{
			PathChangedEventHandler d = new PathChangedEventHandler (IncludedPathChanged);
			p.Changed -= d;
			p.Changed += d;
		}

		void UnregisterListenerOnIncludedPath (Path p)
		{
			PathChangedEventHandler d = new PathChangedEventHandler (IncludedPathChanged);
			p.Changed -= d;
		}

		public IPathModifier[] GetPathModifiers ()
		{
			return pathModifierInstances.ToArray ();
		}
        
		public void AddPathModifer (IPathModifier pm)
		{
			pathModifierInstances.Add (pm);
			pm.Attach (this);
			PathModifiersChanged ();
		}
        
		public void InsertPathModifer (int index, IPathModifier pm)
		{
			pathModifierInstances.Insert (index, pm);
			pm.Attach (this);
			PathModifiersChanged ();
		}
        
		public void RemovePathModifer (int index)
		{
			pathModifierInstances [index].Detach ();
			pathModifierInstances.RemoveAt (index);
			PathModifiersChanged ();
		}
        
		public int IndexOf (IPathModifier pm)
		{
			return pathModifierInstances.IndexOf (pm);
		}
        
		public bool IsSupportsApplyPathModifier ()
		{
			return null != SetPathPoints;
		}

		public void ApplyPathModifier (int index)
		{
			SimplePathModifierContainer pmc = new SimplePathModifierContainer ();
			pmc.SetReferenceContainer (GetReferenceContainer ());
			for (int i = 0; i <= index; i++) {
				IPathModifier pm = pathModifierInstances [i];
				pmc.AddPathModifer (pm);
			}
            
			int flags;
			PathPoint[] pp = DoGetPathPoints (out flags);
			//this.rawPathPointFlags = flags;

			// Create wrapper context that includes only the PathModifier to apply
			IPathInfo pathInfo = GetPathInfo ();

			PathModifierContext subContext = new PathModifierContext (pathInfo, pmc, flags);
			pp = PathModifierUtil.RunPathModifiers (subContext, pp, ref flags, true);
			//this.pathPointFlags = flags;
			//this.pathPoints = new List<PathPoint>(pp);
			SetPathPointsDirty ();
            
			// Convert to Control Points
			SetPathPoints (pp);
            
			for (int i = index; i >= 0; i--) {
				pathModifierInstances.RemoveAt (i);
			}
			PathModifiersChanged ();
            
		}

		public IReferenceContainer GetReferenceContainer ()
		{
			return referenceContainer;
		}
	}
    
}
