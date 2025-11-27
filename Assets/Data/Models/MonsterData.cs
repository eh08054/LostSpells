using System;

namespace LostSpells.Data
{
    /// <summary>
    /// 몬스터 기본 데이터 (JSON 직렬화 가능)
    /// </summary>
    [Serializable]
    public class MonsterData
    {
        // 기본 정보
        public string monsterName;
        public string monsterId;

        // 스탯
        public float health = 100f;
        public float moveSpeed = 3f;
        public float attackPower = 10f;
        public float defense = 5f;

        // 보상
        public int goldReward = 10;
        public int expReward = 5;

        // 비주얼 (경로 문자열로 저장)
        public string monsterPrefabPath;
        public string monsterIconPath;
    }
}
