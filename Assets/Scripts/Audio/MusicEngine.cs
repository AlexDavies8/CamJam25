using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IO.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class ListDictionary<Key, E> : Dictionary<Key, List<E>> {
    public void AddEl(Key k, E e) {
        if (ContainsKey(k)) {
            this[k].Add(e);
        } else {
            this[k] = new List<E>() { e, };
        }
    }
}

public class DictListDictionary<Key1, Key2, E> : Dictionary<Key1, Dictionary<Key2, List<E>>> {
    public void AddEl(Key1 k1, Key2 k2, E e) {
        if (ContainsKey(k1)) {
            if (this[k1].ContainsKey(k2)) {
                this[k1][k2].Add(e);
            } else {
                this[k1][k2] = new List<E>() { e, };
            }
        } else {
            this[k1] = new Dictionary<Key2, List<E>>() { { k2, new List<E>() { e, } }, };
        }
    }
}

public class RuntimeTrackData {
    public string startGroup;
    public Dictionary<string, double> beatsTable;
    public Dictionary<string, ChordProgression> chordsTable;
    public Dictionary<string, double> bpsTable;
    public ListDictionary<string, RuntimeLoop> loopTable;
    public DictListDictionary<string, string, MelodyData> melodies;
    public ListDictionary<string, JingleData> jingles;

    public RuntimeTrackData (TrackData d) {
        startGroup = d.startGroup;
        melodies = new();
        jingles = new();
        loopTable = new();
        beatsTable = new();
        chordsTable = new();
        bpsTable = new();
        foreach (var loop in d.loops) {
            var l = new RuntimeLoop(loop);
            loopTable.AddEl(loop.group, l);
            if (!beatsTable.ContainsKey(l.group)) {
                beatsTable[l.group] = l.beats;
                chordsTable[l.group] = l.chordProgression;
                bpsTable[l.group] = l.bps;
            }
        }
        foreach (var mel in d.melodies) {
            melodies.AddEl(mel.melName, mel.instrument, mel);
            Debug.Log(mel.melName);
        }
        foreach (var j in d.jingles) {
            jingles.AddEl(j.jingleName, j);
        }
    }
}

public class RuntimeMelody {
    public MelodyData d;
    public int loopsLeft;
    public double beats;   // Beats from the start of the melody
    public FullAudioSource playingOn;

    public RuntimeMelody(MelodyData d, int loopsLeft, double beats) {
        this.d = d;
        this.loopsLeft = loopsLeft;
        this.beats = beats;
        this.playingOn = null;
    }
}

public class RuntimeJingle {
    public JingleData d;
    public int barsLeft;
    public double beats;    // Beats until the next bar
    public FullAudioSource playingOn;

    public RuntimeJingle(JingleData d, int barsLeft, double beats) {
        this.d = d;
        this.barsLeft = barsLeft;
        this.beats = beats;
        this.playingOn = null;
    }
}

public class MusicEngine : MonoBehaviour {
    public static double delay = 0.1;

    public double bps;             // Current bps
    public double startTime;    // Start time for current section
    public double beats;        // Beats into the current loop
    public double nextStartTime;

    [SerializeField]
    private float loopVol;
    public float loopVolume {
        get => loopVol;
        set {
            loopVol = value;
            loopAudio.vol = value;
        }
    }
    [SerializeField]
    private float melVol;
    public float melodyVolume {
        get => melVol;
        set {
            melVol = value;
            foreach (var a in melAudio) {
                a.volume = value;
            }
        }
    }
    [SerializeField]
    private float jingleVol;
    public float jingleVolume {
        get => jingleVol;
        set {
            jingleVol = value;
            foreach (var a in jingleAudio) {
                a.volume = value;
            }
        }
    }

    public TrackData trackData;
    public RuntimeTrackData currentTrack;
    public double[] bars;
    public int bar;
    public double barStart;

    public ChordProgression prog;
    public int chordInd;
    public Chord chord => prog.chords[chordInd].chord;
    public double chordStart;

    public string currentGroup => currentLoop?.group;
    public RuntimeLoop currentLoop;

    public int queued;          // If something has been queued next. Priority system
    public RuntimeLoop nextLoop;

    public Dictionary<string, RuntimeMelody> melodies;          // Current melodies playing, by instrument
    public Dictionary<string, RuntimeMelody> queuedMelodies;    // Next melodies playing, by instrument

