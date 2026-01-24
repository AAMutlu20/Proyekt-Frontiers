using UnityEngine;

public class AbstractShooterTower : AbstractTower {
	// Commented out because it is redundant if the arc is computed using arrowSpeed.
	public float range = 20f;
	public LayerMask enemyLayer;
	public Transform firePoint;
	public float projectileSpeed;
	public GameObject projectilePrefab;
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
//transform.RotateAround
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
			Shoot(firePoint, target, projectilePrefab.GetComponent<Rigidbody>(), projectileSpeed);
		}
	}

	public void Shoot(
		Transform firePoint,
		Transform target,
		Rigidbody targetRb,
		float speed
	) {
		float gravity = Mathf.Abs(Physics.gravity.y);

		if (Tools.SolveMovingTargetBallisticArc(
			firePoint.position,
			target.position,
			targetRb.linearVelocity,
			speed,
			gravity,
			10,
			out Vector3 dir))
		{
			Debug.Log(dir);
			GameObject projectile = Instantiate(
				projectilePrefab,
				firePoint.position,
				Quaternion.LookRotation(dir)
			);

			Rigidbody rb = projectile.GetComponent<Rigidbody>();
			rb.linearVelocity = dir * speed;
			lastFiredShot = Time.time;
		}
	}
}
