using UnityEngine;

namespace LostSpells.Data
{
    /// <summary>
    /// 아이템 데이터 정의 (ScriptableObject)
    /// - 씬 간 공유되는 아이템 정보
    /// - 에디터에서 생성하여 관리
    /// </summary>
    // [CreateAssetMenu(fileName = "New Item", menuName = "LostSpells/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("기본 정보")]
        public string itemName;
        public string description;
        public Sprite icon;

        [Header("아이템 타입")]
        public ItemType itemType;

        [Header("가격")]
        public int buyPrice;
        public int sellPrice;

        [Header("효과")]
        public int effectValue;
    }

    public enum ItemType
    {
        Consumable,     // 소모품
        Equipment,      // 장비
        Material,       // 재료
        Currency        // 화폐
    }
}
