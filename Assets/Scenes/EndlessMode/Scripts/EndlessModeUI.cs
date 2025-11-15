using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using LostSpells.Systems;

namespace LostSpells.UI
{
    /// <summary>
    /// 무한 모드 UI - 데모 버전
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class EndlessModeUI : MonoBehaviour
    {
        private UIDocument uiDocument;
        private Button backButton;
        private Button playButton;
        private Button easyButton;
        private Button normalButton;
        private Button hardButton;
        private Label headerTitle;
        private Label rankingTitle;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            var root = uiDocument.rootVisualElement;

            backButton = root.Q<Button>("BackButton");
            playButton = root.Q<Button>("PlayButton");
            easyButton = root.Q<Button>("EasyButton");
            normalButton = root.Q<Button>("NormalButton");
            hardButton = root.Q<Button>("HardButton");
            headerTitle = root.Q<Label>("HeaderTitle");
            rankingTitle = root.Q<Label>("RankingTitle");

            if (backButton != null)
                backButton.clicked += OnBackButtonClicked;

            if (playButton != null)
                playButton.clicked += OnPlayButtonClicked;

            // Localization event registration
            LocalizationManager.Instance.OnLanguageChanged += UpdateLocalization;

            // Update UI with current language
            UpdateLocalization();
        }

        private void OnDisable()
        {
            if (backButton != null)
                backButton.clicked -= OnBackButtonClicked;

            if (playButton != null)
                playButton.clicked -= OnPlayButtonClicked;

            // Localization event unregistration
            UnregisterLocalizationEvents();
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

        private void OnBackButtonClicked()
        {
            SceneManager.LoadScene("GameModeSelection");
        }

        private void OnPlayButtonClicked()
        {
            SceneManager.LoadScene("InGame");
        }

        private void UpdateLocalization()
        {
            var loc = LocalizationManager.Instance;

            if (headerTitle != null)
                headerTitle.text = loc.GetText("endless_mode_title");

            if (rankingTitle != null)
                rankingTitle.text = loc.GetText("endless_mode_best_score");

            if (playButton != null)
                playButton.text = loc.GetText("endless_mode_start_game");

            if (easyButton != null)
                easyButton.text = loc.GetText("endless_mode_easy");

            if (normalButton != null)
                normalButton.text = loc.GetText("endless_mode_normal");

            if (hardButton != null)
                hardButton.text = loc.GetText("endless_mode_hard");

            // BackButton은 이미지만 사용하므로 텍스트 설정 안함
        }
    }
}
