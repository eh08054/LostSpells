using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using LostSpells.Data;
using LostSpells.Components;

namespace LostSpells.Systems
{
    /// <summary>
    /// 음성 인식 매니저
    /// 스페이스바로 음성 인식 시작/중지
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
        [Tooltip("최대 녹음 시간 (초)")]
        public float maxRecordingTime = 5f;

        [Tooltip("최소 녹음 시간 (초)")]
        public float minRecordingTime = 0.5f;

        private bool isRecording = false;
        private float recordingStartTime = 0f;
        private string originalPlayerName = "Wizard"; // PlayerComponent의 기본 이름과 동일하게 설정

        // 현재 활성화된 스킬 목록 (InGameUI에서 설정)
        private System.Collections.Generic.List<SkillData> activeSkills = new System.Collections.Generic.List<SkillData>();
        private LostSpells.UI.InGameUI inGameUI;

        private void Awake()
        {
            // 컴포넌트 자동 찾기 - Awake에서 초기화하여 다른 스크립트의 Start보다 먼저 실행되도록 함
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
            if (playerComponent == null)
            {
                playerComponent = FindFirstObjectByType<PlayerComponent>();
            }

            if (playerComponent != null)
            {
                originalPlayerName = playerComponent.GetPlayerName();
                Debug.Log($"[VoiceRecognition] PlayerComponent 찾음: {originalPlayerName}");
            }
            else
            {
                Debug.LogWarning("[VoiceRecognition] PlayerComponent를 찾을 수 없습니다!");
            }

            // InGameUI 찾기
            inGameUI = FindFirstObjectByType<LostSpells.UI.InGameUI>();
            if (inGameUI == null)
            {
                Debug.LogWarning("[VoiceRecognition] InGameUI를 찾을 수 없습니다!");
            }

            // 언어 설정 로드
            LoadLanguageSettings();

            // UI 언어 변경 시 음성인식 언어도 자동 변경
            LocalizationManager.Instance.OnLanguageChanged += OnUILanguageChanged;

            // 스킬 데이터 로드 및 서버 설정
            InitializeSkills();
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

            // 음성인식 언어 변경
            ChangeLanguage(languageCode);

            // SaveManager에도 저장
            if (SaveManager.Instance != null)
            {
                var saveData = SaveManager.Instance.GetCurrentSaveData();
                if (saveData != null)
                {
                    saveData.voiceRecognitionLanguage = languageCode;
                    SaveManager.Instance.SaveGame();
                    Debug.Log($"[VoiceRecognition] UI 언어 변경에 따라 음성인식 언어 자동 변경: {languageCode}");
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
                    Debug.Log($"[VoiceRecognition] 언어 설정 로드: {languageCode}");
                }
                else
                {
                    Debug.LogWarning("[VoiceRecognition] 언어 설정을 찾을 수 없습니다. 기본값(ko) 사용");
                    serverClient.SetLanguage("ko");
                }
            }
            else
            {
                Debug.LogWarning("[VoiceRecognition] SaveManager를 찾을 수 없습니다. 기본값(ko) 사용");
                serverClient.SetLanguage("ko");
            }
        }

        /// <summary>
        /// 활성화할 스킬 설정 (InGameUI에서 호출)
        /// </summary>
        public void SetActiveSkills(System.Collections.Generic.List<SkillData> skills)
        {
            activeSkills = skills;
            if (skills != null && skills.Count > 0)
            {
                // serverClient가 아직 초기화되지 않았으면 대기
                if (serverClient == null)
                {
                    Debug.LogWarning("[VoiceRecognition] ServerClient가 아직 초기화되지 않았습니다. 나중에 다시 시도합니다.");
                    return;
                }

                // 서버에 새로운 스킬 목록 전송
                StartCoroutine(serverClient.SetSkills(skills));
                Debug.Log($"[VoiceRecognition] 활성 스킬 설정: {skills.Count}개 - {string.Join(", ", skills.ConvertAll(s => s.voiceKeyword))}");
            }
        }

