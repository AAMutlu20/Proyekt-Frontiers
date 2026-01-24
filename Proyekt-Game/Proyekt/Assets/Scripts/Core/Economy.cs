using UnityEngine;
using UnityEngine.Events;

public class Economy : MonoBehaviour {
    [SerializeField] private int coins;
	[SerializeField]private float cooldown;
	[SerializeField] private bool giveCoins = false;
	[SerializeField] private float timer;
	[SerializeField] private int coinsToGive = 5;
	[SerializeField] private int startingCoins = 5;

	// Property
	public int Coins { get { return coins; } private set { coins = value; OnCoinAmountChanged?.Invoke(coins); } }

	public UnityEvent<int> OnCoinAmountChanged;

	public int getBalance() {
		return Coins;
	}

	void Awake() {
		Coins = startingCoins;
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
	void Update()
	{
		if (!giveCoins && timer < cooldown)
		{
			
			timer += Time.deltaTime;

			
			if (timer >= cooldown)
			{
				timer = 0.0f;

				cooldown = 8f;

				giveCoins = true;
			}
		}


		if (giveCoins)
		{
			Coins += coinsToGive;

			giveCoins = false;
		}
    }

}
