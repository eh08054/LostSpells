using System;
using System.Collections.Generic;

namespace LostSpells.Data
{
    /// <summary>
    /// 무한 모드 기록
    /// </summary>
    [Serializable]
    public class EndlessModeRecord
    {
        public int score;           // 점수
        public int wave;            // 클리어한 웨이브
        public string date;         // 날짜

        public EndlessModeRecord(int score, int wave, string date)
        {
            this.score = score;
            this.wave = wave;
            this.date = date;
        }
    }

    /// <summary>
    /// 챕터 진행 상황
    /// </summary>
    [Serializable]
    public class ChapterProgress
    {
        public int chapterId;           // 챕터 ID
        public int clearedWaves;        // 클리어한 웨이브 수 (0~3)
        public bool isCompleted;        // 챕터 완료 여부

        public ChapterProgress(int chapterId, int clearedWaves = 0, bool isCompleted = false)
        {
            this.chapterId = chapterId;
            this.clearedWaves = clearedWaves;
            this.isCompleted = isCompleted;
        }
    }

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

        // 무한 모드 기록 (상위 5개 기록 저장)
        public List<EndlessModeRecord> endlessModeTopRecords = new List<EndlessModeRecord>();
        public int endlessModeCurrentWave = 0;

        // 챕터별 진행 상황
        public List<ChapterProgress> chapterProgressList = new List<ChapterProgress>();

        // 스킬 및 아이템
        public List<string> unlockedSkills = new List<string>();
        public List<string> purchasedItems = new List<string>();

        // 설정
        public bool isFullScreen = true;
        public int qualityLevel = 2; // 0: 1280x720, 1: 1600x900, 2: 1920x1080, 3: 2560x1440
        public int screenMode = 1;   // 0: Windowed, 1: Fullscreen

        // 오디오 장치 설정
        public string microphoneDeviceId = "";

        // 언어 설정
        public string uiLanguage = "Korean";
        public string voiceRecognitionLanguage = "ko";

        // 음성인식 모델 설정
        public string voiceRecognitionModel = "base";

        // 음성인식 서버 모드 (online/offline)
        public string voiceServerMode = "online";

        // 음성 입력 모드 (KeyTriggered/Continuous)
        public string voiceInputMode = "KeyTriggered";

        // 피치 분석 경계 주파수 (Hz)
        // 기본값: 최소 130.81 (C3), 최대 261.63 (C4)
        public float pitchMinFrequency = 130.81f;
        public float pitchMaxFrequency = 261.63f;

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
                endlessModeTopRecords = new List<EndlessModeRecord>
                {
                    new EndlessModeRecord(0, 0, "0000-00-00"),
                    new EndlessModeRecord(0, 0, "0000-00-00"),
                    new EndlessModeRecord(0, 0, "0000-00-00"),
                    new EndlessModeRecord(0, 0, "0000-00-00"),
                    new EndlessModeRecord(0, 0, "0000-00-00")
                }, // 1~5등 초기값
                endlessModeCurrentWave = 0,
                chapterProgressList = new List<ChapterProgress>(),
                unlockedSkills = new List<string>(),
                purchasedItems = new List<string>(),
                isFullScreen = true,
                qualityLevel = 2,
                screenMode = 1,
                microphoneDeviceId = "",
                uiLanguage = "Korean",
                voiceRecognitionLanguage = "ko",
                voiceRecognitionModel = "base",
                voiceServerMode = "online",
                voiceInputMode = "KeyTriggered",
                pitchMinFrequency = 130.81f,
                pitchMaxFrequency = 261.63f,
                keyBindings = new Dictionary<string, string>(),
                totalPlayTime = 0,
                totalMonstersKilled = 0,
                totalDeaths = 0,
                lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            return data;
        }

        /// <summary>
        /// 특정 챕터의 진행 상황 가져오기
        /// </summary>
        public ChapterProgress GetChapterProgress(int chapterId)
        {
            var progress = chapterProgressList.Find(p => p.chapterId == chapterId);
            if (progress == null)
            {
                // 없으면 새로 생성
                progress = new ChapterProgress(chapterId, 0, false);
                chapterProgressList.Add(progress);
            }
            return progress;
        }
    }
}
