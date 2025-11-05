using UnityEngine;

namespace LostSpells.Data
{
    /// <summary>
    /// 플레이어 런타임 데이터 (순수 데이터만, 로직 없음)
    /// </summary>
    [System.Serializable]
    public class PlayerData
    {
        // 레벨 및 경험치
        public int level = 1;
        public int currentExp = 0;
        public int maxExp = 100;

        // 체력/마나
        public int currentHP = 100;
        public int maxHP = 100;
        public int currentMP = 50;
        public int maxMP = 50;

        // 화폐
        public int diamonds = 0;
        public int reviveStones = 3;

        // 진행 상황
        public int currentWave = 1;
        public int currentChapter = 1;
        public string chapterName = "Pride";

        // 키 바인딩 설정
        public KeyBindingData keyBindings = new KeyBindingData();
    }
}
