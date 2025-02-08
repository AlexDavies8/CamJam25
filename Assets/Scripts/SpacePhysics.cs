using System;
using UnityEngine;

public class SpacePhysics : MonoBehaviour
{
    [Header("Config")]
    public float stickThreshold = 5f;

    private Vector2 velocity = Vector2.right * 5f;
    private Planet currentPlanet = null;
    private float planetPos = 0f;
    private float planetVel = 0f;
    
    private void FixedUpdate()
    {
        if (currentPlanet is null)
        {
            float minDist = float.MaxValue;
            Planet closestPlanet = null;
            foreach (var planet in Planet.Planets)
            {
                var planetDiff = planet.transform.position - transform.position;
                var dist = planetDiff.magnitude - planet.SurfaceDistance(transform.position);
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
            //planetVel *= 0.995f;
            planetPos += planetVel * Time.deltaTime;
            var newPos = currentPlanet.SurfacePoint(planetPos);
            transform.right = -currentPlanet.SurfaceTangent(planetPos);

            var outDir = (transform.position - currentPlanet.transform.position).normalized;
            var outVel = -Vector2.Dot(newPos - (Vector2)transform.position, outDir) / Time.deltaTime;
            Debug.Log(outVel);
            if (outVel > stickThreshold)
            {
                var diff = transform.position - currentPlanet.transform.position;
                velocity = currentPlanet.GetLinearVelocity(planetPos, planetVel) + outVel * (Vector2)outDir;
                currentPlanet = null;
            }
            
            transform.position = newPos;
        }
    }
}