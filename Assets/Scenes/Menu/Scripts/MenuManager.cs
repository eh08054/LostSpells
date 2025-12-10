using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using LostSpells.Data;
using LostSpells.Systems;

namespace LostSpells.UI
{
    /// <summary>
    /// 통합 메뉴 매니저 - 모든 메뉴 패널을 하나의 씬에서 관리
    /// 패널 전환 시 배경이 끊기지 않고 유지됨
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MenuManager : MonoBehaviour
    {
        public enum MenuPanel
        {
            MainMenu,
            GameModeSelection,
            StoryMode,
            EndlessMode,
            Options,
            Store
        }

        private UIDocument uiDocument;
        private VisualElement root;
        private MenuParallaxBackground parallaxBackground;

        // 각 패널 참조
        private VisualElement mainMenuPanel;
        private VisualElement gameModeSelectionPanel;
        private VisualElement storyModePanel;
        private VisualElement endlessModePanel;
        private VisualElement optionsPanel;
        private VisualElement storePanel;

        // 현재 활성화된 패널
        private MenuPanel currentPanel = MenuPanel.MainMenu;

        // 패널 히스토리 (뒤로가기 지원)
        private Stack<MenuPanel> panelHistory = new Stack<MenuPanel>();

        // 팝업 참조
        private VisualElement quitConfirmation;

        // 챕터 데이터
        private List<ChapterData> chapters;
        private PlayerSaveData saveData;

        // Options 패널 참조
        private OptionsPanelController optionsPanelController;

        // Store 패널 참조
        private StorePanelController storePanelController;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
            parallaxBackground = GetComponent<MenuParallaxBackground>();
        }

        private void OnEnable()
        {
            root = uiDocument.rootVisualElement;

            // 패널 찾기
            mainMenuPanel = root.Q<VisualElement>("MainMenuPanel");
            gameModeSelectionPanel = root.Q<VisualElement>("GameModeSelectionPanel");
            storyModePanel = root.Q<VisualElement>("StoryModePanel");
            endlessModePanel = root.Q<VisualElement>("EndlessModePanel");
            optionsPanel = root.Q<VisualElement>("OptionsPanel");
            storePanel = root.Q<VisualElement>("StorePanel");
            quitConfirmation = root.Q<VisualElement>("QuitConfirmation");

            // 이벤트 등록
            SetupMainMenuEvents();
            SetupGameModeSelectionEvents();
            SetupStoryModeEvents();
            SetupEndlessModeEvents();
            SetupOptionsPanelEvents();
            SetupStorePanelEvents();

            // Localization 이벤트 등록
            LocalizationManager.Instance.OnLanguageChanged += UpdateLocalization;

            // 초기 패널 표시
            ShowPanel(MenuPanel.MainMenu);

            // 현재 언어로 UI 업데이트
            UpdateLocalization();
        }

