using UnityEngine;
using LostSpells.Data;

namespace LostSpells.Systems
{
    /// <summary>
    /// 챕터 진행 시스템 - 챕터/웨이브 관리
    /// </summary>
    public class ChapterProgressSystem
    {
        private PlayerData playerData;

        // 챕터 제한 (7 Deadly Sins)
        private const int MIN_CHAPTER = 1;
        private const int MAX_CHAPTER = 7;

        // 웨이브 제한
        private const int MIN_WAVE = 1;
        private const int MAX_WAVE = 999;

        public ChapterProgressSystem(PlayerData data)
        {
            playerData = data;
        }

        /// <summary>
        /// 챕터 번호에 해당하는 챕터 이름 가져오기 (7 Deadly Sins)
        /// </summary>
        public static string GetChapterName(int chapterNumber)
        {
            return chapterNumber switch
            {
                1 => "Pride",      // 교만
                2 => "Greed",      // 탐욕
                3 => "Lust",       // 색욕
                4 => "Envy",       // 질투
                5 => "Gluttony",   // 폭식
                6 => "Wrath",      // 분노
                7 => "Sloth",      // 나태
                _ => "Unknown"
            };
        }

        /// <summary>
        /// 챕터 번호 설정 (자동으로 챕터 이름도 업데이트)
        /// </summary>
        public void SetChapter(int chapterNumber)
        {
            // 챕터 범위 제한 (1~7)
            if (chapterNumber < MIN_CHAPTER)
            {
                chapterNumber = MIN_CHAPTER;
                Debug.LogWarning($"[ChapterProgressSystem] 챕터가 최소값으로 조정됨: {MIN_CHAPTER}");
            }
            else if (chapterNumber > MAX_CHAPTER)
            {
                chapterNumber = MAX_CHAPTER;
                Debug.LogWarning($"[ChapterProgressSystem] 챕터가 최대값으로 조정됨: {MAX_CHAPTER}");
            }

            playerData.currentChapter = chapterNumber;
            playerData.chapterName = GetChapterName(chapterNumber);
            Debug.Log($"[ChapterProgressSystem] 챕터 설정: Chapter {chapterNumber} - {playerData.chapterName}");
        }

        /// <summary>
        /// 다음 웨이브로 진행
        /// </summary>
        public void NextWave()
        {
            if (playerData.currentWave >= MAX_WAVE)
            {
                Debug.LogWarning($"[ChapterProgressSystem] 최대 웨이브 도달 (Wave {MAX_WAVE})");
                return;
            }

            playerData.currentWave++;
            Debug.Log($"[ChapterProgressSystem] Wave {playerData.currentWave} 시작");
        }

        /// <summary>
        /// 웨이브 설정
        /// </summary>
        public void SetWave(int wave)
        {
            // 웨이브 범위 제한 (1~999)
            if (wave < MIN_WAVE)
            {
                wave = MIN_WAVE;
                Debug.LogWarning($"[ChapterProgressSystem] 웨이브가 최소값으로 조정됨: {MIN_WAVE}");
            }
            else if (wave > MAX_WAVE)
            {
                wave = MAX_WAVE;
                Debug.LogWarning($"[ChapterProgressSystem] 웨이브가 최대값으로 조정됨: {MAX_WAVE}");
            }

            playerData.currentWave = wave;
            Debug.Log($"[ChapterProgressSystem] Wave 설정: {wave}");
        }
    }
}
