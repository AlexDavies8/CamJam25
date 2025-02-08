using System;
using UnityEngine;

[ExecuteInEditMode]
public class StickToPlanet : MonoBehaviour
{
    public Planet planet;
    [Range(0f, 1f)] public float stickPosition;

    private void Update()
    {
        transform.position = planet.SurfacePoint(stickPosition);
        transform.right = -planet.SurfaceTangent(stickPosition);
    }
}