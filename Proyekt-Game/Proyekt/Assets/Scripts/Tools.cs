using UnityEngine;

public class Tools : MonoBehaviour {
	public static bool SolveStaticTargetBallisticArc(
		Vector3 origin,
		Vector3 target,
		float speed,
		float gravity,
		out Vector3 direction
	) {
		Vector3 delta = target - origin;
		Vector3 deltaXZ = new Vector3(delta.x, 0, delta.z);
		float x = deltaXZ.magnitude;
		float y = delta.y;

		float speed2 = speed * speed;
		float speed4 = speed2 * speed2;
		float g = gravity;

		float discriminant = speed4 - g * (g * x * x + 2 * y * speed2);

		if (discriminant < 0) {
			direction = Vector3.zero;
			return false; // target out of range
		}

		float sqrt = Mathf.Sqrt(discriminant);

		// LOW ARC (use +sqrt for high arc)
		float angle = Mathf.Atan((speed2 - sqrt) / (g * x));

		Vector3 dir = deltaXZ.normalized;
		direction = dir * Mathf.Cos(angle) + Vector3.up * Mathf.Sin(angle);

		return true;
	}
	public static Vector3 PredictTargetPosition(Transform target, float time) {
		Rigidbody rb = target.GetComponent<Rigidbody>();
		return target.position + rb.linearVelocity * time;
	}
	public static bool SolveMovingTargetBallisticArc(
		Vector3 origin,
		Vector3 targetPos,
		Vector3 targetVelocity,
		float speed,
		float gravity,
		int iterations,
		out Vector3 launchDir
	) {
		launchDir = Vector3.zero;

		float travelTime = (targetPos - origin).magnitude / speed;

		for (int i = 0; i < iterations; i++) {
			Vector3 predictedTarget = targetPos + (targetVelocity * travelTime);

			if (!SolveStaticTargetBallisticArc(
				origin,
				predictedTarget,
				speed,
				gravity,
				out launchDir))
				return false;
			
			Debug.Log(predictedTarget);
			//float horizontalDist =
			//	Vector3.ProjectOnPlane(predictedTarget - origin, Vector3.up).magnitude;

			float horizontalDist = (predictedTarget - origin).magnitude;

			float horizontalSpeed = speed * new Vector3(launchDir.x, 0f, launchDir.z).magnitude;
			travelTime = horizontalDist / horizontalSpeed;
			Debug.DrawLine(origin, predictedTarget, Color.red, 1.0f);
		}


	return true;
	}
	public static (Vector3, Quaternion) OrbitCamera(
			Vector3 position,
			Vector3 pivot,
			Vector3 axis,
			float angle
			)
	{
		Quaternion delta = Quaternion.AngleAxis(angle, axis);
		Vector3 newPos = pivot + delta * (position - pivot);
		Quaternion newRot = Quaternion.LookRotation(pivot - newPos, Vector3.up);
		return (newPos, newRot);
	}
}
