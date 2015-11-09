using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using Paths.MeshGenerator.Extruder.Model;



//[ExecuteInEditMode]
public class MyObject : MonoBehaviour, IDieModelContainer
{
	public DieModel dieModel;

	public DieModel GetDieModel ()
	{
		return dieModel;
	}
//	void OnEnable ()
//	{
//		dieModel.Changed += DieModelChanged;
//	}
//	void OnDisable() {
//		dieModel.Changed -= DieModelChanged;
//	}
//
//

	// Use this for initialization
	void Start ()
	{
	
	}
	
	// Update is called once per frame
	void Update ()
	{
	
	}

	void OnDrawGizmos ()
	{
//		for (int i = 0; i < points.Count; i++) {
//			Vector3 pt = new Vector3 ();
//			Gizmos.DrawLine (Vector3.zero, pt);
//		}
	}
}
