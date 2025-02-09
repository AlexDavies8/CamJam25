using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

public class PitchDetector : MonoBehaviour
{
    public string device;

    public float startThreshold = 300f;
    public float endThreshold = 200f;
    public float minLength = 0.1f;
    public float maxPause = 0.1f;
    public float historyLength = 10f;

    private AudioClip clip;
    private int sampleRate = 44100;
    private int bufferSize = 1024 * 16;

    public float volume { get; private set; }
    public List<Note> recentNotes = new();

    private bool hasCurrentNote;
    private Note currentNote;

    [Serializable]
    public struct Note
    {
        public float start;
        public float end;
        public int note;
    }

    private void Awake()
    {
        if (Microphone.devices.Length > 0)
        {
            device = Microphone.devices[0];
            clip = Microphone.Start(device, true, 1, sampleRate);
        }
    }

    private void Update()
    {
        if (Microphone.IsRecording(device))
        {
            float[] samples = GetSamples();
            if (samples is null) return;
            var buckets = BucketNotes(FFT(samples), sampleRate);

            double vol = 0;
            var max = 0;
            for (int i = 0; i < buckets.Length; i++)
            {
                if (buckets[i] > buckets[max]) max = i;
                vol += buckets[i];
            }
            volume = (float)vol;
            
            //string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
            if (!hasCurrentNote && buckets[max] > startThreshold)
            {
                hasCurrentNote = true;
                currentNote = new Note { start = Time.time, note = max };
            }
            if (hasCurrentNote)
            {
                var note = currentNote;
                if (max == note.note && buckets[max] > endThreshold)
                {
                    note.end = Time.time;
                }
                if (max != note.note && buckets[max] > startThreshold)
                {
                    if (note.end - note.start > minLength) recentNotes.Add(note);
                    note = new Note { start = Time.time, end = Time.time, note = max };
                }

                if (Time.time - note.end > maxPause)
                {
                    if (note.end - note.start > minLength) recentNotes.Add(note);
                    hasCurrentNote = false;
                }
                else currentNote = note;
            }

            for (int i = 0; i < recentNotes.Count; i++)
            {
                if (Time.time - recentNotes[i].end < historyLength) break;
                recentNotes.RemoveAt(0);
            }

            int[] targets = new[] { 0, 5, 4, 2, 4, 0 };
            int errors = 0;
            int idx = 0;
            for (int i = 0; i < recentNotes.Count; i++)
            {
                if (idx >= targets.Length) break;
                if (recentNotes[i].note == targets[idx])
                {
                    idx++;
                } else if (idx > 0) errors++;
            }
            if (idx >= targets.Length && errors <= 1)
            {
                Debug.Log("MAGIC!!!");
                recentNotes.Clear();
            }
        }
    }

    private float[] GetSamples()
    {
        if (clip is null) return null;

        var micPos = Microphone.GetPosition(device);
        if (micPos < bufferSize) return null;

        var samples = new float[bufferSize];
        clip.GetData(samples, micPos - bufferSize);
        return samples;
    }

    private static double[] BucketNotes(Complex[] fft, int sampleRate)
    {
        const double A4 = 440.0;
        
        var n = fft.Length;

        double[] buckets = new double[12];

        for (int i = 0; i < n / 2; i++)
        {
            var freq = i * (sampleRate / (double)n);
        
            var semitonesFromA4 = (int)Math.Round(12 * Math.Log(freq / A4, 2));
        
            // Double mod to deal with negatives
            var noteIndex = ((semitonesFromA4 + 9) % 12 + 12) % 12; // +9 to align A4 to index 9

            buckets[noteIndex] += fft[i].Magnitude;
        }

        return buckets;
    }

    private static double FindDominantFrequency(Complex[] fft, int sampleRate)
    {
        var n = fft.Length;
        var maxMag = 0.0;
        var maxIdx = 0;

        for (int i = 0; i < n / 2; i++)
        {
            var mag = fft[i].Magnitude;
            if (mag > maxMag)
            {
                maxMag = mag;
                maxIdx = i;
            }
        }

        return maxIdx * (sampleRate / (double)n);
    }

    private static Complex[] FFT(float[] input)
    {
        var complexInput = new Complex[input.Length];
        for (int i = 0; i < input.Length; i++) complexInput[i] = new Complex(input[i], 0);
        return FFT(complexInput);
    }

    private static Complex[] FFT(Complex[] x)
    {
        if (x.Length == 1) return new[] { x[0] };

        var even = new Complex[x.Length / 2];
        var odd = new Complex[x.Length / 2];
        for (int i = 0; i < x.Length / 2; i++)
        {
            even[i] = x[2 * i];
            odd[i] = x[2 * i + 1];
        }

        even = FFT(even);
        odd = FFT(odd);

        var combined = new Complex[x.Length];
        for (int k = 0; k < x.Length / 2; k++)
        {
            var t = Complex.Exp(-2 * Math.PI * Complex.ImaginaryOne * k / x.Length) * odd[k];
            combined[k] = even[k] + t;
            combined[k + x.Length / 2] = even[k] - t;
        }

        return combined;
    }
    
    static string FrequencyToNote(double frequency)
    {
        if (frequency <= 0) return "Unknown";

        int midiNote = (int)Math.Round(69 + 12 * Math.Log(frequency / 440.0, 2));
        string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
    
        int octave = (midiNote / 12) - 1;
        string note = noteNames[midiNote % 12];

        return $"{note}{octave}";
    }
}