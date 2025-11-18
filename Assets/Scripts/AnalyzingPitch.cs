using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

//이 스크립트는 오디오 클립에서 RMS 피크를 찾아 해당 구간의 주파수를 분석하고 음표로 변환한다.
//작동 과정은 다음과 같다.
//1. Spacebarwhisper 스크립트에서 오디오 녹음이 완료되면 AnalyzeRecordedClip 메서드를 호출한다.
//2. 오디오를 프레임 단위로 나누고 각 프레임의 RMS 값을 계산하여 리스트에 저장한다.
//3. RMS 값이 가장 높은 피크를 순서대로 찾아 해당 프레임의 시작 인덱스를 기록한다. - 이 과정을 통해 진폭이 가장 큰 구간을 찾음으로써 음절의 시작점을 추정한다.
//4. 각 피크 구간에 대해 PitchFromYin 메서드를 사용하여 기본 주파수를 검출한다.
//5. 검출된 주파수를 음표로 변환하고 화면에 출력한다.

// 음절 수가 정해져 있지 않은 문제(플레이어가 말한 스킬이 몇음절인지 알 수 없음) - 고음/중음/저음의 비율을 통해 스킬의 특성을 결정.
// 예를 들어 상위 X개의 피크 중에 고음이 X / 2개 이상일 경우 스턴 부가효과를 주는등.
// 즉 정확한 음이 아니라 플레이어가 발화한 전반적 특성에 따라 스킬의 효과가 결정되므로 플레이어는 이를 유연하게 활용 가능.
public class AnalyzingPitch : MonoBehaviour
{
    public int sampleRate = 44100;
    public int peakCount = 4; // 찾고 싶은 RMS 피크 개수.
    public float basicFrequency = 65.41f;
    private float highFrequency;
    private float lowFrequency;
    private int frameSize = 2048;
    private int hopSize = 512;
    private string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
    [SerializeField] private TMP_Text myText;
    public void AnalyzeRecordedClip(AudioClip clip)
    {
        highFrequency = basicFrequency * 2f;
        lowFrequency = basicFrequency / 2f;
        myText.text = "Detected Notes: ";
        if (clip == null) 
        {
            Debug.LogWarning("No clip recorded.");
            return;
        }

        int sampleCount = clip.samples;
        //Debug.Log($"Analyzing clip with {sampleCount} samples.");

        //frameSize = sampleCount / 40;
        //hopSize = frameSize / 4;

        float[] data = new float[sampleCount];
        clip.GetData(data, 0);

        // 1️. RMS를 구해서 리스트로 저장
        List<float> rmsList = new List<float>();
        List<int> indexList = new List<int>();

        for (int i = 0; i < sampleCount - frameSize; i += hopSize)
        {
            float rms = ComputeRMS(data, i, frameSize);
            //Debug.Log($"RMS at sample {i}: {rms:F5}");
            rmsList.Add(rms);  //rmsList와 indexList에 각각 위에서 반환된 RMS 값과 해당 프레임의 시작 인덱스를 저장
            indexList.Add(i);
        }

        // 2️. 3가지 조건에 맞는 피크 인덱스를 RMS 값이 가장 높은 인덱스부터 내림차순으로 저장.
        List<int> peakIndices = FindTopRMSPeaks(rmsList, indexList, peakCount);

        for (int i = 0; i < peakIndices.Count; i++)
        {
            //Debug.Log($"Found RMS Peak Top {i + 1} at sample {peakIndices[i]}");
        }

        int frequencyCount = 0;
        // 3️. 각 피크 구간의 주파수 분석
        // peakIndices 리스트에 저장된 각 인덱스(프레임 시작점)부터 frameSize 길이만큼 데이터를 잘라서 PitchFromYin 메서드로 주파수 분석
        // 만약 분석한 기본 주파수가 0보다 크면(유효한 주파수) 이를 출력하고 frequencyCount를 증가시킨다.

        int[] countingFrequencyArray = { 0, 0, 0, 0 };
        foreach (int peakIndex in peakIndices)
        {
            float[] frame = new float[frameSize];
            Array.Copy(data, peakIndex, frame, 0, frameSize);  // data 배열로부터 peakIndex에서 시작해 frameSize 길이만큼 frame 배열에 복사
            float freq = PitchFromYin(frame, sampleRate);
            //Debug.Log("Detected frequency: " + freq);
            if (freq > 0)
            {
                if (freq < lowFrequency)
                {
                    countingFrequencyArray[0]++; //주파수(freq) < 설정 주파수 / 2
                }
                else if (freq < basicFrequency)
                {
                    countingFrequencyArray[1]++; //설정 주파수 / 2 <= 주파수(freq) < 설정 주파수
                }
                else if (freq < highFrequency)
                {
                    countingFrequencyArray[2]++; // 설정 주파수 <= 주파수(freq) < 설정 주파수 * 2
                }
                else
                {
                    countingFrequencyArray[3]++; // 주파수(freq) >= 설정 주파수 * 2
                }
                int noteNumber = ToNoteNumberLog(freq);

                string note = noteNames[noteNumber % 12];
                int octave = noteNumber / 12;

                string text = $"{note}{octave}";
                myText.text += text + " ";
                //Debug.Log($"Peak at sample {peakIndex} → {freq:F2} Hz");
                frequencyCount++;
            }
            //else
                //Debug.Log($"No pitch detected at peak {peakIndex}");
        }
    }

