using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;

namespace LostSpells.UI
{
    /// <summary>
    /// 옵션 UI 컨트롤러 - UI 전용 버전
    /// </summary>
    public class OptionsUI : MonoBehaviour
    {
        private UIDocument uiDocument;
        private VisualElement root;

        // UI 요소들
        private Button[] categoryButtons;
        private VisualElement[] panels;
        private Dictionary<string, (VisualElement slider, Label label)> sliders;
        private Dictionary<string, (Button on, Button off)> toggles;

        // 슬라이더 드래그 상태
        private bool isDraggingSlider;
        private string currentSliderId;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            root = uiDocument.rootVisualElement;
            InitializeUI();
            LoadSettings();
            ShowPanel(0);

            // 데이터 변경 이벤트 구독
            SubscribeToDataEvents();
        }

        private void OnDisable()
        {
            // 데이터 변경 이벤트 구독 해제
            UnsubscribeFromDataEvents();

            // 설정 저장
            OptionsData.Save();
        }

        private void InitializeUI()
        {
            // 카테고리 버튼 초기화
            InitializeCategoryButtons();

            // 패널 초기화
            InitializePanels();

            // 슬라이더 초기화
            InitializeSliders();

            // 토글 버튼 초기화
            InitializeToggles();

            // 드롭다운 초기화
            InitializeDropdowns();

            // 기타 버튼 초기화
            root.Q<Button>("BackButton")?.RegisterCallback<ClickEvent>(_ => OnBackButtonClicked());
            root.Q<Button>("KeybindButton")?.RegisterCallback<ClickEvent>(_ => OnKeybindButtonClicked());

            // 기본값 버튼 초기화
            root.Q<Button>("AudioResetButton")?.RegisterCallback<ClickEvent>(_ => OnResetAudio());
            root.Q<Button>("GraphicsResetButton")?.RegisterCallback<ClickEvent>(_ => OnResetGraphics());
            root.Q<Button>("LanguageResetButton")?.RegisterCallback<ClickEvent>(_ => OnResetLanguage());
            root.Q<Button>("GameResetButton")?.RegisterCallback<ClickEvent>(_ => OnResetGame());
        }

        #region Initialization