        private void Update()
        {
            // 스페이스바 입력 처리
            if (Keyboard.current != null)
            {
                // 스페이스바를 누르면 녹음 시작
                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    StartVoiceRecording();
                }
                // 스페이스바를 떼면 녹음 중지
                else if (Keyboard.current.spaceKey.wasReleasedThisFrame)
                {
                    StopVoiceRecording();
                }
            }

            // 최대 녹음 시간 체크
            if (isRecording && Time.time - recordingStartTime >= maxRecordingTime)
            {
                StopVoiceRecording();
            }
        }

        /// <summary>
        /// 스킬 초기화 - DataManager에서 스킬 로드 및 서버 설정
        /// </summary>
        private void InitializeSkills()
        {
            // DataManager에서 스킬 데이터 가져오기
            var skills = DataManager.Instance.GetAllSkillData();

            if (skills != null && skills.Count > 0)
            {
                // 서버에 스킬 설정
                StartCoroutine(serverClient.SetSkills(skills));
                Debug.Log($"[VoiceRecognition] 스킬 초기화: {skills.Count}개");
            }
            else
            {
                Debug.LogWarning("[VoiceRecognition] 스킬 데이터가 없습니다.");
            }
        }

        /// <summary>
        /// 음성 녹음 시작
        /// </summary>
        private void StartVoiceRecording()
        {
            if (isRecording)
            {
                return;
            }

            isRecording = true;
            recordingStartTime = Time.time;

            voiceRecorder.StartRecording();

            // Display "Recording..." on voice recognition UI
            string recordingText = LocalizationManager.Instance.GetText("voice_recording");
            UpdateVoiceRecognitionDisplay(recordingText);

            Debug.Log("[VoiceRecognition] Recording started");
        }

        /// <summary>
        /// 음성 녹음 중지 및 인식 처리
        /// </summary>
        private void StopVoiceRecording()
        {
            if (!isRecording)
            {
                return;
            }

            // 최소 녹음 시간 체크
            float recordingDuration = Time.time - recordingStartTime;
            if (recordingDuration < minRecordingTime)
            {
                Debug.Log($"[VoiceRecognition] Recording too short ({recordingDuration:F2}s < {minRecordingTime}s), cancelling");
                isRecording = false;
                voiceRecorder.StopRecording();
                string tooShortText = LocalizationManager.Instance.GetText("voice_too_short");
                UpdateVoiceRecognitionDisplay(tooShortText);
                StartCoroutine(ClearVoiceRecognitionDisplayAfterDelay(1f));
                return;
            }

            isRecording = false;

            voiceRecorder.StopRecording();

            // Display "Processing..." on voice recognition UI
            string processingText = LocalizationManager.Instance.GetText("voice_processing");
            UpdateVoiceRecognitionDisplay(processingText);

            Debug.Log($"[VoiceRecognition] Recording stopped ({recordingDuration:F2}s)");

            // 서버로 전송
            StartCoroutine(SendAudioToServer());
        }

        /// <summary>
        /// 서버로 오디오 전송
        /// </summary>
        private IEnumerator SendAudioToServer()
        {
            // 녹음된 오디오를 바이트 배열로 변환
            byte[] audioData = voiceRecorder.GetRecordingAsBytes();

            if (audioData == null)
            {
                string failedText = LocalizationManager.Instance.GetText("voice_failed");
                UpdateVoiceRecognitionDisplay(failedText);
                yield return new WaitForSeconds(2f);
                UpdateVoiceRecognitionDisplay("");
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
            if (result == null)
            {
                string failedText = LocalizationManager.Instance.GetText("voice_failed");
                UpdateVoiceRecognitionDisplay(failedText);
                StartCoroutine(ClearVoiceRecognitionDisplayAfterDelay(2f));
                return;
            }

            // Display recognized text
            string recognizedText = result.recognized_text;
            Debug.Log($"[VoiceRecognition] Recognized: '{recognizedText}'");

            // 스킬이 1개뿐이면 무조건 그 스킬 선택
            if (activeSkills != null && activeSkills.Count == 1)
            {
                string onlySkill = activeSkills[0].voiceKeyword;
                // 스킬명만 간결하게 표시
                string skillDisplayName = GetSkillDisplayName(onlySkill);
                UpdateVoiceRecognitionDisplay(skillDisplayName);
                Debug.Log($"[VoiceRecognition] Only one skill available, auto-selecting: {onlySkill}");
                ExecuteSkill(onlySkill);

                // 정확도는 100%로 표시
                if (inGameUI != null)
                {
                    var accuracyScores = new System.Collections.Generic.Dictionary<string, float>();
                    accuracyScores[onlySkill] = 1.0f;
                    inGameUI.UpdateSkillAccuracy(accuracyScores);
                }

                StartCoroutine(ClearVoiceRecognitionDisplayAfterDelay(2f));
                return;
            }

            // 정확도 정보를 InGameUI에 전달
            if (inGameUI != null && result.skill_scores != null)
            {
                // skill_scores는 서버에서 받은 Dictionary<string, float>
                var accuracyScores = result.skill_scores;
                inGameUI.UpdateSkillAccuracy(accuracyScores);
                Debug.Log($"[VoiceRecognition] 정확도 업데이트: {accuracyScores.Count}개 스킬");
            }

            // 임계값 없이 항상 가장 유사한 프롬프트 단어 선택
            if (result.best_match != null)
            {
                // 스킬명만 간결하게 표시
                string skillDisplayName = GetSkillDisplayName(result.best_match.skill);
                UpdateVoiceRecognitionDisplay(skillDisplayName);
                Debug.Log($"[VoiceRecognition] Matched: {result.best_match.skill} ({result.best_match.score:F2})");

                // 스킬 실행
                ExecuteSkill(result.best_match.skill);
            }
            else
            {
                // 매칭 실패 시 인식된 텍스트 표시
                UpdateVoiceRecognitionDisplay(recognizedText);
            }

            // Clear display after 2 seconds
            StartCoroutine(ClearVoiceRecognitionDisplayAfterDelay(2f));
        }

        /// <summary>
        /// 스킬의 로컬라이즈된 이름 가져오기
        /// </summary>
        private string GetSkillDisplayName(string voiceKeyword)
        {
            var skills = DataManager.Instance.GetAllSkillData();
            foreach (var skill in skills)
            {
                if (skill.voiceKeyword == voiceKeyword)
                {
                    return skill.GetLocalizedName();
                }
            }
            return voiceKeyword; // 못 찾으면 키워드 그대로 반환
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
                Debug.Log($"[VoiceRecognition] UI 업데이트: '{text}'");
            }
            else
            {
                Debug.LogWarning("[VoiceRecognition] InGameUI가 null입니다!");
            }
        }

        /// <summary>
        /// 인식된 스킬 실행
        /// </summary>
        private void ExecuteSkill(string skillName)
        {
            if (playerComponent == null)
            {
                Debug.LogWarning("[VoiceRecognition] PlayerComponent를 찾을 수 없습니다!");
                return;
            }

            // 스킬 데이터 찾기
            var skills = DataManager.Instance.GetAllSkillData();
            foreach (var skill in skills)
            {
                if (skill.voiceKeyword == skillName)
                {
                    Debug.Log($"[VoiceRecognition] 스킬 실행 시도: {skill.skillName} (ID: {skill.skillId})");

                    // PlayerComponent를 통해 스킬 실행
                    bool success = playerComponent.CastSkill(skill);

                    if (success)
                    {
                        Debug.Log($"[VoiceRecognition] 스킬 실행 성공: {skill.skillName}");
                    }
                    else
                    {
                        Debug.LogWarning($"[VoiceRecognition] 스킬 실행 실패: {skill.skillName} (마나 부족 또는 프리팹 없음)");
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// 녹음 중인지 확인
        /// </summary>
        public bool IsRecording()
        {
            return isRecording;
        }

        /// <summary>
        /// 음성 인식 언어 변경 (옵션에서 호출 가능)
        /// </summary>
        public void ChangeLanguage(string languageCode)
        {
            if (serverClient != null)
            {
                serverClient.SetLanguage(languageCode);
                Debug.Log($"[VoiceRecognition] 언어 변경: {languageCode}");
            }
            else
            {
                Debug.LogWarning("[VoiceRecognition] ServerClient가 null입니다. 언어 변경 실패");
            }
        }
    }
}
