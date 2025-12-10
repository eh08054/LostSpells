using UnityEngine;
using System.Collections;
using System.IO;

namespace LostSpells.Systems
{
    /// <summary>
    /// 음성 녹음기 - 마이크를 항상 켜둔 상태로 유지하여 녹음 시 끊김 방지
    /// </summary>
    public class VoiceRecorder : MonoBehaviour
    {
        [Header("Recording Settings")]
        [Tooltip("최대 녹음 시간 (초)")]
        public int maxRecordingLength = 10;

        [Tooltip("샘플링 레이트 (Hz) - 음성인식에는 16000Hz가 적합")]
        public int sampleRate = 16000;

        [Tooltip("사용할 마이크 인덱스 (0 = 첫 번째)")]
        public int microphoneIndex = 0;

        [Header("Voice Activity Detection (VAD)")]
        [Tooltip("연속 음성 감지 모드 활성화")]
        public bool enableContinuousMode = false;

        [Tooltip("음성 감지 임계값 (RMS)")]
        public float voiceThreshold = 0.02f;

        [Tooltip("무음 지속 시 녹음 종료 시간 (초)")]
        public float silenceTimeout = 1.0f;

        [Tooltip("발화 전 버퍼 시간 (초) - 놓친 시작 부분 보정")]
        public float preRollSeconds = 1.0f;

        [Tooltip("최소 녹음 길이 (초) - 이보다 짧으면 무시")]
        public float minRecordingLength = 2.0f;

        // VAD 이벤트
        public event System.Action OnVoiceDetected;
        public event System.Action<AudioClip> OnVoiceRecordingComplete;

        // 순환 버퍼용 AudioClip (항상 녹음 중)
        private AudioClip loopingClip;
        private string microphoneDevice;

        // 녹음 상태
        private bool isRecording = false;
        private int recordStartPosition = 0;
        private int recordEndPosition = 0;

        // 추출된 녹음 데이터
        private AudioClip recordedClip;

        // 마이크 초기화 상태
        private bool isMicrophoneReady = false;

        // VAD 상태
        private bool isVoiceDetected = false;
        private float silenceTimer = 0f;
        private float recordingTimer = 0f;

        void Start()
        {
            InitializeMicrophone();
        }

        void Update()
        {
            // 연속 모드가 아니거나 마이크가 준비되지 않으면 무시
            if (!enableContinuousMode || !isMicrophoneReady) return;

            float currentRMS = CalculateCurrentRMS();

            if (!isRecording && !isVoiceDetected)
            {
                // 음성 대기 중 - RMS가 임계값을 넘으면 녹음 시작
                if (currentRMS > voiceThreshold)
                {
                    // Debug.Log($"[VoiceRecorder] 음성 감지됨 (RMS: {currentRMS:F4})");
                    isVoiceDetected = true;
                    silenceTimer = 0f;
                    recordingTimer = 0f;

                    // preRoll 위치 계산 (발화 전 버퍼)
                    int currentPos = Microphone.GetPosition(microphoneDevice);
                    int preRollSamples = (int)(preRollSeconds * sampleRate);
                    int voiceStartPosition = currentPos - preRollSamples;
                    if (voiceStartPosition < 0)
                    {
                        voiceStartPosition += loopingClip.samples;
                    }

                    recordStartPosition = voiceStartPosition;
                    isRecording = true;

                    OnVoiceDetected?.Invoke();
                }
            }
            else if (isRecording && isVoiceDetected)
            {
                // 녹음 중 (Time.unscaledDeltaTime 사용 - 일시정지/게임오버 중에도 동작)
                recordingTimer += Time.unscaledDeltaTime;

                if (currentRMS < voiceThreshold / 2)
                {
                    // 무음 감지
                    silenceTimer += Time.unscaledDeltaTime;

                    if (silenceTimer >= silenceTimeout)
                    {
                        // 녹음 종료
                        float recordingLength = GetCurrentRecordingLength();
                        // Debug.Log($"[VoiceRecorder] 무음 감지, 녹음 종료 (길이: {recordingLength:F2}초)");

                        if (recordingLength >= minRecordingLength)
                        {
                            StopContinuousRecording();
                        }
                        else
                        {
                            // Debug.Log($"[VoiceRecorder] 녹음이 너무 짧음 ({recordingLength:F2}초 < {minRecordingLength}초), 무시");
                            // 리셋
                            isRecording = false;
                            isVoiceDetected = false;
                            silenceTimer = 0f;
                            recordingTimer = 0f;
                        }
                    }
                }
                else
                {
                    // 음성 계속됨, 타이머 리셋
                    silenceTimer = 0f;
                }
            }
        }

