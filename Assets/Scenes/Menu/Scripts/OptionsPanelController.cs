using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using LostSpells.Data;
using LostSpells.Systems;

namespace LostSpells.UI
{
    /// <summary>
    /// Options 패널 컨트롤러 - MenuManager에서 사용
    /// </summary>
    public class OptionsPanelController
    {
        private VisualElement root;
        private VisualElement optionsPanel;
        private MenuManager menuManager;

        // SaveManager 참조
        private SaveManager saveManager;
        private PlayerSaveData saveData;

        // 통합 패널 헤더
        private Label currentPanelTitle;
        private Button currentPanelResetButton;

        // 카테고리 버튼들
        private Button audioButton;
        private Button graphicsButton;
        private Button languageButton;
        private Button gameButton;

        // 패널들
        private VisualElement audioPanel;
        private VisualElement graphicsPanel;
        private VisualElement languagePanel;
        private VisualElement gamePanel;

        // 현재 활성 패널 추적
        private VisualElement currentPanel;

        // Audio 패널 컨트롤
        private CustomDropdown microphoneDropdown;

        // Graphics 패널 컨트롤
        private CustomDropdown qualityDropdown;
        private CustomDropdown screenModeDropdown;

        // Language 패널 컨트롤
        private CustomDropdown uiLanguageDropdown;

        // Voice Recognition 서버 모드 컨트롤
        private CustomDropdown serverModeDropdown;

        // Voice Input Mode 컨트롤
        private CustomDropdown voiceInputModeDropdown;

        // Pitch Element 드롭다운 컨트롤
        private CustomDropdown lowPitchElementDropdown;
        private CustomDropdown midPitchElementDropdown;
        private CustomDropdown highPitchElementDropdown;

        // 사용 가능한 속성 목록
        private static readonly List<string> availableElements = new List<string>
        {
            "Fire", "Ice", "Electric", "Earth", "Holy", "Void"
        };

        // Game 패널 컨트롤
        private CollapsibleSection keyBindingSection;
        private CollapsibleSection voiceRecognitionSection;
        private Label serverStatusLabel;
        private Dictionary<string, Button> keyButtons = new Dictionary<string, Button>();
        private Button resetGameButton;

        // 게임 초기화 확인 팝업
        private VisualElement gameResetConfirmation;
        private Button confirmResetButton;
        private Button cancelResetButton;

        // 피치 테스트 UI
        private VisualElement pitchGaugeBar;
        private VisualElement pitchGaugeLowArea;
        private VisualElement pitchGaugeMediumArea;
        private VisualElement pitchGaugeHighArea;
        private VisualElement pitchMinMarker;
        private VisualElement pitchMaxMarker;
        private VisualElement pitchRealtimeIndicator;
        private Label currentFrequencyLabel;
        private Label minFrequencyValue;
        private Label maxFrequencyValue;
        private Button pitchTestButton;
        private Label pitchLowLabel;
        private Label pitchMediumLabel;
        private Label pitchHighLabel;
        private Label pitchTestResultLabel;

        // 피치 테스트 상태
        private bool isRealtimeMonitoring = false;  // 실시간 피치 모니터링 (항상)
        private bool isTestMode = false;            // 테스트 모드 (종료 버튼 누를때까지 유지)
        private float currentPitchFrequency = 0f;
        private float currentMinFrequency = 130.81f; // C3
        private float currentMaxFrequency = 261.63f; // C4
        private AudioClip realtimeClip;             // 실시간 모니터링 + 녹음용
        private PitchAnalyzer pitchAnalyzer;
        private string micDevice = null;

        // VAD (Voice Activity Detection) 상태
        private bool isVoiceActive = false;         // 현재 음성 감지 중
        private float silenceTimer = 0f;            // 무음 지속 시간
        private int voiceStartPosition = 0;         // 음성 시작 위치
        private List<float> recordingBuffer = new List<float>(); // 녹음 버퍼
        private const float VOICE_THRESHOLD = 0.02f;   // 음성 감지 임계값
        private const float SILENCE_TIMEOUT = 1.0f;    // 무음 지속 시 녹음 종료
        private const float MIN_RECORDING_LENGTH = 0.5f; // 최소 녹음 길이 (초)

        // 키 트리거 모드 상태
        private bool isKeyRecording = false;        // 키를 누르고 있는 동안 녹음 중

        // 결과 표시 타이머
        private float resultDisplayTime = 0f;
        private const float RESULT_DISPLAY_DURATION = 5f; // 5초간 결과 유지

        // 게이지 범위 상수 (고정값으로 사용)
        private const float GAUGE_MIN_FREQ = 50f;   // 게이지 최소 주파수
        private const float GAUGE_MAX_FREQ = 1000f; // 게이지 최대 주파수

        // 마커 드래그 상태
        private bool isDraggingMinMarker = false;
        private bool isDraggingMaxMarker = false;

        // 서버 체크
        private const string SERVER_URL = "http://localhost:8000";
        private MonoBehaviour coroutineRunner;
        private Coroutine serverStatusCheckCoroutine;

        // 키 바인딩 상태
        private bool isWaitingForKey = false;
        private string currentKeyAction = "";

        // 해상도 옵션 (너비x높이)
        private static readonly (int width, int height, string name)[] resolutionOptions = new[]
        {
            (1280, 720, "1280x720 (HD)"),
            (1600, 900, "1600x900"),
            (1920, 1080, "1920x1080 (FHD)"),
            (2560, 1440, "2560x1440 (QHD)")
        };

        public OptionsPanelController(VisualElement root, VisualElement optionsPanel, MenuManager menuManager)
        {
            this.root = root;
            this.optionsPanel = optionsPanel;
            this.menuManager = menuManager;
            this.coroutineRunner = menuManager;

            // SaveManager 싱글톤 인스턴스 가져오기
            saveManager = SaveManager.Instance;

            // UI 요소 찾기
            FindUIElements();

            // 이벤트 등록
            RegisterEvents();

            // SaveData 가져오기
            LoadSaveData();

            // 초기 설정 로드
            LoadSettings();
        }

        private void LoadSaveData()
        {
            if (saveManager != null)
            {
                saveData = saveManager.GetCurrentSaveData();
            }
        }

