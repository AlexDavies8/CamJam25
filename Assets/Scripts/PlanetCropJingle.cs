using System.Collections.Generic;
using UnityEngine;

public class PlanetCropJingle : MonoBehaviour
{
    public List<int> notePattern = new();
    public int maxError = 1;

    public PlayerController player;

    public GameObject cropPrefab;

    private void FixedUpdate()
    {
        if (PitchDetector.Instance.RecognisePattern(notePattern, maxError))
        {
            var planet = player.physics.closestPlanet;
            var cropPlanet = planet.gameObject.GetComponent<CropPlanet>();

            if (cropPlanet != null)
            {
                cropPlanet.SpawnCrops(planet.AngleTo(player.transform.position));
            }
        }
    }
}
