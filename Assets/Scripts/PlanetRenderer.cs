using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;

[RequireComponent(typeof(Planet)), ExecuteInEditMode]
public class PlanetRenderer : MonoBehaviour
{
    [Header("Setup")]
    public LineRenderer outline;
    public DeformableSprite fill;
    
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
        if (resolution != outline.positionCount || (fill && (fill.vertices is null || resolution != fill.vertices.Count)))
        {
            UpdateResolution();
        }
    }

    private void UpdateResolution()
    {
        outline.positionCount = resolution;
        if (fill) fill.vertices = new Vector2[resolution].ToList();
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
            if (fill) fill.vertices[i] = pos;
        }
        if (fill) fill.UpdateMesh();
    }
}