using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using LostSpells.Systems;
using LostSpells.Data;

namespace LostSpells.UI
{
    /// <summary>
    /// 무한 모드 UI - 데모 버전
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class EndlessModeUI : MonoBehaviour
    {
        private UIDocument uiDocument;
        private Button backButton;
        private Button playButton;
        private Button easyButton;
        private Button normalButton;
        private Button hardButton;
        private Label headerTitle;
        private Label rankingTitle;
        private VisualElement rankingHeader;
        private Label headerRank;
        private Label headerScore;
        private Label headerWave;
        private Label headerDate;
        private VisualElement rankingListContainer;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            var root = uiDocument.rootVisualElement;

            backButton = root.Q<Button>("BackButton");
            playButton = root.Q<Button>("PlayButton");
            easyButton = root.Q<Button>("EasyButton");
            normalButton = root.Q<Button>("NormalButton");
            hardButton = root.Q<Button>("HardButton");
            headerTitle = root.Q<Label>("HeaderTitle");
            rankingTitle = root.Q<Label>("RankingTitle");
            rankingHeader = root.Q<VisualElement>("RankingHeader");
            headerRank = root.Q<Label>("HeaderRank");
            headerScore = root.Q<Label>("HeaderScore");
            headerWave = root.Q<Label>("HeaderWave");
            headerDate = root.Q<Label>("HeaderDate");
            rankingListContainer = root.Q<VisualElement>("RankingListContainer");

            if (backButton != null)
                backButton.clicked += OnBackButtonClicked;

            if (playButton != null)
                playButton.clicked += OnPlayButtonClicked;

            // Localization event registration
            LocalizationManager.Instance.OnLanguageChanged += UpdateLocalization;

            // Update UI with current language
            UpdateLocalization();

            // Load and display high score
            UpdateHighScoreDisplay();
        }

        private void OnDisable()
        {
            if (backButton != null)
                backButton.clicked -= OnBackButtonClicked;

            if (playButton != null)
                playButton.clicked -= OnPlayButtonClicked;

            // Localization event unregistration
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

        private void OnBackButtonClicked()
        {
            SceneManager.LoadScene("GameModeSelection");
        }

        private void OnPlayButtonClicked()
        {
            SceneManager.LoadScene("InGame");
        }

        private void UpdateLocalization()
        {
            var loc = LocalizationManager.Instance;

            if (headerTitle != null)
                headerTitle.text = loc.GetText("endless_mode_title");

            if (rankingTitle != null)
                rankingTitle.text = loc.GetText("endless_mode_best_score");

            // 테이블 헤더 텍스트 (하드코딩 - 추후 Localization.json에 추가 가능)
            if (headerRank != null)
                headerRank.text = loc.GetText("endless_mode_rank");

            if (headerScore != null)
                headerScore.text = loc.GetText("endless_mode_score");

            if (headerWave != null)
                headerWave.text = loc.GetText("endless_mode_wave");

            if (headerDate != null)
                headerDate.text = loc.GetText("endless_mode_date");

            if (playButton != null)
                playButton.text = loc.GetText("endless_mode_start_game");

            if (easyButton != null)
                easyButton.text = loc.GetText("endless_mode_easy");

            if (normalButton != null)
                normalButton.text = loc.GetText("endless_mode_normal");

            if (hardButton != null)
                hardButton.text = loc.GetText("endless_mode_hard");

            // BackButton은 이미지만 사용하므로 텍스트 설정 안함
        }

        /// <summary>
        /// 최고 기록을 로드하여 랭킹 리스트에 표시 (1~5등)
        /// </summary>
        private void UpdateHighScoreDisplay()
        {
            if (rankingListContainer == null)
                return;

            // 기존 랭킹 항목 제거
            rankingListContainer.Clear();

            // SaveManager에서 상위 5개 기록 가져오기
            var topRecords = SaveManager.Instance.GetEndlessModeTopRecords();
            var loc = LocalizationManager.Instance;

            // 1등부터 5등까지 표시
            for (int i = 0; i < 5; i++)
            {
                var record = topRecords[i];

                var rankingItem = new VisualElement();
                rankingItem.AddToClassList("ranking-item");

                // 1-3등 특별 스타일 적용
                if (i == 0)
                    rankingItem.AddToClassList("ranking-item-gold");
                else if (i == 1)
                    rankingItem.AddToClassList("ranking-item-silver");
                else if (i == 2)
                    rankingItem.AddToClassList("ranking-item-bronze");

                // 순위 번호 (한국어: "1등", 영어: "1st")
                string rankText = GetRankText(i + 1, loc);
                var rankLabel = new Label(rankText);
                rankLabel.AddToClassList("ranking-rank");

                // 점수
                var scoreLabel = new Label(record.score.ToString());
                scoreLabel.AddToClassList("ranking-score");

                // 웨이브
                var waveLabel = new Label(record.wave.ToString());
                waveLabel.AddToClassList("ranking-wave");

                // 날짜
                var dateLabel = new Label(record.date);
                dateLabel.AddToClassList("ranking-date");

                rankingItem.Add(rankLabel);
                rankingItem.Add(scoreLabel);
                rankingItem.Add(waveLabel);
                rankingItem.Add(dateLabel);
                rankingListContainer.Add(rankingItem);
            }
        }

        /// <summary>
        /// 순위 번호를 언어에 맞게 포맷팅
        /// 한국어: "1등", "2등", "3등"...
        /// 영어: "1st", "2nd", "3rd", "4th", "5th"...
        /// </summary>
        private string GetRankText(int rank, LocalizationManager loc)
        {
            string rankSuffix = loc.GetText("endless_mode_rank_suffix");

            // 한국어 (접미사가 있는 경우)
            if (!string.IsNullOrEmpty(rankSuffix))
            {
                return $"{rank}{rankSuffix}";
            }

            // 영어 (서수형)
            string suffix;
            switch (rank)
            {
                case 1:
                    suffix = "st";
                    break;
                case 2:
                    suffix = "nd";
                    break;
                case 3:
                    suffix = "rd";
                    break;
                default:
                    suffix = "th";
                    break;
            }

            return $"{rank}{suffix}";
        }
    }
}