        private void FindUIElements()
        {
            // OptionsPanel 내에서 요소 찾기
            if (optionsPanel == null) return;

            // 통합 패널 헤더
            currentPanelTitle = optionsPanel.Q<Label>("CurrentPanelTitle");
            currentPanelResetButton = optionsPanel.Q<Button>("CurrentPanelResetButton");

            // 카테고리 버튼들
            audioButton = optionsPanel.Q<Button>("AudioButton");
            graphicsButton = optionsPanel.Q<Button>("GraphicsButton");
            languageButton = optionsPanel.Q<Button>("LanguageButton");
            gameButton = optionsPanel.Q<Button>("GameButton");

            // 패널들
            audioPanel = optionsPanel.Q<VisualElement>("AudioPanel");
            graphicsPanel = optionsPanel.Q<VisualElement>("GraphicsPanel");
            languagePanel = optionsPanel.Q<VisualElement>("LanguagePanel");
            gamePanel = optionsPanel.Q<VisualElement>("GamePanel");

            // Audio 패널 컨트롤
            microphoneDropdown = new CustomDropdown(optionsPanel, "MicrophoneDropdownContainer", "MicrophoneDropdownButton", "MicrophoneDropdownLabel", "MicrophoneDropdownList");

            // Graphics 패널 컨트롤
            qualityDropdown = new CustomDropdown(optionsPanel, "QualityDropdownContainer", "QualityDropdownButton", "QualityDropdownLabel", "QualityDropdownList");
            screenModeDropdown = new CustomDropdown(optionsPanel, "ScreenModeDropdownContainer", "ScreenModeDropdownButton", "ScreenModeDropdownLabel", "ScreenModeDropdownList");

            // Language 패널 컨트롤
            uiLanguageDropdown = new CustomDropdown(optionsPanel, "UILanguageDropdownContainer", "UILanguageDropdownButton", "UILanguageDropdownLabel", "UILanguageDropdownList");

            // Voice Recognition 서버 모드 컨트롤
            serverModeDropdown = new CustomDropdown(optionsPanel, "ServerModeDropdownContainer", "ServerModeDropdownButton", "ServerModeDropdownLabel", "ServerModeDropdownList");

            // Voice Input Mode 컨트롤
            voiceInputModeDropdown = new CustomDropdown(optionsPanel, "VoiceInputModeDropdownContainer", "VoiceInputModeDropdownButton", "VoiceInputModeDropdownLabel", "VoiceInputModeDropdownList");

            // Pitch Element 드롭다운 컨트롤
            lowPitchElementDropdown = new CustomDropdown(optionsPanel, "LowPitchElementDropdownContainer", "LowPitchElementDropdownButton", "LowPitchElementDropdownLabel", "LowPitchElementDropdownList");
            midPitchElementDropdown = new CustomDropdown(optionsPanel, "MidPitchElementDropdownContainer", "MidPitchElementDropdownButton", "MidPitchElementDropdownLabel", "MidPitchElementDropdownList");
            highPitchElementDropdown = new CustomDropdown(optionsPanel, "HighPitchElementDropdownContainer", "HighPitchElementDropdownButton", "HighPitchElementDropdownLabel", "HighPitchElementDropdownList");

            // Game 패널 컨트롤
            keyBindingSection = new CollapsibleSection(optionsPanel, "KeyBindingsHeader", "KeyBindingsToggleButton", "KeyBindingArea");
            voiceRecognitionSection = new CollapsibleSection(optionsPanel, "VoiceRecognitionHeader", "VoiceRecognitionToggleButton", "VoiceRecognitionArea");
            serverStatusLabel = optionsPanel.Q<Label>("ServerStatusLabel");

            // 키 버튼들
            keyButtons["MoveLeft"] = optionsPanel.Q<Button>("MoveLeftKey");
            keyButtons["MoveRight"] = optionsPanel.Q<Button>("MoveRightKey");
            keyButtons["Jump"] = optionsPanel.Q<Button>("JumpKey");
            keyButtons["VoiceRecord"] = optionsPanel.Q<Button>("VoiceRecordKey");
            keyButtons["SkillPanel"] = optionsPanel.Q<Button>("SkillPanelKey");

            // 게임 초기화 버튼
            resetGameButton = optionsPanel.Q<Button>("ResetGameButton");

            // 게임 초기화 확인 팝업
            gameResetConfirmation = optionsPanel.Q<VisualElement>("GameResetConfirmation");
            confirmResetButton = optionsPanel.Q<Button>("ConfirmResetButton");
            cancelResetButton = optionsPanel.Q<Button>("CancelResetButton");

            // 피치 테스트 UI
            pitchGaugeBar = optionsPanel.Q<VisualElement>("PitchGaugeBar");
            pitchGaugeLowArea = optionsPanel.Q<VisualElement>("PitchGaugeLowArea");
            pitchGaugeMediumArea = optionsPanel.Q<VisualElement>("PitchGaugeMediumArea");
            pitchGaugeHighArea = optionsPanel.Q<VisualElement>("PitchGaugeHighArea");
            pitchMinMarker = optionsPanel.Q<VisualElement>("PitchMinMarker");
            pitchMaxMarker = optionsPanel.Q<VisualElement>("PitchMaxMarker");
            pitchRealtimeIndicator = optionsPanel.Q<VisualElement>("PitchRealtimeIndicator");

            // 마커가 포인터 이벤트를 받을 수 있도록 설정
            if (pitchMinMarker != null)
            {
                pitchMinMarker.pickingMode = PickingMode.Position;
            }
            if (pitchMaxMarker != null)
            {
                pitchMaxMarker.pickingMode = PickingMode.Position;
            }

            // 영역과 실시간 인디케이터는 마커 클릭을 방해하지 않도록 설정
            if (pitchGaugeLowArea != null)
                pitchGaugeLowArea.pickingMode = PickingMode.Ignore;
            if (pitchGaugeMediumArea != null)
                pitchGaugeMediumArea.pickingMode = PickingMode.Ignore;
            if (pitchGaugeHighArea != null)
                pitchGaugeHighArea.pickingMode = PickingMode.Ignore;
            if (pitchRealtimeIndicator != null)
                pitchRealtimeIndicator.pickingMode = PickingMode.Ignore;
            currentFrequencyLabel = optionsPanel.Q<Label>("CurrentFrequencyLabel");
            minFrequencyValue = optionsPanel.Q<Label>("MinFrequencyValue");
            maxFrequencyValue = optionsPanel.Q<Label>("MaxFrequencyValue");
            pitchTestButton = optionsPanel.Q<Button>("PitchTestButton");
            pitchLowLabel = optionsPanel.Q<Label>("PitchLowLabel");
            pitchMediumLabel = optionsPanel.Q<Label>("PitchMediumLabel");
            pitchHighLabel = optionsPanel.Q<Label>("PitchHighLabel");
            pitchTestResultLabel = optionsPanel.Q<Label>("PitchTestResultLabel");
        }

        private void RegisterEvents()
        {
            if (audioButton != null)
                audioButton.clicked += () => ShowPanel(audioPanel);

            if (graphicsButton != null)
                graphicsButton.clicked += () => ShowPanel(graphicsPanel);

            if (languageButton != null)
                languageButton.clicked += () => ShowPanel(languagePanel);

            if (gameButton != null)
                gameButton.clicked += () => ShowPanel(gamePanel);

            // 통합 리셋 버튼 이벤트
            if (currentPanelResetButton != null)
                currentPanelResetButton.clicked += OnCurrentPanelReset;

            // 키 버튼 이벤트
            foreach (var kvp in keyButtons)
            {
                if (kvp.Value != null)
                {
                    string action = kvp.Key;
                    kvp.Value.clicked += () => OnKeyButtonClicked(action);
                }
            }

            // 게임 초기화 버튼 이벤트
            if (resetGameButton != null)
                resetGameButton.clicked += OnResetGameButtonClicked;

            // 게임 초기화 확인 팝업 버튼 이벤트
            if (confirmResetButton != null)
                confirmResetButton.clicked += OnConfirmResetButtonClicked;

            if (cancelResetButton != null)
                cancelResetButton.clicked += OnCancelResetButtonClicked;

            // 피치 테스트 이벤트
            if (pitchTestButton != null)
                pitchTestButton.clicked += OnPitchTestButtonClicked;

            // 마커 드래그 이벤트 - 마커 자체에 모든 이벤트 등록
            if (pitchMinMarker != null)
            {
                pitchMinMarker.RegisterCallback<PointerDownEvent>(OnMinMarkerPointerDown);
                pitchMinMarker.RegisterCallback<PointerMoveEvent>(OnMarkerPointerMove);
                pitchMinMarker.RegisterCallback<PointerUpEvent>(OnMarkerPointerUp);
            }
            if (pitchMaxMarker != null)
            {
                pitchMaxMarker.RegisterCallback<PointerDownEvent>(OnMaxMarkerPointerDown);
                pitchMaxMarker.RegisterCallback<PointerMoveEvent>(OnMarkerPointerMove);
                pitchMaxMarker.RegisterCallback<PointerUpEvent>(OnMarkerPointerUp);
            }
        }

        private void LoadSettings()
        {
            if (saveData == null) return;

            // Audio 설정 로드
            LoadAudioSettings();

            // Graphics 설정 로드
            LoadGraphicsSettings();

            // Language 설정 로드
            LoadLanguageSettings();

            // Voice Recognition 설정 로드
            LoadVoiceSettings();

            // Key Binding 설정 로드
            LoadKeyBindings();

            // Pitch 설정 로드
            LoadPitchSettings();

            // Pitch Element 설정 로드
            LoadPitchElementSettings();
        }

        private void LoadAudioSettings()
        {
            // 마이크 목록
            if (microphoneDropdown != null)
            {
                List<string> microphones = new List<string> { "Default" };
                foreach (var device in Microphone.devices)
                {
                    microphones.Add(device);
                }

                // 저장된 마이크 선택
                string selectedMicrophone = "Default";
                if (!string.IsNullOrEmpty(saveData.microphoneDeviceId) && microphones.Contains(saveData.microphoneDeviceId))
                {
                    selectedMicrophone = saveData.microphoneDeviceId;
                }

                // 드롭다운 설정
                microphoneDropdown.SetItems(microphones, selectedMicrophone, OnMicrophoneChanged);
            }
        }

