using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LostSpells.Data;
using LostSpells.Systems;

namespace LostSpells.UI
{
    /// <summary>
    /// 옵션 UI 컨트롤러
    /// 카테고리 버튼을 눌러 패널 전환
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class OptionsUI : MonoBehaviour
    {
        private UIDocument uiDocument;
        private VisualElement root;

        // SaveManager 참조
        private SaveManager saveManager;
        private PlayerSaveData saveData;

        // 헤더 버튼
        private Button backButton;

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

        // Language 패널 컨트롤
        private CustomDropdown uiLanguageDropdown;

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
        private const float SERVER_CHECK_INTERVAL = 3f; // 3초마다 체크
        private Coroutine serverCheckCoroutine;

        // 키 바인딩 상태
        private bool isWaitingForKey = false;
        private string currentKeyAction = "";

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();

            // SaveManager 싱글톤 인스턴스 가져오기
            saveManager = SaveManager.Instance;
        }

        private void OnEnable()
        {
            root = uiDocument.rootVisualElement;

            // SaveData 가져오기
            LoadSaveData();

            // UI 요소 찾기
            FindUIElements();

            // 이벤트 등록
            RegisterEvents();

            // Localization 이벤트 등록
            LocalizationManager.Instance.OnLanguageChanged += UpdateLocalization;

            // 초기 데이터 로드
            LoadSettings();

            // 현재 언어로 UI 업데이트
            UpdateLocalization();

            // 초기 패널 설정 (Audio 패널 표시)
            ShowPanel(audioPanel);
        }

        private void OnDisable()
        {
            // 서버 체크 중지
            if (serverCheckCoroutine != null)
            {
                StopCoroutine(serverCheckCoroutine);
                serverCheckCoroutine = null;
            }

            // 이벤트 해제
            UnregisterEvents();

            // Localization 이벤트 해제
            UnregisterLocalizationEvents();

            // 설정 저장
            SaveSettings();
        }

        private void OnDestroy()
        {
            UnregisterLocalizationEvents();

            // 드롭다운 정리
            microphoneDropdown?.Dispose();
            uiLanguageDropdown?.Dispose();

            // 접기/펼치기 섹션 정리
            keyBindingSection?.Dispose();
            voiceRecognitionSection?.Dispose();
        }

        private void UnregisterLocalizationEvents()
        {
            if (LocalizationManager.Instance != null)
            {
                try
                {
                    LocalizationManager.Instance.OnLanguageChanged -= UpdateLocalization;
                }
                catch (System.Exception)
                {
                    // 이미 해제된 경우 무시
                }
            }
        }

        private void Update()
        {
            // 키 바인딩 대기 중이면 입력 감지
            if (isWaitingForKey && !string.IsNullOrEmpty(currentKeyAction))
            {
                DetectKeyPress();
            }
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
            // 헤더 버튼
            backButton = root.Q<Button>("BackButton");

            // 통합 패널 헤더
            currentPanelTitle = root.Q<Label>("CurrentPanelTitle");
            currentPanelResetButton = root.Q<Button>("CurrentPanelResetButton");

            // 카테고리 버튼들
            audioButton = root.Q<Button>("AudioButton");
            graphicsButton = root.Q<Button>("GraphicsButton");
            languageButton = root.Q<Button>("LanguageButton");
            gameButton = root.Q<Button>("GameButton");

            // 패널들
            audioPanel = root.Q<VisualElement>("AudioPanel");
            graphicsPanel = root.Q<VisualElement>("GraphicsPanel");
            languagePanel = root.Q<VisualElement>("LanguagePanel");
            gamePanel = root.Q<VisualElement>("GamePanel");

            // Audio 패널 컨트롤
            microphoneDropdown = new CustomDropdown(root, "MicrophoneDropdownContainer", "MicrophoneDropdownButton", "MicrophoneDropdownLabel", "MicrophoneDropdownList");

            // Language 패널 컨트롤
            uiLanguageDropdown = new CustomDropdown(root, "UILanguageDropdownContainer", "UILanguageDropdownButton", "UILanguageDropdownLabel", "UILanguageDropdownList");

            // Game 패널 컨트롤
            keyBindingSection = new CollapsibleSection(root, "KeyBindingsHeader", "KeyBindingsToggleButton", "KeyBindingArea");
            voiceRecognitionSection = new CollapsibleSection(root, "VoiceRecognitionHeader", "VoiceRecognitionToggleButton", "VoiceRecognitionArea");
            serverStatusLabel = root.Q<Label>("ServerStatusLabel");

            // 키 버튼들
            keyButtons["MoveLeft"] = root.Q<Button>("MoveLeftKey");
            keyButtons["MoveRight"] = root.Q<Button>("MoveRightKey");
            keyButtons["Jump"] = root.Q<Button>("JumpKey");
            keyButtons["VoiceRecord"] = root.Q<Button>("VoiceRecordKey");
            keyButtons["SkillPanel"] = root.Q<Button>("SkillPanelKey");

            // 게임 초기화 버튼
            resetGameButton = root.Q<Button>("ResetGameButton");

            // 게임 초기화 확인 팝업
            gameResetConfirmation = root.Q<VisualElement>("GameResetConfirmation");
            confirmResetButton = root.Q<Button>("ConfirmResetButton");
            cancelResetButton = root.Q<Button>("CancelResetButton");
        }

