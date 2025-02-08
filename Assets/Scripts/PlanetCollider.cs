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
            var angle = Mathf.PI * 2 * i / resolution;
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var length = planet.SurfaceHeight(angle);
            points[i] = dir * length;
        }

        polygonCollider.points = points;
    }
}