        private void LoadGraphicsSettings()
        {
            // 화질(해상도) 설정
            if (qualityDropdown != null)
            {
                List<string> qualities = new List<string>();
                foreach (var res in resolutionOptions)
                {
                    qualities.Add(res.name);
                }

                int qualityIndex = Mathf.Clamp(saveData.qualityLevel, 0, resolutionOptions.Length - 1);
                string selectedQuality = resolutionOptions[qualityIndex].name;

                qualityDropdown.SetItems(qualities, selectedQuality, OnQualityChanged);
            }

            // 화면 모드 설정
            if (screenModeDropdown != null)
            {
                List<string> screenModes = new List<string> { "Windowed", "Fullscreen" };
                int modeIndex = Mathf.Clamp(saveData.screenMode, 0, 1);
                string selectedMode = screenModes[modeIndex];

                screenModeDropdown.SetItems(screenModes, selectedMode, OnScreenModeChanged);
            }

            // 현재 설정 적용
            ApplyGraphicsSettings();
        }

        private void LoadLanguageSettings()
        {
            // UI 언어
            if (uiLanguageDropdown != null)
            {
                List<string> languages = new List<string> { "Korean", "English" };
                string selectedUILanguage = saveData.uiLanguage;

                // 드롭다운 설정
                uiLanguageDropdown.SetItems(languages, selectedUILanguage, OnUILanguageChanged);
            }
        }

        private void LoadVoiceSettings()
        {
            if (saveData == null) return;

            // 음성 입력 모드 드롭다운
            if (voiceInputModeDropdown != null)
            {
                List<string> inputModes = new List<string> { "Key Triggered", "Continuous" };

                // 저장된 음성 입력 모드 확인
                string selectedInputMode = "Key Triggered";
                if (!string.IsNullOrEmpty(saveData.voiceInputMode))
                {
                    selectedInputMode = saveData.voiceInputMode == "Continuous" ? "Continuous" : "Key Triggered";
                }

                voiceInputModeDropdown.SetItems(inputModes, selectedInputMode, OnVoiceInputModeChanged);

                // VoiceRecognitionManager에도 적용
                if (VoiceRecognitionManager.Instance != null)
                {
                    VoiceInputMode mode = selectedInputMode == "Continuous" ? VoiceInputMode.Continuous : VoiceInputMode.KeyTriggered;
                    VoiceRecognitionManager.Instance.SetVoiceInputMode(mode);
                }
            }

            // 서버 모드 드롭다운
            if (serverModeDropdown != null)
            {
                List<string> serverModes = new List<string> { "Online", "Offline" };

                // 저장된 서버 모드 확인
                string selectedMode = "Online";
                if (!string.IsNullOrEmpty(saveData.voiceServerMode))
                {
                    selectedMode = saveData.voiceServerMode == "offline" ? "Offline" : "Online";
                }

                serverModeDropdown.SetItems(serverModes, selectedMode, OnServerModeChanged);
            }
        }

        private void LoadKeyBindings()
        {
            if (saveData == null)
            {
                return;
            }

            // 기본 키 바인딩
            Dictionary<string, string> defaultKeys = new Dictionary<string, string>
            {
                { "MoveLeft", "A" },
                { "MoveRight", "D" },
                { "Jump", "W" },
                { "VoiceRecord", "Space" },
                { "SkillPanel", "Tab" }
            };

            // SaveData에 키 바인딩이 없으면 기본값 사용
            if (saveData.keyBindings == null || saveData.keyBindings.Count == 0)
            {
                saveData.keyBindings = new Dictionary<string, string>(defaultKeys);
            }

            // 누락된 키 바인딩 추가
            foreach (var defaultKey in defaultKeys)
            {
                if (!saveData.keyBindings.ContainsKey(defaultKey.Key))
                {
                    saveData.keyBindings[defaultKey.Key] = defaultKey.Value;
                }
            }

            // UI에 키 바인딩 표시
            foreach (var kvp in keyButtons)
            {
                if (kvp.Value != null)
                {
                    if (saveData.keyBindings.ContainsKey(kvp.Key))
                    {
                        kvp.Value.text = saveData.keyBindings[kvp.Key];
                    }
                    else
                    {
                        if (defaultKeys.ContainsKey(kvp.Key))
                        {
                            kvp.Value.text = defaultKeys[kvp.Key];
                            saveData.keyBindings[kvp.Key] = defaultKeys[kvp.Key];
                        }
                        else
                        {
                            kvp.Value.text = "None";
                        }
                    }
                }
            }
        }

        private void LoadPitchSettings()
        {
            if (saveData == null) return;

            // 저장된 경계 주파수 로드
            currentMinFrequency = saveData.pitchMinFrequency > 0 ? saveData.pitchMinFrequency : 130.81f;
            currentMaxFrequency = saveData.pitchMaxFrequency > 0 ? saveData.pitchMaxFrequency : 261.63f;

            // UI 업데이트
            UpdateFrequencyDisplay();
            UpdateGaugeAreas();
            UpdateMarkerPositions();
        }

        private void LoadPitchElementSettings()
        {
            if (saveData == null) return;

            // 저장된 속성 로드 (없으면 기본값)
            string lowElement = !string.IsNullOrEmpty(saveData.lowPitchElement) ? saveData.lowPitchElement : "Fire";
            string midElement = !string.IsNullOrEmpty(saveData.midPitchElement) ? saveData.midPitchElement : "Ice";
            string highElement = !string.IsNullOrEmpty(saveData.highPitchElement) ? saveData.highPitchElement : "Electric";

            // 드롭다운 설정
            if (lowPitchElementDropdown != null)
            {
                lowPitchElementDropdown.SetItems(availableElements, lowElement, OnLowPitchElementChanged);
            }
            if (midPitchElementDropdown != null)
            {
                midPitchElementDropdown.SetItems(availableElements, midElement, OnMidPitchElementChanged);
            }
            if (highPitchElementDropdown != null)
            {
                highPitchElementDropdown.SetItems(availableElements, highElement, OnHighPitchElementChanged);
            }
        }

        private void OnLowPitchElementChanged(string value)
        {
            if (saveData != null)
            {
                saveData.lowPitchElement = value;
                SaveSettings();
            }
        }

        private void OnMidPitchElementChanged(string value)
        {
            if (saveData != null)
            {
                saveData.midPitchElement = value;
                SaveSettings();
            }
        }

        private void OnHighPitchElementChanged(string value)
        {
            if (saveData != null)
            {
                saveData.highPitchElement = value;
                SaveSettings();
            }
        }

        private void UpdateFrequencyDisplay()
        {
            if (minFrequencyValue != null)
            {
                string note = FrequencyToNote(currentMinFrequency);
                minFrequencyValue.text = $"{currentMinFrequency:F2} Hz ({note})";
            }
            if (maxFrequencyValue != null)
            {
                string note = FrequencyToNote(currentMaxFrequency);
                maxFrequencyValue.text = $"{currentMaxFrequency:F2} Hz ({note})";
            }
        }

        private string FrequencyToNote(float frequency)
        {
            string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
            int noteNumber = Mathf.RoundToInt(57 + 12 * Mathf.Log(frequency / 440.0f, 2));
            string note = noteNames[noteNumber % 12];
            int octave = noteNumber / 12;
            return $"{note}{octave}";
        }

        private void UpdateGaugeAreas()
        {
            if (pitchGaugeBar == null) return;

            // 게이지 범위: 고정값 사용 (로그 스케일)
            float logMin = Mathf.Log(GAUGE_MIN_FREQ);
            float logMax = Mathf.Log(GAUGE_MAX_FREQ);

            // 최소 경계 위치 (0~1)
            float minPos = (Mathf.Log(currentMinFrequency) - logMin) / (logMax - logMin);
            // 최대 경계 위치 (0~1)
            float maxPos = (Mathf.Log(currentMaxFrequency) - logMin) / (logMax - logMin);

            // Low 영역: 0% ~ minPos%
            if (pitchGaugeLowArea != null)
            {
                pitchGaugeLowArea.style.left = new StyleLength(new Length(0, LengthUnit.Percent));
                pitchGaugeLowArea.style.width = new StyleLength(new Length(minPos * 100f, LengthUnit.Percent));
            }

            // Medium 영역: minPos% ~ maxPos%
            if (pitchGaugeMediumArea != null)
            {
                pitchGaugeMediumArea.style.left = new StyleLength(new Length(minPos * 100f, LengthUnit.Percent));
                pitchGaugeMediumArea.style.width = new StyleLength(new Length((maxPos - minPos) * 100f, LengthUnit.Percent));
            }

            // High 영역: maxPos% ~ 100%
            if (pitchGaugeHighArea != null)
            {
                pitchGaugeHighArea.style.left = new StyleLength(new Length(maxPos * 100f, LengthUnit.Percent));
                pitchGaugeHighArea.style.width = new StyleLength(new Length((1f - maxPos) * 100f, LengthUnit.Percent));
            }
        }

