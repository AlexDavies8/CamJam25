using System;
using System.Collections.Generic;
using UnityEngine;

public class ForestJingle : MonoBehaviour
{
    public PlayerController player;
    
    public List<Sprite> from;
    public List<Sprite> to;

    public List<int> jingle = new();
    public int maxError = 2;

    public void FixedUpdate()
    {
        if (PitchDetector.Instance.RecognisePattern(jingle, maxError))
        {
            var found = FindObjectsByType<StickToPlanet>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var obj in found)
            {
                var rend = obj.GetComponent<SpriteRenderer>();
                if (rend)
                {
                    var idx = from.IndexOf(rend.sprite);
                    if (idx >= 0) rend.sprite = to[idx];
                }
            }
        }
    }
}