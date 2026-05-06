using System;
using System.Collections.Generic;
using UnityEngine; // For Debug.Log

// 職責：保存並管理主角背包中所有物品的持有數量，並處理「使用物品」的請求。

public class ProtagonistInventoryModel
{
    // Dictionary: <物品ID, 數量>
    private readonly Dictionary<string, int> _itemCounts = new Dictionary<string, int>();

    

    // ───── 事件宣告(當任何物品的數量發生變化時觸發) ─────
    public event Action<string, int> OnItemCountChanged;

    


    // ───── 核心庫存管理方法 ─────
    /// <summary>
    /// 獲取指定物品的持有數量。如果背包中沒有該物品，返回 0。
    /// </summary>
    public int GetItemCount(string itemID)
    {
        return _itemCounts.GetValueOrDefault(itemID, 0);
    }
    /// <summary>
    /// 直接設定某個物品的數量。如果數量小於等於 0，則從背包中移除該物品。
    /// </summary>
    public void SetItemCount(string itemID, int count)
    {
        if (string.IsNullOrEmpty(itemID)) return;

        int finalCount = Math.Max(0, count);

        if (finalCount > 0)
        {
            _itemCounts[itemID] = finalCount;
        }
        else
        {
            // 如果數量為 0，則從字典中移除，保持數據乾淨
            if (_itemCounts.ContainsKey(itemID))
            {
                _itemCounts.Remove(itemID);
            }
        }
       
        // 通知 UI 刷新
        OnItemCountChanged?.Invoke(itemID, finalCount);
    }
    /// <summary>
    /// 向背包中增加指定數量的物品。
    /// </summary>
    public void AddItem(string itemID, int amount)
    {
     
        if (string.IsNullOrEmpty(itemID) || amount <= 0) return;
        
        int currentCount = GetItemCount(itemID);   
        SetItemCount(itemID, currentCount + amount);
    }

    /// <summary>
    /// 從背包中移除指定數量的物品。
    /// </summary>
    public void RemoveItem(string itemID, int amount)
    {
        if (string.IsNullOrEmpty(itemID) || amount <= 0) return;

        int currentCount = GetItemCount(itemID);
        SetItemCount(itemID, currentCount - amount);
    }

    



    /// <summary>
    /// 開始新遊戲時，清空所有物品。
    /// </summary>
    public void NewGame()
    {
        _itemCounts.Clear();
        // 傳遞 null 作為 ItemID，作為一個信號，讓 UI 清空整個背包顯示
        OnItemCountChanged?.Invoke(null, 0);
    }

    // ───── 存檔與讀檔方法 (保持不變) ─────

    /// <summary>
    /// 導出 Model 的當前狀態至 InventorySaveData 結構。
    /// </summary>
    public InventorySaveData ToSaveData()
    {
        return new InventorySaveData
        {
            // 將內部字典導出
            ItemCounts = new Dictionary<string, int>(_itemCounts)
        };
    }



    /// <summary>
    /// 從存檔數據中恢復 Model 的狀態。
    /// </summary>
    public void LoadFromSaveData(InventorySaveData data)
    {
        
        if (data == null) return;       
        // 1. 清除舊數據
        _itemCounts.Clear();

        // 2. 載入新數據
        if (data.ItemCounts.Count != 0)
        {
            foreach (var kvp in data.ItemCounts)
            {
                _itemCounts[kvp.Key] = kvp.Value;
                OnItemCountChanged?.Invoke(kvp.Key, kvp.Value);
            }
        }

        // 3. 通知 UI 刷新 (傳遞 null 或空字串來指示全部刷新)
       // OnItemCountChanged?.Invoke(null, 0);
    }

    // <summary>
    /// (UI 輔助方法) 獲取背包中所有物品及其數量的字典拷貝。
    /// </summary>
    public Dictionary<string, int> GetAllItems()
    {
        // 返回一個新的字典，避免外部直接修改內部的 _itemCounts
        return new Dictionary<string, int>(_itemCounts);
    }
}