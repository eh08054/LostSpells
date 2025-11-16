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

        // Audio 패널 컨트롤
        private DropdownField microphoneDropdown;
        private Button audioResetButton;

        // Language 패널 컨트롤
        private DropdownField uiLanguageDropdown;
        private Button languageResetButton;

        // Game 패널 컨트롤
        private Label keyBindingsHeader;
        private Button keyBindingsToggleButton;
        private VisualElement keyBindingArea;
        private Label voiceRecognitionHeader;
        private Button voiceRecognitionToggleButton;
        private VisualElement voiceRecognitionArea;
        private Label serverStatusLabel;
        private Dictionary<string, Button> keyButtons = new Dictionary<string, Button>();
        private Button gameResetButton;

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

            // AudioListener 중복 체크 및 수정
            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            if (listeners.Length > 1)
            {
                Debug.LogWarning($"[OptionsUI] AudioListener가 {listeners.Length}개 발견됨. Main Camera만 남기고 제거합니다.");

                foreach (var listener in listeners)
                {
                    // Main Camera가 아닌 AudioListener는 제거
                    if (listener.gameObject.name != "Main Camera")
                    {
                        Destroy(listener);
                    }
                }
            }
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
            microphoneDropdown = root.Q<DropdownField>("MicrophoneDropdown");
            audioResetButton = root.Q<Button>("AudioResetButton");

            // Language 패널 컨트롤
            uiLanguageDropdown = root.Q<DropdownField>("UILanguageDropdown");
            languageResetButton = root.Q<Button>("LanguageResetButton");

            // Game 패널 컨트롤
            keyBindingsHeader = root.Q<Label>("KeyBindingsHeader");
            keyBindingsToggleButton = root.Q<Button>("KeyBindingsToggleButton");
            keyBindingArea = root.Q<VisualElement>("KeyBindingArea");
            voiceRecognitionHeader = root.Q<Label>("VoiceRecognitionHeader");
            voiceRecognitionToggleButton = root.Q<Button>("VoiceRecognitionToggleButton");
            voiceRecognitionArea = root.Q<VisualElement>("VoiceRecognitionArea");
            serverStatusLabel = root.Q<Label>("ServerStatusLabel");
            gameResetButton = root.Q<Button>("GameResetButton");

            // 키 버튼들
            keyButtons["MoveLeft"] = root.Q<Button>("MoveLeftKey");
            keyButtons["MoveRight"] = root.Q<Button>("MoveRightKey");
            keyButtons["Jump"] = root.Q<Button>("JumpKey");
            keyButtons["VoiceRecord"] = root.Q<Button>("VoiceRecordKey");
            keyButtons["SkillPanel"] = root.Q<Button>("SkillPanelKey");
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

            // Audio 패널 이벤트
            if (microphoneDropdown != null)
                microphoneDropdown.RegisterValueChangedCallback(evt => OnMicrophoneChanged(evt.newValue));

            if (audioResetButton != null)
                audioResetButton.clicked += OnAudioReset;

            // Language 패널 이벤트
            if (uiLanguageDropdown != null)
                uiLanguageDropdown.RegisterValueChangedCallback(evt => OnUILanguageChanged(evt.newValue));

            if (languageResetButton != null)
                languageResetButton.clicked += OnLanguageReset;

            // Game 패널 이벤트
            if (keyBindingsHeader != null)
                keyBindingsHeader.RegisterCallback<ClickEvent>(evt => ToggleKeyBindingArea());

            if (keyBindingsToggleButton != null)
                keyBindingsToggleButton.clicked += ToggleKeyBindingArea;

            if (voiceRecognitionHeader != null)
                voiceRecognitionHeader.RegisterCallback<ClickEvent>(evt => ToggleVoiceRecognitionArea());

            if (voiceRecognitionToggleButton != null)
                voiceRecognitionToggleButton.clicked += ToggleVoiceRecognitionArea;

            if (gameResetButton != null)
                gameResetButton.clicked += OnGameReset;

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

            // Audio 패널 이벤트 해제
            if (audioResetButton != null)
                audioResetButton.clicked -= OnAudioReset;

            if (languageResetButton != null)
                languageResetButton.clicked -= OnLanguageReset;

            if (gameResetButton != null)
                gameResetButton.clicked -= OnGameReset;
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
                microphoneDropdown.choices = microphones;

                // 저장된 마이크 선택
                if (!string.IsNullOrEmpty(saveData.microphoneDeviceId) && microphones.Contains(saveData.microphoneDeviceId))
                {
                    microphoneDropdown.value = saveData.microphoneDeviceId;
                }
                else
                {
                    microphoneDropdown.value = "Default";
                }
            }
        }

        private void LoadLanguageSettings()
        {
            // UI 언어
            if (uiLanguageDropdown != null)
            {
                uiLanguageDropdown.choices = new List<string> { "Korean", "English" };
                uiLanguageDropdown.value = saveData.uiLanguage;
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

            // Category buttons
            if (audioButton != null)
                audioButton.text = loc.GetText("options_audio");
            if (graphicsButton != null)
                graphicsButton.text = loc.GetText("options_graphics");
            if (languageButton != null)
                languageButton.text = loc.GetText("options_language");
            if (gameButton != null)
                gameButton.text = loc.GetText("options_game");

            // Audio Panel
            var audioPanelTitle = root.Q<Label>("AudioPanelTitle");
            if (audioPanelTitle != null)
                audioPanelTitle.text = loc.GetText("options_audio_title");
            if (audioResetButton != null)
                audioResetButton.text = loc.GetText("options_audio_reset");

            var micLabel = root.Q<Label>("MicrophoneLabel");
            if (micLabel != null)
                micLabel.text = loc.GetText("options_audio_microphone");

            // Graphics Panel
            var graphicsPanelTitle = root.Q<Label>("GraphicsPanelTitle");
            if (graphicsPanelTitle != null)
                graphicsPanelTitle.text = loc.GetText("options_graphics_title");
            var graphicsResetButton = root.Q<Button>("GraphicsResetButton");
            if (graphicsResetButton != null)
                graphicsResetButton.text = loc.GetText("options_graphics_reset");

            // Language Panel
            var languagePanelTitle = root.Q<Label>("LanguagePanelTitle");
            if (languagePanelTitle != null)
                languagePanelTitle.text = loc.GetText("options_language_title");
            if (languageResetButton != null)
                languageResetButton.text = loc.GetText("options_language_reset");

            var uiLanguageLabel = root.Q<Label>("UILanguageLabel");
            if (uiLanguageLabel != null)
                uiLanguageLabel.text = loc.GetText("options_language_ui");

            // Game Panel
            var gamePanelTitle = root.Q<Label>("GamePanelTitle");
            if (gamePanelTitle != null)
                gamePanelTitle.text = loc.GetText("options_game_title");
            if (gameResetButton != null)
                gameResetButton.text = loc.GetText("options_game_reset");

            if (keyBindingsHeader != null)
                keyBindingsHeader.text = loc.GetText("options_game_keybindings");

            if (voiceRecognitionHeader != null)
                voiceRecognitionHeader.text = loc.GetText("options_game_voice_recognition");

            // Key Bindings
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
        }

        /// <summary>
        /// 특정 패널만 표시하고 나머지는 숨김
        /// </summary>
        private void ShowPanel(VisualElement panelToShow)
        {
            if (panelToShow == null) return;

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
                uiLanguageDropdown.value = "Korean";
                LocalizationManager.Instance.SetLanguage(Language.Korean);
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

        private void ToggleKeyBindingArea()
        {
            if (keyBindingArea != null)
            {
                bool isVisible = keyBindingArea.style.display == DisplayStyle.Flex;
                keyBindingArea.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;

                if (keyBindingsToggleButton != null)
                {
                    // section-toggle 클래스가 화살표를 CSS로 처리하므로 rotate만 적용
                    if (isVisible)
                    {
                        keyBindingsToggleButton.RemoveFromClassList("expanded");
                    }
                    else
                    {
                        keyBindingsToggleButton.AddToClassList("expanded");
                    }
                }
            }
        }

        private void ToggleVoiceRecognitionArea()
        {
            if (voiceRecognitionArea != null)
            {
                bool isVisible = voiceRecognitionArea.style.display == DisplayStyle.Flex;
                voiceRecognitionArea.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;

                if (voiceRecognitionToggleButton != null)
                {
                    // section-toggle 클래스가 화살표를 CSS로 처리하므로 rotate만 적용
                    if (isVisible)
                    {
                        voiceRecognitionToggleButton.RemoveFromClassList("expanded");
                    }
                    else
                    {
                        voiceRecognitionToggleButton.AddToClassList("expanded");
                    }
                }
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
    }
}
