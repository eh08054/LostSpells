using UnityEngine;
using LostSpells.Data;
using LostSpells.Data.Save;
using System.Collections.Generic;

namespace LostSpells.Systems
{
    /// <summary>
    /// 게임 상태 관리 (Static 클래스)
    /// - 현재 슬롯, 게임 모드, 선택된 챕터/난이도 관리
    /// - 슬롯 데이터 캐싱
    /// </summary>
    public static class GameStateManager
    {
        // 현재 게임 상태
        private static int currentSlot = 1;
        private static GameMode currentGameMode = GameMode.StoryMode;
        private static int selectedChapter = 1;
        private static DifficultyLevel selectedDifficulty = DifficultyLevel.Normal;

        // 슬롯 데이터 캐시
        private static Dictionary<int, PlayerData> slotDataCache = new Dictionary<int, PlayerData>();

        /// <summary>
        /// 현재 선택된 슬롯 번호 (1, 2, 3)
        /// </summary>
        public static int CurrentSlot
        {
            get => currentSlot;
            set
            {
                if (value < 1 || value > 3)
                {
                    Debug.LogWarning($"[GameStateManager] 잘못된 슬롯 번호: {value}. 1-3 사이여야 합니다.");
                    return;
                }
                currentSlot = value;
                SaveSystem.CurrentSlot = value;
                Debug.Log($"[GameStateManager] 현재 슬롯 변경: {value}");
            }
        }

        /// <summary>
        /// 현재 게임 모드
        /// </summary>
        public static GameMode CurrentGameMode
        {
            get => currentGameMode;
            set
            {
                currentGameMode = value;
                Debug.Log($"[GameStateManager] 게임 모드 변경: {value}");
            }
        }

        /// <summary>
        /// 선택된 챕터 번호 (1-7)
        /// </summary>
        public static int SelectedChapter
        {
            get => selectedChapter;
            set
            {
                selectedChapter = Mathf.Clamp(value, 1, 7);
                Debug.Log($"[GameStateManager] 선택된 챕터: {selectedChapter}");
            }
        }

        /// <summary>
        /// 선택된 난이도 (EndlessMode용)
        /// </summary>
        public static DifficultyLevel SelectedDifficulty
        {
            get => selectedDifficulty;
            set
            {
                selectedDifficulty = value;
                Debug.Log($"[GameStateManager] 선택된 난이도: {value}");
            }
        }

        /// <summary>
        /// 특정 슬롯의 데이터 가져오기 (캐싱)
        /// </summary>
        public static PlayerData GetSlotData(int slot)
        {
            if (slot < 1 || slot > 3)
            {
                Debug.LogError($"[GameStateManager] 잘못된 슬롯 번호: {slot}");
                return null;
            }

            // 캐시에 있으면 반환
            if (slotDataCache.ContainsKey(slot))
            {
                return slotDataCache[slot];
            }

            // 없으면 로드
            PlayerData data = new PlayerData();
            if (SaveSystem.LoadGame(slot, ref data))
            {
                slotDataCache[slot] = data;
                return data;
            }

            // 로드 실패시 기본값 반환
            return data;
        }

        /// <summary>
        /// 현재 슬롯의 데이터 가져오기
        /// </summary>
        public static PlayerData GetCurrentSlotData()
        {
            return GetSlotData(currentSlot);
        }

        /// <summary>
        /// 슬롯 데이터 저장
        /// </summary>
        public static void SaveSlotData(int slot, PlayerData data)
        {
            if (slot < 1 || slot > 3)
            {
                Debug.LogError($"[GameStateManager] 잘못된 슬롯 번호: {slot}");
                return;
            }

            SaveSystem.SaveGame(slot, data);
            slotDataCache[slot] = data;
            Debug.Log($"[GameStateManager] 슬롯 {slot} 저장 완료");
        }

        /// <summary>
        /// 현재 슬롯 데이터 저장
        /// </summary>
        public static void SaveCurrentSlotData(PlayerData data)
        {
            SaveSlotData(currentSlot, data);
        }

        /// <summary>
        /// 슬롯 삭제
        /// </summary>
        public static void DeleteSlot(int slot)
        {
            if (slot < 1 || slot > 3)
            {
                Debug.LogError($"[GameStateManager] 잘못된 슬롯 번호: {slot}");
                return;
            }

            SaveSystem.DeleteSlot(slot);
            if (slotDataCache.ContainsKey(slot))
            {
                slotDataCache.Remove(slot);
            }
            Debug.Log($"[GameStateManager] 슬롯 {slot} 삭제 완료");
        }

        /// <summary>
        /// 슬롯 캐시 새로고침
        /// </summary>
        public static void RefreshSlotCache(int slot)
        {
            if (slot < 1 || slot > 3)
            {
                Debug.LogError($"[GameStateManager] 잘못된 슬롯 번호: {slot}");
                return;
            }

            if (slotDataCache.ContainsKey(slot))
            {
                slotDataCache.Remove(slot);
            }

            GetSlotData(slot); // 다시 로드하여 캐싱
        }

        /// <summary>
        /// 모든 슬롯 캐시 초기화
        /// </summary>
        public static void ClearAllCache()
        {
            slotDataCache.Clear();
            Debug.Log("[GameStateManager] 모든 슬롯 캐시 초기화");
        }

        /// <summary>
        /// 슬롯 사용 여부 확인
        /// </summary>
        public static bool IsSlotUsed(int slot)
        {
            if (slot < 1 || slot > 3)
            {
                Debug.LogError($"[GameStateManager] 잘못된 슬롯 번호: {slot}");
                return false;
            }

            return SaveSystem.IsSlotUsed(slot);
        }
    }

    /// <summary>
    /// 게임 모드 열거형
    /// </summary>
    public enum GameMode
    {
        StoryMode,
        ChapterSelect,
        EndlessMode
    }

    /// <summary>
    /// 난이도 열거형
    /// </summary>
    public enum DifficultyLevel
    {
        Easy,
        Normal,
        Hard
    }
}