        /// <summary>
        /// 현재 마이크 입력의 RMS(Root Mean Square) 계산
        /// </summary>
        private float CalculateCurrentRMS()
        {
            // 마이크 및 클립 유효성 철저히 검사
            if (loopingClip == null || !isMicrophoneReady) return 0f;
            if (string.IsNullOrEmpty(microphoneDevice)) return 0f;
            if (!Microphone.IsRecording(microphoneDevice)) return 0f;

            // 클립이 유효한지 확인
            if (loopingClip.samples <= 0 || loopingClip.channels <= 0) return 0f;

            int currentPosition = Microphone.GetPosition(microphoneDevice);
            if (currentPosition < 0) return 0f;

            int samplesToAnalyze = 1024; // ~64ms at 16kHz

            if (currentPosition < samplesToAnalyze) return 0f;

            // 오프셋이 유효한 범위인지 확인
            int offset = currentPosition - samplesToAnalyze;
            if (offset < 0) return 0f;

            // 오프셋 + 분석할 샘플 수가 클립 범위 내인지 확인
            if (offset + samplesToAnalyze > loopingClip.samples) return 0f;

            try
            {
                float[] samples = new float[samplesToAnalyze];
                loopingClip.GetData(samples, offset);

                float sum = 0f;
                for (int i = 0; i < samples.Length; i++)
                {
                    sum += Mathf.Abs(samples[i]);
                }
                return sum / samples.Length;
            }
            catch (System.Exception)
            {
                // 클립이 무효화된 경우 0 반환
                return 0f;
            }
        }

        /// <summary>
        /// 현재 녹음 중인 길이 (초) 반환
        /// </summary>
        private float GetCurrentRecordingLength()
        {
            int currentPos = Microphone.GetPosition(microphoneDevice);
            int sampleCount;
            if (currentPos >= recordStartPosition)
            {
                sampleCount = currentPos - recordStartPosition;
            }
            else
            {
                sampleCount = (loopingClip.samples - recordStartPosition) + currentPos;
            }
            return (float)sampleCount / sampleRate;
        }

        /// <summary>
        /// 연속 모드 녹음 종료 및 이벤트 발생
        /// </summary>
        private void StopContinuousRecording()
        {
            recordEndPosition = Microphone.GetPosition(microphoneDevice);
            isRecording = false;
            isVoiceDetected = false;
            silenceTimer = 0f;
            recordingTimer = 0f;

            // 오디오 추출
            ExtractRecordedAudio();

            // 이벤트 발생
            OnVoiceRecordingComplete?.Invoke(recordedClip);
        }

        void OnDestroy()
        {
            // 마이크 종료
            if (!string.IsNullOrEmpty(microphoneDevice) && Microphone.IsRecording(microphoneDevice))
            {
                Microphone.End(microphoneDevice);
            }

            if (loopingClip != null)
            {
                Destroy(loopingClip);
            }
        }

        /// <summary>
        /// 마이크 초기화 및 연속 녹음 시작
        /// </summary>
        private void InitializeMicrophone()
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogWarning("[VoiceRecorder] 마이크를 찾을 수 없습니다!");
                return;
            }

            int index = Mathf.Clamp(microphoneIndex, 0, Microphone.devices.Length - 1);
            microphoneDevice = Microphone.devices[index];

