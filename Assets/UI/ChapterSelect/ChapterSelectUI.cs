using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace LostSpells.UI
{
    /// <summary>
    /// 챕터 선택 UI 컨트롤러 - 7대죄악 챕터 관리
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

        // 챕터 데이터 (7대죄악)
        private ChapterData[] chapters = new ChapterData[]
        {
            new ChapterData(1, "Pride", true, 0),      // 교만 - 처음엔 해금됨
            new ChapterData(2, "Greed", false),        // 탐욕
            new ChapterData(3, "Lust", false),         // 색욕
            new ChapterData(4, "Envy", false),         // 질투
            new ChapterData(5, "Gluttony", false),     // 탐식
            new ChapterData(6, "Wrath", false),        // 분노
            new ChapterData(7, "Sloth", false)         // 나태
        };

        private int centerIndex = 1; // 중앙 카드 인덱스 (1부터 시작해서 양쪽에 카드가 있도록)

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
                leftChapter.RegisterCallback<ClickEvent>(evt => OnSideChapterClicked(-1));

            if (centerChapter != null)
                centerChapter.RegisterCallback<ClickEvent>(evt => OnSideChapterClicked(0));

            if (rightChapter != null)
                rightChapter.RegisterCallback<ClickEvent>(evt => OnSideChapterClicked(1));

            // TODO: 저장된 진행 상황 불러오기
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
            if (centerIndex > 1)
            {
                centerIndex--;
                UpdateChapterDisplay();
            }
        }

        private void OnNextButtonClicked()
        {
            if (centerIndex < chapters.Length - 2)
            {
                centerIndex++;
                UpdateChapterDisplay();
            }
        }

        private void OnSideChapterClicked(int direction)
        {
            // direction: -1 = 왼쪽 카드 클릭, 0 = 중앙 카드 클릭, +1 = 오른쪽 카드 클릭
            int targetIndex = centerIndex + direction;

            // 범위 확인
            if (targetIndex < 0 || targetIndex >= chapters.Length)
                return;

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
            Debug.Log($"[ChapterSelect] 챕터 {selectedChapter.chapterNumber} - {selectedChapter.chapterName}, 상태: {statusInfo}{completeInfo}");

            // 중앙 카드가 아니고 해금된 카드라면 중앙으로 이동
            if (direction != 0 && selectedChapter.isUnlocked)
            {
                // 범위 확인 (중앙은 항상 1~5 사이여야 양쪽에 카드가 보임)
                if (targetIndex >= 1 && targetIndex <= chapters.Length - 2)
                {
                    centerIndex = targetIndex;
                    UpdateChapterDisplay();
                }
            }
        }

        #endregion

        #region Chapter Display

        private void UpdateChapterDisplay()
        {
            int leftIndex = centerIndex - 1;
            int rightIndex = centerIndex + 1;

            // 왼쪽 카드 (항상 표시)
            UpdateChapterCard(leftIndex, leftChapter,
                leftChapterNumber, leftChapterTitle, leftChapterProgress,
                leftChapterStatus, leftChapterLock);
            leftChapter.style.display = DisplayStyle.Flex;

            // 중앙 카드 (항상 표시)
            UpdateChapterCard(centerIndex, centerChapter,
                centerChapterNumber, centerChapterTitle, centerChapterProgress,
                centerChapterStatus, centerChapterLock);
            centerChapter.style.display = DisplayStyle.Flex;

            // 오른쪽 카드 (항상 표시)
            UpdateChapterCard(rightIndex, rightChapter,
                rightChapterNumber, rightChapterTitle, rightChapterProgress,
                rightChapterStatus, rightChapterLock);
            rightChapter.style.display = DisplayStyle.Flex;

            // 화살표 버튼 활성화/비활성화
            // centerIndex가 1일 때 왼쪽 끝 (챕터 1,2,3)
            // centerIndex가 chapters.Length - 2일 때 오른쪽 끝 (챕터 5,6,7)
            if (prevButton != null)
                prevButton.SetEnabled(centerIndex > 1);

            if (nextButton != null)
                nextButton.SetEnabled(centerIndex < chapters.Length - 2);
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

            // 진행 상황
            if (progressLabel != null)
            {
                if (data.isCompleted)
                {
                    progressLabel.text = "COMPLETED!";
                    progressLabel.style.color = new StyleColor(new Color(0.8f, 0.6f, 0.2f));
                }
                else
                {
                    progressLabel.text = $"Progress: {data.currentStage}/{data.totalStages}";
                    progressLabel.style.color = new StyleColor(new Color(0.4f, 0.24f, 0.12f));
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
            // TODO: PlayerPrefs 또는 세이브 시스템에서 진행 상황 불러오기
            // 예시: 테스트용으로 첫 번째 챕터만 해금

            // 테스트: 챕터 1은 진행 중, 챕터 2는 해금됨
            // chapters[0].currentStage = 3;
            // chapters[1].isUnlocked = true;
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

                // TODO: 진행 상황 저장
            }
        }

        #endregion
    }
}
