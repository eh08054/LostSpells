using UnityEngine;
using System.Collections.Generic;
using LostSpells.UI;

namespace LostSpells.Data.Save
{
    /// <summary>
    /// Endless Mode 랭킹 정보를 Inspector에서 확인하고 수정할 수 있는 컴포넌트
    /// </summary>
    public class EndlessModeRankingComponent : MonoBehaviour
    {
        [Header("Endless Mode Rankings (수정 가능)")]
        public List<RankingDisplayInfo> rankings = new List<RankingDisplayInfo>();

        private void Start()
        {
            // Play Mode에서만 PlayerPrefs에서 로드
            if (Application.isPlaying)
            {
                LoadRankings();
            }
        }

        private void OnValidate()
        {
            // Play Mode 중에만 UI 즉시 업데이트
            if (Application.isPlaying)
            {
                RefreshUI();
            }
        }

        #region Public Methods for Editor Buttons

        /// <summary>
        /// 샘플 데이터 생성 (Editor 버튼용)
        /// </summary>
        public void GenerateSample()
        {
            GenerateSampleRankings();
            LoadRankings();
            RefreshUI();
        }

        /// <summary>
        /// 모든 랭킹 삭제 (Editor 버튼용)
        /// </summary>
        public void ClearAll()
        {
            SaveSystem.ClearEndlessModeRankings();
            rankings.Clear();
            RefreshUI();
        }

        /// <summary>
        /// 정렬 및 저장 (Editor 버튼용)
        /// </summary>
        public void SortAndSave()
        {
            SortAndSaveRankings();
            RefreshUI();
        }

        #endregion

        /// <summary>
        /// UI 새로고침 (Play Mode 중에만 동작)
        /// </summary>
        private void RefreshUI()
        {
            if (!Application.isPlaying) return;

            var endlessModeUI = FindFirstObjectByType<EndlessModeUI>();
            if (endlessModeUI != null && endlessModeUI.IsUIInitialized)
            {
                // Inspector 데이터를 EndlessModeRecord로 변환
                var records = new List<EndlessModeUI.EndlessModeRecord>();
                foreach (var ranking in rankings)
                {
                    if (ranking.wave > 0) // 유효한 데이터만
                    {
                        records.Add(new EndlessModeUI.EndlessModeRecord(ranking.wave, ranking.level, ranking.date));
                    }
                }

                endlessModeUI.RefreshRankingsUIWithData(records);
            }
        }

        /// <summary>
        /// PlayerPrefs에서 랭킹 데이터 불러오기
        /// </summary>
        public void LoadRankings()
        {
            rankings.Clear();
            var savedRankings = SaveSystem.GetEndlessModeRankings();

            foreach (var record in savedRankings)
            {
                rankings.Add(new RankingDisplayInfo
                {
                    wave = record.wave,
                    level = record.level,
                    date = record.date
                });
            }

            // 빈 슬롯 추가 (5개까지)
            while (rankings.Count < 5)
            {
                rankings.Add(new RankingDisplayInfo
                {
                    wave = 0,
                    level = 0,
                    date = "Empty"
                });
            }
        }

        /// <summary>
        /// 웨이브 기준으로 정렬 후 PlayerPrefs에 저장
        /// </summary>
        private void SortAndSaveRankings()
        {
            // 빈 슬롯 제거
            rankings.RemoveAll(r => r.wave <= 0);

            // 웨이브 기준 내림차순 정렬 (같으면 레벨 기준)
            rankings.Sort((a, b) =>
            {
                if (a.wave != b.wave)
                    return b.wave.CompareTo(a.wave); // 웨이브 내림차순
                return b.level.CompareTo(a.level); // 레벨 내림차순
            });

            // 상위 5개만 유지
            if (rankings.Count > 5)
            {
                rankings.RemoveRange(5, rankings.Count - 5);
            }

            // PlayerPrefs에 저장
            SaveSystem.ClearEndlessModeRankings();
            for (int i = 0; i < rankings.Count; i++)
            {
                string prefix = $"EndlessMode_Rank{i + 1}_";
                PlayerPrefs.SetInt(prefix + "Wave", rankings[i].wave);
                PlayerPrefs.SetInt(prefix + "Level", rankings[i].level);
                PlayerPrefs.SetString(prefix + "Date", rankings[i].date);
            }

            PlayerPrefs.SetInt("EndlessMode_RankingCount", rankings.Count);
            PlayerPrefs.Save();

            // 빈 슬롯 추가 (5개까지)
            while (rankings.Count < 5)
            {
                rankings.Add(new RankingDisplayInfo
                {
                    wave = 0,
                    level = 0,
                    date = "Empty"
                });
            }
        }

        /// <summary>
        /// 샘플 랭킹 데이터 생성 (테스트용)
        /// </summary>
        private void GenerateSampleRankings()
        {
            // 기존 데이터 삭제
            SaveSystem.ClearEndlessModeRankings();

            // 샘플 데이터 생성 (1등부터 5등까지)
            string currentDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");

            // 1등: Wave 50, Level 25
            SaveSystem.SaveEndlessModeRanking(50, 25, currentDate);

            // 2등: Wave 45, Level 22
            SaveSystem.SaveEndlessModeRanking(45, 22, System.DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd HH:mm"));

            // 3등: Wave 40, Level 20
            SaveSystem.SaveEndlessModeRanking(40, 20, System.DateTime.Now.AddDays(-2).ToString("yyyy-MM-dd HH:mm"));

            // 4등: Wave 35, Level 18
            SaveSystem.SaveEndlessModeRanking(35, 18, System.DateTime.Now.AddDays(-3).ToString("yyyy-MM-dd HH:mm"));

            // 5등: Wave 30, Level 15
            SaveSystem.SaveEndlessModeRanking(30, 15, System.DateTime.Now.AddDays(-4).ToString("yyyy-MM-dd HH:mm"));
        }
    }

    /// <summary>
    /// Inspector 표시용 랭킹 정보
    /// </summary>
    [System.Serializable]
    public class RankingDisplayInfo
    {
        [Tooltip("웨이브 수")]
        public int wave;

        [Tooltip("레벨")]
        public int level;

        [Tooltip("플레이 날짜")]
        public string date;
    }
}
