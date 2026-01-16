using UnityEngine;

public class Tools : MonoBehaviour {
	public static bool SolveBallisticArc(
	    Vector3 origin,
	    Vector3 target,
	    float speed,
	    float gravity,
	    out Vector3 direction
	)
	{
	    Vector3 delta = target - origin;
	    Vector3 deltaXZ = new Vector3(delta.x, 0, delta.z);
	    float x = deltaXZ.magnitude;
	    float y = delta.y;

	    float speed2 = speed * speed;
	    float speed4 = speed2 * speed2;
	    float g = gravity;

	    float discriminant = speed4 - g * (g * x * x + 2 * y * speed2);

	    if (discriminant < 0)
	    {
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
	public Vector3 PredictTargetPosition(Transform target, float time) {
		Rigidbody rb = target.GetComponent<Rigidbody>();
		return target.position + rb.linearVelocity * time;
	}
}
