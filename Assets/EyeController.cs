using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class EyeController : MonoBehaviour
{

	public Transform target;
	public bool copyTargetRotation;
	private Quaternion originalRotation;

	public Vector3 rotationOffset;

	void Reset ()
	{
		if (null != transform.parent) {
			rotationOffset = transform.parent.rotation.eulerAngles;
		} else {
			rotationOffset = Vector3.zero;
		}
	}
	void OnEnable ()
	{
		this.originalRotation = transform.localRotation;
	}
	void OnDisable ()
	{
		transform.localRotation = this.originalRotation;
	}
	
	// Update is called once per frame
	void LateUpdate ()
	{
		if (null != target) {

			if (copyTargetRotation) {
				transform.localRotation = target.localRotation;
			} else {
				Vector3 lookDir = target.position - transform.position;
				Quaternion lookRot = Quaternion.LookRotation (lookDir, Vector3.up);
				transform.rotation = lookRot;
				transform.Rotate (rotationOffset);
			}
		}
	}
}
