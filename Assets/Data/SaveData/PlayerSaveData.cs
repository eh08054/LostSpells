using System;
using System.Collections.Generic;

namespace LostSpells.Data
{
    /// <summary>
    /// 플레이어 저장 데이터
    /// JSON으로 직렬화되어 파일로 저장됨
    /// </summary>
    [Serializable]
    public class PlayerSaveData
    {
        // 플레이어 기본 정보
        public string playerName = "Player";
        public int level = 1;
        public int experience = 0;
        public int gold = 100;

        // 프리미엄 화폐
        public int diamonds = 0;
        public int reviveStones = 0;

        // 게임 진행 상황
        public int currentChapter = 1;
        public int currentStage = 1;
        public int highestChapterUnlocked = 1;

        // 무한 모드 기록
        public int endlessModeHighScore = 0;
        public int endlessModeCurrentWave = 0;

        // 스킬 및 아이템
        public List<string> unlockedSkills = new List<string>();
        public List<string> purchasedItems = new List<string>();

        // 설정
        public bool isFullScreen = true;

        // 오디오 장치 설정
        public string microphoneDeviceId = "";

        // 언어 설정
        public string uiLanguage = "Korean";
        public string voiceRecognitionLanguage = "ko";

        // 음성인식 모델 설정
        public string voiceRecognitionModel = "base";

        // 키 바인딩 (액션 이름 -> 키 코드)
        public Dictionary<string, string> keyBindings = new Dictionary<string, string>();

        // 통계
        public int totalPlayTime = 0; // 초 단위
        public int totalMonstersKilled = 0;
        public int totalDeaths = 0;

        // 마지막 저장 시간
        public string lastSaveTime;

        /// <summary>
        /// 기본 초기 데이터 생성
        /// </summary>
        public static PlayerSaveData CreateDefault()
        {
            var data = new PlayerSaveData
            {
                playerName = "Player",
                level = 1,
                experience = 0,
                gold = 100,
                diamonds = 0,
                reviveStones = 0,
                currentChapter = 1,
                currentStage = 1,
                highestChapterUnlocked = 1,
                endlessModeHighScore = 0,
                endlessModeCurrentWave = 0,
                unlockedSkills = new List<string>(),
                purchasedItems = new List<string>(),
                isFullScreen = true,
                microphoneDeviceId = "",
                uiLanguage = "Korean",
                voiceRecognitionLanguage = "ko",
                voiceRecognitionModel = "base",
                keyBindings = new Dictionary<string, string>(),
                totalPlayTime = 0,
                totalMonstersKilled = 0,
                totalDeaths = 0,
                lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            return data;
        }
    }
}