        private void OnDisable()
        {
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

        #region Panel Navigation

        /// <summary>
        /// 패널 전환
        /// </summary>
        public void ShowPanel(MenuPanel panel)
        {
            // 현재 패널을 히스토리에 저장 (MainMenu가 아닌 경우)
            if (currentPanel != panel && currentPanel != MenuPanel.MainMenu)
            {
                panelHistory.Push(currentPanel);
            }

            // 모든 패널 숨기기
            HideAllPanels();

            // 선택된 패널 표시
            switch (panel)
            {
                case MenuPanel.MainMenu:
                    if (mainMenuPanel != null) mainMenuPanel.style.display = DisplayStyle.Flex;
                    panelHistory.Clear(); // 메인메뉴로 돌아오면 히스토리 초기화
                    break;
                case MenuPanel.GameModeSelection:
                    if (gameModeSelectionPanel != null) gameModeSelectionPanel.style.display = DisplayStyle.Flex;
                    break;
                case MenuPanel.StoryMode:
                    if (storyModePanel != null) storyModePanel.style.display = DisplayStyle.Flex;
                    LoadChapters();
                    break;
                case MenuPanel.EndlessMode:
                    if (endlessModePanel != null) endlessModePanel.style.display = DisplayStyle.Flex;
                    LoadEndlessRankings();
                    break;
                case MenuPanel.Options:
                    if (optionsPanel != null) optionsPanel.style.display = DisplayStyle.Flex;
                    if (optionsPanelController != null) optionsPanelController.OnPanelShown();
                    break;
                case MenuPanel.Store:
                    if (storePanel != null) storePanel.style.display = DisplayStyle.Flex;
                    if (storePanelController != null) storePanelController.OnPanelShown();
                    break;
            }

            currentPanel = panel;
            UpdateLocalization();
        }

        /// <summary>
        /// 뒤로가기
        /// </summary>
        public void GoBack()
        {
            if (panelHistory.Count > 0)
            {
                MenuPanel previousPanel = panelHistory.Pop();
                HideAllPanels();

                switch (previousPanel)
                {
                    case MenuPanel.MainMenu:
                        if (mainMenuPanel != null) mainMenuPanel.style.display = DisplayStyle.Flex;
                        break;
                    case MenuPanel.GameModeSelection:
                        if (gameModeSelectionPanel != null) gameModeSelectionPanel.style.display = DisplayStyle.Flex;
                        break;
                    case MenuPanel.StoryMode:
                        if (storyModePanel != null) storyModePanel.style.display = DisplayStyle.Flex;
                        break;
                    case MenuPanel.EndlessMode:
                        if (endlessModePanel != null) endlessModePanel.style.display = DisplayStyle.Flex;
                        break;
                    case MenuPanel.Options:
                        if (optionsPanel != null) optionsPanel.style.display = DisplayStyle.Flex;
                        break;
                    case MenuPanel.Store:
                        if (storePanel != null) storePanel.style.display = DisplayStyle.Flex;
                        break;
                }

                currentPanel = previousPanel;
            }
            else
            {
                // 히스토리가 비어있으면 메인메뉴로
                ShowPanel(MenuPanel.MainMenu);
            }
        }

        private void HideAllPanels()
        {
            if (mainMenuPanel != null) mainMenuPanel.style.display = DisplayStyle.None;
            if (gameModeSelectionPanel != null) gameModeSelectionPanel.style.display = DisplayStyle.None;
            if (storyModePanel != null) storyModePanel.style.display = DisplayStyle.None;
            if (endlessModePanel != null) endlessModePanel.style.display = DisplayStyle.None;

            // Options 패널 숨김 시 정리 (서버 체크 루프 중지 등)
            if (optionsPanel != null && optionsPanel.style.display == DisplayStyle.Flex)
            {
                if (optionsPanelController != null) optionsPanelController.OnPanelHidden();
            }
            if (optionsPanel != null) optionsPanel.style.display = DisplayStyle.None;

            if (storePanel != null) storePanel.style.display = DisplayStyle.None;
        }

        #endregion

        #region MainMenu Events

        private void SetupMainMenuEvents()
        {
            var playButton = root.Q<Button>("PlayButton");
            var optionsButton = root.Q<Button>("OptionsButton");
            var storeButton = root.Q<Button>("StoreButton");
            var quitButton = root.Q<Button>("QuitButton");
            var quitConfirmButton = root.Q<Button>("QuitConfirmButton");
            var quitCancelButton = root.Q<Button>("QuitCancelButton");

            if (playButton != null) playButton.clicked += () => ShowPanel(MenuPanel.GameModeSelection);
            if (optionsButton != null) optionsButton.clicked += () => ShowPanel(MenuPanel.Options);
            if (storeButton != null) storeButton.clicked += () => ShowPanel(MenuPanel.Store);
            if (quitButton != null) quitButton.clicked += ShowQuitConfirmation;
            if (quitConfirmButton != null) quitConfirmButton.clicked += QuitGame;
            if (quitCancelButton != null) quitCancelButton.clicked += HideQuitConfirmation;
        }

        private void ShowQuitConfirmation()
        {
            if (quitConfirmation != null)
                quitConfirmation.style.display = DisplayStyle.Flex;
        }

        private void HideQuitConfirmation()
        {
            if (quitConfirmation != null)
                quitConfirmation.style.display = DisplayStyle.None;
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region GameModeSelection Events

        private void SetupGameModeSelectionEvents()
        {
            var storyModeButton = root.Q<Button>("StoryModeButton");
            var endlessModeButton = root.Q<Button>("EndlessModeButton");
            var gameModeBackButton = root.Q<Button>("GameModeBackButton");

            if (storyModeButton != null) storyModeButton.clicked += () => ShowPanel(MenuPanel.StoryMode);
            if (endlessModeButton != null) endlessModeButton.clicked += () => ShowPanel(MenuPanel.EndlessMode);
            if (gameModeBackButton != null) gameModeBackButton.clicked += () => ShowPanel(MenuPanel.MainMenu);
        }

        #endregion

        #region StoryMode Events

        private void SetupStoryModeEvents()
        {
            var storyModeBackButton = root.Q<Button>("StoryModeBackButton");
            if (storyModeBackButton != null) storyModeBackButton.clicked += () => ShowPanel(MenuPanel.GameModeSelection);
        }

        private void LoadChapters()
        {
            chapters = DataManager.Instance.GetAllChapterData();
            saveData = SaveManager.Instance.GetCurrentSaveData();
            DisplayChapters();
        }

        private void DisplayChapters()
        {
            var chapterListContainer = root.Q<VisualElement>("ChapterListContainer");
            if (chapterListContainer == null || chapters == null)
                return;

            chapterListContainer.Clear();

            foreach (var chapter in chapters)
            {
                CreateChapterButton(chapterListContainer, chapter);
            }
        }

        private void CreateChapterButton(VisualElement container, ChapterData chapterData)
        {
            var chapterButton = new Button();
            chapterButton.AddToClassList("chapter-button");

            var chapterInfo = new VisualElement();
            chapterInfo.AddToClassList("chapter-info");

            var loc = LocalizationManager.Instance;

            var chapterText = loc.GetText("story_mode_chapter");
            var chapterNumber = new Label($"{chapterText} {chapterData.chapterId}");
            chapterNumber.AddToClassList("chapter-number");
            chapterInfo.Add(chapterNumber);

            var chapterName = new Label(chapterData.GetLocalizedName());
            chapterName.AddToClassList("chapter-name");
            chapterInfo.Add(chapterName);

            var progress = saveData.GetChapterProgress(chapterData.chapterId);
            var waveText = loc.GetText("story_mode_wave");
            var progressInfo = new Label($"{waveText} {progress.clearedWaves}");
            progressInfo.AddToClassList("chapter-progress");
            chapterInfo.Add(progressInfo);

            chapterButton.Add(chapterInfo);

            var clearedChapterIds = new List<int>();
            bool isLocked = chapterData.IsLocked(saveData.level, clearedChapterIds);

            if (isLocked)
            {
                var lockOverlay = new VisualElement();
                lockOverlay.AddToClassList("lock-overlay");

                var lockIcon = new VisualElement();
                lockIcon.AddToClassList("lock-icon");
                lockOverlay.Add(lockIcon);

                chapterButton.Add(lockOverlay);
            }

            chapterButton.clicked += () => OnChapterButtonClicked(chapterData);
            container.Add(chapterButton);
        }

        private void OnChapterButtonClicked(ChapterData chapterData)
        {
            GameStateManager.Instance.StartChapter(chapterData.chapterId);
            SceneManager.LoadScene("InGame");
        }

        #endregion

        #region EndlessMode Events

        private void SetupEndlessModeEvents()
        {
            var endlessModeBackButton = root.Q<Button>("EndlessModeBackButton");
            var endlessPlayButton = root.Q<Button>("EndlessPlayButton");

            if (endlessModeBackButton != null) endlessModeBackButton.clicked += () => ShowPanel(MenuPanel.GameModeSelection);
            if (endlessPlayButton != null) endlessPlayButton.clicked += OnEndlessPlayClicked;
        }

        private void LoadEndlessRankings()
        {
            var rankingListContainer = root.Q<VisualElement>("RankingListContainer");
            if (rankingListContainer == null)
                return;

            // 기존 랭킹 항목 제거
            rankingListContainer.Clear();

            // SaveManager에서 상위 5개 기록 가져오기
            var topRecords = SaveManager.Instance.GetEndlessModeTopRecords();
            var loc = LocalizationManager.Instance;

            // 1등부터 5등까지 표시
            for (int i = 0; i < 5; i++)
            {
                var record = topRecords[i];

                var rankingItem = new VisualElement();
                rankingItem.AddToClassList("ranking-item");

                // 1-3등 특별 스타일 적용
                if (i == 0)
                    rankingItem.AddToClassList("ranking-item-gold");
                else if (i == 1)
                    rankingItem.AddToClassList("ranking-item-silver");
                else if (i == 2)
                    rankingItem.AddToClassList("ranking-item-bronze");

                // 순위 번호
                string rankText = GetRankText(i + 1, loc);
                var rankLabel = new Label(rankText);
                rankLabel.AddToClassList("ranking-rank");

                // 점수
                var scoreLabel = new Label(record.score.ToString());
                scoreLabel.AddToClassList("ranking-score");

                // 웨이브
                var waveLabel = new Label(record.wave.ToString());
                waveLabel.AddToClassList("ranking-wave");

                // 날짜
                var dateLabel = new Label(record.date);
                dateLabel.AddToClassList("ranking-date");

                rankingItem.Add(rankLabel);
                rankingItem.Add(scoreLabel);
                rankingItem.Add(waveLabel);
                rankingItem.Add(dateLabel);
                rankingListContainer.Add(rankingItem);
            }
        }

        /// <summary>
        /// 순위 번호를 언어에 맞게 포맷팅
        /// </summary>
        private string GetRankText(int rank, LocalizationManager loc)
        {
            string rankSuffix = loc.GetText("endless_mode_rank_suffix");

            // 한국어 (접미사가 있는 경우)
            if (!string.IsNullOrEmpty(rankSuffix))
            {
                return $"{rank}{rankSuffix}";
            }

            // 영어 (서수형)
            string suffix;
            switch (rank)
            {
                case 1:
                    suffix = "st";
                    break;
                case 2:
                    suffix = "nd";
                    break;
                case 3:
                    suffix = "rd";
                    break;
                default:
                    suffix = "th";
                    break;
            }

            return $"{rank}{suffix}";
        }

        private void OnEndlessPlayClicked()
        {
            GameStateManager.Instance.StartEndlessMode();
            SceneManager.LoadScene("InGame");
        }

        #endregion

        #region Options Panel Events

        private void SetupOptionsPanelEvents()
        {
            // Options 패널 Back 버튼
            var optionsBackButton = root.Q<Button>("OptionsBackButton");
            if (optionsBackButton != null) optionsBackButton.clicked += () => ShowPanel(MenuPanel.MainMenu);

            // OptionsPanelController 초기화
            optionsPanelController = new OptionsPanelController(root, optionsPanel, this);
        }

        /// <summary>
        /// OptionsPanelController 반환 (음성 명령용)
        /// </summary>
        public OptionsPanelController GetOptionsPanelController()
        {
            return optionsPanelController;
        }

        #endregion

        #region Store Panel Events

        private void SetupStorePanelEvents()
        {
            // Store 패널 Back 버튼
            var storeBackButton = root.Q<Button>("StoreBackButton");
            if (storeBackButton != null) storeBackButton.clicked += () => ShowPanel(MenuPanel.MainMenu);

            // StorePanelController 초기화
            storePanelController = new StorePanelController(root, storePanel, this);
        }

        #endregion

        #region Localization

        private void UpdateLocalization()
        {
            var loc = LocalizationManager.Instance;

            // MainMenu
            var gameTitle = root.Q<Label>("GameTitle");
            if (gameTitle != null) gameTitle.text = loc.GetText("game_title");

            var playButton = root.Q<Button>("PlayButton");
            if (playButton != null) playButton.text = loc.GetText("main_menu_play");

            var optionsButton = root.Q<Button>("OptionsButton");
            if (optionsButton != null) optionsButton.text = loc.GetText("main_menu_options");

            var storeButton = root.Q<Button>("StoreButton");
            if (storeButton != null) storeButton.text = loc.GetText("main_menu_store");

            var quitButton = root.Q<Button>("QuitButton");
            if (quitButton != null) quitButton.text = loc.GetText("main_menu_quit");

            // Quit Confirmation
            var quitTitle = root.Q<Label>("QuitTitle");
            if (quitTitle != null) quitTitle.text = loc.GetText("quit_title");

            var quitMessage = root.Q<Label>("QuitMessage");
            if (quitMessage != null) quitMessage.text = loc.GetText("quit_message");

            var quitConfirmButton = root.Q<Button>("QuitConfirmButton");
            if (quitConfirmButton != null) quitConfirmButton.text = loc.GetText("quit_confirm");

            var quitCancelButton = root.Q<Button>("QuitCancelButton");
            if (quitCancelButton != null) quitCancelButton.text = loc.GetText("quit_cancel");

            // GameModeSelection
            var gameModeTitle = root.Q<Label>("GameModeTitle");
            if (gameModeTitle != null) gameModeTitle.text = loc.GetText("game_mode_title");

            var storyModeButton = root.Q<Button>("StoryModeButton");
            if (storyModeButton != null) storyModeButton.text = loc.GetText("game_mode_story");

            var endlessModeButton = root.Q<Button>("EndlessModeButton");
            if (endlessModeButton != null) endlessModeButton.text = loc.GetText("game_mode_endless");

            // StoryMode
            var storyModeTitle = root.Q<Label>("StoryModeTitle");
            if (storyModeTitle != null) storyModeTitle.text = loc.GetText("story_mode_title");

            // EndlessMode
            var endlessModeTitle = root.Q<Label>("EndlessModeTitle");
            if (endlessModeTitle != null) endlessModeTitle.text = loc.GetText("endless_mode_title");

            var rankingTitle = root.Q<Label>("RankingTitle");
            if (rankingTitle != null) rankingTitle.text = loc.GetText("endless_mode_best_score");

            var headerRank = root.Q<Label>("HeaderRank");
            if (headerRank != null) headerRank.text = loc.GetText("endless_mode_rank");

            var headerScore = root.Q<Label>("HeaderScore");
            if (headerScore != null) headerScore.text = loc.GetText("endless_mode_score");

            var headerWave = root.Q<Label>("HeaderWave");
            if (headerWave != null) headerWave.text = loc.GetText("endless_mode_wave");

            var headerDate = root.Q<Label>("HeaderDate");
            if (headerDate != null) headerDate.text = loc.GetText("endless_mode_date");

            var endlessPlayButton = root.Q<Button>("EndlessPlayButton");
            if (endlessPlayButton != null) endlessPlayButton.text = loc.GetText("endless_mode_start_game");

            // Options 패널 로컬라이제이션
            if (optionsPanelController != null)
            {
                optionsPanelController.UpdateLocalization(loc);
            }

            // Store 패널 로컬라이제이션
            if (storePanelController != null)
            {
                storePanelController.UpdateLocalization(loc);
            }
        }

        #endregion
    }
}
