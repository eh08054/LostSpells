using System;
using System.Collections.Generic;

namespace LostSpells.Data
{
    /// <summary>
    /// 챕터 데이터 (JSON 직렬화 가능)
    /// </summary>
    [Serializable]
    public class ChapterData
    {
        // 기본 정보
        public int chapterId;
        public string chapterName;          // 영어 이름 (기본값)
        public string chapterNameKo;        // 한국어 이름
        public string chapterDescription;   // 영어 설명
        public string chapterDescriptionKo; // 한국어 설명

        // 7대 죄악 테마
        public string sinName;
        public SerializableColor sinColor = new SerializableColor(1f, 1f, 1f, 1f);

        // 난이도
        public int recommendedLevel = 1;
        public int difficulty = 1;

        // 잠금 조건
        public int requiredLevel = 1;
        public int requiredChapterId = -1;

        // 웨이브 구성
        public List<WaveConfig> waves = new List<WaveConfig>();

        // 보상
        public int goldReward = 100;
        public int expReward = 50;
        public int firstClearBonusGold = 50;

        // 비주얼 (경로 문자열로 저장)
        public string thumbnailPath;
        public string backgroundImagePath;
        public string iconPath;

        // 오디오 (경로 문자열로 저장)
        public string bgmPath;

        /// <summary>
        /// 챕터가 잠겨있는지 확인
        /// </summary>
        public bool IsLocked(int playerLevel, List<int> clearedChapterIds)
        {
            // 레벨 체크
            if (playerLevel < requiredLevel)
                return true;

            // 이전 챕터 클리어 체크
            if (requiredChapterId >= 0 && !clearedChapterIds.Contains(requiredChapterId))
                return true;

            return false;
        }

        /// <summary>
        /// 총 웨이브 수
        /// </summary>
        public int TotalWaves => waves != null ? waves.Count : 0;

        /// <summary>
        /// 현재 언어에 맞는 챕터 이름 반환
        /// </summary>
        public string GetLocalizedName()
        {
            var currentLanguage = LostSpells.Systems.LocalizationManager.Instance.CurrentLanguage;
            if (currentLanguage == LostSpells.Systems.Language.Korean)
                return !string.IsNullOrEmpty(chapterNameKo) ? chapterNameKo : chapterName;
            else
                return !string.IsNullOrEmpty(chapterName) ? chapterName : chapterNameKo;
        }

        /// <summary>
        /// 현재 언어에 맞는 챕터 설명 반환
        /// </summary>
        public string GetLocalizedDescription()
        {
            var currentLanguage = LostSpells.Systems.LocalizationManager.Instance.CurrentLanguage;
            if (currentLanguage == LostSpells.Systems.Language.Korean)
                return !string.IsNullOrEmpty(chapterDescriptionKo) ? chapterDescriptionKo : chapterDescription;
            else
                return !string.IsNullOrEmpty(chapterDescription) ? chapterDescription : chapterDescriptionKo;
        }
    }

    /// <summary>
    /// JSON 직렬화 가능한 Color 구조체
    /// </summary>
    [Serializable]
    public class SerializableColor
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public SerializableColor(float r, float g, float b, float a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }
    }
}
