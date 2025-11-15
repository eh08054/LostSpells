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
        private DropdownField voiceLanguageDropdown;
        private DropdownField voiceModelDropdown;
        private Label serverStatusLabel;
        private Button checkServerButton;
        private Button downloadModelButton;
        private Button deleteModelButton;
        private Label modelStatusLabel;
        private Button languageResetButton;

        // Game 패널 컨트롤
        private Label keyBindingsHeader;
        private Button keyBindingsToggleButton;
        private VisualElement keyBindingArea;
        private Label voiceRecognitionHeader;
        private Button voiceRecognitionToggleButton;
        private VisualElement voiceRecognitionArea;
        private Dictionary<string, Button> keyButtons = new Dictionary<string, Button>();
        private Button gameResetButton;

        // 키 바인딩 상태
        private bool isWaitingForKey = false;
        private string currentKeyAction = "";

        // 다운로드 상태
        private bool isDownloading = false;
        private string downloadingModel = "";

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
            voiceLanguageDropdown = root.Q<DropdownField>("VoiceLanguageDropdown");
            voiceModelDropdown = root.Q<DropdownField>("VoiceModelDropdown");
            serverStatusLabel = root.Q<Label>("ServerStatusLabel");
            checkServerButton = root.Q<Button>("CheckServerButton");
            downloadModelButton = root.Q<Button>("DownloadModelButton");
            deleteModelButton = root.Q<Button>("DeleteModelButton");
            modelStatusLabel = root.Q<Label>("ModelStatusLabel");
            languageResetButton = root.Q<Button>("LanguageResetButton");

            // Game 패널 컨트롤
            keyBindingsHeader = root.Q<Label>("KeyBindingsHeader");
            keyBindingsToggleButton = root.Q<Button>("KeyBindingsToggleButton");
            keyBindingArea = root.Q<VisualElement>("KeyBindingArea");
            voiceRecognitionHeader = root.Q<Label>("VoiceRecognitionHeader");
            voiceRecognitionToggleButton = root.Q<Button>("VoiceRecognitionToggleButton");
            voiceRecognitionArea = root.Q<VisualElement>("VoiceRecognitionArea");
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

            if (voiceLanguageDropdown != null)
                voiceLanguageDropdown.RegisterValueChangedCallback(evt => OnVoiceLanguageChanged(evt.newValue));

            if (voiceModelDropdown != null)
                voiceModelDropdown.RegisterValueChangedCallback(evt => OnVoiceModelChanged(evt.newValue));

            if (checkServerButton != null)
                checkServerButton.clicked += OnCheckServerClicked;

            if (downloadModelButton != null)
                downloadModelButton.clicked += OnDownloadModelClicked;

            if (deleteModelButton != null)
                deleteModelButton.clicked += OnDeleteModelClicked;

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

            // 음성인식 언어
            if (voiceLanguageDropdown != null)
            {
                voiceLanguageDropdown.choices = new List<string>
                {
                    "ko (Korean)",
                    "en (English)"
                };

                // saveData의 언어 코드를 표시 형식으로 변환
                string displayValue = ConvertLanguageCodeToDisplay(saveData.voiceRecognitionLanguage);
                voiceLanguageDropdown.value = displayValue;
            }

            // 음성인식 모델
            LoadVoiceModelSettings();
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

            // Voice Recognition
            var voiceLabels = voiceRecognitionArea?.Query<Label>("setting-label").ToList();
            if (voiceLabels != null)
            {
                foreach (var label in voiceLabels)
                {
                    if (label.text.Contains("Server Status") || label.text.Contains("서버 상태"))
                        label.text = loc.GetText("options_voice_server_status");
                    else if (label.text.Contains("Voice Recognition Language") || label.text.Contains("음성 인식 언어") || label.text.Contains("음성인식 언어"))
                        label.text = loc.GetText("options_voice_language");
                    else if (label.text.Contains("Voice Recognition Model") || label.text.Contains("음성 인식 모델") || label.text.Contains("음성인식 모델"))
                        label.text = loc.GetText("options_voice_model");
                    else if (label.text.Contains("Model Status") || label.text.Contains("모델 상태"))
                        label.text = loc.GetText("options_voice_model_status");
                }
            }

            if (checkServerButton != null)
                checkServerButton.text = loc.GetText("options_voice_server_check");
            if (downloadModelButton != null)
                downloadModelButton.text = loc.GetText("options_voice_model_download");
            if (deleteModelButton != null)
                deleteModelButton.text = loc.GetText("options_voice_model_delete");
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

        private void OnVoiceLanguageChanged(string value)
        {
            if (saveData != null)
            {
                // 표시 형식을 언어 코드로 변환
                string languageCode = ConvertDisplayToLanguageCode(value);
                saveData.voiceRecognitionLanguage = languageCode;

                // 게임 중이라면 VoiceRecognitionManager에 즉시 적용
                var voiceRecognitionManager = FindFirstObjectByType<LostSpells.Systems.VoiceRecognitionManager>();
                if (voiceRecognitionManager != null)
                {
                    voiceRecognitionManager.ChangeLanguage(languageCode);
                    // Debug.Log($"[OptionsUI] 음성인식 언어 즉시 적용: {languageCode}");
                }
            }
        }

        private void LoadVoiceModelSettings()
        {
            if (voiceModelDropdown == null || modelStatusLabel == null)
                return;

            // 모델 상태 표시
            modelStatusLabel.text = "Loading...";

            // 드롭다운에 모델 목록 설정 (기본값)
            var modelChoices = new List<string> { "tiny", "base", "small", "medium", "large-v3" };
            voiceModelDropdown.choices = modelChoices;
            voiceModelDropdown.value = saveData.voiceRecognitionModel;

            // 서버에서 모델 정보 가져오기
            StartCoroutine(GetModelsFromServer());
        }

        private IEnumerator GetModelsFromServer()
        {
            string serverUrl = "http://localhost:8000";

            // Debug.Log($"[OptionsUI] Attempting to connect to server: {serverUrl}/models");

            using (UnityWebRequest request = UnityWebRequest.Get($"{serverUrl}/models"))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = request.downloadHandler.text;
                    // Debug.Log($"[OptionsUI] Server connected successfully. Models response: {jsonResponse}");

                    // 서버 연결 상태 업데이트
                    if (serverStatusLabel != null)
                    {
                        serverStatusLabel.text = "Connected";
                        // Debug.Log("[OptionsUI] Server status set to 'Connected'");
                    }

                    var modelsInfo = JsonUtility.FromJson<LostSpells.Systems.ModelsInfo>(jsonResponse);
                    if (modelsInfo != null)
                    {
                        UpdateModelStatus(modelsInfo.current_model);
                        // Debug.Log($"[OptionsUI] Updated model status to: {modelsInfo.current_model}");
                    }
                    else
                    {
                        Debug.LogWarning("[OptionsUI] Failed to parse ModelsInfo JSON");
                        if (modelStatusLabel != null)
                        {
                            modelStatusLabel.text = "Server error";
                            // Debug.Log("[OptionsUI] Status label set to 'Server error'");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[OptionsUI] Failed to connect to server. Error: {request.error}, Result: {request.result}");

                    // 서버 연결 상태 업데이트
                    if (serverStatusLabel != null)
                    {
                        serverStatusLabel.text = "Not connected";
                        // Debug.Log("[OptionsUI] Server status set to 'Not connected'");
                    }

                    if (modelStatusLabel != null)
                    {
                        modelStatusLabel.text = "Server not available";
                        // Debug.Log("[OptionsUI] Status label set to 'Server not available'");
                    }
                    else
                    {
                        Debug.LogError("[OptionsUI] modelStatusLabel is null!");
                    }
                }
            }
        }

        private void UpdateModelStatus(string currentModel)
        {
            if (modelStatusLabel == null)
                return;

            // 현재 선택된 모델의 다운로드 상태 확인
            if (voiceModelDropdown != null && !string.IsNullOrEmpty(voiceModelDropdown.value))
            {
                StartCoroutine(CheckModelDownloadStatus(voiceModelDropdown.value));
            }
            else if (string.IsNullOrEmpty(currentModel) || currentModel == "none")
            {
                modelStatusLabel.text = "No model loaded";
            }
            else
            {
                modelStatusLabel.text = $"Current: {currentModel}";
            }
        }

        private IEnumerator CheckModelDownloadStatus(string modelSize)
        {
            string serverUrl = "http://localhost:8000";

            using (UnityWebRequest request = UnityWebRequest.Get($"{serverUrl}/models/{modelSize}/status"))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = request.downloadHandler.text;

                    try
                    {
                        // status 값 추출
                        int statusStart = jsonResponse.IndexOf("\"status\":");
                        if (statusStart >= 0)
                        {
                            int stringStart = jsonResponse.IndexOf("\"", statusStart + "\"status\":".Length);
                            int stringEnd = jsonResponse.IndexOf("\"", stringStart + 1);

                            string status = jsonResponse.Substring(stringStart + 1, stringEnd - stringStart - 1);

                            if (modelStatusLabel != null)
                            {
                                if (status == "downloaded")
                                {
                                    modelStatusLabel.text = "Downloaded";
                                }
                                else if (status == "downloading")
                                {
                                    // 진행률 추출
                                    int progressStart = jsonResponse.IndexOf("\"download_progress\":");
                                    if (progressStart >= 0)
                                    {
                                        int numberStart = progressStart + "\"download_progress\":".Length;
                                        int numberEnd = jsonResponse.IndexOf(",", numberStart);
                                        if (numberEnd < 0) numberEnd = jsonResponse.IndexOf("}", numberStart);

                                        string progressStr = jsonResponse.Substring(numberStart, numberEnd - numberStart).Trim();
                                        if (float.TryParse(progressStr, out float progress))
                                        {
                                            modelStatusLabel.text = $"Downloading {progress:F0}%";
                                        }
                                    }
                                }
                                else
                                {
                                    modelStatusLabel.text = "Not downloaded";
                                }
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[OptionsUI] Failed to parse status: {e.Message}");
                        if (modelStatusLabel != null)
                        {
                            modelStatusLabel.text = "Status unknown";
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[OptionsUI] Failed to get model status: {request.error}");
                    if (modelStatusLabel != null)
                    {
                        modelStatusLabel.text = "Server error";
                    }
                }
            }
        }

        private void OnVoiceModelChanged(string value)
        {
            if (saveData == null || string.IsNullOrEmpty(value))
                return;

            // 저장 데이터 업데이트
            saveData.voiceRecognitionModel = value;

            // Debug.Log($"[OptionsUI] Voice model changed to: {value}");

            // 서버에 모델 선택 요청
            if (modelStatusLabel != null)
                modelStatusLabel.text = "Changing model...";

            StartCoroutine(SelectModelOnServer(value));
        }

        private IEnumerator SelectModelOnServer(string modelSize)
        {
            string serverUrl = "http://localhost:8000";

            WWWForm form = new WWWForm();
            form.AddField("model_size", modelSize);

            using (UnityWebRequest request = UnityWebRequest.Post($"{serverUrl}/models/select", form))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    // Debug.Log($"[OptionsUI] Model successfully changed to: {modelSize}");
                    UpdateModelStatus(modelSize);
                }
                else
                {
                    Debug.LogError($"[OptionsUI] Failed to change model to: {modelSize} - {request.error}");
                    if (modelStatusLabel != null)
                        modelStatusLabel.text = "Failed to change model";
                }
            }
        }

        private void OnDownloadModelClicked()
        {
            if (voiceModelDropdown == null || saveData == null)
                return;

            string selectedModel = voiceModelDropdown.value;
            if (string.IsNullOrEmpty(selectedModel))
            {
                Debug.LogWarning("[OptionsUI] No model selected for download");
                return;
            }

            // 이미 다운로드 중이면 무시
            if (isDownloading)
            {
                Debug.LogWarning("[OptionsUI] Download already in progress");
                return;
            }

            // 다운로드 상태 설정
            isDownloading = true;
            downloadingModel = selectedModel;

            // 다운로드 버튼 비활성화
            if (downloadModelButton != null)
                downloadModelButton.SetEnabled(false);

            if (modelStatusLabel != null)
                modelStatusLabel.text = "Downloading 0%";

            // Debug.Log($"[OptionsUI] Starting download for model: {selectedModel}");

            // 서버에 모델 다운로드 요청 및 진행률 폴링 시작
            StartCoroutine(DownloadModelFromServer(selectedModel));
            StartCoroutine(PollDownloadProgress(selectedModel));
        }

        private IEnumerator DownloadModelFromServer(string modelSize)
        {
            string serverUrl = "http://localhost:8000";

            WWWForm form = new WWWForm();
            form.AddField("model_size", modelSize);

            using (UnityWebRequest request = UnityWebRequest.Post($"{serverUrl}/models/download", form))
            {
                // 모델 다운로드는 시간이 오래 걸릴 수 있으므로 타임아웃 연장
                request.timeout = 600; // 10분

                yield return request.SendWebRequest();

                // 다운로드 상태 해제
                isDownloading = false;
                downloadingModel = "";

                // 다운로드 버튼 다시 활성화
                if (downloadModelButton != null)
                    downloadModelButton.SetEnabled(true);

                if (request.result == UnityWebRequest.Result.Success)
                {
                    // Debug.Log($"[OptionsUI] Model {modelSize} downloaded successfully");
                    UpdateModelStatus(modelSize);
                }
                else
                {
                    Debug.LogError($"[OptionsUI] Failed to download model: {modelSize} - {request.error}");
                    if (modelStatusLabel != null)
                        modelStatusLabel.text = "Download failed";
                }
            }
        }

        private IEnumerator PollDownloadProgress(string modelSize)
        {
            string serverUrl = "http://localhost:8000";

            // Debug.Log($"[OptionsUI] Starting progress polling for model: {modelSize}");

            // 다운로드가 완료될 때까지 주기적으로 진행률 확인
            while (isDownloading && downloadingModel == modelSize)
            {
                using (UnityWebRequest request = UnityWebRequest.Get($"{serverUrl}/models/{modelSize}/status"))
                {
                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string jsonResponse = request.downloadHandler.text;

                        // JSON 파싱 (간단한 파싱)
                        try
                        {
                            // download_progress 값 추출
                            int progressStart = jsonResponse.IndexOf("\"download_progress\":");
                            if (progressStart >= 0)
                            {
                                int numberStart = progressStart + "\"download_progress\":".Length;
                                int numberEnd = jsonResponse.IndexOf(",", numberStart);
                                if (numberEnd < 0) numberEnd = jsonResponse.IndexOf("}", numberStart);

                                string progressStr = jsonResponse.Substring(numberStart, numberEnd - numberStart).Trim();
                                if (float.TryParse(progressStr, out float progress))
                                {
                                    if (modelStatusLabel != null)
                                    {
                                        modelStatusLabel.text = $"Downloading {progress:F0}%";
                                        // Debug.Log($"[OptionsUI] Download progress: {progress}%");
                                    }

                                    // 진행률이 100이면 다운로드 완료
                                    if (progress >= 100f)
                                    {
                                        // Debug.Log($"[OptionsUI] Download completed: {modelSize}");
                                        if (modelStatusLabel != null)
                                            modelStatusLabel.text = "Downloaded";
                                        break;
                                    }
                                }
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"[OptionsUI] Failed to parse progress: {e.Message}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[OptionsUI] Failed to get progress: {request.error}");
                    }
                }

                // 1초 대기 후 다시 확인
                yield return new WaitForSeconds(1f);
            }

            // Debug.Log($"[OptionsUI] Progress polling stopped for model: {modelSize}");
        }

        private IEnumerator UpdateModelDownloadProgress(string modelSize)
        {
            string serverUrl = "http://localhost:8000";

            // Debug.Log($"[OptionsUI] Checking download progress for model: {modelSize}");

            using (UnityWebRequest request = UnityWebRequest.Get($"{serverUrl}/models/{modelSize}/status"))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = request.downloadHandler.text;

                    try
                    {
                        // download_progress 값 추출
                        int progressStart = jsonResponse.IndexOf("\"download_progress\":");
                        if (progressStart >= 0)
                        {
                            int numberStart = progressStart + "\"download_progress\":".Length;
                            int numberEnd = jsonResponse.IndexOf(",", numberStart);
                            if (numberEnd < 0) numberEnd = jsonResponse.IndexOf("}", numberStart);

                            string progressStr = jsonResponse.Substring(numberStart, numberEnd - numberStart).Trim();
                            if (float.TryParse(progressStr, out float progress))
                            {
                                // 진행률이 0이면 상태 확인
                                int statusStart = jsonResponse.IndexOf("\"status\":");
                                if (statusStart >= 0)
                                {
                                    int stringStart = jsonResponse.IndexOf("\"", statusStart + "\"status\":".Length);
                                    int stringEnd = jsonResponse.IndexOf("\"", stringStart + 1);
                                    string status = jsonResponse.Substring(stringStart + 1, stringEnd - stringStart - 1);

                                    if (modelStatusLabel != null)
                                    {
                                        if (status == "downloaded")
                                        {
                                            modelStatusLabel.text = "Downloaded";
                                        }
                                        else if (status == "downloading")
                                        {
                                            modelStatusLabel.text = $"Downloading {progress:F0}%";
                                        }
                                        else
                                        {
                                            modelStatusLabel.text = "Not downloaded";
                                        }
                                    }
                                }

                                // Debug.Log($"[OptionsUI] Model {modelSize} download progress: {progress}%");
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[OptionsUI] Failed to parse progress: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[OptionsUI] Failed to get model status: {request.error}");
                    // 서버에서 정보를 가져올 수 없으면 모델 상태를 서버 에러로 설정
                    if (modelStatusLabel != null)
                    {
                        modelStatusLabel.text = "Server error";
                    }
                }
            }
        }

        private void OnCheckServerClicked()
        {
            // Debug.Log("[OptionsUI] Check server button clicked");

            if (serverStatusLabel != null)
                serverStatusLabel.text = "Checking...";

            if (modelStatusLabel != null)
                modelStatusLabel.text = "Checking server...";

            StartCoroutine(GetModelsFromServer());

            // 현재 선택된 모델의 다운로드 진행률 확인
            if (voiceModelDropdown != null && !string.IsNullOrEmpty(voiceModelDropdown.value))
            {
                StartCoroutine(UpdateModelDownloadProgress(voiceModelDropdown.value));
            }
        }

        private void OnDeleteModelClicked()
        {
            if (voiceModelDropdown == null || saveData == null)
                return;

            string selectedModel = voiceModelDropdown.value;
            if (string.IsNullOrEmpty(selectedModel))
            {
                Debug.LogWarning("[OptionsUI] No model selected for deletion");
                return;
            }

            // Debug.Log($"[OptionsUI] Deleting model: {selectedModel}");

            if (modelStatusLabel != null)
                modelStatusLabel.text = $"Deleting {selectedModel}...";

            StartCoroutine(DeleteModelFromServer(selectedModel));
        }

        private IEnumerator DeleteModelFromServer(string modelSize)
        {
            string serverUrl = "http://localhost:8000";

            using (UnityWebRequest request = UnityWebRequest.Delete($"{serverUrl}/models/{modelSize}"))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    // Debug.Log($"[OptionsUI] Model {modelSize} deleted successfully");
                    if (modelStatusLabel != null)
                        modelStatusLabel.text = "Not downloaded";
                }
                else
                {
                    Debug.LogError($"[OptionsUI] Failed to delete model: {modelSize} - {request.error}");
                    if (modelStatusLabel != null)
                        modelStatusLabel.text = "Delete failed";
                }
            }
        }

        private void OnLanguageReset()
        {
            if (saveData != null)
            {
                saveData.uiLanguage = "Korean";
                saveData.voiceRecognitionLanguage = "ko";
                saveData.voiceRecognitionModel = "base";
                LoadLanguageSettings();

                // 게임 중이라면 VoiceRecognitionManager도 리셋
                var voiceRecognitionManager = FindFirstObjectByType<LostSpells.Systems.VoiceRecognitionManager>();
                if (voiceRecognitionManager != null)
                {
                    voiceRecognitionManager.ChangeLanguage("ko");
                    // Debug.Log("[OptionsUI] 음성인식 언어 기본값으로 리셋: ko");
                }

                // 서버에 모델 리셋 요청
                StartCoroutine(SelectModelOnServer("base"));
            }
        }

        // Game 패널 이벤트 핸들러
        private void ToggleKeyBindingArea()
        {
            if (keyBindingArea == null) return;

            // 토글: 현재 표시되고 있으면 숨기고, 숨겨져 있으면 표시
            bool isVisible = keyBindingArea.style.display == DisplayStyle.Flex;

            if (isVisible)
            {
                // 키 바인딩 대기 중이면 취소하고 원래 키로 복원
                if (isWaitingForKey && !string.IsNullOrEmpty(currentKeyAction))
                {
                    LoadKeyBindings();
                }

                keyBindingArea.style.display = DisplayStyle.None;
                isWaitingForKey = false;
                currentKeyAction = "";

                // 버튼 회전: 0도 (오른쪽 화살표)
                if (keyBindingsToggleButton != null)
                    keyBindingsToggleButton.style.rotate = new StyleRotate(new Rotate(0));

                // Debug.Log("Key binding area collapsed.");
            }
            else
            {
                keyBindingArea.style.display = DisplayStyle.Flex;

                // 버튼 회전: 90도 (아래쪽 화살표)
                if (keyBindingsToggleButton != null)
                    keyBindingsToggleButton.style.rotate = new StyleRotate(new Rotate(90));

                // Debug.Log("Key binding area expanded.");
            }
        }

        private void ToggleVoiceRecognitionArea()
        {
            if (voiceRecognitionArea == null) return;

            // 토글: 현재 표시되고 있으면 숨기고, 숨겨져 있으면 표시
            bool isVisible = voiceRecognitionArea.style.display == DisplayStyle.Flex;
            voiceRecognitionArea.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;

            // 펼칠 때 서버 상태 새로고침
            if (!isVisible)
            {
                // 버튼 회전: 90도 (아래쪽 화살표)
                if (voiceRecognitionToggleButton != null)
                    voiceRecognitionToggleButton.style.rotate = new StyleRotate(new Rotate(90));

                // Debug.Log("[OptionsUI] Voice recognition area expanded. Refreshing server status...");

                // 즉시 상태 표시를 "Checking..."로 변경하여 이전 캐시된 상태 제거
                if (serverStatusLabel != null)
                {
                    serverStatusLabel.text = "Checking...";
                    // Debug.Log("[OptionsUI] Server status label reset to 'Checking...'");
                }

                if (modelStatusLabel != null)
                {
                    modelStatusLabel.text = "Checking server...";
                    // Debug.Log("[OptionsUI] Model status label reset to 'Checking server...'");
                }

                StartCoroutine(GetModelsFromServer());

                // 현재 선택된 모델의 다운로드 진행률 확인
                if (voiceModelDropdown != null && !string.IsNullOrEmpty(voiceModelDropdown.value))
                {
                    StartCoroutine(UpdateModelDownloadProgress(voiceModelDropdown.value));
                }
            }
            else
            {
                // 버튼 회전: 0도 (오른쪽 화살표)
                if (voiceRecognitionToggleButton != null)
                    voiceRecognitionToggleButton.style.rotate = new StyleRotate(new Rotate(0));

                // Debug.Log("[OptionsUI] Voice recognition area collapsed.");
            }
        }

        private void OnKeyButtonClicked(string action)
        {
            if (Keyboard.current == null)
            {
                Debug.LogError("Input System is not available. Cannot change key bindings.");
                return;
            }

            if (string.IsNullOrEmpty(action))
            {
                Debug.LogError("Invalid action name.");
                return;
            }

            // 이전에 대기 중이던 키 복원
            if (isWaitingForKey && !string.IsNullOrEmpty(currentKeyAction))
            {
                LoadKeyBindings();
            }

            isWaitingForKey = true;
            currentKeyAction = action;

            // 버튼 텍스트를 "Press a key..." 로 변경
            if (keyButtons.ContainsKey(action) && keyButtons[action] != null)
            {
                keyButtons[action].text = "Press a key...";
            }

            // Debug.Log($"Waiting for key input for action: {action}");
        }

        private void OnGameReset()
        {
            if (saveData == null)
            {
                Debug.LogError("SaveData is null. Cannot reset key bindings.");
                return;
            }

            // 키 바인딩 대기 중이면 취소
            if (isWaitingForKey)
            {
                isWaitingForKey = false;
                currentKeyAction = "";
            }

            saveData.keyBindings = new Dictionary<string, string>
            {
                { "MoveLeft", "A" },
                { "MoveRight", "D" },
                { "Jump", "W" },
                { "VoiceRecord", "Space" },
                { "SkillPanel", "Tab" }
            };
            LoadKeyBindings();
            // Debug.Log("Key bindings reset to default values.");
        }

        private void DetectKeyPress()
        {
            if (Keyboard.current == null)
            {
                Debug.LogWarning("Keyboard.current is null. Input System may not be enabled.");
                return;
            }

            if (string.IsNullOrEmpty(currentKeyAction))
            {
                isWaitingForKey = false;
                return;
            }

            try
            {
                // 모든 키 체크
                foreach (var keyValue in System.Enum.GetValues(typeof(Key)))
                {
                    Key keyCode = (Key)keyValue;

                    // None 키는 건너뛰기
                    if (keyCode == Key.None)
                        continue;

                    try
                    {
                        var keyControl = Keyboard.current[keyCode];
                        if (keyControl == null)
                            continue;

                        if (keyControl.wasPressedThisFrame)
                        {
                            // Escape는 취소로 사용
                            if (keyCode == Key.Escape)
                            {
                                isWaitingForKey = false;
                                currentKeyAction = "";
                                LoadKeyBindings(); // 원래 키로 복원
                                return;
                            }

                            // 키 바인딩 저장
                            string keyName = GetKeyDisplayName(keyCode);
                            if (saveData != null && saveData.keyBindings != null)
                            {
                                saveData.keyBindings[currentKeyAction] = keyName;
                            }

                            // 버튼 텍스트 업데이트
                            if (keyButtons.ContainsKey(currentKeyAction) && keyButtons[currentKeyAction] != null)
                            {
                                keyButtons[currentKeyAction].text = keyName;
                            }

                            isWaitingForKey = false;
                            currentKeyAction = "";
                            break;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        // 특정 키에 대한 접근 오류는 무시하고 계속 진행
                        Debug.LogWarning($"Error accessing key {keyCode}: {ex.Message}");
                        continue;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in DetectKeyPress: {ex.Message}");
                isWaitingForKey = false;
                currentKeyAction = "";
                LoadKeyBindings();
            }
        }

        private string GetKeyDisplayName(Key key)
        {
            // 특수 키는 읽기 쉬운 이름으로 변환
            switch (key)
            {
                case Key.Space: return "Space";
                case Key.LeftShift: return "LShift";
                case Key.RightShift: return "RShift";
                case Key.LeftCtrl: return "LCtrl";
                case Key.RightCtrl: return "RCtrl";
                case Key.LeftAlt: return "LAlt";
                case Key.RightAlt: return "RAlt";
                case Key.Tab: return "Tab";
                case Key.Enter: return "Enter";
                case Key.Backspace: return "Backspace";
                default: return key.ToString();
            }
        }

        private string ConvertLanguageCodeToDisplay(string code)
        {
            switch (code)
            {
                case "ko": return "ko (Korean)";
                case "en": return "en (English)";
                default: return "ko (Korean)";
            }
        }

        private string ConvertDisplayToLanguageCode(string display)
        {
            if (display.StartsWith("ko")) return "ko";
            if (display.StartsWith("en")) return "en";
            return "ko";
        }

        private void OnBackButtonClicked()
        {
            SaveSettings();

            // Additive로 로드되었는지 확인 (씬이 여러 개면 Additive 로드)
            if (SceneManager.sceneCount > 1)
            {
                // InGame에서 Additive로 로드된 경우 - 현재 씬만 언로드하고 게임 재개
                Time.timeScale = 1f;
                SceneManager.UnloadSceneAsync("Options");
                Debug.Log("[OptionsUI] Unloading Options scene (Additive mode) and resuming game");
            }
            else
            {
                // 메인메뉴에서 일반 로드된 경우 - 이전 씬으로 이동
                string previousScene = SceneNavigationManager.Instance.GetPreviousScene();
                SceneManager.LoadScene(previousScene);
                Debug.Log($"[OptionsUI] Loading previous scene: {previousScene}");
            }
        }
    }
}
