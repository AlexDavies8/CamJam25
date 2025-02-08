using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer)), ExecuteInEditMode]
public class DeformableSprite : MonoBehaviour
{
    public Sprite sprite;
    public Color color = Color.white;

    public List<Vector2> vertices = new();
    
    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private Triangulator triangulator;
    private MaterialPropertyBlock mpb;

    private void OnEnable()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        triangulator = new();
        mpb = new();
    }

    public void UpdateMesh()
    {
        if (vertices == null || vertices.Count < 3) return;

        if (!mesh)
        {
            mesh = new();
            mesh.name = "DynamicSprite";
        }

        var vertices3D = new Vector3[vertices.Count];
        for (int i = 0; i < vertices.Count; i++)
        {
            vertices3D[i] = vertices[i];
        }
        triangulator.points = vertices;
        var indices = triangulator.Triangulate();

        // TODO: Better UVs
        // From ChatGPT
        // Calculate UVs:
        // (For example, we compute a bounding box around the polygon and map vertices to [0,1])
        Vector2 min = vertices[0];
        Vector2 max = vertices[0];
        foreach (Vector2 v in vertices)
        {
            min = Vector2.Min(min, v);
            max = Vector2.Max(max, v);
        }
        Vector2 size = max - min;
        Vector2[] uvs = new Vector2[vertices.Count];
        for (int i = 0; i < vertices.Count; i++)
        {
            uvs[i] = new Vector2((vertices[i].x - min.x) / size.x, (vertices[i].y - min.y) / size.y);
        }
        
        mesh.vertices = vertices3D;
        mesh.triangles = indices;
        mesh.uv = uvs;
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;

        meshRenderer.GetPropertyBlock(mpb);
        if (sprite) mpb.SetTexture("_MainTex", sprite.texture);
        mpb.SetColor("_RendererColor", color);
        meshRenderer.SetPropertyBlock(mpb);
    }
}

// From ChatGPT
public class Triangulator
{
    public List<Vector2> points;

    /// <summary>
    /// Triangulate the polygon, returning an array of triangle vertex indices.
    /// </summary>
    public int[] Triangulate()
    {
        List<int> indices = new List<int>();
        int n = points.Count;
        if (n < 3)
            return indices.ToArray();

        int[] V = new int[n];
        // Determine if polygon vertices are in clockwise or counter-clockwise order
        if (Area() > 0)
        {
            for (int v = 0; v < n; v++)
                V[v] = v;
        }
        else
        {
            for (int v = 0; v < n; v++)
                V[v] = (n - 1) - v;
        }

        int nv = n;
        int count = 2 * nv; // error detection counter

        for (int v = nv - 1; nv > 2;)
        {
            if (count-- <= 0)
            {
                // Polygon is probably non-simple
                return indices.ToArray();
            }

            int u = v;
            if (nv <= u)
                u = 0;
            v = u + 1;
            if (nv <= v)
                v = 0;
            int w = v + 1;
            if (nv <= w)
                w = 0;

            if (Snip(u, v, w, nv, V))
            {
                int a = V[u];
                int b = V[v];
                int c = V[w];
                indices.Add(a);
                indices.Add(b);
                indices.Add(c);
                for (int s = v, t = v + 1; t < nv; s++, t++)
                {
                    V[s] = V[t];
                }
                nv--;
                count = 2 * nv;
            }
        }

        return indices.ToArray();
    }

    /// <summary>
    /// Returns twice the signed area of the polygon.
    /// </summary>
    private float Area()
    {
        int n = points.Count;
        float A = 0.0f;
        for (int p = n - 1, q = 0; q < n; p = q++)
        {
            Vector2 pval = points[p];
            Vector2 qval = points[q];
            A += pval.x * qval.y - qval.x * pval.y;
        }
        return A * 0.5f;
    }

    /// <summary>
    /// Check if we can “snip” a triangle from the polygon without intersecting any other edges.
    /// </summary>
    private bool Snip(int u, int v, int w, int n, int[] V)
    {
        int p;
        Vector2 A = points[V[u]];
        Vector2 B = points[V[v]];
        Vector2 C = points[V[w]];

        // Check if triangle is degenerate
        if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
            return false;

        for (p = 0; p < n; p++)
        {
            if ((p == u) || (p == v) || (p == w))
                continue;
            Vector2 P = points[V[p]];
            if (InsideTriangle(A, B, C, P))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Check if point P is inside triangle ABC.
    /// </summary>
    private bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
    {
        float ax = C.x - B.x;
        float ay = C.y - B.y;
        float bx = A.x - C.x;
        float by = A.y - C.y;
        float cx = B.x - A.x;
        float cy = B.y - A.y;
        float apx = P.x - A.x;
        float apy = P.y - A.y;
        float bpx = P.x - B.x;
        float bpy = P.y - B.y;
        float cpx = P.x - C.x;
        float cpy = P.y - C.y;

        float aCROSSbp = ax * bpy - ay * bpx;
        float cCROSSap = cx * apy - cy * apx;
        float bCROSScp = bx * cpy - by * cpx;

        return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
    }
}