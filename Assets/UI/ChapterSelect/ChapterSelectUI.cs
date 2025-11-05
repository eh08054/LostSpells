using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using LostSpells.Systems;
using LostSpells.Data.Save;

namespace LostSpells.UI
{
    /// <summary>
    /// 챕터 선택 UI 컨트롤러 - 7대죄악 챕터 관리 (GameManager 없이 SaveSystem 직접 사용)
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ChapterSelectUI : MonoBehaviour
    {
        [System.Serializable]
        public class ChapterData
        {
            public int chapterNumber;
            public string chapterName;
            public bool isUnlocked;
            public bool isCompleted;
            public int currentStage;
            public int totalStages;

            public ChapterData(int number, string name, bool unlocked = false, int current = 0, int total = 10)
            {
                chapterNumber = number;
                chapterName = name;
                isUnlocked = unlocked;
                isCompleted = false;
                currentStage = current;
                totalStages = total;
            }
        }

        private UIDocument uiDocument;
        private Button backButton;
        private Button prevButton;
        private Button nextButton;

        // 챕터 카드 요소들
        private VisualElement leftChapter;
        private VisualElement centerChapter;
        private VisualElement rightChapter;

        // 왼쪽 카드 요소
        private Label leftChapterNumber;
        private Label leftChapterTitle;
        private Label leftChapterProgress;
        private Label leftChapterStatus;
        private VisualElement leftChapterLock;

        // 중앙 카드 요소
        private Label centerChapterNumber;
        private Label centerChapterTitle;
        private Label centerChapterProgress;
        private Label centerChapterStatus;
        private VisualElement centerChapterLock;

        // 오른쪽 카드 요소
        private Label rightChapterNumber;
        private Label rightChapterTitle;
        private Label rightChapterProgress;
        private Label rightChapterStatus;
        private VisualElement rightChapterLock;

        // 챕터 데이터 (7대죄악) - 챕터명은 ChapterProgressSystem에서 가져옴
        private ChapterData[] chapters = new ChapterData[]
        {
            new ChapterData(1, ChapterProgressSystem.GetChapterName(1), true, 0),   // Pride (교만) - 처음엔 해금됨
            new ChapterData(2, ChapterProgressSystem.GetChapterName(2), false),     // Greed (탐욕)
            new ChapterData(3, ChapterProgressSystem.GetChapterName(3), false),     // Lust (색욕)
            new ChapterData(4, ChapterProgressSystem.GetChapterName(4), false),     // Envy (질투)
            new ChapterData(5, ChapterProgressSystem.GetChapterName(5), false),     // Gluttony (폭식)
            new ChapterData(6, ChapterProgressSystem.GetChapterName(6), false),     // Wrath (분노)
            new ChapterData(7, ChapterProgressSystem.GetChapterName(7), false)      // Sloth (나태)
        };

        private int centerIndex = 0; // 중앙 카드 인덱스 (0부터 시작 - 챕터 1이 중앙에)

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            var root = uiDocument.rootVisualElement;

            // UI 요소 찾기
            backButton = root.Q<Button>("BackButton");
            prevButton = root.Q<Button>("PrevButton");
            nextButton = root.Q<Button>("NextButton");

            // 챕터 카드들
            leftChapter = root.Q<VisualElement>("LeftChapter");
            centerChapter = root.Q<VisualElement>("CenterChapter");
            rightChapter = root.Q<VisualElement>("RightChapter");

            // 왼쪽 카드 요소
            leftChapterNumber = root.Q<Label>("LeftChapterNumber");
            leftChapterTitle = root.Q<Label>("LeftChapterTitle");
            leftChapterProgress = root.Q<Label>("LeftChapterProgress");
            leftChapterStatus = root.Q<Label>("LeftChapterStatus");
            leftChapterLock = root.Q<VisualElement>("LeftChapterLock");

            // 중앙 카드 요소
            centerChapterNumber = root.Q<Label>("CenterChapterNumber");
            centerChapterTitle = root.Q<Label>("CenterChapterTitle");
            centerChapterProgress = root.Q<Label>("CenterChapterProgress");
            centerChapterStatus = root.Q<Label>("CenterChapterStatus");
            centerChapterLock = root.Q<VisualElement>("CenterChapterLock");

            // 오른쪽 카드 요소
            rightChapterNumber = root.Q<Label>("RightChapterNumber");
            rightChapterTitle = root.Q<Label>("RightChapterTitle");
            rightChapterProgress = root.Q<Label>("RightChapterProgress");
            rightChapterStatus = root.Q<Label>("RightChapterStatus");
            rightChapterLock = root.Q<VisualElement>("RightChapterLock");

            // 이벤트 등록
            if (backButton != null)
                backButton.clicked += OnBackButtonClicked;

            if (prevButton != null)
                prevButton.clicked += OnPrevButtonClicked;

            if (nextButton != null)
                nextButton.clicked += OnNextButtonClicked;

            if (leftChapter != null)
            {
                leftChapter.RegisterCallback<ClickEvent>(evt => OnSideChapterClicked(-1));
            }
            else
            {
                Debug.LogError("[ChapterSelect] leftChapter가 null입니다!");
            }

            if (centerChapter != null)
            {
                centerChapter.RegisterCallback<ClickEvent>(evt => OnSideChapterClicked(0));
            }
            else
            {
                Debug.LogError("[ChapterSelect] centerChapter가 null입니다!");
            }

            if (rightChapter != null)
            {
                rightChapter.RegisterCallback<ClickEvent>(evt => OnSideChapterClicked(1));
            }
            else
            {
                Debug.LogError("[ChapterSelect] rightChapter가 null입니다!");
            }

            // 저장된 진행 상황 불러오기
            LoadChapterProgress();

            // 초기 표시
            UpdateChapterDisplay();
        }

