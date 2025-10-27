using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace LostSpells.UI
{
    /// <summary>
    /// Endless Mode UI 컨트롤러 - 난이도 선택 및 무한 모드 시작
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class EndlessModeUI : MonoBehaviour
    {
        public enum Difficulty
        {
            Easy,
            Normal,
            Hard
        }

        [System.Serializable]
        public class DifficultyStats
        {
            public int bestScore;
            public int bestWave;

            public DifficultyStats()
            {
                bestScore = 0;
                bestWave = 0;
            }
        }

        private UIDocument uiDocument;
        private Button backButton;
        private Button startButton;

        // 난이도 카드들
        private VisualElement easyCard;
        private VisualElement normalCard;
        private VisualElement hardCard;

        // 통계 라벨들
        private Label easyBestScore;
        private Label easyBestWave;
        private Label normalBestScore;
        private Label normalBestWave;
        private Label hardBestScore;
        private Label hardBestWave;

        // 난이도별 통계 데이터
        private DifficultyStats easyStats = new DifficultyStats();
        private DifficultyStats normalStats = new DifficultyStats();
        private DifficultyStats hardStats = new DifficultyStats();

        // 현재 선택된 난이도
        private Difficulty selectedDifficulty = Difficulty.Normal;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            var root = uiDocument.rootVisualElement;

            // UI 요소 찾기
            backButton = root.Q<Button>("BackButton");
            startButton = root.Q<Button>("StartButton");

            // 난이도 카드들
            easyCard = root.Q<VisualElement>("EasyCard");
            normalCard = root.Q<VisualElement>("NormalCard");
            hardCard = root.Q<VisualElement>("HardCard");

            // 통계 라벨들
            easyBestScore = root.Q<Label>("EasyBestScore");
            easyBestWave = root.Q<Label>("EasyBestWave");
            normalBestScore = root.Q<Label>("NormalBestScore");
            normalBestWave = root.Q<Label>("NormalBestWave");
            hardBestScore = root.Q<Label>("HardBestScore");
            hardBestWave = root.Q<Label>("HardBestWave");

            // 이벤트 등록
            if (backButton != null)
                backButton.clicked += OnBackButtonClicked;

            if (startButton != null)
                startButton.clicked += OnStartButtonClicked;

            // 난이도 카드 클릭 이벤트
            if (easyCard != null)
                easyCard.RegisterCallback<ClickEvent>(evt => OnDifficultyCardClicked(Difficulty.Easy));

            if (normalCard != null)
                normalCard.RegisterCallback<ClickEvent>(evt => OnDifficultyCardClicked(Difficulty.Normal));

            if (hardCard != null)
                hardCard.RegisterCallback<ClickEvent>(evt => OnDifficultyCardClicked(Difficulty.Hard));

            // 저장된 통계 불러오기
            LoadStats();

            // 초기 UI 업데이트
            UpdateUI();
        }

        private void OnDisable()
        {
            // 이벤트 해제
            if (backButton != null)
                backButton.clicked -= OnBackButtonClicked;

            if (startButton != null)
                startButton.clicked -= OnStartButtonClicked;

            if (easyCard != null)
                easyCard.UnregisterCallback<ClickEvent>(evt => OnDifficultyCardClicked(Difficulty.Easy));

            if (normalCard != null)
                normalCard.UnregisterCallback<ClickEvent>(evt => OnDifficultyCardClicked(Difficulty.Normal));

            if (hardCard != null)
                hardCard.UnregisterCallback<ClickEvent>(evt => OnDifficultyCardClicked(Difficulty.Hard));
        }

        #region Button Click Handlers

        private void OnBackButtonClicked()
        {
            SceneManager.LoadScene("GameModeSelection");
        }

        private void OnDifficultyCardClicked(Difficulty difficulty)
        {
            selectedDifficulty = difficulty;
            UpdateUI();

            Debug.Log($"[EndlessMode] {difficulty} 난이도 선택됨");
        }

        private void OnStartButtonClicked()
        {
            Debug.Log($"========================================");
            Debug.Log($"[EndlessMode] Endless Mode 시작!");
            Debug.Log($"[EndlessMode] 난이도: {selectedDifficulty}");
            Debug.Log($"[EndlessMode] 최고 점수: {GetCurrentStats().bestScore}");
            Debug.Log($"[EndlessMode] 최고 웨이브: {GetCurrentStats().bestWave}");
            Debug.Log($"========================================");

            // TODO: 실제 게임 씬으로 이동
            // SceneManager.LoadScene($"EndlessMode_{selectedDifficulty}");
        }

        #endregion

        #region UI Update

        private void UpdateUI()
        {
            // 모든 카드에서 선택 표시 제거
            easyCard?.RemoveFromClassList("selected-card");
            normalCard?.RemoveFromClassList("selected-card");
            hardCard?.RemoveFromClassList("selected-card");

            // 선택된 카드에 selected-card 클래스 추가
            switch (selectedDifficulty)
            {
                case Difficulty.Easy:
                    easyCard?.AddToClassList("selected-card");
                    break;
                case Difficulty.Normal:
                    normalCard?.AddToClassList("selected-card");
                    break;
                case Difficulty.Hard:
                    hardCard?.AddToClassList("selected-card");
                    break;
            }

            // 통계 업데이트
            UpdateStatsDisplay();
        }

        private void UpdateStatsDisplay()
        {
            // Easy 통계
            if (easyBestScore != null)
                easyBestScore.text = easyStats.bestScore.ToString();
            if (easyBestWave != null)
                easyBestWave.text = easyStats.bestWave.ToString();

            // Normal 통계
            if (normalBestScore != null)
                normalBestScore.text = normalStats.bestScore.ToString();
            if (normalBestWave != null)
                normalBestWave.text = normalStats.bestWave.ToString();

            // Hard 통계
            if (hardBestScore != null)
                hardBestScore.text = hardStats.bestScore.ToString();
            if (hardBestWave != null)
                hardBestWave.text = hardStats.bestWave.ToString();
        }

        #endregion

        #region Data Management

        private DifficultyStats GetCurrentStats()
        {
            switch (selectedDifficulty)
            {
                case Difficulty.Easy:
                    return easyStats;
                case Difficulty.Normal:
                    return normalStats;
                case Difficulty.Hard:
                    return hardStats;
                default:
                    return normalStats;
            }
        }

        private void LoadStats()
        {
            // TODO: PlayerPrefs 또는 세이브 시스템에서 통계 불러오기
            // 예시:
            // easyStats.bestScore = PlayerPrefs.GetInt("EndlessMode_Easy_BestScore", 0);
            // easyStats.bestWave = PlayerPrefs.GetInt("EndlessMode_Easy_BestWave", 0);

            // 테스트용 더미 데이터
            // easyStats.bestScore = 1250;
            // easyStats.bestWave = 15;
            // normalStats.bestScore = 3500;
            // normalStats.bestWave = 25;
            // hardStats.bestScore = 7800;
            // hardStats.bestWave = 40;
        }

        public void SaveStats(Difficulty difficulty, int score, int wave)
        {
            DifficultyStats stats = null;
            string difficultyName = "";

            switch (difficulty)
            {
                case Difficulty.Easy:
                    stats = easyStats;
                    difficultyName = "Easy";
                    break;
                case Difficulty.Normal:
                    stats = normalStats;
                    difficultyName = "Normal";
                    break;
                case Difficulty.Hard:
                    stats = hardStats;
                    difficultyName = "Hard";
                    break;
            }

            if (stats != null)
            {
                bool updated = false;

                // 최고 점수 갱신
                if (score > stats.bestScore)
                {
                    stats.bestScore = score;
                    updated = true;
                }

                // 최고 웨이브 갱신
                if (wave > stats.bestWave)
                {
                    stats.bestWave = wave;
                    updated = true;
                }

                if (updated)
                {
                    Debug.Log($"[EndlessMode] {difficultyName} 난이도 기록 갱신!");
                    Debug.Log($"[EndlessMode] 점수: {stats.bestScore}, 웨이브: {stats.bestWave}");

                    // TODO: PlayerPrefs에 저장
                    // PlayerPrefs.SetInt($"EndlessMode_{difficultyName}_BestScore", stats.bestScore);
                    // PlayerPrefs.SetInt($"EndlessMode_{difficultyName}_BestWave", stats.bestWave);
                    // PlayerPrefs.Save();

                    UpdateStatsDisplay();
                }
            }
        }

        #endregion
    }
}
