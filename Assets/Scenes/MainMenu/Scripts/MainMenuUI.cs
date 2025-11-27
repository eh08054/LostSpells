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

        // 메뉴 버튼 컨테이너
        private VisualElement menuButtonContainer;

        // 종료 확인 화면
        private VisualElement quitConfirmation;
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

            // 메뉴 버튼 컨테이너 찾기
            menuButtonContainer = root.Q<VisualElement>("MenuButtonContainer");

            // 종료 확인 화면 찾기
            quitConfirmation = root.Q<VisualElement>("QuitConfirmation");
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

            // 종료 확인 화면 초기 상태 - 숨김
            HideQuitConfirmation();
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

            // 종료 확인 화면
            if (quitConfirmation != null)
            {
                var quitTitle = quitConfirmation.Q<Label>("QuitTitle");
                if (quitTitle != null)
                    quitTitle.text = loc.GetText("quit_popup_title");

                var quitMessage = quitConfirmation.Q<Label>("QuitMessage");
                if (quitMessage != null)
                    quitMessage.text = loc.GetText("quit_popup_message");
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
            // 종료 확인 화면 표시
            ShowQuitConfirmation();
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
            // 종료 확인 화면 숨김
            HideQuitConfirmation();
        }

        #endregion

        #region Debug Methods

        private void LogSaveFileInfo()
        {
            var saveManager = LostSpells.Data.SaveManager.Instance;
            string savePath = saveManager.GetSaveFilePath();
            bool exists = saveManager.SaveFileExists();
            var saveData = saveManager.GetCurrentSaveData();

            Debug.Log("========== 저장 파일 정보 ==========");
            Debug.Log($"저장 파일 경로: {savePath}");
            Debug.Log($"파일 존재 여부: {exists}");
            if (saveData != null)
            {
                Debug.Log($"다이아몬드: {saveData.diamonds}");
                Debug.Log($"부활석: {saveData.reviveStones}");
                Debug.Log($"골드: {saveData.gold}");
                Debug.Log($"마지막 저장 시간: {saveData.lastSaveTime}");
            }
            else
            {
                Debug.LogWarning("세이브 데이터가 null입니다!");
            }
            Debug.Log("===================================");
        }

        #endregion

        #region Quit Confirmation Control

        private void ShowQuitConfirmation()
        {
            // 메뉴 버튼들 숨기기
            if (menuButtonContainer != null)
            {
                menuButtonContainer.style.display = DisplayStyle.None;
            }

            // 종료 확인 화면 표시
            if (quitConfirmation != null)
            {
                quitConfirmation.style.display = DisplayStyle.Flex;
            }
        }

        private void HideQuitConfirmation()
        {
            // 종료 확인 화면 숨기기
            if (quitConfirmation != null)
            {
                quitConfirmation.style.display = DisplayStyle.None;
            }

            // 메뉴 버튼들 다시 표시
            if (menuButtonContainer != null)
            {
                menuButtonContainer.style.display = DisplayStyle.Flex;
            }
        }

        #endregion
    }
}
