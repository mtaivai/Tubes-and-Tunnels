using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

using Util;

namespace Paths
{

	class PMCUtil
	{

		public static void FireLifecyleEvent (IPathModifier pm, PathModifierLifecycleEventArgs e)
		{
			if (null != pm && pm is IPathModifierLifecycleAware) {
				IPathModifierLifecycleAware pmla = (IPathModifierLifecycleAware)pm;
				DoFireLifecyleEvent (pmla, e);
			}
		}

		public static void DoFireLifecyleEvent (IPathModifierLifecycleAware pm, PathModifierLifecycleEventArgs e)
		{
			try {
				pm.HandleLifecycleEvent (e);
			} catch (Exception ex) {
				Debug.LogErrorFormat ("Catched an exception from HandleLifecycleEvent; phase={0}, pm={1}, exception={2}", e.Phase, pm, ex);
			}
		}

		public static void FireEvent (IPathModifierContainer c, IPathModifier pm, PathModifierLifecyclePhase phase)
		{
			if (null != pm && pm is IPathModifierLifecycleAware) {
				PathModifierLifecycleEventArgs e = new PathModifierLifecycleEventArgs (c, pm, phase);
				DoFireLifecyleEvent ((IPathModifierLifecycleAware)pm, e);
			}
		}
//		public static void FireCreateEvent (IPathModifierContainer c, IPathModifier pm)
//		{
//			FireEvent (c, pm, PathModifierLifecyclePhase.Create);
//		}
		public static void FireAttachEvent (IPathModifierContainer c, IPathModifier pm)
		{
			FireEvent (c, pm, PathModifierLifecyclePhase.Attach);
		}
		public static void FireDetachEvent (IPathModifierContainer c, IPathModifier pm)
		{
			FireEvent (c, pm, PathModifierLifecyclePhase.Detach);
		}
		public static void FireDestroyEvent (IPathModifierContainer c, IPathModifier pm)
		{
			FireEvent (c, pm, PathModifierLifecyclePhase.Destroy);
		}
	}

	// TODO we don't really need the IPath interface since we're always referring to Path
	// (which is a GameObject)
	public class SimplePathModifierContainer : IPathModifierContainer
	{
        
		private List<IPathModifier> pathModifiers = new List<IPathModifier> ();
		private IReferenceContainer referenceContainer;
		private ParameterStore parameterStore;

		public SimplePathModifierContainer ()
		{
            
		}

		public IPathModifier[] GetPathModifiers ()
		{
			return pathModifiers.ToArray ();
		}
        
		public void AddPathModifer (IPathModifier pm)
		{
			InsertPathModifier (pathModifiers.Count, pm);
		}
        
		public void InsertPathModifier (int index, IPathModifier pm)
		{
			pathModifiers.Insert (index, pm);
//			PMCUtil.FireCreateEvent (this, pm);
			PMCUtil.FireAttachEvent (this, pm);
		}
        
		public void RemovePathModifier (int index)
		{
			PMCUtil.FireDetachEvent (this, pathModifiers [index]);
			PMCUtil.FireDestroyEvent (this, pathModifiers [index]);

			pathModifiers.RemoveAt (index);
		}
		public void RemoveAllPathModifiers ()
		{
			for (int i = pathModifiers.Count - 1; i >= 0; i--) {
				RemovePathModifier (i);
			}
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
		public PathPoint[] RunPathModifiers (PathModifierContext context, PathPoint[] pp, ref int flags)
		{
			throw new NotImplementedException ();
		}
		public bool HasMessages (PathModifierMessageType messageType)
		{
			return false;
		}
//
//		public string[] GetCurrentErrors ()
//		{
//			throw new NotImplementedException ();
//		}
//
		public bool HasMessages (PathModifierMessageType messageType, IPathModifier pm)
		{
			return false;
		}

		public string[] GetCurrentMessages (PathModifierMessageType messageType, IPathModifier pm)
		{
			throw new NotImplementedException ();
		}
	}

	public class PathModifierContainerEvent : EventArgs
	{

	}

	public delegate void PathModifiersChangedHandler (PathModifierContainerEvent e);

	public class DefaultPathModifierContainer : IPathModifierContainer
	{
		private List<IPathModifier> pathModifierInstances = new List<IPathModifier> ();

		//public delegate IPathInfo GetPathInfoDelegate ();

		public event PathModifiersChangedHandler PathModifiersChanged;


		public delegate PathPoint[] DoGetPathPointsDelegate (out int ppFlags);

