using System;

namespace LostSpells.Data
{
    /// <summary>
    /// 상점 아이템 타입
    /// </summary>
    public enum StoreItemType
    {
        Diamond,        // 다이아몬드
        ReviveStone,    // 부활석
        Gold            // 골드
    }

    /// <summary>
    /// 가격 타입
    /// </summary>
    public enum PriceType
    {
        RealMoney,      // 실제 돈 ($)
        Diamond         // 다이아몬드
    }

    /// <summary>
    /// 상점 아이템 데이터
    /// </summary>
    [Serializable]
    public class StoreItemData
    {
        public string itemId;           // 아이템 ID
        public string itemName;         // 아이템 이름
        public StoreItemType itemType;  // 아이템 타입
        public int quantity;            // 수량
        public int price;               // 가격
        public PriceType priceType;     // 가격 타입 (실제 돈 or 다이아몬드)
        public string iconPath;         // 아이콘 경로
        public bool isBestValue;        // 베스트 밸류 여부

        public StoreItemData(string id, string name, StoreItemType type, int qty, int price, PriceType priceType, bool bestValue = false)
        {
            this.itemId = id;
            this.itemName = name;
            this.itemType = type;
            this.quantity = qty;
            this.price = price;
            this.priceType = priceType;
            this.isBestValue = bestValue;
        }
    }
}
