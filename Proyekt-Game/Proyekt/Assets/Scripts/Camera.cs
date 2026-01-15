using UnityEngine;
using UnityEngine.InputSystem;

public class CameraControls : MonoBehaviour {
	public float cameraSpeedIncrement = 0.01f;
	public float cameraSpeedLimit = 0.08f;
	public float cameraDrag = 0.0001f;
	public int tiltAmount = 100;
	public int tiltMax = 10;

	// public for testing purposes
	public Vector3 velocity;

	private Quaternion originalRotation;

	void Awake() {
		originalRotation = transform.rotation;
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
			velocity -= new Vector3(cameraSpeedIncrement, 0, 0);
		}
		if (Keyboard.current.dKey.isPressed) {
			velocity += new Vector3(cameraSpeedIncrement, 0, 0);
		}
		
		// Forwards and backwards, respectively
		
		if (Keyboard.current.wKey.isPressed) {
			velocity += new Vector3(0, 0, cameraSpeedIncrement);
		}
		if (Keyboard.current.sKey.isPressed) {
			velocity -= new Vector3(0, 0, cameraSpeedIncrement);
		}

	
		// Slow down
		velocity = Vector3.Scale(velocity, new Vector3(0.95f,0.95f,0.95f));

		// Older, technically better system, but it had a bug where the
		// camera would only be able to move in one direction, x or z
		
		//Vector3 positiveVelocity = new Vector3(Mathf.Abs(velocity.x), Mathf.Abs(velocity.y), Mathf.Abs(velocity.z));
		//velocity = Vector3.Scale(positiveVelocity - new Vector3(cameraDrag, 0, cameraDrag),velocity.normalized);

		// Clamp
		velocity.x = Mathf.Clamp(velocity.x, -cameraSpeedLimit, cameraSpeedLimit);
		velocity.z = Mathf.Clamp(velocity.z, -cameraSpeedLimit, cameraSpeedLimit);

		// Apply
		//transform.localPosition = transform.localPosition + velocity;		
		transform.Translate(velocity, Space.Self);

		//   _______ _ _ _   
		//  |__   __(_) | |  
		//     | |   _| | |_ 
		//     | |  | | | __|
		//     | |  | | | |_ 
		//     |_|  |_|_|\__|
		
		Vector3 myRotation = new Vector3(0, 0, 0);
		myRotation.z = -Mathf.Clamp(velocity.x * tiltAmount, -tiltMax, tiltMax);
		myRotation.x = -Mathf.Clamp(velocity.z * tiltAmount, -tiltMax, tiltMax);

		transform.rotation = originalRotation * Quaternion.Euler(myRotation);

                  
                  
	}

	
}
