using UnityEngine;
using System.Collections;

using Paths;
public class RollingBallController : MonoBehaviour
{

//	public PathSelectorObject pathSelectorObject;
	public PathSelector pathSelector;

//	private PathPositionLookup ppLookup;

//	private Vector3 currentForceDir;
//	private int nextPointIndex;
//
	private float pathPosition;

	public float PathPosition {
		get {
			return pathPosition;
		}
	}

	// Use this for initialization
	void Start ()
	{
		//ppLookup = new PathFlyer.PathPositionLookup ();
		//ppLookup.G
//		currentForceDir = Vector3.zero;
//		nextPointIndex = -1;

		playerUp = Vector3.up;
		playerFwd = Vector3.forward;

		Rigidbody rigidbody = GetComponent<Rigidbody> ();
		rigidbody.maxAngularVelocity = 50f;

		pathPosition = 0.0f;

	}

	private Vector3 playerUp = Vector3.up;
	private Vector3 playerFwd = Vector3.forward;

	private bool holdingTight = false;

	// Update is called once per frame
	void FixedUpdate ()
	{
	
		PathPoint[] pathPoints = pathSelector.PathData.GetAllPoints ();
		int nearestIndex = FindNearestNextPathPointIndex (pathPoints);
		PathPoint nearestPoint = pathPoints [nearestIndex];
		this.pathPosition = (nearestPoint.DistanceFromBegin - (nearestPoint.Position - transform.position).magnitude) / pathSelector.PathData.GetTotalDistance ();


		//Debug.DrawLine (transform.position, nearestPoint.Position, Color.white);

		// Now we have the nearest next point... let's look to distance:
		int farthestVisiblePointIndex = nearestIndex;
		for (int i = 0; i < pathPoints.Length; i++) {
			int pointIndex = nearestIndex + i;
			if (pointIndex >= pathPoints.Length) {
				pointIndex -= pathPoints.Length;
			}
			RaycastHit hitInfo;
			Vector3 rayDir = (pathPoints [pointIndex].Position - transform.position);
			float maxDist = rayDir.magnitude;
			rayDir.Normalize ();

			Ray ray = new Ray (transform.position, rayDir);
			Debug.DrawRay (transform.position, rayDir, Color.yellow, 0f, false);
			if (Physics.Raycast (ray, out hitInfo, maxDist)) {
				// we hit something
				// TODO at the moment the only thing we can hit on is the track but this will change!
				//Debug.Log ("HIT: " + hitInfo.collider);
				break;
			} else {
				farthestVisiblePointIndex = pointIndex;
			}
		}
		nearestIndex = farthestVisiblePointIndex;
		nearestPoint = pathPoints [nearestIndex];
		Debug.DrawLine (transform.position, nearestPoint.Position, Color.red);


		//Debug.Log ("pathPosition: " + pathPosition);

		// Try to keep the guy standing straight
		
		
		Vector3 playerTargetDir = (nearestPoint.Position - transform.position).normalized;




//		Vector3 forceDir = (nearestPoint.Position - transform.position).normalized;
		GameObject charInBall = GameObject.FindGameObjectWithTag ("CharacterInBall");

		Rigidbody rigidbody = GetComponent<Rigidbody> ();

		float accelerateAmount = Input.GetAxis ("Vertical");
		float turnAmount = Input.GetAxis ("Horizontal");



		if (holdingTight) {
			holdingTight = rigidbody.angularVelocity.magnitude > 15.0f;
		} else {
			holdingTight = rigidbody.angularVelocity.magnitude > 20.0f;
		}


		if (!holdingTight && turnAmount != 0.0f) {
			playerTargetDir = Quaternion.AngleAxis (turnAmount * 90f, playerUp) * playerTargetDir;
			//			playerFwd
		}

		playerUp = Vector3.Lerp (playerUp, Vector3.up, Time.deltaTime * 3f);
		playerFwd = Vector3.Lerp (playerFwd, playerTargetDir, Time.deltaTime * 3f);



		Vector3.OrthoNormalize (ref playerUp, ref playerFwd);
		Vector3 right = Quaternion.AngleAxis (90f, playerUp) * playerFwd;
		
		
		//		charInBall.transform.position = transform.tra;
		

		Debug.Log ("AV.z: " + rigidbody.angularVelocity.x);


		if (!holdingTight) {
			if (accelerateAmount != 0.0f) {
				rigidbody.AddTorque (right * accelerateAmount * 200.0f * Time.deltaTime);

			} else {
			}
		}


		// TODO enable three lines for first person view!
		//		GameObject camObj = GameObject.FindGameObjectWithTag ("MainCamera");
		//		camObj.transform.position = transform.position;
		//		camObj.transform.LookAt (transform.position + playerFwd, playerUp);
		//		Debug.DrawLine (transform.position, transform.position + playerUp * 5.0f, Color.green);
		Debug.DrawLine (transform.position, transform.position + playerFwd * 5.0f, Color.blue);
		//		Debug.DrawLine (transform.position, transform.position + right * 5.0f, Color.red);


		Animator anim = charInBall.GetComponent<Animator> ();
		anim.SetBool ("HoldingTight", holdingTight);
		anim.SetFloat ("FwdSpeed", rigidbody.angularVelocity.magnitude);

		if (holdingTight) {
			// Can't stand up; roll with the ball!
			charInBall.transform.position = transform.TransformPoint (0f, -0.5f, 0f);
			charInBall.transform.rotation = Quaternion.Lerp (charInBall.transform.rotation, transform.rotation, Time.deltaTime * 25f);
		} else {
			// Standing up, walking / running
			charInBall.transform.rotation = Quaternion.Lerp (charInBall.transform.rotation, Quaternion.LookRotation (playerFwd, playerUp), Time.deltaTime * 25f);
			charInBall.transform.position = transform.position - new Vector3 (0f, 0.5f, 0f);

		}

//
//
//		Vector3 horizPt0 = new Vector3 (-5f, 0f, 0f);
//		Vector3 horizPt1 = new Vector3 (5f, 0f, 0f);
//		transform.TransformVector
//		Debug.DrawLine (transform.TransformPoint (horizPt0), transform.TransformPoint (horizPt1), Color.yellow);
//
//		transform.rotat
		//Debug.DrawLine (transform.position, transform.position + forceDir * 5.0f, Color.yellow);


		Transform cubeTransform = transform.FindChild ("Cube");
		if (null != cubeTransform) {
			cubeTransform.position = new Vector3 (transform.position.x, transform.position.y + 1.3f, transform.position.z);
			cubeTransform.rotation = Quaternion.identity;
		}

		// Find next point:
		// TODO we should have a kd-map

//		Vector3 ourPos = transform.position;
//		PathPoint[] pathPoints = pathSelector.PathData.GetAllPoints ();
//		float shortestDist = (nextPointIndex >= 0 ? (pathPoints [nextPointIndex].Position - ourPos).magnitude : 0);
//		for (int i = 0; i < pathPoints.Length; i++) {
//			PathPoint pp = pathPoints [i];
//			Vector3 toPtVector = (pp.Position - ourPos);
//			Vector3 dirToPt = toPtVector.normalized;
//			float distToPt = toPtVector.magnitude;
//
//			if (-1 == nextPointIndex) {
//				shortestDist = distToPt;
//				nextPointIndex = i;
//				continue;
//			}
//
//			float angle = Vector3.Angle (pp.Direction, dirToPt);
//			if (angle <= 90.0f || angle >= 270.0f) {
//				if (distToPt < shortestDist) {
//					shortestDist = distToPt;
//					nextPointIndex = i;
//				}
//			} else {
//				// Behind our current pos
//			}
//		}

		//PathPoint[] points = path.GetA
	}

