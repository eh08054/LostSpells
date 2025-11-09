using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class AnalyzingPitch : MonoBehaviour
{
    public int sampleRate = 44100;
    public int frameSize = 2048;
    public int hopSize = 1024;
    public int peakCount = 4; // 찾고 싶은 RMS 피크 개수 

    private string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
    [SerializeField] private TMP_Text myText;
    public void AnalyzeRecordedClip(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("No clip recorded.");
            return;
        }

        int sampleCount = clip.samples;
        float[] data = new float[sampleCount];
        clip.GetData(data, 0);

        // 1️. RMS를 구해서 리스트로 저장
        List<float> rmsList = new List<float>();
        List<int> indexList = new List<int>();

        for (int i = 0; i < sampleCount - frameSize; i += hopSize)
        {
            float rms = ComputeRMS(data, i, frameSize);
            Debug.Log($"RMS at sample {i}: {rms:F5}");
            rmsList.Add(rms);
            indexList.Add(i);
        }

        // 2️. RMS 피크 찾기
        List<int> peakIndices = FindTopRMSPeaks(rmsList, indexList, peakCount);

        for(int i = 0; i < peakIndices.Count; i++)
        {
            Debug.Log($"🔍 Found RMS Peak {i + 1} at sample {peakIndices[i]}");
        }   

        // 3️. 각 피크 구간의 주파수 분석
        foreach (int peakIndex in peakIndices)
        {
            float[] frame = new float[frameSize];
            Array.Copy(data, peakIndex, frame, 0, frameSize);
            float freq = PitchFromAutocorrelation(frame, sampleRate);
            int noteNumber = ToNoteNumberLog(freq);
            string note = noteNames[noteNumber % 12];
            int octave = noteNumber / 12;

            string text = $"{note}{octave}";
            myText.text += text + " ";

            if (freq > 0)
                Debug.Log($"🎵 Peak at sample {peakIndex} → {freq:F2} Hz");
            else
                Debug.Log($"⚠️ No pitch detected at peak {peakIndex}");
        }
    }

    // RMS 계산
    private float ComputeRMS(float[] buffer, int start, int length)
    {
        double sum = 0.0;
        int end = Mathf.Min(start + length, buffer.Length);
        for (int i = start; i < end; i++)
            sum += buffer[i] * buffer[i];
        return (float)Math.Sqrt(sum / (end - start));
    }

    // RMS 피크 N개 찾기
    private List<int> FindTopRMSPeaks(List<float> rmsList, List<int> indexList, int count)
    {
        List<int> peakIndices = new List<int>();
        List<(float rms, int index)> pairs = new List<(float, int)>();

        for (int i = 0; i < rmsList.Count; i++)
            pairs.Add((rmsList[i], indexList[i]));

        // RMS 값 기준으로 내림차순 정렬
        pairs.Sort((a, b) => b.rms.CompareTo(a.rms));

        int minDistance = hopSize * 2; 
        foreach (var p in pairs)
        {
            bool tooClose = false;
            foreach (int existing in peakIndices)
            {
                if (Mathf.Abs(existing - p.index) < minDistance)
                {
                    tooClose = true;
                    break;
                }
            }
            if (!tooClose)
                peakIndices.Add(p.index);

            if (peakIndices.Count >= count)
                break;
        }

        peakIndices.Sort();
        return peakIndices;
    }

    // 기본 주파수 검출 (Auto-correlation 방식)
    private float PitchFromAutocorrelation(float[] buffer, int sampleRate)
    {
        int size = buffer.Length;
        float[] autocorr = new float[size];

        Debug.Log("size:" + size);

        float mean = buffer.Average();
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] -= mean;

        for (int lag = 0; lag < size; lag++)
        {
            float sum = 0;
            for (int i = 0; i < size - lag; i++)
                sum += buffer[i] * buffer[i + lag];
            autocorr[lag] = sum;
        }

        int peakIndex = -1;
        float maxCorr = 0;
        for (int lag = 200; lag < size / 2; lag++) 
        {
            Debug.Log("lag: " + lag + ", autocorr[lag]: " + autocorr[lag]);
            if (autocorr[lag] > maxCorr)
            {
                maxCorr = autocorr[lag];
                peakIndex = lag;
            }
        }

        Debug.Log($"Autocorrelation peak index: {peakIndex}, value: {maxCorr}");

        if (peakIndex > 0)
            return sampleRate / (float)peakIndex;
        return -1;
    }

    private int ToNoteNumberLog(float freq)
    {
        return Mathf.RoundToInt(57 + 12 * Mathf.Log(freq / 440.0f, 2));
    }
}