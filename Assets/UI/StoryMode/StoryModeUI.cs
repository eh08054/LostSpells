using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using LostSpells.Systems;
using LostSpells.Data;
using LostSpells.Data.Save;

namespace LostSpells.UI
{
    /// <summary>
    /// 스토리 모드 UI 컨트롤러
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class StoryModeUI : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("자동으로 빈 슬롯을 마지막에 추가")]
        [SerializeField] private bool autoAddEmptySlot = true;

        [Header("Components")]
        [Tooltip("SlotInfo 컴포넌트 (자동 검색)")]
        [SerializeField] private SlotInfoComponent slotInfoComponent;

        private UIDocument uiDocument;
        private VisualElement root;
        private VisualElement slotListContainer;

        // 슬롯 데이터 리스트 (빈 슬롯은 isUsed = false)
        private List<SaveSlotInfo> slotDataList = new List<SaveSlotInfo>();

        // 마지막 슬롯 카운트 (변경 감지용)
        private int lastSlotCount = -1;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();

            // SlotInfoComponent 자동 검색
            if (slotInfoComponent == null)
            {
                GameObject gameManager = GameObject.Find("GameManager");
                if (gameManager != null)
                {
                    slotInfoComponent = gameManager.GetComponent<SlotInfoComponent>();
                }
            }
        }

        private void OnEnable()
        {
            root = uiDocument.rootVisualElement;
            InitializeUI();
            LoadSaveData();
            RenderAllSlots();

            // 초기 슬롯 카운트 저장
            lastSlotCount = slotDataList.FindAll(s => s.isUsed).Count;

            // 1초마다 변경사항 확인 후 필요시에만 UI 새로고침
            InvokeRepeating(nameof(RefreshSlotData), 1f, 1f);
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(RefreshSlotData));
        }

        /// <summary>
        /// 주기적으로 슬롯 데이터 새로고침 (변경사항이 있을 때만)
        /// </summary>
        private void RefreshSlotData()
        {
            // SaveSystem에서 사용 중인 슬롯 데이터 확인
            List<SaveSlotInfo> savedSlots = SaveSystem.GetAllSlots();
            int currentSlotCount = 0;
            foreach (var slot in savedSlots)
            {
                if (slot.isUsed) currentSlotCount++;
            }

            // 슬롯 개수가 변경된 경우 UI 새로고침
            if (currentSlotCount != lastSlotCount)
            {
                lastSlotCount = currentSlotCount;
                LoadSaveData();
                RenderAllSlots();
                return;
            }

            // 개수가 같아도 데이터가 변경되었는지 확인
            if (HasSlotDataChanged(savedSlots))
            {
                LoadSaveData();
                RenderAllSlots();
            }
        }

        /// <summary>
        /// 슬롯 데이터가 변경되었는지 확인
        /// </summary>
        private bool HasSlotDataChanged(List<SaveSlotInfo> savedSlots)
        {
            // 현재 표시 중인 슬롯과 저장된 슬롯 비교 (빈 슬롯 제외)
            var currentUsedSlots = slotDataList.FindAll(s => s.isUsed);

            if (currentUsedSlots.Count != savedSlots.Count)
                return true;

            for (int i = 0; i < currentUsedSlots.Count; i++)
            {
                var current = currentUsedSlots[i];
                var saved = savedSlots.Find(s => s.slotNumber == current.slotNumber);

                if (saved == null) return true;

                // 각 필드 비교
                if (current.slotName != saved.slotName ||
                    current.chapterNumber != saved.chapterNumber ||
                    current.chapterName != saved.chapterName ||
                    current.lastPlayed != saved.lastPlayed)
                {
                    return true;
                }
            }

            return false;
        }

        private void InitializeUI()
        {
            // 뒤로가기 버튼
            var backButton = root.Q<Button>("BackButton");
            if (backButton != null)
                backButton.clicked += OnBackButtonClicked;

            // 슬롯 리스트 컨테이너 참조
            slotListContainer = root.Q<VisualElement>("SlotListContainer");
        }

        private void LoadSaveData()
        {
            slotDataList.Clear();

            // SaveSystem에서 데이터 불러오기
            List<SaveSlotInfo> savedSlots = SaveSystem.GetAllSlots();

            // 모든 사용 중인 슬롯 추가
            foreach (var slotInfo in savedSlots)
            {
                if (slotInfo.isUsed)
                {
                    slotDataList.Add(slotInfo);
                }
            }

            // 설정에 따라 빈 슬롯 자동 추가
            if (autoAddEmptySlot)
            {
                EnsureEmptySlotAtEnd();
            }
        }

        private void EnsureEmptySlotAtEnd()
        {
            // 마지막 슬롯이 빈 슬롯인지 확인 (isUsed가 false인 슬롯)
            if (slotDataList.Count == 0 || slotDataList[slotDataList.Count - 1].isUsed)
            {
                // 빈 슬롯 번호 찾기 (1부터 시작해서 사용되지 않은 첫 번째 번호)
                List<int> usedSlotNumbers = new List<int>();
                foreach (var slot in slotDataList)
                {
                    if (slot.isUsed)
                    {
                        usedSlotNumbers.Add(slot.slotNumber);
                    }
                }
                usedSlotNumbers.Sort();

                int nextSlotNumber = 1;
                while (usedSlotNumbers.Contains(nextSlotNumber))
                {
                    nextSlotNumber++;
                }

                // 빈 슬롯 추가 (isUsed = false)
                slotDataList.Add(new SaveSlotInfo
                {
                    slotNumber = nextSlotNumber,
                    isUsed = false,
                    slotName = "",
                    level = 0,
                    chapterNumber = 0,
                    chapterName = "",
                    currentWave = 0,
                    lastPlayed = ""
                });
            }
        }

        /// <summary>
        /// 모든 슬롯을 스크롤 뷰에 렌더링
        /// </summary>
        private void RenderAllSlots()
        {
            if (slotListContainer == null) return;

            // 기존 슬롯 카드 모두 제거
            slotListContainer.Clear();

            // 각 슬롯에 대한 카드 생성 (인덱스와 함께)
            for (int i = 0; i < slotDataList.Count; i++)
            {
                // 빈 슬롯이 아닌 경우에만 인덱스를 1부터 시작
                int displayIndex = i + 1;
                CreateSlotCard(slotDataList[i], displayIndex);
            }
        }

        /// <summary>
        /// 개별 슬롯 카드 생성
        /// </summary>
        /// <param name="slotData">슬롯 데이터</param>
        /// <param name="displayIndex">UI에 표시할 슬롯 인덱스 (1부터 시작)</param>
        private void CreateSlotCard(SaveSlotInfo slotData, int displayIndex)
        {
            // 슬롯 카드 컨테이너
            var slotCard = new VisualElement();
            slotCard.AddToClassList("slot-card");

            // 클로저를 위해 로컬 변수에 복사
            int capturedSlotNumber = slotData.slotNumber;
            bool capturedIsEmpty = !slotData.isUsed;
            int capturedDisplayIndex = displayIndex;

            if (!slotData.isUsed)  // 빈 슬롯
            {
                // 빈 슬롯: 카드 전체를 클릭 가능하게
                slotCard.AddToClassList("empty-slot-card");

                // 슬롯 정보 영역 (가운데 정렬)
                var slotInfo = new VisualElement();
                slotInfo.AddToClassList("slot-info");
                slotInfo.AddToClassList("empty-slot-info");

                var emptyLabel = new Label("+ New Slot");
                emptyLabel.AddToClassList("empty-label");
                slotInfo.Add(emptyLabel);

                slotCard.Add(slotInfo);

                // 카드 전체에 클릭 이벤트
                slotCard.RegisterCallback<ClickEvent>(evt =>
                {
                    OnAddNewSlot(capturedSlotNumber);
                });
            }
            else
            {
                // 저장된 슬롯: 상세 정보와 버튼 표시
                var slotInfo = new VisualElement();
                slotInfo.AddToClassList("slot-info");

                // 슬롯 이름 (클릭하여 편집 가능)
                // 빈 문자열이면 "Slot {인덱스}" 표시, 아니면 슬롯 이름 표시
                string displayName = string.IsNullOrEmpty(slotData.slotName) ? $"Slot {capturedDisplayIndex}" : slotData.slotName;
                var slotNameLabel = new Label(displayName);
                slotNameLabel.AddToClassList("slot-name");
                slotNameLabel.AddToClassList("editable-label");

                // 슬롯 이름 클릭 시 TextField로 전환
                slotNameLabel.RegisterCallback<ClickEvent>(evt =>
                {
                    var textField = new TextField();
                    // 편집 시작할 때는 실제 저장된 슬롯 이름에서 시작 (빈 문자열일 수 있음)
                    textField.value = slotData.slotName;
                    textField.AddToClassList("slot-name-input");

                    // TextField에서 포커스를 잃으면 저장하고 UI 새로고침
                    textField.RegisterCallback<BlurEvent>(blurEvt =>
                    {
                        string newName = textField.value.Trim();
                        if (newName != slotData.slotName)
                        {
                            SaveSystem.SaveSlotName(capturedSlotNumber, newName);

                            // SlotInfoComponent 동기화
                            if (slotInfoComponent != null)
                            {
                                slotInfoComponent.LoadSlotInfo();
                            }

                            // 전체 UI 즉시 새로고침
                            LoadSaveData();
                            RenderAllSlots();
                        }
                        else
                        {
                            // 변경사항이 없으면 그냥 Label로 되돌리기
                            slotInfo.Remove(textField);
                            slotInfo.Insert(0, slotNameLabel);
                        }
                    });

                    // Enter 키로도 저장 가능
                    textField.RegisterCallback<KeyDownEvent>(keyEvt =>
                    {
                        if (keyEvt.keyCode == KeyCode.Return || keyEvt.keyCode == KeyCode.KeypadEnter)
                        {
                            textField.Blur();
                        }
                        else if (keyEvt.keyCode == KeyCode.Escape)
                        {
                            textField.value = slotData.slotName;
                            textField.Blur();
                        }
                    });

                    slotInfo.Remove(slotNameLabel);
                    slotInfo.Insert(0, textField);
                    textField.Focus();
                    textField.SelectAll();
                });

                slotInfo.Add(slotNameLabel);

                // 진행상황 (챕터 번호와 이름)
                var progressLabel = new Label($"Chapter {slotData.chapterNumber}: {slotData.chapterName}");
                progressLabel.AddToClassList("slot-progress");
                slotInfo.Add(progressLabel);

                // 마지막 플레이 시간
                var dateLabel = new Label(slotData.lastPlayed);
                dateLabel.AddToClassList("slot-date");
                slotInfo.Add(dateLabel);

                slotCard.Add(slotInfo);

                // 버튼 영역
                var buttonContainer = new VisualElement();
                buttonContainer.AddToClassList("bottom-buttons");

                // Continue 버튼
                var continueButton = new Button();
                continueButton.AddToClassList("action-btn");
                continueButton.text = "Continue";
                continueButton.clicked += () =>
                {
                    OnContinueGame(capturedSlotNumber);
                };
                buttonContainer.Add(continueButton);

                // Delete 버튼
                var deleteButton = new Button();
                deleteButton.AddToClassList("action-btn");
                deleteButton.text = "Delete";
                deleteButton.clicked += () =>
                {
                    OnDeleteSlot(capturedSlotNumber);
                };
                buttonContainer.Add(deleteButton);

                slotCard.Add(buttonContainer);
            }

            // 슬롯 카드를 리스트 컨테이너에 추가
            slotListContainer.Add(slotCard);
        }


        #region 이벤트 핸들러

        private void OnBackButtonClicked()
        {
            SceneManager.LoadScene("GameModeSelection");
        }

        private void OnAddNewSlot(int slotNumber)
        {
            // 새 게임 초기 데이터 생성
            PlayerData newPlayerData = new PlayerData
            {
                level = 1,
                currentExp = 0,
                maxExp = 100,
                currentHP = 100,
                maxHP = 100,
                currentMP = 50,
                maxMP = 50,
                diamonds = 0,
                reviveStones = 0,
                currentChapter = 1,
                chapterName = ChapterProgressSystem.GetChapterName(1),
                currentWave = 1
            };

            // SaveSystem에 저장
            SaveSystem.SaveGame(slotNumber, newPlayerData);

            // 챕터 1 해금
            SaveSystem.SaveChapterProgress(slotNumber, 1, true, false, 0, 10);

            // SlotInfoComponent 동기화
            if (slotInfoComponent != null)
            {
                slotInfoComponent.LoadSlotInfo();
            }

            // UI 새로고침
            LoadSaveData();
            RenderAllSlots();
        }

        private void OnDeleteSlot(int slotNumber)
        {
            // TODO: 삭제 확인 다이얼로그 표시

            // 슬롯 찾기
            int index = slotDataList.FindIndex(s => s.slotNumber == slotNumber);
            if (index >= 0 && slotDataList[index].isUsed)
            {
                // SaveSystem에서 슬롯 삭제
                SaveSystem.DeleteSlot(slotNumber);

                slotDataList.RemoveAt(index);

                // 빈 슬롯이 마지막에 있는지 확인
                EnsureEmptySlotAtEnd();

                // SlotInfoComponent 동기화
                if (slotInfoComponent != null)
                {
                    slotInfoComponent.LoadSlotInfo();
                }

                // UI 다시 렌더링
                RenderAllSlots();
            }
        }

        private void OnContinueGame(int slotNumber)
        {
            // 현재 슬롯 설정
            GameStateManager.CurrentSlot = slotNumber;
            SaveSystem.CurrentSlot = slotNumber;

            // SlotInfoComponent 동기화
            if (slotInfoComponent != null)
            {
                slotInfoComponent.selectedSlot = slotNumber;
                slotInfoComponent.LoadSlotInfo();
            }

            // 게임 씬으로 전환
            SceneManager.LoadScene("InGame");
        }


        #endregion
    }
}
