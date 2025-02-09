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
    public PitchDetector pitchDetector;

    private void Awake()
    {
        if (!planet) planet = GetComponent<Planet>();
        if (!pitchDetector) pitchDetector = FindAnyObjectByType<PitchDetector>();
    }

    private void Update()
    {
        var t = Mathf.InverseLerp(remapFromMin, remapFromMax, pitchDetector.volume);
        var target = Mathf.Lerp(remapToMin, remapToMax, t);
        planet.radius = Mathf.Lerp(planet.radius, target, 1f - Mathf.Exp(-damping * Time.deltaTime));
    }
}