using UnityEngine;
using UnityEngine.SceneManagement;

namespace LostSpells.Systems
{
    /// <summary>
    /// Options 씬이 Additive 모드로 로드될 때 AudioListener 중복을 방지
    /// 이 스크립트는 Main Camera에 부착되어야 하며, Script Execution Order를 가장 먼저 실행되도록 설정해야 합니다.
    /// </summary>
    public class AudioListenerController : MonoBehaviour
    {
        private AudioListener audioListener;

        private void Awake()
        {
            audioListener = GetComponent<AudioListener>();

            // Additive 모드로 로드된 경우 AudioListener 비활성화
            if (SceneManager.sceneCount > 1 && audioListener != null)
            {
                audioListener.enabled = false;
            }
        }
    }
}
