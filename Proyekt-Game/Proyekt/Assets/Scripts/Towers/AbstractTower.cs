using UnityEngine;

public class AbstractTower : MonoBehaviour {
	protected float maxHP;
	protected float currentHP;

	protected void Damage(float amount) {
		currentHP -= amount;
		if (currentHP < 0) {
			Destroy(gameObject);
		}
	}
	protected void Heal(float amount) {
		currentHP += amount;
		Mathf.Clamp(currentHP, 0, maxHP);
	}
	protected float GetHealth() {
		return currentHP;
	}
	protected void SetHealth(float value) {
		currentHP = value;
		Mathf.Clamp(currentHP, 0, maxHP);
	}
}
