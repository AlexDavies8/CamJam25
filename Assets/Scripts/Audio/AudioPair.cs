using System;
using UnityEngine;

public class AudioPair : MonoBehaviour {

    [SerializeField]
    private FullAudioSource audio1;
    [SerializeField]
    private FullAudioSource audio2;
    private int playing = 0;
    private AudioSource[] audios;
    [SerializeField, Range(0, 1)]
    private float volume;
    public float vol {
        get => volume;
        set {
            volume = value;
            audio1.volume = volume;
            audio2.volume = volume;
        }
    }

    public void Play(AudioClip clip, double time) {
        audios[playing].clip = clip;
        audios[playing].PlayScheduled(time + MusicEngine.delay);
        playing = 1 - playing;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audios = new AudioSource[] { audio1.source, audio2.source };
        audio1.volume = volume;
        audio2.volume = volume;
    }

    // Update is called once per frame
    void Update()
    {
    }
}