using System;
using System.Collections.Generic;
using UnityEngine;

public class PlanetGrowJingle : MonoBehaviour
{
    public List<int> notePattern = new();
    public int maxError = 1;
    public float bounceInfluence = 1f;
    public float bounceWidth = 3f;
    public float bounceVelocity = 2f;
    public float playerBounceVelocity = 8f;
    
    public PlayerController player;

    private void FixedUpdate()
    {
        if (PitchDetector.Instance.RecognisePattern(notePattern, maxError))
        {
            var planet = player.physics.closestPlanet;
            var angle = planet.AngleTo(player.transform.position);
            planet.impacts.Add(new Planet.Impact { angle = angle, pos = 0, vel = -bounceVelocity, influence = bounceInfluence, bandwidth = bounceWidth });
            planet.impacts.Add(new Planet.Impact { angle = angle, pos = 0, vel = -bounceVelocity / 2, influence = bounceInfluence / 2, bandwidth = bounceWidth * 2 });
            var jumpSpeed = bounceVelocity * bounceInfluence * planet.impactStrength;
            if (planet.impactStrength > 0.5f) // TODO: Ideally estimate actual surface velocity
            {
                player.physics.Detach();
                player.physics.velocity += planet.SurfaceNormal(player.physics.planetPos) * jumpSpeed;
            }
        }
    }
}