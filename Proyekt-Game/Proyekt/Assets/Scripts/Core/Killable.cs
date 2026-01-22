using UnityEngine;


public class Killabale : MonoBehaviour {
	public Economy economy;

	void Die() {
		Destroy(gameObject);
		economy.AwardCoins(5);
	}
}