    public List<RuntimeJingle> jingles;                    // Current jingles playing / queued
    public List<RuntimeJingle> queuedJingles;

    public List<string> currLoops;                  // Next loops that must play for the currently playing melodies
    public List<string> queuedLoops;                // Next loops that must play for the queued melodies
    public int loopCount => currLoops.Count + queuedLoops.Count;

    public AudioPair loopAudio;
    [SerializeField]
    public FullAudioSource[] melAudio;
    [SerializeField]
    private List<FullAudioSource> freeAudio;
    [SerializeField]
    public FullAudioSource[] jingleAudio;
    [SerializeField]
    private List<FullAudioSource> freeJingleAudio;

    [Serializable]
    private struct MelodyQueueInfo {
        public string instrument;
        public int loopsLeft;

        public MelodyQueueInfo (string instrument, int loopsLeft) {
            this.instrument = instrument;
            this.loopsLeft = loopsLeft;
        }
    }

    [SerializeField]
    private List<MelodyQueueInfo> melodyInfo;
    [SerializeField]
    private List<MelodyQueueInfo> queuedMelodyInfo;

    [SerializeField]
    private List<string> availMelodies;
    [SerializeField]
    private List<string> availJingles;

    double buffer = 0.5;
    double prevTime = 0;

    int started;

    // Start is called before the first frame update
    void Start() {
        melodies = new();
        queuedMelodies = new();
        jingles = new();
        queuedJingles = new();
        started = 0;
        queued = 0;
        currentTrack = new RuntimeTrackData(trackData);
        if (currentTrack == null) {
            Debug.Log("No track??");
        }
        freeAudio = new(melAudio);
        freeJingleAudio = new(jingleAudio);
        double prevTime = AudioSettings.dspTime;
        loopVolume = loopVol;
        melodyVolume = melVol;
        jingleVolume = jingleVol;
        availMelodies = new(currentTrack.melodies.Keys);
        availJingles = new(currentTrack.jingles.Keys);
    }

