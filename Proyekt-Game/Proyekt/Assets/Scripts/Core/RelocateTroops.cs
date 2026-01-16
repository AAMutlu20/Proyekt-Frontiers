using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class RelocateTroops : MonoBehaviour {

	private NavMeshAgent agent;

	void Awake() {
		agent = GetComponent<NavMeshAgent>();
	}

	void Update() {
		if (Mouse.current.leftButton.isPressed) {
			Vector2 mousePos = Mouse.current.position.ReadValue();
			Ray ray = Camera.main.ScreenPointToRay(mousePos);
			RaycastHit hit;

			if (Physics.Raycast(ray, out hit)) {
				agent.SetDestination(hit.point);
			}
		}
	}
}
