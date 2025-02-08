using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class Planet : MonoBehaviour
{
    [Header("Setup")]
    public StaticSoftbody softbody;
    public PolygonCollider2D polygonCollider;
    public SpriteShapeController spriteShape;
    public LineRenderer lineRenderer;

    [Header("Planet Settings")]
    public float mouseStrength = 10f;
    public float radius = 5f;
    public float wobbleStrength = 0.1f;
    public List<NoiseLayer> noiseLayers = new();

    private void Awake()
    {
        polygonCollider.points = new Vector2[softbody.pointCount];
        spriteShape.spline.Clear();
        for (int i = 0; i < softbody.pointCount; i++)
        {
            spriteShape.spline.InsertPointAt(i, i * Vector2.one);
        }
        lineRenderer.positionCount = softbody.pointCount;
    }

    private void FixedUpdate()
    {
        var centre = Vector2.zero;

        for (int i = 0; i < softbody.pointCount; i++)
        {
            centre += softbody.points[i].Position;
        }

        centre /= softbody.pointCount;
        
        // Update Target Positions
        for (int i = 0; i < softbody.pointCount; i++)
        {
            var point = softbody.points[i];
            
            var frac = (float)i / softbody.pointCount;
            var x = Mathf.Cos(frac * Mathf.PI * 2);
            var y = Mathf.Sin(frac * Mathf.PI * 2);
            var dir = new Vector2(x, y);
            var dist = radius;
            foreach (var layer in noiseLayers)
            {
                var t = Time.time * layer.frequency + layer.offset + frac * layer.waveCount;
                dist += Mathf.Sin(t * Mathf.PI * 2) * layer.amplitude * wobbleStrength;
            }
            point.TargetPosition = centre + dir * dist;
            
            softbody.points[i] = point;
        }
        
        // Push Point Positions
        var newPoints = polygonCollider.points;
        for (int i = 0; i < softbody.points.Length; i++)
        {
            var pos = softbody.points[i].Position;
            newPoints[i] = pos;
            spriteShape.spline.SetPosition(i, pos);
            lineRenderer.SetPosition(i, pos);
        }
        polygonCollider.points = newPoints;
        spriteShape.RefreshSpriteShape();

        if (Input.GetMouseButton(0))
        {
            var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            for (int i = 0; i < softbody.pointCount; i++)
            {
                var point = softbody.points[i];

                var diff = point.Position - (Vector2)mousePos;
                var force = diff.normalized / Mathf.Max(1f, diff.magnitude);
                point.Position += force * (Time.deltaTime * mouseStrength);
            
                softbody.points[i] = point;
            }
        }
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