    void Update() {
        melodyInfo.Clear();
        foreach (var kvp in melodies) {
            melodyInfo.Add(new MelodyQueueInfo(kvp.Key, kvp.Value.loopsLeft));
        }
        queuedMelodyInfo.Clear();
        foreach (var kvp in queuedMelodies) {
            queuedMelodyInfo.Add(new MelodyQueueInfo(kvp.Key, kvp.Value.loopsLeft));
        }
        if (currentTrack == null) {
            Debug.Log("No track??" + started.ToString());
        }

        double time = AudioSettings.dspTime;
        // Play scheduled stuff
        if (started < 3) {
            started++;
            if (started == 3) {
                startTime = time + buffer;
                QueueNextGroup(1, currentTrack.startGroup);
                started++;
                //PlayNext();
                //if (nextLoop != null) {
                    //Debug.Log("Already scheduled??");
                //}
                //Debug.Log(startTime.ToString());
                //Debug.Log(nextStartTime.ToString());
            }
        } else {

            if (currLoops.Count == 0) {
                currLoops.Add(currentGroup);
            }

            List<string> remove = new();

            // Play scheduled stuff
            if (nextStartTime > 0 && time > nextStartTime) {
                // Increment current loop
                PlayNext();
                currLoops.RemoveAt(0);
                bps = currentLoop.bps;
                bars = currentLoop.bars;
                bar = 0;
                barStart = 0;
                prog = currentLoop.chordProgression;
                chordInd = 0;
                chordStart = 0;
                foreach (var kvp in melodies) {
                    kvp.Value.loopsLeft--;
                    if (kvp.Value.loopsLeft == 0) {
                        remove.Add(kvp.Key);
                        freeAudio.Add(kvp.Value.playingOn);
                    }
                }
                foreach (var k in remove) {
                    melodies.Remove(k);
                }
                remove.Clear();
                foreach (var kvp in queuedMelodies) {
                    kvp.Value.loopsLeft--;
                }
            }

            beats = (time - startTime) * bps;
            if (bar < bars.Length - 1 && beats - barStart > bars[bar]) {
                barStart += bars[bar];
                bar++;
                foreach (var v in queuedJingles) {
                    v.barsLeft--;
                }
            }
            for (int i = 0; i < jingles.Count; ) {
                var v = jingles[i];
                if (v.d.clip.length - v.playingOn.source.time < 1) {
                    freeJingleAudio.Add(v.playingOn);
                    jingles.RemoveAt(i);
                } else {
                    i++;
                }
            }
            if (chordInd < prog.Count - 1 && beats - chordStart > prog.chords[chordInd].beats) {
                chordStart += prog.chords[chordInd].beats;
                chordInd++;
            }
            foreach (var kvp in queuedMelodies) {
                if (kvp.Value.loopsLeft == 0 && time > kvp.Value.d.beatsIntoLoop / bps + startTime) {
                    if (freeAudio.Count > 0) {
                        var a = freeAudio[^1];
                        freeAudio.RemoveAt(freeAudio.Count - 1);
                        a.clip = kvp.Value.d.clip;
                        a.source.PlayScheduled(startTime + kvp.Value.d.beatsIntoLoop / bps + delay);
                        kvp.Value.playingOn = a;
                        kvp.Value.loopsLeft = kvp.Value.d.end;
                        melodies[kvp.Key] = kvp.Value;
                        for (int i = currLoops.Count; i < kvp.Value.d.overLoop.Count; i++) {
                            currLoops.Add(queuedLoops[0]);
                            queuedLoops.RemoveAt(0);
                        }
                    } else {
                        Debug.Log("Failed to play melody due to lack of Audio Sources");
                    }
                    remove.Add(kvp.Key);
                }
                else if (kvp.Value.loopsLeft < 0) {
                    remove.Add(kvp.Key);
                }
            }
            foreach (var k in remove) {
                queuedMelodies.Remove(k);
            }
            remove.Clear();
            for (int i = 0; i < queuedJingles.Count; ) {
                var v = queuedJingles[i];
                if (v.barsLeft == 0 && beats - barStart > bars[bar] - v.beats) {
                    if (freeJingleAudio.Count > 0) {
                        var a = freeJingleAudio[^1];
                        freeJingleAudio.RemoveAt(freeJingleAudio.Count - 1);
                        a.clip = v.d.clip;
                        a.source.PlayScheduled(startTime + (barStart + bars[bar] - v.beats) / bps + delay);
                        v.playingOn = a;
                        v.barsLeft = 0;
                        jingles.Add(v);
                    } else {
                        Debug.Log("Failed to play jingle due to lack of Audio Sources");
                    }
                    queuedJingles.RemoveAt(i);
                } else {
                    i++;
                }
            }

            if (currentLoop != null) {
                // Schedule stuff
                if (nextStartTime == -1 && bar == bars.Length - 1) {
                    // Debug.Log("Scheduling: " + currentLoop.length.ToString());
                    if (currLoops.Count == 1 && queuedLoops.Count > 0) {
                        currLoops.Add(queuedLoops[0]);
                        queuedLoops.RemoveAt(0);
                    }
                    if (currLoops.Count > 1) {
                        QueueNextLoop(1, currLoops[1]);
                    } else {
                        QueueNextLoop(1, null);
                        currLoops.Add(nextLoop.group);
                    }
                    // Debug.Log("Scheduled: " + nextStartTime.ToString());
                }
            }
        }

        prevTime = time;
    }

    void PlayNext() {
        if (loopAudio == null) {
            Debug.Log("Null music audio");
        }
        if (nextLoop == null) {
            Debug.Log("Null next clip");
        }
        loopAudio.Play(nextLoop.clip, nextStartTime);
        
        startTime = nextStartTime;
        nextStartTime = -1;
        currentLoop = nextLoop;
        nextLoop = null;
        queued = 0;
    }

    public string GetLoop(int i) {
        Debug.Log("i: " + i.ToString() + " currLoops.Count " + currLoops.Count.ToString());
        if (i < currLoops.Count) {
            return currLoops[i];
        }
        else if (i - currLoops.Count < queuedLoops.Count) {
            return queuedLoops[i - currLoops.Count];
        }
        return null;
    }

    public Chord? GetChordAtNextBar() {
        double testChordStart = chordStart;
        int i = chordInd;
        while (testChordStart < barStart + bars[bar] && i < prog.Count) {
            testChordStart += prog.chords[i].beats;
            i++;
        }
        if (i < prog.Count) {
            return prog.chords[i].chord;
        }
        if (nextLoop is not null) {
            return nextLoop.chordProgression.chords[0].chord;
        }
        return null;
    }

