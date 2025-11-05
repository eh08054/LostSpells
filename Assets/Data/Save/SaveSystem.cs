using UnityEngine;
using System.Collections.Generic;
using LostSpells.Systems;

namespace LostSpells.Data.Save
{
    /// <summary>
    /// 정적 저장 시스템 - PlayerPrefs를 사용한 슬롯별 저장/불러오기
    /// GameManager 없이 어디서든 접근 가능
    /// </summary>
    public static class SaveSystem
    {
        private const string CURRENT_SLOT_KEY = "CurrentSlot";
        private const string USED_SLOTS_KEY = "UsedSlots";

        /// <summary>
        /// 현재 선택된 슬롯
        /// </summary>
        public static int CurrentSlot
        {
            get => PlayerPrefs.GetInt(CURRENT_SLOT_KEY, 1);
            set
            {
                PlayerPrefs.SetInt(CURRENT_SLOT_KEY, value);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// 사용 중인 슬롯 번호 목록 가져오기
        /// </summary>
        private static List<int> GetUsedSlotNumbers()
        {
            string usedSlotsStr = PlayerPrefs.GetString(USED_SLOTS_KEY, "");
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

        /// <summary>
        /// 사용 중인 슬롯 번호 목록 저장
        /// </summary>
        private static void SaveUsedSlotNumbers(List<int> usedSlots)
        {
            usedSlots.Sort();
            string usedSlotsStr = string.Join(",", usedSlots);
            PlayerPrefs.SetString(USED_SLOTS_KEY, usedSlotsStr);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 슬롯을 사용 중인 목록에 추가
        /// </summary>
        private static void AddUsedSlot(int slotNumber)
        {
            List<int> usedSlots = GetUsedSlotNumbers();
            if (!usedSlots.Contains(slotNumber))
            {
                usedSlots.Add(slotNumber);
                SaveUsedSlotNumbers(usedSlots);
            }
        }

        /// <summary>
        /// 슬롯을 사용 중인 목록에서 제거
        /// </summary>
        private static void RemoveUsedSlot(int slotNumber)
        {
            List<int> usedSlots = GetUsedSlotNumbers();
            if (usedSlots.Contains(slotNumber))
            {
                usedSlots.Remove(slotNumber);
                SaveUsedSlotNumbers(usedSlots);
            }
        }

        /// <summary>
        /// 특정 슬롯에 게임 데이터 저장 (InGame에서만 사용)
        /// </summary>
        public static void SaveGame(int slotNumber, PlayerData playerData)
        {
            string prefix = $"Slot{slotNumber}_";

            PlayerPrefs.SetInt(prefix + "PlayerLevel", playerData.level);
            PlayerPrefs.SetInt(prefix + "PlayerCurrentExp", playerData.currentExp);
            PlayerPrefs.SetInt(prefix + "PlayerMaxExp", playerData.maxExp);
            PlayerPrefs.SetInt(prefix + "PlayerCurrentHP", playerData.currentHP);
            PlayerPrefs.SetInt(prefix + "PlayerMaxHP", playerData.maxHP);
            PlayerPrefs.SetInt(prefix + "PlayerCurrentMP", playerData.currentMP);
            PlayerPrefs.SetInt(prefix + "PlayerMaxMP", playerData.maxMP);
            PlayerPrefs.SetInt(prefix + "PlayerDiamonds", playerData.diamonds);
            PlayerPrefs.SetInt(prefix + "PlayerReviveStones", playerData.reviveStones);
            PlayerPrefs.SetInt(prefix + "PlayerChapter", playerData.currentChapter);
            PlayerPrefs.SetInt(prefix + "PlayerWave", playerData.currentWave);
            PlayerPrefs.SetString(prefix + "LastPlayed", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

            // 슬롯 이름이 없으면 빈 문자열로 설정
            if (!PlayerPrefs.HasKey(prefix + "SlotName"))
            {
                PlayerPrefs.SetString(prefix + "SlotName", "");
            }

            // 슬롯이 사용중임을 표시
            PlayerPrefs.SetInt(prefix + "IsUsed", 1);

            // 사용 중인 슬롯 목록에 추가
            AddUsedSlot(slotNumber);

            PlayerPrefs.Save();
        }

        /// <summary>
        /// 슬롯 이름 저장
        /// </summary>
        public static void SaveSlotName(int slotNumber, string slotName)
        {
            string prefix = $"Slot{slotNumber}_";
            PlayerPrefs.SetString(prefix + "SlotName", slotName);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 슬롯 이름 가져오기
        /// </summary>
        public static string GetSlotName(int slotNumber)
        {
            string prefix = $"Slot{slotNumber}_";
            return PlayerPrefs.GetString(prefix + "SlotName", "");
        }

        /// <summary>
        /// 특정 슬롯에서 게임 데이터 불러오기 (InGame에서만 사용)
        /// </summary>
        public static bool LoadGame(int slotNumber, ref PlayerData playerData)
        {
            string prefix = $"Slot{slotNumber}_";

            if (PlayerPrefs.GetInt(prefix + "IsUsed", 0) == 1)
            {
                playerData.level = PlayerPrefs.GetInt(prefix + "PlayerLevel", 1);
                playerData.currentExp = PlayerPrefs.GetInt(prefix + "PlayerCurrentExp", 0);
                playerData.maxExp = PlayerPrefs.GetInt(prefix + "PlayerMaxExp", 100);
                playerData.currentHP = PlayerPrefs.GetInt(prefix + "PlayerCurrentHP", 100);
                playerData.maxHP = PlayerPrefs.GetInt(prefix + "PlayerMaxHP", 100);
                playerData.currentMP = PlayerPrefs.GetInt(prefix + "PlayerCurrentMP", 50);
                playerData.maxMP = PlayerPrefs.GetInt(prefix + "PlayerMaxMP", 50);
                playerData.diamonds = PlayerPrefs.GetInt(prefix + "PlayerDiamonds", 0);
                playerData.reviveStones = PlayerPrefs.GetInt(prefix + "PlayerReviveStones", 0);
                playerData.currentChapter = PlayerPrefs.GetInt(prefix + "PlayerChapter", 1);
                playerData.currentWave = PlayerPrefs.GetInt(prefix + "PlayerWave", 1);

                // 챕터 번호에 맞는 챕터명 자동 설정
                playerData.chapterName = ChapterProgressSystem.GetChapterName(playerData.currentChapter);

                return true;
            }

            return false;
        }

        /// <summary>
        /// 특정 슬롯 삭제
        /// </summary>
        public static void DeleteSlot(int slotNumber)
        {
            string prefix = $"Slot{slotNumber}_";

            // 플레이어 데이터 삭제
            PlayerPrefs.DeleteKey(prefix + "PlayerLevel");
            PlayerPrefs.DeleteKey(prefix + "PlayerCurrentExp");
            PlayerPrefs.DeleteKey(prefix + "PlayerMaxExp");
            PlayerPrefs.DeleteKey(prefix + "PlayerCurrentHP");
            PlayerPrefs.DeleteKey(prefix + "PlayerMaxHP");
            PlayerPrefs.DeleteKey(prefix + "PlayerCurrentMP");
            PlayerPrefs.DeleteKey(prefix + "PlayerMaxMP");
            PlayerPrefs.DeleteKey(prefix + "PlayerDiamonds");
            PlayerPrefs.DeleteKey(prefix + "PlayerReviveStones");
            PlayerPrefs.DeleteKey(prefix + "PlayerChapter");
            PlayerPrefs.DeleteKey(prefix + "PlayerWave");
            PlayerPrefs.DeleteKey(prefix + "LastPlayed");
            PlayerPrefs.DeleteKey(prefix + "IsUsed");

            // 모든 챕터 진행 상황 삭제 (1~7)
            for (int i = 1; i <= 7; i++)
            {
                string chapterPrefix = $"Slot{slotNumber}_Chapter{i}_";
                PlayerPrefs.DeleteKey(chapterPrefix + "IsUnlocked");
                PlayerPrefs.DeleteKey(chapterPrefix + "IsCompleted");
                PlayerPrefs.DeleteKey(chapterPrefix + "CurrentStage");
                PlayerPrefs.DeleteKey(chapterPrefix + "TotalStages");
            }

            // 사용 중인 슬롯 목록에서 제거
            RemoveUsedSlot(slotNumber);

            PlayerPrefs.Save();
        }

        /// <summary>
        /// 특정 슬롯이 사용중인지 확인
        /// </summary>
        public static bool IsSlotUsed(int slotNumber)
        {
            string prefix = $"Slot{slotNumber}_";
            return PlayerPrefs.GetInt(prefix + "IsUsed", 0) == 1;
        }

        /// <summary>
        /// 슬롯 정보 가져오기 (UI 표시용)
        /// </summary>
        public static SaveSlotInfo GetSlotInfo(int slotNumber)
        {
            string prefix = $"Slot{slotNumber}_";

            if (PlayerPrefs.GetInt(prefix + "IsUsed", 0) == 1)
            {
                int chapterNum = PlayerPrefs.GetInt(prefix + "PlayerChapter", 1);
                return new SaveSlotInfo
                {
                    slotNumber = slotNumber,
                    isUsed = true,
                    slotName = PlayerPrefs.GetString(prefix + "SlotName", ""),
                    level = PlayerPrefs.GetInt(prefix + "PlayerLevel", 1),
                    chapterNumber = chapterNum,
                    chapterName = ChapterProgressSystem.GetChapterName(chapterNum),
                    currentWave = PlayerPrefs.GetInt(prefix + "PlayerWave", 1),
                    lastPlayed = PlayerPrefs.GetString(prefix + "LastPlayed", "")
                };
            }

            return new SaveSlotInfo
            {
                slotNumber = slotNumber,
                isUsed = false
            };
        }

        /// <summary>
        /// 모든 슬롯 정보 가져오기 (사용 중인 슬롯만)
        /// </summary>
        public static List<SaveSlotInfo> GetAllSlots()
        {
            List<SaveSlotInfo> slots = new List<SaveSlotInfo>();
            List<int> usedSlotNumbers = GetUsedSlotNumbers();

            foreach (int slotNumber in usedSlotNumbers)
            {
                SaveSlotInfo slotInfo = GetSlotInfo(slotNumber);
                if (slotInfo.isUsed)
                {
                    slots.Add(slotInfo);
                }
            }

            return slots;
        }

        /// <summary>
        /// 챕터 진행 상황 저장 (슬롯별)
        /// </summary>
        public static void SaveChapterProgress(int slotNumber, int chapterNumber, bool isUnlocked, bool isCompleted, int currentStage, int totalStages)
        {
            string prefix = $"Slot{slotNumber}_Chapter{chapterNumber}_";

            PlayerPrefs.SetInt(prefix + "IsUnlocked", isUnlocked ? 1 : 0);
            PlayerPrefs.SetInt(prefix + "IsCompleted", isCompleted ? 1 : 0);
            PlayerPrefs.SetInt(prefix + "CurrentStage", currentStage);
            PlayerPrefs.SetInt(prefix + "TotalStages", totalStages);

            PlayerPrefs.Save();
        }

        /// <summary>
        /// 챕터 진행 상황 불러오기 (슬롯별)
        /// </summary>
        public static ChapterProgressInfo GetChapterProgress(int slotNumber, int chapterNumber)
        {
            string prefix = $"Slot{slotNumber}_Chapter{chapterNumber}_";

            // 챕터 1은 기본으로 해금
            bool defaultUnlocked = (chapterNumber == 1);

            return new ChapterProgressInfo
            {
                chapterNumber = chapterNumber,
                isUnlocked = PlayerPrefs.GetInt(prefix + "IsUnlocked", defaultUnlocked ? 1 : 0) == 1,
                isCompleted = PlayerPrefs.GetInt(prefix + "IsCompleted", 0) == 1,
                currentStage = PlayerPrefs.GetInt(prefix + "CurrentStage", 0),
                totalStages = PlayerPrefs.GetInt(prefix + "TotalStages", 10)
            };
        }

        /// <summary>
        /// 모든 챕터 진행 상황 가져오기 (슬롯별, 7개 챕터)
        /// </summary>
        public static List<ChapterProgressInfo> GetAllChapterProgress(int slotNumber, int totalChapters = 7)
        {
            List<ChapterProgressInfo> chapters = new List<ChapterProgressInfo>();

            for (int i = 1; i <= totalChapters; i++)
            {
                chapters.Add(GetChapterProgress(slotNumber, i));
            }

            return chapters;
        }

        #region Endless Mode Rankings

        private const string ENDLESS_RANKING_COUNT = "EndlessMode_RankingCount";
        private const int MAX_RANKINGS = 5;

        /// <summary>
        /// 무한 모드 순위 저장 (웨이브 기준으로 자동 정렬)
        /// </summary>
        public static void SaveEndlessModeRanking(int wave, int level, string date)
        {
            // 기존 순위 불러오기
            List<LostSpells.UI.EndlessModeUI.EndlessModeRecord> rankings = GetEndlessModeRankings();

            // 새로운 기록 추가
            var newRecord = new LostSpells.UI.EndlessModeUI.EndlessModeRecord(wave, level, date);
            rankings.Add(newRecord);

            // 웨이브 기준 내림차순 정렬 (같으면 레벨 기준)
            rankings.Sort((a, b) =>
            {
                if (a.wave != b.wave)
                    return b.wave.CompareTo(a.wave); // 웨이브 내림차순
                return b.level.CompareTo(a.level); // 레벨 내림차순
            });

            // 상위 5개만 유지
            if (rankings.Count > MAX_RANKINGS)
            {
                rankings.RemoveRange(MAX_RANKINGS, rankings.Count - MAX_RANKINGS);
            }

            // PlayerPrefs에 저장
            for (int i = 0; i < rankings.Count; i++)
            {
                string prefix = $"EndlessMode_Rank{i + 1}_";
                PlayerPrefs.SetInt(prefix + "Wave", rankings[i].wave);
                PlayerPrefs.SetInt(prefix + "Level", rankings[i].level);
                PlayerPrefs.SetString(prefix + "Date", rankings[i].date);
            }

            // 순위 개수 저장
            PlayerPrefs.SetInt(ENDLESS_RANKING_COUNT, rankings.Count);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 무한 모드 순위 불러오기 (1-10등)
        /// </summary>
        public static List<LostSpells.UI.EndlessModeUI.EndlessModeRecord> GetEndlessModeRankings()
        {
            List<LostSpells.UI.EndlessModeUI.EndlessModeRecord> rankings = new List<LostSpells.UI.EndlessModeUI.EndlessModeRecord>();

            int count = PlayerPrefs.GetInt(ENDLESS_RANKING_COUNT, 0);

            for (int i = 1; i <= count && i <= MAX_RANKINGS; i++)
            {
                string prefix = $"EndlessMode_Rank{i}_";
                int wave = PlayerPrefs.GetInt(prefix + "Wave", 0);
                int level = PlayerPrefs.GetInt(prefix + "Level", 0);
                string date = PlayerPrefs.GetString(prefix + "Date", "");

                // 유효한 기록만 추가 (웨이브가 0보다 큰 경우)
                if (wave > 0)
                {
                    rankings.Add(new LostSpells.UI.EndlessModeUI.EndlessModeRecord(wave, level, date));
                }
            }

            return rankings;
        }

        /// <summary>
        /// 무한 모드 순위 초기화
        /// </summary>
        public static void ClearEndlessModeRankings()
        {
            int count = PlayerPrefs.GetInt(ENDLESS_RANKING_COUNT, 0);

            for (int i = 1; i <= count; i++)
            {
                string prefix = $"EndlessMode_Rank{i}_";
                PlayerPrefs.DeleteKey(prefix + "Wave");
                PlayerPrefs.DeleteKey(prefix + "Level");
                PlayerPrefs.DeleteKey(prefix + "Date");
            }

            PlayerPrefs.DeleteKey(ENDLESS_RANKING_COUNT);
            PlayerPrefs.Save();
        }

        #endregion
    }

    /// <summary>
    /// 저장 슬롯 정보 (UI 표시용)
    /// </summary>
    [System.Serializable]
    public class SaveSlotInfo
    {
        public int slotNumber;
        public bool isUsed;
        public string slotName;
        public int level;
        public int chapterNumber;
        public string chapterName;
        public int currentWave;
        public string lastPlayed;
    }

    /// <summary>
    /// 챕터 진행 상황 정보
    /// </summary>
    [System.Serializable]
    public class ChapterProgressInfo
    {
        public int chapterNumber;
        public bool isUnlocked;
        public bool isCompleted;
        public int currentStage;
        public int totalStages;
    }
}
