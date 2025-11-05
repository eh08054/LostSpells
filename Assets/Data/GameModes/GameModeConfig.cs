using UnityEngine;
using LostSpells.Systems;

namespace LostSpells.Data
{
    /// <summary>
    /// 게임모드별 설정 데이터 (ScriptableObject)
    /// - 스토리모드, 무한모드 등 모드별 다른 설정
    /// </summary>
    // [CreateAssetMenu(fileName = "New GameMode", menuName = "LostSpells/GameMode Config")]
    public class GameModeConfig : ScriptableObject
    {
        [Header("모드 정보")]
        public string modeName;
        public GameModeType modeType;

        [Header("플레이어 초기 상태")]
        public int startingLevel = 1;
        public int startingHP = 100;
        public int startingMP = 50;
        public int startingDiamonds = 0;
        public int startingReviveStones = 3;

        [Header("난이도 설정")]
        public DifficultyLevel difficulty;
        public float enemyHealthMultiplier = 1.0f;
        public float enemyDamageMultiplier = 1.0f;

        [Header("웨이브 설정")]
        public int startingWave = 1;
        public bool isEndlessMode = false;

        [Header("챕터 설정")]
        [Tooltip("인게임에서 챕터를 변경할 수 있는지 여부 (스토리모드: true, 챕터선택모드: false)")]
        public bool canChangeChapterInGame = true;
    }

    public enum GameModeType
    {
        Story,
        Endless,
        Chapter
    }
}
