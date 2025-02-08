using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum Tag {
    Intro, Melodic, NonMelodic, Special, Harp, Guitar, Oboe, Saxophone, Horn, Flute, Violin
}

[Serializable]
public enum Chord {
    A, Am, Bb, Bbm, B, Bm, C, Cm, Db, CSm, D, Dm, Eb, Ebm, E, Em, F, Fm, FS, FSm, G, Gm, Ab, GSm, None
}

[Serializable]
public struct NextGroup {
    public string next;
    public double weighting;

    public NextGroup(string next, double weighting) { this.next = next; this.weighting = weighting; }
}

[Serializable]
public struct NextTag {
    public string tag;
    public double weighting;
}

[Serializable]
public struct ChordTerm {
    public Chord chord;
    public double beats;
}

[Serializable]
public struct ChordProgression {
    public ChordTerm[] chords;

    public int Count => chords.Length;

    public bool Match(ChordProgression other) => Match(chords, other.chords);

    public static bool Match(ChordTerm[] a, ChordTerm[] b) {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; ++i) {
            if (a[i].chord != b[i].chord || a[i].beats != b[i].beats) return false;
        }
        return true;
    }

}

[Serializable]
[CreateAssetMenu(fileName = "LoopData", menuName = "Music Data/LoopData")]
/**
 * Stores the data for a musical loop component
 * A musical loop:
 * Must have the same looping chord progression
 * Must have the same lengths
 * May change by itself, but normally changed by melodies
 * Extension: Different variations
 * Extension: Support for different drum loops
 */
public class LoopData : ScriptableObject {
    public AudioClip clip;
    public double bpm;                          // Tempo
    public string group;                        // What group the loop component represents
    public List<string> tags;                   // Useful tags
    public List<NextGroup> nextGroups;          // What next groups to choose from
    public List<NextTag> nextTags;              // Tag alterations
    public ChordProgression chordProgression;   // Loop's chord progression
    public double[] bars;                       // Bar lengths
}

public class RuntimeLoop {
    public AudioClip clip;
    public string group;
    public double bps;
    public HashSet<string> tags;
    public Dictionary<string, double> nextGroups;
    public Dictionary<string, double> nextTags;
    public ChordProgression chordProgression;       // Loop's chord progression
    public double beats;
    public double length;
    public double[] bars;

    public RuntimeLoop(LoopData d) {
        clip = d.clip;
        group = d.group;
        bps = d.bpm / 60;
        tags = new();
        foreach (var tag in d.tags) {
            tags.Add(tag);
        }
        nextGroups = new();
        foreach (var next in d.nextGroups) {
            nextGroups[next.next] = next.weighting;
        }
        nextTags = new();
        foreach(var next in d.nextTags) {
            nextTags[next.tag] = next.weighting;
        }
        chordProgression = d.chordProgression;
        beats = 0;
        foreach (var beat in d.bars) {
            beats += beat;
        }
        length = beats / bps;
        bars = d.bars;
    }
}
