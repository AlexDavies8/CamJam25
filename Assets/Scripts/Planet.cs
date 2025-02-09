using System;
using System.Collections.Generic;
using UnityEngine;

public class Planet : MonoBehaviour
{
    [Header("Config")]
    public float radius = 5f;
    public float wobbleStrength = 0.01f;
    public float gravity = 5f;
    public float impactStrength = 1f;
    public List<NoiseLayer> layers = new();
    
    [Serializable]
    public struct NoiseLayer
    {
        public float amplitude;
        public float frequency;
        public float offset;
        public int waveCount;
    }

    public struct Impact
    {
        public float angle;
        public float influence;
        public float vel;
        public float pos;
        public float bandwidth;
    }
    
    public static List<Planet> Planets = new();

    public List<Impact> impacts = new();

    private void OnEnable()
    {
        Planets.Add(this);
    }

    private void OnDisable()
    {
        Planets.Remove(this);
    }

    private void Update()
    {
        for (int i = 0; i < impacts.Count; i++)
        {
            var impact = impacts[i];

            impact.influence -= Time.deltaTime;

            impact.vel -= impact.pos * Time.deltaTime * 20f;
            impact.pos += impact.vel * Time.deltaTime;
            
            if (impact.influence <= 0) impacts.RemoveAt(i);
            else impacts[i] = impact;
        }
    }

    public float SurfaceHeight(float angle)
    {
        var dist = radius;
        foreach (var layer in layers)
        {
            var t = Time.time * layer.frequency + layer.offset + angle * layer.waveCount;
            dist += Mathf.Sin(t) * layer.amplitude * wobbleStrength;
        }
        foreach (var impact in impacts)
        {
            var diff = (impact.angle - angle) % (Mathf.PI * 2f);
            if (diff < 0) diff += Mathf.PI * 2f;
            var fracDist = Mathf.Min(diff, Mathf.PI * 2 - diff);
            var influence = Bump(fracDist * radius / impact.bandwidth) * impact.influence;
            dist -= influence * impactStrength * impact.pos;
        }
        return dist;
    }

    private static float Bump(float x)
    {
        if (x >= 1) return 0;
        return Mathf.Exp(1f / (x * x - 1f));
    }

    public float AngleTo(Vector2 position)
    {
        var diff = position - (Vector2)transform.position;
        return Mathf.Atan2(diff.y, diff.x);
    }

    public Vector2 SurfacePoint(float angle)
    {
        var length = SurfaceHeight(angle);
        return (Vector2)transform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * length;
    }

    public Vector2 SurfaceTangent(float angle)
    {
        return (SurfacePoint(angle) - SurfacePoint(angle + 0.01f)).normalized;
    }

    public Vector2 SurfaceNormal(float angle)
    {
        return Vector2.Perpendicular(SurfaceTangent(angle));
    }

    public float GetAngularVelocity(float angle, Vector2 linear)
    {
        var tangent = SurfaceTangent(angle);
        var length = Vector2.Dot(tangent, linear);
        return length / (2f * Mathf.PI * SurfaceHeight(angle));
    }

    public Vector2 GetLinearVelocity(float angle, float angular)
    {
        return -SurfaceTangent(angle) * (angular * SurfaceHeight(angle));
    }
}