using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using System.Collections;
using LostSpells.Data;
using LostSpells.Components;

namespace LostSpells.Systems
{
    /// <summary>
    /// 게임 컨텍스트 (현재 화면 상태)
    /// </summary>
    public enum GameContext
    {
        Unknown,
        Menu_MainMenu,
        Menu_GameModeSelection,
        Menu_StoryMode,
        Menu_EndlessMode,
        InGame_Playing,
        InGame_Paused,
        InGame_GameOver,
        Options,
        Store
    }

    /// <summary>
    /// 음성 입력 모드
    /// </summary>
    public enum VoiceInputMode
    {
        KeyTriggered,  // 키(스페이스바) 트리거 모드
        Continuous     // 연속 음성 감지 모드 (VAD)
    }

    /// <summary>
    /// 음성 인식 매니저 (전역 싱글톤)
    /// 스페이스바로 음성 인식 시작/중지
    /// 모든 씬에서 동작하며 씬 전환 시에도 유지됨
    /// </summary>
    public class VoiceRecognitionManager : MonoBehaviour
    {
        // 싱글톤 인스턴스
        private static VoiceRecognitionManager _instance;
        public static VoiceRecognitionManager Instance => _instance;

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

        [Header("Voice Input Mode")]
        [Tooltip("음성 입력 모드 (키 트리거 / 연속 감지)")]
        public VoiceInputMode inputMode = VoiceInputMode.KeyTriggered;

        [Tooltip("피치 분석 활성화")]
        public bool enablePitchAnalysis = true;

        [Tooltip("피치 분석기")]
        public PitchAnalyzer pitchAnalyzer;

        private bool isRecording = false;
        private float recordingStartTime = 0f;
        private string originalPlayerName = "Wizard"; // PlayerComponent의 기본 이름과 동일하게 설정

        // 피치 기반 속성 시스템
        private PitchCategory lastDetectedPitchCategory = PitchCategory.Medium;

        // 고정 속성 모드 (특정 탭 선택 시)
        private bool useFixedElement = false;
        private string fixedElement = null;
        private PitchCategory? fixedPitchCategory = null;

        // 일반 스킬 목록 (피치 기반 속성 적용 대상)
        private static readonly System.Collections.Generic.HashSet<string> genericSkillIds = new System.Collections.Generic.HashSet<string>
        {
            "missile", "shield", "slash", "explosion", "vortex"
        };

        // 현재 활성화된 스킬 목록 (InGameUI에서 설정)
        private System.Collections.Generic.List<SkillData> activeSkills = new System.Collections.Generic.List<SkillData>();
        private LostSpells.UI.InGameUI inGameUI;
        private LostSpells.UI.MenuManager menuManager;

        private void Awake()
        {
            // 싱글톤 패턴: 이미 인스턴스가 있으면 제거
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시에도 유지

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
                    serverObj.transform.SetParent(transform); // 자식으로 설정하여 함께 유지
                    serverClient = serverObj.AddComponent<VoiceServerClient>();
                }
            }

            // PitchAnalyzer 초기화
            if (pitchAnalyzer == null && enablePitchAnalysis)
            {
                pitchAnalyzer = gameObject.AddComponent<PitchAnalyzer>();
            }

            // VoiceRecorder 이벤트 구독 (연속 모드용)
            if (voiceRecorder != null)
            {
                voiceRecorder.OnVoiceDetected += OnContinuousModeVoiceDetected;
                voiceRecorder.OnVoiceRecordingComplete += OnContinuousModeRecordingComplete;
            }

            // 씬 로드 이벤트 등록
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            RefreshSceneReferences();

            // 언어 설정 로드
            LoadLanguageSettings();

            // 음성 입력 모드 설정 로드
            LoadVoiceInputModeSettings();

            // UI 언어 변경 시 음성인식 언어도 자동 변경
            LocalizationManager.Instance.OnLanguageChanged += OnUILanguageChanged;

