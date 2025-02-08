using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Setup")]
    public SpacePhysics physics;
    
    [Header("Config")]
    public float speed = 2f;
    public float acceleration = 2f;
    public float jumpVel = 6f;

    private bool prevOnPlanet;
    
    private void FixedUpdate()
    {
        if (physics.onPlanet)
        {
            var dir = 0;
            if (Input.GetKey(KeyCode.A)) dir--;
            if (Input.GetKey(KeyCode.D)) dir++;
            var target = dir * speed / physics.closestPlanet.radius;

            if (Math.Sign(target) != Math.Sign(physics.planetVel) || Math.Abs(target) > Math.Abs(physics.planetVel))
            {
                physics.planetVel = Mathf.Lerp(physics.planetVel, target, 1 - Mathf.Exp(-acceleration * Time.deltaTime));
            }
            
            if (!prevOnPlanet) physics.closestPlanet.impacts.Add(new Planet.Impact { angle = physics.planetPos, influence = 1.4f, vel = 2f, pos = 0.1f });
        }
        prevOnPlanet = physics.onPlanet;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && physics.onPlanet)
        {
            physics.velocity = physics.closestPlanet.GetLinearVelocity(physics.planetPos, physics.planetVel) + physics.closestPlanet.SurfaceNormal(physics.planetPos) * jumpVel;
            physics.onPlanet = false;
        }
    }
}
