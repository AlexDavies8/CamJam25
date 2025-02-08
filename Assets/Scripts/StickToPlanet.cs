using System;
using UnityEngine;

[ExecuteInEditMode]
public class StickToPlanet : MonoBehaviour
{
    public Planet planet;
    [Range(0f, 1f)] public float stickPosition;
    [Min(0)] public float hoverHeight;

    private void Update()
    {
        var normal = planet.SurfaceNormal(stickPosition);

        transform.position = planet.SurfacePoint(stickPosition) + normal * hoverHeight;
        transform.up = normal;
    }
}
