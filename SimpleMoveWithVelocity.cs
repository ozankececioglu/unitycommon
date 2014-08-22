using UnityEngine;
using System.Collections;

public class SimpleMoveWithVelocity : MonoBehaviour
{

    public float DirectionX = 1;
    public float DirectionY = 0;
    public float DirectionZ = 0.1f;
    public float Velocity = 1;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () 
    {

        transform.position = new Vector3(   transform.position.x + Time.deltaTime * Velocity * DirectionX,
                                            transform.position.y + Time.deltaTime * Velocity * DirectionY,
                                            transform.position.z + Time.deltaTime * Velocity * DirectionZ );
	}
}
