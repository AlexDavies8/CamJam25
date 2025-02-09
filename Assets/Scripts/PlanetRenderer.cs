using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class PlanetRenderer : MonoBehaviour
{
    [Header("Setup")]
    public LineRenderer outline;
    public DeformableSprite fill;

    [SerializeField] private Planet planet;
    
    [Header("Config")]
    public int resolution = 50;
    public float inset = 0f;

    private void Awake()
    {
        if (!planet) planet = GetComponent<Planet>();
        UpdateResolution();
    }

    private void OnValidate()
    {
        if ((outline && resolution != outline.positionCount) || (fill && (fill.vertices is null || resolution != fill.vertices.Count)))
        {
            UpdateResolution();
        }
    }

    private void UpdateResolution()
    {
        if (outline) outline.positionCount = resolution;
        if (fill) fill.vertices = new Vector2[resolution].ToList();
    }

    private void Update()
    {
        for (int i = 0; i < resolution; i++)
        {
            var angle = Mathf.PI * 2f * i / resolution;
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var dist = planet.SurfaceHeight(angle) - inset;
            var pos = dir * dist;
            if (outline) outline.SetPosition(i, pos);
            if (fill) fill.vertices[i] = pos;
        }
        if (fill) fill.UpdateMesh();
    }
}