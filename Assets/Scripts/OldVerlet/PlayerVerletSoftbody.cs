using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerVerletSoftbody : MonoBehaviour
{
    public PlayerController player;
    public VerletSolver solver;
    public LineRenderer lineRenderer;

    public int pointCount = 20;
    public float stiffness = 20f;
    public float damping = 20f;
    public float internalStiffness = 20f;
    public float radius = 1f;
    public float followStrength = 1f;
    public float followDamping = 1f;
    public float gravityStrength = 1f;

    private void Awake()
    {
        var points = new Vector2[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            var angle = Mathf.PI * 2 * i / pointCount;
            points[i] = (Vector2)player.transform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        solver.CreateFullyLinkedSoftbody(points, stiffness, damping);
        //solver.AddUnityCollisionToAll();
        solver.AddPoint(player.transform.position);
        for (int i = 0; i < pointCount; i++)
        {
            solver.springConstraints.Add(new VerletSolver.SpringConstraint { a = i, b = pointCount, damping = damping, stiffness = internalStiffness, length = radius });
        }

        solver.customConstraints += FollowPlayerContraint;

        lineRenderer.positionCount = pointCount;
    }

    private void FollowPlayerContraint(List<VerletSolver.VerletPoint> points, int steps, float dt)
    {
        var target = (Vector2)player.transform.position;
        
        for (int i = 0; i < points.Count; i++)
        {
            var point = points[i];

            var diff = target - point.currPos;
            var vel = (point.currPos - point.prevPos) / dt;
            point.Accelerate(diff * followStrength - vel * followDamping);
            
            var planetCoreDelta = (Vector2)player.physics.closestPlanet.transform.position - target;
            point.Accelerate(planetCoreDelta.normalized * (gravityStrength * player.physics.closestPlanet.gravity) / planetCoreDelta.magnitude);

            point.currPos = target + Vector2.ClampMagnitude(point.currPos - target, radius);
            
            var planet = player.physics.closestPlanet;
            var planetDiff = (Vector2)planet.transform.position - point.currPos;
            var angle = planet.AngleTo(point.currPos);
            var surface = planet.SurfaceHeight(angle);
            if (planetDiff.magnitude < surface) point.currPos = planet.SurfacePoint(angle);
            
            points[i] = point;
        }
    }

    private void FixedUpdate()
    {
        // var point = solver.points[pointCount];
        // point.currPos = Vector2.MoveTowards(point.currPos, player.transform.position, Time.deltaTime * 100f);
        // solver.points[pointCount] = point;
        
        for (int i = 0; i < pointCount; i++)
        {
            lineRenderer.SetPosition(i, solver.points[i].currPos);
        }
    }
}