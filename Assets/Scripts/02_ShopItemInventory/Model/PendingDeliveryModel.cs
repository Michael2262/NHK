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
    private bool _isProcessing;

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
    /// <param name="amount">實際入庫個數</param>
    /// <param name="currentDay">下單時的遊戲天數（Protagonist.Day）</param>
    /// <param name="purchaseCount">購買份數，決定購得效果執行次數；省略時沿用舊版每份一個。</param>
    public void PlaceOrder(string itemID, int amount, int currentDay, int purchaseCount = 0)
    {
        if (string.IsNullOrEmpty(itemID) || amount <= 0) return;

        int deliveryDay = currentDay + 1;

        _pendingOrders.Add(new DeliveryOrder(itemID, amount, deliveryDay, purchaseCount));

        Debug.Log($"[PendingDeliveryModel] 下單：{itemID} x{amount}，預計第 {deliveryDay} 天到貨。");
    }

    // ───── 到貨檢查（由 GameStatusService 在正確時機呼叫） ─────

    /// <summary>
    /// 檢查是否有訂單應在今天到貨，若有則全部入庫並觸發一次提示事件。
    /// 預期呼叫時機：隔天的 P0S1（Phase 0, Slot 1）。
    /// </summary>
    /// <param name="currentDay">當前遊戲天數</param>
    /// <param name="shopService">統一處理商品入庫與購得效果的服務</param>
    public void ProcessDeliveries(int currentDay, ShopService shopService)
    {
        if (_isProcessing || shopService == null) return;
        bool anyDelivered = false;
        _isProcessing = true;
        try
        {
            // 使用快照，避免入庫事件的回呼改變待配送清單。
            var orders = _pendingOrders.ToArray();
            foreach (var order in orders)
            {
                if (order == null || currentDay < order.DeliveryDay || !_pendingOrders.Contains(order)) continue;
                if (!shopService.TryDeliverOrder(order)) continue;
                _pendingOrders.Remove(order);
                anyDelivered = true;
                Debug.Log($"[PendingDeliveryModel] 到貨入庫：{order.ItemID} x{order.Amount}");
            }
        }
        finally { _isProcessing = false; }

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
