using UnityEngine;
using UnityEngine.U2D;

[RequireComponent(typeof(Planet))]
public class PlanetRenderer : MonoBehaviour
{
    [Header("Setup")]
    public LineRenderer outline;
    public SpriteShapeController fill;
    
    [Header("Config")]
    public int resolution = 50;

    private Planet planet;

    private void Awake()
    {
        planet = GetComponent<Planet>();
        outline.positionCount = resolution;
        fill.spline.Clear();
        for (int i = 0; i < resolution; i++) fill.spline.InsertPointAt(i, Vector2.one * i);
    }
    
    private void Update()
    {
        for (int i = 0; i < resolution; i++)
        {
            var frac = (float)i / resolution;
            var dir = new Vector2(Mathf.Cos(frac * Mathf.PI * 2), Mathf.Sin(frac * Mathf.PI * 2));
            var dist = planet.SurfaceDistance(dir);
            var pos = dir * dist;
            outline.SetPosition(i, pos);
            fill.spline.SetPosition(i, pos);
        }
    }
}