    public Chord? GetChordAtNextNextBar() {
        double testChordStart = chordStart;
        double? nextBar = GetNextBarLength();
        if (nextBar is null) {
            return null;
        }
        double target = barStart + bars[bar] + nextBar.Value;
        int i = chordInd;
        while (testChordStart < barStart + bars[bar] && i < prog.Count) {
            testChordStart += prog.chords[i].beats;
            i++;
        }
        if (i < prog.Count) {
            return prog.chords[i].chord;
        }
        if (nextLoop is null) {
            return null;
        }
        testChordStart = 0;
        while (testChordStart < nextLoop.bars[0] && i < nextLoop.chordProgression.Count) {
            testChordStart += nextLoop.chordProgression.chords[i].beats;
            i++;
        }
        if (i < nextLoop.chordProgression.Count) {
            return nextLoop.chordProgression.chords[i].chord;
        }
        return null;
    }

    public double? GetNextBarLength() {
        if (bar < bars.Length - 1) {
            return bars[bar + 1];
        }
        else if (nextLoop is not null) {
            return nextLoop.bars[0];
        }
        return null;
    }

    public double BeatsUntilLoop(int i) {
        double beats = 0;
        for (int j = 0; j < i; ++i) {
            beats += currentTrack.beatsTable[GetLoop(j)];
        }
        return beats;
    }

    public bool CheckQueue(List<string> test, int startLoop) {
        Debug.Log("Checking queue " + startLoop.ToString());
        if (loopCount == 0) {
            return false;
        }
        for (int i = 0; i + startLoop < loopCount && i < test.Count; ++i) {
            string group = GetLoop(i + startLoop);
            Debug.Log(group is null);
            Debug.Log("Checking " + test[i] + " against " + group);
            if (test[i] != group) {
                return false;
            }
        }
        return true;
    }

    public void QueueNextGroup(int priority, string group) {
        if (queued < priority) {
            List<RuntimeLoop> candidates = currentTrack.loopTable[group];
            nextLoop = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            queued = priority;
            nextStartTime = startTime + (currentLoop?.length ?? 0);
        }
    }

    public void QueueNextLoop(int priority, string nextGroup) {
        if (queued < priority) {
            List<RuntimeLoop> candidates = new List<RuntimeLoop>();
            foreach (var kvp in currentLoop.nextGroups) {
                foreach (var loop in currentTrack.loopTable[kvp.Key]) {
                    if (nextGroup != null && !loop.nextGroups.ContainsKey(nextGroup)) {
                        continue;
                    }
                    candidates.Add(loop);
                }
            }
            List<double> weights = new List<double>();
            double totalWeight = 0;
            foreach (var loop in candidates) {
                double w = getWeighting(loop, currentLoop.nextGroups[loop.group], currentLoop.nextTags);
                weights.Add(w);
                totalWeight += w;
            }
            double choice = UnityEngine.Random.Range(0, 1);
            for (int i = 0; i < weights.Count; i++) {
                choice -= weights[i];
                if (choice < 0) {
                    nextLoop = candidates[i];
                    nextStartTime = startTime + (currentLoop?.length ?? 0);
                    queued = priority;
                    break;
                }
            }
        }
    }

    public double getWeighting(RuntimeLoop loop, double groupWeight, IDictionary<string, double> nextTags) {
        double totalWeight = groupWeight;
        if (nextTags != null) {
            foreach (var nextTag in nextTags) {
                if (loop.tags.Contains(nextTag.Key)) {
                    totalWeight += nextTag.Value;
                }
            }
        }
        return Math.Exp(totalWeight);
    }

    public void QueueMelody(int startLoop, RuntimeMelody d) {
        for (int i = Math.Max(0, loopCount - startLoop); i < d.d.overLoop.Count; ++i) {
            if (!(startLoop == 0) || !(i == 0)) {
                queuedLoops.Add(d.d.overLoop[i]);
            }
        }
        queuedMelodies[d.d.instrument] = d;
    }

    public void TryQueueMelody(string name) {
        TryQueueMelody(name, 2);
    }

