using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Unity.IO.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using static UnityEditor.Progress;

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
    public Dictionary<string, ChordTerm[]> chordsTable;
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
                chordsTable[l.group] = l.chordBars;
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
    public int bar;
    public double barStart;

    public ChordTerm[] prog;
    public int chordInd;
    public Chord chord => prog[chordInd].chord;
    public double chordStart;

    public string currentGroup => currentLoop?.group;
    public RuntimeLoop currentLoop;

    public int queued;          // If something has been queued next. Priority system
    public RuntimeLoop nextLoop;

    public Dictionary<string, RuntimeMelody> melodies = new();          // Current melodies playing, by instrument
    public Dictionary<string, RuntimeMelody> queuedMelodies = new();    // Next melodies playing, by instrument
    private List<RuntimeMelody> removingMelodies = new();

    public List<RuntimeJingle> jingles = new();                    // Current jingles playing / queued
    public List<RuntimeJingle> queuedJingles = new();

    public List<string> currLoops = new();                  // Next loops that must play for the currently playing melodies
    public List<string> queuedLoops = new();                // Next loops that must play for the queued melodies
    public int loopCount => currLoops.Count + queuedLoops.Count;

    public bool playing = true;

    public Queue<string> staleGroups = new();
    public int stalerMemory = 10;
    public Func<float, float> stalerFactor = (float x) => (Mathf.Exp(x / 10) - 1) + (x > 3 ? 0.5f : 0) + (x > 6 ? 1f : 0) + (x > 7 ? 2f : 0);
    public float stalerAlpha = 0.7f;

    public Dictionary<string, float> staleCounts() {
        var r = new Dictionary<string, float>();
        var alpha = Mathf.Pow(stalerAlpha, stalerMemory);
        foreach (var v in staleGroups) {
            if (r.TryGetValue(v, out var t)) { r[v] += alpha; alpha /= stalerAlpha; }
            else { r[v] = alpha; alpha /= stalerAlpha; }
        }
        return r;
    }

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

    [SerializeField]
    int started;

    // Start is called before the first frame update
    void Start() {
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
        if (playing) {
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
                    
                    if (staleGroups.Count == stalerMemory) {
                        staleGroups.Dequeue();
                    }
                    staleGroups.Enqueue(currentGroup);

                    bps = currentLoop.bps;
                    bar = 0;
                    barStart = 0;
                    prog = currentLoop.chordBars;
                    chordInd = 0;
                    chordStart = 0;
                    foreach (var v in queuedJingles) {
                        v.barsLeft--;
                    }
                    foreach (var kvp in melodies) {
                        kvp.Value.loopsLeft--;
                        if (kvp.Value.loopsLeft == 0) {
                            remove.Add(kvp.Key);
                            removingMelodies.Add(kvp.Value);
                        } else if (kvp.Value.loopsLeft == 1) {
                            ScheduleFollow(kvp.Value);
                        } else if (kvp.Value.loopsLeft < 0) {
                            remove.Add(kvp.Key);
                            if (kvp.Value.playingOn is not null) {
                                freeAudio.Add(kvp.Value.playingOn);
                            }
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
                if (bar < prog.Length - 1 && beats - barStart > prog[bar].beats) {
                    barStart += prog[bar].beats;
                    bar++;
                    foreach (var v in queuedJingles) {
                        v.barsLeft--;
                    }
                    for (int i = 0; i < removingMelodies.Count; ) {
                        var v = removingMelodies[i];
                        if (v.d.beatsToKill < beats) {
                            freeAudio.Add(v.playingOn);
                            removingMelodies.RemoveAt(i);
                        } else {
                            ++i;
                        }
                    }
                }
                for (int i = 0; i < jingles.Count;) {
                    var v = jingles[i];
                    if (v.d.clip.length - v.playingOn.source.time < 1) {
                        freeJingleAudio.Add(v.playingOn);
                        jingles.RemoveAt(i);
                    } else {
                        i++;
                    }
                }
                if (chordInd < prog.Length - 1 && beats - chordStart > prog[chordInd].beats) {
                    chordStart += prog[chordInd].beats;
                    chordInd++;
                }
                var scheduleFollow = new List<RuntimeMelody>();
                foreach (var kvp in queuedMelodies) {
                    if (kvp.Value.loopsLeft == 0 && time > kvp.Value.d.beatsIntoLoop / bps + startTime) {
                        if (freeAudio.Count > 0) {
                            var a = freeAudio[^1];
                            freeAudio.RemoveAt(freeAudio.Count - 1);
                            a.clip = kvp.Value.d.clip;
                            a.source.PlayScheduled(startTime + kvp.Value.d.beatsIntoLoop / bps + delay);
                            kvp.Value.playingOn = a;
                            kvp.Value.loopsLeft = kvp.Value.d.end;
                            if (kvp.Value.loopsLeft == 1) {
                                scheduleFollow.Add(kvp.Value);
                            }
                            melodies[kvp.Key] = kvp.Value;
                            for (int i = currLoops.Count; i < kvp.Value.d.overLoop.Count; i++) {
                                currLoops.Add(queuedLoops[0]);
                                queuedLoops.RemoveAt(0);
                            }
                        } else {
                            Debug.Log("Failed to play melody due to lack of Audio Sources");
                        }
                        remove.Add(kvp.Key);
                    } else if (kvp.Value.loopsLeft < 0) {
                        remove.Add(kvp.Key);
                    }
                }
                foreach (var k in remove) {
                    queuedMelodies.Remove(k);
                }
                foreach (var f in scheduleFollow) {
                    ScheduleFollow(f);
                }
                remove.Clear();
                for (int i = 0; i < queuedJingles.Count;) {
                    var v = queuedJingles[i];
                    if (v.barsLeft == 0 && beats - barStart > prog[bar].beats - v.beats) {
                        if (freeJingleAudio.Count > 0) {
                            var a = freeJingleAudio[^1];
                            freeJingleAudio.RemoveAt(freeJingleAudio.Count - 1);
                            a.clip = v.d.clip;
                            a.source.PlayScheduled(startTime + (barStart + prog[bar].beats - v.beats) / bps + delay);
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
                    if (nextStartTime == -1 && bar >= prog.Length - 2) {
                        // Debug.Log("Scheduling: " + currentLoop.length.ToString());
                        if (currLoops.Count == 1 && queuedLoops.Count > 0) {
                            currLoops.Add(queuedLoops[0]);
                            queuedLoops.RemoveAt(0);
                        }
                        if (currLoops.Count == 2 && queuedLoops.Count > 0) {
                            currLoops.Add(queuedLoops[0]);
                            queuedLoops.RemoveAt(0);
                        }
                        if (currLoops.Count > 2) {
                            QueueNextLoop(1, currLoops[1], currLoops[2]);
                        } else if (currLoops.Count > 1) {
                            QueueNextLoop(1, currLoops[1], null);
                        } else {
                            QueueNextLoop(1, null, null);
                            currLoops.Add(nextLoop.group);
                        }
                        // Debug.Log("Scheduled: " + nextStartTime.ToString());
                    }
                }
            }

            prevTime = time;
        }
    }

    public void Reset() {
        staleGroups.Clear();
        loopAudio.Stop();
        foreach (var a in melAudio) {
            a.source.Stop();
        }
        foreach (var a in jingleAudio) {
            a.source.Stop();
        }
        melodies.Clear();
        queuedMelodies.Clear();
        jingles.Clear();
        queuedJingles.Clear();
        playing = false;
        currentLoop = null;
        nextLoop = null;
        queued = 0;
        started = 0;
        currLoops.Clear();
        queuedLoops.Clear();
        bar = 0;
        barStart = 0;
        chordInd = 0;
        chordStart = 0;
        startTime = 0;
        nextStartTime = -1;
        bps = 0;
        beats = 0;
        freeAudio.Clear();
        freeAudio.AddRange(melAudio);
        freeJingleAudio.Clear();
        freeJingleAudio.AddRange(jingleAudio);
        loopVolume = loopVol;
        melodyVolume = melVol;
        jingleVolume = jingleVol;
    }

    public void StartPlaying() {
        playing = true;
    }

    public void ChangeTrack(TrackData d) {
        Reset();
        currentTrack = new RuntimeTrackData(d);
    }

    public void ChangeTrackAndStart(TrackData d) {
        Reset();
        currentTrack = new RuntimeTrackData(d);
        StartPlaying();
    }

    void ScheduleFollow(RuntimeMelody d) {
        if (d.d.follows.Count != 0) {
            var weights = new Dictionary<(string name, string instr, string group), float>();
            foreach (var follow in d.d.follows) {
                weights.Add((follow.name, follow.instrument, follow.startGroup), follow.weight);
            }
            TryQueueMelody("", 2, null, weights, d.d.noFollowW);
        }
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
        Debug.Log("Getting next bar length from bar " + bar.ToString() + " against bars " + prog.Length.ToString());
        if (bar < prog.Length - 1) {
            return prog[bar + 1].chord;
        }
        Debug.Log("Checking next loop bars");
        if (nextLoop is not null) {
            return nextLoop.chordBars[0].chord;
        }
        return null;
    }

    public Chord? GetChordAtNextNextBar() {
        Debug.Log("Getting next bar length from bar " + bar.ToString() + " against bars " + prog.Length.ToString());
        if (bar < prog.Length - 2) {
            return prog[bar + 2].chord;
        }
        if (bar == prog.Length - 2) {
            return nextLoop?.chordBars[0].chord;
        }
        if (bar == prog.Length - 1) {
            return nextLoop?.chordBars[1].chord;
        }
        return null;
    }

    public double? GetNextBarLength() {
        Debug.Log("Getting next bar length from bar " + bar.ToString() + " against bars " + prog.Length.ToString());
        if (bar < prog.Length - 1) {
            return prog[bar + 1].beats;
        }
        Debug.Log("Checking next loop bars");
        if (nextLoop is not null) {
            return nextLoop.chordBars[0].beats;
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

    public void QueueNextLoop(int priority, string thisGroup, string nextGroup) {
        if (queued < priority) {
            List<RuntimeLoop> candidates = new List<RuntimeLoop>();
            if (thisGroup is null) {
                foreach (var kvp in currentLoop.nextGroups) {
                    foreach (var loop in currentTrack.loopTable[kvp.Key]) {
                        if (nextGroup != null && !loop.nextGroups.ContainsKey(nextGroup)) {
                            continue;
                        }
                        candidates.Add(loop);
                    }
                }
            } else {
                foreach (var loop in currentTrack.loopTable[thisGroup]) {
                    if (nextGroup != null && !loop.nextGroups.ContainsKey(nextGroup)) {
                        continue;
                    }
                    candidates.Add(loop);
                }
            }
            List<float> weights = new List<float>();
            float totalWeight = 0;
            var staled = staleCounts();
            foreach (var loop in candidates) {
                float groupWeight = 0;
                if (staled.TryGetValue(loop.group, out var count)) {
                    Debug.Log("Staling " + loop.group + " by -" + stalerFactor(count).ToString());
                    groupWeight -= stalerFactor(count);
                }
                if (currentLoop.nextGroups.ContainsKey(loop.group)) {
                    groupWeight = currentLoop.nextGroups[loop.group];
                }
                float w = getWeighting(loop, groupWeight, currentLoop.nextTags);
                weights.Add(w);
                totalWeight += w;
            }
            float choice = UnityEngine.Random.Range(0, totalWeight);
            // Debug.Log(choice.ToString() + " chosen from " + weights.ToString());
            for (int i = 0; i < weights.Count; i++) {
                choice -= weights[i];
                if (choice <= 0.01) {
                    nextLoop = candidates[i];
                    nextStartTime = startTime + (currentLoop?.length ?? 0);
                    queued = priority;
                    break;
                }
            }
        }
    }

    public float getWeighting(RuntimeLoop loop, float groupWeight, IDictionary<string, float> nextTags) {
        float totalWeight = groupWeight;
        if (nextTags != null) {
            foreach (var nextTag in nextTags) {
                if (loop.tags.Contains(nextTag.Key)) {
                    totalWeight += nextTag.Value;
                }
            }
        }
        return Mathf.Exp(totalWeight);
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

    public bool TryQueueMelody(string name, int loops, string instrument = null, Dictionary<(string name, string instr, string group), float> nextWeights = null, float noFollowProb = -1000) {
        if (!currentTrack.melodies.ContainsKey(name) && nextWeights is null) {
            Debug.Log("Attempted to queue a unvailable melody " + name);
            return false;
        }
        var currCandidates = new List<MelodyData> ();
        var futureCandidates = new List<MelodyData>();
        if (nextWeights is null) {
            if (instrument is null) {
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
            } else {
                if (!queuedMelodies.ContainsKey(instrument) && currentTrack.melodies[name].ContainsKey(instrument)) {
                    if (melodies.TryGetValue(instrument, out var melody)) {
                        if (melody.loopsLeft < loops) {
                            futureCandidates.AddRange(currentTrack.melodies[name][instrument]);
                        }
                    } else {
                        currCandidates.AddRange(currentTrack.melodies[name][instrument]);
                    }
                }
            }
        } else {
            foreach (var kvp in nextWeights) {
                if (!queuedMelodies.ContainsKey(kvp.Key.instr) && currentTrack.melodies[kvp.Key.name].ContainsKey(kvp.Key.instr)) {
                    if (melodies.TryGetValue(kvp.Key.instr, out var melody)) {
                        if (melody.loopsLeft < loops) {
                            foreach (var cand in currentTrack.melodies[kvp.Key.name][kvp.Key.instr]) {
                                if (cand.overLoop[0] == kvp.Key.group) {
                                    futureCandidates.Add(cand);
                                }
                            }
                        }
                    } else {
                        foreach (var cand in currentTrack.melodies[kvp.Key.name][kvp.Key.instr]) {
                            if (cand.overLoop[0] == kvp.Key.group) {
                                currCandidates.Add(cand);
                            }
                        }
                    }
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
        List<float> weights = new();
        float totalWeight = 0f;

        var staled = staleCounts();
        float totalStaling = 0;
        foreach (var cand in candidates) {
            float w = 0;
            w += cand.r.d.end / 10;
            float staleCost = 0;
            foreach (var s in cand.r.d.overLoop) {
                if (staled.TryGetValue(s, out var count)) {
                    staleCost += stalerFactor(count);
                }
            }
            w -= staleCost / cand.r.d.overLoop.Count;
            totalStaling += staleCost / cand.r.d.overLoop.Count;
            w = Mathf.Exp(w);
            if (nextWeights is not null) {
                if (nextWeights.TryGetValue((cand.r.d.melName, cand.r.d.instrument, cand.r.d.overLoop[0]), out var v)) {
                    w += v;
                }
            }
            weights.Add(w);
            totalWeight += w;
        }
        if (nextWeights is not null) {
            totalWeight += Mathf.Exp(noFollowProb - totalStaling / candidates.Count);
        }

        var choice = UnityEngine.Random.Range(0, totalWeight);
        for (int i = 0; i < weights.Count; i++) {
            choice -= weights[i];
            if (choice <= 0.01) {
                QueueMelody(candidates[i].i, candidates[i].r);
                return true;
            }
        }
        return false;
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
        Debug.Log("Queueing Jingle at beat " + beats.ToString() + " with chord " + chord.ToString() + " and next chord " + nextChord.Value.ToString());
        foreach (var cand in candidates) {
            foreach (var startTime in cand.beatsUntilBar) {
                double timeUntil = prog[bar].beats - startTime;
                if (timeUntil > beats - barStart && bps < cand.bpm / 60 + 0.01 && bps > cand.bpm / 60 - 0.01 &&
                    (chord == Chord.Any || cand.startChord.Contains(Chord.Any) || cand.startChord.Contains(chord)) &&
                    timeUntil <= minTime + 0.01 &&
                    (nextChord is null || nextChord == Chord.Any || cand.endChord.Contains(Chord.Any) || cand.endChord.Contains(nextChord.Value))) {
                    if (timeUntil >= minTime - 0.01) {
                        finalCand.Add(new RuntimeJingle(cand, 0, startTime));
                    } else {
                        finalCand.Clear();
                        finalCand.Add(new RuntimeJingle(cand, 0, startTime));
                        minTime = timeUntil;
                    }
                }
            }
        }
        if (finalCand.Count == 0) {
            Debug.Log("Failed to find candidates for this bar, looking at next bar.");
            double? nextBarLength = GetNextBarLength();
            var nextNextChord = GetChordAtNextNextBar();
            minTime = 10000;
            if (nextBarLength is not null) {
                Debug.Log("Queueing Jingle at beat " + beats.ToString() + " with next chord " + nextChord.Value.ToString() + " and next next chord " + nextNextChord.ToString());
                foreach (var cand in candidates) {
                    foreach (var startTime in cand.beatsUntilBar) {
                        var timeUntil = nextBarLength.Value - startTime;
                        if (timeUntil >= -0.05 && bps < cand.bpm / 60 + 0.01 && bps > cand.bpm / 60 - 0.01 &&
                            (nextChord is null || nextChord.Value == Chord.Any || cand.startChord.Contains(Chord.Any) || cand.startChord.Contains(nextChord.Value)) &&
                            timeUntil <= minTime + 0.01 &&
                            (nextNextChord is null || nextNextChord.Value == Chord.Any || cand.endChord.Contains(Chord.Any) || cand.endChord.Contains(nextNextChord.Value))) {
                            if (timeUntil >= minTime - 0.01) {
                                finalCand.Add(new RuntimeJingle(cand, 1, startTime));
                            } else {
                                finalCand.Clear();
                                finalCand.Add(new RuntimeJingle(cand, 1, startTime));
                                minTime = timeUntil;
                            }
                        }
                    }
                }
            } else {
                Debug.Log("Next bar length is null, failed to check next bar.");
            }
        }
        if (finalCand.Count == 0) {
            Debug.Log("Failed to find candidates for next bar, choosing randomly.");
            foreach (var cand in candidates) {
                foreach (var startTime in cand.beatsUntilBar) {
                    double timeUntil = prog[bar].beats - startTime;
                    if (timeUntil > beats - barStart && bps < cand.bpm / 60 + 0.01 && bps > cand.bpm / 60 - 0.01 &&
                        timeUntil <= minTime + 0.01) {
                        if (timeUntil >= minTime - 0.01) {
                            finalCand.Add(new RuntimeJingle(cand, 0, startTime));
                        } else {
                            finalCand.Clear();
                            finalCand.Add(new RuntimeJingle(cand, 0, startTime));
                            minTime = timeUntil;
                        }
                    }
                }
            }
        }
        if (finalCand.Count == 0) {
            Debug.Log("Failed to find candidates for this bar, looking at next bar.");
            double? nextBarLength = GetNextBarLength();
            minTime = 10000;
            if (nextBarLength is not null) {
                foreach (var cand in candidates) {
                    foreach (var startTime in cand.beatsUntilBar) {
                        var timeUntil = nextBarLength.Value - startTime;
                        if (timeUntil >= -0.05 && bps < cand.bpm / 60 + 0.01 && bps > cand.bpm / 60 - 0.01 &&
                            timeUntil <= minTime + 0.01) {
                            if (timeUntil >= minTime - 0.01) {
                                finalCand.Add(new RuntimeJingle(cand, 1, startTime));
                            } else {
                                finalCand.Clear();
                                finalCand.Add(new RuntimeJingle(cand, 1, startTime));
                                minTime = timeUntil;
                            }
                        }
                    }
                }
            } else {
                Debug.Log("Next bar length is null, failed to check next bar.");
            }
        }
        var choice = finalCand[UnityEngine.Random.Range(0, finalCand.Count)];
        queuedJingles.Add(choice);
    }
}
