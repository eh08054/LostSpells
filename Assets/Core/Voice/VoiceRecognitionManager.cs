using LostSpells.Components;
using LostSpells.Data;
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem; 
using UnityEngine.UI; 

namespace LostSpells.Systems
{
    /// <summary>
    /// 음성 인식 매니저 (상시 음성 감지 기반으로 변경됨)
    /// </summary>
    public class VoiceRecognitionManager : MonoBehaviour
    {
        [Header("Components")]
        [Tooltip("음성 녹음 컴포넌트")]
        public VoiceRecorder voiceRecorder;

        [Tooltip("서버 클라이언트")]
        public VoiceServerClient serverClient;

        [Tooltip("플레이어 컴포넌트 (상태 표시용)")]
        public PlayerComponent playerComponent;

        [Header("Settings")]
        [Tooltip("최소 녹음 시간. 이 시간 미만은 서버로 보내지 않음.")]
        public float minSpeechDuration = 1f;

        [Header("Constant Recording Settings")]
        [SerializeField] private int sampleRate = 44100;
        [SerializeField] private int preRollSeconds = 1; // 발화 전 추가할 시간
        [SerializeField] private float threshold = 0.02f; // 음성 감지 임계값
        [SerializeField] private float silenceTimeout = 1f; // 발화 종료 판정 시간

        private AudioClip audioClip;
        private string micDevice;

        private bool isRecording = false; // 현재 음성 발화 중인지 여부
        private float silenceTimer = 0f;
        private float recordingTimer = 0f;

        private int detectionStartIndex = 0; // 발화 시작 위치 (AudioClip 샘플 인덱스)
        private int sampleSize = 1024; // RMS 계산에 사용할 샘플 크기

        private bool isProcessing = false; // 서버 통신 중 상태 (중복 방지)

        private string originalPlayerName = "Wizard";
        private System.Collections.Generic.List<SkillData> activeSkills = new System.Collections.Generic.List<SkillData>();
        private UI.InGameUI inGameUI;


        private void Awake()
        {
            // 중복 방지 로직 
            VoiceRecognitionManager[] managers = FindObjectsByType<VoiceRecognitionManager>(FindObjectsSortMode.None);
            if (managers.Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            // 컴포넌트 자동 찾기 
            if (voiceRecorder == null)
            {
                voiceRecorder = gameObject.AddComponent<VoiceRecorder>();
            }

            if (serverClient == null)
            {
                serverClient = FindFirstObjectByType<VoiceServerClient>();
                if (serverClient == null)
                {
                    GameObject serverObj = new GameObject("VoiceServerClient");
                    serverClient = serverObj.AddComponent<VoiceServerClient>();
                }
            }
        }

        private void Start()
        {
            // PlayerComponent 초기화 
            if (playerComponent == null)
            {
                playerComponent = FindFirstObjectByType<PlayerComponent>();
            }

            if (playerComponent != null)
            {
                originalPlayerName = playerComponent.GetPlayerName();
            }

            // InGameUI 찾기 
            inGameUI = FindFirstObjectByType<LostSpells.UI.InGameUI>();

            // 언어 설정 로드 및 이벤트 구독 
            LoadLanguageSettings();
            LocalizationManager.Instance.OnLanguageChanged += OnUILanguageChanged;
            InitializeSkills();

            // --- ConstantRecording 로직 적용 시작 ---

            audioClip = Microphone.Start(micDevice, true, 60, sampleRate);

            while (Microphone.GetPosition(micDevice) <= 0) { }

            // --- ConstantRecording 로직 적용 끝 ---
        }

        private void OnDestroy()
        {
            // 이벤트 해제 
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= OnUILanguageChanged;
            }
        }

        /// <summary>
        /// UI 언어가 변경될 때 음성인식 언어도 자동 변경 
        /// </summary>
        private void OnUILanguageChanged()
        {
            var currentLanguage = LocalizationManager.Instance.CurrentLanguage;
            string languageCode = currentLanguage == Language.Korean ? "ko" : "en";

            ChangeLanguage(languageCode);

            var skills = DataManager.Instance.GetAllSkillData();
            if (skills != null && skills.Count > 0)
            {
                StartCoroutine(serverClient.SetSkills(skills));
            }

            if (SaveManager.Instance != null)
            {
                var saveData = SaveManager.Instance.GetCurrentSaveData();
                if (saveData != null)
                {
                    saveData.voiceRecognitionLanguage = languageCode;
                    SaveManager.Instance.SaveGame();
                }
            }
        }

