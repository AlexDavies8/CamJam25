using System;
using UnityEngine;
using UnityEngine.U2D;

[RequireComponent(typeof(Planet)), ExecuteInEditMode]
public class PlanetRenderer : MonoBehaviour
{
    [Header("Setup")]
    public LineRenderer outline;
    public SpriteShapeController fill;
    
    [Header("Config")]
    public int resolution = 50;

    private Planet planet;

    private void Awake()
    {
        planet = GetComponent<Planet>();
        UpdateResolution();
    }

    private void OnValidate()
    {
        if (resolution != outline.positionCount)
        {
            UpdateResolution();
        }
    }

    private void UpdateResolution()
    {
        outline.positionCount = resolution;
        fill.spline.Clear();
        for (int i = 0; i < resolution; i++) fill.spline.InsertPointAt(i, Vector2.one * i);
    }

    private void Update()
    {
        for (int i = 0; i < resolution; i++)
        {
            var angle = Mathf.PI * 2f * i / resolution;
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var dist = planet.SurfaceHeight(angle);
            var pos = dir * dist;
            outline.SetPosition(i, pos);
            fill.spline.SetPosition(i, pos);
        }
    }
}