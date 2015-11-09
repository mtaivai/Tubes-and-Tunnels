using UnityEngine;
using System.Collections;
using Paths;

public class RollingBallCam : MonoBehaviour
{

	public RollingBallController target;
	public PathSelector cameraPath;

	//private PathPositionLookup targetPathLookup;
	private PathPositionLookup cameraPathLookup; 
//	Vector3 lastKnownTargetPos;
//	Vector3 lastKnownTargetDir;

//	private float currentPathPosition;

	void Awake ()
	{
		Debug.Log ("AWAKE");
	}

	// Use this for initialization
	void Start ()
	{
//		lastKnownTargetPos = target.transform.position;
//		lastKnownTargetDir = Vector3.forward;
		cameraPathLookup = new PathPositionLookup (cameraPath.PathData);
//		currentPathPosition = 0.0f;

	}
	
	// Update is called once per frame
	void FixedUpdate ()
	{

		//Vector3 pos = cameraPathLookup.GetPathPointAtDistance (currentDist).Position;
		Camera cam = GetComponent<Camera> ();
		//cam.transform.position = Vector3.Lerp (cam.transform.position, pos, Time.deltaTime * 10f);

		Vector3 lookAtPos = target.transform.position;

		// Try to stay about n meters behind the ball
		// Find position position closest to the target distance
//
//		float targetDistance = 20.0f;
//
//		// TODO it should be enough to run the lookup on every Nth frame:
//		Paths.PathPoint[] pathPoints = cameraPath.PathPoints;
//		int nearestPpIndex = 0;
//		float nearestTargetDistDelta = 
//			Mathf.Abs ((pathPoints [nearestPpIndex].Position - lookAtPos).magnitude - targetDistance);
//		// TODO this is O(n) although theoretical best case is O(1) but it's unlikely to happen!
//		// TODO Could we use a kd map here
//		// TODO we should also take the "distance in path" in account; so that we don't drive the camera
//		// too far just to get few inches closer to the target...
//		for (int i = 0; i < pathPoints.Length; i++) {
//			float distToPp = (pathPoints [i].Position - lookAtPos).magnitude;
//			float deltaToTarget = distToPp - targetDistance;
//			if (Mathf.Approximately (0f, deltaToTarget)) {
//				// Close enough, i.e. an exact match!
//				nearestPpIndex = i;
//				nearestTargetDistDelta = deltaToTarget;
//				break;
//			} else if (Mathf.Abs (deltaToTarget) < nearestTargetDistDelta) {
//				// Remember this but keep on lookin'...
//				nearestTargetDistDelta = Mathf.Abs (deltaToTarget);
//				nearestPpIndex = i;
//			}
//		}



		//Debug.Log ("Nearest index: " + nearestPpIndex + "; dist: " + nearestTargetDistDelta);
		float speed = 2f;
		float followBehindDistance = 3f;
		float targetPathPosition = target.PathPosition * cameraPathLookup.GetTotalDistance () - followBehindDistance;

		PathPoint cameraTargetPp = cameraPathLookup.GetPathPointAtDistance (targetPathPosition);
		cam.transform.position = Vector3.Lerp (cam.transform.position, cameraTargetPp.Position, speed * Time.deltaTime);

//		cam.transform.position = Vector3.Lerp (cam.transform.position, pathPoints [nearestPpIndex].Position, speed * Time.deltaTime);
		//currentDist += speed * Time.deltaTime;

		cam.transform.rotation = 
			Quaternion.Lerp (
				cam.transform.rotation, 
				Quaternion.LookRotation ((lookAtPos - cam.transform.position).normalized),
				Time.deltaTime * 2f);

//		cam.transform.LookAt (lookAtPos); // TODO Lerp this!

//		Vector3 targetPos = target.transform.position;
//
//		transform.position = new Vector3 (targetPos.x, targetPos.y + 20.0f, targetPos.z);
		//transform.LookAt (new Vector3(transform.position.x, trans);
//		Vector3 targetVelocity = targetPos - lastKnownTargetPos;
//		Vector3 targetDir;
//		if (targetVelocity.magnitude < 0.1f) {
//			// Not moved enough
//			targetDir = lastKnownTargetDir;
//		} else {
//			targetDir = targetVelocity.normalized;
//			lastKnownTargetDir = targetDir;
//			lastKnownTargetPos = targetPos;
//		}
//		Vector3 newCamPos = targetPos + new Vector3 (targetDir.x, targetDir.y, targetDir.z).normalized * -10f;
//		transform.position = Vector3.Lerp (transform.position, newCamPos, Time.deltaTime);
//		transform.LookAt (targetPos);
	}
}
