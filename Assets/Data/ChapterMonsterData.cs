using UnityEngine;

namespace LostSpells.Data
{
    /// <summary>
    /// 챕터별 몬스터 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "ChapterMonsterData", menuName = "LostSpells/Chapter Monster Data")]
    public class ChapterMonsterData : ScriptableObject
    {
        [Header("Chapter Info")]
        [Tooltip("챕터 ID (1~8)")]
        public int chapterId;

        [Header("Monster Sprite")]
        [Tooltip("이 챕터에서 사용할 몬스터 스프라이트")]
        public Sprite monsterSprite;

        [Header("Monster Stats")]
        [Tooltip("몬스터 이름")]
        public string monsterName = "Dragon";

        [Tooltip("기본 체력")]
        public int baseHealth = 50;

        [Tooltip("기본 이동 속도")]
        public float baseSpeed = 2f;
    }
}
