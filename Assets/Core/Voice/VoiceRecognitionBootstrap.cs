using UnityEngine;

namespace LostSpells.Systems
{
    /// <summary>
    /// 음성 인식 시스템 부트스트랩
    /// 게임 시작 시 VoiceRecognitionManager를 자동으로 생성
    /// RuntimeInitializeOnLoadMethod를 사용하여 어떤 씬에서 시작하든 자동 초기화
    /// </summary>
    public static class VoiceRecognitionBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // VoiceRecognitionManager가 없으면 생성
            if (VoiceRecognitionManager.Instance == null)
            {
                GameObject voiceManagerObj = new GameObject("VoiceRecognitionManager");
                voiceManagerObj.AddComponent<VoiceRecognitionManager>();
                // DontDestroyOnLoad은 VoiceRecognitionManager.Awake에서 처리됨
            }
        }
    }
}