        /// <summary>
        /// SaveManager에서 언어 설정 로드 
        /// </summary>
        private void LoadLanguageSettings()
        {
            if (SaveManager.Instance != null)
            {
                var saveData = SaveManager.Instance.GetCurrentSaveData();
                if (saveData != null && !string.IsNullOrEmpty(saveData.voiceRecognitionLanguage))
                {
                    string languageCode = saveData.voiceRecognitionLanguage;
                    serverClient.SetLanguage(languageCode);
                }
                else
                {
                    serverClient.SetLanguage("ko");
                }
            }
            else
            {
                serverClient.SetLanguage("ko");
            }
        }

        /// <summary>
        /// 활성화할 스킬 설정 (InGameUI에서 호출) (기존과 동일)
        /// </summary>
        public void SetActiveSkills(System.Collections.Generic.List<SkillData> skills)
        {
            activeSkills = skills;
            if (skills != null && skills.Count > 0)
            {
                if (serverClient == null)
                {
                    return;
                }
                StartCoroutine(serverClient.SetSkills(skills));
            }
        }

        private void Update()
        {
            // 서버 통신 중이거나 AudioClip이 없으면 로직 건너뛰기
            if (audioClip == null || isProcessing) return;

            int currentPos = Microphone.GetPosition(micDevice);

            // 현재 위치의 RMS 계산
            float rms = CalculateRMS(currentPos);

            if (!isRecording)
            {
                // rms가 threshold를 넘을 경우 발화 시작 판정. 
                if (rms > threshold)
                {
                    Debug.Log("Voice detected, starting recording.");
                    isRecording = true;
                    detectionStartIndex = currentPos;
                    silenceTimer = 0f;
                    recordingTimer = 0f; // 새로 시작 시 타이머 초기화

                    // UI 업데이트 (ConstantRecording의 UI 표시를 대체)
                    string recordingText = LocalizationManager.Instance.GetText("voice_recording");
                    UpdateVoiceRecognitionDisplay(recordingText);
                }
            }
            else
            {
                recordingTimer += Time.deltaTime;

                // rms가 threshold / 2보다 낮을 경우 "silence" 판정 
                if (rms < threshold / 2)
                {
                    silenceTimer += Time.deltaTime;
                    if (silenceTimer > silenceTimeout)
                    {
                        Debug.Log("Silence detected, stopping recording.");
                        Debug.Log("Total Recording Time: " + recordingTimer + " seconds");

                        // 녹음된 길이가 최소 시간 미만일 시 서버로 전달하지 않음.
                        if (recordingTimer < minSpeechDuration)
                        {
                            Debug.Log("Recording too short, discarding.");
                            isRecording = false;
                            silenceTimer = 0f;
                            recordingTimer = 0f;

                            // UI 업데이트
                            string tooShortText = LocalizationManager.Instance.GetText("voice_too_short");
                            UpdateVoiceRecognitionDisplay(tooShortText);
                            StartCoroutine(ClearVoiceRecognitionDisplayAfterDelay(1f));
                            return;
                        }
                        else
                        {
                            // 발화 종료 및 서버 전송
                            StopAndSend(currentPos);
                            silenceTimer = 0f;
                            recordingTimer = 0f;
                            isRecording = false;

                            // UI 업데이트
                            string processingText = LocalizationManager.Instance.GetText("voice_processing");
                            UpdateVoiceRecognitionDisplay(processingText);
                        }
                    }
                }
                else // 음성 지속
                {
                    silenceTimer = 0f; // 음성이 다시 감지되면 침묵 타이머 초기화
                }
            }
        }

        //현재 인덱스 앞 sampleSize만큼의 음성의 RMS를 계산함. (ConstantRecording 로직)
        private float CalculateRMS(int currentPos)
        {
            int startReadPos = currentPos - sampleSize;
            if (startReadPos < 0) startReadPos += audioClip.samples;

            float[] tempSamples = new float[sampleSize];
            if (startReadPos + sampleSize < audioClip.samples)
            {
                audioClip.GetData(tempSamples, startReadPos);
            }
            else
            {
                int endCount = audioClip.samples - startReadPos;
                float[] part1 = new float[endCount];
                float[] part2 = new float[sampleSize - endCount];

                audioClip.GetData(part1, startReadPos);
                audioClip.GetData(part2, 0);

                Array.Copy(part1, 0, tempSamples, 0, endCount);
                Array.Copy(part2, 0, tempSamples, endCount, part2.Length);
            }

            float sum = 0f;
            for (int i = 0; i < tempSamples.Length; i++)
            {
                sum += tempSamples[i] * tempSamples[i];
            }
            return Mathf.Sqrt(sum / tempSamples.Length);
        }

