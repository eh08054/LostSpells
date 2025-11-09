using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using TMPro;

public class PitchTest : MonoBehaviour
{
    struct PeakData
    {
        public float sampleIndex;
        public float amplitude;
    }
    [SerializeField] private TMP_Text myText;
    private AudioSource audioSource;
    private List<PeakData> peaks;
    private float[] samples;
    private float[] smoothSamples;

    private float maxFreq;

    public float threshold = 0.01f;

    const int sampleCount = 2048;
    private string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = Microphone.Start(null, false, 999, 44100);
        while (!(Microphone.GetPosition(null) > 0)) ;
        audioSource.Play();

        maxFreq = AudioSettings.outputSampleRate * 0.5f;
        samples = new float[sampleCount];
        smoothSamples = new float[sampleCount];
        peaks = new List<PeakData>();
    }
    void Update()
    {
        audioSource.GetSpectrumData(samples, 0, FFTWindow.BlackmanHarris);
        peaks.Clear(); 

        for (int i = 0; i < sampleCount; i++)
        {
            smoothSamples[i] = Mathf.Lerp(smoothSamples[i], samples[i], Time.deltaTime * 10);
            if (samples[i] > threshold)
            {
                peaks.Add(new PeakData
                {
                    sampleIndex = i,
                    amplitude = smoothSamples[i]
                });
            }
        }


        if (peaks.Count > 0)
        {
            peaks.Sort((a, b) => -a.amplitude.CompareTo(b.amplitude));

            float peakFreq = (peaks[0].sampleIndex / sampleCount) * maxFreq;
            {
                int noteNumber = ToNoteNumberLog(peakFreq);
                string note = noteNames[noteNumber % 12];
                int octave = noteNumber / 12;

                string text = $"{note}{octave}";
                myText.text = text;
            }
        }


    }
    private int ToNoteNumberLog(float freq)
    {
        return Mathf.RoundToInt(57 + 12 * Mathf.Log(freq / 440.0f, 2));
    }
}
