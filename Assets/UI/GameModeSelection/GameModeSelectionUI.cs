using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

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
        private Button chapterSelectButton;
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
            chapterSelectButton = root.Q<Button>("ChapterSelectButton");
            endlessModeButton = root.Q<Button>("EndlessModeButton");

            // 이벤트 등록
            if (backButton != null)
                backButton.clicked += OnBackButtonClicked;

            if (storyModeButton != null)
                storyModeButton.clicked += OnStoryModeButtonClicked;

            if (chapterSelectButton != null)
                chapterSelectButton.clicked += OnChapterSelectButtonClicked;

            if (endlessModeButton != null)
                endlessModeButton.clicked += OnEndlessModeButtonClicked;
        }

        private void OnDisable()
        {
            // 이벤트 해제
            if (backButton != null)
                backButton.clicked -= OnBackButtonClicked;

            if (storyModeButton != null)
                storyModeButton.clicked -= OnStoryModeButtonClicked;

            if (chapterSelectButton != null)
                chapterSelectButton.clicked -= OnChapterSelectButtonClicked;

            if (endlessModeButton != null)
                endlessModeButton.clicked -= OnEndlessModeButtonClicked;
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

        private void OnChapterSelectButtonClicked()
        {
            SceneManager.LoadScene("ChapterSelect");
        }

        private void OnEndlessModeButtonClicked()
        {
            SceneManager.LoadScene("EndlessMode");
        }

        #endregion
    }
}
