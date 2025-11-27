using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using LostSpells.Systems;

namespace LostSpells.UI
{
    /// <summary>
    /// 게임모드 선택 UI 컨트롤러
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class GameModeSelectionUI : MonoBehaviour
    {
        private UIDocument uiDocument;

        // 공통 요소
        private Button backButton;

        // 게임모드 버튼들
        private Button storyModeButton;
        private Button endlessModeButton;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            var root = uiDocument.rootVisualElement;

            // UI 요소 찾기
            backButton = root.Q<Button>("BackButton");
            storyModeButton = root.Q<Button>("StoryModeButton");
            endlessModeButton = root.Q<Button>("EndlessModeButton");

            // 이벤트 등록
            if (backButton != null)
                backButton.clicked += OnBackButtonClicked;

            if (storyModeButton != null)
                storyModeButton.clicked += OnStoryModeButtonClicked;

            if (endlessModeButton != null)
                endlessModeButton.clicked += OnEndlessModeButtonClicked;

            // Localization 이벤트 등록
            LocalizationManager.Instance.OnLanguageChanged += UpdateLocalization;

            // 현재 언어로 UI 업데이트
            UpdateLocalization();
        }

        private void OnDisable()
        {
            // 이벤트 해제
            if (backButton != null)
                backButton.clicked -= OnBackButtonClicked;

            if (storyModeButton != null)
                storyModeButton.clicked -= OnStoryModeButtonClicked;

            if (endlessModeButton != null)
                endlessModeButton.clicked -= OnEndlessModeButtonClicked;

            // Localization 이벤트 해제
            UnregisterLocalizationEvents();
        }

        private void OnDestroy()
        {
            // 오브젝트 파괴 시에도 이벤트 해제
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

        private void UpdateLocalization()
        {
            var loc = LocalizationManager.Instance;
            var root = uiDocument.rootVisualElement;

            // Title/Header
            var headerTitle = root.Q<Label>("HeaderTitle");
            if (headerTitle != null)
                headerTitle.text = loc.GetText("game_mode_selection_title");

            // Buttons
            if (storyModeButton != null)
                storyModeButton.text = loc.GetText("game_mode_story");

            if (endlessModeButton != null)
                endlessModeButton.text = loc.GetText("game_mode_endless");

            // BackButton은 이미지만 사용하므로 텍스트 설정 안함
        }

        #region Button Click Handlers

        private void OnBackButtonClicked()
        {
            SceneManager.LoadScene("MainMenu");
        }

        private void OnStoryModeButtonClicked()
        {
            // 스토리 모드 씬으로 이동
            SceneManager.LoadScene("StoryMode");
        }

        private void OnEndlessModeButtonClicked()
        {
            SceneManager.LoadScene("EndlessMode");
        }

        #endregion
    }
}