	private int FindNearestPathPointIndex (PathPoint[] pathPoints)
	{
		Vector3 ourPos = transform.position;
		int nearestIndex = 0;
		float shortestDist = (pathPoints [nearestIndex].Position - ourPos).magnitude;

		for (int i = 0; i < pathPoints.Length; i++) {
			PathPoint pp = pathPoints [i];
			Vector3 toPtVector = (pp.Position - ourPos);
//			Vector3 dirToPt = toPtVector.normalized;
			float distToPt = toPtVector.magnitude;
			if (distToPt < shortestDist) {
				shortestDist = distToPt;
				nearestIndex = i;
			}
		}
		return nearestIndex;
	}
	private int FindNearestNextPathPointIndex (PathPoint[] pathPoints)
	{
		int nearestIndex = FindNearestPathPointIndex (pathPoints);
		
		PathPoint nearestPoint = pathPoints [nearestIndex];
		Vector3 dirToNearestPoint = (nearestPoint.Position - transform.position).normalized;
		// Is behind? TODO should we iterate until dot >= 0 ?
		float dot = Vector3.Dot (nearestPoint.Direction, dirToNearestPoint);
		if (dot < 0.0f) {
			// Is behind, check next point:
			if (nearestIndex >= pathPoints.Length - 1) {
				// Last; if loop move to first
				if (pathSelector.PathData.GetPathInfo ().IsLoop ()) {
					nearestIndex = 0;
				}
			} else {
				nearestIndex++;
			}
			//			nearestPoint = pathPoints [nearestIndex];
			
		}
		return nearestIndex;
	}

