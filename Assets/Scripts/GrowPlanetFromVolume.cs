using System;
using UnityEngine;

public class GrowPlanetFromVolume : MonoBehaviour
{
    public float remapFromMin = 400f;
    public float remapFromMax = 1200f;
    public float remapToMin = 5f;
    public float remapToMax = 7f;
    public float damping = 5f;
    
    public Planet planet;

    private void Awake()
    {
        if (!planet) planet = GetComponent<Planet>();
    }

    private void Update()
    {
        var t = Mathf.InverseLerp(remapFromMin, remapFromMax, PitchDetector.Instance.volume);
        var target = Mathf.Lerp(remapToMin, remapToMax, t);
        planet.radius = Mathf.Lerp(planet.radius, target, 1f - Mathf.Exp(-damping * Time.deltaTime));
    }
}