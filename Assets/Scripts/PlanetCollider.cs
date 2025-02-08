using System;
using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
public class PlanetCollider : MonoBehaviour
{
    [Header("Config")]
    public int resolution = 25;

    private Planet planet;
    private PolygonCollider2D polygonCollider;

    private void Awake()
    {
        planet = GetComponent<Planet>();
        polygonCollider = GetComponent<PolygonCollider2D>();
        polygonCollider.points = new Vector2[resolution];
    }

    private void FixedUpdate()
    {
        var points = polygonCollider.points;
        for (int i = 0; i < resolution; i++)
        {
            var frac = (float)i / resolution;
            var dir = new Vector2(Mathf.Cos(frac * Mathf.PI * 2), Mathf.Sin(frac * Mathf.PI * 2));
            var dist = planet.SurfaceDistance(dir);
            var pos = dir * dist;
            points[i] = pos;
        }

        polygonCollider.points = points;
    }
}