using UnityEngine;
using UnityEngine.InputSystem;

public class CameraControls : MonoBehaviour {
	[Header("Movement")]
	[SerializeField] private float cameraSpeedIncrement = 0.01f;
	[SerializeField] private float cameraSpeedLimit = 0.1f;
	[SerializeField] [Range(0f, 1f)] private float damping = 0.95f;

	[Header("Rotation")]
	[SerializeField] private float rotationSpeed = 1f;
	
	[Header("Tilt")]
	[SerializeField] private float tiltAmount = 10f;
	[SerializeField] private float tiltMax = 100f;


	private Vector3 globalVel;
	private Vector3 localVel;

	private Quaternion originalRotation;
	
	private Quaternion baseRotation = Quaternion.Euler(new Vector3(0, 0, 0));
	private Vector3 basePosition = new Vector3(0,0,0);

	private Vector3 hitPoint;

	private bool panning = false;

	void Awake() {
		baseRotation = transform.rotation;
		basePosition = transform.position;
	}

	void Update() {
		//   __  __                                     _   
		//  |  \/  |                                   | |  
		//  | \  / | _____   _____ _ __ ___   ___ _ __ | |_ 
		//  | |\/| |/ _ \ \ / / _ \ '_ ` _ \ / _ \ '_ \| __|
		//  | |  | | (_) \ V /  __/ | | | | |  __/ | | | |_ 
		//  |_|  |_|\___/ \_/ \___|_| |_| |_|\___|_| |_|\__|
                                                 
                                                 	
		Vector3 input = Vector3.zero;

		// Left and right, respectively
		if (Keyboard.current.aKey.isPressed) input.x -= 1f;
		if (Keyboard.current.dKey.isPressed) input.x += 1f;
		
		// Forwards and backwards, respectively
		if (Keyboard.current.wKey.isPressed) input.z += 1f;
		if (Keyboard.current.sKey.isPressed) input.z -= 1f;

		localVel += input * cameraSpeedIncrement;
	
		// Slow down
		localVel *= damping; //Vector3.Scale(localVel, new Vector3(0.95f,0.95f,0.95f));

		// Clamp
		localVel.x = Mathf.Clamp(localVel.x, -cameraSpeedLimit, cameraSpeedLimit);
		localVel.z = Mathf.Clamp(localVel.z, -cameraSpeedLimit, cameraSpeedLimit);
		
		// Apply
		localVel.y = 0f;
		globalVel = transform.TransformDirection(localVel);
		globalVel.y = 0f;
		basePosition += globalVel;

		//   _______ _ _ _   
		//  |__   __(_) | |  
		//     | |   _| | |_ 
		//     | |  | | | __|
		//     | |  | | | |_ 
		//     |_|  |_|_|\__|
		//

		Vector3 myRotation = new Vector3(0,0,0);

		myRotation.x = Mathf.Clamp(localVel.z * tiltAmount, -tiltMax, tiltMax);
		myRotation.z = -Mathf.Clamp(localVel.x * tiltAmount, -tiltMax, tiltMax);

		//  ____                   _             
		// |  _ \ __ _ _ __  _ __ (_)_ __   __ _ 
		// | |_) / _` | '_ \| '_ \| | '_ \ / _` |
		// |  __/ (_| | | | | | | | | | | | (_| |
		// |_|   \__,_|_| |_|_| |_|_|_| |_|\__, |
		//                                 |___/ 	
                  

		if (Mouse.current.rightButton.wasPressedThisFrame) {
			// Ray cast to find the place where the cursor collides with the terrain
			Debug.Log("Poep hihi");
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;

			Vector2 mousePos = Mouse.current.position.ReadValue();
			//Ray ray = Camera.main.ScreenPointToRay(mousePos);
			Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
			RaycastHit hit;

			// Made-up point for testing purposes
			//Vector3 rotateAround = new Vector3(0, 0, 0);
			
			if (Physics.Raycast(ray, out hit)) {
				hitPoint = hit.point;
			}
			panning = true;
		}

		if (Mouse.current.rightButton.wasReleasedThisFrame) {
			panning = false;
			
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}

                if (panning) {
			float mouseX = Mouse.current.delta.ReadValue().x * rotationSpeed;
			(basePosition, baseRotation) = Tools.OrbitCamera(
					transform.position,
					hitPoint,
					Vector3.up,
					mouseX
				);
			Debug.DrawRay(hitPoint, Vector3.up * 0.05f, Color.red);
		}

		// Zooming
		float scroll = Mouse.current.scroll.ReadValue().y;
		Vector3 zoom = transform.forward * scroll;
		basePosition += zoom;

		transform.rotation = baseRotation * Quaternion.Euler(myRotation);
		transform.position = basePosition;
	}
}
