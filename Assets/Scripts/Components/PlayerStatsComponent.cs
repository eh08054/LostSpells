using UnityEngine;
using LostSpells.Systems;

namespace LostSpells.Components
{
    /// <summary>
    /// 플레이어 스탯 컴포넌트 - 레벨, 경험치, HP, MP
    /// </summary>
    public class PlayerStatsComponent : MonoBehaviour
    {
        [Header("레벨 & 경험치")]
        public int level = 1;
        public int currentExp = 0;
        public int maxExp = 100;

        [Header("체력 (HP)")]
        public int currentHP = 100;
        public int maxHP = 100;

        [Header("마나 (MP)")]
        public int currentMP = 50;
        public int maxMP = 50;

        private void Start()
        {
            LoadData();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                SaveData();
            }
        }

        public void LoadData()
        {
            int slot = GameStateManager.CurrentSlot;
            if (GameStateManager.IsSlotUsed(slot))
            {
                var data = GameStateManager.GetSlotData(slot);
                level = data.level;
                currentExp = data.currentExp;
                maxExp = data.maxExp;
                currentHP = data.currentHP;
                maxHP = data.maxHP;
                currentMP = data.currentMP;
                maxMP = data.maxMP;

                Debug.Log($"[PlayerStats] 로드 완료: Lv.{level}, HP {currentHP}/{maxHP}");
            }
        }

        public void SaveData()
        {
            int slot = GameStateManager.CurrentSlot;
            if (GameStateManager.IsSlotUsed(slot))
            {
                var data = GameStateManager.GetSlotData(slot);
                data.level = level;
                data.currentExp = currentExp;
                data.maxExp = maxExp;
                data.currentHP = currentHP;
                data.maxHP = maxHP;
                data.currentMP = currentMP;
                data.maxMP = maxMP;
                GameStateManager.SaveSlotData(slot, data);

                Debug.Log($"[PlayerStats] 저장됨");
            }
        }
    }
}
