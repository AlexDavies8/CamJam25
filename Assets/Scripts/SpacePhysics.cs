using System;
using UnityEngine;

public class SpacePhysics : MonoBehaviour
{
    [Header("Config")]
    public float stickThreshold = 5f;
    public float surfaceOffset = 0.5f;
    public float friction = 2f;
    public float airResistance = 0.5f;
    public float velocityInferenceDamping = 20f;
    public float maximumSpeed = 20f;

    [HideInInspector] public Vector2 velocity = Vector2.right * 5f;
    [HideInInspector] public float planetPos = 0f;
    [HideInInspector] public float planetVel = 0f;

    [HideInInspector] public Planet closestPlanet;

    [HideInInspector] public bool onPlanet;

    private Vector2 prevPosition;
    private Vector2 inferredVelocity;
    
    private void FixedUpdate()
    {
        inferredVelocity = Vector2.Lerp(inferredVelocity, ((Vector2)transform.position - prevPosition) / Time.fixedDeltaTime, 1f - Mathf.Exp(-velocityInferenceDamping * Time.fixedDeltaTime));
        prevPosition = transform.position;
        
        Debug.DrawRay(Vector2.zero, inferredVelocity.magnitude * Vector2.right, Color.red);
        
        if (onPlanet) DoPlanetPhysics();
        else DoSpacePhysics();
    }

    private void DoSpacePhysics()
    {
        UpdateClosestPlanet();

        // We are using 1/dist gravity instead of 1/dist^2 because it feels nicer
        var planetCoreDelta = (Vector2)(closestPlanet.transform.position - transform.position);
        velocity += planetCoreDelta.normalized * closestPlanet.gravity / Mathf.Max(1f, planetCoreDelta.magnitude);
        velocity = Vector2.Lerp(velocity, Vector2.zero, 1f - Mathf.Exp(-airResistance * Time.deltaTime));
        
        velocity = Vector2.ClampMagnitude(velocity, maximumSpeed);

        transform.position += (Vector3)velocity * Time.deltaTime;
    }

    private void UpdateClosestPlanet()
    {
        float minDist = float.MaxValue;
        foreach (var planet in Planet.Planets)
        {
            var centreDist = Vector2.Distance(planet.transform.position, transform.position);
            var height = planet.SurfaceHeight(planet.AngleTo(transform.position));
            var dist = centreDist - height;
            if (dist < minDist)
            {
                minDist = dist;
                closestPlanet = planet;
            }
        }

        if (minDist < 0 && Vector2.Dot(velocity, closestPlanet.transform.position - transform.position) >= 0) Land();
    }

    private void Land()
    {
        onPlanet = true;

        planetPos = closestPlanet.AngleTo(transform.position);
        planetVel = closestPlanet.GetAngularVelocity(planetPos, velocity);
        velocity = Vector2.zero; // Cut linear velocity since we are on a planet now
    }

    public void Detach()
    {
        // Restore inferred velocity
        velocity += inferredVelocity;
        onPlanet = false;
    }

    private void DoPlanetPhysics()
    {
        // Keep between 0 and 2 PI
        if (planetPos < 0) planetPos += Mathf.PI * 2;
        if (planetPos > Mathf.PI * 2) planetPos -= Mathf.PI * 2;

        planetVel = Mathf.Lerp(planetVel, 0f, 1 - Mathf.Exp(-friction * Time.deltaTime));
        planetPos += planetVel * Time.deltaTime;

        var normal = closestPlanet.SurfaceNormal(planetPos);
        var pos = closestPlanet.SurfacePoint(planetPos);
        transform.position = pos + normal * surfaceOffset;
        transform.up = normal;
    }
}