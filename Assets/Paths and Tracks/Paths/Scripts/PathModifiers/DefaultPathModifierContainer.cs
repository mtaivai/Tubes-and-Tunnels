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
		private ParameterStore paramterStore;

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
		public IPathSnapshotManager GetPathSnapshotManager ()
		{
			return UnsupportedSnapshotManager.Instance;
		}
		public ParameterStore GetParameterStore ()
		{
			return null;
		}
		public void ConfigurationChanged ()
		{
			throw new NotImplementedException ();
		}
	}

	public class PathModifierContainerEvent : EventArgs
	{

	}

	public delegate void PathModifiersChangedHandler (object sender,PathModifierContainerEvent e);

	public class DefaultPathModifierContainer : IPathModifierContainer
	{
		private List<IPathModifier> pathModifierInstances = new List<IPathModifier> ();

		public delegate IPathInfo GetPathInfoDelegate ();

		public event PathModifiersChangedHandler PathModifiersChanged;


		public delegate PathPoint[] DoGetPathPointsDelegate (out int ppFlags);

		public delegate void SetPathPointsDelegate (PathPoint[] points);

		// TODO maybe we should replace these delegates with an interface? (not SetPathPointsDelegate?)
		private GetPathInfoDelegate GetPathInfo;
		private DoGetPathPointsDelegate DoGetPathPoints;
		private SetPathPointsDelegate SetPathPoints;

		private IReferenceContainer referenceContainer;
		private IPathSnapshotManager snapshotManager;
		private ParameterStore parameterStore;

		public DefaultPathModifierContainer (GetPathInfoDelegate getPathInfoFunc,
                                            DoGetPathPointsDelegate doGetPathPointsFunc,
                                            SetPathPointsDelegate setPathPointsFunc,
                                            IReferenceContainer referenceContainer,
		                                     IPathSnapshotManager snapshotManager,
		                                     ParameterStore parameterStore)
		{
			this.GetPathInfo = getPathInfoFunc;
			this.DoGetPathPoints = doGetPathPointsFunc;
			this.SetPathPoints = setPathPointsFunc;
			this.referenceContainer = referenceContainer;
			this.snapshotManager = snapshotManager;
			if (null == this.snapshotManager) {
				this.snapshotManager = UnsupportedSnapshotManager.Instance;
			}
			this.parameterStore = parameterStore;
		}

		private void LoadPathModifiers (ParameterStore parameterStore)
		{
			pathModifierInstances = PathModifierUtil.LoadPathModifiers (parameterStore);
			foreach (IPathModifier pm in pathModifierInstances) {
				pm.Attach (this);
			}
			RegisterListenerOnIncludedPaths ();
		}
		public void LoadConfiguration ()
		{
			LoadPathModifiers (GetParameterStore ());
		}

		private void SavePathModifiers (ParameterStore parameterStore)
		{
			PathModifierUtil.SavePathModifiers (parameterStore, pathModifierInstances);
			RegisterListenerOnIncludedPaths ();

		}
		public void SaveConfiguration ()
		{
			SavePathModifiers (GetParameterStore ());
		}


		public void ConfigurationChanged ()
		{
			SaveConfiguration ();
			PathModifierContainerEvent e = new PathModifierContainerEvent ();
			PathModifiersChanged (this, e);
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
			this.ConfigurationChanged ();
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
			ConfigurationChanged ();
		}
        
		public void InsertPathModifer (int index, IPathModifier pm)
		{
			pathModifierInstances.Insert (index, pm);
			pm.Attach (this);
			ConfigurationChanged ();
		}
        
		public void RemovePathModifer (int index)
		{
			pathModifierInstances [index].Detach ();
			pathModifierInstances.RemoveAt (index);
			ConfigurationChanged ();
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

			// Create wrapper context that includes only the PathModifier to apply
			IPathInfo pathInfo = GetPathInfo ();

			PathModifierContext subContext = new PathModifierContext (pathInfo, pmc, flags);
			pp = PathModifierUtil.RunPathModifiers (subContext, pp, ref flags, true);

			// Convert to Control Points
			SetPathPoints (pp);
            
			for (int i = index; i >= 0; i--) {
				pathModifierInstances.RemoveAt (i);
			}
			ConfigurationChanged ();
            
		}

		public IReferenceContainer GetReferenceContainer ()
		{
			return referenceContainer;
		}
		public IPathSnapshotManager GetPathSnapshotManager ()
		{
			return snapshotManager;
		}
		// TODO rename this method to SetPathSnapshotManager
		public void SetPathBranchManager (IPathSnapshotManager branchManager)
		{
			this.snapshotManager = branchManager;
		}
		public ParameterStore GetParameterStore ()
		{
			return parameterStore;
		}

	}
    
}
