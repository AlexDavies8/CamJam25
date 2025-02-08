using System;
using System.Collections.Generic;
using UnityEngine;

public class StaticSoftbody : MonoBehaviour
{
    public float damping = 5f;
    public float tension = 5f;
    public int pointCount = 30;
    public Vector2 gravity = Vector2.left * 10f;
    public int substeps = 8;
    
    [HideInInspector] public SoftbodyPoint[] points;
    private SoftbodyPoint[] _nextPoints;

    private void Awake()
    {
        points = new SoftbodyPoint[pointCount];
        _nextPoints = new SoftbodyPoint[pointCount];
    }

    public void SetPointCount(int count)
    {
        pointCount = count;

        var newPoints = new SoftbodyPoint[pointCount];
        _nextPoints = new SoftbodyPoint[pointCount];
        if (points is not null)
        {
            for (int i = 0; i < pointCount; i++)
            {
                var frac = (float)i / pointCount;
                var prev = Mathf.FloorToInt(frac * points.Length);
                var next = Mathf.CeilToInt(frac * points.Length) % points.Length;
                var t = Mathf.InverseLerp((float)prev / points.Length, (float)next / points.Length, frac);
                newPoints[i] = new SoftbodyPoint
                {
                    Position = Vector2.Lerp(points[prev].Position, points[next].Position, t),
                    PrevPosition = Vector2.Lerp(points[prev].PrevPosition, points[next].PrevPosition, t),
                    TargetPosition = Vector2.Lerp(points[prev].TargetPosition, points[next].TargetPosition, t)
                };
            }
        }
        points = newPoints;
    }

    private void FixedUpdate()
    {
        float deltaTime = Time.deltaTime / substeps;
        for (int step = 0; step < substeps; step++)
        {
            // Update Points
            for (int i = 0; i < pointCount; i++)
            {
                var point = points[i];

                var vel = point.Position - point.PrevPosition;

                var force = SpringForce(point.Position, point.TargetPosition, 0f);
                var prev = points[(i - 1 + pointCount) % pointCount];
                var next = points[(i + 1) % pointCount];
                force += SpringForce(point.Position, prev.Position, (prev.TargetPosition - point.TargetPosition).magnitude) * 0.5f;
                force += SpringForce(point.Position, next.Position, (next.TargetPosition - point.TargetPosition).magnitude) * 0.5f;
                vel += force * (tension * deltaTime);
                vel = Vector2.Lerp(vel, Vector2.zero, 1 - Mathf.Exp(-damping * deltaTime));

                vel += gravity * deltaTime;

                HandleCollision(ref point, ref vel);
                
                point.PrevPosition = point.Position;
                point.Position += vel;

                _nextPoints[i] = point;
            }

            // Apply changes
            (points, _nextPoints) = (_nextPoints, points);
        }
    }
    
    private void HandleCollision(ref SoftbodyPoint point, ref Vector2 velocity)
    {
        const float radius = 0.1f; // Define a small radius for each point
        float distance = velocity.magnitude;
    
        if (distance > 0f)
        {
            RaycastHit2D hit = Physics2D.CircleCast(point.Position, radius, velocity.normalized, distance);
    
            if (hit.collider != null)
            {
                // Move the point to the collision point, slightly offset to prevent sticking
                point.Position = hit.point + hit.normal * radius;
    
                // Reflect velocity based on surface normal
                velocity = Vector2.Reflect(velocity, hit.normal) * 0.8f; // Apply damping (0.8 reduces energy slightly)
            }
        }
    }

    private Vector2 SpringForce(Vector2 a, Vector2 b, float targetDistance)
    {
        var diff = a - b;
        var dir = diff.normalized;
        var dist = diff.magnitude;
        return dir * (targetDistance - dist);
    }

    public struct SoftbodyPoint
    {
        public Vector2 PrevPosition;
        public Vector2 Position;
        public Vector2 TargetPosition;
    }
}
