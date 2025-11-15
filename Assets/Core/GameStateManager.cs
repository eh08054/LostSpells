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

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 챕터 시작 - 선택한 챕터 정보 저장
        /// </summary>
        public void StartChapter(int chapterId)
        {
            currentChapterId = chapterId;
            currentWaveNumber = 1; // 웨이브 초기화
            Debug.Log($"챕터 {chapterId} 시작");
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
    }
}