        private void UpdateMarkerPositions()
        {
            if (pitchGaugeBar == null) return;

            // 게이지 범위: 고정값 사용 (로그 스케일)
            float logMin = Mathf.Log(GAUGE_MIN_FREQ);
            float logMax = Mathf.Log(GAUGE_MAX_FREQ);

            // 최소 마커 위치
            float minPos = (Mathf.Log(currentMinFrequency) - logMin) / (logMax - logMin);
            if (pitchMinMarker != null)
            {
                pitchMinMarker.style.left = new StyleLength(new Length(minPos * 100f - 0.5f, LengthUnit.Percent));
            }

            // 최대 마커 위치
            float maxPos = (Mathf.Log(currentMaxFrequency) - logMin) / (logMax - logMin);
            if (pitchMaxMarker != null)
            {
                pitchMaxMarker.style.left = new StyleLength(new Length(maxPos * 100f - 0.5f, LengthUnit.Percent));
            }
        }

        private float GetFrequencyFromGaugePosition(float position)
        {
            // 게이지 범위: 고정값 사용 (로그 스케일)
            float logMin = Mathf.Log(GAUGE_MIN_FREQ);
            float logMax = Mathf.Log(GAUGE_MAX_FREQ);

            float logFreq = logMin + position * (logMax - logMin);
            return Mathf.Exp(logFreq);
        }

        private float GetGaugePositionFromFrequency(float frequency)
        {
            // 게이지 범위: 고정값 사용 (로그 스케일)
            float logMin = Mathf.Log(GAUGE_MIN_FREQ);
            float logMax = Mathf.Log(GAUGE_MAX_FREQ);
            float logFreq = Mathf.Log(Mathf.Clamp(frequency, GAUGE_MIN_FREQ, GAUGE_MAX_FREQ));

            return (logFreq - logMin) / (logMax - logMin);
        }

        /// <summary>
        /// 패널이 표시될 때 호출
        /// </summary>
        public void OnPanelShown()
        {
            // SaveData 새로고침
            LoadSaveData();
            LoadSettings();

            // 초기 패널 설정 (Audio 패널 표시)
            // 서버 체크와 피치 모니터링은 ShowPanel에서 게임 탭일 때만 시작됨
            ShowPanel(audioPanel);
        }

        /// <summary>
        /// 패널이 숨겨질 때 호출 (서버 체크 루프 등 정리)
        /// </summary>
        public void OnPanelHidden()
        {
            // 게임 탭 기능 정리 (서버 체크, 피치 모니터링, 테스트 모드)
            StopGameTabFeatures();
        }

        #region Voice Command Tab/Section Control

        /// <summary>
        /// 오디오 탭 열기 (음성 명령용)
        /// </summary>
        public void ShowAudioTab()
        {
            ShowPanel(audioPanel);
        }

        /// <summary>
        /// 그래픽 탭 열기 (음성 명령용)
        /// </summary>
        public void ShowGraphicsTab()
        {
            ShowPanel(graphicsPanel);
        }

        /// <summary>
        /// 언어 탭 열기 (음성 명령용)
        /// </summary>
        public void ShowLanguageTab()
        {
            ShowPanel(languagePanel);
        }

        /// <summary>
        /// 게임 탭 열기 (음성 명령용)
        /// </summary>
        public void ShowGameTab()
        {
            ShowPanel(gamePanel);
        }

        /// <summary>
        /// 현재 활성 탭 이름 반환
        /// </summary>
        public string GetCurrentTabName()
        {
            if (currentPanel == audioPanel) return "Audio";
            if (currentPanel == graphicsPanel) return "Graphics";
            if (currentPanel == languagePanel) return "Language";
            if (currentPanel == gamePanel) return "Game";
            return "Unknown";
        }

        /// <summary>
        /// 음성인식 섹션 펼치기 (음성 명령용)
        /// </summary>
        public void ExpandVoiceRecognitionSection()
        {
            // 게임 탭이 아니면 먼저 게임 탭으로 전환
            if (currentPanel != gamePanel)
            {
                ShowPanel(gamePanel);
            }
            voiceRecognitionSection?.Expand();
        }

        /// <summary>
        /// 음성인식 섹션 접기 (음성 명령용)
        /// </summary>
        public void CollapseVoiceRecognitionSection()
        {
            voiceRecognitionSection?.Collapse();
        }

        /// <summary>
        /// 키 설정 섹션 펼치기 (음성 명령용)
        /// </summary>
        public void ExpandKeyBindingSection()
        {
            // 게임 탭이 아니면 먼저 게임 탭으로 전환
            if (currentPanel != gamePanel)
            {
                ShowPanel(gamePanel);
            }
            keyBindingSection?.Expand();
        }

        /// <summary>
        /// 키 설정 섹션 접기 (음성 명령용)
        /// </summary>
        public void CollapseKeyBindingSection()
        {
            keyBindingSection?.Collapse();
        }

        #endregion

        /// <summary>
        /// 특정 패널만 표시하고 나머지는 숨김
        /// </summary>
        private void ShowPanel(VisualElement panelToShow)
        {
            if (panelToShow == null) return;

            // 이전 패널이 게임 탭이었으면 서버 체크와 피치 모니터링 중지
            if (currentPanel == gamePanel && panelToShow != gamePanel)
            {
                StopGameTabFeatures();
            }

            // 모든 카테고리 버튼에서 selected 클래스 제거
            audioButton?.RemoveFromClassList("selected");
            graphicsButton?.RemoveFromClassList("selected");
            languageButton?.RemoveFromClassList("selected");
            gameButton?.RemoveFromClassList("selected");

            // 모든 패널 숨기기
            if (audioPanel != null) audioPanel.style.display = DisplayStyle.None;
            if (graphicsPanel != null) graphicsPanel.style.display = DisplayStyle.None;
            if (languagePanel != null) languagePanel.style.display = DisplayStyle.None;
            if (gamePanel != null) gamePanel.style.display = DisplayStyle.None;

            // 선택한 패널만 표시
            panelToShow.style.display = DisplayStyle.Flex;
            currentPanel = panelToShow;

            // 해당 카테고리 버튼에 selected 클래스 추가
            if (panelToShow == audioPanel)
                audioButton?.AddToClassList("selected");
            else if (panelToShow == graphicsPanel)
                graphicsButton?.AddToClassList("selected");
            else if (panelToShow == languagePanel)
                languageButton?.AddToClassList("selected");
            else if (panelToShow == gamePanel)
            {
                gameButton?.AddToClassList("selected");
                // 게임 탭일 때만 서버 체크와 피치 모니터링 시작
                StartGameTabFeatures();
            }

            // 패널 제목 업데이트
            UpdateCurrentPanelTitle();
        }

        /// <summary>
        /// 게임 탭 기능 시작 (서버 체크, 피치 모니터링)
        /// </summary>
        private void StartGameTabFeatures()
        {
            // 서버 상태 체크 시작 (3초마다)
            if (serverStatusCheckCoroutine != null && coroutineRunner != null)
            {
                coroutineRunner.StopCoroutine(serverStatusCheckCoroutine);
            }
            serverStatusCheckCoroutine = coroutineRunner.StartCoroutine(CheckServerStatusLoop());

            // 실시간 피치 모니터링 시작
            StartRealtimePitchMonitoring();
        }

        /// <summary>
        /// 게임 탭 기능 중지 (서버 체크, 피치 모니터링)
        /// </summary>
        private void StopGameTabFeatures()
        {
            // 서버 상태 체크 중지
            if (serverStatusCheckCoroutine != null && coroutineRunner != null)
            {
                coroutineRunner.StopCoroutine(serverStatusCheckCoroutine);
                serverStatusCheckCoroutine = null;
            }

            // 실시간 피치 모니터링 중지
            StopRealtimePitchMonitoring();

            // 테스트 모드 종료
            if (isTestMode)
            {
                StopTestMode();
            }
        }

        /// <summary>
        /// 현재 패널에 따라 통합 헤더의 제목 업데이트
        /// </summary>
        private void UpdateCurrentPanelTitle()
        {
            if (currentPanelTitle == null || currentPanel == null) return;

            var loc = LocalizationManager.Instance;

            if (currentPanel == audioPanel)
                currentPanelTitle.text = loc.GetText("options_audio_title");
            else if (currentPanel == graphicsPanel)
                currentPanelTitle.text = loc.GetText("options_graphics_title");
            else if (currentPanel == languagePanel)
                currentPanelTitle.text = loc.GetText("options_language_title");
            else if (currentPanel == gamePanel)
                currentPanelTitle.text = loc.GetText("options_game_title");
        }

