using UnityEngine;

namespace LostSpells.Data
{
    /// <summary>
    /// 스킬 데이터 정의 (ScriptableObject)
    /// - 씬 간 공유되는 스킬 정보
    /// </summary>
    // [CreateAssetMenu(fileName = "New Skill", menuName = "LostSpells/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [Header("기본 정보")]
        public string skillName;
        public string description;
        public Sprite icon;

        [Header("스킬 타입")]
        public SkillCategory category;

        [Header("스탯")]
        public int damage;
        public float cooldown;
        public int manaCost;

        [Header("레벨별 정보")]
        public int maxLevel = 5;
        public float damagePerLevel = 1.2f;
    }

    public enum SkillCategory
    {
        Attack,
        Defense,
        Support
    }
}