        private void OnDisable()
        {
            // 이벤트 해제
            if (backButton != null)
                backButton.clicked -= OnBackButtonClicked;

            if (prevButton != null)
                prevButton.clicked -= OnPrevButtonClicked;

            if (nextButton != null)
                nextButton.clicked -= OnNextButtonClicked;

            if (leftChapter != null)
                leftChapter.UnregisterCallback<ClickEvent>(evt => OnSideChapterClicked(-1));

            if (centerChapter != null)
                centerChapter.UnregisterCallback<ClickEvent>(evt => OnSideChapterClicked(0));

            if (rightChapter != null)
                rightChapter.UnregisterCallback<ClickEvent>(evt => OnSideChapterClicked(1));
        }

        #region Button Click Handlers

        private void OnBackButtonClicked()
        {
            SceneManager.LoadScene("GameModeSelection");
        }

        private void OnPrevButtonClicked()
        {
            if (centerIndex > 0)
            {
                centerIndex--;
                UpdateChapterDisplay();
            }
        }

        private void OnNextButtonClicked()
        {
            if (centerIndex < chapters.Length - 1)
            {
                centerIndex++;
                UpdateChapterDisplay();
            }
        }

        private void OnSideChapterClicked(int direction)
        {
            // direction: -1 = 왼쪽 카드 클릭, 0 = 중앙 카드 클릭, +1 = 오른쪽 카드 클릭

            // 화면에 표시된 중앙 카드의 실제 인덱스 계산 (UpdateChapterDisplay와 동일한 로직)
            int displayCenterIndex = centerIndex;
            if (centerIndex == 0)
                displayCenterIndex = 1;
            if (centerIndex == chapters.Length - 1)
                displayCenterIndex = chapters.Length - 2;

            int targetIndex = displayCenterIndex + direction;

            // 범위 확인
            if (targetIndex < 0 || targetIndex >= chapters.Length)
            {
                Debug.LogWarning($"[ChapterSelect] 범위 벗어남 - targetIndex: {targetIndex}");
                return;
            }

            ChapterData selectedChapter = chapters[targetIndex];

            // 카드 정보 출력
            string statusInfo = selectedChapter.isUnlocked ? "열림" : "잠김";
            string completeInfo = "";
            if (selectedChapter.isUnlocked)
            {
                if (selectedChapter.isCompleted)
                {
                    completeInfo = $", 진행도: {selectedChapter.currentStage}/{selectedChapter.totalStages}, 완료 여부: 완료됨";
                }
                else if (selectedChapter.currentStage > 0)
                {
                    completeInfo = $", 진행도: {selectedChapter.currentStage}/{selectedChapter.totalStages}, 완료 여부: 진행 중";
                }
                else
                {
                    completeInfo = $", 진행도: {selectedChapter.currentStage}/{selectedChapter.totalStages}, 완료 여부: 시작 안 함";
                }
            }

            // 어떤 카드든 클릭하면 - 해금되어 있으면 바로 게임 시작
            if (selectedChapter.isUnlocked)
            {
                StartChapter(selectedChapter.chapterNumber);
            }
            else
            {
                Debug.LogWarning($"[ChapterSelect] 챕터 {selectedChapter.chapterNumber}는 잠겨있습니다!");
            }
        }

