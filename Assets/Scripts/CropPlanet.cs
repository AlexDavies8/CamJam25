using System.Collections.Generic;
using UnityEngine;

public class CropPlanet : MonoBehaviour
{
    public List<GameObject> cropPrefabs;
    public GameObject decor;
    [Min(1)]
    public int maximumSpawnAmount = 5;

    private readonly List<GameObject> crops = new();
    private readonly List<float> cropsLifeLeft = new();

    private const float CROP_LIFE_LENGTH = 20f;

    public void SpawnCrops(float angle)
    {
        var cropAmount = Random.Range(1, maximumSpawnAmount);

        for (var i = 0; i < cropAmount; i++)
        {
            var prefab = cropPrefabs[Random.Range(0, cropPrefabs.Count)];
            var crop = Instantiate(prefab, decor.transform);

            crop.GetComponent<StickToPlanet>().stickPosition = (angle + Random.Range(-2, 2) / GetComponent<Planet>().radius) / (2 * Mathf.PI);

            crops.Add(crop);
            cropsLifeLeft.Add(CROP_LIFE_LENGTH);
        }
    }

    private void Update()
    {
        for (var i = crops.Count - 1; i >= 0; i--)
        {
            cropsLifeLeft[i] -= Time.deltaTime;

            if (cropsLifeLeft[i] <= 0) {
                Destroy(crops[i]);

                crops.RemoveAt(i);
                cropsLifeLeft.RemoveAt(i);
            }
        }
    }
}
