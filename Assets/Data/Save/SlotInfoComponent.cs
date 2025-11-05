using UnityEngine;
using System.Collections.Generic;
using LostSpells.Systems;

namespace LostSpells.Data.Save
{
    /// <summary>
    /// 슬롯 정보 컴포넌트 - 인스펙터에서 수정 가능
    /// 변경사항은 자동으로 PlayerPrefs에 저장되며 게임 종료 후에도 유지됨
    /// </summary>
    public class SlotInfoComponent : MonoBehaviour
    {
        [HideInInspector]
        public int selectedSlot = 1;

        [Header("모든 슬롯 정보 (수정 가능 - 변경 후 자동 저장)")]
        [SerializeField] private List<SlotDisplayInfo> allSlots = new List<SlotDisplayInfo>();

        [Header("현재 선택된 슬롯 정보")]
        [SerializeField] private SlotDisplayInfo currentSlotInfo;

        private List<SlotDisplayInfo> previousSlots = new List<SlotDisplayInfo>();
        private bool isUpdating = false;

        private void Start()
        {
            LoadSlotInfo();
        }

        private void OnValidate()
        {
            if (isUpdating) return;

            // 에디터와 플레이 모드 모두에서 작동
            if (Application.isPlaying)
            {
                // 변경 사항 감지 및 저장
                DetectAndSaveChanges();
            }
        }

        public void LoadSlotInfo()
        {
            isUpdating = true;

            // 사용 중인 슬롯만 불러오기
            allSlots.Clear();
            previousSlots.Clear();
            List<SaveSlotInfo> savedSlots = SaveSystem.GetAllSlots();

            // 사용 중인 슬롯만 추가
            foreach (var slotInfo in savedSlots)
            {
                if (slotInfo.isUsed)
                {
                    var displayInfo = new SlotDisplayInfo
                    {
                        slotNumber = slotInfo.slotNumber,
                        isUsed = true,
                        slotName = slotInfo.slotName,
                        level = slotInfo.level,
                        currentChapter = slotInfo.chapterNumber,
                        chapterName = slotInfo.chapterName,
                        currentWave = slotInfo.currentWave,
                        lastPlayed = slotInfo.lastPlayed
                    };
                    allSlots.Add(displayInfo);
                    previousSlots.Add(displayInfo.Clone());
                }
            }

            // 현재 선택된 슬롯 정보 업데이트
            var currentSlot = SaveSystem.GetSlotInfo(selectedSlot);
            if (currentSlot.isUsed)
            {
                currentSlotInfo = new SlotDisplayInfo
                {
                    slotNumber = currentSlot.slotNumber,
                    isUsed = true,
                    slotName = currentSlot.slotName,
                    level = currentSlot.level,
                    currentChapter = currentSlot.chapterNumber,
                    chapterName = currentSlot.chapterName,
                    currentWave = currentSlot.currentWave,
                    lastPlayed = currentSlot.lastPlayed
                };
            }
            else
            {
                currentSlotInfo = new SlotDisplayInfo
                {
                    slotNumber = selectedSlot,
                    isUsed = false,
                    slotName = "",
                    level = 0,
                    currentChapter = 0,
                    chapterName = "Empty Slot",
                    currentWave = 0,
                    lastPlayed = ""
                };
            }

            isUpdating = false;
        }

        /// <summary>
        /// 변경 사항 감지 및 저장
        /// </summary>
        private void DetectAndSaveChanges()
        {
            // 슬롯이 추가되었는지 확인
            if (allSlots.Count > previousSlots.Count)
            {
                // 새로 추가된 슬롯 처리
                for (int i = previousSlots.Count; i < allSlots.Count; i++)
                {
                    var newSlot = allSlots[i];

                    // 슬롯 번호 자동 할당 (빈 번호 재사용)
                    List<int> usedSlotNumbers = GetUsedSlotNumbersFromAllSlots();
                    int newSlotNumber = 1;

                    // 1부터 시작해서 사용되지 않은 첫 번째 번호 찾기
                    while (usedSlotNumbers.Contains(newSlotNumber))
                    {
                        newSlotNumber++;
                    }

                    // 새로운 슬롯으로 완전히 초기화 (복사된 데이터 무시)
                    newSlot.slotNumber = newSlotNumber;
                    newSlot.isUsed = true;
                    newSlot.slotName = "";  // 항상 빈 문자열로 시작
                    newSlot.level = 1;
                    newSlot.currentChapter = 1;
                    newSlot.currentWave = 1;
                    newSlot.chapterName = ChapterProgressSystem.GetChapterName(1);
                    newSlot.lastPlayed = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");

                    // PlayerPrefs에 저장
                    SaveSlotToPlayerPrefs(newSlot);
                    previousSlots.Add(newSlot.Clone());
                }
                return;
            }

            // 슬롯이 삭제되었는지 확인
            if (allSlots.Count < previousSlots.Count)
            {
                // 삭제된 슬롯 찾기
                for (int i = 0; i < previousSlots.Count; i++)
                {
                    var prevSlot = previousSlots[i];
                    bool found = false;

                    foreach (var currentSlot in allSlots)
                    {
                        if (currentSlot.slotNumber == prevSlot.slotNumber)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        // 슬롯 삭제
                        SaveSystem.DeleteSlot(prevSlot.slotNumber);
                    }
                }

                // 전체 리로드
                LoadSlotInfo();
                return;
            }

            // 기존 슬롯의 변경사항 확인
            for (int i = 0; i < allSlots.Count; i++)
            {
                if (i >= previousSlots.Count) break;

                var current = allSlots[i];
                var previous = previousSlots[i];

                if (HasSlotChanged(current, previous))
                {
                    SaveSlotToPlayerPrefs(current);
                    previousSlots[i] = current.Clone();
                }
            }
        }