    // RMS 계산
    // RMS(Root Mean Square)는 신호의 에너지를 나타내는 지표로, 오디오 신호의 진폭을 측정하는 데 사용된다.
    // 주어진 버퍼의 특정 구간(start부터 length 길이까지)의 RMS 값을 계산한다.
    private float ComputeRMS(float[] buffer, int start, int length)
    {
        double sum = 0.0;
        int end = Mathf.Min(start + length, buffer.Length);
        for (int i = start; i < end; i++)
            sum += Math.Abs(buffer[i]);
        return (float)(sum / (end - start));
    }

    // RMS가 가장 높은 순서대로 해당 프레임의 시작 인덱스를 peakIndices 리스트에 넣는다.
    // 이 때 조건은 다음 3가지가 존재한다.
    // 1. 피크 인덱스가 포함된 프레임(이하 피크 프레임)은 인접한 프레임보다 RMS 값이 커야 한다.(국소 최대값)
    // 2. 피크 프레임의 RMS 값이 노이즈 플로어(하위 10%를 제외한 RMS 값 중 최솟값)의 1.5배 이상이어야 한다.
    // 3. 이미 선택된 피크 프레임과 최소 hopSize*2 프레임 이상 떨어져 있어야 한다.(너무 가까운 피크는 제외)
    private List<int> FindTopRMSPeaks(List<float> rmsList, List<int> indexList, int count)
    {
        float noiseFloor = rmsList.OrderBy(r => r).Skip((int)(rmsList.Count * 0.1)).FirstOrDefault(); //rmsList에서 하위 10%를 제외한 후의 최솟값
        float rmsThreshold = noiseFloor * 1.5f; // 노이즈 플로어보다 1.5배 높은 값만 유효한 음성으로 간주
        List<int> peakIndices = new List<int>();
        List<(float rms, int index)> pairs = new List<(float, int)>();

        for (int i = 1; i < rmsList.Count - 1; i++)
        {
            if (rmsList[i] > rmsList[i - 1] && rmsList[i] > rmsList[i + 1])
            {
                if (rmsList[i] > rmsThreshold)
                {
                    pairs.Add((rmsList[i], indexList[i]));
                }
            }
        }

        //Debug.Log($"Found {pairs.Count} RMS peaks above threshold {rmsThreshold:F5}");
        // RMS 값 기준으로 내림차순 정렬
        pairs.Sort((a, b) => b.rms.CompareTo(a.rms));

        int minDistance = hopSize * 4;
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
        }

