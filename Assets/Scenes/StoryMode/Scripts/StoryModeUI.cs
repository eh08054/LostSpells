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
    /// 스토리 모드 UI - 챕터 선택
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class StoryModeUI : MonoBehaviour
    {
        private UIDocument uiDocument;
        private VisualElement root;
        private Button backButton;
        private VisualElement chapterListContainer;
        private Label titleLabel;

        private List<ChapterData> chapters;
        private PlayerSaveData saveData;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            root = uiDocument.rootVisualElement;

            // UI 요소 찾기
            titleLabel = root.Q<Label>("HeaderTitle");
            backButton = root.Q<Button>("BackButton");
            chapterListContainer = root.Q<VisualElement>("SlotListContainer");

            // 이벤트 등록
            if (backButton != null)
                backButton.clicked += OnBackButtonClicked;

            // Localization 이벤트 등록
            LocalizationManager.Instance.OnLanguageChanged += UpdateLocalization;

            // 챕터 리스트 로드 및 표시
            LoadChapters();

            // 현재 언어로 UI 업데이트
            UpdateLocalization();
        }

        private void OnDisable()
        {
            if (backButton != null)
                backButton.clicked -= OnBackButtonClicked;

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

        /// <summary>
        /// 챕터 데이터 로드 및 UI 생성
        /// </summary>
        private void LoadChapters()
        {
            // DataManager에서 챕터 데이터 가져오기
            chapters = DataManager.Instance.GetAllChapterData();

            // SaveManager에서 저장 데이터 가져오기
            saveData = SaveManager.Instance.GetCurrentSaveData();

            // 챕터 리스트 표시
            DisplayChapters();
        }

        /// <summary>
        /// 챕터 버튼들을 동적으로 생성하여 표시
        /// </summary>
        private void DisplayChapters()
        {
            if (chapterListContainer == null || chapters == null)
                return;

            // 기존 버튼들 제거
            chapterListContainer.Clear();

            // 각 챕터마다 버튼 생성
            foreach (var chapter in chapters)
            {
                CreateChapterButton(chapter);
            }
        }

        /// <summary>
        /// 챕터 버튼 생성
        /// </summary>
        private void CreateChapterButton(ChapterData chapterData)
        {
            // 버튼 컨테이너 생성
            var chapterButton = new Button();
            chapterButton.AddToClassList("chapter-button");

            // 챕터 정보 표시
            var chapterInfo = new VisualElement();
            chapterInfo.AddToClassList("chapter-info");

            var loc = LocalizationManager.Instance;

            // 챕터 번호
            var chapterText = loc.GetText("story_mode_chapter");
            var chapterNumber = new Label($"{chapterText} {chapterData.chapterId}");
            chapterNumber.AddToClassList("chapter-number");
            chapterInfo.Add(chapterNumber);

            // 챕터 이름
            var chapterName = new Label(chapterData.GetLocalizedName());
            chapterName.AddToClassList("chapter-name");
            chapterInfo.Add(chapterName);

            // 진행 상황 표시 (간결하게: "웨이브 2")
            var progress = saveData.GetChapterProgress(chapterData.chapterId);
            var waveText = loc.GetText("story_mode_wave");
            var progressInfo = new Label($"{waveText} {progress.clearedWaves}");
            progressInfo.AddToClassList("chapter-progress");
            chapterInfo.Add(progressInfo);

            chapterButton.Add(chapterInfo);

            // 잠금 상태 확인
            // TODO: 실제 클리어한 챕터 ID 리스트를 SaveData에서 가져와야 함
            var clearedChapterIds = new List<int>(); // 임시로 빈 리스트
            bool isLocked = chapterData.IsLocked(saveData.level, clearedChapterIds);

            if (isLocked)
            {
                // 잠금 오버레이 레이어 생성 (시각적 표시만)
                var lockOverlay = new VisualElement();
                lockOverlay.AddToClassList("lock-overlay");

                // 잠금 아이콘 (GUI_0)
                var lockIcon = new VisualElement();
                lockIcon.AddToClassList("lock-icon");
                lockOverlay.Add(lockIcon);

                chapterButton.Add(lockOverlay);
            }

            // 잠금 여부와 관계없이 클릭 이벤트 등록
            chapterButton.clicked += () => OnChapterButtonClicked(chapterData);

            // 컨테이너에 추가
            chapterListContainer.Add(chapterButton);
        }

        /// <summary>
        /// 챕터 버튼 클릭 시
        /// </summary>
        private void OnChapterButtonClicked(ChapterData chapterData)
        {
            // GameStateManager에 선택한 챕터 정보 저장
            GameStateManager.Instance.StartChapter(chapterData.chapterId);

            // InGame 씬으로 비동기 이동
            StartCoroutine(LoadSceneAsync("InGame"));
        }

        private void OnBackButtonClicked()
        {
            StartCoroutine(LoadSceneAsync("GameModeSelection"));
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

            // 로딩이 완료될 때까지 대기
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }

        private void UpdateLocalization()
        {
            var loc = LocalizationManager.Instance;

            if (titleLabel != null)
                titleLabel.text = loc.GetText("story_mode_title");
            // BackButton은 이미지만 사용하므로 텍스트 설정 안함
        }
    }
}
