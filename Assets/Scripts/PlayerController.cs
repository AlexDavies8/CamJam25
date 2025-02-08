using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Setup")]
    public SpacePhysics physics;
    
    [Header("Config")]
    public float speed = 2f;
    public float acceleration = 2f;

    private bool prevOnPlanet = false;
    
    private void FixedUpdate()
    {
        if (physics.OnPlanet)
        {
            var dir = 0;
            if (Input.GetKey(KeyCode.A)) dir--;
            if (Input.GetKey(KeyCode.D)) dir++;
            var target = dir * speed / (Mathf.PI * 2 * physics.currentPlanet.radius);

            if (Math.Sign(target) != Math.Sign(physics.planetVel) || Math.Abs(target) > Math.Abs(physics.planetVel))
            {
                physics.planetVel =
                    Mathf.Lerp(physics.planetVel, target, 1 - Mathf.Exp(-acceleration * Time.deltaTime));
            }
            
            // if (!prevOnPlanet) physics.currentPlanet.impacts.Add(new Planet.Impact { position = physics.planetPos + 0.5f, strength = 0.00005f });
        }
        prevOnPlanet = physics.OnPlanet;
    }
}
