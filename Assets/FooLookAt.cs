using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class FooLookAt : MonoBehaviour
{

	public Transform target;
	public bool copyTargetRotation;

	// Use this for initialization
	void Start ()
	{
	
	}
	
	// Update is called once per frame
	void LateUpdate ()
	{
	
		if (null != target) {
			if (copyTargetRotation) {
				transform.localRotation = target.localRotation;
			} else {
				transform.LookAt (target.position);
			}
//			transform.rotation = Quaternion.identity;
		}

	}
}
