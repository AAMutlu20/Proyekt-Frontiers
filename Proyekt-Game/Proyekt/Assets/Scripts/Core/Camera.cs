using UnityEngine;
using UnityEngine.InputSystem;

public class CameraControls : MonoBehaviour {
	public float cameraSpeedIncrement = 0.5f;
	public float cameraSpeedLimit = 3f;
	public float cameraDrag = 0.0001f;

	public float rotationSpeed = 1f;

	public float tiltAmount = 1f;
	public float tiltMax = 10;

	public Transform cube;

	// public for testing purposes
	public Vector3 velocity;
	public Vector3 velocityHelp;

	private Quaternion originalRotation;
	
	public Quaternion baseRotation = Quaternion.Euler(new Vector3(0, 0, 0));
	public Vector3 basePosition = new Vector3(0,0,0);

	public Vector3 hitPoint;

	[SerializeField] private bool panning = false;

	void Awake() {
		//originalRotation = transform.rotation;
		baseRotation = transform.rotation;
		basePosition = transform.position;
		//velocity = transform.forward;
	}

	void Update() {
		//   __  __                                     _   
		//  |  \/  |                                   | |  
		//  | \  / | _____   _____ _ __ ___   ___ _ __ | |_ 
		//  | |\/| |/ _ \ \ / / _ \ '_ ` _ \ / _ \ '_ \| __|
		//  | |  | | (_) \ V /  __/ | | | | |  __/ | | | |_ 
		//  |_|  |_|\___/ \_/ \___|_| |_| |_|\___|_| |_|\__|
                                                 
                                                 	
		// Left and right, respectively

		if (Keyboard.current.aKey.isPressed) {
			velocity -= transform.right * cameraSpeedIncrement; // (cameraSpeedIncrement, 0, 0);
			velocityHelp -= Vector3.right * cameraSpeedIncrement; // (cameraSpeedIncrement, 0, 0);
		}
		if (Keyboard.current.dKey.isPressed) {
			velocity += transform.right * cameraSpeedIncrement; //(cameraSpeedIncrement, 0, 0);
			velocityHelp += Vector3.right * cameraSpeedIncrement; // (cameraSpeedIncrement, 0, 0);

		}
		
		// Forwards and backwards, respectively
		
		if (Keyboard.current.wKey.isPressed) {
			velocity += transform.forward * cameraSpeedIncrement;
			velocityHelp += Vector3.forward * cameraSpeedIncrement;
			//velocity += new Vector3(0, 0, cameraSpeedIncrement);
		}
		
		if (Keyboard.current.sKey.isPressed) {
			velocity -= transform.forward * cameraSpeedIncrement;
			velocityHelp -= Vector3.forward * cameraSpeedIncrement;
		}

	
		// Slow down
		velocity = Vector3.Scale(velocity, new Vector3(0.95f,0.95f,0.95f));
		velocityHelp = Vector3.Scale(velocity, new Vector3(0.95f,0.95f,0.95f));

		// Clamp
		velocity.x = Mathf.Clamp(velocity.x, -cameraSpeedLimit, cameraSpeedLimit);
		velocity.z = Mathf.Clamp(velocity.z, -cameraSpeedLimit, cameraSpeedLimit);
		velocityHelp.x = Mathf.Clamp(velocityHelp.x, -cameraSpeedLimit, cameraSpeedLimit);
		velocityHelp.z = Mathf.Clamp(velocityHelp.z, -cameraSpeedLimit, cameraSpeedLimit);
		
		// Apply
		velocity = new Vector3(velocity.x, 0, velocity.z);
		velocityHelp = new Vector3(velocityHelp.x, 0, velocityHelp.z);
		basePosition += velocity;

		//   _______ _ _ _   
		//  |__   __(_) | |  
		//     | |   _| | |_ 
		//     | |  | | | __|
		//     | |  | | | |_ 
		//     |_|  |_|_|\__|
		//

		Vector3 myRotation = new Vector3(0,0,0);

		myRotation.x = Mathf.Clamp(velocityHelp.z * tiltAmount, -tiltMax, tiltMax);
		myRotation.z = -Mathf.Clamp(velocityHelp.x * tiltAmount, -tiltMax, tiltMax);

		//Vector3 facingDir = transform.forward;
		//Vector3 facingEuler = Quaternion.LookRotation(facingDir).eulerAngles;
		//myRotation += facingEuler;


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
			Ray ray = Camera.main.ScreenPointToRay(mousePos);
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
		transform.rotation = baseRotation * Quaternion.Euler(myRotation);
		transform.position = basePosition;
	}
}