        #endregion

        #region Chapter Display

        private void UpdateChapterDisplay()
        {
            // 화면에 항상 3장의 카드를 표시하기 위해 중앙 카드의 실제 표시 위치 조정
            int displayCenterIndex = centerIndex;

            // 첫 페이지에서도 3장 표시 (챕터 1, 2, 3)
            if (centerIndex == 0)
                displayCenterIndex = 1;

            // 마지막 페이지에서도 3장 표시 (챕터 5, 6, 7)
            if (centerIndex == chapters.Length - 1)
                displayCenterIndex = chapters.Length - 2;

            int leftIndex = displayCenterIndex - 1;
            int rightIndex = displayCenterIndex + 1;

            // 왼쪽 카드 (항상 표시)
            UpdateChapterCard(leftIndex, leftChapter,
                leftChapterNumber, leftChapterTitle, leftChapterProgress,
                leftChapterStatus, leftChapterLock);
            leftChapter.style.display = DisplayStyle.Flex;

            // 중앙 카드 (항상 표시)
            UpdateChapterCard(displayCenterIndex, centerChapter,
                centerChapterNumber, centerChapterTitle, centerChapterProgress,
                centerChapterStatus, centerChapterLock);
            centerChapter.style.display = DisplayStyle.Flex;

            // 오른쪽 카드 (항상 표시)
            UpdateChapterCard(rightIndex, rightChapter,
                rightChapterNumber, rightChapterTitle, rightChapterProgress,
                rightChapterStatus, rightChapterLock);
            rightChapter.style.display = DisplayStyle.Flex;

            // 화살표 버튼 활성화/비활성화
            // centerIndex가 0일 때 왼쪽 끝
            // centerIndex가 chapters.Length - 1일 때 오른쪽 끝
            if (prevButton != null)
                prevButton.SetEnabled(centerIndex > 0);

            if (nextButton != null)
                nextButton.SetEnabled(centerIndex < chapters.Length - 1);
        }