        /// <summary>
        /// 로컬라이제이션 업데이트
        /// </summary>
        public void UpdateLocalization(LocalizationManager loc)
        {
            if (optionsPanel == null) return;

            // Header
            var optionsTitle = optionsPanel.Q<Label>("OptionsTitle");
            if (optionsTitle != null)
                optionsTitle.text = loc.GetText("options_title");

            // 통합 리셋 버튼
            if (currentPanelResetButton != null)
                currentPanelResetButton.text = loc.GetText("options_audio_reset");

            // Category buttons
            if (audioButton != null)
                audioButton.text = loc.GetText("options_audio");
            if (graphicsButton != null)
                graphicsButton.text = loc.GetText("options_graphics");
            if (languageButton != null)
                languageButton.text = loc.GetText("options_language");
            if (gameButton != null)
                gameButton.text = loc.GetText("options_game");

            // Audio Panel labels
            var micLabel = optionsPanel.Q<Label>("MicrophoneLabel");
            if (micLabel != null)
                micLabel.text = loc.GetText("options_audio_microphone");

            // Graphics Panel labels
            var qualityLabel = optionsPanel.Q<Label>("QualityLabel");
            if (qualityLabel != null)
                qualityLabel.text = loc.GetText("options_graphics_quality");

            var screenModeLabel = optionsPanel.Q<Label>("ScreenModeLabel");
            if (screenModeLabel != null)
                screenModeLabel.text = loc.GetText("options_graphics_screen_mode");

            // Language Panel labels
            var uiLanguageLabel = optionsPanel.Q<Label>("UILanguageLabel");
            if (uiLanguageLabel != null)
                uiLanguageLabel.text = loc.GetText("options_language_ui");

            // Game Panel - Key Bindings 헤더
            var keyBindingsHeader = optionsPanel.Q<Label>("KeyBindingsHeader");
            if (keyBindingsHeader != null)
                keyBindingsHeader.text = loc.GetText("options_game_keybindings");

            // Voice Recognition 헤더
            var voiceRecognitionHeader = optionsPanel.Q<Label>("VoiceRecognitionHeader");
            if (voiceRecognitionHeader != null)
                voiceRecognitionHeader.text = loc.GetText("options_game_voice_recognition");

            // Voice Recognition - 음성 입력 모드 라벨
            var voiceInputModeLabel = optionsPanel.Q<Label>("VoiceInputModeLabel");
            if (voiceInputModeLabel != null)
                voiceInputModeLabel.text = loc.GetText("options_voice_input_mode");

            // Voice Recognition - 서버 모드 라벨
            var serverModeLabel = optionsPanel.Q<Label>("ServerModeLabel");
            if (serverModeLabel != null)
                serverModeLabel.text = loc.GetText("options_voice_server_mode");

            // 게임 초기화
            var resetGameLabel = optionsPanel.Q<Label>("ResetGameLabel");
            if (resetGameLabel != null)
                resetGameLabel.text = loc.GetText("options_game_reset_game");

            if (resetGameButton != null)
                resetGameButton.text = loc.GetText("options_game_reset_game");

            // 게임 초기화 확인 팝업
            if (gameResetConfirmation != null)
            {
                var gameResetTitle = gameResetConfirmation.Q<Label>("GameResetTitle");
                if (gameResetTitle != null)
                    gameResetTitle.text = loc.GetText("game_reset_popup_title");

                var gameResetMessage = gameResetConfirmation.Q<Label>("GameResetMessage");
                if (gameResetMessage != null)
                    gameResetMessage.text = loc.GetText("game_reset_popup_message");
            }

            if (confirmResetButton != null)
                confirmResetButton.text = loc.GetText("game_reset_popup_confirm");
            if (cancelResetButton != null)
                cancelResetButton.text = loc.GetText("game_reset_popup_cancel");

            // 피치 테스트 로컬라이제이션
            var pitchTestLabel = optionsPanel.Q<Label>("PitchTestLabel");
            if (pitchTestLabel != null)
                pitchTestLabel.text = loc.GetText("pitch_test_title");

            if (pitchLowLabel != null)
                pitchLowLabel.text = loc.GetText("pitch_low");
            if (pitchMediumLabel != null)
                pitchMediumLabel.text = loc.GetText("pitch_medium");
            if (pitchHighLabel != null)
                pitchHighLabel.text = loc.GetText("pitch_high");

            var minFreqLabel = optionsPanel.Q<Label>("MinFrequencyLabel");
            if (minFreqLabel != null)
                minFreqLabel.text = loc.GetText("pitch_min_frequency");

            var maxFreqLabel = optionsPanel.Q<Label>("MaxFrequencyLabel");
            if (maxFreqLabel != null)
                maxFreqLabel.text = loc.GetText("pitch_max_frequency");

            var pitchHelpLabel = optionsPanel.Q<Label>("PitchHelpLabel");
            if (pitchHelpLabel != null)
                pitchHelpLabel.text = loc.GetText("pitch_help_text");

            if (pitchTestButton != null && !isTestMode)
                pitchTestButton.text = loc.GetText("pitch_start_test");
            else if (pitchTestButton != null && isTestMode)
                pitchTestButton.text = loc.GetText("pitch_stop_test");

            // 현재 패널 제목 업데이트
            UpdateCurrentPanelTitle();
        }

        #region Event Handlers

        private void OnMicrophoneChanged(string value)
        {
            if (saveData != null)
            {
                saveData.microphoneDeviceId = value;
                SaveSettings();
            }
        }

        private void OnQualityChanged(string value)
        {
            if (saveData != null)
            {
                for (int i = 0; i < resolutionOptions.Length; i++)
                {
                    if (resolutionOptions[i].name == value)
                    {
                        saveData.qualityLevel = i;
                        break;
                    }
                }
                ApplyGraphicsSettings();
                SaveSettings();
            }
        }

        private void OnScreenModeChanged(string value)
        {
            if (saveData != null)
            {
                saveData.screenMode = (value == "Windowed") ? 0 : 1;
                saveData.isFullScreen = (saveData.screenMode == 1);
                ApplyGraphicsSettings();
                SaveSettings();
            }
        }

        private void ApplyGraphicsSettings()
        {
            int qualityIndex = Mathf.Clamp(saveData.qualityLevel, 0, resolutionOptions.Length - 1);
            var resolution = resolutionOptions[qualityIndex];

            if (saveData.screenMode == 0)
            {
                Screen.SetResolution(resolution.width, resolution.height, FullScreenMode.Windowed);
            }
            else
            {
                Screen.SetResolution(resolution.width, resolution.height, FullScreenMode.ExclusiveFullScreen);
            }
        }

        private void OnUILanguageChanged(string value)
        {
            if (saveData != null)
            {
                saveData.uiLanguage = value;
                Language language = value == "Korean" ? Language.Korean : Language.English;
                LocalizationManager.Instance.SetLanguage(language);
                SaveSettings();
            }
        }

        private void OnServerModeChanged(string value)
        {
            if (saveData != null)
            {
                saveData.voiceServerMode = value == "Offline" ? "offline" : "online";

                var voiceClient = Object.FindObjectOfType<VoiceServerClient>();
                if (voiceClient != null)
                {
                    voiceClient.SetServerMode(saveData.voiceServerMode);
                }
                SaveSettings();
            }
        }

        private void OnVoiceInputModeChanged(string value)
        {
            if (saveData != null)
            {
                saveData.voiceInputMode = value == "Continuous" ? "Continuous" : "KeyTriggered";

                if (VoiceRecognitionManager.Instance != null)
                {
                    VoiceInputMode mode = value == "Continuous" ? VoiceInputMode.Continuous : VoiceInputMode.KeyTriggered;
                    VoiceRecognitionManager.Instance.SetVoiceInputMode(mode);
                }
                SaveSettings();
            }
        }

        private void OnCurrentPanelReset()
        {
            if (currentPanel == audioPanel)
                OnAudioReset();
            else if (currentPanel == graphicsPanel)
                OnGraphicsReset();
            else if (currentPanel == languagePanel)
                OnLanguageReset();
            else if (currentPanel == gamePanel)
                OnGameReset();
        }

