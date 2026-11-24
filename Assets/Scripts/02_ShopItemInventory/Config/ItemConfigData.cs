using UnityEngine;
using System.Collections.Generic;

/// <summary>僅供 UI 篩選的分類，不決定消耗、購買上限或效果。</summary>
public enum ItemCategory
{
    [InspectorName("消耗（Use）")] Use = 0,
    [InspectorName("永久（Claim）")] Claim = 1
}

/// <summary>道具本身的設定；上架順序與解鎖條件由 ItemDatabase 管理。</summary>
[CreateAssetMenu(menuName = "Game/Shop/Item Config")]
public class ItemConfigData : ScriptableObject
{
    public string ItemID => name;

    [Header("基礎屬性")]
    [Tooltip("供開發時辨識，遊戲內文字使用下方的 Key。")]
    public string DisplayName = "道具名";
    public string Description = "道具說明";
    public string DisplayNameKey = "ITEM_NAME_DEFAULT";
    [Tooltip("一般道具說明；背包固定使用此 Key。")]
    public string DescriptionKey = "ITEM_DESC_DEFAULT";
    [Tooltip("商店專用說明 Key，可留空；空白時沿用 DescriptionKey。")]
    public string ShopDescriptionKey = "";
    public Sprite Icon;

    [Header("分類")]
    [Tooltip("僅供商店與背包篩選，不影響入庫、消耗或效果。")]
    public ItemCategory Category = ItemCategory.Use;

    [Header("購買與限制")]
    [Tooltip("購買一份的價格，不是單個內容物的價格。")]
    [Min(0)] public int Price = 100;
    [Tooltip("每買一份，背包增加多少個此道具。例如每包 3 個保險套，填 3。")]
    [Min(1)] public int PurchaseQuantity = 1;
    [Tooltip("最多購買幾份；0 = 無限制。每份含多個道具仍只計一次。")]
    [Min(0)] public int MaxPurchaseLimit = 0;
    public PurchaseFrequency ResetFrequency = PurchaseFrequency.None;

    [Header("配送模式")]
    [Tooltip("網購：隔天 P0S1 到貨時才入庫並執行購得效果。")]
    public bool IsOnlineOrder;

    [Header("購得效果")]
    [Tooltip("每份商品實際收到時執行一次，不受分類或每份數量影響。\n" +
             "留空 = 只取得商品。額外給予道具不會連鎖執行其購得效果。")]
    [SerializeReference] public List<ItemEffect> AcquireEffects = new List<ItemEffect>();

    public string EffectiveShopDescriptionKey =>
        string.IsNullOrWhiteSpace(ShopDescriptionKey) ? DescriptionKey : ShopDescriptionKey;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Icon != null) return;
        string path = $"SHOP/ItemImage/Item_{ItemID}";
        var found = Resources.Load<Sprite>(path);
        if (found != null) Icon = found;
        else Debug.LogWarning($"[ItemConfigData] 找不到 Icon: Resources/{path}  (ItemID={ItemID})");
    }
#endif
}

public enum PurchaseFrequency { None, Daily, Weekly }
