using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace LostSpells.UI
{
    /// <summary>
    /// 스토리 모드 UI 컨트롤러
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class StoryModeUI : MonoBehaviour
    {
        private UIDocument uiDocument;
        private VisualElement root;
        private VisualElement currentSlotDisplay;
        private Label slotNumber;
        private Label slotChapter;
        private Label slotProgress;
        private Label slotDate;
        private Button actionButton;
        private Button deleteButton;
        private Button prevButton;
        private Button nextButton;

        // 슬롯 데이터 리스트 (동적으로 관리, Add New Slot 포함)
        private List<SaveSlotData> slotDataList = new List<SaveSlotData>();
        private int currentSlotIndex = 0;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            root = uiDocument.rootVisualElement;
            InitializeUI();
            LoadSaveData();
            UpdateCurrentSlotDisplay();
        }

        private void InitializeUI()
        {
            // 뒤로가기 버튼
            var backButton = root.Q<Button>("BackButton");
            if (backButton != null)
                backButton.clicked += OnBackButtonClicked;

            // UI 요소 참조
            currentSlotDisplay = root.Q<VisualElement>("CurrentSlotDisplay");
            slotNumber = root.Q<Label>("SlotNumber");
            slotChapter = root.Q<Label>("SlotChapter");
            slotProgress = root.Q<Label>("SlotProgress");
            slotDate = root.Q<Label>("SlotDate");

            actionButton = root.Q<Button>("ActionButton");
            deleteButton = root.Q<Button>("DeleteButton");
            prevButton = root.Q<Button>("PrevButton");
            nextButton = root.Q<Button>("NextButton");

            // 버튼 이벤트 등록
            if (actionButton != null)
                actionButton.clicked += OnActionButtonClicked;

            if (deleteButton != null)
                deleteButton.clicked += OnDeleteButtonClicked;

            if (prevButton != null)
                prevButton.clicked += OnPrevButtonClicked;

            if (nextButton != null)
                nextButton.clicked += OnNextButtonClicked;
        }

        private void LoadSaveData()
        {
            // TODO: 실제 세이브 데이터 로드
            // 임시 데이터
            slotDataList.Clear();
            slotDataList.Add(new SaveSlotData
            {
                slotNumber = 1,
                isEmpty = false,
                chapterName = "Chapter 3: The Dark Forest",
                level = 15,
                hoursPlayed = 12,
                lastPlayed = "2025-10-26 14:30"
            });
            slotDataList.Add(new SaveSlotData
            {
                slotNumber = 2,
                isEmpty = false,
                chapterName = "Chapter 1: The Beginning",
                level = 5,
                hoursPlayed = 3,
                lastPlayed = "2025-10-25 09:15"
            });

            // 항상 맨 끝에 빈 슬롯 하나 추가
            EnsureEmptySlotAtEnd();
        }

        private void EnsureEmptySlotAtEnd()
        {
            // 마지막 슬롯이 빈 슬롯인지 확인
            if (slotDataList.Count == 0 || !slotDataList[slotDataList.Count - 1].isEmpty)
            {
                slotDataList.Add(new SaveSlotData
                {
                    slotNumber = slotDataList.Count + 1,
                    isEmpty = true
                });
            }
        }

        private void UpdateCurrentSlotDisplay()
        {
            if (currentSlotDisplay == null) return;

            // 슬롯 번호 재정렬
            for (int i = 0; i < slotDataList.Count; i++)
            {
                slotDataList[i].slotNumber = i + 1;
            }

            // 인덱스 범위 체크
            if (currentSlotIndex < 0) currentSlotIndex = 0;
            if (currentSlotIndex >= slotDataList.Count) currentSlotIndex = slotDataList.Count - 1;

            // 현재 슬롯 표시
            SaveSlotData currentSlot = slotDataList[currentSlotIndex];
            ShowSlotData(currentSlot);

            // 화살표 버튼 활성화/비활성화
            // 왼쪽: 첫 번째 슬롯이 아니면 활성화
            if (prevButton != null)
                prevButton.SetEnabled(currentSlotIndex > 0);

            // 오른쪽: 현재 슬롯이 빈 슬롯이면 비활성화 (빈 슬롯은 항상 마지막)
            if (nextButton != null)
                nextButton.SetEnabled(!currentSlot.isEmpty && currentSlotIndex < slotDataList.Count - 1);
        }

        private void ShowSlotData(SaveSlotData data)
        {
            var slotCard = root.Q<VisualElement>("CurrentSlot");
            if (slotCard != null)
            {
                slotCard.RemoveFromClassList("add-new-slot-card");
                slotCard.style.display = DisplayStyle.Flex;
            }

            if (data.isEmpty)
            {
                // 빈 슬롯: "Empty Slot"만 중앙에 표시
                if (slotNumber != null)
                    slotNumber.style.display = DisplayStyle.None;

                if (slotChapter != null)
                {
                    slotChapter.text = "Empty Slot";
                    slotChapter.RemoveFromClassList("slot-chapter");
                    slotChapter.AddToClassList("empty-label");
                    slotChapter.style.display = DisplayStyle.Flex;
                }

                if (slotProgress != null)
                    slotProgress.style.display = DisplayStyle.None;

                if (slotDate != null)
                    slotDate.style.display = DisplayStyle.None;

                // 버튼 텍스트: New Game
                if (actionButton != null)
                {
                    actionButton.text = "New Game";
                    actionButton.style.display = DisplayStyle.Flex;
                }

                // Delete 버튼 숨김 (빈 슬롯은 삭제 불가)
                if (deleteButton != null)
                    deleteButton.style.display = DisplayStyle.None;
            }
            else
            {
                // 저장된 슬롯
                if (slotNumber != null)
                {
                    slotNumber.text = $"SLOT {data.slotNumber}";
                    slotNumber.style.display = DisplayStyle.Flex;
                }

                if (slotChapter != null)
                {
                    slotChapter.text = data.chapterName;
                    slotChapter.AddToClassList("slot-chapter");
                    slotChapter.RemoveFromClassList("empty-label");
                    slotChapter.style.display = DisplayStyle.Flex;
                }

                if (slotProgress != null)
                {
                    slotProgress.text = $"Level {data.level}  •  {data.hoursPlayed} hours played";
                    slotProgress.AddToClassList("slot-progress");
                    slotProgress.RemoveFromClassList("empty-label");
                    slotProgress.style.display = DisplayStyle.Flex;
                }

                if (slotDate != null)
                {
                    slotDate.text = $"Last played: {data.lastPlayed}";
                    slotDate.style.display = DisplayStyle.Flex;
                }

                // 버튼 텍스트: Continue
                if (actionButton != null)
                {
                    actionButton.text = "Continue";
                    actionButton.style.display = DisplayStyle.Flex;
                }

                // Delete 버튼 표시
                if (deleteButton != null)
                    deleteButton.style.display = DisplayStyle.Flex;
            }
        }

        #region 이벤트 핸들러

        private void OnBackButtonClicked()
        {
            SceneManager.LoadScene("MainMenu");
        }

        private void OnActionButtonClicked()
        {
            if (currentSlotIndex >= slotDataList.Count) return;

            SaveSlotData currentSlot = slotDataList[currentSlotIndex];

            if (currentSlot.isEmpty)
            {
                OnNewGame(currentSlot.slotNumber);
            }
            else
            {
                OnContinueGame(currentSlot.slotNumber);
            }
        }

        private void OnDeleteButtonClicked()
        {
            if (currentSlotIndex >= slotDataList.Count) return;

            SaveSlotData currentSlot = slotDataList[currentSlotIndex];

            // 빈 슬롯은 삭제 불가
            if (currentSlot.isEmpty)
            {
                Debug.Log("빈 슬롯은 삭제할 수 없습니다.");
                return;
            }

            OnDeleteSlot(currentSlot.slotNumber);
        }

        private void OnDeleteSlot(int slotNumber)
        {
            Debug.Log($"슬롯 {slotNumber} 삭제");

            // TODO: 삭제 확인 다이얼로그 표시

            // 슬롯 찾기
            int index = slotDataList.FindIndex(s => s.slotNumber == slotNumber);
            if (index >= 0 && !slotDataList[index].isEmpty)
            {
                slotDataList.RemoveAt(index);

                // 인덱스 조정
                if (currentSlotIndex >= slotDataList.Count)
                {
                    currentSlotIndex = slotDataList.Count - 1;
                }
                if (currentSlotIndex < 0)
                {
                    currentSlotIndex = 0;
                }

                // 빈 슬롯이 마지막에 있는지 확인
                EnsureEmptySlotAtEnd();

                UpdateCurrentSlotDisplay();

                // TODO: 실제 세이브 파일 삭제
            }
        }

        private void OnContinueGame(int slotNumber)
        {
            Debug.Log($"게임 계속하기: 슬롯 {slotNumber}");
            // TODO: 해당 슬롯의 세이브 데이터 로드
            // TODO: 게임 씬으로 전환
        }

        private void OnNewGame(int slotNumber)
        {
            Debug.Log($"새 게임 시작: 슬롯 {slotNumber}");

            // TODO: 새 게임 시작 확인
            // TODO: 게임 씬으로 전환하고 새 세이브 생성

            // 임시: 빈 슬롯을 저장된 슬롯으로 변경 (데모용)
            int index = slotDataList.FindIndex(s => s.slotNumber == slotNumber);
            if (index >= 0 && slotDataList[index].isEmpty)
            {
                slotDataList[index].isEmpty = false;
                slotDataList[index].chapterName = "Chapter 1: New Beginning";
                slotDataList[index].level = 1;
                slotDataList[index].hoursPlayed = 0;
                slotDataList[index].lastPlayed = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");

                // 새로운 빈 슬롯 추가
                EnsureEmptySlotAtEnd();

                UpdateCurrentSlotDisplay();
            }
        }

        private void OnPrevButtonClicked()
        {
            if (currentSlotIndex > 0)
            {
                currentSlotIndex--;
                UpdateCurrentSlotDisplay();
            }
        }

        private void OnNextButtonClicked()
        {
            // 빈 슬롯이 아니고, 마지막 슬롯이 아니면 다음으로 이동
            if (currentSlotIndex < slotDataList.Count - 1)
            {
                SaveSlotData currentSlot = slotDataList[currentSlotIndex];
                if (!currentSlot.isEmpty)
                {
                    currentSlotIndex++;
                    UpdateCurrentSlotDisplay();
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// 세이브 슬롯 데이터
    /// </summary>
    [System.Serializable]
    public class SaveSlotData
    {
        public int slotNumber;
        public bool isEmpty;
        public string chapterName;
        public int level;
        public int hoursPlayed;
        public string lastPlayed;
    }
}
