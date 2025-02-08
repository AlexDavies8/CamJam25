using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "TrackData", menuName = "Music Data/TrackData")]
public class TrackData : ScriptableObject
{
    public string startGroup;
    public List<LoopData> loops;
    public List<MelodyData> melodies;
    public List<JingleData> jingles;
}
