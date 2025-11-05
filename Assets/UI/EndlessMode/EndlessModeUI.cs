using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using LostSpells.Data;
using LostSpells.Systems;
using LostSpells.Data.Save;
using System.Collections.Generic;

namespace LostSpells.UI
{
    /// <summary>
    /// Endless Mode UI 컨트롤러 - 최고 기록 순위표 및 무한 모드 시작
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class EndlessModeUI : MonoBehaviour
    {
        [System.Serializable]
        public class EndlessModeRecord
        {
            public int wave;         // 웨이브 수
            public int level;        // 레벨
            public string date;      // 플레이 날짜

            public EndlessModeRecord()
            {
                wave = 0;
                level = 0;
                date = "";
            }

            public EndlessModeRecord(int wave, int level, string date)
            {
                this.wave = wave;
                this.level = level;
                this.date = date;
            }
        }

        private UIDocument uiDocument;
        private VisualElement root;
        private Button backButton;
        private Button playButton;
        private VisualElement rankingListContainer;

        // 순위 데이터 (1-10등)
        private List<EndlessModeRecord> rankings = new List<EndlessModeRecord>();

        // UI 초기화 완료 여부
        public bool IsUIInitialized => rankingListContainer != null;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            root = uiDocument.rootVisualElement;
            InitializeUI();

            LoadRankings();
            RenderRankings();
        }

        private void OnDisable()
        {
            // 이벤트 해제
            if (backButton != null)
                backButton.clicked -= OnBackButtonClicked;
            if (playButton != null)
                playButton.clicked -= OnPlayButtonClicked;
        }

        private void InitializeUI()
        {
            // 버튼 참조
            backButton = root.Q<Button>("BackButton");
            playButton = root.Q<Button>("PlayButton");

            // 순위 리스트 컨테이너
            rankingListContainer = root.Q<VisualElement>("RankingListContainer");

            // 이벤트 등록
            if (backButton != null)
                backButton.clicked += OnBackButtonClicked;
            if (playButton != null)
                playButton.clicked += OnPlayButtonClicked;
        }

        /// <summary>
        /// 저장된 순위 데이터 불러오기
        /// </summary>
        private void LoadRankings()
        {
            rankings.Clear();
            rankings = SaveSystem.GetEndlessModeRankings();

            // 순위가 5개 미만이면 빈 슬롯으로 채움
            while (rankings.Count < 5)
            {
                rankings.Add(new EndlessModeRecord(0, 0, ""));
            }
        }

        /// <summary>
        /// 순위표 렌더링
        /// </summary>
        private void RenderRankings()
        {
            if (rankingListContainer == null)
                return;

            // 기존 항목 제거
            rankingListContainer.Clear();

            // 1-5등 순위 항목 생성
            for (int i = 0; i < rankings.Count && i < 5; i++)
            {
                CreateRankingItem(rankings[i], i + 1);
            }
        }

        /// <summary>
        /// 개별 순위 항목 생성
        /// </summary>
        private void CreateRankingItem(EndlessModeRecord record, int displayRank)
        {
            // 항목 컨테이너
            var item = new VisualElement();
            item.AddToClassList("ranking-item");

            // 기록이 있는 경우
            if (record.wave > 0)
            {
                // 1-3등 특별 스타일
                if (displayRank == 1)
                    item.AddToClassList("ranking-item-gold");
                else if (displayRank == 2)
                    item.AddToClassList("ranking-item-silver");
                else if (displayRank == 3)
                    item.AddToClassList("ranking-item-bronze");

                // 순위 번호
                var rankLabel = new Label($"{displayRank}");
                rankLabel.AddToClassList("ranking-rank");
                item.Add(rankLabel);

                // 정보 컨테이너
                var infoContainer = new VisualElement();
                infoContainer.AddToClassList("ranking-info");

                // 웨이브 정보
                var waveLabel = new Label($"Wave {record.wave}");
                waveLabel.AddToClassList("ranking-wave");
                infoContainer.Add(waveLabel);

                item.Add(infoContainer);

                // 날짜
                var dateLabel = new Label(record.date);
                dateLabel.AddToClassList("ranking-date");
                item.Add(dateLabel);
            }
            else
            {
                // 기록이 없는 경우
                item.AddToClassList("ranking-item-empty");

                // 순위 번호
                var rankLabel = new Label($"{displayRank}");
                rankLabel.AddToClassList("ranking-rank");
                item.Add(rankLabel);

                // 빈 텍스트
                var emptyLabel = new Label("기록 없음");
                emptyLabel.AddToClassList("ranking-empty-text");
                item.Add(emptyLabel);
            }

            rankingListContainer.Add(item);
        }

        #region Event Handlers

        private void OnBackButtonClicked()
        {
            SceneManager.LoadScene("GameModeSelection");
        }

        private void OnPlayButtonClicked()
        {
            // 무한 모드 설정
            GameStateManager.CurrentGameMode = GameMode.EndlessMode;
            GameStateManager.CurrentSlot = -1; // 무한 모드는 슬롯 사용 안 함

            // InGame 씬으로 이동
            SceneManager.LoadScene("InGame");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 게임 종료 시 순위 업데이트
        /// </summary>
        public void UpdateRanking(int wave, int level)
        {
            // SaveSystem에 저장 (자동으로 순위 정렬됨)
            string currentDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            SaveSystem.SaveEndlessModeRanking(wave, level, currentDate);

            // UI 새로고침
            RefreshRankingsUI();
        }

        /// <summary>
        /// 랭킹 UI 새로고침 (외부에서 호출 가능)
        /// </summary>
        public void RefreshRankingsUI()
        {
            LoadRankings();
            RenderRankings();
        }

        /// <summary>
        /// Inspector 데이터로 랭킹 UI 새로고침
        /// </summary>
        public void RefreshRankingsUIWithData(List<EndlessModeRecord> customRankings)
        {
            rankings.Clear();
            rankings.AddRange(customRankings);

            // 순위가 5개 미만이면 빈 슬롯으로 채움
            while (rankings.Count < 5)
            {
                rankings.Add(new EndlessModeRecord(0, 0, ""));
            }

            RenderRankings();
        }

        #endregion

        #region Debug / Test Methods

        /// <summary>
        /// 테스트용 샘플 데이터 생성 (기록이 없을 경우에만)
        /// </summary>
        private void CreateSampleDataIfNeeded()
        {
            // 기존 데이터가 있는지 확인
            var existingData = SaveSystem.GetEndlessModeRankings();

            if (existingData.Count == 0)
            {
                // 샘플 데이터 생성
                SaveSystem.SaveEndlessModeRanking(50, 10, "2025-01-15 14:30");
                SaveSystem.SaveEndlessModeRanking(42, 8, "2025-01-14 10:20");
                SaveSystem.SaveEndlessModeRanking(38, 7, "2025-01-13 16:45");
                SaveSystem.SaveEndlessModeRanking(35, 6, "2025-01-12 09:15");
                SaveSystem.SaveEndlessModeRanking(30, 5, "2025-01-11 18:00");
            }
        }

        #endregion
    }
}
