using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(CreateChildObjectsTest))]
public class CreateChildObjectsTestEditor : Editor
{

	public override void OnInspectorGUI ()
	{
		DrawDefaultInspector ();
		if (GUILayout.Button ("Create")) {
			DoCreate ();
		}
		//base.OnInspectorGUI ();
	}

	private void DoCreate ()
	{
		// this / MeshColliders / MeshCollider<n>
		CreateChildObjectsTest test = target as CreateChildObjectsTest;
		test.CreateChildren ();

	}
}
