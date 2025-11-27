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

        // 이전 씬 정보 (상점 등에서 돌아갈 때 사용)
        private string previousScene = "";

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
        /// 챕터 시작 - 선택한 챕터 정보 저장
        /// </summary>
        public void StartChapter(int chapterId)
        {
            currentChapterId = chapterId;
            currentWaveNumber = 1; // 웨이브 초기화
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
        /// 게임 상태 초기화
        /// </summary>
        public void ResetGameState()
        {
            currentChapterId = -1;
            currentWaveNumber = 1;
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
