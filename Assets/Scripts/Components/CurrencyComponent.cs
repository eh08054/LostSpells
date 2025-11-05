using UnityEngine;
using LostSpells.Systems;

namespace LostSpells.Components
{
    /// <summary>
    /// 화폐 컴포넌트 - 다이아몬드, 부활석
    /// </summary>
    public class CurrencyComponent : MonoBehaviour
    {
        [Header("화폐")]
        public int diamonds = 0;
        public int reviveStones = 3;

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
                diamonds = data.diamonds;
                reviveStones = data.reviveStones;

                Debug.Log($"[Currency] 로드 완료: 💎{diamonds}, 🔮{reviveStones}");
            }
        }

        public void SaveData()
        {
            int slot = GameStateManager.CurrentSlot;
            if (GameStateManager.IsSlotUsed(slot))
            {
                var data = GameStateManager.GetSlotData(slot);
                data.diamonds = diamonds;
                data.reviveStones = reviveStones;
                GameStateManager.SaveSlotData(slot, data);

                Debug.Log($"[Currency] 저장됨");
            }
        }
    }
}
