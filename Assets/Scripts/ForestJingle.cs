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
    public float dist = 5f;
    
    MusicEngine musicEngine;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        musicEngine = GameObject.FindGameObjectsWithTag("Music")[0].GetComponent<MusicEngine>();
    }
    
    public void FixedUpdate()
    {
        if (PitchDetector.Instance.RecognisePattern(jingle, maxError))
        {
            var found = FindObjectsByType<StickToPlanet>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var obj in found)
            {
                var rend = obj.GetComponent<SpriteRenderer>();
                if (rend && Vector2.Distance(rend.transform.position, player.transform.position) < dist)
                {
                    var idx = from.IndexOf(rend.sprite);
                    if (idx >= 0) rend.sprite = to[idx];
                }
            }
            musicEngine.QueueJingle("Tree");
        }
    }
}