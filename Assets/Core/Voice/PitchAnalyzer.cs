using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LostSpells.Systems
{
    /// <summary>
    /// 피치 분석 결과 데이터
    /// </summary>
    public class PitchAnalysisResult
    {
        public float[] DetectedFrequencies { get; set; }
        public string[] DetectedNotes { get; set; }
        public PitchCategory DominantCategory { get; set; }
        public int LowCount { get; set; }
        public int MediumCount { get; set; }
        public int HighCount { get; set; }
    }

    /// <summary>
    /// 피치 카테고리 (저음/중음/고음)
    /// </summary>
    public enum PitchCategory
    {
        Low,    // freq < minFrequency
        Medium, // minFrequency <= freq < maxFrequency
        High    // freq >= maxFrequency
    }

    /// <summary>
    /// YIN 알고리즘 기반 피치 분석기
    /// person-gyeongil 브랜치의 AnalyzingPitch.cs에서 포팅
    /// </summary>
    public class PitchAnalyzer : MonoBehaviour
    {
        [Header("Analysis Settings")]
        [Tooltip("샘플 레이트")]
        public int sampleRate = 44100;

        [Tooltip("분석할 RMS 피크 개수")]
        public int peakCount = 4;

        [Tooltip("피치 분류 최소 경계 주파수 (Hz) - 기본값 C3 = 130.81Hz")]
        public float minFrequency = 130.81f;

        [Tooltip("피치 분류 최대 경계 주파수 (Hz) - 기본값 C4 = 261.63Hz")]
        public float maxFrequency = 261.63f;

        // 분석 파라미터
        private int frameSize = 2048;
        private int hopSize = 512;
        private string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        /// <summary>
        /// 오디오 클립의 피치 분석
        /// </summary>
        public PitchAnalysisResult AnalyzeClip(AudioClip clip)
        {
            if (clip == null)
            {
                return null;
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

            // 2. 상위 RMS 피크 인덱스 찾기
            List<int> peakIndices = FindTopRMSPeaks(rmsList, indexList, peakCount);

            // 3. 각 피크 구간의 주파수 분석
            List<float> frequencies = new List<float>();
            List<string> notes = new List<string>();
            int[] categoryCount = { 0, 0, 0 }; // Low, Medium, High

            foreach (int peakIndex in peakIndices)
            {
                if (peakIndex + frameSize > data.Length) continue;

                float[] frame = new float[frameSize];
                Array.Copy(data, peakIndex, frame, 0, frameSize);
                float freq = PitchFromYin(frame, clip.frequency);

                if (freq > 0)
                {
                    frequencies.Add(freq);

                    // 카테고리 분류 (3단계: Low/Medium/High)
                    if (freq < minFrequency)
                    {
                        categoryCount[0]++; // Low
                    }
                    else if (freq < maxFrequency)
                    {
                        categoryCount[1]++; // Medium
                    }
                    else
                    {
                        categoryCount[2]++; // High
                    }

                    // 음표 변환
                    int noteNumber = ToNoteNumberLog(freq);
                    string note = noteNames[noteNumber % 12];
                    int octave = noteNumber / 12;
                    notes.Add($"{note}{octave}");
                }
            }

            // 결과 생성
            PitchAnalysisResult result = new PitchAnalysisResult
            {
                DetectedFrequencies = frequencies.ToArray(),
                DetectedNotes = notes.ToArray(),
                LowCount = categoryCount[0],
                MediumCount = categoryCount[1],
                HighCount = categoryCount[2]
            };

            // 지배적인 카테고리 결정
            int maxIndex = 0;
            int maxCount = categoryCount[0];
            for (int i = 1; i < 3; i++)
            {
                if (categoryCount[i] > maxCount)
                {
                    maxCount = categoryCount[i];
                    maxIndex = i;
                }
            }
            result.DominantCategory = (PitchCategory)maxIndex;

            return result;
        }

        /// <summary>
        /// RMS (Root Mean Square) 계산
        /// </summary>
        private float ComputeRMS(float[] buffer, int start, int length)
        {
            double sum = 0.0;
            int end = Mathf.Min(start + length, buffer.Length);
            for (int i = start; i < end; i++)
            {
                sum += Math.Abs(buffer[i]);
            }
            return (float)(sum / (end - start));
        }

        /// <summary>
        /// 상위 RMS 피크 찾기
        /// 조건: 국소 최대값, 노이즈 플로어의 1.5배 이상, 다른 피크와 최소 거리 유지
        /// </summary>
        private List<int> FindTopRMSPeaks(List<float> rmsList, List<int> indexList, int count)
        {
            // 노이즈 플로어 계산 (하위 10% 제외 후 최솟값)
            float noiseFloor = rmsList.OrderBy(r => r).Skip((int)(rmsList.Count * 0.1)).FirstOrDefault();
            float rmsThreshold = noiseFloor * 1.5f;

            List<int> peakIndices = new List<int>();
            List<(float rms, int index)> pairs = new List<(float, int)>();

            // 국소 최대값 && 임계값 이상인 피크 찾기
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

            // RMS 값 기준 내림차순 정렬
            pairs.Sort((a, b) => b.rms.CompareTo(a.rms));

            // 최소 거리 조건 적용
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
                }

                if (peakIndices.Count >= count) break;
            }

            return peakIndices;
        }

        /// <summary>
        /// YIN 알고리즘을 사용한 기본 주파수 검출
        /// </summary>
        private float PitchFromYin(float[] buffer, int sampleRate, float threshold = 0.2f)
        {
            int N = buffer.Length;
            int halfN = N / 2;

            // Lag 범위 설정 (50Hz ~ 1000Hz)
            int minLag = Mathf.Max(2, sampleRate / 1000);
            int maxLag = Mathf.Min(halfN, sampleRate / 50);

            if (maxLag <= minLag) return -1f;

            // 1. 차이 함수 계산
            float[] d = new float[maxLag];
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

            // 2. 누적 정규화 차이 함수 계산
            float[] d_prime = new float[maxLag];
            float sum_d = 0f;

            d_prime[1] = d[1];
            sum_d = d[1];

            for (int tau = 2; tau < maxLag; tau++)
            {
                sum_d += d[tau];
                d_prime[tau] = d[tau] / (sum_d / tau);
            }

            // 3. 임계값 이하의 첫 번째 피크 찾기
            int periodIndex = -1;
            for (int tau = minLag; tau < maxLag; tau++)
            {
                if (d_prime[tau] < threshold)
                {
                    periodIndex = tau;

                    // 국소 최소값 확인
                    while (periodIndex + 1 < maxLag && d_prime[periodIndex + 1] < d_prime[periodIndex])
                    {
                        periodIndex++;
                    }
                    break;
                }
            }

            if (periodIndex <= 0) return -1f;

            // 4. 이차 보간으로 정확한 주기 계산
            int tau0 = periodIndex;
            if (tau0 > 1 && tau0 < maxLag - 1)
            {
                float a = d[tau0 - 1];
                float b = d[tau0];
                float c = d[tau0 + 1];

                float shift = (a - c) / (2 * (a - 2 * b + c));
                float actualTau = tau0 + shift;

                if (actualTau > 0)
                {
                    return sampleRate / actualTau;
                }
            }

            return sampleRate / (float)periodIndex;
        }

        /// <summary>
        /// 주파수를 MIDI 노트 번호로 변환
        /// </summary>
        private int ToNoteNumberLog(float freq)
        {
            return Mathf.RoundToInt(57 + 12 * Mathf.Log(freq / 440.0f, 2));
        }

        /// <summary>
        /// 실시간 피치 검출 (테스트 모드용)
        /// </summary>
        /// <param name="samples">오디오 샘플 데이터</param>
        /// <param name="sampleRate">샘플 레이트</param>
        /// <returns>검출된 주파수 (Hz), 검출 실패시 -1</returns>
        public float DetectPitchRealtime(float[] samples, int sampleRate)
        {
            if (samples == null || samples.Length < frameSize)
                return -1f;

            // RMS 체크 (무음 필터링)
            float rms = ComputeRMS(samples, 0, samples.Length);
            if (rms < 0.01f)
                return -1f;

            // 마지막 frameSize 샘플로 피치 검출
            int startIndex = Mathf.Max(0, samples.Length - frameSize);
            float[] frame = new float[frameSize];
            Array.Copy(samples, startIndex, frame, 0, frameSize);

            return PitchFromYin(frame, sampleRate);
        }

        /// <summary>
        /// 주파수를 카테고리로 변환
        /// </summary>
        public PitchCategory GetCategory(float frequency)
        {
            if (frequency <= 0) return PitchCategory.Medium;

            if (frequency < minFrequency)
                return PitchCategory.Low;
            else if (frequency < maxFrequency)
                return PitchCategory.Medium;
            else
                return PitchCategory.High;
        }

        /// <summary>
        /// 주파수를 0~1 범위의 게이지 값으로 변환
        /// 게이지 범위: minFrequency/2 ~ maxFrequency*2 (로그 스케일)
        /// </summary>
        public float GetGaugeValue(float frequency)
        {
            if (frequency <= 0) return 0.5f;

            // 게이지 표시 범위: minFrequency/2 ~ maxFrequency*2
            float gaugeMin = minFrequency / 2f;
            float gaugeMax = maxFrequency * 2f;

            // 로그 스케일로 정규화
            float logMin = Mathf.Log(gaugeMin);
            float logMax = Mathf.Log(gaugeMax);
            float logFreq = Mathf.Log(Mathf.Clamp(frequency, gaugeMin, gaugeMax));

            return (logFreq - logMin) / (logMax - logMin);
        }

        /// <summary>
        /// 최소 경계 주파수의 게이지 위치 (0~1)
        /// </summary>
        public float GetMinMarkerPosition()
        {
            float gaugeMin = minFrequency / 2f;
            float gaugeMax = maxFrequency * 2f;

            float logMin = Mathf.Log(gaugeMin);
            float logMax = Mathf.Log(gaugeMax);
            float logFreq = Mathf.Log(minFrequency);

            return (logFreq - logMin) / (logMax - logMin);
        }

        /// <summary>
        /// 최대 경계 주파수의 게이지 위치 (0~1)
        /// </summary>
        public float GetMaxMarkerPosition()
        {
            float gaugeMin = minFrequency / 2f;
            float gaugeMax = maxFrequency * 2f;

            float logMin = Mathf.Log(gaugeMin);
            float logMax = Mathf.Log(gaugeMax);
            float logFreq = Mathf.Log(maxFrequency);

            return (logFreq - logMin) / (logMax - logMin);
        }

        /// <summary>
        /// 게이지 위치 (0~1)를 주파수로 변환
        /// </summary>
        public float GaugePositionToFrequency(float position)
        {
            float gaugeMin = minFrequency / 2f;
            float gaugeMax = maxFrequency * 2f;

            float logMin = Mathf.Log(gaugeMin);
            float logMax = Mathf.Log(gaugeMax);

            float logFreq = logMin + position * (logMax - logMin);
            return Mathf.Exp(logFreq);
        }

        /// <summary>
        /// 최소 경계 주파수 설정
        /// </summary>
        public void SetMinFrequency(float frequency)
        {
            minFrequency = Mathf.Clamp(frequency, 50f, maxFrequency - 10f);
        }

        /// <summary>
        /// 최대 경계 주파수 설정
        /// </summary>
        public void SetMaxFrequency(float frequency)
        {
            maxFrequency = Mathf.Clamp(frequency, minFrequency + 10f, 1000f);
        }

        /// <summary>
        /// 경계 주파수 설정 (min, max 동시)
        /// </summary>
        public void SetBoundaryFrequencies(float min, float max)
        {
            minFrequency = Mathf.Clamp(min, 50f, 990f);
            maxFrequency = Mathf.Clamp(max, minFrequency + 10f, 1000f);
        }

        /// <summary>
        /// 현재 최소 경계 주파수 반환
        /// </summary>
        public float GetMinFrequency()
        {
            return minFrequency;
        }

        /// <summary>
        /// 현재 최대 경계 주파수 반환
        /// </summary>
        public float GetMaxFrequency()
        {
            return maxFrequency;
        }
    }
}
