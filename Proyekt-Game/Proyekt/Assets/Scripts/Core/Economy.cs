using UnityEngine;

public class Economy : MonoBehaviour {
	private int coins;

	public int getBalance() {
		return coins;
	}

	void Awake() {
		coins = 0;
	}

	public void AwardCoins(int amount) {
		coins += amount;
	}

	public void withDrag(int amount) {
		coins -= amount;
	}

	void Update() {
		if (Mathf.Round(Time.time % 10) == 0) {
			coins++;
		}
	}
}