            // 마이크를 루프 모드로 시작 (항상 켜둠)
            StartCoroutine(StartContinuousRecording());
        }

        /// <summary>
        /// 연속 녹음 시작 (코루틴으로 프레임 분산)
        /// </summary>
        private IEnumerator StartContinuousRecording()
        {
            // 다음 프레임에서 마이크 시작 (초기화 부하 분산)
            yield return null;

            // 루프 모드로 마이크 시작 - 최대 녹음 시간만큼의 순환 버퍼
            loopingClip = Microphone.Start(microphoneDevice, true, maxRecordingLength, sampleRate);

            // 마이크가 실제로 시작될 때까지 대기
            while (Microphone.GetPosition(microphoneDevice) <= 0)
            {
                yield return null;
            }

            isMicrophoneReady = true;
            // Debug.Log("[VoiceRecorder] 마이크 연속 녹음 시작됨");
        }

        /// <summary>
        /// 녹음 시작 - 현재 마이크 위치만 기록 (Microphone.Start 호출 없음!)
        /// </summary>
        public void StartRecording()
        {
            if (isRecording)
            {
                // Debug.Log("[VoiceRecorder] StartRecording 무시: 이미 녹음 중");
                return;
            }

            if (!isMicrophoneReady)
            {
                Debug.LogWarning("[VoiceRecorder] StartRecording 실패: 마이크가 준비되지 않음");
                return;
            }

            isRecording = true;
            recordStartPosition = Microphone.GetPosition(microphoneDevice);
            // Debug.Log($"[VoiceRecorder] 녹음 시작됨 (position: {recordStartPosition})");
        }

        /// <summary>
        /// 녹음 중지 - 시작~끝 위치의 오디오 추출
        /// </summary>
        public void StopRecording()
        {
            if (!isRecording)
            {
                // Debug.Log("[VoiceRecorder] StopRecording 무시: 녹음 중이 아님");
                return;
            }

            if (!isMicrophoneReady)
            {
                Debug.LogWarning("[VoiceRecorder] StopRecording 경고: 마이크가 준비되지 않음");
            }

            isRecording = false;
            recordEndPosition = Microphone.GetPosition(microphoneDevice);
            // Debug.Log($"[VoiceRecorder] 녹음 중지됨 (start: {recordStartPosition}, end: {recordEndPosition})");

            // 녹음된 구간 추출
            ExtractRecordedAudio();
        }

        /// <summary>
        /// 순환 버퍼에서 녹음된 구간 추출
        /// </summary>
        private void ExtractRecordedAudio()
        {
            if (loopingClip == null)
            {
                Debug.LogError("[VoiceRecorder] loopingClip이 null입니다!");
                return;
            }

            int totalSamples = loopingClip.samples;
            int channels = loopingClip.channels;

            // 위치 값 유효성 검사 및 보정
            if (totalSamples <= 0 || channels <= 0)
            {
                Debug.LogError($"[VoiceRecorder] 잘못된 클립 정보: totalSamples={totalSamples}, channels={channels}");
                return;
            }

            // 위치를 유효한 범위로 보정
            int startPos = recordStartPosition % totalSamples;
            int endPos = recordEndPosition % totalSamples;
            if (startPos < 0) startPos += totalSamples;
            if (endPos < 0) endPos += totalSamples;

            // Debug.Log($"[VoiceRecorder] ExtractRecordedAudio: start={startPos}, end={endPos}, totalSamples={totalSamples}, channels={channels}");

            // 샘플 수 계산 (순환 버퍼 고려)
            int sampleCount;
            if (endPos >= startPos)
            {
                sampleCount = endPos - startPos;
            }
            else
            {
                // 버퍼가 한 바퀴 돌았을 경우
                sampleCount = (totalSamples - startPos) + endPos;
            }

            if (sampleCount <= 0)
            {
                Debug.LogWarning("[VoiceRecorder] 녹음된 샘플이 없습니다.");
                return;
            }

            // 최대 샘플 수 제한 (버퍼 크기를 초과하지 않도록)
            if (sampleCount > totalSamples)
            {
                Debug.LogWarning($"[VoiceRecorder] 샘플 수가 버퍼 크기를 초과: {sampleCount} > {totalSamples}, 제한 적용");
                sampleCount = totalSamples;
            }

            // Debug.Log($"[VoiceRecorder] 추출할 샘플 수: {sampleCount} ({(float)sampleCount / sampleRate:F2}초)");

            try
            {
                // 전체 버퍼 데이터 가져오기
                float[] allSamples = new float[totalSamples * channels];
                loopingClip.GetData(allSamples, 0);

                // 녹음 구간 추출
                float[] recordedSamples = new float[sampleCount * channels];

                if (endPos >= startPos)
                {
                    // 연속 구간
                    int copyLength = sampleCount * channels;
                    int sourceIndex = startPos * channels;

                    // 범위 검사
                    if (sourceIndex + copyLength <= allSamples.Length && copyLength <= recordedSamples.Length)
                    {
                        System.Array.Copy(allSamples, sourceIndex, recordedSamples, 0, copyLength);
                    }
                    else
                    {
                        Debug.LogError($"[VoiceRecorder] 배열 범위 초과: sourceIndex={sourceIndex}, copyLength={copyLength}, allSamples.Length={allSamples.Length}");
                        return;
                    }
                }
                else
                {
                    // 순환된 구간 (끝 + 시작)
                    int firstPart = totalSamples - startPos;
                    int firstCopyLength = firstPart * channels;
                    int secondCopyLength = endPos * channels;

                    // 범위 검사
                    if (startPos * channels + firstCopyLength <= allSamples.Length &&
                        firstCopyLength + secondCopyLength <= recordedSamples.Length &&
                        secondCopyLength <= allSamples.Length)
                    {
                        System.Array.Copy(allSamples, startPos * channels, recordedSamples, 0, firstCopyLength);
                        System.Array.Copy(allSamples, 0, recordedSamples, firstCopyLength, secondCopyLength);
                    }
                    else
                    {
                        Debug.LogError($"[VoiceRecorder] 순환 구간 배열 범위 초과");
                        return;
                    }
                }

                // 새 AudioClip 생성
                if (recordedClip != null)
                {
                    Destroy(recordedClip);
                }

                recordedClip = AudioClip.Create("RecordedAudio", sampleCount, channels, sampleRate, false);
                recordedClip.SetData(recordedSamples, 0);

                // Debug.Log($"[VoiceRecorder] AudioClip 생성 완료: {recordedClip.samples} samples, {recordedClip.channels} channels, {recordedClip.frequency}Hz");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[VoiceRecorder] ExtractRecordedAudio 예외 발생: {e.Message}");
            }
        }

        /// <summary>
        /// 녹음된 오디오를 WAV 파일로 저장
        /// </summary>
        public string SaveRecordingAsWav()
        {
            if (recordedClip == null)
            {
                return null;
            }

            string filePath = Path.Combine(Application.persistentDataPath, "recorded_audio.wav");
            SavWav.Save(filePath, recordedClip);
            return filePath;
        }

        /// <summary>
        /// 녹음된 AudioClip 반환
        /// </summary>
        public AudioClip GetRecordedClip()
        {
            return recordedClip;
        }

        /// <summary>
        /// 녹음 중인지 확인
        /// </summary>
        public bool IsRecording()
        {
            return isRecording;
        }

        /// <summary>
        /// 녹음된 오디오를 바이트 배열로 변환 (무음 제거 적용)
        /// </summary>
        public byte[] GetRecordingAsBytes()
        {
            if (recordedClip == null)
            {
                Debug.LogError("[VoiceRecorder] GetRecordingAsBytes: recordedClip이 null입니다!");
                return null;
            }

            // Debug.Log($"[VoiceRecorder] GetRecordingAsBytes: 원본 클립 {recordedClip.samples} samples");

            // 무음 제거된 AudioClip 생성
            AudioClip trimmedClip = TrimSilence(recordedClip);

            if (trimmedClip == null)
            {
                Debug.LogError("[VoiceRecorder] GetRecordingAsBytes: trimmedClip이 null입니다!");
                return null;
            }

            string tempPath = Path.Combine(Application.persistentDataPath, "temp_audio.wav");
            SavWav.Save(tempPath, trimmedClip);

            byte[] audioData = File.ReadAllBytes(tempPath);
            // Debug.Log($"[VoiceRecorder] GetRecordingAsBytes: WAV 파일 크기 {audioData.Length} bytes");

            File.Delete(tempPath);

            // 임시 클립 정리
            if (trimmedClip != recordedClip)
            {
                Destroy(trimmedClip);
            }

            return audioData;
        }

        /// <summary>
        /// 오디오 클립에서 앞뒤 무음 제거
        /// </summary>
        private AudioClip TrimSilence(AudioClip clip, float silenceThreshold = 0.01f)
        {
            if (clip == null) return null;

            int channels = clip.channels;
            int totalSamples = clip.samples; // 채널당 샘플 수

            // 전체 샘플 데이터 가져오기
            float[] samples = new float[totalSamples * channels];
            clip.GetData(samples, 0);

            // 무음이 아닌 시작 프레임 찾기 (채널 단위로)
            int startFrame = 0;
            for (int frame = 0; frame < totalSamples; frame++)
            {
                bool hasSound = false;
                for (int ch = 0; ch < channels; ch++)
                {
                    if (Mathf.Abs(samples[frame * channels + ch]) > silenceThreshold)
                    {
                        hasSound = true;
                        break;
                    }
                }
                if (hasSound)
                {
                    startFrame = frame;
                    break;
                }
            }

            // 무음이 아닌 끝 프레임 찾기
            int endFrame = totalSamples - 1;
            for (int frame = totalSamples - 1; frame >= 0; frame--)
            {
                bool hasSound = false;
                for (int ch = 0; ch < channels; ch++)
                {
                    if (Mathf.Abs(samples[frame * channels + ch]) > silenceThreshold)
                    {
                        hasSound = true;
                        break;
                    }
                }
                if (hasSound)
                {
                    endFrame = frame;
                    break;
                }
            }

            // 전체가 무음인 경우 원본 반환
            if (startFrame >= endFrame)
            {
                return clip;
            }

            // 무음 제거된 샘플 추출
            int trimmedFrames = endFrame - startFrame + 1;
            float[] trimmedSamples = new float[trimmedFrames * channels];
            System.Array.Copy(samples, startFrame * channels, trimmedSamples, 0, trimmedFrames * channels);

            // 새 AudioClip 생성
            AudioClip trimmedClip = AudioClip.Create(
                "TrimmedAudio",
                trimmedFrames,
                channels,
                clip.frequency,
                false
            );

            trimmedClip.SetData(trimmedSamples, 0);

            // Debug.Log($"[VoiceRecorder] TrimSilence: {totalSamples} -> {trimmedFrames} frames");

            return trimmedClip;
        }

        /// <summary>
        /// 녹음 상태 리셋 (모드 변경 시 호출)
        /// </summary>
        public void ResetRecordingState()
        {
            // Debug.Log("[VoiceRecorder] 녹음 상태 리셋");

            isRecording = false;
            isVoiceDetected = false;
            silenceTimer = 0f;
            recordingTimer = 0f;

            // 마이크가 준비되지 않았으면 재시작
            if (!isMicrophoneReady && Microphone.devices.Length > 0)
            {
                // Debug.Log("[VoiceRecorder] 마이크 재시작 시도");
                StartCoroutine(StartContinuousRecording());
            }
        }

        /// <summary>
        /// 연속 모드 일시정지 (다른 컴포넌트가 마이크 사용 시)
        /// </summary>
        public void PauseContinuousMode()
        {
            if (!enableContinuousMode) return;

            // Debug.Log("[VoiceRecorder] 연속 모드 일시정지");

            // 마이크 정지
            if (!string.IsNullOrEmpty(microphoneDevice) && Microphone.IsRecording(microphoneDevice))
            {
                Microphone.End(microphoneDevice);
            }

            isMicrophoneReady = false;
            isRecording = false;
            isVoiceDetected = false;
            silenceTimer = 0f;
            recordingTimer = 0f;
        }

        /// <summary>
        /// 연속 모드 재개
        /// </summary>
        public void ResumeContinuousMode()
        {
            if (!enableContinuousMode) return;

            // Debug.Log("[VoiceRecorder] 연속 모드 재개");

            // 마이크 재시작
            StartCoroutine(StartContinuousRecording());
        }
    }

    /// <summary>
    /// AudioClip을 WAV 파일로 저장하는 유틸리티 클래스
    /// </summary>
    public static class SavWav
    {
        const int HEADER_SIZE = 44;

        public static void Save(string filepath, AudioClip clip)
        {
            // WAV 파일 생성
            using (var fileStream = CreateEmpty(filepath))
            {
                ConvertAndWrite(fileStream, clip);
                WriteHeader(fileStream, clip);
            }
        }

        static FileStream CreateEmpty(string filepath)
        {
            var fileStream = new FileStream(filepath, FileMode.Create);
            byte emptyByte = new byte();

            for (int i = 0; i < HEADER_SIZE; i++)
            {
                fileStream.WriteByte(emptyByte);
            }

            return fileStream;
        }

        static void ConvertAndWrite(FileStream fileStream, AudioClip clip)
        {
            // 채널 수를 고려하여 전체 샘플 수 계산
            int totalSamples = clip.samples * clip.channels;
            var samples = new float[totalSamples];
            clip.GetData(samples, 0);

            short[] intData = new short[samples.Length];
            byte[] bytesData = new byte[samples.Length * 2];

            int rescaleFactor = 32767;

            for (int i = 0; i < samples.Length; i++)
            {
                intData[i] = (short)(samples[i] * rescaleFactor);
                byte[] byteArr = System.BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }

            fileStream.Write(bytesData, 0, bytesData.Length);
        }

        static void WriteHeader(FileStream fileStream, AudioClip clip)
        {
            var hz = clip.frequency;
            var channels = clip.channels;
            var samples = clip.samples;

            fileStream.Seek(0, SeekOrigin.Begin);

            byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
            fileStream.Write(riff, 0, 4);

            byte[] chunkSize = System.BitConverter.GetBytes(fileStream.Length - 8);
            fileStream.Write(chunkSize, 0, 4);

            byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
            fileStream.Write(wave, 0, 4);

            byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
            fileStream.Write(fmt, 0, 4);

            byte[] subChunk1 = System.BitConverter.GetBytes(16);
            fileStream.Write(subChunk1, 0, 4);

            ushort one = 1;
            byte[] audioFormat = System.BitConverter.GetBytes(one);
            fileStream.Write(audioFormat, 0, 2);

            byte[] numChannels = System.BitConverter.GetBytes(channels);
            fileStream.Write(numChannels, 0, 2);

            byte[] sampleRate = System.BitConverter.GetBytes(hz);
            fileStream.Write(sampleRate, 0, 4);

            byte[] byteRate = System.BitConverter.GetBytes(hz * channels * 2);
            fileStream.Write(byteRate, 0, 4);

            ushort blockAlign = (ushort)(channels * 2);
            fileStream.Write(System.BitConverter.GetBytes(blockAlign), 0, 2);

            ushort bps = 16;
            byte[] bitsPerSample = System.BitConverter.GetBytes(bps);
            fileStream.Write(bitsPerSample, 0, 2);

            byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
            fileStream.Write(datastring, 0, 4);

            byte[] subChunk2 = System.BitConverter.GetBytes(samples * channels * 2);
            fileStream.Write(subChunk2, 0, 4);
        }
    }
}