        private void UpdateChapterCard(int index, VisualElement card, Label numberLabel, Label titleLabel,
            Label progressLabel, Label statusLabel, VisualElement lockIcon)
        {
            if (index < 0 || index >= chapters.Length)
                return;

            ChapterData data = chapters[index];

            // 카드 잠금 상태에 따라 어두운 tint 적용
            if (card != null)
            {
                if (data.isUnlocked)
                {
                    card.RemoveFromClassList("locked-card");
                }
                else
                {
                    card.AddToClassList("locked-card");
                }
            }

            // 챕터 번호
            if (numberLabel != null)
                numberLabel.text = $"CHAPTER {data.chapterNumber}";

            // 챕터 제목
            if (titleLabel != null)
                titleLabel.text = data.chapterName;

            // 레벨 및 웨이브 정보 표시
            if (progressLabel != null)
            {
                if (data.isUnlocked)
                {
                    // SaveSystem에서 현재 슬롯 데이터 가져오기
                    int currentSlot = GameStateManager.CurrentSlot;
                    if (GameStateManager.IsSlotUsed(currentSlot))
                    {
                        var playerData = GameStateManager.GetSlotData(currentSlot);
                        progressLabel.text = $"Lv.{playerData.level}  |  Wave {playerData.currentWave}";
                        progressLabel.style.color = new StyleColor(new Color(0.4f, 0.24f, 0.12f));
                    }
                    else
                    {
                        progressLabel.text = "Lv.1  |  Wave 1";
                        progressLabel.style.color = new StyleColor(new Color(0.4f, 0.24f, 0.12f));
                    }
                }
                else
                {
                    progressLabel.text = "Locked";
                    progressLabel.style.color = new StyleColor(new Color(0.5f, 0.5f, 0.5f));
                }
            }

            // 잠금 상태
            if (statusLabel != null)
            {
                statusLabel.RemoveFromClassList("locked");
                statusLabel.RemoveFromClassList("unlocked");
                statusLabel.RemoveFromClassList("completed");

                if (data.isCompleted)
                {
                    statusLabel.text = "COMPLETED";
                    statusLabel.AddToClassList("completed");
                }
                else if (data.isUnlocked)
                {
                    statusLabel.text = "UNLOCKED";
                    statusLabel.AddToClassList("unlocked");
                }
                else
                {
                    statusLabel.text = "LOCKED";
                    statusLabel.AddToClassList("locked");
                }
            }

            // 잠금 아이콘
            if (lockIcon != null)
            {
                lockIcon.style.display = data.isUnlocked ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        #endregion

        #region Data Management

        private void LoadChapterProgress()
        {
            // SaveSystem에서 직접 현재 슬롯의 챕터 진행 상황 불러오기
            int currentSlot = GameStateManager.CurrentSlot;
            var chapterProgressList = SaveSystem.GetAllChapterProgress(currentSlot, 7);

            // 불러온 데이터로 chapters 배열 업데이트
            for (int i = 0; i < chapterProgressList.Count && i < chapters.Length; i++)
            {
                var progressInfo = chapterProgressList[i];
                chapters[i].isUnlocked = progressInfo.isUnlocked;
                chapters[i].isCompleted = progressInfo.isCompleted;
                chapters[i].currentStage = progressInfo.currentStage;
                chapters[i].totalStages = progressInfo.totalStages;
            }
        }

        public void UnlockNextChapter(int completedChapterIndex)
        {
            if (completedChapterIndex >= 0 && completedChapterIndex < chapters.Length)
            {
                chapters[completedChapterIndex].isCompleted = true;

                // 다음 챕터 해금
                if (completedChapterIndex + 1 < chapters.Length)
                {
                    chapters[completedChapterIndex + 1].isUnlocked = true;
                }

                UpdateChapterDisplay();

                // SaveSystem을 통해 진행 상황 저장
                int chapterNumber = completedChapterIndex + 1; // 챕터는 1부터 시작
                int currentSlot = GameStateManager.CurrentSlot;

                SaveSystem.SaveChapterProgress(
                    currentSlot,
                    chapterNumber,
                    chapters[completedChapterIndex].isUnlocked,
                    chapters[completedChapterIndex].isCompleted,
                    chapters[completedChapterIndex].currentStage,
                    chapters[completedChapterIndex].totalStages
                );

                // 다음 챕터 해금 상태도 저장
                if (completedChapterIndex + 1 < chapters.Length)
                {
                    SaveSystem.SaveChapterProgress(
                        currentSlot,
                        chapterNumber + 1,
                        chapters[completedChapterIndex + 1].isUnlocked,
                        chapters[completedChapterIndex + 1].isCompleted,
                        chapters[completedChapterIndex + 1].currentStage,
                        chapters[completedChapterIndex + 1].totalStages
                    );
                }
            }
        }

        /// <summary>
        /// 챕터 진행 상황 업데이트 및 저장
        /// </summary>
        public void UpdateChapterProgress(int chapterIndex, int currentStage)
        {
            if (chapterIndex >= 0 && chapterIndex < chapters.Length)
            {
                chapters[chapterIndex].currentStage = currentStage;

                // 진행 상황 저장
                int chapterNumber = chapterIndex + 1; // 챕터는 1부터 시작
                int currentSlot = GameStateManager.CurrentSlot;

                SaveSystem.SaveChapterProgress(
                    currentSlot,
                    chapterNumber,
                    chapters[chapterIndex].isUnlocked,
                    chapters[chapterIndex].isCompleted,
                    chapters[chapterIndex].currentStage,
                    chapters[chapterIndex].totalStages
                );

                UpdateChapterDisplay();
            }
        }

        private void StartChapter(int chapterNumber)
        {
            Debug.Log($"[ChapterSelect] 챕터 {chapterNumber} 시작");

            // 선택된 챕터 설정
            GameStateManager.SelectedChapter = chapterNumber;
            GameStateManager.CurrentGameMode = GameMode.ChapterSelect;

            // InGame 씬으로 이동
            SceneManager.LoadScene("InGame");
        }

        #endregion
    }
}