        //발화된 시간 사이의 음성 + 발화 전 preRollSeconds를 클립에 합쳐서 처리 준비. (ConstantRecording 로직)
        private void StopAndSend(int currentPos)
        {
            int preRollSamples = preRollSeconds * sampleRate;
            int finalStartIndex = detectionStartIndex - preRollSamples;
            if (finalStartIndex < 0) finalStartIndex += audioClip.samples;

            int totalLength = 0;
            if (currentPos >= finalStartIndex)
            {
                totalLength = currentPos - finalStartIndex;
            }
            else
            {
                totalLength = (audioClip.samples - finalStartIndex) + currentPos;
            }

            float[] fullData = new float[totalLength];

            if (currentPos >= finalStartIndex)
            {
                audioClip.GetData(fullData, finalStartIndex);
            }
            else
            {
                float[] part1 = new float[audioClip.samples - finalStartIndex];
                float[] part2 = new float[currentPos];

                audioClip.GetData(part1, finalStartIndex);
                audioClip.GetData(part2, 0);

                Array.Copy(part1, 0, fullData, 0, part1.Length);
                Array.Copy(part2, 0, fullData, part1.Length, part2.Length);
            }

            // ConstantRecording과 달리, 여기서 바로 서버로 보내지 않고
            // VoiceRecorder를 사용하기 위해 AudioClip을 생성하고 코루틴을 호출합니다.
            AudioClip clipToSend = AudioClip.Create("UserVoice", totalLength, 1, sampleRate, false);
            clipToSend.SetData(fullData, 0);

            // isProcessing 플래그 설정
            isProcessing = true;

            // 녹음된 클립을 처리하고 서버로 전송
            StartCoroutine(SendAudioToServer(clipToSend));
        }

        /// <summary>
        /// 서버로 오디오 전송 
        /// </summary>
        private IEnumerator SendAudioToServer(AudioClip clipToSend)
        {
            // VoiceRecorder의 TrimSilence/GetRecordingAsBytes 로직을 사용하지 않음
            // ConstantRecording 로직에서 이미 필요한 부분만 추출함.

            string tempPath = Path.Combine(Application.persistentDataPath, "temp_constant_audio.wav");

            // VoiceRecorder.cs에 있는 SavWav 유틸리티를 사용하여 WAV 파일로 저장
            SavWav.Save(tempPath, clipToSend);

            // 저장된 파일을 바이트 배열로 읽기
            byte[] audioData = File.ReadAllBytes(tempPath);
            File.Delete(tempPath);

            if (audioData == null || audioData.Length == 0)
            {
                string failedText = LocalizationManager.Instance.GetText("voice_failed");
                UpdateVoiceRecognitionDisplay(failedText);
                isProcessing = false;
                StartCoroutine(ClearVoiceRecognitionDisplayAfterDelay(2f));
                yield break;
            }

            // 서버로 전송
            yield return serverClient.RecognizeSkill(audioData, OnRecognitionResult);
        }