        private void InitializeCategoryButtons()
        {
            string[] names = { "AudioButton", "GraphicsButton", "LanguageButton", "GameButton" };
            categoryButtons = new Button[names.Length];

            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                categoryButtons[i] = root.Q<Button>(names[i]);
                categoryButtons[i]?.RegisterCallback<ClickEvent>(_ => ShowPanel(index));
            }
        }

        private void InitializePanels()
        {
            string[] names = { "AudioPanel", "GraphicsPanel", "LanguagePanel", "GamePanel" };
            panels = new VisualElement[names.Length];

            for (int i = 0; i < names.Length; i++)
            {
                panels[i] = root.Q<VisualElement>(names[i]);
            }
        }

        private void InitializeSliders()
        {
            sliders = new Dictionary<string, (VisualElement, Label)>();
            string[] names = { "Master", "BGM", "SFX", "VoiceThreshold", "Brightness", "Contrast" };

            foreach (var name in names)
            {
                var slider = root.Q<VisualElement>($"{name}Slider");
                var label = root.Q<Label>($"{name}Value");

                if (slider != null && label != null)
                {
                    sliders[name] = (slider, label);

                    // 이벤트 등록
                    slider.RegisterCallback<PointerDownEvent>(evt => StartSliderDrag(name, evt));
                    slider.RegisterCallback<PointerMoveEvent>(evt => UpdateSliderDrag(evt));
                    slider.RegisterCallback<PointerUpEvent>(evt => EndSliderDrag(evt));
                }
            }
        }

        private void InitializeToggles()
        {
            toggles = new Dictionary<string, (Button, Button)>();
            string[] names = { "VSync", "Subtitle", "ScreenShake", "HitEffect" };

            foreach (var name in names)
            {
                var onButton = root.Q<Button>($"{name}On");
                var offButton = root.Q<Button>($"{name}Off");

                if (onButton != null && offButton != null)
                {
                    toggles[name] = (onButton, offButton);

                    // 이벤트 등록
                    string toggleName = name;
                    onButton.RegisterCallback<ClickEvent>(_ => OnToggleChanged(toggleName, true));
                    offButton.RegisterCallback<ClickEvent>(_ => OnToggleChanged(toggleName, false));
                }
            }
        }

        private void InitializeDropdowns()
        {
            // 해상도 드롭다운 (언어 무관)
            InitializeDropdown("ResolutionDropdown",
                new[] { "1920 x 1080", "1600 x 900", "1366 x 768", "1280 x 720" });

            // 언어별 드롭다운 텍스트 설정
            UpdateDropdownsForLanguage();

            // 마이크 드롭다운
            var micDropdown = root.Q<DropdownField>("MicrophoneDropdown");
            if (micDropdown != null)
            {
                var devices = Microphone.devices;
                if (devices.Length > 0)
                {
                    micDropdown.choices = new List<string>(devices);
                    // 초기값 설정 - 저장된 값이 있으면 사용, 없으면 첫 번째 장치
                    if (!string.IsNullOrEmpty(OptionsData.MicrophoneDevice) &&
                        micDropdown.choices.Contains(OptionsData.MicrophoneDevice))
                    {
                        micDropdown.value = OptionsData.MicrophoneDevice;
                    }
                    else
                    {
                        micDropdown.value = devices[0];
                        OptionsData.MicrophoneDevice = devices[0];
                    }
                }
                else
                {
                    micDropdown.choices = new List<string> { "No Microphone" };
                    micDropdown.value = "No Microphone";
                }

                micDropdown.RegisterValueChangedCallback(evt =>
                    OptionsData.MicrophoneDevice = evt.newValue);
            }
        }

        private void UpdateDropdownsForLanguage()
        {
            // 화면 모드 드롭다운 (영어 고정)
            InitializeDropdown("ScreenModeDropdown",
                new[] { "Fullscreen", "Windowed", "Borderless Window" });

            // 언어 드롭다운
            InitializeDropdown("GameLanguageDropdown",
                new[] { "한국어", "English", "日本語", "中文" });

            InitializeDropdown("VoiceLanguageDropdown",
                new[] { "한국어", "English", "日本語" });

            // 난이도 드롭다운 (영어 고정)
            InitializeDropdown("DifficultyDropdown",
                new[] { "Easy", "Normal", "Hard", "Very Hard" });
        }

        private void InitializeDropdown(string name, string[] choices)
        {
            var dropdown = root.Q<DropdownField>(name);
            if (dropdown != null)
            {
                dropdown.choices = new List<string>(choices);
                dropdown.RegisterValueChangedCallback(evt =>
                {
                    int index = dropdown.choices.IndexOf(evt.newValue);
                    HandleDropdownChange(name, index);
                });
            }
        }

        private void HandleDropdownChange(string dropdownName, int index)
        {
            switch (dropdownName)
            {
                case "ResolutionDropdown":
                    OptionsData.ResolutionIndex = index;
                    OptionsData.ApplyResolution(index);
                    break;
                case "ScreenModeDropdown":
                    OptionsData.ScreenMode = (ScreenMode)index;
                    OptionsData.ApplyScreenMode((ScreenMode)index);
                    break;
                case "GameLanguageDropdown":
                    OptionsData.GameLanguage = (Language)index;
                    OptionsData.Save();  // 언어 변경 시 즉시 저장
                    break;
                case "VoiceLanguageDropdown":
                    OptionsData.VoiceLanguage = (Language)index;
                    break;
                case "DifficultyDropdown":
                    OptionsData.Difficulty = (Difficulty)index;
                    break;
            }
        }

        #endregion

        #region Load/Save Settings

        private void LoadSettings()
        {
            // 슬라이더 값 로드
            UpdateSliderVisual("Master", OptionsData.MasterVolume);
            UpdateSliderVisual("BGM", OptionsData.BGMVolume);
            UpdateSliderVisual("SFX", OptionsData.SFXVolume);
            UpdateSliderVisual("VoiceThreshold", OptionsData.VoiceThreshold);
            UpdateSliderVisual("Brightness", OptionsData.Brightness);
            UpdateSliderVisual("Contrast", OptionsData.Contrast);

            // 토글 값 로드
            SetToggleVisual("VSync", OptionsData.VSync);
            SetToggleVisual("Subtitle", OptionsData.Subtitle);
            SetToggleVisual("ScreenShake", OptionsData.ScreenShake);
            SetToggleVisual("HitEffect", OptionsData.HitEffect);

            // 드롭다운 값 로드
            LoadDropdownValue("ResolutionDropdown", OptionsData.ResolutionIndex);

            // 드롭다운 값 로드
            LoadDropdownValue("ScreenModeDropdown", (int)OptionsData.ScreenMode);
            LoadDropdownValue("GameLanguageDropdown", (int)OptionsData.GameLanguage);
            LoadDropdownValue("VoiceLanguageDropdown", (int)OptionsData.VoiceLanguage);
            LoadDropdownValue("DifficultyDropdown", (int)OptionsData.Difficulty);

            // 마이크 드롭다운은 InitializeDropdowns에서 이미 초기화됨
        }

        private void LoadDropdownValue(string name, int index)
        {
            var dropdown = root.Q<DropdownField>(name);
            if (dropdown != null && index >= 0 && index < dropdown.choices.Count)
            {
                dropdown.SetValueWithoutNotify(dropdown.choices[index]);
            }
        }

        #endregion

        #region Slider Handling

        private void StartSliderDrag(string name, PointerDownEvent evt)
        {
            if (evt.button == 0)
            {
                isDraggingSlider = true;
                currentSliderId = name;
                sliders[name].slider.CapturePointer(evt.pointerId);
                UpdateSliderFromPosition(name, evt.localPosition.x);
            }
        }

        private void UpdateSliderDrag(PointerMoveEvent evt)
        {
            if (isDraggingSlider && currentSliderId != null && sliders.ContainsKey(currentSliderId))
            {
                var slider = sliders[currentSliderId].slider;
                if (slider.HasPointerCapture(evt.pointerId))
                {
                    UpdateSliderFromPosition(currentSliderId, evt.localPosition.x);
                }
            }
        }

        private void EndSliderDrag(PointerUpEvent evt)
        {
            if (isDraggingSlider && currentSliderId != null && sliders.ContainsKey(currentSliderId))
            {
                var slider = sliders[currentSliderId].slider;
                if (slider.HasPointerCapture(evt.pointerId))
                {
                    slider.ReleasePointer(evt.pointerId);
                }
                isDraggingSlider = false;
                currentSliderId = null;
            }
        }

        private void UpdateSliderFromPosition(string name, float localX)
        {
            var slider = sliders[name].slider;
            float value = Mathf.Clamp01(localX / slider.resolvedStyle.width) * 100f;

            // 데이터 업데이트
            switch (name)
            {
                case "Master": OptionsData.MasterVolume = value; break;
                case "BGM": OptionsData.BGMVolume = value; break;
                case "SFX": OptionsData.SFXVolume = value; break;
                case "VoiceThreshold": OptionsData.VoiceThreshold = value; break;
                case "Brightness": OptionsData.Brightness = value; break;
                case "Contrast": OptionsData.Contrast = value; break;
            }

            UpdateSliderVisual(name, value);
        }

        private void UpdateSliderVisual(string name, float value)
        {
            if (!sliders.ContainsKey(name)) return;

            var (slider, label) = sliders[name];
            var fill = slider.Q<VisualElement>($"{name}Fill");
            if (fill != null)
            {
                fill.style.width = Length.Percent(value);
            }
            label.text = $"{Mathf.RoundToInt(value)}%";
        }

        #endregion

        #region Toggle Handling

        private void OnToggleChanged(string name, bool isOn)
        {
            // 데이터 업데이트
            switch (name)
            {
                case "VSync": OptionsData.VSync = isOn; break;
                case "Subtitle": OptionsData.Subtitle = isOn; break;
                case "ScreenShake": OptionsData.ScreenShake = isOn; break;
                case "HitEffect": OptionsData.HitEffect = isOn; break;
            }

            SetToggleVisual(name, isOn);
        }

        private void SetToggleVisual(string name, bool isOn)
        {
            if (!toggles.ContainsKey(name)) return;

            var (onButton, offButton) = toggles[name];

            if (isOn)
            {
                onButton.AddToClassList("active");
                offButton.RemoveFromClassList("active");
            }
            else
            {
                onButton.RemoveFromClassList("active");
                offButton.AddToClassList("active");
            }
        }

        #endregion

        #region Panel Management

        private void ShowPanel(int index)
        {
            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] != null)
                {
                    panels[i].style.display = i == index ? DisplayStyle.Flex : DisplayStyle.None;
                }

                if (categoryButtons[i] != null)
                {
                    if (i == index)
                        categoryButtons[i].AddToClassList("selected");
                    else
                        categoryButtons[i].RemoveFromClassList("selected");
                }
            }

            // 스크롤 초기화
            var scrollView = root.Q<ScrollView>("RightPanel");
            if (scrollView != null)
            {
                scrollView.scrollOffset = Vector2.zero;
            }
        }

        #endregion

        #region Button Handlers

        private void OnBackButtonClicked()
        {
            OptionsData.Save();
            SceneManager.LoadScene("MainMenu");
        }

        private void OnKeybindButtonClicked()
        {
            // TODO: 키바인드 설정 팝업 열기
            Debug.Log("키바인드 설정 열기");
        }

        private void OnResetAudio()
        {
            OptionsData.ResetAudioToDefaults();
            LoadSettings();  // UI 새로고침
            Debug.Log("[Options] 오디오 설정이 기본값으로 초기화되었습니다");
        }

        private void OnResetGraphics()
        {
            OptionsData.ResetGraphicsToDefaults();
            LoadSettings();  // UI 새로고침
            Debug.Log("[Options] 그래픽 설정이 기본값으로 초기화되었습니다");
        }

        private void OnResetLanguage()
        {
            OptionsData.ResetLanguageToDefaults();
            LoadSettings();  // UI 새로고침
            Debug.Log("[Options] 언어 설정이 기본값으로 초기화되었습니다");
        }

        private void OnResetGame()
        {
            OptionsData.ResetGameToDefaults();
            LoadSettings();  // UI 새로고침
            Debug.Log("[Options] 게임 설정이 기본값으로 초기화되었습니다");
        }

        #endregion

        #region Event Subscriptions

        private void SubscribeToDataEvents()
        {
            // 외부에서 데이터가 변경될 경우 UI 업데이트
            OptionsData.OnAudioSettingsChanged += OnAudioSettingsChanged;
            OptionsData.OnGraphicsSettingsChanged += OnGraphicsSettingsChanged;
        }

        private void UnsubscribeFromDataEvents()
        {
            OptionsData.OnAudioSettingsChanged -= OnAudioSettingsChanged;
            OptionsData.OnGraphicsSettingsChanged -= OnGraphicsSettingsChanged;
        }

        private void OnAudioSettingsChanged()
        {
            // 오디오 설정이 외부에서 변경되면 UI 업데이트
            UpdateSliderVisual("Master", OptionsData.MasterVolume);
            UpdateSliderVisual("BGM", OptionsData.BGMVolume);
            UpdateSliderVisual("SFX", OptionsData.SFXVolume);
        }

        private void OnGraphicsSettingsChanged()
        {
            // 그래픽 설정이 외부에서 변경되면 UI 업데이트
            UpdateSliderVisual("Brightness", OptionsData.Brightness);
            UpdateSliderVisual("Contrast", OptionsData.Contrast);
            SetToggleVisual("VSync", OptionsData.VSync);
        }

        #endregion
    }
}