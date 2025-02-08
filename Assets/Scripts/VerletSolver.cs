using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VerletSolver : MonoBehaviour
{
    public int substeps = 4;

    public List<SpringConstraint> springConstraints = new();
    public List<ChainConstraint> chainConstraints = new();
    public List<UnityCollisionConstraint> collisionConstraints = new();
    public Action<List<VerletPoint>, float> customConstraints = null;
    public List<VerletPoint> points = new();

    private void CreateFullyLinkedSoftbody(IEnumerable<Vector2> positions, float stiffness = 20f, float damping = 20f)
    {
        var start = points.Count;
        foreach (var pos in positions)
        {
            points.Add(new VerletPoint { currPos = pos, prevPos = pos });
        }
        var end = points.Count;

        for (int a = start; a < end; a++)
        {
            for (int b = start; b < end; b++)
            {
                if (a == b) continue;
                springConstraints.Add(new SpringConstraint { a = a, b = b, length = (points[b].currPos - points[a].currPos).magnitude, damping = damping, stiffness = stiffness });
            }
        }
    }

    public void AddUnityCollisionToAll(float radius = 0.1f)
    {
        for (int i = 0; i < points.Count; i++)
        {
            collisionConstraints.Add(new UnityCollisionConstraint { idx = i, radius = radius });
        }
    }

    private void FixedUpdate()
    {
        var dt = Time.deltaTime / substeps;
        
        for (int step = 0; step < substeps; step++)
        {
            customConstraints?.Invoke(points, dt);
            ApplySpringConstraints();
            ApplyChainConstraints();
            ApplyUnityCollisionConstraints();
            UpdatePositions(dt);
        }
    }

    private void UpdatePositions(float dt)
    {
        for (int i = 0; i < points.Count; i++)
        {
            var point = points[i];
            point.UpdatePosition(dt);
            points[i] = point;
        }
    }
    
    private void ApplySpringConstraints()
    {
        foreach (var constraint in springConstraints)
        {
            var a = constraint.a;
            var b = constraint.b;
            
            var diff = points[b].currPos - points[a].currPos;
            var mag = diff.magnitude;
            var dir = diff.normalized;

            var relVel = (points[a].currPos - points[a].prevPos) - (points[b].currPos - points[b].prevPos);
            var dampingForce = Vector2.Dot(relVel, dir) * constraint.damping;

            var forceMag = constraint.stiffness * (mag - constraint.length);

            var force = dir * (forceMag - dampingForce);

            var pointA = points[a];
            pointA.Accelerate(force);
            points[a] = pointA;
            
            var pointB = points[b];
            pointB.Accelerate(-force);
            points[b] = pointB;
        }
    }

    private void ApplyChainConstraints()
    {
        foreach (var constraint in chainConstraints)
        {
            var a = constraint.a;
            var b = constraint.b;

            var diff = points[b].currPos - points[a].currPos;
            var dir = diff.normalized;
            
            var delta = dir * constraint.length - diff;
            
            var pointA = points[a];
            pointA.currPos -= delta * 0.5f;
            points[a] = pointA;
            
            var pointB = points[b];
            pointB.currPos += delta * 0.5f;
            points[b] = pointB;
        }
    }

    private void ApplyUnityCollisionConstraints()
    {
        foreach (var constraint in collisionConstraints)
        {
            var point = points[constraint.idx];

            var other = Physics2D.OverlapCircle(point.currPos, constraint.radius);
            if (other)
            {
                var closest = other.ClosestPoint(point.currPos);
                var diff = closest - point.currPos;
                point.currPos += diff.normalized * (diff.magnitude - constraint.radius);
            }
            
            points[constraint.idx] = point;
        }
    }

    public struct UnityCollisionConstraint
    {
        public int idx;
        public float radius;
    }

    public struct ChainConstraint
    {
        public int a;
        public int b;
        public float length;
    }

    public struct SpringConstraint
    {
        public int a;
        public int b;
        public float length;
        public float stiffness;
        public float damping;
    }

    public struct VerletPoint
    {
        public Vector2 currPos;
        public Vector2 prevPos;
        public Vector2 accel;

        public void UpdatePosition(float dt)
        {
            var vel = currPos - prevPos;
            prevPos = currPos;
            currPos = currPos + vel + accel * (dt * dt);
            accel = Vector2.zero;
        }

        public void Accelerate(Vector2 acceleration)
        {
            accel += acceleration;
        }
    }
}