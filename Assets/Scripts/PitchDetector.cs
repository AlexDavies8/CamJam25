using System;
using System.Numerics;
using UnityEngine;

public class PitchDetector : MonoBehaviour
{
    public string device;

    private AudioClip clip;
    private int sampleRate = 44100;
    private int bufferSize = 1024 * 8;

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

            var max = 0;
            for (int i = 0; i < buckets.Length; i++)
            {
                if (buckets[i] > buckets[max]) max = i;
            }
            
            string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
            if (buckets[max] > 500) Debug.Log($"Note: {noteNames[max]}, Magnitude: {buckets[max]}");
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