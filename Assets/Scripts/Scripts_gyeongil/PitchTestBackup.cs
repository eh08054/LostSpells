//using System.Collections;
//using System.Collections.Generic;
//using System.Runtime.InteropServices;
//using UnityEngine;
//using TMPro;

//public class PitchTestBackup : MonoBehaviour
//{
//    struct PeakData
//    {
//        public float sampleIndex;
//        public float amplitude;
//    }
//    private AudioSource audioSource;
//    public TMP_Text myText;
//    private List<PeakData> peaks;
//    private float[] samples;
//    private float[] smoothSamples;

//    private float maxFreq;
//    private float[] noteFreqs;

//    public float threshold = 0.01f;

//    const int sampleCount = 4096;
//    private string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
//    void Awake()
//    {
//        noteFreqs = new float[108]; // 12 * 9 = 108
//        for (int i = 0; i < 12 * 9; i++)
//        {
//            noteFreqs[i] = 440f * Mathf.Pow(2, (i - 57) / 12.0f);
//            //noteFreqs[i] = 16.35f * Mathf.Pow(2, i / 12.0f);
//        }
//    }
//    // Start is called before the first frame update
//    void Start()
//    {
//        audioSource = GetComponent<AudioSource>();
//        audioSource.clip = Microphone.Start(null, false, 999, 44100);
//        while (!(Microphone.GetPosition(null) > 0)) ;
//        audioSource.Play();

//        maxFreq = AudioSettings.outputSampleRate * 0.5f;
//        samples = new float[sampleCount];
//        smoothSamples = new float[sampleCount];
//        peaks = new List<PeakData>();
//    }

//    // Update is called once per frame
//    void Update()
//    {
//        audioSource.GetSpectrumData(samples, 0, FFTWindow.BlackmanHarris);
//        peaks.Clear(); // ��ũ ����Ʈ �ʱ�ȭ

//        for (int i = 0; i < sampleCount; i++)
//        {
//            smoothSamples[i] = Mathf.Lerp(smoothSamples[i], samples[i], Time.deltaTime * 10);
//            if (samples[i] > threshold)
//            {
//                peaks.Add(new PeakData
//                {
//                    sampleIndex = i,
//                    amplitude = smoothSamples[i]
//                });
//            }
//        }


//        if (peaks.Count > 0)
//        {
//            // ����� ������ ������� ��
//            peaks.Sort((a, b) => -a.amplitude.CompareTo(b.amplitude));

//            float peakFreq = (peaks[0].sampleIndex / sampleCount) * maxFreq;
//            Debug.Log(peaks[0].sampleIndex + " / " + sampleCount + " * " + maxFreq + " = " + peakFreq);
//            if (peaks.Count > 0)
//            {
//                int noteNumber = ToNoteNumber(peakFreq);

//                string note = noteNames[noteNumber % 12];
//                int octave = noteNumber / 12;

//                string text = $"{note}{octave}";
//                myText.text = text;
//            }
//        }


//    }
//    private int ToNoteNumberLog(float freq)
//    {
//        return Mathf.RoundToInt(57 + 12 * Mathf.Log(freq / 440.0f, 2));
//    }

//    private int ToNoteNumber(float freq)
//    {

//        for (int i = 1; i < 107; i++)
//        {
//            float prev = noteFreqs[i - 1],
//                  next = noteFreqs[i + 1];
//            float current = noteFreqs[i];
//            float min = (prev + current) / 2,
//                  max = (next + current) / 2;


//            if (min <= freq && freq <= max)
//            {
//                return i;
//            }
//        }

//        return -1;
//    }
//}

