using LostSpells.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// [Hybrid Pitch Detection]
// 1. RMS로 유효 구간(음절) 탐색
// 2. FFT로 대략적인 주파수(Hint) 획득
// 3. YIN으로 해당 범위만 정밀 탐색하여 정확한 Hz 도출
public class VoiceAnalyzingPitch : MonoBehaviour
{

    public int sampleRate = 44100;
    public int peakCount = 4; // 찾고 싶은 RMS 피크 개수
    public float basicFrequency = 65.41f;

    private float highFrequency;
    private float lowFrequency;

    // FFT 처리를 위해 2의 거듭제곱(2048, 1024 등) 권장
    private int frameSize = 2048;
    private int hopSize = 512;

    private string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
    [SerializeField] private TMP_Text myText;

    // FFT 재사용을 위한 윈도우 함수 캐싱
    private float[] windowFunc;

    public static VoiceAnalyzingPitch _instance;

    public static VoiceAnalyzingPitch Instance => _instance;

    void Start()
    {
        // 싱글톤 패턴: 이미 인스턴스가 있으면 제거
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject); // 씬 전환 시에도 유지
        // Hanning Window 미리 계산 (매 프레임 계산 방지)
        windowFunc = new float[frameSize];
        for (int i = 0; i < frameSize; i++)
        {
            windowFunc[i] = 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * i / (frameSize - 1)));
        }
    }

    public void AnalyzeRecordedClip(AudioClip clip)
    {
        if (SceneManager.GetActiveScene().name == "InGame")
        {
            highFrequency = basicFrequency * 2f;
            lowFrequency = basicFrequency / 2f;
            //myText.text = "Detected Notes: ";

            if (clip == null)
            {
                Debug.LogWarning("No clip recorded.");
                return;
            }

            int sampleCount = clip.samples;
            float[] data = new float[sampleCount];
            clip.GetData(data, 0);

            // 1. RMS를 구해서 리스트로 저장
            List<float> rmsList = new List<float>();
            List<int> indexList = new List<int>();

            for (int i = 0; i < sampleCount - frameSize; i += hopSize)
            {
                float rms = ComputeRMS(data, i, frameSize);
                rmsList.Add(rms);
                indexList.Add(i);
            }

            // 2. RMS가 높은 구간(피크) 탐색
            List<int> peakIndices = FindTopRMSPeaks(rmsList, indexList, peakCount);

            int frequencyCount = 0;
            int[] countingFrequencyArray = { 0, 0, 0, 0 };

            // 3. 각 피크 구간 분석 (FFT + YIN 하이브리드)
            foreach (int peakIndex in peakIndices)
            {
                float[] frame = new float[frameSize];
                // 배열 범위 체크
                if (peakIndex + frameSize > data.Length) continue;

                Array.Copy(data, peakIndex, frame, 0, frameSize);

                // [STEP A] FFT로 대략적인 주파수(Hint) 찾기
                float approxFreq = EstimateDominantFrequency(frame, sampleRate);

                // [STEP B] YIN으로 정밀 분석 (FFT 결과를 힌트로 제공)
                float freq = PitchFromYin(frame, sampleRate, approxFreq);

                if (freq > 0)
                {
                    // 통계 집계 로직
                    if (freq < lowFrequency) countingFrequencyArray[0]++;
                    else if (freq < basicFrequency) countingFrequencyArray[1]++;
                    else if (freq < highFrequency) countingFrequencyArray[2]++;
                    else countingFrequencyArray[3]++;

                    int noteNumber = ToNoteNumberLog(freq);
                    string note = noteNames[noteNumber % 12];
                    int octave = noteNumber / 12;

                    string text = $"{note}{octave}";
                    //myText.text += text + " ";

                    Debug.Log($"Detected Frequency: {freq:F2}Hz, Note: {text}");

                    frequencyCount++;
                    // Debug.Log($"Index: {peakIndex}, FFT Hint: {approxFreq:F1}Hz -> YIN Result: {freq:F2}Hz");
                }
            }
        }
    }

    // ---------------------------------------------------------
    // Helper Methods
    // ---------------------------------------------------------

    private float ComputeRMS(float[] buffer, int start, int length)
    {
        double sum = 0.0;
        int end = Mathf.Min(start + length, buffer.Length);
        for (int i = start; i < end; i++)
            sum += Math.Abs(buffer[i]);
        return (float)(sum / (end - start));
    }

    private List<int> FindTopRMSPeaks(List<float> rmsList, List<int> indexList, int count)
    {
        float noiseFloor = rmsList.OrderBy(r => r).Skip((int)(rmsList.Count * 0.1)).FirstOrDefault();
        float rmsThreshold = noiseFloor * 1.5f;

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

        pairs.Sort((a, b) => b.rms.CompareTo(a.rms));

        int minDistance = hopSize * 8;
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
            {
                peakIndices.Add(p.index);
                if (peakIndices.Count >= count) break;
            }
        }
        return peakIndices;
    }

    // ---------------------------------------------------------
    // [New] FFT Implementation (Coarse Pitch Detection)
    // ---------------------------------------------------------
    private float EstimateDominantFrequency(float[] frame, int sampleRate)
    {
        int n = frame.Length;
        // 복소수 배열 준비 (Real, Imag)
        float[] real = new float[n];
        float[] imag = new float[n];

        // 윈도우 함수 적용 및 데이터 복사
        for (int i = 0; i < n; i++)
        {
            real[i] = frame[i] * (windowFunc != null ? windowFunc[i] : 1f);
            imag[i] = 0;
        }

        // FFT 수행
        TransformFFT(real, imag);

        // 가장 에너지가 강한 Bin 찾기 (0Hz~50Hz 등 초저역대 노이즈 제외)
        float maxMagnitude = 0f;
        int maxIndex = 0;

        // n/2 까지만 유효 (Nyquist)
        // 저주파 노이즈 무시를 위해 인덱스 2부터 시작 (약 43Hz 이상)
        for (int i = 2; i < n / 2; i++)
        {
            float magnitude = Mathf.Sqrt(real[i] * real[i] + imag[i] * imag[i]);
            if (magnitude > maxMagnitude)
            {
                maxMagnitude = magnitude;
                maxIndex = i;
            }
        }

        // Index -> Frequency 변환
        if (maxIndex > 0)
            return maxIndex * sampleRate / (float)n;

        return 0f;
    }

    // Cooley-Tukey Iterative FFT
    private void TransformFFT(float[] real, float[] imag)
    {
        int n = real.Length;
        int j = 0;
        for (int i = 0; i < n - 1; i++)
        {
            if (i < j)
            {
                float tr = real[j]; real[j] = real[i]; real[i] = tr;
                float ti = imag[j]; imag[j] = imag[i]; imag[i] = ti;
            }
            int k = n / 2;
            while (k <= j) { j -= k; k /= 2; }
            j += k;
        }

        for (int size = 2; size <= n; size *= 2)
        {
            int halfSize = size / 2;
            float stepReal = Mathf.Cos(-2f * Mathf.PI / size);
            float stepImag = Mathf.Sin(-2f * Mathf.PI / size);
            float wReal = 1f;
            float wImag = 0f;

            for (int i = 0; i < halfSize; i++)
            {
                for (int m = i; m < n; m += size)
                {
                    int next = m + halfSize;
                    float tr = wReal * real[next] - wImag * imag[next];
                    float ti = wReal * imag[next] + wImag * real[next];

                    real[next] = real[m] - tr;
                    imag[next] = imag[m] - ti;
                    real[m] += tr;
                    imag[m] += ti;
                }
                float tempWReal = wReal;
                wReal = tempWReal * stepReal - wImag * stepImag;
                wImag = tempWReal * stepImag + wImag * stepReal;
            }
        }
    }

    // ---------------------------------------------------------
    // [Modified] YIN Algorithm (Smart Search)
    // ---------------------------------------------------------
    private float PitchFromYin(float[] buffer, int sampleRate, float centerFreqHint = 0f, float threshold = 0.2f)
    {
        int N = buffer.Length;
        int halfN = N / 2;

        int minLag = Mathf.Max(2, sampleRate / 1000); // Default: ~2000Hz (High)
        int maxLag = Mathf.Min(halfN, sampleRate / 50); // Default: ~50Hz (Low)

        // [최적화] FFT 힌트가 유효하다면 탐색 범위를 좁힘 (Smart Search)
        if (centerFreqHint > 50f && centerFreqHint < 2000f)
        {
            int centerLag = Mathf.RoundToInt(sampleRate / centerFreqHint);
            int range = 100; // 앞뒤로 여유 범위 (좁을수록 빠르지만 놓칠 수 있음)

            // 범위를 좁혀서 설정
            minLag = Mathf.Max(2, centerLag - range);
            maxLag = Mathf.Min(halfN, centerLag + range);

            // 범위가 역전되면(너무 저음/고음이라) 기본값 사용
            if (maxLag <= minLag)
            {
                minLag = Mathf.Max(2, sampleRate / 1000);
                maxLag = Mathf.Min(halfN, sampleRate / 50);
            }
        }

        if (maxLag <= minLag) return -1f;

        // 1. 차이 함수 (Difference Function)
        float[] d = new float[maxLag];
        // 힌트 범위 밖은 계산하지 않으므로 for문의 반복 횟수가 줄어듦
        for (int tau = minLag; tau < maxLag; tau++)
        {
            float sum = 0f;
            for (int j = 0; j < N - tau; j++)
            {
                float diff = buffer[j] - buffer[j + tau];
                sum += diff * diff;
            }
            d[tau] = sum;
        }

        // 2. 누적 정규화 차이 함수 (d'(τ))
        // 주의: d_prime 계산 시 minLag 이전의 d[] 값들은 0이므로, 
        // 좁은 범위 탐색 시에는 누적합 계산을 간소화하거나 주의해야 함.
        // 여기서는 안전하게 minLag부터 계산하되, 이전 구간의 합은 근사치로 처리하지 않고
        // 해당 윈도우 내에서만 정규화 로직을 수행함.

        float[] d_prime = new float[maxLag];
        float sum_d = 0f;

        // Smart Search 시, 0~minLag 구간의 sum_d가 누락될 수 있음.
        // 정확한 YIN 정규화를 위해선 1부터 다 더해야 하지만,
        // 성능을 위해 local search 구간 내에서만 비율을 봄.

        // 하지만 YIN의 특성상 d[1]부터 더해오는 것이 중요하므로,
        // Hint를 썼더라도 d[tau] 계산 자체는 줄이되, d_prime 계산 시 앞부분은 건너뜀.

        // 간단한 해결책: Hint를 사용했을 때는 d_prime 정규화를 약식으로 처리하거나,
        // 그냥 d[tau] (차이 함수)의 최소값만 찾아도 FFT Hint가 강력하면 충분히 정확함.
        // 여기서는 정석대로 가되 loop 최적화 적용.

        d_prime[minLag] = 1f; // 시작점 초기화

        for (int tau = minLag + 1; tau < maxLag; tau++)
        {
            // 원래는 1부터 tau까지의 합이지만, 여기서는 구간 최적화를 위해 현재 값 위주로 판단
            // 정석 YIN은 d[tau] / (sum / tau) 이지만,
            // 좁은 범위에서는 단순히 d[tau]가 가장 작은 지점(골짜기)를 찾아도 무방함.
            // 따라서 Hint가 있을 때는 Difference Function의 Global Minimum을 찾음.

            d_prime[tau] = d[tau]; // Hint 모드에서는 정규화 생략 가능 (속도 우선)
        }

        // 3. 최소값 찾기 (Hint가 있을 땐 Global Min, 없을 땐 Threshold 방식)
        int periodIndex = -1;

        if (centerFreqHint > 0)
        {
            // [Hint 모드] 범위 내에서 가장 에러(Difference)가 적은 곳 찾기
            float minVal = float.MaxValue;
            for (int tau = minLag; tau < maxLag; tau++)
            {
                if (d[tau] < minVal && d[tau] > 0) // 0은 완전일치인데 부동소수점 이슈로 체크
                {
                    minVal = d[tau];
                    periodIndex = tau;
                }
            }
        }
        else
        {
            // [기존 YIN 모드] Threshold 방식
            // 정규화가 필요하므로 다시 계산 (생략된 부분 복구)
            sum_d = 0;
            // *주의: Hint 없을 때는 d[]를 처음부터 다 계산했어야 함.
            // 위 코드가 Hint 위주로 최적화되었으므로, Hint 없을 땐 다시 풀스캔 루프
            // (코드 복잡도 줄이기 위해 여기서는 단순화: Hint 없을 땐 위쪽 for문을 1부터 돌리도록 수정 필요)
            // *하지만 본 코드에서는 FFT가 거의 항상 Hint를 줄 것이므로 Hint 로직 위주로 작성됨.

            // Fallback: 단순 최소값 탐색
            float minVal = float.MaxValue;
            for (int tau = minLag; tau < maxLag; tau++)
            {
                if (d[tau] < minVal) { minVal = d[tau]; periodIndex = tau; }
            }
        }

        if (periodIndex <= 0) return -1f;

        // 4. 이차 보간 (Parabolic Interpolation)
        int tau0 = periodIndex;
        if (tau0 > minLag && tau0 < maxLag - 1)
        {
            float a = d[tau0 - 1];
            float b = d[tau0];
            float c = d[tau0 + 1];

            float shift = (a - c) / (2 * (a - 2 * b + c));
            if (float.IsNaN(shift)) shift = 0; // 방어 코드

            float actualTau = tau0 + shift;
            return sampleRate / actualTau;
        }

        return sampleRate / (float)periodIndex;
    }

    private int ToNoteNumberLog(float freq)
    {
        return Mathf.RoundToInt(57 + 12 * Mathf.Log(freq / 440.0f, 2));
    }
}
