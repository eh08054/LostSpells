using UnityEngine;
using LostSpells.Systems;

namespace LostSpells.Components
{
    /// <summary>
    /// 게임 상태 정보 컴포넌트 - 읽기 전용
    /// </summary>
    public class GameStateInfoComponent : MonoBehaviour
    {
        [Header("게임 상태 (읽기 전용)")]
        [SerializeField] private int currentSlot;
        [SerializeField] private GameMode gameMode;
        [SerializeField] private int selectedChapter;
        [SerializeField] private DifficultyLevel difficulty;

        private void Start()
        {
            UpdateInfo();
        }

        private void Update()
        {
            // 매 프레임 정보 갱신 (GameStateManager가 변경될 수 있으므로)
            UpdateInfo();
        }

        private void UpdateInfo()
        {
            currentSlot = GameStateManager.CurrentSlot;
            gameMode = GameStateManager.CurrentGameMode;
            selectedChapter = GameStateManager.SelectedChapter;
            difficulty = GameStateManager.SelectedDifficulty;
        }
    }
}
