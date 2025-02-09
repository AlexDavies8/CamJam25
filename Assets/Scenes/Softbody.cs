using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Unity.VisualScripting;
using UnityEngine.U2D;

public class Spring
{
    public int u;
    public int v;
    public float rest_length;
    public float spring_coefficient;
    public float damping_coefficient; // big number => more damping
    public float min_length;
    public bool anchor = false;

    public Spring(int u, int v, float rest_length, float spring_coefficient, float damping_coefficient)
    {
        this.u = u;
        this.v = v;
        this.rest_length = rest_length;
        this.spring_coefficient = spring_coefficient;
        this.damping_coefficient = damping_coefficient;
    }
}

public class Point
{
    public Vector2 position;
    public Vector2 velocity;
    public bool anchor;

    public Point(Vector2 position, bool anchor)
    {
        this.position = position;
        this.velocity = new Vector2(0, 0);
        this.anchor = anchor;
    }
}

public class Softbody : MonoBehaviour
{
    public List<Point> points = new List<Point>();
    public List<Spring> springs = new List<Spring>();
    public List<Vector2> net_force = new List<Vector2>();
    public List<int> points_to_draw = new List<int>();
    public DeformableSprite fill;
    public void AddPoint(float x, float y, bool anchor)
    {
        points.Add(new Point(new Vector2(x, y), anchor));
        net_force.Add(Vector2.zero);
    }

    public void AddPoint(Vector2 v, bool anchor)
    {
        AddPoint(v.x, v.y, anchor);
    }

    public void AddPointToDraw(int x)
    {
        points_to_draw.Add(x);
    }

    public void AddSpring(int u, int v, float spring_coefficient)
    {
        springs.Add(new Spring(u, v, (points[u].position - points[v].position).magnitude, spring_coefficient, 0.3f));
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    
    int Compare(Vector2 a, Vector2 b, Vector2 center)
    {
        float angleA = Mathf.Atan2(a.y - center.y, a.x - center.x);
        float angleB = Mathf.Atan2(b.y - center.y, b.x - center.x);
        return angleA.CompareTo(angleB);
    }

    // Update is called once per frame
    void FixedUpdate()
    {

    }

    public void Physics()
    {
        //apply physics
        float dt = Time.fixedDeltaTime;
        int n = points.Count;
        foreach (Spring s in springs)
        {
            int u = s.u;
            int v = s.v;

            Vector2 d = points[v].position - points[u].position;
            float difference = d.magnitude - s.rest_length;
            float force = difference * s.spring_coefficient;
            d = d.normalized;

            net_force[u] += d * force - Vector2.Dot(d, points[u].velocity) * d * s.damping_coefficient;
            net_force[v] -= d * force - Vector2.Dot(d, points[v].velocity) * (-d) * s.damping_coefficient;
        }

        for (int i = 0; i < n; i++)
        {
            if (points[i].anchor)
                continue;
            points[i].velocity += net_force[i] * dt;
            points[i].position += points[i].velocity * dt;
            net_force[i] = Vector2.zero;
        }

        //draw
        LineRenderer l = this.GetComponent<LineRenderer>();

        List<int> ord = new List<int>();
        for (int i = 0; i < n; i++)
        {
            ord.Add(i);
        }

        Vector2 center = Vector2.zero;
        for (int i = 0; i < n; i++)
        {
            center += points[i].position;
        }
        center /= n;

        ord.Sort((int x, int y) => Compare(points[x].position, points[y].position, center));
        List<int> ord_inv = new List<int>();
        for (int i = 0; i < n; i++)
            ord_inv.Add(0);
        for (int i = 0; i < n; i++)
        {
            ord_inv[ord[i]] = i;
        }

        n = points_to_draw.Count;
        int prev = -10000;
        l.positionCount = n + 1;
        fill.vertices = new Vector2[n].ToList();
        foreach (int i in points_to_draw)
        {
            l.SetPosition(i, points[i].position);
            fill.vertices[i] = points[i].position;

            int distance = ord_inv[i] - prev;
            if (distance < -n / 2)
                distance = n;
            if (distance > 0)
            {
                prev = ord_inv[i];
            }
        }
        if (n > 0)
        {
            l.SetPosition(n, points[0].position);
        }
    }

    private void Update()
    {
        fill.UpdateMesh();
    }
}
