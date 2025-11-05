using UnityEngine;
using LostSpells.Systems;

namespace LostSpells.Components
{
    /// <summary>
    /// 진행도 컴포넌트 - 챕터, 웨이브
    /// </summary>
    public class ProgressComponent : MonoBehaviour
    {
        [Header("진행도")]
        public int currentChapter = 1;
        public string chapterName = "Lust";
        public int currentWave = 1;

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
                currentChapter = data.currentChapter;
                chapterName = data.chapterName;
                currentWave = data.currentWave;

                Debug.Log($"[Progress] 로드 완료: Ch.{currentChapter}, Wave {currentWave}");
            }
        }

        public void SaveData()
        {
            int slot = GameStateManager.CurrentSlot;
            if (GameStateManager.IsSlotUsed(slot))
            {
                var data = GameStateManager.GetSlotData(slot);
                data.currentChapter = currentChapter;
                data.chapterName = chapterName;
                data.currentWave = currentWave;
                GameStateManager.SaveSlotData(slot, data);

                Debug.Log($"[Progress] 저장됨");
            }
        }
    }
}
