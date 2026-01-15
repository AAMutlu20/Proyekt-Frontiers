using UnityEngine;


public class ArcherTower : MonoBehaviour {
	public float range = 20f;
	public LayerMask enemyLayer;
	public Transform firePoint;
	public float arrowSpeed;
	public GameObject arrowPrefab;
	public float fireRate = 60; // In RPM

	private float lastFiredShot;

	void Awake() {
		lastFiredShot = Time.time;
	}

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
			tryShoot(closestEnemy);
		}

	}

	void tryShoot(Transform target) {
		if (Time.time - lastFiredShot > (60 / fireRate)) {
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
				lastFiredShot = Time.time;
			}
		} else {
			Debug.Log("INFO: tried to shoot, but shooting is on cooldown! (");
			Debug.Log(Time.time);
		}
	}
}
