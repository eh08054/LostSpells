using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using LostSpells.Data;
using LostSpells.Systems;

namespace LostSpells.UI
{
    /// <summary>
    /// InGame Options 팝업 패널 컨트롤러
    /// </summary>
    public class InGameOptionsPanelController
    {
        private VisualElement root;
        private VisualElement optionsPopup;

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

        // 키 바인딩 상태
        private bool isWaitingForKey = false;
        private string currentKeyAction = "";

        // 해상도 옵션
        private static readonly (int width, int height, string name)[] resolutionOptions = new[]
        {
            (1280, 720, "1280x720 (HD)"),
            (1600, 900, "1600x900"),
            (1920, 1080, "1920x1080 (FHD)"),
            (2560, 1440, "2560x1440 (QHD)")
        };

        public InGameOptionsPanelController(VisualElement root, VisualElement optionsPopup)
        {
            this.root = root;
            this.optionsPopup = optionsPopup;

            // SaveManager 싱글톤 인스턴스 가져오기
            saveManager = SaveManager.Instance;

            // UI 요소 찾기
            FindUIElements();

            // 이벤트 등록
            RegisterEvents();
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
            if (optionsPopup == null) return;

            // 통합 패널 헤더 (공유 템플릿 사용)
            currentPanelTitle = optionsPopup.Q<Label>("CurrentPanelTitle");
            currentPanelResetButton = optionsPopup.Q<Button>("CurrentPanelResetButton");

            // 카테고리 버튼들
            audioButton = optionsPopup.Q<Button>("AudioButton");
            graphicsButton = optionsPopup.Q<Button>("GraphicsButton");
            languageButton = optionsPopup.Q<Button>("LanguageButton");
            gameButton = optionsPopup.Q<Button>("GameButton");

            // 패널들
            audioPanel = optionsPopup.Q<VisualElement>("AudioPanel");
            graphicsPanel = optionsPopup.Q<VisualElement>("GraphicsPanel");
            languagePanel = optionsPopup.Q<VisualElement>("LanguagePanel");
            gamePanel = optionsPopup.Q<VisualElement>("GamePanel");

            // Audio 패널 컨트롤
            microphoneDropdown = new CustomDropdown(optionsPopup, "MicrophoneDropdownContainer", "MicrophoneDropdownButton", "MicrophoneDropdownLabel", "MicrophoneDropdownList");

            // Graphics 패널 컨트롤
            qualityDropdown = new CustomDropdown(optionsPopup, "QualityDropdownContainer", "QualityDropdownButton", "QualityDropdownLabel", "QualityDropdownList");
            screenModeDropdown = new CustomDropdown(optionsPopup, "ScreenModeDropdownContainer", "ScreenModeDropdownButton", "ScreenModeDropdownLabel", "ScreenModeDropdownList");

            // Language 패널 컨트롤
            uiLanguageDropdown = new CustomDropdown(optionsPopup, "UILanguageDropdownContainer", "UILanguageDropdownButton", "UILanguageDropdownLabel", "UILanguageDropdownList");

            // Voice Recognition 서버 모드 컨트롤
            serverModeDropdown = new CustomDropdown(optionsPopup, "ServerModeDropdownContainer", "ServerModeDropdownButton", "ServerModeDropdownLabel", "ServerModeDropdownList");

            // Game 패널 컨트롤
            keyBindingSection = new CollapsibleSection(optionsPopup, "KeyBindingsHeader", "KeyBindingsToggleButton", "KeyBindingArea");
            voiceRecognitionSection = new CollapsibleSection(optionsPopup, "VoiceRecognitionHeader", "VoiceRecognitionToggleButton", "VoiceRecognitionArea");
            serverStatusLabel = optionsPopup.Q<Label>("ServerStatusLabel");

            // 키 버튼들
            keyButtons["MoveLeft"] = optionsPopup.Q<Button>("MoveLeftKey");
            keyButtons["MoveRight"] = optionsPopup.Q<Button>("MoveRightKey");
            keyButtons["Jump"] = optionsPopup.Q<Button>("JumpKey");
            keyButtons["VoiceRecord"] = optionsPopup.Q<Button>("VoiceRecordKey");
            keyButtons["SkillPanel"] = optionsPopup.Q<Button>("SkillPanelKey");

            // 게임 초기화 버튼
            resetGameButton = optionsPopup.Q<Button>("ResetGameButton");

            // 게임 초기화 확인 팝업
            gameResetConfirmation = optionsPopup.Q<VisualElement>("GameResetConfirmation");
            confirmResetButton = optionsPopup.Q<Button>("ConfirmResetButton");
            cancelResetButton = optionsPopup.Q<Button>("CancelResetButton");
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

            LoadAudioSettings();
            LoadGraphicsSettings();
            LoadLanguageSettings();
            LoadVoiceSettings();
            LoadKeyBindings();
        }

        private void LoadAudioSettings()
        {
            if (microphoneDropdown != null)
            {
                List<string> microphones = new List<string> { "Default" };
                foreach (var device in Microphone.devices)
                {
                    microphones.Add(device);
                }

                string selectedMicrophone = "Default";
                if (!string.IsNullOrEmpty(saveData.microphoneDeviceId) && microphones.Contains(saveData.microphoneDeviceId))
                {
                    selectedMicrophone = saveData.microphoneDeviceId;
                }

                microphoneDropdown.SetItems(microphones, selectedMicrophone, OnMicrophoneChanged);
            }
        }