        /// <summary>
        /// 인식 결과 처리 
        /// </summary>
        private void OnRecognitionResult(RecognitionResult result)
        {
            isProcessing = false; // 처리 완료

            if (result == null)
            {
                // 실패: "서버 연결 실패" 표시
                UpdateVoiceRecognitionDisplay("서버 연결 실패");
                StartCoroutine(ClearVoiceRecognitionDisplayAfterDelay(3f));
                return;
            }

            string recognizedText = result.recognized_text;

            // 스킬이 1개뿐이면 무조건 그 스킬 선택
            if (activeSkills != null && activeSkills.Count == 1)
            {
                string onlySkill = activeSkills[0].voiceKeyword;
                string skillDisplayName = GetSkillDisplayName(onlySkill);

                UpdateVoiceRecognitionDisplay($"인식: {skillDisplayName} (100%)");
                ExecuteSkill(onlySkill);

                if (inGameUI != null)
                {
                    var accuracyScores = new System.Collections.Generic.Dictionary<string, float>();
                    accuracyScores[onlySkill] = 1.0f;
                    inGameUI.UpdateSkillAccuracy(accuracyScores);
                }

                StartCoroutine(ClearVoiceRecognitionDisplayAfterDelay(3f));
                return;
            }

            // 스킬 매칭 결과 처리
            if (result.best_match != null && !string.IsNullOrEmpty(result.best_match.skill))
            {
                string skillDisplayName = GetSkillDisplayName(result.best_match.skill);
                int scorePercent = Mathf.RoundToInt(result.best_match.score * 100);

                UpdateVoiceRecognitionDisplay($"인식: {skillDisplayName} ({scorePercent}%)");

                ExecuteSkill(result.best_match.skill);

                if (inGameUI != null)
                {
                    var accuracyScores = result.skill_scores;
                    if (accuracyScores == null || accuracyScores.Count == 0)
                    {
                        accuracyScores = new System.Collections.Generic.Dictionary<string, float>();
                        accuracyScores[result.best_match.skill] = result.best_match.score;
                    }
                    inGameUI.UpdateSkillAccuracy(accuracyScores);
                }
            }
            else
            {
                // 매칭 실패
                if (string.IsNullOrEmpty(recognizedText))
                {
                    UpdateVoiceRecognitionDisplay("인식 실패: 음성 없음");
                }
                else
                {
                    UpdateVoiceRecognitionDisplay($"인식 실패: {recognizedText}");
                }
            }

            StartCoroutine(ClearVoiceRecognitionDisplayAfterDelay(3f));
        }

        /// <summary>
        /// 스킬의 로컬라이즈된 이름 가져오기 
        /// </summary>
        private string GetSkillDisplayName(string voiceKeyword)
        {
            if (activeSkills != null && activeSkills.Count > 0)
            {
                foreach (var skill in activeSkills)
                {
                    if (skill.voiceKeyword == voiceKeyword)
                    {
                        return skill.GetLocalizedName();
                    }
                }
            }

            var allSkills = DataManager.Instance.GetAllSkillData();
            foreach (var skill in allSkills)
            {
                if (skill.voiceKeyword == voiceKeyword)
                {
                    return skill.GetLocalizedName();
                }
            }

            return voiceKeyword;
        }

        /// <summary>
        /// 일정 시간 후 음성인식 표시 지우기 
        /// </summary>
        private IEnumerator ClearVoiceRecognitionDisplayAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            UpdateVoiceRecognitionDisplay("");
        }

        /// <summary>
        /// 음성인식 결과 UI 업데이트 
        /// </summary>
        private void UpdateVoiceRecognitionDisplay(string text)
        {
            if (inGameUI != null)
            {
                inGameUI.UpdateVoiceRecognitionDisplay(text);
            }
        }

        /// <summary>
        /// 인식된 스킬 실행 
        /// </summary>
        private void ExecuteSkill(string skillName)
        {
            if (playerComponent == null)
            {
                Debug.LogError($"[VoiceRecognition] PlayerComponent가 null입니다!");
                return;
            }

            if (activeSkills == null || activeSkills.Count == 0)
            {
                Debug.LogError($"[VoiceRecognition] activeSkills가 비어있습니다!");
                return;
            }

            foreach (var skill in activeSkills)
            {
                if (skill.voiceKeyword == skillName)
                {
                    playerComponent.CastSkill(skill);
                    return;
                }
            }

            Debug.LogWarning($"[VoiceRecognition] 스킬을 찾을 수 없음: {skillName}");
        }

        /// <summary>
        /// 녹음 중인지 확인
        /// </summary>
        public bool IsRecording()
        {
            return isRecording;
        }

        /// <summary>
        /// 음성 인식 언어 변경
        /// </summary>
        public void ChangeLanguage(string languageCode)
        {
            if (serverClient != null)
            {
                serverClient.SetLanguage(languageCode);
            }
        }

        // 기존 VoiceRecognitionManager에 있던 불필요한 메서드 제거 (StartVoiceRecording, StopVoiceRecording, GetVoiceRecordKey, ParseKey)
        // 기존 Update()의 키 입력 처리 로직 제거
        // 기존 maxRecordingTime 필드 제거 (ConstantRecording 로직이 자동으로 끝냄)

        /// <summary>
        /// 스킬 초기화 - DataManager에서 스킬 로드 및 서버 설정 
        /// </summary>
        private void InitializeSkills()
        {
            var skills = DataManager.Instance.GetAllSkillData();

            if (skills != null && skills.Count > 0)
            {
                StartCoroutine(serverClient.SetSkills(skills));
            }
        }
    }
}