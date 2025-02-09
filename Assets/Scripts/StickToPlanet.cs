using System;
using UnityEngine;

[ExecuteInEditMode]
public class StickToPlanet : MonoBehaviour
{
    public Planet planet;
    [Range(0f, 1f)] public float stickPosition;
    [Min(0)] public float hoverHeight;

    private void OnEnable()
    {
        if (!planet) planet = GetComponentInParent<Planet>();
    }

    private void Update()
    {
        if (!planet) return;
        
        var normal = planet.SurfaceNormal(stickPosition * Mathf.PI * 2);

        transform.position = planet.SurfacePoint(stickPosition * Mathf.PI * 2) + normal * hoverHeight;
        transform.up = normal;
    }
}
