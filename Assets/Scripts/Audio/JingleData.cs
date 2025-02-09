using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
[CreateAssetMenu(fileName = "JingleData", menuName = "Music Data/JingleData")]
/**
 * Short, responsive jingles
 * Snappy, responds to in game events
 * Cares only about chords
 * No care for the underlying loop
 */
public class JingleData : ScriptableObject {
    public AudioClip clip;
    public double bpm;
    public double[] beatsUntilBar;   // Beats until the target chord change
    public bool barParity;      // Basically do we care about which bar it is
    public List<Chord> startChord;    // The starting chord the jingle expects to be on
    public List<Chord> endChord;      // The ending chord the jingle expects
    public string jingleName;   // The name of the jingle (used for triggering)
}
