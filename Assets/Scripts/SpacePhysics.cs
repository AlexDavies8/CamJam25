using System;
using UnityEngine;

public class SpacePhysics : MonoBehaviour
{
    [Header("Config")]
    public float stickThreshold = 5f;
    public float surfaceOffset = 0.5f;
    public float friction = 2f;

    private Vector2 velocity = Vector2.right * 5f;
    public Planet currentPlanet = null;
    public float planetPos = 0f;
    public float planetVel = 0f;

    public Planet closestPlanet;
    
    public bool OnPlanet => currentPlanet is not null;
    
    private void FixedUpdate()
    {
        if (currentPlanet is null)
        {
            float minDist = float.MaxValue;
            foreach (var planet in Planet.Planets)
            {
                var planetDiff = planet.transform.position - transform.position;
                var dist = planetDiff.magnitude - planet.SurfaceDistance(transform.position) - surfaceOffset;
                if (dist < minDist)
                {
                    minDist = dist;
                    closestPlanet = planet;
                }
            }

            var diff = (Vector2)(closestPlanet.transform.position - transform.position);
            velocity += diff.normalized * closestPlanet.gravity / diff.magnitude;

            if (minDist <= 0)
            {
                currentPlanet = closestPlanet;
                planetPos = Mathf.Atan2(-diff.y, -diff.x) / (Mathf.PI * 2);
                planetVel = currentPlanet.GetAngularVelocity(transform.position, velocity);
                velocity = Vector2.zero;
            }
            else
            {
                transform.position += (Vector3)velocity * Time.deltaTime;
            }
        }
        else
        {
            planetVel = Mathf.Lerp(planetVel, 0f, 1 - Mathf.Exp(-friction * Time.deltaTime));
            planetPos += planetVel * Time.deltaTime;
            
            var outDir = (Vector2)(transform.position - currentPlanet.transform.position).normalized;
            var newPos = currentPlanet.SurfacePoint(planetPos) + outDir * surfaceOffset;
            transform.right = -currentPlanet.SurfaceTangent(planetPos);

            var outVel = -Vector2.Dot(newPos - (Vector2)transform.position, outDir) / Time.deltaTime;
            if (outVel > stickThreshold)
            {
                velocity = currentPlanet.GetLinearVelocity(planetPos, planetVel) + outVel * outDir;
                currentPlanet = null;
            }
            
            transform.position = newPos;
        }
    }
}