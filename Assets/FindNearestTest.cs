using UnityEngine;
using System.Collections;
using Paths;

public class FindNearestTest : MonoBehaviour
{

	public PathSelector pathSelector;
	private KDTree<PathPoint> kdTree;

	[TextArea(10, 40)]
	public string
		Foo;

	// Use this for initialization
	void Start ()
	{

//		PathPoint[] pp = new PathPoint[9] {
//			new PathPoint (new Vector3 (-1, 1)),
//			new PathPoint (new Vector3 (0, 1)),
//			new PathPoint (new Vector3 (1, 1)),
//			new PathPoint (new Vector3 (-1, 0)),
//			new PathPoint (new Vector3 (0, 0)),
//			new PathPoint (new Vector3 (1, 0)),
//			new PathPoint (new Vector3 (-1, -1)),
//			new PathPoint (new Vector3 (0, -1)),
//			new PathPoint (new Vector3 (1, -1)),
//
//		};
//		kdTree = KDTree.Build (pp);
//		Foo = kdTree.ToString ();
//
//		Debug.Log ("Nearest: " + kdTree.FindNearest (new Vector3 (0.6f, 0.6f)).Position);
		PathPoint[] points = pathSelector.PathData.GetAllPoints ();
		kdTree = KDTree<PathPoint>.Build (points);

	}
	
	// Update is called once per frame
	void Update ()
	{
		PathPoint nearest = kdTree.FindNearest (transform.position);
		Debug.DrawLine (transform.position, nearest.Position, Color.green);
	}
}
