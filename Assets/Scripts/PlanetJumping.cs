using Unity.VisualScripting;
using UnityEngine;

public class PlanetJumping : MonoBehaviour
{
    public SpacePhysics spacePhysics;
    public float speed = 30f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("omg");
            planetJump();
        }
    }

    void planetJump()
    {
        Planet cp = spacePhysics.closestPlanet;

        Vector2 direction = transform.position - cp.transform.position;
        spacePhysics.velocity = direction.normalized * speed;
    }
}