		//public delegate void SetPathPointsDelegate (PathPoint[] points);

		// TODO maybe we should replace these delegates with an interface? (not SetPathPointsDelegate?)
		private Func<IPathMetadata> GetPathMetadata;
		private Func<IPathInfo> GetPathInfo;
		private DoGetPathPointsDelegate DoGetPathPoints;
		private Action<PathPoint[]> SetPathPoints;

		private Func<IReferenceContainer> getReferenceContainer;
		private Func<IPathSnapshotManager> getSnapshotManager;
		private Func<ParameterStore> getParameterStore;

		private Dictionary<PathModifierMessageType, Dictionary<int, List<string> >> currentMessages = new Dictionary<PathModifierMessageType, Dictionary<int, List<string> >> ();


		public DefaultPathModifierContainer (Func<IPathInfo> getPathInfoFunc,
		                                     Func<IPathMetadata> getPathMetadataFunc,
                                            DoGetPathPointsDelegate doGetPathPointsFunc,
                                            Action<PathPoint[]> setPathPointsFunc,
		                                     Func<IReferenceContainer> getReferenceContainerFunc,
		                                     Func<IPathSnapshotManager> getSnapshotManagerFunc,
		                                     Func<ParameterStore> getParameterStoreFunc)
		{
			this.GetPathInfo = getPathInfoFunc;
			this.GetPathMetadata = getPathMetadataFunc;
			this.DoGetPathPoints = doGetPathPointsFunc;
			this.SetPathPoints = setPathPointsFunc;
			this.getReferenceContainer = getReferenceContainerFunc;
			this.getSnapshotManager = getSnapshotManagerFunc;
//			if (null == this.snapshotManager) {
//				this.snapshotManager = UnsupportedSnapshotManager.Instance;
//			}
			this.getParameterStore = getParameterStoreFunc;
		}

		private void LoadPathModifiers (ParameterStore parameterStore)
		{
			pathModifierInstances = PathModifierUtil.LoadPathModifiers (parameterStore);
			foreach (IPathModifier pm in pathModifierInstances) {
				PMCUtil.FireAttachEvent (this, pm);
			}
		}
		public void LoadConfiguration ()
		{
			ParameterStore store = GetParameterStore ();
			if (null != store) {
				LoadPathModifiers (store);
			}
		}

		private void SavePathModifiers (ParameterStore parameterStore)
		{
			PathModifierUtil.SavePathModifiers (parameterStore, pathModifierInstances);

		}
		public void SaveConfiguration ()
		{
			SavePathModifiers (GetParameterStore ());
		}


		public void ConfigurationChanged ()
		{
			SaveConfiguration ();
			PathModifierContainerEvent e = new PathModifierContainerEvent ();
			if (null != PathModifiersChanged) {
				PathModifiersChanged (e);
			}
		}

//		public PathPoint[] xxxRunPathModifiers (PathModifierContext context, PathPoint[] pp, ref int flags)
//		{
//			return xxxRunPathModifiers (context, pp, ref flags, true);
//		}

		public PathPoint[] RunPathModifiers (PathModifierContext context, PathPoint[] pp, ref int flags)
		{
			// TODO we don't need the Context!

			PathPoint[] points;
			try {
				points = DoRunPathModifiers (pp, false, ref flags, true);
			} catch (Exception e) {
				Debug.LogError ("PathModifier processing was aborted due to exception: " + e);
				AddMessage (PathModifierMessageType.Error, "PathModifier processing was aborted due to exception: " + e.Message);
				throw e;
			}
			return points;
		}

		public IPathModifier[] GetPathModifiers ()
		{
			return pathModifierInstances.ToArray ();
		}
        
		public void AddPathModifer (IPathModifier pm)
		{
			InsertPathModifier (pathModifierInstances.Count, pm);
		}
        
		public void InsertPathModifier (int index, IPathModifier pm)
		{
			pathModifierInstances.Insert (index, pm);
//			PMCUtil.FireCreateEvent (this, pm);
			PMCUtil.FireAttachEvent (this, pm);
			ConfigurationChanged ();
		}
        
