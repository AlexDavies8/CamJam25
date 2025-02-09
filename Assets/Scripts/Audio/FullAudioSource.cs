using UnityEngine;

public class FullAudioSource : MonoBehaviour
{
    public AudioSource source;
    public AudioReverbFilter reverb;
    public AudioLowPassFilter lowpass;
    public AudioHighPassFilter highpass;
    public AudioDistortionFilter distortion;
    public AudioChorusFilter chorus;
    public AudioEchoFilter echo;

    public AudioClip clip {
        get => source?.clip;
        set { if (source is not null) source.clip = value; }
    }
    public bool? mute {
        get => source?.mute;
        set { if (source is not null) source.mute = value ?? source.mute; }
    }
    public float? volume {
        get => source?.volume;
        set { if (source is not null) source.volume = value ?? source.volume; }
    }
    public bool? enableReverb {
        get => reverb?.enabled;
        set { if (reverb is not null) reverb.enabled = value ?? reverb.enabled; }
    }
    public bool? enableLowpass {
        get => lowpass?.enabled;
        set { if (lowpass is not null) lowpass.enabled = value ?? lowpass.enabled; }
    }
    public float? lowCutoff {
        get => lowpass?.cutoffFrequency;
        set { if (lowpass is not null) lowpass.cutoffFrequency = value ?? lowpass.cutoffFrequency; }
    }
    public float? lowQ {
        get => lowpass?.lowpassResonanceQ;
        set { if (lowpass is not null) lowpass.lowpassResonanceQ = value ?? lowpass.lowpassResonanceQ; }
    }
    public bool? enableHighpass {
        get => highpass?.enabled;
        set { if (highpass is not null) highpass.enabled = value ?? highpass.enabled; }
    }
    public float? highCutoff {
        get => highpass?.cutoffFrequency;
        set { if (highpass is not null) highpass.cutoffFrequency = value ?? highpass.cutoffFrequency; }
    }
    public float? highQ {
        get => highpass?.highpassResonanceQ;
        set { if (highpass is not null) highpass.highpassResonanceQ = value ?? highpass.highpassResonanceQ; }
    }
    public bool? enableDistort {
        get => distortion?.enabled;
        set { if (distortion is not null) distortion.enabled = value ?? distortion.enabled; }
    }
    public float distortLevel {
        get => distortion.distortionLevel;
        set => distortion.distortionLevel = value;
    }
    public bool? enableChorus {
        get => chorus?.enabled;
        set { if (chorus is not null) chorus.enabled = value ?? chorus.enabled; }
    }
    public bool? enableEcho {
        get => echo?.enabled;
        set { if (echo is not null) echo.enabled = value ?? echo.enabled; }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        source ??= gameObject.GetComponent<AudioSource>();
        reverb ??= gameObject.GetComponent<AudioReverbFilter>();
        highpass ??= gameObject.GetComponent<AudioHighPassFilter>();
        lowpass ??= gameObject.GetComponent<AudioLowPassFilter>();
        distortion ??= gameObject.GetComponent<AudioDistortionFilter>();
        chorus ??= gameObject.GetComponent<AudioChorusFilter>();
        echo ??= gameObject.GetComponent<AudioEchoFilter>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