    public bool TryQueueMelody(string name, int loops) {
        if (!currentTrack.melodies.ContainsKey(name)) {
            Debug.Log("Attempted to queue a unvailable nmelody " + name);
            return false;
        }
        var currCandidates = new List<MelodyData> ();
        var futureCandidates = new List<MelodyData>();
        foreach (var instr in currentTrack.melodies[name]) {
            if (!queuedMelodies.ContainsKey(instr.Key)) {
                if (melodies.TryGetValue(instr.Key, out var melody)) {
                    if (melody.loopsLeft < loops) {
                        futureCandidates.AddRange(instr.Value);
                    }
                } else {
                    currCandidates.AddRange(instr.Value);
                }
            }
        }
        // Debug.Log("Found " + (currCandidates.Count + futureCandidates.Count).ToString() + " potential candidates");
        // Candidates of next melody to play, with their starting beats
        var candidates = new List<(int i, RuntimeMelody r)>();

        foreach (var cand in currCandidates) {
            for (int i = 0; i < loops; i++) {
                if (i == 0 && beats > cand.beatsIntoLoop) {
                    continue;
                }
                if (CheckQueue(cand.overLoop, i)) {
                    candidates.Add((i, new RuntimeMelody(cand, i, cand.beatsIntoLoop)));
                    break;
                }
            }
        }

        foreach (var cand in futureCandidates) {
            for (int i = melodies[cand.instrument].loopsLeft; i < loops; ++i) {
                if (i == 0 && beats > cand.beatsIntoLoop) {
                    continue;
                }
                if (CheckQueue(cand.overLoop, i)) {
                    candidates.Add((i, new RuntimeMelody(cand, i, cand.beatsIntoLoop)));
                    break;
                }
            }
        }

        Debug.Log("Found " + (candidates.Count).ToString() + " candidates");
        if (candidates.Count == 0) {
            return false;
        }
        var choice = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        QueueMelody(choice.i, choice.r);
        return true;
    }

    public void QueueJingle(string name) {
        if (!currentTrack.jingles.ContainsKey(name)) {
            Debug.Log("Attempted to queue a unvailable jingle " + name);
            return;
        }
        var candidates = currentTrack.jingles[name];
        var finalCand = new List<RuntimeJingle>();
        double minTime = 10000;
        var nextChord = GetChordAtNextBar();
        foreach (var cand in candidates) {
            var timeUntil = bars[bar] - cand.beatsUntilBar;
            if (timeUntil > beats - barStart && bps < cand.bpm / 60 + 0.01 && bps > cand.bpm / 60 - 0.01 && cand.startChord.Contains(chord) && timeUntil <= minTime) {
                if (nextChord is null || cand.endChord.Contains(nextChord.Value) && timeUntil >= minTime - 0.01) {
                    finalCand.Add(new RuntimeJingle(cand, 0, cand.beatsUntilBar));
                } else {
                    finalCand.Clear();
                    finalCand.Add(new RuntimeJingle(cand, 0, cand.beatsUntilBar));
                    minTime = timeUntil;
                }
            }
        }
        if (finalCand.Count == 0) {
            var nextBarLength = GetNextBarLength();
            var nextNextChord = GetChordAtNextNextBar();
            if (nextBarLength is null) {
                foreach (var cand in candidates) {
                    var timeUntil = nextBarLength.Value - cand.beatsUntilBar;
                    if (timeUntil > 0 && bps < cand.bpm / 60 + 0.01 && bps > cand.bpm / 60 - 0.01 && (nextChord is null || cand.startChord.Contains(nextChord.Value)) && timeUntil <= minTime) {
                        if (nextNextChord is null || cand.endChord.Contains(nextNextChord.Value) && timeUntil >= minTime - 0.01) {
                            finalCand.Add(new RuntimeJingle(cand, 0, cand.beatsUntilBar));
                        } else {
                            finalCand.Clear();
                            finalCand.Add(new RuntimeJingle(cand, 0, cand.beatsUntilBar));
                            minTime = timeUntil;
                        }
                    }
                }
            }
        }
        if (finalCand.Count == 0) {
            foreach (var cand in candidates) {
                finalCand.Add(new RuntimeJingle(cand, 1, cand.beatsUntilBar));
            }
        }
        var choice = finalCand[UnityEngine.Random.Range(0, finalCand.Count)];
        queuedJingles.Add(choice);
    }
}
