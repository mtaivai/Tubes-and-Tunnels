// /*
//  * Copyright (C) 2015 Mikko Taivainen <mikko.taivainen@gmail.com>
//  */

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Paths.MeshGenerator
{
	[System.Serializable]
	public class HierarchicalObjectContainer
	{
		[SerializeField]
		public bool
			createContainerForChildren = true;

		[SerializeField]
		private GameObject
			childContainer;

		[SerializeField]
		private List<GameObject>
			ownedLeafs = new List<GameObject> ();
	
		[SerializeField]
		public string
			childContainerName;

		[SerializeField]
		private HideFlags
			containerHideFlags;

		[SerializeField]
		private HideFlags
			leafHideFlags;

		public static readonly HideFlags AllowedHideFlags = 
		HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.NotEditable;

		public HierarchicalObjectContainer ()
		{
		}
		public HierarchicalObjectContainer (string childContainerName)
		{
			this.childContainerName = childContainerName;
		}

		public HideFlags ContainerHideFlags {
			get {
				return containerHideFlags;
			}
			set {
				HideFlags previous = this.containerHideFlags;
				this.containerHideFlags = AllowedHideFlags & value;
				if (this.containerHideFlags != previous) {
					ApplyHideFlags ();
				}
			}
		}
		public HideFlags LeafHideFlags {
			get {
				return leafHideFlags;
			}
			set {
				HideFlags previous = this.leafHideFlags;
				this.leafHideFlags = AllowedHideFlags & value; 
				if (this.leafHideFlags != previous) {
					ApplyHideFlags ();
				}
			}
		}

		public void Reset ()
		{
			//childContainerName = "MeshColliders";
			createContainerForChildren = true;

			ContainerHideFlags = HideFlags.None;
			LeafHideFlags = HideFlags.None;
		}

		void ApplyHideFlags ()
		{
			if (createContainerForChildren && null != childContainer) {
				childContainer.hideFlags = ContainerHideFlags;
			}
			foreach (GameObject lo in ownedLeafs) {
				if (null != lo) {
					lo.hideFlags = LeafHideFlags;
				}
			}
		}

		public delegate void InitChildObjectDelegate (int index,GameObject childObject,object context);

		public void CreateChildren (GameObject owner, Func<int> getChildCountFunc, InitChildObjectDelegate initChildAction, object context)
		{
			// this / MeshColliders / MeshCollider<n>

			GameObject oldContainerToDestroy = null;

			// Do we already have the container object?
			if (null == childContainer
				|| (createContainerForChildren && childContainer == owner)) {
				if (createContainerForChildren) {
					childContainer = new GameObject (childContainerName);
					childContainer.transform.SetParent (owner.transform, false);
				} else {
					childContainer = owner;
				}
			} else if (childContainer != owner) {
				if (!createContainerForChildren) {
					// Destroy the previous container, but not yet...
					oldContainerToDestroy = childContainer;
					childContainer = owner;
				}

				if (childContainer.name != childContainerName) {
					childContainer.name = childContainerName;
				}
			}
			if (Util.StringUtil.IsEmpty (childContainer.name)) {
				childContainer.name = "Children";
			}

			if (childContainer != owner) {
				childContainer.hideFlags = ContainerHideFlags;
			}

			// Create grandchildren:
			int childCount = getChildCountFunc ();
		
			// Delete existing owned leaf objects:
			foreach (GameObject existing in ownedLeafs) {
				if (null != existing) {
					existing.transform.SetParent (null);
					GameObject.DestroyImmediate (existing);
				}
			}
			ownedLeafs.Clear ();
		
			for (int i = 0; i < childCount; i++) {
				GameObject c = new GameObject (childContainerName + "_Child" + (i + 1));
				ownedLeafs.Add (c);
				c.hideFlags = LeafHideFlags;
				c.transform.SetParent (childContainer.transform, false);
				initChildAction (i, c, context);
			
			}

			if (null != oldContainerToDestroy) {
				// Destroy the old container object, but only if it doesn't have
				// children or any Components left (we have already destroyed components
				// owned by us). Note that we allow one component, which we always have: Transform
				int oldChildCount = oldContainerToDestroy.transform.childCount;
				int oldComponentCount = oldContainerToDestroy.GetComponents<Component> ().Length;
				if (oldChildCount == 0 && oldComponentCount == 1) {
					GameObject.DestroyImmediate (oldContainerToDestroy);

				}

			}
			//		for (int i = 0; i < transform.childCount; i++) {
			//			transform.GetChild (i).hideFlags = HideFlags.None;
			//		}
			//		transform.childCount
		
		}

	}
}
