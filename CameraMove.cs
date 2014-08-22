using UnityEngine;
using System.Collections;

public class CameraMove : MonoBehaviour 
{	
	public float mouseSensitivity = 3f;
	public float keySensitivity = 0.15f;
	
	void Start () {
	
	}
	
	void Update () 
	{
		if (Input.GetMouseButton(1)) {
			Vector2 mouseDelta;
			mouseDelta.x = Input.GetAxis("Mouse X");
			mouseDelta.y = -Input.GetAxis("Mouse Y");
			mouseDelta *= mouseSensitivity;
			var rotation = Quaternion.identity;
			rotation *= Quaternion.AngleAxis(mouseDelta.x, transform.InverseTransformDirection(Vector3.up));
			rotation *= Quaternion.AngleAxis(mouseDelta.y, Vector3.right);
			transform.rotation = transform.rotation * rotation;
			
			Vector3 direction = Vector3.zero;
			if (Input.GetKey(KeyCode.A)) {
				direction += Vector3.left;
			}
			if (Input.GetKey(KeyCode.D)) {
				direction += Vector3.right;
			}
			if (Input.GetKey(KeyCode.W)) {
				direction += Vector3.forward;
			}
			if (Input.GetKey(KeyCode.S)) {
				direction += Vector3.back;
			}
			if (Input.GetKey(KeyCode.E)) {
				direction += Vector3.up;
			}
			if (Input.GetKey(KeyCode.Q)) {
				direction += Vector3.down;
			}
			direction *= Input.GetKey(KeyCode.LeftShift) ? 3 * keySensitivity : keySensitivity;
			transform.position = transform.position + transform.TransformDirection(direction);
		}
	}
}
