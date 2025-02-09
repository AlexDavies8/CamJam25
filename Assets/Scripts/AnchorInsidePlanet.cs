using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class AnchorInsidePlanet : MonoBehaviour
{
    public Planet planet;
    [Min(0)] public float centreContribution = 1f;
    public List<float> anchorAngles = new();
    
    private void OnEnable()
    {
        if (!planet) planet = GetComponentInParent<Planet>();
    }

    private void Update()
    {
        if (!planet) return;

        var pos = planet.transform.position * centreContribution;
        foreach (var angle in anchorAngles)
        {
            pos += (Vector3)planet.SurfacePoint(angle);
        }
        transform.position = pos / (centreContribution + anchorAngles.Count);
    }

    private void OnDrawGizmosSelected()
    {
        foreach (var angle in anchorAngles)
        {
            Debug.DrawLine(transform.position, planet.SurfacePoint(angle), Color.black);
        }
    }
}