	void OnCollisionEnter (Collision collision)
	{
		Debug.Log ("COLLISION");
		foreach (ContactPoint contact in collision.contacts) {
			//Debug.DrawRay (contact.point, contact.normal * 10.0f, Color.white, 1f);
//			contact.otherCollider.CompareTag("Ground");
//			Rigidbody r;r.
		}

//		collision.
		
//		if (collision.relativeVelocity.magnitude > 2)
//			audio.Play(); 
	}

	void OnDrawGizmos ()
	{
		// White line is the current force dir
//		Gizmos.color = Color.white;
//		Vector3 pt0 = transform.position;
//		Vector3 pt1 = pt0 + currentForceDir * 5.0f;
//		Gizmos.DrawLine (pt0, pt1);

		PathPoint[] pathPoints = pathSelector.PathData.GetAllPoints ();
		int nearestIndex = FindNearestNextPathPointIndex (pathPoints);

//		PathPoint nearestPoint = pathPoints [nearestIndex];

		//Vector3.OrthoNormalize(ref normal, ref tangent, ref binormal);
//		Vector3.P§
//		Vector3 floorPlane;
//		Physics.Raycast (



		// Magenta line from current pos to next target point:
		Gizmos.color = Color.gray;
		Gizmos.DrawLine (transform.position, pathPoints [nearestIndex].Position);

//		Vector3 localTargetPos = transform.InverseTransformPoint (pathPoints [nearestIndex].Position);
//		Vector3 fwdTargetPos = transform.TransformPoint (new Vector3 (localTargetPos.x, localTargetPos.y, localTargetPos.z));
//		Debug.Log ("Local t: " + localTargetPos);

		Gizmos.color = Color.red;
		//Gizmos.DrawLine (transform.position, fwdTargetPos);
//		for (int i = 0; i < pathPoints.Length; i++) {
//
//		}

		// Draw target dir to next point:
//		if (nextPointIndex >= 0) {
//			PathPoint pp = pathSelector.PathData.GetPointAtIndex (nextPointIndex);
//			pt1 = pp.Position;
//			Vector3 dirToNextPt = (pt1 - pt0).normalized;
//
//			Vector3 cross = Vector3.Cross (dirToNextPt, pp.Direction);
////			Debug.Log ("Cross: " + cross);
//
//			float angle = Vector3.Angle (dirToNextPt, pp.Direction);
//
//
//			if (angle <= 90.0f) {
//				Gizmos.color = Color.magenta;
//			} else {
//				Gizmos.color = Color.white;
//			}
//
//			Gizmos.DrawLine (pt0, pt1);
//
//			Gizmos.color = Color.cyan;
//			Gizmos.DrawLine (pt0, cross.normalized * 10f);
//		}
	}
}
