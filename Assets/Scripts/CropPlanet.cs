using UnityEngine;

public class CropPlanet : MonoBehaviour
{
    public GameObject cropPrefab;
    public GameObject decor;
    [Min(1)]
    public int maximumSpawnAmount = 5;

    public void SpawnCrops(float angle)
    {
        var cropAmount = Random.Range(1, maximumSpawnAmount);

        for (var i = 0; i < cropAmount; i++)
        {
            var crop = Instantiate(cropPrefab, decor.transform);

            crop.GetComponent<StickToPlanet>().stickPosition = (angle + Random.Range(-2, 2) / GetComponent<Planet>().radius) / (2 * Mathf.PI);
        }
    }
}
