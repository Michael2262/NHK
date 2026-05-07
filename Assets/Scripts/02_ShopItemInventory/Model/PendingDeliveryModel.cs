using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 職責：管理所有「網購」訂單的生命週期。
/// 下單 → 等待 → 到貨時自動入庫並觸發提示事件。
/// </summary>
public class PendingDeliveryModel
{
    private readonly List<DeliveryOrder> _pendingOrders = new List<DeliveryOrder>();

    // ───── 事件 ─────

    /// <summary>
    /// 當有貨物到貨並入庫時觸發（不帶參數，UI 端只需顯示「你的貨品已送達」）。
    /// </summary>
    public event Action OnDeliveryArrived;

    // ───── 下單 ─────

    /// <summary>
    /// 新增一筆網購訂單。到貨日 = 當前天數 + 1。
    /// </summary>
    /// <param name="itemID">商品 ID</param>
    /// <param name="amount">數量</param>
    /// <param name="currentDay">下單時的遊戲天數（Protagonist.Day）</param>
    public void PlaceOrder(string itemID, int amount, int currentDay)
    {
        if (string.IsNullOrEmpty(itemID) || amount <= 0) return;

        int deliveryDay = currentDay + 1;

        _pendingOrders.Add(new DeliveryOrder(itemID, amount, deliveryDay));

        Debug.Log($"[PendingDeliveryModel] 下單：{itemID} x{amount}，預計第 {deliveryDay} 天到貨。");
    }

    // ───── 到貨檢查（由 GameStatusService 在正確時機呼叫） ─────

    /// <summary>
    /// 檢查是否有訂單應在今天到貨，若有則全部入庫並觸發一次提示事件。
    /// 預期呼叫時機：隔天的 P0S1（Phase 0, Slot 1）。
    /// </summary>
    /// <param name="currentDay">當前遊戲天數</param>
    /// <param name="inventory">主角背包 Model，用於入庫</param>
    public void ProcessDeliveries(int currentDay, ProtagonistInventoryModel inventory)
    {
        bool anyDelivered = false;

        // 倒序遍歷，方便移除
        for (int i = _pendingOrders.Count - 1; i >= 0; i--)
        {
            var order = _pendingOrders[i];

            if (currentDay >= order.DeliveryDay)
            {
                // 入庫
                inventory.AddItem(order.ItemID, order.Amount);
                Debug.Log($"[PendingDeliveryModel] 到貨入庫：{order.ItemID} x{order.Amount}");

                _pendingOrders.RemoveAt(i);
                anyDelivered = true;
            }
        }

        // 只要這批有任何東西到貨，就觸發一次提示
        if (anyDelivered)
        {
            OnDeliveryArrived?.Invoke();
        }
    }

    // ───── 查詢 ─────

    /// <summary>
    /// 是否有任何待到貨的訂單。
    /// </summary>
    public bool HasPendingOrders => _pendingOrders.Count > 0;

    /// <summary>
    /// 取得所有待到貨訂單的唯讀拷貝（Debug / UI 用）。
    /// </summary>
    public List<DeliveryOrder> GetPendingOrders()
    {
        return new List<DeliveryOrder>(_pendingOrders);
    }

    // ───── 新遊戲 / 存讀檔 ─────

    public void NewGame()
    {
        _pendingOrders.Clear();
    }

    public DeliverySaveData ToSaveData()
    {
        return new DeliverySaveData
        {
            PendingOrders = new List<DeliveryOrder>(_pendingOrders)
        };
    }

    public void LoadFromSaveData(DeliverySaveData data)
    {
        _pendingOrders.Clear();

        if (data?.PendingOrders != null)
        {
            _pendingOrders.AddRange(data.PendingOrders);
        }
    }
}
