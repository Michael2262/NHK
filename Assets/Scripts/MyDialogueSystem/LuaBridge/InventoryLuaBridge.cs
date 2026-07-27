using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>
/// 將 ProtagonistInventoryModel 的道具持有狀態橋接到 Dialogue System 的 Lua 環境，
/// 讓對話條件式 (Conditions) 可以依「是否持有某 ID 道具」與「持有數量」判斷分支。
/// 掛載於 GameStatusService 同一個 GameObject 上（與 ProgressLuaBridge 相同）。
/// </summary>
public class InventoryLuaBridge : MonoBehaviour
{
    void OnEnable()
    {
        RegisterLuaFunctions();
    }

    void OnDisable()
    {
        UnregisterLuaFunctions();
    }

    // ==========================================================
    // Lua 函數註冊 / 取消註冊
    // ==========================================================

    private void RegisterLuaFunctions()
    {
        // 註：Dialogue System 透過反射呼叫，不支援 C# 選擇性參數，
        // 故不同參數數量必須拆成不同名的函式分別註冊。
        Lua.RegisterFunction("HasItem", this, typeof(InventoryLuaBridge).GetMethod("HasItem"));
        Lua.RegisterFunction("HasItemCount", this, typeof(InventoryLuaBridge).GetMethod("HasItemCount"));
        Lua.RegisterFunction("ItemCount", this, typeof(InventoryLuaBridge).GetMethod("ItemCount"));

        Debug.Log("InventoryLuaBridge: Lua functions registered.");
    }

    private void UnregisterLuaFunctions()
    {
        Lua.UnregisterFunction("HasItem");
        Lua.UnregisterFunction("HasItemCount");
        Lua.UnregisterFunction("ItemCount");

        Debug.Log("InventoryLuaBridge: Lua functions unregistered.");
    }

    // ==========================================================
    // 道具相關 Lua 函數
    // ==========================================================

    private ProtagonistInventoryModel GetInventory()
    {
        if (GameStatusService.Instance == null)
        {
            Debug.LogWarning("InventoryLuaBridge: GameStatusService not available.");
            return null;
        }
        return GameStatusService.Instance.Inventory;
    }

    /// <summary>
    /// 是否持有指定 ID 的道具（數量 ≥ 1）。
    /// Lua 用法: HasItem("sticker") → 回傳 true/false
    /// </summary>
    public bool HasItem(string itemID)
    {
        var inv = GetInventory();
        if (inv == null || string.IsNullOrEmpty(itemID)) return false;
        return inv.GetItemCount(itemID) > 0;
    }

    /// <summary>
    /// 是否持有足夠數量的指定道具（數量 ≥ count）。
    /// Lua 用法: HasItemCount("sticker", 3) → 回傳 true/false
    /// </summary>
    public bool HasItemCount(string itemID, double count)
    {
        var inv = GetInventory();
        if (inv == null || string.IsNullOrEmpty(itemID)) return false;
        return inv.GetItemCount(itemID) >= (int)count;
    }

    /// <summary>
    /// 取得指定 ID 道具目前的持有數量（沒有則回傳 0）。
    /// Lua 用法: ItemCount("sticker") → 回傳數量，可用於 ItemCount("sticker") >= 5 之類的比較
    /// </summary>
    public double ItemCount(string itemID)
    {
        var inv = GetInventory();
        if (inv == null || string.IsNullOrEmpty(itemID)) return 0;
        return inv.GetItemCount(itemID);
    }
}
