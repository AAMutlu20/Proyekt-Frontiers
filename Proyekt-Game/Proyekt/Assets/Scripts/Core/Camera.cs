using UnityEngine;
using UnityEngine.InputSystem;

public class CameraControls : MonoBehaviour {
	public float cameraSpeedIncrement = 0.5f;
	public float cameraSpeedLimit = 3f;
	public float cameraDrag = 0.0001f;
<<<<<<< HEAD
	public int tiltAmount = 1;
	public int tiltMax = 10;
=======
	public float tiltAmount = 1f;
	public float tiltMax = 10;
>>>>>>> 9099fdaa1e43f06964496fcbdb1a3d0ea9657584

	// public for testing purposes
	public Vector3 velocity;

	private Quaternion originalRotation;

	void Awake() {
		originalRotation = transform.rotation;
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
<<<<<<< HEAD
			velocity -= transform.right * cameraSpeedIncrement; // (cameraSpeedIncrement, 0, 0);
		}
		if (Keyboard.current.dKey.isPressed) {
			velocity += transform.right * cameraSpeedIncrement; //(cameraSpeedIncrement, 0, 0);
=======
			velocity -= transform.right * cameraSpeedIncrement;
		}
		if (Keyboard.current.dKey.isPressed) {
			velocity += transform.right * cameraSpeedIncrement;
>>>>>>> 9099fdaa1e43f06964496fcbdb1a3d0ea9657584
		}
		
		// Forwards and backwards, respectively
		
		if (Keyboard.current.wKey.isPressed) {
			velocity += transform.forward * cameraSpeedIncrement;
<<<<<<< HEAD
			//velocity += new Vector3(0, 0, cameraSpeedIncrement);
		}
		if (Keyboard.current.sKey.isPressed) {
			velocity -= transform.forward * cameraSpeedIncrement;
			//velocity -= new Vector3(0, 0, cameraSpeedIncrement);
=======
		}
		if (Keyboard.current.sKey.isPressed) {
			velocity -= transform.forward * cameraSpeedIncrement;
>>>>>>> 9099fdaa1e43f06964496fcbdb1a3d0ea9657584
		}

	
		// Slow down
		velocity = Vector3.Scale(velocity, new Vector3(0.95f,0.95f,0.95f));

		// Clamp
		velocity.x = Mathf.Clamp(velocity.x, -cameraSpeedLimit, cameraSpeedLimit);
		velocity.z = Mathf.Clamp(velocity.z, -cameraSpeedLimit, cameraSpeedLimit);
		
		// Apply
		velocity = new Vector3(velocity.x, 0, velocity.z);
		transform.position = transform.position + velocity;

		//   _______ _ _ _   
		//  |__   __(_) | |  
		//     | |   _| | |_ 
		//     | |  | | | __|
		//     | |  | | | |_ 
		//     |_|  |_|_|\__|
		
		Vector3 myRotation = new Vector3(0, 0, 0);
		myRotation.x = Mathf.Clamp(velocity.x * tiltAmount, -tiltMax, tiltMax);
		myRotation.z = Mathf.Clamp(velocity.z * tiltAmount, -tiltMax, tiltMax);

		transform.rotation = originalRotation * Quaternion.Euler(myRotation);

                  
                  
	}

	
}
