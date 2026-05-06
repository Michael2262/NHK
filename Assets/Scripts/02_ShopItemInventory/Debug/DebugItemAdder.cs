using UnityEngine;

/// <summary>
/// 【Debug 測試腳本】
/// 允許您在 Play Mode 中，透過 Inspector 右鍵選單，
/// 將指定 ID 的物品添加到玩家背包中。
/// </summary>
public class DebugItemAdder : MonoBehaviour
{
    [Header("測試設定")]
    [Tooltip("請輸入您 ItemConfigData 的 ItemID (即 .asset 檔案名稱)")]
    public string itemIDToAdd = "YOUR_ITEM_ID_HERE"; // 讓您在 Inspector 定義

    [Tooltip("要給予的數量")]
    public int amountToAdd = 1;


    [ContextMenu("執行：給予物品")]
    private void GiveItemToPlayer()
    {
        if (string.IsNullOrEmpty(itemIDToAdd) || itemIDToAdd == "YOUR_ITEM_ID_HERE")
        {
            Debug.LogWarning("請先在 Inspector 的 'Item ID To Add' 欄位中指定一個有效的 ItemID。", this);
            return;
        }

        if (GameStatusService.Instance == null)
        {
            Debug.LogError("GameStatusService 尚未初始化！", this);
            return;
        }

        // 1. 獲取背包服務
        // (我們從 BackpackUI.cs 得知可以這樣存取)
        var inventory = GameStatusService.Instance.Inventory;

        if (inventory == null)
        {
            Debug.LogError("Inventory 服務尚未初始化！", this);
            return;
        }

        // 2. 呼叫 AddItem
        // (BackpackUI.cs 使用了 .RemoveItem，我們推斷 .AddItem 是對應的方法)
        inventory.AddItem(itemIDToAdd, amountToAdd);

        Debug.Log($"[Debug] 成功給予物品：{itemIDToAdd} x {amountToAdd}。");

        // 提醒：
        // 由於 BackpackUI 訂閱了 OnItemCountChanged 事件，
        // 如果您的 AddItem 方法有正確觸發這個事件，
        // 且 BackpackUI 是開啟狀態，您應該會看到 UI 自動刷新。
    }
}