using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEngine;

public class PitchDetector : MonoBehaviour
{
    public static PitchDetector Instance;
    
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
        if (Instance)
        {
            Destroy(this);
            return;
        }
        else Instance = this;
        
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

            for (int i = 0; i < recentNotes.Count; i++) // Clean up old notes
            {
                if (Time.time - recentNotes[i].end < historyLength) break;
                recentNotes.RemoveAt(0);
            }
        }
    }

    public bool RecognisePattern(List<int> notes, int maxDistance)
    {
        if (recentNotes.Count < notes.Count) return false;
        var match = FuzzySubarrayMatch(recentNotes.Select(note => note.note).ToList(), notes, maxDistance);
        if (match.start < 0) return false;
        recentNotes.RemoveRange(0, match.start + match.length); // Consume everything older than matched notes (and the matched ones)
        return true;
    }
    
    // From ChatGPT
    private static (int start, int length, int distance) FuzzySubarrayMatch(List<int> text, List<int> pattern, int maxDistance)
    {
        const int InsertionDeletionCost = 2; // cost for an extra/missing number
        const int OffByOneCost = 1;          // cost if a number is off by one
        const int ExactMatchCost = 0;
        const int MismatchCost = int.MaxValue / 2;
        
        int n = text.Count;
        int m = pattern.Count;
        // We'll build a DP matrix where dp[i,j] is the cost to match pattern[0..i-1] with text[0..j-1]
        int[,] dp = new int[m + 1, n + 1];

        // When pattern is empty, cost is 0 regardless of j (we allow matching at any point)
        for (int j = 0; j <= n; j++)
        {
            dp[0, j] = 0;
        }
        // When text is empty but pattern is not, we have to “delete” all items from the pattern.
        for (int i = 1; i <= m; i++)
        {
            dp[i, 0] = i * InsertionDeletionCost;
        }

        // To keep track of the best match (if any)
        int bestDistance = int.MaxValue;
        int bestEndIndex = -1;

        // Fill the DP table column by column (i.e. as we slide the pattern along the text)
        for (int j = 1; j <= n; j++)
        {
            for (int i = 1; i <= m; i++)
            {
                int costSubstitution;
                if (pattern[i - 1] == text[j - 1])
                {
                    costSubstitution = ExactMatchCost;
                }
                else if (Math.Abs(pattern[i - 1] - text[j - 1]) == 1)
                {
                    costSubstitution = OffByOneCost;
                }
                else
                {
                    // Here you can choose how to handle numbers that are not equal and not off by one.
                    // For our purposes we set the cost so high that such a substitution would be rejected.
                    costSubstitution = MismatchCost;
                }

                dp[i, j] = Math.Min(
                    dp[i - 1, j - 1] + costSubstitution,                        // substitution (or match)
                    Math.Min(
                        dp[i - 1, j] + InsertionDeletionCost,                   // deletion in text (or insertion in pattern)
                        dp[i, j - 1] + InsertionDeletionCost                    // insertion in text (extra number)
                    )
                );
            }

            // When we have processed text[0..j-1], check if matching the entire pattern (i.e. dp[m,j])
            // gives a distance that is acceptable.
            if (dp[m, j] <= maxDistance && dp[m, j] < bestDistance)
            {
                bestDistance = dp[m, j];
                bestEndIndex = j; // pattern match ends at text index j-1.
            }
        }

        if (bestEndIndex == -1)
        {
            // No acceptable match was found.
            return (-1, 0, int.MaxValue);
        }

        // Backtrack to find the starting index of the matching segment.
        // We start at (i = m, j = bestEndIndex) and work backwards.
        int iIndex = m;
        int jIndex = bestEndIndex;
        while (iIndex > 0 && jIndex > 0)
        {
            // Check if the current cell came from a diagonal (substitution or match)
            int currentCost = dp[iIndex, jIndex];
            int costSubst;
            if (pattern[iIndex - 1] == text[jIndex - 1])
            {
                costSubst = ExactMatchCost;
            }
            else if (Math.Abs(pattern[iIndex - 1] - text[jIndex - 1]) == 1)
            {
                costSubst = OffByOneCost;
            }
            else
            {
                costSubst = MismatchCost;
            }

            if (currentCost == dp[iIndex - 1, jIndex - 1] + costSubst)
            {
                iIndex--;
                jIndex--;
            }
            else if (currentCost == dp[iIndex, jIndex - 1] + InsertionDeletionCost)
            {
                // Came from an insertion (extra item in text)
                jIndex--;
            }
            else if (currentCost == dp[iIndex - 1, jIndex] + InsertionDeletionCost)
            {
                // Came from a deletion (a missing item in text)
                iIndex--;
            }
            else
            {
                break;
            }
        }
        int startIndex = jIndex;
        int length = bestEndIndex - startIndex;
        return (startIndex, length, bestDistance);
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