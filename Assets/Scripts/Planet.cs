using System;
using System.Collections.Generic;
using UnityEngine;

public class Planet : MonoBehaviour
{
    [Header("Config")]
    public float radius = 5f;
    public float wobbleStrength = 0.01f;
    public float gravity = 5f;
    public List<NoiseLayer> layers = new();
    
    [Serializable]
    public struct NoiseLayer
    {
        public float amplitude;
        public float frequency;
        public float offset;
        public int waveCount;
    }
    
    public static List<Planet> Planets = new();

    private void OnEnable()
    {
        Planets.Add(this);
    }

    private void OnDisable()
    {
        Planets.Remove(this);
    }

    public float SurfaceDistance(float frac)
    {
        var dist = radius;
        foreach (var layer in layers)
        {
            var t = Time.time * layer.frequency + layer.offset + frac * layer.waveCount;
            dist += Mathf.Sin(t * Mathf.PI * 2) * layer.amplitude * wobbleStrength;
        }
        return dist;
    }

    public Vector2 SurfaceTangent(float frac)
    {
        return (SurfacePoint(frac + 0.001f) - SurfacePoint(frac)).normalized;
    }

    public float SurfaceDistance(Vector2 from)
    {
        var frac = Mathf.Atan2(from.y, from.x) / (Mathf.PI * 2);
        return SurfaceDistance(frac);
    }

    public Vector2 SurfacePoint(float frac)
    {
        var dist = SurfaceDistance(frac);
        return new Vector2(Mathf.Cos(frac * Mathf.PI * 2), Mathf.Sin(frac * Mathf.PI * 2)) * dist;
    }

    public float GetAngularVelocity(Vector2 pos, Vector2 linear)
    {
        var frac = Mathf.Atan2(pos.y, pos.x) / (Mathf.PI * 2);
        var tangent = SurfaceTangent(frac);
        var vel = Vector2.Dot(tangent, linear);
        return vel / (2f * Mathf.PI * SurfaceDistance(frac));
    }

    public Vector2 GetLinearVelocity(float frac, float angular)
    {
        var tangent = SurfaceTangent(frac);
        return angular * tangent * SurfaceDistance(frac) * Mathf.PI * 2f;
    }
}