        private void OnAudioReset()
        {
            if (saveData != null)
            {
                saveData.microphoneDeviceId = "";
                LoadAudioSettings();
                SaveSettings();
            }
        }

        private void OnGraphicsReset()
        {
            if (saveData != null)
            {
                saveData.qualityLevel = 2;
                saveData.screenMode = 1;
                saveData.isFullScreen = true;
                LoadGraphicsSettings();
                SaveSettings();
            }
        }

        private void OnLanguageReset()
        {
            if (saveData != null && uiLanguageDropdown != null)
            {
                saveData.uiLanguage = "Korean";
                uiLanguageDropdown.SetValue("Korean");
                LocalizationManager.Instance.SetLanguage(Language.Korean);
                LoadLanguageSettings();
                SaveSettings();
            }
        }

        private void OnGameReset()
        {
            if (saveData != null)
            {
                saveData.keyBindings = new Dictionary<string, string>
                {
                    { "MoveLeft", "A" },
                    { "MoveRight", "D" },
                    { "Jump", "W" },
                    { "VoiceRecord", "Space" },
                    { "SkillPanel", "Tab" }
                };

                // 피치 설정도 초기화 (기본값: C3 ~ C4)
                saveData.pitchMinFrequency = 130.81f;
                saveData.pitchMaxFrequency = 261.63f;
                currentMinFrequency = 130.81f;
                currentMaxFrequency = 261.63f;

                // 피치 속성 초기화
                saveData.lowPitchElement = "Fire";
                saveData.midPitchElement = "Ice";
                saveData.highPitchElement = "Electric";

                LoadKeyBindings();
                LoadPitchSettings();
                LoadPitchElementSettings();
                SaveSettings();
            }
        }

        private void OnKeyButtonClicked(string action)
        {
            isWaitingForKey = true;
            currentKeyAction = action;

            if (keyButtons.ContainsKey(action))
            {
                keyButtons[action].text = "Press a key...";
            }
        }

        private void OnResetGameButtonClicked()
        {
            ShowGameResetConfirmation();
        }

