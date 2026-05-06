using System;
using System.Collections.Generic;
using UnityEngine; // For Debug.Log

// 職責：保存並管理商店中所有商品的「已購買次數」，並處理相關的重置邏輯。

public class ShopStatusModel
{
    // Dictionary: <物品ID, 已購買次數>使用 readonly 確保這個字典實例在 Model 生命週期內不會被替換
    private readonly Dictionary<string, int> _purchasedCounts = new Dictionary<string, int>();

    // ───── 事件宣告(當任何商品的購買次數發生變化時觸發) ─────
    public event Action<string, int> OnPurchasedCountChanged;

    // ───── 方法 (Increase/Get/Set/ResetDailyLimits... 保持不變) ─────
    /// <summary>
    /// 獲取指定商品的已購買次數。如果從未買過，返回 0。
    /// </summary>
    public int GetPurchasedCount(string itemID)
    {
        // GetValueOrDefault 是最安全和高效的方式，如果 key 不存在，它會返回預設值 (對 int 來說是 0)
        return _purchasedCounts.GetValueOrDefault(itemID, 0);
    }

    /// <summary>
    /// 為指定商品增加購買次數。
    /// </summary>
    public void IncreasePurchasedCount(string itemID, int amount)
    {
        if (string.IsNullOrEmpty(itemID) || amount <= 0) return;

        int currentCount = GetPurchasedCount(itemID);
        int newCount = currentCount + amount;

        _purchasedCounts[itemID] = newCount;
        OnPurchasedCountChanged?.Invoke(itemID, newCount);
    }

    /// <summary>
    /// 直接設定某個商品的購買次數 (例如：從存檔載入或GM工具使用)。
    /// </summary>
    public void SetPurchasedCount(string itemID, int count)
    {
        if (string.IsNullOrEmpty(itemID)) return;

        int finalCount = Math.Max(0, count); // 確保次數不為負
        _purchasedCounts[itemID] = finalCount;
        OnPurchasedCountChanged?.Invoke(itemID, finalCount);
    }

    /// <summary>
    /// 重置所有具有「每日重置」標籤的商品的購買次數。
    /// </summary>
    public void ResetDailyLimits(ItemDatabase itemDatabase)
    {
        if (itemDatabase == null) return;
        Debug.Log("[ShopStatusModel] 正在重置每日商品購買限制...");

        foreach (var itemConfig in itemDatabase.AllItemsInAllTiers)
        {
            if (itemConfig.ResetFrequency == PurchaseFrequency.Daily)
            {
                // 如果字典中存在這個商品，就將其購買次數重置為 0
                if (_purchasedCounts.ContainsKey(itemConfig.ItemID))
                {
                    SetPurchasedCount(itemConfig.ItemID, 0);
                }
            }
        }
    }
    /// <summary>
    /// 開始新遊戲時，清空所有購買記錄。
    /// </summary>
    public void NewGame()
    {
        _purchasedCounts.Clear();
        // 通知 UI 全部刷新
        OnPurchasedCountChanged?.Invoke(null, 0);
    }

    /// <summary>
    /// 導出 Model 的當前狀態至 ShopSaveData 結構。
    /// </summary>
    public ShopSaveData ToSaveData()
    {
        return new ShopSaveData
        {
            // 將內部字典導出
            PurchasedCounts = new Dictionary<string, int>(_purchasedCounts)
        };
    }

    /// <summary>
    /// 從存檔數據中恢復 Model 的狀態。
    /// </summary>
    public void LoadFromSaveData(ShopSaveData data)
    {
        if (data == null) return;

        // 1. 清除舊數據
        _purchasedCounts.Clear();

        // 2. 載入新數據
        if (data.PurchasedCounts != null)
        {
            foreach (var kvp in data.PurchasedCounts)
            {
                _purchasedCounts[kvp.Key] = kvp.Value;
            }
        }

        // 3. 通知 UI 刷新 (傳遞 null 或空字串來指示全部刷新)
        OnPurchasedCountChanged?.Invoke(null, 0);
    }
}