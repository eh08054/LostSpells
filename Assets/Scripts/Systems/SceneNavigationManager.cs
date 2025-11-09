using UnityEngine;

namespace LostSpells.Systems
{
    /// <summary>
    /// 씬 네비게이션 매니저 - 이전 씬을 기억하여 뒤로가기 기능 제공
    /// </summary>
    public class SceneNavigationManager : MonoBehaviour
    {
        private static SceneNavigationManager instance;
        public static SceneNavigationManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("SceneNavigationManager");
                    instance = go.AddComponent<SceneNavigationManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private string previousScene = "MainMenu"; // 기본값은 메인메뉴

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
        /// 이전 씬 설정
        /// </summary>
        public void SetPreviousScene(string sceneName)
        {
            previousScene = sceneName;
            Debug.Log($"[SceneNavigation] 이전 씬 설정: {sceneName}");
        }

        /// <summary>
        /// 이전 씬 가져오기
        /// </summary>
        public string GetPreviousScene()
        {
            return previousScene;
        }

        /// <summary>
        /// 이전 씬으로 돌아가기 전에 기본값으로 리셋
        /// </summary>
        public void ResetToMainMenu()
        {
            previousScene = "MainMenu";
        }
    }
}
