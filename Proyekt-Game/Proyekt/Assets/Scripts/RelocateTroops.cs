using UnityEngine;
using UnityEngine.AI;

public class AgentMover : MonoBehaviour {

	private NavMeshAgent agent;

	void Awake() {
		agent = GetComponent<NavMeshAgent>();
	}

	void Update() {
		if (Input.GetKey(KeyCode.Mouse0)) {
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;

			if (Physics.Raycast(ray, out hit)) {
				agent.SetDestination(hit.point);
			}
		}
	}
}