            // 스킬 데이터 로드 및 서버 설정
            InitializeSkills();
        }

        /// <summary>
        /// 저장된 음성 입력 모드 설정 로드
        /// </summary>
        private void LoadVoiceInputModeSettings()
        {
            var saveData = SaveManager.Instance?.GetCurrentSaveData();
            if (saveData != null && !string.IsNullOrEmpty(saveData.voiceInputMode))
            {
                VoiceInputMode mode = saveData.voiceInputMode == "Continuous"
                    ? VoiceInputMode.Continuous
                    : VoiceInputMode.KeyTriggered;
                SetVoiceInputMode(mode);
            }
        }

        /// <summary>
        /// 피치 카테고리에 해당하는 속성 가져오기
        /// </summary>
        private string GetElementForPitchCategory(PitchCategory category)
        {
            var saveData = SaveManager.Instance?.GetCurrentSaveData();
            if (saveData == null)
            {
                // 기본값
                switch (category)
                {
                    case PitchCategory.Low: return "Fire";
                    case PitchCategory.Medium: return "Ice";
                    case PitchCategory.High: return "Electric";
                    default: return "Fire";
                }
            }

            switch (category)
            {
                case PitchCategory.Low:
                    return !string.IsNullOrEmpty(saveData.lowPitchElement) ? saveData.lowPitchElement : "Fire";
                case PitchCategory.Medium:
                    return !string.IsNullOrEmpty(saveData.midPitchElement) ? saveData.midPitchElement : "Ice";
                case PitchCategory.High:
                    return !string.IsNullOrEmpty(saveData.highPitchElement) ? saveData.highPitchElement : "Electric";
                default:
                    return "Fire";
            }
        }

        /// <summary>
        /// 고정 속성 모드 설정 (특정 탭 선택 시)
        /// pitchCategory가 null이면 전체 탭 (실시간 피치 감지)
        /// </summary>
        public void SetFixedElement(PitchCategory? pitchCategory, string element)
        {
            fixedPitchCategory = pitchCategory;

            if (pitchCategory == null)
            {
                // 전체 탭: 실시간 피치 감지 모드
                useFixedElement = false;
                fixedElement = null;
                Debug.Log("[VoiceRecognition] 전체 탭 선택: 실시간 피치 감지 모드");
            }
            else
            {
                // 특정 탭: 고정 속성 모드
                useFixedElement = true;
                fixedElement = element;
                Debug.Log($"[VoiceRecognition] 고정 속성 모드: {element} (피치: {pitchCategory})");
            }
        }

        /// <summary>
        /// 현재 사용할 속성 반환 (고정 모드면 고정 속성, 아니면 피치 기반)
        /// </summary>
        public string GetCurrentElement()
        {
            if (useFixedElement && !string.IsNullOrEmpty(fixedElement))
            {
                return fixedElement;
            }
            return GetElementForPitchCategory(lastDetectedPitchCategory);
        }

        /// <summary>
        /// 현재 고정 속성 모드인지 반환
        /// </summary>
        public bool IsFixedElementMode()
        {
            return useFixedElement;
        }

        /// <summary>
        /// 일반 스킬을 속성별 스킬로 변환
        /// </summary>
        private string GetElementSkillName(SkillData skill, string element)
        {
            if (skill == null || skill.elementVariants == null) return null;

            if (skill.elementVariants.ContainsKey(element))
            {
                var variant = skill.elementVariants[element];
                return variant.name;
            }

            return null;
        }

        /// <summary>
        /// 씬이 로드될 때마다 참조 갱신
        /// </summary>
        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            // Additive 로드는 무시 (Options 등)
            if (mode == UnityEngine.SceneManagement.LoadSceneMode.Additive)
                return;

            RefreshSceneReferences();
        }

        /// <summary>
        /// 현재 씬의 참조 갱신
        /// </summary>
        private void RefreshSceneReferences()
        {
            playerComponent = FindFirstObjectByType<PlayerComponent>();
            if (playerComponent != null)
            {
                originalPlayerName = playerComponent.GetPlayerName();
            }

            // InGameUI 찾기 (없으면 null)
            inGameUI = FindFirstObjectByType<LostSpells.UI.InGameUI>();

            // MenuManager 찾기 (없으면 null)
            menuManager = FindFirstObjectByType<LostSpells.UI.MenuManager>();

            // 스킬 초기화
            InitializeSkills();
        }

        private void OnDestroy()
        {
            // 이벤트 해제
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= OnUILanguageChanged;
            }

            // VoiceRecorder 이벤트 해제
            if (voiceRecorder != null)
            {
                voiceRecorder.OnVoiceDetected -= OnContinuousModeVoiceDetected;
                voiceRecorder.OnVoiceRecordingComplete -= OnContinuousModeRecordingComplete;
            }

            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;

            if (_instance == this)
            {
                _instance = null;
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

            // 스킬 목록 다시 설정 (언어에 맞는 키워드로 업데이트)
            var skills = DataManager.Instance.GetAllSkillData();
            if (skills != null && skills.Count > 0)
            {
                StartCoroutine(serverClient.SetSkills(skills));
            }

            // SaveManager에도 저장
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
        /// 활성화할 스킬 설정 (InGameUI에서 호출)
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

        // 실시간 피치 분석 간격 (프레임마다 분석하면 부하가 크므로)
        private float pitchAnalysisInterval = 0.1f; // 100ms마다
        private float lastPitchAnalysisTime = 0f;

        private void Update()
        {
            // 키 트리거 모드에서만 키 입력 처리
            if (inputMode == VoiceInputMode.KeyTriggered)
            {
                // 키바인딩에서 음성 녹음 키 가져오기
                if (Keyboard.current != null)
                {
                    Key voiceRecordKey = GetVoiceRecordKey();

                    // 음성 녹음 키를 누르면 녹음 시작
                    if (Keyboard.current[voiceRecordKey].wasPressedThisFrame)
                    {
                        // Debug.Log($"[VoiceRecognition] 음성 녹음 키 눌림 (TimeScale: {Time.timeScale})");
                        StartVoiceRecording();
                    }
                    // 음성 녹음 키를 떼면 녹음 중지
                    else if (Keyboard.current[voiceRecordKey].wasReleasedThisFrame)
                    {
                        // Debug.Log($"[VoiceRecognition] 음성 녹음 키 뗌 (TimeScale: {Time.timeScale})");
                        StopVoiceRecording();
                    }
                }

                // 최대 녹음 시간 체크 (Time.unscaledTime 사용 - 일시정지 중에도 동작)
                if (isRecording && Time.unscaledTime - recordingStartTime >= maxRecordingTime)
                {
                    StopVoiceRecording();
                }
            }
            // 연속 모드는 VoiceRecorder의 이벤트로 처리됨

            // 실시간 피치 분석 및 시각화 (녹음 중일 때만)
            if (isRecording && enablePitchAnalysis && pitchAnalyzer != null && playerComponent != null)
            {
                var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (currentScene == "InGame" && Time.unscaledTime - lastPitchAnalysisTime >= pitchAnalysisInterval)
                {
                    lastPitchAnalysisTime = Time.unscaledTime;
                    UpdateRealtimePitchVisualization();
                }
            }
        }

        /// <summary>
        /// 실시간 피치 시각화 업데이트
        /// </summary>
        private void UpdateRealtimePitchVisualization()
        {
            if (voiceRecorder == null || pitchAnalyzer == null) return;

            // 고정 속성 모드일 때는 고정 색상 유지
            if (useFixedElement && !string.IsNullOrEmpty(fixedElement))
            {
                if (playerComponent != null)
                {
                    playerComponent.UpdateVoiceParticleByPitch(
                        fixedPitchCategory ?? PitchCategory.Medium,
                        fixedElement
                    );
                }
                return;
            }

            // 현재 오디오 샘플 가져오기
            float[] samples = voiceRecorder.GetCurrentAudioSamples(2048);
            if (samples == null) return;

            // 피치 감지
            float frequency = pitchAnalyzer.DetectPitchRealtime(samples, 16000);
            if (frequency <= 0) return;

            // 피치 카테고리 결정
            PitchCategory category = pitchAnalyzer.GetCategory(frequency);
            lastDetectedPitchCategory = category;

            // 피치 카테고리에 해당하는 속성 가져오기
            string element = GetElementForPitchCategory(category);

            // 파티클 색상 업데이트
            if (playerComponent != null)
            {
                playerComponent.UpdateVoiceParticleByPitch(category, element);
            }
        }

        /// <summary>
        /// SaveData에서 음성 녹음 키 가져오기
        /// </summary>
        private Key GetVoiceRecordKey()
        {
            var saveData = SaveManager.Instance?.GetCurrentSaveData();
            if (saveData != null && saveData.keyBindings != null && saveData.keyBindings.ContainsKey("VoiceRecord"))
            {
                string keyString = saveData.keyBindings["VoiceRecord"];
                return ParseKey(keyString, Key.Space);
            }

            // 기본값: Space
            return Key.Space;
        }

        /// <summary>
        /// 키 문자열을 Key enum으로 변환 (Options의 GetKeyDisplayName 역함수)
        /// </summary>
        private Key ParseKey(string keyString, Key defaultKey)
        {
            // 특수 키 매핑 (Options의 GetKeyDisplayName과 반대)
            switch (keyString)
            {
                case "Space": return Key.Space;
                case "LShift": return Key.LeftShift;
                case "RShift": return Key.RightShift;
                case "LCtrl": return Key.LeftCtrl;
                case "RCtrl": return Key.RightCtrl;
                case "LAlt": return Key.LeftAlt;
                case "RAlt": return Key.RightAlt;
                case "Tab": return Key.Tab;
                case "Enter": return Key.Enter;
                case "Backspace": return Key.Backspace;
                default:
                    // 일반 키는 Enum.TryParse 시도
                    if (System.Enum.TryParse<Key>(keyString, true, out Key key))
                    {
                        return key;
                    }
                    return defaultKey;
            }
        }

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

        /// <summary>
        /// 음성 녹음 시작
        /// </summary>
        private void StartVoiceRecording()
        {
            // Debug.Log($"[VoiceRecognition] StartVoiceRecording 호출됨 (isRecording: {isRecording})");

            if (isRecording)
            {
                // Debug.Log("[VoiceRecognition] StartVoiceRecording 무시: 이미 녹음 중");
                return;
            }

            isRecording = true;
            recordingStartTime = Time.unscaledTime; // 일시정지 중에도 동작하도록
            // Debug.Log($"[VoiceRecognition] 녹음 시작 시간: {recordingStartTime}");

            voiceRecorder.StartRecording();

            // 음성인식 파티클 표시
            if (playerComponent != null)
            {
                playerComponent.ShowVoiceRecognitionParticle();
            }

            // 인게임에서는 "녹음 중" 메시지 표시하지 않음 (스킬 인식 결과만 표시)
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentScene != "InGame")
            {
                string recordingText = LocalizationManager.Instance.GetText("voice_recording");
                UpdateVoiceRecognitionDisplay(recordingText);
            }
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

            float recordingDuration = Time.unscaledTime - recordingStartTime; // 일시정지 중에도 동작하도록
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            if (recordingDuration < minRecordingTime)
            {
                isRecording = false;
                voiceRecorder.StopRecording();

                // 녹음 시간 부족: 파티클 숨김
                if (playerComponent != null)
                {
                    playerComponent.HideVoiceRecognitionParticle();
                }

                // 인게임에서는 "녹음 시간 부족" 메시지 표시하지 않음
                if (currentScene != "InGame")
                {
                    string tooShortText = LocalizationManager.Instance.GetText("voice_too_short");
                    UpdateVoiceRecognitionDisplay(tooShortText);
                    StartCoroutine(ClearVoiceRecognitionDisplayAfterDelay(1f));
                }
                return;
            }

            isRecording = false;

            voiceRecorder.StopRecording();

            // 인게임에서는 "처리 중" 메시지 표시하지 않음
            if (currentScene != "InGame")
            {
                string processingText = LocalizationManager.Instance.GetText("voice_processing");
                UpdateVoiceRecognitionDisplay(processingText);
            }

            // 화면 하단 상태 패널에 "인식중..." 표시
            UpdateVoiceStatusPanelUI("", "인식중");

            // 서버로 전송
            StartCoroutine(SendAudioToServer());
        }

        /// <summary>
        /// 서버로 오디오 전송
        /// </summary>
        private IEnumerator SendAudioToServer()
        {
            // 피치 분석 (녹음된 오디오로 분석)
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (enablePitchAnalysis && currentScene == "InGame" && pitchAnalyzer != null)
            {
                AudioClip recordedClip = voiceRecorder.GetRecordedClip();
                if (recordedClip != null)
                {
                    var pitchResult = pitchAnalyzer.AnalyzeClip(recordedClip);
                    if (pitchResult != null)
                    {
                        lastDetectedPitchCategory = pitchResult.DominantCategory;
                        Debug.Log($"[VoiceRecognition] 피치 분석 결과: {lastDetectedPitchCategory} (Low:{pitchResult.LowCount}, Mid:{pitchResult.MediumCount}, High:{pitchResult.HighCount})");
                    }
                }
            }

            // 녹음된 오디오를 바이트 배열로 변환
            byte[] audioData = voiceRecorder.GetRecordingAsBytes();

            if (audioData == null)
            {
                // 오디오 데이터 없음: 파티클 숨김
                if (playerComponent != null)
                {
                    playerComponent.HideVoiceRecognitionParticle();
                }

                string failedText = LocalizationManager.Instance.GetText("voice_failed");
                UpdateVoiceRecognitionDisplay(failedText);
                yield return new WaitForSecondsRealtime(2f); // 일시정지 중에도 동작
                UpdateVoiceRecognitionDisplay("");
                yield break;
            }

            // 현재 컨텍스트와 키워드 가져오기
            string context = GetCurrentContextString();
            string contextKeywords = GetContextKeywords();

            // 서버로 전송 (컨텍스트 정보 포함)
            yield return serverClient.RecognizeSkill(audioData, context, contextKeywords, OnRecognitionResult);
        }

        /// <summary>
        /// 인식 결과 처리
        /// </summary>
        private void OnRecognitionResult(RecognitionResult result)
        {
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            bool isInGame = currentScene == "InGame";

            if (result == null)
            {
                // 실패: 파티클 숨김
                if (playerComponent != null)
                {
                    playerComponent.HideVoiceRecognitionParticle();
                }

                // 인게임에서는 실패 메시지 표시하지 않음
                if (!isInGame)
                {
                    UpdateVoiceRecognitionDisplay("서버 연결 실패");
                    StartCoroutine(ClearVoiceRecognitionDisplayAfterDelay(3f));
                }

                // 하단 상태 패널에도 실패 표시
                UpdateVoiceStatusPanelUI("", "서버 연결 실패");
                StartCoroutine(ClearVoiceStatusPanelAfterDelay(3f));
                return;
            }

            string recognizedText = result.recognized_text;

            // 스킬이 1개뿐이면 무조건 그 스킬 선택
            if (activeSkills != null && activeSkills.Count == 1)
            {
                string onlySkill = activeSkills[0].voiceKeyword;
                string skillDisplayName = GetSkillDisplayName(onlySkill);

                // 인게임: 스킬 이름만 표시, 그 외: "인식: [스킬명] (100%)"
                if (isInGame)
                {
                    UpdateVoiceRecognitionDisplay(skillDisplayName);
                }
                else
                {
                    UpdateVoiceRecognitionDisplay($"인식: {skillDisplayName} (100%)");
                }

                // 화면 하단 상태 패널 업데이트
                UpdateVoiceStatusPanelUI(recognizedText, skillDisplayName);

                ExecuteSkill(onlySkill);

                StartCoroutine(ClearVoiceRecognitionDisplayAfterDelay(3f));
                StartCoroutine(ClearVoiceStatusPanelAfterDelay(3f));
                return;
            }

            // 스킬 매칭 결과 처리
            if (result.best_match != null && !string.IsNullOrEmpty(result.best_match.skill))
            {
                string skillDisplayName = GetSkillDisplayName(result.best_match.skill);
                int scorePercent = Mathf.RoundToInt(result.best_match.score * 100);

                // 인게임: 스킬 이름만 표시, 그 외: "인식: [스킬명] (정확도%)"
                if (isInGame)
                {
                    UpdateVoiceRecognitionDisplay(skillDisplayName);
                }
                else
                {
                    UpdateVoiceRecognitionDisplay($"인식: {skillDisplayName} ({scorePercent}%)");
                }

                // 화면 하단 상태 패널 업데이트
                UpdateVoiceStatusPanelUI(recognizedText, skillDisplayName);

                // 스킬 실행
                ExecuteSkill(result.best_match.skill);
            }
            else
            {
                // 매칭 실패: 파티클 숨김
                if (playerComponent != null)
                {
                    playerComponent.HideVoiceRecognitionParticle();
                }

                // 인게임에서는 실패 메시지 표시하지 않음
                if (!isInGame)
                {
                    if (string.IsNullOrEmpty(recognizedText))
                    {
                        UpdateVoiceRecognitionDisplay("인식실패: 음성 없음");
                    }
                    else
                    {
                        UpdateVoiceRecognitionDisplay($"인식실패: {recognizedText}");
                    }
                }

                // 실패 시에도 하단 패널에 표시
                UpdateVoiceStatusPanelUI(recognizedText, "인식실패");
            }

            StartCoroutine(ClearVoiceRecognitionDisplayAfterDelay(3f));
            StartCoroutine(ClearVoiceStatusPanelAfterDelay(3f));
        }

        /// <summary>
        /// 속성의 한국어 이름 반환
        /// </summary>
        private string GetElementKoreanName(string element)
        {
            switch (element)
            {
                case "Fire": return "화";
                case "Ice": return "빙";
                case "Electric": return "뇌";
                case "Earth": return "지";
                case "Holy": return "성";
                case "Void": return "암";
                default: return element;
            }
        }

        /// <summary>
        /// 스킬 또는 시스템 명령의 표시 이름 가져오기 (속성명 포함)
        /// </summary>
        private string GetSkillDisplayName(string voiceKeyword)
        {
            // 시스템 명령 처리
            if (voiceKeyword.StartsWith("SYSTEM:"))
            {
                string command = voiceKeyword.Substring(7);
                return GetSystemCommandDisplayName(command);
            }

            // 스킬 찾기 (전체에서 검색)
            var allSkills = DataManager.Instance.GetAllSkillData();
            foreach (var skill in allSkills)
            {
                if (skill.voiceKeyword == voiceKeyword)
                {
                    // 일반 스킬인 경우 속성명 추가
                    if (skill.isGenericSkill && skill.elementVariants != null)
                    {
                        string element = GetCurrentElement();
                        if (skill.elementVariants.ContainsKey(element))
                        {
                            string elementKorean = GetElementKoreanName(element);
                            return $"{skill.GetLocalizedName()} ({elementKorean})";
                        }
                    }
                    return skill.GetLocalizedName();
                }
            }

            return voiceKeyword; // 못 찾으면 키워드 그대로 반환
        }

        /// <summary>
        /// 시스템 명령의 표시 이름 가져오기
        /// </summary>
        private string GetSystemCommandDisplayName(string command)
        {
            switch (command)
            {
                case "OpenSettings": return "설정 열기";
                case "CloseSettings": return "설정 닫기";
                case "OpenMenu": return "메뉴 열기";
                case "CloseMenu": return "메뉴 닫기";
                case "PauseGame": return "일시정지";
                case "ResumeGame": return "게임 재개";
                case "RestartGame": return "재시작";
                case "QuitToMainMenu": return "메인 메뉴로";
                case "OpenInventory": return "인벤토리";
                case "CloseInventory": return "인벤토리 닫기";
                case "OpenMap": return "지도";
                case "CloseMap": return "지도 닫기";
                case "ShowHelp": return "도움말";
                // 메뉴 네비게이션
                case "StartGame": return "게임 시작";
                case "SelectStoryMode": return "스토리 모드";
                case "SelectEndlessMode": return "무한 모드";
                case "StartEndless": return "무한 모드 시작";
                case "GoBack": return "뒤로가기";
                case "GoToMainMenu": return "메인 메뉴로";
                case "GoToGameModeSelection": return "게임 모드 선택으로";
                case "OpenStore": return "상점";
                // 튜토리얼 및 챕터 선택
                case "SelectTutorial": return "튜토리얼";
                case "SelectChapter1": return "챕터 1";
                case "SelectChapter2": return "챕터 2";
                case "SelectChapter3": return "챕터 3";
                case "SelectChapter4": return "챕터 4";
                case "SelectChapter5": return "챕터 5";
                case "SelectChapter6": return "챕터 6";
                case "SelectChapter7": return "챕터 7";
                case "SelectChapter8": return "챕터 8";
                case "SelectChapter9": return "챕터 9";
                case "SelectChapter10": return "챕터 10";
                case "SelectChapter11": return "챕터 11";
                case "SelectChapter12": return "챕터 12";
                // Options 탭 전환
                case "ShowAudioTab": return "오디오 탭";
                case "ShowGraphicsTab": return "그래픽 탭";
                case "ShowLanguageTab": return "언어 탭";
                case "ShowGameTab": return "게임 탭";
                // Options 섹션 명령
                case "ExpandVoiceRecognition": return "음성인식 펼치기";
                case "CollapseVoiceRecognition": return "음성인식 접기";
                case "ExpandKeyBinding": return "키설정 펼치기";
                case "CollapseKeyBinding": return "키설정 접기";
                // InGame 이동 명령
                case "MoveLeft": return "왼쪽 이동";
                case "MoveRight": return "오른쪽 이동";
                case "Jump": return "점프";
                case "StopMove": return "정지";
                default: return command;
            }
        }

        /// <summary>
        /// 일정 시간 후 음성인식 표시 지우기 (일시정지 중에도 동작)
        /// </summary>
        private IEnumerator ClearVoiceRecognitionDisplayAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay); // 일시정지 중에도 동작
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
        /// 음성인식 상태 패널 업데이트 (화면 하단 - 인식 텍스트와 명령어 표시)
        /// </summary>
        private void UpdateVoiceStatusPanelUI(string recognizedText, string executedCommand)
        {
            if (inGameUI != null)
            {
                inGameUI.UpdateVoiceStatusPanel(recognizedText, executedCommand);
            }
            if (menuManager != null)
            {
                menuManager.UpdateVoiceStatusPanel(recognizedText, executedCommand);
            }
        }

        /// <summary>
        /// 음성인식 상태 패널 지우기 (일정 시간 후)
        /// </summary>
        private IEnumerator ClearVoiceStatusPanelAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            UpdateVoiceStatusPanelUI("", "");
        }

        /// <summary>
        /// 인식된 스킬 또는 시스템 명령 실행
        /// </summary>
        private void ExecuteSkill(string skillName)
        {
            // Debug.Log($"[VoiceRecognition] ExecuteSkill 호출됨: {skillName}");

            // 음성인식 파티클 숨김 (스킬 발동 전)
            if (playerComponent != null)
            {
                playerComponent.HideVoiceRecognitionParticle();
            }

            // 시스템 명령 처리 (SYSTEM:OpenSettings 형식)
            if (skillName.StartsWith("SYSTEM:"))
            {
                string systemCommand = skillName.Substring(7); // "SYSTEM:" 제거

                // SYSTEM:UseXXX 형태는 스킬로 처리 (서버에서 잘못 분류된 경우)
                if (systemCommand.StartsWith("Use"))
                {
                    string extractedSkillName = systemCommand.Substring(3); // "Use" 제거
                    // Debug.Log($"[VoiceRecognition] SYSTEM:Use 명령을 스킬로 변환: {extractedSkillName}");
                    ExecuteSkillByName(extractedSkillName);
                    return;
                }

                // Debug.Log($"[VoiceRecognition] 시스템 명령 추출됨: {systemCommand}");
                ExecuteSystemCommand(systemCommand);
                return;
            }

            ExecuteSkillByName(skillName);
        }

        /// <summary>
        /// 스킬 이름으로 스킬 실행 (음성인식: 전체 스킬에서 검색하여 현재 피치 속성 적용)
        /// </summary>
        private void ExecuteSkillByName(string skillName)
        {
            if (playerComponent == null)
            {
                return;
            }

            // 음성인식은 항상 전체 스킬에서 검색 (전체 탭처럼 동작)
            // DataManager에서 모든 스킬 가져오기
            var allSkills = DataManager.Instance.GetAllSkillData();
            if (allSkills == null || allSkills.Count == 0)
            {
                Debug.LogWarning("[VoiceRecognition] 스킬 데이터가 없습니다.");
                return;
            }

            string skillNameLower = skillName.ToLower().Replace(" ", "");
            foreach (var skill in allSkills)
            {
                // 음성 키워드로 매칭
                if (!string.IsNullOrEmpty(skill.voiceKeyword))
                {
                    string keywordLower = skill.voiceKeyword.ToLower().Replace(" ", "");
                    if (keywordLower == skillNameLower || skillNameLower.Contains(keywordLower))
                    {
                        ExecuteMatchedSkill(skill);
                        return;
                    }
                }

                // 영어 스킬 이름으로 매칭
                if (!string.IsNullOrEmpty(skill.skillNameEn))
                {
                    string skillEnLower = skill.skillNameEn.ToLower().Replace(" ", "");
                    if (skillEnLower.Contains(skillNameLower) || skillNameLower.Contains(skillEnLower))
                    {
                        ExecuteMatchedSkill(skill);
                        return;
                    }
                }

                // 한국어 스킬 이름으로 매칭
                if (!string.IsNullOrEmpty(skill.skillName) && skill.skillName.Contains(skillName))
                {
                    ExecuteMatchedSkill(skill);
                    return;
                }
            }

            // Debug.LogWarning($"[VoiceRecognition] 스킬을 찾을 수 없음: {skillName}");
        }

        /// <summary>
        /// 매칭된 스킬 실행 (피치 기반 또는 고정 속성 적용)
        /// </summary>
        private void ExecuteMatchedSkill(SkillData skill)
        {
            if (playerComponent == null || skill == null) return;

            // 일반 스킬인 경우 속성 적용
            if (skill.isGenericSkill && skill.elementVariants != null)
            {
                // 현재 속성 가져오기 (고정 모드면 고정 속성, 아니면 피치 기반)
                string element = GetCurrentElement();

                // 해당 속성의 스킬 변형이 있는지 확인
                if (skill.elementVariants.ContainsKey(element))
                {
                    var variant = skill.elementVariants[element];
                    string modeStr = useFixedElement ? "고정 속성" : "피치 기반";
                    Debug.Log($"[VoiceRecognition] {modeStr} 스킬 발동: {variant.name} (속성: {element})");

                    // 속성별 이펙트 프리팹으로 스킬 발동
                    playerComponent.CastSkillByDataWithVariant(skill, variant);
                    return;
                }
                else
                {
                    // 해당 속성에 변형이 없으면 기본 스킬 발동
                    Debug.Log($"[VoiceRecognition] 속성 {element}에 대한 변형 없음, 기본 스킬 발동: {skill.skillName}");
                }
            }

            // 일반 스킬이 아니거나 변형이 없으면 기본 스킬 발동
            playerComponent.CastSkillByData(skill);
        }

        /// <summary>
        /// 시스템 명령 실행
        /// </summary>
        private void ExecuteSystemCommand(string command)
        {
            // Debug.Log($"[VoiceRecognition] 시스템 명령 실행 시도: {command}");

            // 현재 게임 컨텍스트 확인
            GameContext currentContext = GetCurrentGameContext();
            // Debug.Log($"[VoiceRecognition] 현재 컨텍스트: {currentContext}");

            // 컨텍스트에서 허용되지 않는 명령인지 확인
            if (!IsCommandAllowedInContext(command, currentContext))
            {
                string hint = GetContextHintMessage(command, currentContext);
                // Debug.LogWarning($"[VoiceRecognition] 현재 컨텍스트({currentContext})에서 '{command}' 명령 불가: {hint}");
                UpdateVoiceRecognitionDisplay(hint);
                StartCoroutine(ClearVoiceRecognitionDisplayAfterDelay(3f));
                return;
            }

            // Debug.Log($"[VoiceRecognition] 명령 실행 허용됨: {command}");

            switch (command)
            {
                case "OpenSettings":
                    // 설정창 열기
                    var settingsScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                    if (settingsScene == "InGame")
                    {
                        // InGame에서는 내장 Options 팝업 표시
                        if (inGameUI != null && !inGameUI.IsOptionsPopupVisible())
                        {
                            Time.timeScale = 0f;
                            inGameUI.OpenOptionsPopup();
                        }
                    }
                    else if (settingsScene == "Menu")
                    {
                        // Menu에서는 Options 패널로 이동
                        var menuMgr = FindFirstObjectByType<LostSpells.UI.MenuManager>();
                        if (menuMgr != null)
                        {
                            menuMgr.ShowPanel(LostSpells.UI.MenuManager.MenuPanel.Options);
                        }
                    }
                    break;

                case "CloseSettings":
                    // 설정창 닫기
                    var closeSettingsScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                    if (closeSettingsScene == "InGame")
                    {
                        // InGame에서는 내장 Options 팝업 숨기기
                        if (inGameUI != null && inGameUI.IsOptionsPopupVisible())
                        {
                            inGameUI.CloseOptionsPopup();
                        }
                    }
                    else if (closeSettingsScene == "Menu")
                    {
                        // Menu에서는 이전 패널로 돌아가기
                        var closeSettingsMgr = FindFirstObjectByType<LostSpells.UI.MenuManager>();
                        if (closeSettingsMgr != null)
                        {
                            closeSettingsMgr.GoBack();
                        }
                    }
                    break;

                case "OpenMenu":
                case "PauseGame":
                    // 일시정지 및 메뉴 팝업 열기
                    if (inGameUI != null)
                    {
                        // InGameUI의 메뉴 팝업 열기
                        var root = inGameUI.GetComponent<UIDocument>()?.rootVisualElement;
                        var menuPopup = root?.Q<VisualElement>("MenuPopup");
                        if (menuPopup != null)
                        {
                            menuPopup.style.display = DisplayStyle.Flex;
                            // 사이드바 숨기기
                            var leftSidebar = root.Q<VisualElement>("LeftSidebar");
                            var rightSidebar = root.Q<VisualElement>("RightSidebar");
                            if (leftSidebar != null) leftSidebar.style.display = DisplayStyle.None;
                            if (rightSidebar != null) rightSidebar.style.display = DisplayStyle.None;
                        }
                    }
                    Time.timeScale = 0f;
                    // Debug.Log("[VoiceRecognition] 게임 일시정지됨");
                    break;

                case "CloseMenu":
                case "ResumeGame":
                    // 재개 및 메뉴 팝업 닫기
                    if (inGameUI != null)
                    {
                        var root = inGameUI.GetComponent<UIDocument>()?.rootVisualElement;
                        var menuPopup = root?.Q<VisualElement>("MenuPopup");
                        if (menuPopup != null)
                        {
                            menuPopup.style.display = DisplayStyle.None;
                            // 사이드바 다시 표시
                            var leftSidebar = root.Q<VisualElement>("LeftSidebar");
                            var rightSidebar = root.Q<VisualElement>("RightSidebar");
                            if (leftSidebar != null) leftSidebar.style.display = DisplayStyle.Flex;
                            if (rightSidebar != null) rightSidebar.style.display = DisplayStyle.Flex;
                        }
                    }
                    Time.timeScale = 1f;
                    // Debug.Log("[VoiceRecognition] 게임 재개됨");
                    break;

                case "RestartGame":
                    // 게임 재시작
                    Time.timeScale = 1f;
                    UnityEngine.SceneManagement.SceneManager.LoadScene(
                        UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                    break;

                case "QuitToMainMenu":
                    // 메뉴로 이동
                    Time.timeScale = 1f;
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
                    break;

                case "QuitGame":
                    // 게임 종료
                    #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                    #else
                    Application.Quit();
                    #endif
                    break;

                case "OpenInventory":
                    // Debug.Log("[VoiceRecognition] 인벤토리 열기 (미구현)");
                    break;

                case "CloseInventory":
                    // Debug.Log("[VoiceRecognition] 인벤토리 닫기 (미구현)");
                    break;

                case "OpenMap":
                    // Debug.Log("[VoiceRecognition] 지도 열기 (미구현)");
                    break;

                case "CloseMap":
                    // Debug.Log("[VoiceRecognition] 지도 닫기 (미구현)");
                    break;

                case "ShowHelp":
                    // Debug.Log("[VoiceRecognition] 도움말 표시 (미구현)");
                    break;

                // 메뉴 네비게이션 명령어
                case "StartGame":
                    // 게임 모드 선택 화면으로 이동
                    NavigateMenuPanel("GameModeSelection");
                    break;

                case "SelectStoryMode":
                    // 스토리 모드 선택
                    NavigateMenuPanel("StoryMode");
                    break;

                case "SelectEndlessMode":
                    // 무한 모드 선택
                    NavigateMenuPanel("EndlessMode");
                    break;

                case "StartEndless":
                    // 무한 모드 게임 시작
                    if (GameStateManager.Instance != null)
                    {
                        GameStateManager.Instance.StartEndlessMode();
                    }
                    UnityEngine.SceneManagement.SceneManager.LoadScene("InGame");
                    break;

                case "GoBack":
                    // 이전 화면으로
                    NavigateMenuBack();
                    break;

                case "GoToMainMenu":
                    // 메인 메뉴로 이동
                    var currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

                    // Options 씬이 Additive로 열려있으면 먼저 닫기
                    if (IsSceneLoaded("Options"))
                    {
                        UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync("Options");
                    }

                    // InGame의 OptionsPopup이 열려있으면 먼저 닫기
                    if (currentSceneName == "InGame" && inGameUI != null && inGameUI.IsOptionsPopupVisible())
                    {
                        inGameUI.CloseOptionsPopup();
                    }

                    if (currentSceneName == "Store")
                    {
                        // Store에서는 Menu 씬으로 이동
                        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
                    }
                    else if (currentSceneName == "InGame")
                    {
                        // InGame에서는 Menu 씬으로 이동
                        Time.timeScale = 1f;
                        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
                    }
                    else
                    {
                        // Menu 씬 내에서는 패널 전환
                        NavigateMenuPanel("MainMenu");
                    }
                    break;

                case "GoToGameModeSelection":
                    // 게임 모드 선택으로 이동
                    NavigateMenuPanel("GameModeSelection");
                    break;

                case "OpenStore":
                    // 상점 열기
                    var storeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                    if (storeScene == "InGame")
                    {
                        // InGame에서는 내장 Store 팝업 표시
                        if (inGameUI != null && !inGameUI.IsStorePopupVisible())
                        {
                            inGameUI.OpenStorePopup();
                        }
                    }
                    else if (storeScene == "Menu")
                    {
                        // Menu에서는 Store 패널로 이동
                        var storeMgr = FindFirstObjectByType<LostSpells.UI.MenuManager>();
                        if (storeMgr != null)
                        {
                            storeMgr.ShowPanel(LostSpells.UI.MenuManager.MenuPanel.Store);
                        }
                    }
                    break;

                // 튜토리얼 선택
                case "SelectTutorial":
                    // Debug.Log("[VoiceRecognition] 튜토리얼 선택 명령 감지");
                    StartChapter(0);
                    break;

                // 챕터 선택 (1-12)
                case "SelectChapter1":
                case "SelectChapter2":
                case "SelectChapter3":
                case "SelectChapter4":
                case "SelectChapter5":
                case "SelectChapter6":
                case "SelectChapter7":
                case "SelectChapter8":
                case "SelectChapter9":
                case "SelectChapter10":
                case "SelectChapter11":
                case "SelectChapter12":
                    // Debug.Log($"[VoiceRecognition] 챕터 선택 명령 감지: {command}");
                    // 챕터 번호 추출 (SelectChapter1 -> 1)
                    string chapterNumStr = command.Substring("SelectChapter".Length);
                    // Debug.Log($"[VoiceRecognition] 챕터 번호 문자열: {chapterNumStr}");
                    int chapterId = int.Parse(chapterNumStr);
                    // Debug.Log($"[VoiceRecognition] 파싱된 챕터 ID: {chapterId}");
                    StartChapter(chapterId);
                    break;

                // Options 탭 전환 명령어
                case "ShowAudioTab":
                    ExecuteOptionsTabCommand("Audio");
                    break;

                case "ShowGraphicsTab":
                    ExecuteOptionsTabCommand("Graphics");
                    break;

                case "ShowLanguageTab":
                    ExecuteOptionsTabCommand("Language");
                    break;

                case "ShowGameTab":
                    ExecuteOptionsTabCommand("Game");
                    break;

                // Options 게임 탭 섹션 명령어
                case "ExpandVoiceRecognition":
                    ExecuteOptionsSectionCommand("ExpandVoiceRecognition");
                    break;

                case "CollapseVoiceRecognition":
                    ExecuteOptionsSectionCommand("CollapseVoiceRecognition");
                    break;

                case "ExpandKeyBinding":
                    ExecuteOptionsSectionCommand("ExpandKeyBinding");
                    break;

                case "CollapseKeyBinding":
                    ExecuteOptionsSectionCommand("CollapseKeyBinding");
                    break;

                // InGame 이동 명령어
                case "MoveLeft":
                    ExecuteMovementCommand("Left");
                    break;

                case "MoveRight":
                    ExecuteMovementCommand("Right");
                    break;

                case "Jump":
                    ExecuteMovementCommand("Jump");
                    break;

                case "StopMove":
                    ExecuteMovementCommand("Stop");
                    break;

                default:
                    // Debug.LogWarning($"[VoiceRecognition] 알 수 없는 시스템 명령: {command}");
                    break;
            }
        }

        /// <summary>
        /// Menu 씬의 패널 네비게이션
        /// </summary>
        private void NavigateMenuPanel(string panelName)
        {
            // Debug.Log($"[VoiceRecognition] NavigateMenuPanel 호출됨: {panelName}");

            // 현재 씬 확인
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            // Debug.Log($"[VoiceRecognition] 현재 씬: {currentScene}");

            if (currentScene != "Menu")
            {
                // Debug.LogWarning($"[VoiceRecognition] Menu 씬이 아닙니다. 현재: {currentScene}");
                return;
            }

            var menuManager = FindFirstObjectByType<LostSpells.UI.MenuManager>();
            if (menuManager != null)
            {
                // Debug.Log($"[VoiceRecognition] MenuManager 찾음, 패널 전환 시도: {panelName}");
                switch (panelName)
                {
                    case "GameModeSelection":
                        menuManager.ShowPanel(LostSpells.UI.MenuManager.MenuPanel.GameModeSelection);
                        break;
                    case "StoryMode":
                        menuManager.ShowPanel(LostSpells.UI.MenuManager.MenuPanel.StoryMode);
                        break;
                    case "EndlessMode":
                        menuManager.ShowPanel(LostSpells.UI.MenuManager.MenuPanel.EndlessMode);
                        break;
                    case "MainMenu":
                        menuManager.ShowPanel(LostSpells.UI.MenuManager.MenuPanel.MainMenu);
                        break;
                }
                // Debug.Log($"[VoiceRecognition] 메뉴 패널 이동 완료: {panelName}");
            }
            // MenuManager를 찾을 수 없는 경우 무시
        }

        /// <summary>
        /// 챕터 시작
        /// </summary>
        private void StartChapter(int chapterId)
        {
            // Debug.Log($"[VoiceRecognition] StartChapter 호출됨: 챕터 {chapterId}");

            // GameStateManager를 통해 챕터 시작
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.StartChapter(chapterId);
                // Debug.Log($"[VoiceRecognition] GameStateManager.StartChapter({chapterId}) 호출 완료");
            }
            else
            {
                Debug.LogError("[VoiceRecognition] GameStateManager.Instance가 null입니다!");
            }

            // InGame 씬으로 이동
            // Debug.Log("[VoiceRecognition] InGame 씬으로 이동 시도...");
            UnityEngine.SceneManagement.SceneManager.LoadScene("InGame");
        }

        /// <summary>
        /// Menu 씬에서 이전 화면으로 돌아가기
        /// </summary>
        private void NavigateMenuBack()
        {
            // Options 씬이 열려있으면 닫기
            if (IsSceneLoaded("Options"))
            {
                UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync("Options");
                // InGame에서 메뉴 팝업이 열려있으면 일시정지 유지
                if (!IsInGameMenuPopupVisible())
                {
                    Time.timeScale = 1f;
                }
                return;
            }

            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            // InGame에서 OptionsPopup이 열려있으면 닫기
            if (currentScene == "InGame" && inGameUI != null)
            {
                if (inGameUI.IsOptionsPopupVisible())
                {
                    inGameUI.CloseOptionsPopup();
                    return;
                }
            }

            // Store 씬이 열려있으면 Menu로 이동
            if (currentScene == "Store")
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
                return;
            }

            // Menu 씬에서는 MenuManager의 GoBack 사용
            var menuManager = FindFirstObjectByType<LostSpells.UI.MenuManager>();
            if (menuManager != null)
            {
                menuManager.GoBack();
                // Debug.Log("[VoiceRecognition] 이전 메뉴로 돌아가기");
            }
        }

        /// <summary>
        /// Options 탭 전환 명령 실행
        /// </summary>
        private void ExecuteOptionsTabCommand(string tabName)
        {
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            if (currentScene == "Menu")
            {
                var menuManager = FindFirstObjectByType<LostSpells.UI.MenuManager>();
                var controller = menuManager?.GetOptionsPanelController();
                if (controller != null)
                {
                    switch (tabName)
                    {
                        case "Audio": controller.ShowAudioTab(); break;
                        case "Graphics": controller.ShowGraphicsTab(); break;
                        case "Language": controller.ShowLanguageTab(); break;
                        case "Game": controller.ShowGameTab(); break;
                    }
                }
            }
            else if (currentScene == "InGame" && inGameUI != null)
            {
                var controller = inGameUI.GetOptionsPanelController();
                if (controller != null)
                {
                    switch (tabName)
                    {
                        case "Audio": controller.ShowAudioTab(); break;
                        case "Graphics": controller.ShowGraphicsTab(); break;
                        case "Language": controller.ShowLanguageTab(); break;
                        case "Game": controller.ShowGameTab(); break;
                    }
                }
            }
        }

        /// <summary>
        /// Options 게임 탭 섹션 명령 실행
        /// </summary>
        private void ExecuteOptionsSectionCommand(string sectionCommand)
        {
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            if (currentScene == "Menu")
            {
                var menuManager = FindFirstObjectByType<LostSpells.UI.MenuManager>();
                var controller = menuManager?.GetOptionsPanelController();
                if (controller != null)
                {
                    switch (sectionCommand)
                    {
                        case "ExpandVoiceRecognition": controller.ExpandVoiceRecognitionSection(); break;
                        case "CollapseVoiceRecognition": controller.CollapseVoiceRecognitionSection(); break;
                        case "ExpandKeyBinding": controller.ExpandKeyBindingSection(); break;
                        case "CollapseKeyBinding": controller.CollapseKeyBindingSection(); break;
                    }
                }
            }
            else if (currentScene == "InGame" && inGameUI != null)
            {
                var controller = inGameUI.GetOptionsPanelController();
                if (controller != null)
                {
                    switch (sectionCommand)
                    {
                        case "ExpandVoiceRecognition": controller.ExpandVoiceRecognitionSection(); break;
                        case "CollapseVoiceRecognition": controller.CollapseVoiceRecognitionSection(); break;
                        case "ExpandKeyBinding": controller.ExpandKeyBindingSection(); break;
                        case "CollapseKeyBinding": controller.CollapseKeyBindingSection(); break;
                    }
                }
            }
        }

        /// <summary>
        /// 이동 명령 실행
        /// </summary>
        private void ExecuteMovementCommand(string direction)
        {
            if (playerComponent == null) return;

            switch (direction)
            {
                case "Left":
                    playerComponent.SetVoiceMovement(-1);
                    break;
                case "Right":
                    playerComponent.SetVoiceMovement(1);
                    break;
                case "Jump":
                    playerComponent.VoiceJump();
                    break;
                case "Stop":
                    playerComponent.SetVoiceMovement(0);
                    break;
            }
        }

        /// <summary>
        /// 특정 씬이 로드되어 있는지 확인
        /// </summary>
        private bool IsSceneLoaded(string sceneName)
        {
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (scene.name == sceneName && scene.isLoaded)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// InGame에서 메뉴 팝업이 열려있는지 확인
        /// </summary>
        private bool IsInGameMenuPopupVisible()
        {
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentScene != "InGame") return false;

            if (inGameUI != null)
            {
                var root = inGameUI.GetComponent<UIDocument>()?.rootVisualElement;
                var menuPopup = root?.Q<VisualElement>("MenuPopup");
                if (menuPopup != null && menuPopup.style.display == DisplayStyle.Flex)
                {
                    return true;
                }
            }
            return false;
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
            }
        }

        #region Voice Input Mode

        /// <summary>
        /// 음성 입력 모드 설정
        /// </summary>
        public void SetVoiceInputMode(VoiceInputMode mode)
        {
            // 현재 녹음 중이면 상태 리셋
            if (isRecording)
            {
                isRecording = false;
                // Debug.Log("[VoiceRecognition] 모드 변경으로 녹음 상태 리셋");
            }

            inputMode = mode;

            // VoiceRecorder의 연속 모드 설정 및 상태 리셋
            if (voiceRecorder != null)
            {
                voiceRecorder.ResetRecordingState();
                voiceRecorder.enableContinuousMode = (mode == VoiceInputMode.Continuous);
            }

            // Debug.Log($"[VoiceRecognition] 음성 입력 모드 변경: {mode}");
        }

        /// <summary>
        /// 현재 음성 입력 모드 반환
        /// </summary>
        public VoiceInputMode GetVoiceInputMode()
        {
            return inputMode;
        }

        /// <summary>
        /// 연속 모드: 음성 감지됨 이벤트 핸들러
        /// </summary>
        private void OnContinuousModeVoiceDetected()
        {
            if (inputMode != VoiceInputMode.Continuous) return;

            isRecording = true;
            recordingStartTime = Time.unscaledTime;

            // 음성인식 파티클 표시
            if (playerComponent != null)
            {
                playerComponent.ShowVoiceRecognitionParticle();
            }

            // 인게임에서는 "듣는 중" 메시지 표시하지 않음
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentScene != "InGame")
            {
                string listeningText = LocalizationManager.Instance.GetText("voice_listening");
                if (string.IsNullOrEmpty(listeningText)) listeningText = "듣는 중...";
                UpdateVoiceRecognitionDisplay(listeningText);
            }
        }

        /// <summary>
        /// 연속 모드: 녹음 완료 이벤트 핸들러
        /// </summary>
        private void OnContinuousModeRecordingComplete(AudioClip recordedClip)
        {
            if (inputMode != VoiceInputMode.Continuous) return;

            isRecording = false;

            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            // 인게임에서는 "처리 중" 메시지 표시하지 않음
            if (currentScene != "InGame")
            {
                string processingText = LocalizationManager.Instance.GetText("voice_processing");
                if (string.IsNullOrEmpty(processingText)) processingText = "처리 중...";
                UpdateVoiceRecognitionDisplay(processingText);
            }

            // 화면 하단 상태 패널에 "인식중..." 표시
            UpdateVoiceStatusPanelUI("", "인식중");

            // 피치 분석 (인게임에서만 활성화)
            if (enablePitchAnalysis && currentScene == "InGame" && pitchAnalyzer != null && recordedClip != null)
            {
                var pitchResult = pitchAnalyzer.AnalyzeClip(recordedClip);
                // 피치 결과는 PitchAnalyzer에서 콘솔에 출력됨
            }

            // 서버로 전송
            StartCoroutine(SendAudioToServer());
        }

        #endregion

        #region Context-Aware Command Filtering

        /// <summary>
        /// 현재 게임 컨텍스트 감지
        /// </summary>
        private GameContext GetCurrentGameContext()
        {
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            // Options 씬이 Additive로 로드되어 있는지 확인
            if (IsSceneLoaded("Options"))
            {
                return GameContext.Options;
            }

            // Store 씬 확인
            if (currentScene == "Store")
            {
                return GameContext.Store;
            }

            // Menu 씬 확인
            if (currentScene == "Menu")
            {
                var menuManager = FindFirstObjectByType<LostSpells.UI.MenuManager>();
                if (menuManager != null)
                {
                    // MenuManager의 currentPanel 필드에 접근 (public으로 변경하거나 메서드 추가 필요)
                    // 현재는 reflection 없이 간단히 처리
                    return GetMenuPanelContext(menuManager);
                }
                return GameContext.Menu_MainMenu;
            }

            // InGame 씬 확인
            if (currentScene == "InGame")
            {
                // 일시정지 상태 확인 (Time.timeScale == 0)
                if (Time.timeScale == 0f)
                {
                    if (inGameUI != null)
                    {
                        var root = inGameUI.GetComponent<UIDocument>()?.rootVisualElement;

                        // OptionsPopup이 열려있는지 확인
                        var optionsPopup = root?.Q<VisualElement>("OptionsPopup");
                        if (optionsPopup != null && optionsPopup.style.display == DisplayStyle.Flex)
                        {
                            return GameContext.Options;
                        }

                        // 게임오버인지 확인
                        var gameOverPopup = root?.Q<VisualElement>("GameOverPopup");
                        if (gameOverPopup != null && gameOverPopup.style.display == DisplayStyle.Flex)
                        {
                            return GameContext.InGame_GameOver;
                        }
                    }
                    return GameContext.InGame_Paused;
                }
                return GameContext.InGame_Playing;
            }

            return GameContext.Unknown;
        }

        /// <summary>
        /// MenuManager의 현재 패널 컨텍스트 가져오기
        /// </summary>
        private GameContext GetMenuPanelContext(LostSpells.UI.MenuManager menuManager)
        {
            // MenuManager에 GetCurrentPanel() 메서드가 없으므로
            // 현재 활성화된 UI 요소로 판단
            var root = FindFirstObjectByType<UIDocument>()?.rootVisualElement;
            if (root == null) return GameContext.Menu_MainMenu;

            var mainMenuPanel = root.Q<VisualElement>("MainMenuPanel");
            var gameModePanel = root.Q<VisualElement>("GameModeSelectionPanel");
            var storyModePanel = root.Q<VisualElement>("StoryModePanel");
            var endlessModePanel = root.Q<VisualElement>("EndlessModePanel");
            var optionsPanel = root.Q<VisualElement>("OptionsPanel");
            var storePanel = root.Q<VisualElement>("StorePanel");

            // Options/Store 패널 먼저 확인 (오버레이 패널)
            if (optionsPanel != null && optionsPanel.style.display == DisplayStyle.Flex)
                return GameContext.Options;
            if (storePanel != null && storePanel.style.display == DisplayStyle.Flex)
                return GameContext.Store;

            if (storyModePanel != null && storyModePanel.style.display == DisplayStyle.Flex)
                return GameContext.Menu_StoryMode;
            if (endlessModePanel != null && endlessModePanel.style.display == DisplayStyle.Flex)
                return GameContext.Menu_EndlessMode;
            if (gameModePanel != null && gameModePanel.style.display == DisplayStyle.Flex)
                return GameContext.Menu_GameModeSelection;

            return GameContext.Menu_MainMenu;
        }

        /// <summary>
        /// 현재 컨텍스트에서 명령어가 허용되는지 확인
        /// </summary>
        private bool IsCommandAllowedInContext(string command, GameContext context)
        {
            switch (context)
            {
                case GameContext.Menu_MainMenu:
                    // 메인 메뉴: 게임 시작, 설정, 상점, 종료
                    return command == "StartGame" || command == "OpenSettings" || command == "OpenStore" || command == "QuitGame";

                case GameContext.Menu_GameModeSelection:
                    // 게임 모드 선택: 스토리/무한 모드 선택, 뒤로, 메인메뉴로, 설정
                    return command == "SelectStoryMode" || command == "SelectEndlessMode" ||
                           command == "GoBack" || command == "GoToMainMenu" || command == "OpenSettings";

                case GameContext.Menu_StoryMode:
                    // 스토리 모드: 챕터 선택, 뒤로, 메인메뉴로, 게임모드선택으로, 설정
                    return command == "SelectTutorial" ||
                           command.StartsWith("SelectChapter") ||
                           command == "GoBack" || command == "GoToMainMenu" ||
                           command == "GoToGameModeSelection" || command == "OpenSettings";

                case GameContext.Menu_EndlessMode:
                    // 무한 모드: 시작, 뒤로, 메인메뉴로, 게임모드선택으로, 설정
                    return command == "StartEndless" || command == "GoBack" ||
                           command == "GoToMainMenu" || command == "GoToGameModeSelection" ||
                           command == "OpenSettings";

                case GameContext.InGame_Playing:
                    // 인게임 플레이 중: 일시정지/메뉴 열기, 이동 명령 가능
                    return command == "PauseGame" || command == "OpenMenu" ||
                           command == "MoveLeft" || command == "MoveRight" ||
                           command == "Jump" || command == "StopMove";

                case GameContext.InGame_Paused:
                    // 인게임 일시정지: 재개, 설정, 상점, 재시작, 메인메뉴
                    return command == "ResumeGame" || command == "CloseMenu" ||
                           command == "OpenSettings" || command == "OpenStore" ||
                           command == "RestartGame" || command == "QuitToMainMenu";

                case GameContext.InGame_GameOver:
                    // 게임오버: 재시작, 메인메뉴
                    return command == "RestartGame" || command == "QuitToMainMenu";

                case GameContext.Options:
                    // 설정창: 닫기/뒤로, 메인메뉴로, 탭 전환, 섹션 펼치기/접기 가능
                    return command == "CloseSettings" || command == "GoBack" || command == "GoToMainMenu" ||
                           command == "ShowAudioTab" || command == "ShowGraphicsTab" ||
                           command == "ShowLanguageTab" || command == "ShowGameTab" ||
                           command == "ExpandVoiceRecognition" || command == "CollapseVoiceRecognition" ||
                           command == "ExpandKeyBinding" || command == "CollapseKeyBinding";

                case GameContext.Store:
                    // 상점: 뒤로, 메인메뉴로
                    return command == "GoBack" || command == "GoToMainMenu";

                default:
                    return false;
            }
        }

        /// <summary>
        /// 현재 컨텍스트에 맞는 Whisper 키워드 반환
        /// </summary>
        public string GetContextKeywords()
        {
            GameContext context = GetCurrentGameContext();
            return GetKeywordsForContext(context);
        }

        /// <summary>
        /// 특정 컨텍스트에 대한 Whisper 키워드 반환
        /// </summary>
        private string GetKeywordsForContext(GameContext context)
        {
            switch (context)
            {
                case GameContext.Menu_MainMenu:
                    return "시작, 설정, 상점, 종료";

                case GameContext.Menu_GameModeSelection:
                    return "스토리, 무한, 뒤로, 설정";

                case GameContext.Menu_StoryMode:
                    return "튜토리얼, 챕터, 뒤로, 설정";

                case GameContext.Menu_EndlessMode:
                    return "시작, 뒤로, 설정";

                case GameContext.InGame_Playing:
                    return "일시정지, 왼쪽, 오른쪽, 점프, 정지, 멈춰";

                case GameContext.InGame_Paused:
                    return "계속, 설정, 상점, 재시작, 메인";

                case GameContext.InGame_GameOver:
                    return "재시작, 메인";

                case GameContext.Options:
                    return "뒤로, 닫기, 메인, 오디오, 그래픽, 언어, 게임, 음성인식, 키설정, 펼쳐, 접어";

                case GameContext.Store:
                    return "뒤로, 메인";

                default:
                    // 알 수 없는 컨텍스트면 기본 키워드 반환
                    return "시작, 설정, 상점, 뒤로, 메인";
            }
        }

        /// <summary>
        /// 현재 게임 컨텍스트 문자열로 반환 (서버 전송용)
        /// </summary>
        public string GetCurrentContextString()
        {
            GameContext context = GetCurrentGameContext();
            return context.ToString();
        }

        /// <summary>
        /// 컨텍스트에 맞지 않는 명령어에 대한 안내 메시지
        /// </summary>
        private string GetContextHintMessage(string command, GameContext context)
        {
            switch (context)
            {
                case GameContext.InGame_Playing:
                    if (command == "OpenSettings" || command == "RestartGame" || command == "QuitToMainMenu")
                        return "먼저 '일시정지' 또는 '메뉴'를 말해주세요";
                    break;

                case GameContext.Menu_MainMenu:
                    if (command == "SelectStoryMode" || command == "SelectEndlessMode")
                        return "먼저 '게임 시작'을 말해주세요";
                    if (command.StartsWith("SelectChapter") || command == "SelectTutorial")
                        return "먼저 '게임 시작' 후 '스토리 모드'를 선택해주세요";
                    break;

                case GameContext.Menu_GameModeSelection:
                    if (command.StartsWith("SelectChapter") || command == "SelectTutorial")
                        return "먼저 '스토리 모드'를 선택해주세요";
                    break;
            }

            return "현재 화면에서 사용할 수 없는 명령입니다";
        }

        #endregion
    }
}
