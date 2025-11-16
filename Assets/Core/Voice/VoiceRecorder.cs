using UnityEngine;
using System.Collections;
using System.IO;

namespace LostSpells.Systems
{
    public class VoiceRecorder : MonoBehaviour
    {
        [Header("Recording Settings")]
        [Tooltip("녹음 시간 (초)")]
        public int recordingLength = 5;

        [Tooltip("샘플링 레이트 (Hz)")]
        public int sampleRate = 44100;

        [Tooltip("사용할 마이크 인덱스 (0 = 첫 번째)")]
        public int microphoneIndex = 0;

        private AudioClip recordedClip;
        private string microphoneDevice;
        private bool isRecording = false;

        void Start()
        {
            // 마이크 디바이스 선택
            if (Microphone.devices.Length > 0)
            {
                int index = Mathf.Clamp(microphoneIndex, 0, Microphone.devices.Length - 1);
                microphoneDevice = Microphone.devices[index];
                // Debug.Log($"마이크 선택: {microphoneDevice}");
            }
            else
            {
                Debug.LogError("마이크를 찾을 수 없습니다!");
            }
        }

        /// <summary>
        /// 녹음 시작
        /// </summary>
        public void StartRecording()
        {
            if (isRecording)
            {
                return;
            }

            if (string.IsNullOrEmpty(microphoneDevice))
            {
                Debug.LogError("마이크 디바이스가 없습니다!");
                return;
            }

            isRecording = true;

            // 녹음 시작
            recordedClip = Microphone.Start(microphoneDevice, false, recordingLength, sampleRate);
            // Debug.Log("녹음 시작");
        }

        /// <summary>
        /// 녹음 중지
        /// </summary>
        public void StopRecording()
        {
            if (!isRecording)
            {
                return;
            }

            isRecording = false;
            Microphone.End(microphoneDevice);
            // Debug.Log("녹음 중지");
        }

        /// <summary>
        /// 녹음된 오디오를 WAV 파일로 저장
        /// </summary>
        /// <returns>저장된 파일 경로</returns>
        public string SaveRecordingAsWav()
        {
            if (recordedClip == null)
            {
                Debug.LogError("저장할 녹음이 없습니다!");
                return null;
            }

            string filePath = Path.Combine(Application.persistentDataPath, "recorded_audio.wav");

            // WAV 파일로 변환
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
                Debug.LogError("녹음된 오디오가 없습니다!");
                return null;
            }

            // 무음 제거된 AudioClip 생성
            AudioClip trimmedClip = TrimSilence(recordedClip);

            string tempPath = Path.Combine(Application.persistentDataPath, "temp_audio.wav");
            SavWav.Save(tempPath, trimmedClip);

            byte[] audioData = File.ReadAllBytes(tempPath);
            File.Delete(tempPath);

            return audioData;
        }

        /// <summary>
        /// 오디오 클립에서 앞뒤 무음 제거
        /// </summary>
        private AudioClip TrimSilence(AudioClip clip, float silenceThreshold = 0.01f)
        {
            if (clip == null) return null;

            // 전체 샘플 데이터 가져오기
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            // 무음이 아닌 시작 지점 찾기
            int startIndex = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                if (Mathf.Abs(samples[i]) > silenceThreshold)
                {
                    startIndex = i;
                    break;
                }
            }

            // 무음이 아닌 끝 지점 찾기
            int endIndex = samples.Length - 1;
            for (int i = samples.Length - 1; i >= 0; i--)
            {
                if (Mathf.Abs(samples[i]) > silenceThreshold)
                {
                    endIndex = i;
                    break;
                }
            }

            // 전체가 무음인 경우 원본 반환
            if (startIndex >= endIndex)
            {
                return clip;
            }

            // 무음 제거된 샘플 추출
            int trimmedLength = endIndex - startIndex + 1;
            float[] trimmedSamples = new float[trimmedLength];
            System.Array.Copy(samples, startIndex, trimmedSamples, 0, trimmedLength);

            // 새 AudioClip 생성
            AudioClip trimmedClip = AudioClip.Create(
                "TrimmedAudio",
                trimmedLength / clip.channels,
                clip.channels,
                clip.frequency,
                false
            );

            trimmedClip.SetData(trimmedSamples, 0);

            return trimmedClip;
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
            var samples = new float[clip.samples];
            clip.GetData(samples, 0);

            short[] intData = new short[samples.Length];

            byte[] bytesData = new byte[samples.Length * 2];

            int rescaleFactor = 32767;

            for (int i = 0; i < samples.Length; i++)
            {
                intData[i] = (short)(samples[i] * rescaleFactor);
                byte[] byteArr = new byte[2];
                byteArr = System.BitConverter.GetBytes(intData[i]);
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
