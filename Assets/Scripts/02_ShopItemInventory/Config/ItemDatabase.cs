using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 商店貨架上的一筆商品條目。
/// 將道具本身的定義 (ItemConfigData) 與「在這間商店裡的上架規則」分離。
/// 同一個道具可以出現在不同 ItemDatabase（不同商店）中，
/// 各自有不同的排序和解鎖條件。
/// </summary>
[System.Serializable]
public class ShopEntry
{
    [Tooltip("要上架的道具")]
    public ItemConfigData Item;

    [Tooltip("商品在此商店中的顯示順序，數字越小越前面。\n" +
             "建議用 10 的倍數 (10, 20, 30...)，方便日後插入新商品。")]
    public int SortOrder = 100;

    [Tooltip("在此商店中上架所需的進度 Flag。\n" +
             "留空 (None) = 預設上架，遊戲一開始就會出現。")]
    public ProgressFlagDefinition UnlockFlag = null;
}


/// <summary>
/// 代表一間商店的完整貨物清單。
/// 每間商店各自擁有一個 ItemDatabase SO，決定要賣哪些道具、順序、解鎖條件。
/// 
/// 【用法】
/// 1. 在 Project 視窗 Create > Game/Shop/Item Database
/// 2. 把 ShopEntry 拖進 ShopEntries 列表，設定排序和解鎖條件
/// 3. 將此 SO 拖到 ShopUI 的 Assigned Database 欄位（或 GameStatusService 作為預設）
/// </summary>
[CreateAssetMenu(menuName = "Game/Shop/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [Tooltip("此商店的商品清單。\n" +
             "每筆 Entry 包含道具引用、排序順位、解鎖條件。")]
    public List<ShopEntry> ShopEntries = new List<ShopEntry>();

    // 註：ID → ItemConfigData 的全域查表已移至 ItemCatalog（自動掃 Resources/SHOP/Item）。
    // ItemDatabase 只負責「這間商店的貨架」：賣哪些、排序、解鎖條件。
    // 需要用 ID 查道具設定請改用 GameStatusService.Instance.ItemCatalog.GetItemConfig。

    /// <summary>
    /// 取得「已解鎖」的商店商品列表，依 SortOrder 由小到大排序。
    /// (給 ShopUI 用的)
    /// </summary>
    public List<ItemConfigData> GetUnlockedShopList(GameStatusService gs)
    {
        if (gs == null || gs.ProgressFlags == null)
        {
            Debug.LogError("[ItemDatabase] GameStatusService 或 ProgressFlags 尚未初始化！");
            return new List<ItemConfigData>();
        }

        // 篩選已解鎖的 Entry，按 SortOrder 排序，取出 ItemConfigData
        var result = new List<ShopEntry>();

        foreach (var entry in ShopEntries)
        {
            if (entry == null || entry.Item == null) continue;

            string flag = entry.UnlockFlag != null
                ? entry.UnlockFlag.FlagID
                : null;

            bool isUnlocked = string.IsNullOrEmpty(flag) || gs.ProgressFlags.Contains(flag);

            if (isUnlocked)
                result.Add(entry);
        }

        result.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
        return result.Select(e => e.Item).ToList();
    }

    /// <summary>
    /// 獲取所有道具的可列舉集合。
    /// (給 ShopStatusModel.ResetDailyLimits 等需要遍歷全部道具的地方用)
    /// </summary>
    public IEnumerable<ItemConfigData> AllItemsInAllTiers
        => ShopEntries
            .Where(e => e != null && e.Item != null)
            .Select(e => e.Item);
}