        /// <summary>
        /// 현재 allSlots에서 사용 중인 슬롯 번호 목록 가져오기
        /// </summary>
        private List<int> GetUsedSlotNumbersFromAllSlots()
        {
            List<int> slotNumbers = new List<int>();
            foreach (var slot in allSlots)
            {
                if (slot.isUsed && slot.slotNumber > 0)
                {
                    slotNumbers.Add(slot.slotNumber);
                }
            }
            slotNumbers.Sort();
            return slotNumbers;
        }

        /// <summary>
        /// 슬롯이 변경되었는지 확인
        /// </summary>
        private bool HasSlotChanged(SlotDisplayInfo current, SlotDisplayInfo previous)
        {
            return current.slotName != previous.slotName ||
                   current.level != previous.level ||
                   current.currentChapter != previous.currentChapter ||
                   current.currentWave != previous.currentWave ||
                   current.chapterName != previous.chapterName ||
                   current.lastPlayed != previous.lastPlayed;
        }

        /// <summary>
        /// 슬롯 정보를 PlayerPrefs에 저장
        /// </summary>
        private void SaveSlotToPlayerPrefs(SlotDisplayInfo slotInfo)
        {
            if (!slotInfo.isUsed) return;

            string prefix = $"Slot{slotInfo.slotNumber}_";

            PlayerPrefs.SetString(prefix + "SlotName", slotInfo.slotName);
            PlayerPrefs.SetInt(prefix + "PlayerLevel", slotInfo.level);
            PlayerPrefs.SetInt(prefix + "PlayerChapter", slotInfo.currentChapter);
            PlayerPrefs.SetInt(prefix + "PlayerWave", slotInfo.currentWave);
            PlayerPrefs.SetString(prefix + "LastPlayed", slotInfo.lastPlayed);
            PlayerPrefs.SetInt(prefix + "IsUsed", 1);

            // 기본 데이터도 저장 (SaveSystem과 호환성 유지)
            PlayerPrefs.SetInt(prefix + "PlayerCurrentExp", 0);
            PlayerPrefs.SetInt(prefix + "PlayerMaxExp", 100);
            PlayerPrefs.SetInt(prefix + "PlayerCurrentHP", 100);
            PlayerPrefs.SetInt(prefix + "PlayerMaxHP", 100);
            PlayerPrefs.SetInt(prefix + "PlayerCurrentMP", 50);
            PlayerPrefs.SetInt(prefix + "PlayerMaxMP", 50);
            PlayerPrefs.SetInt(prefix + "PlayerDiamonds", 0);
            PlayerPrefs.SetInt(prefix + "PlayerReviveStones", 0);

            // 사용 중인 슬롯 목록에 추가
            List<int> usedSlots = GetUsedSlotNumbersFromPlayerPrefs();
            if (!usedSlots.Contains(slotInfo.slotNumber))
            {
                usedSlots.Add(slotInfo.slotNumber);
                usedSlots.Sort();
                string usedSlotsStr = string.Join(",", usedSlots);
                PlayerPrefs.SetString("UsedSlots", usedSlotsStr);
            }

            PlayerPrefs.Save();
        }

        /// <summary>
        /// PlayerPrefs에서 사용 중인 슬롯 번호 목록 가져오기
        /// </summary>
        private List<int> GetUsedSlotNumbersFromPlayerPrefs()
        {
            string usedSlotsStr = PlayerPrefs.GetString("UsedSlots", "");
            List<int> usedSlots = new List<int>();

            if (!string.IsNullOrEmpty(usedSlotsStr))
            {
                string[] slotNumbers = usedSlotsStr.Split(',');
                foreach (string slotStr in slotNumbers)
                {
                    if (int.TryParse(slotStr, out int slotNum))
                    {
                        usedSlots.Add(slotNum);
                    }
                }
            }

            usedSlots.Sort();
            return usedSlots;
        }
    }

    /// <summary>
    /// 인스펙터 표시용 슬롯 정보 (수정 가능)
    /// </summary>
    [System.Serializable]
    public class SlotDisplayInfo
    {
        // 내부 데이터 (인스펙터에 숨김)
        [HideInInspector]
        public int slotNumber;

        [HideInInspector]
        public bool isUsed;

        [HideInInspector]
        public int level;

        [HideInInspector]
        public int currentWave;

        // UI에 표시되는 데이터 (인스펙터에 표시)
        [Header("UI 표시 정보")]

        [Tooltip("슬롯 이름 (빈 문자열이면 'Slot N' 표시)")]
        public string slotName;

        [Tooltip("현재 챕터 번호")]
        public int currentChapter;

        [Tooltip("챕터 이름")]
        public string chapterName;

        [Tooltip("마지막 플레이 시간 (yyyy-MM-dd HH:mm)")]
        public string lastPlayed;

        /// <summary>
        /// 슬롯 정보 복제
        /// </summary>
        public SlotDisplayInfo Clone()
        {
            return new SlotDisplayInfo
            {
                slotNumber = this.slotNumber,
                isUsed = this.isUsed,
                slotName = this.slotName,
                level = this.level,
                currentChapter = this.currentChapter,
                chapterName = this.chapterName,
                currentWave = this.currentWave,
                lastPlayed = this.lastPlayed
            };
        }
    }
}
