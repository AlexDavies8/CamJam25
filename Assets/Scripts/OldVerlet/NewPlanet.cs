using System;
using System.Collections.Generic;
using UnityEngine;

public class NewPlanet : MonoBehaviour
{
    [Header("Setup")]
    public VerletSolver solver;
    public LineRenderer lineRenderer;
    public PolygonCollider2D polygonCollider;

    [Header("Config")]
    public int pointCount = 30;
    public float radius = 5f;
    public float wobbleStrength = 0.1f;
    public float collisionRadius = 0.1f;
    public float stiffness = 20f;
    public float damping = 40f;
    public List<NoiseLayer> noiseLayers = new();

    private void Awake()
    {
        lineRenderer.positionCount = pointCount;
        polygonCollider.points = new Vector2[pointCount];

        var points = new Vector2[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            var pos = Evaluate((float)i / pointCount, Time.time);
            points[i] = pos;
        }
        solver.CreateFullyLinkedSoftbody(points);
        solver.AddUnityCollisionToAll(collisionRadius);
        solver.customConstraints += ApplyPlanetConstraint;
    }

    private void ApplyPlanetConstraint(List<VerletSolver.VerletPoint> points, int step, float dt)
    {
        var time = Time.time + dt * step;
        for (int i = 0; i < pointCount; i++)
        {
            var point = points[i];
            var target = Evaluate((float)i / pointCount, time);
            var force = VerletSolver.CalculateSpringForce(point.currPos, point.prevPos, target, target, 0f, stiffness, damping);
            Debug.DrawRay(point.currPos, force, Color.red);
            point.Accelerate(force);
            points[i] = point;
        }
    }

    private Vector2 Evaluate(float frac, float time)
    {
        var dir = new Vector2(Mathf.Cos(frac * Mathf.PI * 2), Mathf.Sin(frac * Mathf.PI * 2));
        var dist = radius;
        foreach (var layer in noiseLayers)
        {
            var t = time * layer.frequency + layer.offset + frac * layer.waveCount;
            dist += Mathf.Sin(t * Mathf.PI * 2) * layer.amplitude * wobbleStrength;
        }
        return dir * dist;
    }

    private void FixedUpdate()
    {
        var points = polygonCollider.points;
        for (int i = 0; i < pointCount; i++)
        {
            lineRenderer.SetPosition(i, solver.points[i].currPos);
            points[i] = solver.points[i].currPos;
        }
        polygonCollider.points = points;
    }

    [Serializable]
    public struct NoiseLayer
    {
        public float amplitude;
        public float frequency;
        public float offset;
        public int waveCount;
    }
}