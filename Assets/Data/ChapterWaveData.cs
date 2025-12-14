using UnityEngine;

namespace LostSpells.Data
{
    /// <summary>
    /// 챕터별 웨이브 데이터
    /// Unity ScriptableObject는 클래스명과 파일명이 일치해야 함
    /// </summary>
    [CreateAssetMenu(fileName = "ChapterWaveData", menuName = "LostSpells/Chapter Wave Data")]
    public class ChapterWaveData : ScriptableObject
    {
        [Header("Chapter Info")]
        [Tooltip("챕터 번호 (1~8)")]
        public int chapterId = 1;

        [Tooltip("챕터 이름")]
        public string chapterName = "Chapter 1";

        [Header("Wave Data")]
        [Tooltip("이 챕터의 웨이브들")]
        public WaveInfo[] waves;

        /// <summary>
        /// 특정 웨이브 데이터 가져오기
        /// </summary>
        public WaveInfo GetWave(int waveNumber)
        {
            if (waves == null || waves.Length == 0)
                return null;

            foreach (var wave in waves)
            {
                if (wave.waveNumber == waveNumber)
                    return wave;
            }

            // 마지막 웨이브 반복 (무한 모드용)
            return waves[waves.Length - 1];
        }
    }
}