        return peakIndices;
    }

    // YIN 알고리즘을 사용한 기본 주파수 검출(RMS가 가장 높은 N개의 구간에서 호출되며 각 구간의 기본 주파수를 반환함.)
    // 알고리즘의 주요 단계: 차이 함수 계산, 누적 정규화 차이 함수 계산, 절대 최소값 및 임계값 이하의 첫 번째 값 찾기, 이차 보간을 통한 정확한 주기 위치 계산.
    // 1. 차이 함수 (Difference Function, d(τ)) 계산. 차이 함수는 신호의 자기 상관성을 측정하는데 사용됨.
    // 2. 누적 정규화 차이 함수 (Cumulative Normalized Difference Function, d'(τ)) 계산. 이 함수는 차이 함수를 정규화하여 피치 검출의 정확성을 높임.
    // 3. 절대 최소값 (Absolute Minimum) 및 임계값 이하의 첫 번째 값 찾기 (Absolute Threshold). 이 단계에서는 d'(τ)가 임계값 이하로 떨어지는 첫 번째 지점을 찾아 피치의 후보 주기를 결정함.
    // 4. 이차 보간(Parabolic Interpolation)을 사용하여 정확한 주기(Lag) 위치 계산. 이 단계에서는 발견된 주기의 정확도를 높이기 위해 보간을 수행함.
    private float PitchFromYin(float[] buffer, int sampleRate, float threshold = 0.2f)
    {
        int N = buffer.Length;
        int halfN = N / 2;

        //여기서 Lag는 샘플 단위의 지연 시간(주기)임. 예를 들어 1000Hz면 Lag=44.1(44100 / 1000), 50Hz면 Lag=882(44100 / 50)
        int minLag = Mathf.Max(2, sampleRate / 1000);
        int maxLag = Mathf.Min(halfN, sampleRate / 50);

        //Debug.Log($"YIN Analysis: minLag= {minLag}, maxLag={maxLag}");

        if (maxLag <= minLag) return -1f;

        // 1. 차이 함수 (Difference Function, d(τ)) 계산
        // 차이 함수는 tau만큼 지연된 신호와 원래 신호 간의 차이를 제곱하여 합산한 값이다. 이 값이 작을수록 해당 tau에서의 자기 상관성이 높음을 의미한다.
        // 차이 함수의 값이 작을수록 해당 프레임에서의 주기가 될 가능성이 높다.
        float[] d = new float[maxLag];

        // d[τ] = Σ_{j=1}^{N-τ} (x_j - x_{j+τ})^2
        for (int tau = 1; tau < maxLag; tau++)
        {
            float sum = 0f;
            for (int j = 0; j < N - tau; j++)
            {
                float diff = buffer[j] - buffer[j + tau];
                sum += diff * diff;
            }
            d[tau] = sum;
        }

        // 2. 누적 정규화 차이 함수 (Cumulative Normalized Difference Function, d'(τ)) 계산
        // 이것을 한 번 더 계산하는 이유는 차이함수에서 tau가 커질수록 for이 돌아가는 횟수가 줄어 값이 작아지는 경향이 있기 때문이다.
        // d'(τ) = d(τ) / [(1/τ) * Σ_{i=1}^{τ} d(i)]
        float[] d_prime = new float[maxLag];
        float sum_d = 0f;

        // d'[1]은 정의되지 않으므로 d[1]로 초기화 (또는 계산 범위에서 제외)
        d_prime[1] = d[1];
        sum_d = d[1];

        for (int tau = 2; tau < maxLag; tau++)
        {
            sum_d += d[tau];
            d_prime[tau] = d[tau] / (sum_d / tau);
        }

        // 3. 임계값 이하의 첫 번째 값 찾기 (Absolute Threshold)

        // 임계값 이하의 첫 번째 피크(최소값) 찾기
        int periodIndex = -1;
        for (int tau = minLag; tau < maxLag; tau++)
        {
            //Debug.Log($"tau = {tau}, d_prime[tau]: " + d_prime[tau]);
            if (d_prime[tau] < threshold)
            {
                periodIndex = tau;

                // 4. 최소값 주변의 국소 최소값(Local Minimum) 확인
                // 최소값이 발견된 이후, 다시 상승하는 지점을 찾는다. 그 상승하기 전까지의 지점을 최종 피크로 간주한다.
                while (periodIndex + 1 < maxLag && d_prime[periodIndex + 1] < d_prime[periodIndex])
                {
                    periodIndex++;
                }
                break; // 첫 번째 유효한 피크를 찾았으므로 종료
            }
        }

        //Debug.Log($"YIN periodIndex: {periodIndex}");

        if (periodIndex <= 0) return -1f;

        // 5. 이차 보간(Parabolic Interpolation)을 사용하여 정확한 주기(Lag) 위치 계산
        // 더 정확한 피치 값(소수점 단위)을 얻기 위함이다.
        int tau0 = periodIndex;
        if (tau0 > 1 && tau0 < maxLag - 1)
        {
            float a = d[tau0 - 1];
            float b = d[tau0];
            float c = d[tau0 + 1];

            // 정수 피크 주변의 정밀 보정
            float shift = (a - c) / (2 * (a - 2 * b + c));

            // 보정된 주기 (lag)
            float actualTau = tau0 + shift;

            if (actualTau > 0)
            {
                // F0 = SampleRate / T0(기본 주파수 = SampleRate / 주기)
                return sampleRate / actualTau;
            }
        }

        // 보간 실패 시 정수 주기로 계산
        return sampleRate / (float)periodIndex;
    }

    private int ToNoteNumberLog(float freq)
    {
        return Mathf.RoundToInt(57 + 12 * Mathf.Log(freq / 440.0f, 2));
    }
}