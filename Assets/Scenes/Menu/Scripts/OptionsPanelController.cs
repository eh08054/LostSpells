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

        // 서버 체크
        private const string SERVER_URL = "http://localhost:8000";
        private MonoBehaviour coroutineRunner;

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
                Debug.LogError("SaveData is null. Cannot load key bindings.");
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

        /// <summary>
        /// 패널이 표시될 때 호출
        /// </summary>
        public void OnPanelShown()
        {
            // SaveData 새로고침
            LoadSaveData();
            LoadSettings();

            // 초기 패널 설정 (Audio 패널 표시)
            ShowPanel(audioPanel);

            // 서버 상태 체크 시작
            coroutineRunner.StartCoroutine(CheckServerStatus());
        }

        /// <summary>
        /// 특정 패널만 표시하고 나머지는 숨김
        /// </summary>
        private void ShowPanel(VisualElement panelToShow)
        {
            if (panelToShow == null) return;

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
                gameButton?.AddToClassList("selected");

            // 패널 제목 업데이트
            UpdateCurrentPanelTitle();
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
                LoadKeyBindings();
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

        private void SaveSettings()
        {
            if (saveData == null || saveManager == null) return;
            saveManager.SaveGame();
        }

        #endregion

        #region Server Status

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
            // 드롭다운 정리
            microphoneDropdown?.Dispose();
            qualityDropdown?.Dispose();
            screenModeDropdown?.Dispose();
            uiLanguageDropdown?.Dispose();
            serverModeDropdown?.Dispose();

            // 접기/펼치기 섹션 정리
            keyBindingSection?.Dispose();
            voiceRecognitionSection?.Dispose();
        }
    }
}
