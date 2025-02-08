using UnityEngine;

public class MicrophoneTest : MonoBehaviour
{
    AudioClip clip;

    string device;

    int sampleWindow = 1024;

    GameObject circle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        device = Microphone.devices[0];
        clip = Microphone.Start(device, true, 20, AudioSettings.outputSampleRate);

        circle = GameObject.Find("Circle");
    }

    // Update is called once per frame
    void Update()
    {
        var loudness = GetLoudness();

        circle.gameObject.transform.localScale = Vector2.Lerp(
            circle.gameObject.transform.localScale,
            new Vector2(loudness, loudness) * 10,
            0.25f
        );
    }

    float GetLoudness()
    {
        var position = Microphone.GetPosition(device);

        var startPosition = position - sampleWindow;

        if (startPosition < 0)
            return 0;

        float[] waveData = new float[sampleWindow];
        clip.GetData(waveData, startPosition);

        float totalLoudness = 0;

        for (int i = 0; i < sampleWindow; i++)
        {
            totalLoudness += Mathf.Abs(waveData[i]);
        }

        return totalLoudness / sampleWindow;
    }
}