		public void RemovePathModifier (int index)
		{
			DoRemovePathModifier (index, true);
		}
		private void DoRemovePathModifier (int index, bool notifyConfigurationChanged)
		{
			IPathModifier pmToRemove = pathModifierInstances [index];
			pathModifierInstances.RemoveAt (index);
			PMCUtil.FireDetachEvent (this, pmToRemove);
			PMCUtil.FireDestroyEvent (this, pmToRemove);
			if (notifyConfigurationChanged) {
				ConfigurationChanged ();
			}
		}
		public void RemoveAllPathModifiers ()
		{
			bool wasntEmpty = pathModifierInstances.Count > 0;
			for (int i = pathModifierInstances.Count - 1; i >= 0; i--) {
				DoRemovePathModifier (i, false);
			}
			if (wasntEmpty) {
				ConfigurationChanged ();
			}
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
//			IPathInfo pathInfo = GetPathInfo ();

//			PathModifierContext subContext = new PathModifierContext (pathInfo, pmc, flags);
			pp = DoRunPathModifiers (pp, false, ref flags, true);
			// TODO WHAT ABOUT POSSIBLE ERRORS???

			// Convert to Control Points
			SetPathPoints (pp);
            
			for (int i = index; i >= 0; i--) {
				pathModifierInstances.RemoveAt (i);
			}
			ConfigurationChanged ();
            
		}

		public IReferenceContainer GetReferenceContainer ()
		{
			return getReferenceContainer ();
		}
		public IPathSnapshotManager GetPathSnapshotManager ()
		{
			return getSnapshotManager ();
		}
		// TODO rename this method to SetPathSnapshotManager
//		public void SetPathBranchManager (IPathSnapshotManager branchManager)
//		{
//			this.snapshotManager = branchManager;
//		}
		public ParameterStore GetParameterStore ()
		{
			return getParameterStore ();
		}

		// TODO this could be public?
//		private static PathPoint[] RunPathModifiers (PathModifierContext context, PathPoint[] _pathPoints, bool protectInputPoints, ref int flags, bool fixResultFlags) {
//
//		}

		private PathPoint[] DoRunPathModifiers (PathPoint[] _pathPoints, bool protectInputPoints, ref int flags, bool fixResultFlags)
		{
			long startTicks = System.DateTime.Now.Ticks;
			
			PathPoint[] processedPoints;
			
			if (protectInputPoints) {
				processedPoints = new PathPoint[_pathPoints.Length];
				// We cant't just Array.Copy(...) the array because PathPoints are mutable and
				// any changes would still reflect the input array. We need to clone each point
				// as well...
				//				Array.Copy (_pathPoints, processedPoints, processedPoints.Length);
				int c = _pathPoints.Length;
				for (int i = 0; i < c; i++) {
					processedPoints [i] = new PathPoint (_pathPoints [i]);
				}
			} else {
				processedPoints = _pathPoints;
			}

			// TODO need to get parameters from the caller?
			Dictionary<string, object> parameters = new Dictionary<string, object> ();

			this.currentMessages.Clear ();

			bool processingAborted = false;
			IPathModifier[] modifiers = GetPathModifiers ();
			for (int pmIndex = 0; pmIndex < modifiers.Length; pmIndex++) {
				if (processingAborted) {
					break;
				}
				IPathModifier mod = modifiers [pmIndex];
				if (!mod.IsEnabled ()) {
					continue;
				}
				IPathInfo pathInfo = GetPathInfo ();

				PathModifierContext pmc = new PathModifierContext (pathInfo, this, GetPathMetadata (), flags, parameters);
				
				// Timing:
				
				long thisStartTicks = System.DateTime.Now.Ticks;
				
				// Do run the path modifier
				try {
					processedPoints = mod.GetModifiedPoints (processedPoints, pmc);
					pmc.Errors.ForEach ((msg) => AddMessage (PathModifierMessageType.Error, msg));
					pmc.Warnings.ForEach ((msg) => AddMessage (PathModifierMessageType.Warning, msg));
					pmc.Info.ForEach ((msg) => AddMessage (PathModifierMessageType.Info, msg));


					if (fixResultFlags) {
						bool gotNulls = false;
						int outputFlags = mod.GetOutputFlags (pmc);
						for (int i = 0; i < processedPoints.Length; i++) {
							PathPoint pp = processedPoints [i];
							if (null == pp) {
								gotNulls = true;
								processedPoints [i] = new PathPoint ();
								//pathPoints [i].Flags = outputFlags;
							} else {
								if (pp.Flags != outputFlags) {
									//								pathPoints [i].Flags = outputFlags;
									outputFlags &= processedPoints [i].Flags;
								}
							}
						}
						flags = outputFlags;
						if (gotNulls) {
							// TODO add to context warnings!
							Debug.LogWarning ("PathModifier " + PathModifierUtil.GetDisplayName (mod) + " (" + mod.GetType ().FullName + ") returned null point(s)");
						}
						
					}
					// input & (process | passthrough) | generate
					flags = (flags & (mod.GetPassthroughFlags (pmc) | mod.GetProcessFlags (pmc))) | mod.GetGenerateFlags (pmc);
					
					long thisEndTicks = System.DateTime.Now.Ticks;
					float thisDeltaTimeMs = (float)(thisEndTicks - thisStartTicks) / (float)System.TimeSpan.TicksPerMillisecond;

					string infoMsg = string.Format ("Running Pathmodifier '{0}' took {1:f1} ms", PathModifierUtil.GetDisplayName (mod), thisDeltaTimeMs);
					AddMessage (PathModifierMessageType.Info, pmIndex, infoMsg);
					Debug.Log (infoMsg);
				} catch (CircularPathReferenceException e) {
					processingAborted = true;

					string errorMsg = string.Format ("Circular Path cross reference was detected while running PathModifier \"{0}\"", PathModifierUtil.GetDisplayName (mod));
					AddMessage (PathModifierMessageType.Error, pmIndex, errorMsg);

					string logMsg = string.Format ("{0}; exception catched: {1} ", errorMsg, e);
					Debug.LogError (logMsg);
					continue;

				} catch (Exception e) {
					processingAborted = true;
					
					string errorMsg = string.Format ("An error occurred while processing PathModifier \"{0}\": {1}", PathModifierUtil.GetDisplayName (mod), e.Message);
					AddMessage (PathModifierMessageType.Error, pmIndex, errorMsg);
					
					string logMsg = string.Format ("{0}; exception catched: {1} ", errorMsg, e);
					Debug.LogError (logMsg);
				}
				
				
			}
			if (processingAborted) {
				Debug.LogError ("Processing of PathModifiers was aborted due to previous errors");
				processedPoints = new PathPoint[0];
				flags = 0;
			}
			
			long endTicks = System.DateTime.Now.Ticks;
			float deltaTimeMs = (float)(endTicks - startTicks) / (float)System.TimeSpan.TicksPerMillisecond;
//			Debug.Log ("Running " + modifiers.Length + " PathModifiers took " + deltaTimeMs + " ms");
			
			return processedPoints;
		}
		private void AddMessage (PathModifierMessageType messageType, string message)
		{
			AddMessage (messageType, -1, message);
		}

