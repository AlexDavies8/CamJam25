using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MelInstPair {
    public string name;
    public string instr;
}

public class TriggerMelody : MonoBehaviour
{
    MusicEngine musicEngine;

    [SerializeField]
    private float musicFrequency = 0.005f;
    private float timeSinceLastMel = 0;
    [SerializeField]
    private float probGrowthFact = 0.0001f;
    [SerializeField]
    private List<MelInstPair> options = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        musicEngine = GameObject.FindGameObjectsWithTag("Music")[0].GetComponent<MusicEngine>();
    }

    // Update is called once per frame
    void FixedUpdate() {
        timeSinceLastMel += Time.fixedDeltaTime;
        if (UnityEngine.Random.Range(0f, 1f) < musicFrequency + (timeSinceLastMel * probGrowthFact)) {
            var choice = options[UnityEngine.Random.Range(0, options.Count)];
            musicEngine.TryQueueMelody(choice.name, 2, choice.instr);
            timeSinceLastMel = 0f;
        }

    }
}
