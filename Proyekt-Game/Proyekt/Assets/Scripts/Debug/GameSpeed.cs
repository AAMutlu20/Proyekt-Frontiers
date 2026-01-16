using UnityEngine;
using UnityEngine.InputSystem;

public class GameSpeed : MonoBehaviour {
	void Update() {
		if (Keyboard.current.pKey.isPressed) {
			if (Time.timeScale == 1f) {
				Time.timeScale = 0.1f;
			} else {
				Time.timeScale = 1f;
			}
		}
	}
}
