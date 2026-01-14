using UnityEngine;


public class ArcherTower : MonoBehaviour {
	public Transform origin;

	public float range = 20f;
	public LayerMask enemyLayer;
	public Transform firePoint;
	public float arrowSpeed;
	public GameObject arrowPrefab;

	void Update() {
		DetectEnemies();
	}

	void DetectEnemies() {
		Collider[] hits = Physics.OverlapSphere(
			transform.position,
			range,
			enemyLayer
		);

		if (hits.Length == 0) { return; }

		Transform closestEnemy = null;
		float closestDistance = Mathf.Infinity;

		foreach (Collider hit in hits) {
			float dist = Vector3.Distance(transform.position, hit.transform.position);
			if (dist < closestDistance) {
				closestDistance = dist;
				closestEnemy = hit.transform;
			}
		}

		if (closestEnemy != null) {
			Shoot(closestEnemy);
		}

	}

	void Shoot(Transform target) {
		if (Tools.SolveBallisticArc(
			firePoint.position,
			target.position,
			arrowSpeed,
			Mathf.Abs(Physics.gravity.y),
			out Vector3 dir
    	))
		{
			GameObject arrow = Instantiate(arrowPrefab, firePoint.position, Quaternion.LookRotation(dir));
			Rigidbody rb = arrow.GetComponent<Rigidbody>();
			rb.linearVelocity = dir * arrowSpeed;
    	}
	}
}