        private void OnConfirmResetButtonClicked()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.ResetSaveData();
                saveData = SaveManager.Instance.GetCurrentSaveData();
                LoadSettings();
            }
            HideGameResetConfirmation();
        }

        private void OnCancelResetButtonClicked()
        {
            HideGameResetConfirmation();
        }

        private void ShowGameResetConfirmation()
        {
            if (gameResetConfirmation != null)
            {
                gameResetConfirmation.style.display = DisplayStyle.Flex;
            }
        }

        private void HideGameResetConfirmation()
        {
            if (gameResetConfirmation != null)
            {
                gameResetConfirmation.style.display = DisplayStyle.None;
            }
        }

        #region Marker Drag Handlers

        private void OnMinMarkerPointerDown(PointerDownEvent evt)
        {
            isDraggingMinMarker = true;
            pitchMinMarker?.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnMaxMarkerPointerDown(PointerDownEvent evt)
        {
            isDraggingMaxMarker = true;
            pitchMaxMarker?.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnMarkerPointerMove(PointerMoveEvent evt)
        {
            if (!isDraggingMinMarker && !isDraggingMaxMarker) return;
            if (pitchGaugeBar == null) return;

            // 게이지 바 기준 위치 계산 (마커의 부모인 게이지 바 좌표로 변환)
            Vector2 localPos = pitchGaugeBar.WorldToLocal(evt.position);
            float gaugeWidth = pitchGaugeBar.resolvedStyle.width;
            float position = Mathf.Clamp01(localPos.x / gaugeWidth);

            // 고정된 게이지 범위로 주파수 계산
            float newFrequency = GetFrequencyFromGaugePosition(position);

            // 주파수 범위 제한 (50Hz ~ 1000Hz)
            newFrequency = Mathf.Clamp(newFrequency, GAUGE_MIN_FREQ, GAUGE_MAX_FREQ);

            if (isDraggingMinMarker)
            {
                currentMinFrequency = newFrequency;
            }
            else if (isDraggingMaxMarker)
            {
                currentMaxFrequency = newFrequency;
            }

            // 두 값이 엇갈리면 스왑 (낮은 값이 항상 min, 높은 값이 항상 max)
            if (currentMinFrequency > currentMaxFrequency)
            {
                float temp = currentMinFrequency;
                currentMinFrequency = currentMaxFrequency;
                currentMaxFrequency = temp;

                // 드래그 중인 마커도 스왑
                bool tempDragging = isDraggingMinMarker;
                isDraggingMinMarker = isDraggingMaxMarker;
                isDraggingMaxMarker = tempDragging;
            }

            // UI 업데이트
            UpdateFrequencyDisplay();
            UpdateGaugeAreas();
            UpdateMarkerPositions();

            evt.StopPropagation();
        }

        private void OnMarkerPointerUp(PointerUpEvent evt)
        {
            if (isDraggingMinMarker || isDraggingMaxMarker)
            {
                // 저장
                SavePitchSettings();

                // PitchAnalyzer에도 적용
                if (pitchAnalyzer != null)
                {
                    pitchAnalyzer.SetBoundaryFrequencies(currentMinFrequency, currentMaxFrequency);
                }
            }

            isDraggingMinMarker = false;
            isDraggingMaxMarker = false;

            pitchMinMarker?.ReleasePointer(evt.pointerId);
            pitchMaxMarker?.ReleasePointer(evt.pointerId);

            evt.StopPropagation();
        }

        private void SavePitchSettings()
        {
            if (saveData != null)
            {
                saveData.pitchMinFrequency = currentMinFrequency;
                saveData.pitchMaxFrequency = currentMaxFrequency;
                SaveSettings();
            }
        }

        #endregion

        #region Realtime Pitch Monitoring

        /// <summary>
        /// 실시간 피치 모니터링 시작 (패널 열릴 때 자동 시작)
        /// VoiceRecorder의 마이크를 공유하여 음성인식과 동시에 동작
        /// </summary>
        private void StartRealtimePitchMonitoring()
        {
            if (isRealtimeMonitoring) return;

            isRealtimeMonitoring = true;

            // VoiceRecorder의 마이크를 공유 (일시정지하지 않음!)
            // 이렇게 하면 피치 모니터링과 음성인식이 동시에 동작
            var voiceRecorder = VoiceRecognitionManager.Instance?.voiceRecorder;
            if (voiceRecorder == null || !voiceRecorder.IsMicrophoneReady)
            {
                // VoiceRecorder가 없거나 준비되지 않은 경우 대기
                coroutineRunner.StartCoroutine(WaitForVoiceRecorderAndStart());
                return;
            }

            // VoiceRecorder의 마이크 정보 사용
            micDevice = voiceRecorder.MicrophoneDevice;
            realtimeClip = voiceRecorder.LoopingClip;

            if (realtimeClip == null)
            {
                isRealtimeMonitoring = false;
                return;
            }

            // PitchAnalyzer 찾기 또는 생성
            pitchAnalyzer = Object.FindObjectOfType<PitchAnalyzer>();
            if (pitchAnalyzer == null)
            {
                var go = new GameObject("PitchAnalyzer_Options");
                pitchAnalyzer = go.AddComponent<PitchAnalyzer>();
            }
            pitchAnalyzer.SetBoundaryFrequencies(currentMinFrequency, currentMaxFrequency);

            // 실시간 인디케이터 표시
            if (pitchRealtimeIndicator != null)
            {
                pitchRealtimeIndicator.style.display = DisplayStyle.Flex;
            }

            // 실시간 피치 모니터링 코루틴 시작
            coroutineRunner.StartCoroutine(RealtimePitchCoroutine());
        }

        /// <summary>
        /// VoiceRecorder가 준비될 때까지 대기 후 피치 모니터링 시작
        /// </summary>
        private IEnumerator WaitForVoiceRecorderAndStart()
        {
            float timeout = 3f;
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                var voiceRecorder = VoiceRecognitionManager.Instance?.voiceRecorder;
                if (voiceRecorder != null && voiceRecorder.IsMicrophoneReady && voiceRecorder.LoopingClip != null)
                {
                    micDevice = voiceRecorder.MicrophoneDevice;
                    realtimeClip = voiceRecorder.LoopingClip;

                    // PitchAnalyzer 설정
                    pitchAnalyzer = Object.FindObjectOfType<PitchAnalyzer>();
                    if (pitchAnalyzer == null)
                    {
                        var go = new GameObject("PitchAnalyzer_Options");
                        pitchAnalyzer = go.AddComponent<PitchAnalyzer>();
                    }
                    pitchAnalyzer.SetBoundaryFrequencies(currentMinFrequency, currentMaxFrequency);

                    // 실시간 인디케이터 표시
                    if (pitchRealtimeIndicator != null)
                    {
                        pitchRealtimeIndicator.style.display = DisplayStyle.Flex;
                    }

                    // 실시간 피치 모니터링 코루틴 시작
                    coroutineRunner.StartCoroutine(RealtimePitchCoroutine());
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // 타임아웃 - 피치 모니터링 비활성화
            isRealtimeMonitoring = false;
        }

        /// <summary>
        /// 실시간 피치 모니터링 종료
        /// </summary>
        private void StopRealtimePitchMonitoring()
        {
            if (!isRealtimeMonitoring) return;

            isRealtimeMonitoring = false;

            // VoiceRecorder의 마이크를 공유하므로 여기서 마이크를 정지하지 않음!
            // Microphone.End(micDevice); <- 삭제

            // 실시간 인디케이터 숨기기
            if (pitchRealtimeIndicator != null)
            {
                pitchRealtimeIndicator.style.display = DisplayStyle.None;
            }

            // VoiceRecorder의 마이크를 공유하므로 재개 호출 불필요
            // VoiceRecorder는 계속 동작 중
        }

        /// <summary>
        /// 실시간 피치 모니터링 코루틴 (VAD 포함)
        /// </summary>
        private IEnumerator RealtimePitchCoroutine()
        {
            float[] samples = new float[2048];
            float lastUpdateTime = 0f;

            while (isRealtimeMonitoring)
            {
                yield return null;

                if (realtimeClip == null || pitchAnalyzer == null) continue;

                // 마이크가 녹음 중인지 확인
                if (!Microphone.IsRecording(micDevice)) continue;

                // 마이크 데이터 읽기
                int micPosition = Microphone.GetPosition(micDevice);
                if (micPosition < samples.Length) continue;

                // 오프셋이 유효한 범위인지 확인
                int offset = micPosition - samples.Length;
                if (offset < 0) continue;

                try
                {
                    realtimeClip.GetData(samples, offset);
                }
                catch (System.Exception)
                {
                    continue;
                }

                // RMS 계산 (음성 감지용)
                float rms = 0f;
                for (int i = 0; i < samples.Length; i++)
                {
                    rms += Mathf.Abs(samples[i]);
                }
                rms /= samples.Length;

                // 피치 검출 (VoiceRecorder의 샘플레이트 사용)
                int sampleRate = realtimeClip.frequency;
                float frequency = pitchAnalyzer.DetectPitchRealtime(samples, sampleRate);

                // 실시간 인디케이터 위치 업데이트 (빨간색 선) - 항상 동작
                if (frequency > 0)
                {
                    currentPitchFrequency = frequency;
                    float gaugeValue = GetGaugePositionFromFrequency(frequency);
                    if (pitchRealtimeIndicator != null)
                    {
                        pitchRealtimeIndicator.style.left = new StyleLength(new Length(gaugeValue * 100f, LengthUnit.Percent));
                    }
                }

                // 테스트 모드가 아니면 주파수만 표시 (카테고리 없이 Hz만)
                if (!isTestMode)
                {
                    if (frequency > 0 && currentFrequencyLabel != null)
                    {
                        currentFrequencyLabel.text = $"{frequency:F1} Hz";
                    }
                    continue;
                }

                // === 테스트 모드 처리 ===

                // 결과 표시 5초 후 자동 삭제
                if (resultDisplayTime > 0 && Time.realtimeSinceStartup - resultDisplayTime >= RESULT_DISPLAY_DURATION)
                {
                    if (pitchTestResultLabel != null)
                    {
                        pitchTestResultLabel.text = "";
                    }
                    resultDisplayTime = 0f;
                }

                var voiceInputMode = VoiceRecognitionManager.Instance?.GetVoiceInputMode() ?? VoiceInputMode.KeyTriggered;

                if (voiceInputMode == VoiceInputMode.Continuous)
                {
                    // 연속 모드: VAD 기반 음성 감지
                    ProcessContinuousModeVAD(rms, micPosition);
                }
                else
                {
                    // 키 트리거 모드: 키 입력 확인
                    ProcessKeyTriggeredMode(micPosition);
                }
            }
        }

        /// <summary>
        /// 연속 모드 VAD 처리
        /// </summary>
        private void ProcessContinuousModeVAD(float rms, int currentPosition)
        {
            if (!isVoiceActive)
            {
                // 음성 대기 중
                if (rms > VOICE_THRESHOLD)
                {
                    // 음성 감지 시작
                    isVoiceActive = true;
                    silenceTimer = 0f;
                    recordingBuffer.Clear();
                    voiceStartPosition = currentPosition;
                }
            }
            else
            {
                // 녹음 중
                if (rms < VOICE_THRESHOLD / 2)
                {
                    silenceTimer += Time.deltaTime;

                    if (silenceTimer >= SILENCE_TIMEOUT)
                    {
                        // 녹음 종료 및 처리
                        isVoiceActive = false;
                        ProcessRecordedVoice(voiceStartPosition, currentPosition);
                    }
                }
                else
                {
                    silenceTimer = 0f;
                }
            }
        }

        private float lastVADUpdateTime = 0f;

        /// <summary>
        /// 키 트리거 모드 처리
        /// </summary>
        private void ProcessKeyTriggeredMode(int currentPosition)
        {
            if (Keyboard.current == null) return;

            Key voiceRecordKey = GetVoiceRecordKeyFromSettings();

            if (Keyboard.current[voiceRecordKey].wasPressedThisFrame && !isKeyRecording)
            {
                // 녹음 시작
                isKeyRecording = true;
                recordingBuffer.Clear();
                voiceStartPosition = currentPosition;
            }
            else if (Keyboard.current[voiceRecordKey].wasReleasedThisFrame && isKeyRecording)
            {
                // 녹음 종료
                isKeyRecording = false;
                ProcessRecordedVoice(voiceStartPosition, currentPosition);
            }
        }

        /// <summary>
        /// 저장된 음성 녹음 키 가져오기
        /// </summary>
        private Key GetVoiceRecordKeyFromSettings()
        {
            var saveData = SaveManager.Instance?.GetCurrentSaveData();
            if (saveData != null && saveData.keyBindings.TryGetValue("VoiceRecord", out string keyString))
            {
                if (System.Enum.TryParse(keyString, out Key key))
                {
                    return key;
                }
            }
            return Key.Space; // 기본값
        }

        /// <summary>
        /// 녹음된 음성 처리 (피치 분석 + 서버 전송)
        /// </summary>
        private void ProcessRecordedVoice(int startPosition, int endPosition)
        {
            if (realtimeClip == null) return;

            // 녹음 길이 계산
            int totalSamples = realtimeClip.samples;
            int sampleCount;

            if (endPosition >= startPosition)
            {
                sampleCount = endPosition - startPosition;
            }
            else
            {
                // 순환 버퍼
                sampleCount = (totalSamples - startPosition) + endPosition;
            }

            int clipSampleRate = realtimeClip.frequency;
            float recordingLength = (float)sampleCount / clipSampleRate;

            if (recordingLength < MIN_RECORDING_LENGTH)
            {
                return;
            }

            // 오디오 데이터 추출
            float[] allSamples = new float[totalSamples];
            realtimeClip.GetData(allSamples, 0);

            float[] recordedSamples = new float[sampleCount];
            if (endPosition >= startPosition)
            {
                System.Array.Copy(allSamples, startPosition, recordedSamples, 0, sampleCount);
            }
            else
            {
                int firstPart = totalSamples - startPosition;
                System.Array.Copy(allSamples, startPosition, recordedSamples, 0, firstPart);
                System.Array.Copy(allSamples, 0, recordedSamples, firstPart, endPosition);
            }

            // AudioClip 생성 (VoiceRecorder와 동일한 샘플레이트 사용)
            AudioClip recordedClip = AudioClip.Create("RecordedVoice", sampleCount, 1, clipSampleRate, false);
            recordedClip.SetData(recordedSamples, 0);

            // 분석 및 서버 전송
            coroutineRunner.StartCoroutine(AnalyzeAndSendToServer(recordedClip));
        }

        /// <summary>
        /// 피치 분석 및 서버 전송
        /// </summary>
        private IEnumerator AnalyzeAndSendToServer(AudioClip recordedClip)
        {
            // 피치 분석
            PitchAnalysisResult pitchResult = null;
            if (pitchAnalyzer != null)
            {
                pitchResult = pitchAnalyzer.AnalyzeClip(recordedClip);
            }

            // 중간음 범위 판단
            bool isInMediumRange = false;
            if (pitchResult != null)
            {
                isInMediumRange = (pitchResult.DominantCategory == PitchCategory.Medium);
            }

            // 서버로 음성 인식 요청
            var serverClient = Object.FindObjectOfType<VoiceServerClient>();
            string recognizedText = null;

            if (serverClient != null)
            {
                byte[] wavData = AudioClipToWav(recordedClip);

                yield return serverClient.RecognizeSkill(wavData, "Options", "", (result) =>
                {
                    if (result != null && !string.IsNullOrEmpty(result.recognized_text))
                    {
                        recognizedText = result.recognized_text;
                    }
                });
            }

            // 결과 표시 (테스트 모드 유지)
            DisplayTestResult(recognizedText, pitchResult, isInMediumRange);

            // 클립 정리
            Object.Destroy(recordedClip);
        }

        #endregion

        #region Test Mode Control

        /// <summary>
        /// 테스트 버튼 클릭 핸들러
        /// </summary>
        private void OnPitchTestButtonClicked()
        {
            if (isTestMode)
            {
                StopTestMode();
            }
            else
            {
                StartTestMode();
            }
        }

        /// <summary>
        /// 테스트 모드 시작
        /// </summary>
        private void StartTestMode()
        {
            if (isTestMode) return;

            isTestMode = true;
            isVoiceActive = false;
            isKeyRecording = false;
            silenceTimer = 0f;
            recordingBuffer.Clear();

            // 버튼 텍스트 변경
            if (pitchTestButton != null)
            {
                pitchTestButton.text = LocalizationManager.Instance.GetText("pitch_stop_test");
                pitchTestButton.AddToClassList("testing");
            }

            // 결과 라벨 초기화
            if (pitchTestResultLabel != null)
            {
                pitchTestResultLabel.text = "";
            }

        }

        /// <summary>
        /// 테스트 모드 종료
        /// </summary>
        private void StopTestMode()
        {
            if (!isTestMode) return;

            isTestMode = false;
            isVoiceActive = false;
            isKeyRecording = false;
            silenceTimer = 0f;

            // 버튼 텍스트 변경
            if (pitchTestButton != null)
            {
                pitchTestButton.text = LocalizationManager.Instance.GetText("pitch_start_test");
                pitchTestButton.RemoveFromClassList("testing");
            }

            // 테스트 결과 라벨 초기화
            if (pitchTestResultLabel != null)
            {
                pitchTestResultLabel.text = "";
            }
        }

        /// <summary>
        /// AudioClip을 WAV byte[]로 변환
        /// </summary>
        private byte[] AudioClipToWav(AudioClip clip)
        {
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            int sampleRate = clip.frequency;
            int channels = clip.channels;

            using (var stream = new System.IO.MemoryStream())
            {
                using (var writer = new System.IO.BinaryWriter(stream))
                {
                    // WAV 헤더 작성
                    int bytesPerSample = 2; // 16-bit
                    int fileSize = 44 + samples.Length * bytesPerSample;

                    // RIFF header
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                    writer.Write(fileSize - 8);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

                    // fmt chunk
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                    writer.Write(16); // chunk size
                    writer.Write((short)1); // PCM format
                    writer.Write((short)channels);
                    writer.Write(sampleRate);
                    writer.Write(sampleRate * channels * bytesPerSample); // byte rate
                    writer.Write((short)(channels * bytesPerSample)); // block align
                    writer.Write((short)(bytesPerSample * 8)); // bits per sample

                    // data chunk
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                    writer.Write(samples.Length * bytesPerSample);

                    // 샘플 데이터 작성
                    foreach (float sample in samples)
                    {
                        short intSample = (short)(sample * 32767f);
                        writer.Write(intSample);
                    }
                }

                return stream.ToArray();
            }
        }

        /// <summary>
        /// 테스트 결과 표시 (인식문장(음) 형식)
        /// </summary>
        private void DisplayTestResult(string recognizedText, PitchAnalysisResult pitchResult, bool isInMediumRange)
        {
            if (pitchTestResultLabel == null) return;

            // 인식 실패 시 표시하지 않음
            if (string.IsNullOrEmpty(recognizedText))
            {
                return;
            }

            string categoryStr = "중";
            if (pitchResult != null)
            {
                categoryStr = GetCategoryStringShort(pitchResult.DominantCategory);
            }

            // "인식문장(음)" 형식으로 표시
            pitchTestResultLabel.text = $"{recognizedText}({categoryStr})";

            // 결과 표시 시간 기록 (5초간 유지)
            resultDisplayTime = Time.realtimeSinceStartup;
        }

        /// <summary>
        /// 피치 카테고리를 문자열로 변환 (풀네임)
        /// </summary>
        private string GetCategoryString(PitchCategory category)
        {
            var loc = LocalizationManager.Instance;
            switch (category)
            {
                case PitchCategory.Low:
                    return loc.GetText("pitch_low");
                case PitchCategory.Medium:
                    return loc.GetText("pitch_medium");
                case PitchCategory.High:
                    return loc.GetText("pitch_high");
                default:
                    return loc.GetText("pitch_medium");
            }
        }

        /// <summary>
        /// 피치 카테고리를 짧은 문자열로 변환 (저/중/고)
        /// </summary>
        private string GetCategoryStringShort(PitchCategory category)
        {
            var loc = LocalizationManager.Instance;
            if (loc.CurrentLanguage == Language.Korean)
            {
                switch (category)
                {
                    case PitchCategory.Low: return "저";
                    case PitchCategory.Medium: return "중";
                    case PitchCategory.High: return "고";
                    default: return "중";
                }
            }
            else
            {
                switch (category)
                {
                    case PitchCategory.Low: return "L";
                    case PitchCategory.Medium: return "M";
                    case PitchCategory.High: return "H";
                    default: return "M";
                }
            }
        }

        #endregion

        private void SaveSettings()
        {
            if (saveData == null || saveManager == null) return;
            saveManager.SaveGame();
        }

        #endregion

        #region Server Status

        /// <summary>
        /// 3초마다 서버 상태를 체크하는 루프 코루틴
        /// </summary>
        private IEnumerator CheckServerStatusLoop()
        {
            while (true)
            {
                yield return CheckServerStatus();
                yield return new WaitForSecondsRealtime(3f);
            }
        }

        /// <summary>
        /// 서버 연결 상태를 한 번 체크
        /// </summary>
        private IEnumerator CheckServerStatus()
        {
            using (UnityWebRequest request = UnityWebRequest.Get($"{SERVER_URL}/"))
            {
                request.timeout = 2;
                yield return request.SendWebRequest();

                if (serverStatusLabel != null)
                {
                    var loc = LocalizationManager.Instance;
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        serverStatusLabel.text = loc.GetText("options_voice_server_connected");
                        serverStatusLabel.style.color = new Color(0.2f, 0.8f, 0.2f);
                    }
                    else
                    {
                        serverStatusLabel.text = loc.GetText("options_voice_server_disconnected");
                        serverStatusLabel.style.color = new Color(0.8f, 0.2f, 0.2f);
                    }
                }
            }
        }

        #endregion

        public void Dispose()
        {
            // 테스트 모드 종료
            if (isTestMode)
            {
                StopTestMode();
            }

            // 서버 상태 체크 코루틴 중지
            if (serverStatusCheckCoroutine != null && coroutineRunner != null)
            {
                coroutineRunner.StopCoroutine(serverStatusCheckCoroutine);
                serverStatusCheckCoroutine = null;
            }

            // 실시간 피치 모니터링 중지
            StopRealtimePitchMonitoring();

            // 드롭다운 정리
            microphoneDropdown?.Dispose();
            qualityDropdown?.Dispose();
            screenModeDropdown?.Dispose();
            uiLanguageDropdown?.Dispose();
            serverModeDropdown?.Dispose();
            voiceInputModeDropdown?.Dispose();
            lowPitchElementDropdown?.Dispose();
            midPitchElementDropdown?.Dispose();
            highPitchElementDropdown?.Dispose();

            // 접기/펼치기 섹션 정리
            keyBindingSection?.Dispose();
            voiceRecognitionSection?.Dispose();
        }
    }
}
