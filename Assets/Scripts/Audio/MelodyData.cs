using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class MelodyFollow {
    public string name;
    public string instrument;
    public string startGroup;
    public float weight;
}

[Serializable]
[CreateAssetMenu(fileName = "MelodyData", menuName = "Music Data/MelodyData")]
/**
 * Stores data for melodies
 * Can change the current musical loop
 * Does loop based matching:
 * No actual idea of chords
 * Only one instrument
 */
public class MelodyData : ScriptableObject {
    public AudioClip clip;
    public double beatsIntoLoop;        // The number of beats into the start loop the melody expects (Can be negative)
    public List<string> overLoop;       // The loops the melody expects to play over
    public int end;                     // The end number of loops, after which the instrument could be replaced by more melody
    public string instrument;           // The instrument the melody is in
    public string melName;              // The name of the melody (used to trigger)
    public double randomCall;           // If the melody can be randomly triggered
    public List<MelodyFollow> follows;  // 
    public float noFollowW;
    public int beatsToKill;
}
