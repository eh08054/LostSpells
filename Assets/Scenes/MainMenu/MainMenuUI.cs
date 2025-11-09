using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using LostSpells.Systems;

namespace LostSpells.UI
{
    /// <summary>
    /// 메인메뉴 UI 컨트롤러 - 버튼 이벤트를 처리합니다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuUI : MonoBehaviour
    {
        private UIDocument uiDocument;
        private Button playButton;
        private Button optionsButton;
        private Button storeButton;
        private Button quitButton;

        // 팝업 요소
        private VisualElement quitPopup;
        private Button confirmQuitButton;
        private Button cancelQuitButton;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            // UI 요소 찾기 및 이벤트 등록
            var root = uiDocument.rootVisualElement;

            playButton = root.Q<Button>("PlayButton");
            optionsButton = root.Q<Button>("OptionsButton");
            storeButton = root.Q<Button>("StoreButton");
            quitButton = root.Q<Button>("QuitButton");

            // 팝업 요소 찾기
            quitPopup = root.Q<VisualElement>("QuitPopup");
            confirmQuitButton = root.Q<Button>("ConfirmQuitButton");
            cancelQuitButton = root.Q<Button>("CancelQuitButton");

            // 이벤트 등록
            if (playButton != null)
                playButton.clicked += OnPlayButtonClicked;

            if (optionsButton != null)
                optionsButton.clicked += OnOptionsButtonClicked;

            if (storeButton != null)
                storeButton.clicked += OnStoreButtonClicked;

            if (quitButton != null)
                quitButton.clicked += OnQuitButtonClicked;

            // 팝업 버튼 이벤트 등록
            if (confirmQuitButton != null)
                confirmQuitButton.clicked += OnConfirmQuitButtonClicked;

            if (cancelQuitButton != null)
                cancelQuitButton.clicked += OnCancelQuitButtonClicked;

            // Localization 이벤트 등록
            LocalizationManager.Instance.OnLanguageChanged += UpdateLocalization;

            // 현재 언어로 UI 업데이트
            UpdateLocalization();

            // 팝업 초기 상태 - 숨김
            HideQuitPopup();
        }

        private void OnDisable()
        {
            // 이벤트 해제
            if (playButton != null)
                playButton.clicked -= OnPlayButtonClicked;

            if (optionsButton != null)
                optionsButton.clicked -= OnOptionsButtonClicked;

            if (storeButton != null)
                storeButton.clicked -= OnStoreButtonClicked;

            if (quitButton != null)
                quitButton.clicked -= OnQuitButtonClicked;

            // 팝업 버튼 이벤트 해제
            if (confirmQuitButton != null)
                confirmQuitButton.clicked -= OnConfirmQuitButtonClicked;

            if (cancelQuitButton != null)
                cancelQuitButton.clicked -= OnCancelQuitButtonClicked;

            // Localization 이벤트 해제
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

        private void UpdateLocalization()
        {
            var loc = LocalizationManager.Instance;

            if (playButton != null)
                playButton.text = loc.GetText("main_menu_play");
            if (optionsButton != null)
                optionsButton.text = loc.GetText("main_menu_options");
            if (storeButton != null)
                storeButton.text = loc.GetText("main_menu_store");
            if (quitButton != null)
                quitButton.text = loc.GetText("main_menu_quit");

            // Popup
            if (quitPopup != null)
            {
                var popupTitle = quitPopup.Q<Label>("PopupTitle");
                if (popupTitle != null)
                    popupTitle.text = loc.GetText("quit_popup_title");

                var popupMessage = quitPopup.Q<Label>("PopupMessage");
                if (popupMessage != null)
                    popupMessage.text = loc.GetText("quit_popup_message");
            }

            if (confirmQuitButton != null)
                confirmQuitButton.text = loc.GetText("quit_popup_quit");
            if (cancelQuitButton != null)
                cancelQuitButton.text = loc.GetText("quit_popup_cancel");
        }

        #region Button Click Handlers

        private void OnPlayButtonClicked()
        {
            SceneManager.LoadScene("GameModeSelection");
        }

        private void OnOptionsButtonClicked()
        {
            // 현재 씬을 이전 씬으로 저장
            SceneNavigationManager.Instance.SetPreviousScene("MainMenu");
            SceneManager.LoadScene("Options");
        }

        private void OnStoreButtonClicked()
        {
            // 현재 씬을 이전 씬으로 저장
            SceneNavigationManager.Instance.SetPreviousScene("MainMenu");
            SceneManager.LoadScene("Store");
        }

        private void OnQuitButtonClicked()
        {
            // 팝업 표시
            ShowQuitPopup();
        }

        private void OnConfirmQuitButtonClicked()
        {
            // 게임 종료
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnCancelQuitButtonClicked()
        {
            // 팝업 숨김
            HideQuitPopup();
        }

        #endregion

        #region Popup Control

        private void ShowQuitPopup()
        {
            if (quitPopup != null)
            {
                quitPopup.style.display = DisplayStyle.Flex;
            }
        }

        private void HideQuitPopup()
        {
            if (quitPopup != null)
            {
                quitPopup.style.display = DisplayStyle.None;
            }
        }

        #endregion
    }
}
