using UnityEngine;
using UnityEngine.Events;

public class Economy : MonoBehaviour {
	private int coins;

	public int Coins { get { return coins; } private set { coins = value; OnCoinAmountChanged?.Invoke(coins); } }

	public UnityEvent<int> OnCoinAmountChanged;

	public int getBalance() {
		return Coins;
	}

	void Awake() {
		Coins = 0;
	}

	public void AwardCoins(int amount) {
		Coins += amount;
	}

	public void withDrag(int amount) {
		Coins -= amount;
	}

	public bool CanAfford(int cost)
	{
		return coins - cost >= 0;
	}

	void Update() {
		if (Mathf.Round(Time.time % 10) == 0) {
			Coins++;
		}
	}
}