		private void AddMessage (PathModifierMessageType messageType, int pmIndex, string message)
		{

			if (pmIndex < 0) {
				// Add error to ALL
				IPathModifier[] modifiers = GetPathModifiers ();
				for (int i = 0; i < modifiers.Length; i++) {
					AddMessage (messageType, i, message);
				}
			} else {
				if (!currentMessages.ContainsKey (messageType)) {
					currentMessages [messageType] = new Dictionary<int, List<string>> ();
				}
				Dictionary<int, List<string>> idToMessages = currentMessages [messageType];

				if (!idToMessages.ContainsKey (pmIndex)) {
					idToMessages.Add (pmIndex, new List<string> ());
				}
				List<string> messages = idToMessages [pmIndex];
				if (!messages.Contains (message)) {
					messages.Add (message);
				}
			}
		}

		public bool HasMessages (PathModifierMessageType messageType)
		{
			if (currentMessages.ContainsKey (messageType)) {
				Dictionary<int, List<string>> messages = currentMessages [messageType];
				foreach (List<string> messageList in messages.Values) {
					if (messageList.Count > 0) {
						return true;
					}
				}
			}
			return false;
		}

//		public string[] GetCurrentErrors ()
//		{
//			// Collect all errors
//
//			throw new NotImplementedException ();
//		}
//
		public bool HasMessages (PathModifierMessageType messageType, IPathModifier pm)
		{
			if (currentMessages.ContainsKey (messageType)) {
				Dictionary<int, List<string>> messages = currentMessages [messageType];
				int index = IndexOf (pm);
				if (messages.ContainsKey (index)) {
					return messages [index].Count > 0;
				} else {
					return false;
				}
			} else {
				return false;
			}
		}

		public string[] GetCurrentMessages (PathModifierMessageType messageType, IPathModifier pm)
		{
			if (currentMessages.ContainsKey (messageType)) {
				Dictionary<int, List<string>> messages = currentMessages [messageType];
				int index = IndexOf (pm);
				if (messages.ContainsKey (index)) {
					return messages [index].ToArray ();
				} else {
					return new string[0];
				}
			} else {
				return new string[0];
			}
		}
	}
    
}