        private void LoadGraphicsSettings()
        {
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

            if (screenModeDropdown != null)
            {
                List<string> screenModes = new List<string> { "Windowed", "Fullscreen" };
                int modeIndex = Mathf.Clamp(saveData.screenMode, 0, 1);
                string selectedMode = screenModes[modeIndex];

                screenModeDropdown.SetItems(screenModes, selectedMode, OnScreenModeChanged);
            }

            ApplyGraphicsSettings();
        }

        private void LoadLanguageSettings()
        {
            if (uiLanguageDropdown != null)
            {
                List<string> languages = new List<string> { "Korean", "English" };
                string selectedUILanguage = saveData.uiLanguage;

                uiLanguageDropdown.SetItems(languages, selectedUILanguage, OnUILanguageChanged);
            }
        }

        private void LoadVoiceSettings()
        {
            if (saveData == null) return;

            if (serverModeDropdown != null)
            {
                List<string> serverModes = new List<string> { "Online", "Offline" };

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
            if (saveData == null) return;

            Dictionary<string, string> defaultKeys = new Dictionary<string, string>
            {
                { "MoveLeft", "A" },
                { "MoveRight", "D" },
                { "Jump", "W" },
                { "VoiceRecord", "Space" },
                { "SkillPanel", "Tab" }
            };

            if (saveData.keyBindings == null || saveData.keyBindings.Count == 0)
            {
                saveData.keyBindings = new Dictionary<string, string>(defaultKeys);
            }

            foreach (var defaultKey in defaultKeys)
            {
                if (!saveData.keyBindings.ContainsKey(defaultKey.Key))
                {
                    saveData.keyBindings[defaultKey.Key] = defaultKey.Value;
                }
            }

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
                        kvp.Value.text = defaultKeys.ContainsKey(kvp.Key) ? defaultKeys[kvp.Key] : "None";
                    }
                }
            }
        }

        /// <summary>
        /// 패널이 표시될 때 호출
        /// </summary>
        public void OnPanelShown()
        {
            LoadSaveData();
            LoadSettings();
            ShowPanel(audioPanel);
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

        private void ShowPanel(VisualElement panelToShow)
        {
            if (panelToShow == null) return;

            audioButton?.RemoveFromClassList("selected");
            graphicsButton?.RemoveFromClassList("selected");
            languageButton?.RemoveFromClassList("selected");
            gameButton?.RemoveFromClassList("selected");

            if (audioPanel != null) audioPanel.style.display = DisplayStyle.None;
            if (graphicsPanel != null) graphicsPanel.style.display = DisplayStyle.None;
            if (languagePanel != null) languagePanel.style.display = DisplayStyle.None;
            if (gamePanel != null) gamePanel.style.display = DisplayStyle.None;

            panelToShow.style.display = DisplayStyle.Flex;
            currentPanel = panelToShow;

            if (panelToShow == audioPanel)
                audioButton?.AddToClassList("selected");
            else if (panelToShow == graphicsPanel)
                graphicsButton?.AddToClassList("selected");
            else if (panelToShow == languagePanel)
                languageButton?.AddToClassList("selected");
            else if (panelToShow == gamePanel)
                gameButton?.AddToClassList("selected");

            UpdateCurrentPanelTitle();
        }

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
            if (optionsPopup == null) return;

            // Header
            var optionsTitle = optionsPopup.Q<Label>("OptionsPopupTitle");
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
            var micLabel = optionsPopup.Q<Label>("MicrophoneLabel");
            if (micLabel != null)
                micLabel.text = loc.GetText("options_audio_microphone");

            // Graphics Panel labels
            var qualityLabel = optionsPopup.Q<Label>("QualityLabel");
            if (qualityLabel != null)
                qualityLabel.text = loc.GetText("options_graphics_quality");

            var screenModeLabel = optionsPopup.Q<Label>("ScreenModeLabel");
            if (screenModeLabel != null)
                screenModeLabel.text = loc.GetText("options_graphics_screen_mode");

            // Language Panel labels
            var uiLanguageLabel = optionsPopup.Q<Label>("UILanguageLabel");
            if (uiLanguageLabel != null)
                uiLanguageLabel.text = loc.GetText("options_language_ui");

            // Game Panel - Key Bindings 헤더
            var keyBindingsHeader = optionsPopup.Q<Label>("KeyBindingsHeader");
            if (keyBindingsHeader != null)
                keyBindingsHeader.text = loc.GetText("options_game_keybindings");

            // Voice Recognition 헤더
            var voiceRecognitionHeader = optionsPopup.Q<Label>("VoiceRecognitionHeader");
            if (voiceRecognitionHeader != null)
                voiceRecognitionHeader.text = loc.GetText("options_game_voice_recognition");

            // Voice Recognition - 서버 모드 라벨
            var serverModeLabel = optionsPopup.Q<Label>("ServerModeLabel");
            if (serverModeLabel != null)
                serverModeLabel.text = loc.GetText("options_voice_server_mode");

            // 게임 초기화
            var resetGameLabel = optionsPopup.Q<Label>("ResetGameLabel");
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

        public void Dispose()
        {
            microphoneDropdown?.Dispose();
            qualityDropdown?.Dispose();
            screenModeDropdown?.Dispose();
            uiLanguageDropdown?.Dispose();
            serverModeDropdown?.Dispose();

            keyBindingSection?.Dispose();
            voiceRecognitionSection?.Dispose();
        }
    }
}
