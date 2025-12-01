using UnityEngine;
using LostSpells.Data;

namespace LostSpells.Systems
{
    /// <summary>
    /// 게임 상태 관리 싱글톤
    /// 씬 간에 유지되어야 하는 게임 상태 정보를 관리
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        private static GameStateManager instance;
        public static GameStateManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("GameStateManager");
                    instance = go.AddComponent<GameStateManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        // 현재 선택된 챕터 정보
        private int currentChapterId = -1;
        private int currentWaveNumber = 1;

        // 현재 맵 설정 (Sky, Mountain, Ground 번호)
        private int currentSkyNumber = 12;
        private int currentMountainNumber = 1;
        private int currentGroundNumber = 1;

        // 12개의 미리 정의된 맵 (챕터별로 순서대로 배정)
        private static readonly (int sky, int mountain, int ground)[] predefinedMaps = new[]
        {
            (12, 1, 1),   // 챕터 1: Sky-12, Mountain-1, Ground-1
            (5, 6, 4),    // 챕터 2: Sky-5, Mountain-6, Ground-4
            (15, 8, 6),   // 챕터 3: Sky-15, Mountain-8, Ground-6
            (6, 9, 19),   // 챕터 4: Sky-6, Mountain-9, Ground-19
            (9, 3, 13),   // 챕터 5: Sky-9, Mountain-3, Ground-13
            (14, 5, 16),  // 챕터 6: Sky-14, Mountain-5, Ground-16
            (15, 5, 10),  // 챕터 7: Sky-15, Mountain-5, Ground-10
            (11, 4, 17),  // 챕터 8: Sky-11, Mountain-4, Ground-17
            (13, 5, 20),  // 챕터 9: Sky-13, Mountain-5, Ground-20
            (3, 2, 4),    // 챕터 10: Sky-3, Mountain-2, Ground-4
            (4, 7, 18),   // 챕터 11: Sky-4, Mountain-7, Ground-18
            (5, 1, 21),   // 챕터 12: Sky-5, Mountain-1, Ground-21
        };

        // 이전 씬 정보 (상점 등에서 돌아갈 때 사용)
        private string previousScene = "";

        // 엔드리스 모드 여부
        private bool isEndlessMode = false;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            // 씬 로드 시 AudioListener 중복 체크
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            // AudioListener 중복 체크 및 수정
            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            if (listeners.Length > 1)
            {
                // Main Camera의 AudioListener만 남기고 제거
                foreach (var listener in listeners)
                {
                    if (listener.gameObject.name != "Main Camera")
                    {
                        Destroy(listener);
                    }
                }
            }
        }

        /// <summary>
        /// 챕터 시작 - 선택한 챕터 정보 저장 및 맵 설정
        /// </summary>
        public void StartChapter(int chapterId)
        {
            currentChapterId = chapterId;
            currentWaveNumber = 1; // 웨이브 초기화
            isEndlessMode = false;

            // 챕터 ID에 따른 맵 설정 (1-12 챕터 순서대로, 초과 시 순환)
            int mapIndex = (chapterId - 1) % predefinedMaps.Length;
            if (mapIndex < 0) mapIndex = 0;

            var map = predefinedMaps[mapIndex];
            currentSkyNumber = map.sky;
            currentMountainNumber = map.mountain;
            currentGroundNumber = map.ground;

            Debug.Log($"[GameState] Chapter {chapterId} started with Map {mapIndex + 1}: Sky-{currentSkyNumber}, Mountain-{currentMountainNumber}, Ground-{currentGroundNumber}");
        }

        /// <summary>
        /// 엔드리스 모드 시작
        /// </summary>
        public void StartEndlessMode()
        {
            currentChapterId = -1;
            currentWaveNumber = 1;
            isEndlessMode = true;

            // 엔드리스 모드는 랜덤 맵 선택
            int mapIndex = Random.Range(0, predefinedMaps.Length);
            var map = predefinedMaps[mapIndex];
            currentSkyNumber = map.sky;
            currentMountainNumber = map.mountain;
            currentGroundNumber = map.ground;

            Debug.Log($"[GameState] Endless Mode started with Map {mapIndex + 1}: Sky-{currentSkyNumber}, Mountain-{currentMountainNumber}, Ground-{currentGroundNumber}");
        }

        /// <summary>
        /// 엔드리스 모드 여부 확인
        /// </summary>
        public bool IsEndlessMode()
        {
            return isEndlessMode;
        }

        /// <summary>
        /// 현재 챕터 ID 가져오기
        /// </summary>
        public int GetCurrentChapterId()
        {
            return currentChapterId;
        }

        /// <summary>
        /// 현재 챕터 데이터 가져오기
        /// </summary>
        public ChapterData GetCurrentChapterData()
        {
            if (currentChapterId < 0)
                return null;

            return DataManager.Instance.GetChapterData(currentChapterId);
        }

        /// <summary>
        /// 현재 웨이브 번호 설정
        /// </summary>
        public void SetCurrentWave(int waveNumber)
        {
            currentWaveNumber = waveNumber;
        }

        /// <summary>
        /// 현재 웨이브 번호 가져오기
        /// </summary>
        public int GetCurrentWave()
        {
            return currentWaveNumber;
        }

        /// <summary>
        /// 현재 맵의 Sky 번호 가져오기
        /// </summary>
        public int GetCurrentSkyNumber()
        {
            return currentSkyNumber;
        }

        /// <summary>
        /// 현재 맵의 Mountain 번호 가져오기
        /// </summary>
        public int GetCurrentMountainNumber()
        {
            return currentMountainNumber;
        }

        /// <summary>
        /// 현재 맵의 Ground 번호 가져오기
        /// </summary>
        public int GetCurrentGroundNumber()
        {
            return currentGroundNumber;
        }

        /// <summary>
        /// 게임 상태 초기화
        /// </summary>
        public void ResetGameState()
        {
            currentChapterId = -1;
            currentWaveNumber = 1;
            currentSkyNumber = 12;
            currentMountainNumber = 1;
            currentGroundNumber = 1;
            isEndlessMode = false;
        }

        /// <summary>
        /// 이전 씬 설정 (상점 등으로 이동할 때 호출)
        /// </summary>
        public void SetPreviousScene(string sceneName)
        {
            previousScene = sceneName;
        }

        /// <summary>
        /// 이전 씬 가져오기
        /// </summary>
        public string GetPreviousScene()
        {
            return previousScene;
        }

        /// <summary>
        /// 이전 씬이 인게임인지 확인
        /// </summary>
        public bool IsFromInGame()
        {
            return previousScene == "InGame";
        }
    }
}