        private void RegisterEvents()
        {
            if (backButton != null)
                backButton.clicked += OnBackButtonClicked;

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

            // 서버 상태 실시간 체크 시작
            serverCheckCoroutine = StartCoroutine(CheckServerStatusLoop());

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

        private void UnregisterEvents()
        {
            if (backButton != null)
                backButton.clicked -= OnBackButtonClicked;

            if (audioButton != null)
                audioButton.clicked -= () => ShowPanel(audioPanel);

            if (graphicsButton != null)
                graphicsButton.clicked -= () => ShowPanel(graphicsPanel);

            if (languageButton != null)
                languageButton.clicked -= () => ShowPanel(languagePanel);

            if (gameButton != null)
                gameButton.clicked -= () => ShowPanel(gamePanel);

            // 통합 리셋 버튼 이벤트 해제
            if (currentPanelResetButton != null)
                currentPanelResetButton.clicked -= OnCurrentPanelReset;

            // 게임 초기화 버튼 이벤트 해제
            if (resetGameButton != null)
                resetGameButton.clicked -= OnResetGameButtonClicked;

            // 게임 초기화 확인 팝업 버튼 이벤트 해제
            if (confirmResetButton != null)
                confirmResetButton.clicked -= OnConfirmResetButtonClicked;

            if (cancelResetButton != null)
                cancelResetButton.clicked -= OnCancelResetButtonClicked;
        }

        private void LoadSettings()
        {
            if (saveData == null) return;

            // Audio 설정 로드
            LoadAudioSettings();

            // Language 설정 로드
            LoadLanguageSettings();

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

            // 음성인식 모델 로드 제거됨 (서버 상태만 표시)
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
                // // Debug.Log("Initialized key bindings with default values.");
            }

            // 누락된 키 바인딩 추가 (기존 저장 데이터에 새로운 키가 추가된 경우 대비)
            foreach (var defaultKey in defaultKeys)
            {
                if (!saveData.keyBindings.ContainsKey(defaultKey.Key))
                {
                    saveData.keyBindings[defaultKey.Key] = defaultKey.Value;
                    // Debug.Log($"Added missing key binding: {defaultKey.Key} = {defaultKey.Value}");
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
                        // 키 바인딩이 없으면 기본값으로 설정
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

        private void SaveSettings()
        {
            if (saveData == null || saveManager == null) return;

            saveManager.SaveGame();
        }

        /// <summary>
        /// 현재 언어로 UI 텍스트 업데이트
        /// </summary>
        private void UpdateLocalization()
        {
            var loc = LocalizationManager.Instance;

            // Header
            var headerTitle = root.Q<Label>("HeaderTitle");
            if (headerTitle != null)
                headerTitle.text = loc.GetText("options_title");

            // 통합 리셋 버튼
            if (currentPanelResetButton != null)
                currentPanelResetButton.text = loc.GetText("options_audio_reset"); // "Reset" 텍스트

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
            var micLabel = root.Q<Label>("MicrophoneLabel");
            if (micLabel != null)
                micLabel.text = loc.GetText("options_audio_microphone");

            // Language Panel labels
            var uiLanguageLabel = root.Q<Label>("UILanguageLabel");
            if (uiLanguageLabel != null)
                uiLanguageLabel.text = loc.GetText("options_language_ui");

            // Game Panel - Key Bindings 헤더
            var keyBindingsHeader = root.Q<Label>("KeyBindingsHeader");
            if (keyBindingsHeader != null)
                keyBindingsHeader.text = loc.GetText("options_game_keybindings");

            // Voice Recognition 헤더
            var voiceRecognitionHeader = root.Q<Label>("VoiceRecognitionHeader");
            if (voiceRecognitionHeader != null)
                voiceRecognitionHeader.text = loc.GetText("options_game_voice_recognition");

            // Key Bindings
            var keyBindingArea = root.Q<VisualElement>("KeyBindingArea");
            var keyBindingLabels = keyBindingArea?.Query<Label>("keybinding-label").ToList();
            if (keyBindingLabels != null)
            {
                foreach (var label in keyBindingLabels)
                {
                    if (label.text.Contains("Move Left") || label.text.Contains("왼쪽 이동"))
                        label.text = loc.GetText("options_keybinding_move_left");
                    else if (label.text.Contains("Move Right") || label.text.Contains("오른쪽 이동"))
                        label.text = loc.GetText("options_keybinding_move_right");
                    else if (label.text.Contains("Jump") || label.text.Contains("점프"))
                        label.text = loc.GetText("options_keybinding_jump");
                    else if (label.text.Contains("Voice Recording") || label.text.Contains("음성 녹음"))
                        label.text = loc.GetText("options_keybinding_voice_record");
                    else if (label.text.Contains("Skill Panel") || label.text.Contains("스킬 패널"))
                        label.text = loc.GetText("options_keybinding_skill_panel");
                }
            }

            var inGameTitle = keyBindingArea?.Q<Label>("scene-title");
            if (inGameTitle != null)
                inGameTitle.text = loc.GetText("options_keybinding_ingame");

            // Voice Recognition - 서버 상태 라벨
            var voiceRecognitionArea = root.Q<VisualElement>("VoiceRecognitionArea");
            var serverStatusLabelText = voiceRecognitionArea?.Q<Label>("setting-label");
            if (serverStatusLabelText != null)
            {
                // "Voice Server Status:" 라벨 찾기
                var labels = voiceRecognitionArea.Query<Label>("setting-label").ToList();
                if (labels != null && labels.Count > 0)
                {
                    // 첫 번째 라벨이 "Voice Server Status:" 라벨
                    labels[0].text = loc.GetText("options_voice_server_status");
                }
            }

            // 게임 초기화
            var resetGameLabel = root.Q<Label>("ResetGameLabel");
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
            if (audioPanel != null)
                audioPanel.style.display = DisplayStyle.None;

            if (graphicsPanel != null)
                graphicsPanel.style.display = DisplayStyle.None;

            if (languagePanel != null)
                languagePanel.style.display = DisplayStyle.None;

            if (gamePanel != null)
                gamePanel.style.display = DisplayStyle.None;

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
        /// 통합 리셋 버튼 클릭 시 현재 패널에 따라 리셋 처리
        /// </summary>
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


        // Audio 패널 이벤트 핸들러
        private void OnMicrophoneChanged(string value)
        {
            if (saveData != null)
            {
                saveData.microphoneDeviceId = value;
            }
        }

        private void OnAudioReset()
        {
            if (saveData != null)
            {
                saveData.microphoneDeviceId = "";
                LoadAudioSettings();
            }
        }

        private void OnGraphicsReset()
        {
            // Graphics 패널 리셋 기능 (필요시 구현)
            // 현재는 빈 메서드로 유지
        }

        // Language 패널 이벤트 핸들러
        private void OnUILanguageChanged(string value)
        {
            if (saveData != null)
            {
                saveData.uiLanguage = value;

                // LocalizationManager에 언어 변경 적용
                Language language = value == "Korean" ? Language.Korean : Language.English;
                LocalizationManager.Instance.SetLanguage(language);
            }
        }

        private void OnLanguageReset()
        {
            if (saveData != null && uiLanguageDropdown != null)
            {
                saveData.uiLanguage = "Korean";
                uiLanguageDropdown.SetValue("Korean");
                LocalizationManager.Instance.SetLanguage(Language.Korean);

                // 드롭다운 항목 업데이트
                LoadLanguageSettings();
            }
        }

        private void OnGameReset()
        {
            if (saveData != null)
            {
                // 키 바인딩 초기화
                saveData.keyBindings = new Dictionary<string, string>
                {
                    { "MoveLeft", "A" },
                    { "MoveRight", "D" },
                    { "Jump", "W" },
                    { "VoiceRecord", "Space" },
                    { "SkillPanel", "Tab" }
                };

                LoadKeyBindings();
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

        private void DetectKeyPress()
        {
            if (Keyboard.current == null) return;

            foreach (var key in System.Enum.GetValues(typeof(Key)))
            {
                Key k = (Key)key;

                // Skip None and IMESelected as they are not valid keyboard keys
                if (k == Key.None || k == Key.IMESelected)
                    continue;

                try
                {
                    if (Keyboard.current[k].wasPressedThisFrame && k != Key.Escape)
                    {
                        string keyName = GetKeyDisplayName(k);

                        if (saveData != null && saveData.keyBindings != null)
                        {
                            saveData.keyBindings[currentKeyAction] = keyName;
                        }

                        if (keyButtons.ContainsKey(currentKeyAction))
                        {
                            keyButtons[currentKeyAction].text = keyName;
                        }

                        isWaitingForKey = false;
                        currentKeyAction = "";
                        return;
                    }
                }
                catch (System.ArgumentOutOfRangeException)
                {
                    // Skip invalid keys that are not supported by Keyboard
                    continue;
                }
            }

            if (Keyboard.current[Key.Escape].wasPressedThisFrame)
            {
                LoadKeyBindings();
                isWaitingForKey = false;
                currentKeyAction = "";
            }
        }

        private string GetKeyDisplayName(Key key)
        {
            return key.ToString();
        }

        private void OnBackButtonClicked()
        {
            // Options 씬이 Additive로 로드되었는지 확인
            bool isLoadedAdditively = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name == "Options" && SceneManager.sceneCount > 1)
                {
                    isLoadedAdditively = true;
                    break;
                }
            }

            if (isLoadedAdditively)
            {
                Time.timeScale = 1f;
                SceneManager.UnloadSceneAsync("Options");
            }
            else
            {
                string previousScene = SceneNavigationManager.Instance.GetPreviousScene();
                SceneManager.LoadScene(previousScene);
            }
        }

        #region Server Status Check

        private IEnumerator CheckServerStatusLoop()
        {
            while (true)
            {
                yield return StartCoroutine(CheckServerStatus());
                yield return new WaitForSeconds(SERVER_CHECK_INTERVAL);
            }
        }

        private IEnumerator CheckServerStatus()
        {
            using (UnityWebRequest request = UnityWebRequest.Get($"{SERVER_URL}/"))
            {
                request.timeout = 2; // 2초 타임아웃

                yield return request.SendWebRequest();

                if (serverStatusLabel != null)
                {
                    var loc = LocalizationManager.Instance;
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        serverStatusLabel.text = loc.GetText("options_voice_server_connected");
                        serverStatusLabel.style.color = new Color(0.2f, 0.8f, 0.2f); // 초록색
                    }
                    else
                    {
                        serverStatusLabel.text = loc.GetText("options_voice_server_disconnected");
                        serverStatusLabel.style.color = new Color(0.8f, 0.2f, 0.2f); // 빨간색
                    }
                }
            }
        }

        #endregion

        #region Game Reset

        private void OnResetGameButtonClicked()
        {
            // 게임 초기화 확인 팝업 표시
            ShowGameResetConfirmation();
        }

        private void OnConfirmResetButtonClicked()
        {
            // SaveManager를 통해 게임 데이터 초기화
            if (Data.SaveManager.Instance != null)
            {
                Data.SaveManager.Instance.ResetSaveData();

                // 현재 saveData 참조도 새로 불러오기
                saveData = Data.SaveManager.Instance.GetCurrentSaveData();

                // UI에 초기화된 설정 반영
                LoadSettings();
            }

            // 팝업 닫기
            HideGameResetConfirmation();
        }

        private void OnCancelResetButtonClicked()
        {
            // 팝업 닫기
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

        #endregion
    }
}
