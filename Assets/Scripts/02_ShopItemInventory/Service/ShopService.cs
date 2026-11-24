using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>購買、到貨入庫與購得效果的執行入口；一般 Inventory.AddItem 不觸發效果。</summary>
public class ShopService
{
    private readonly ProtagonistStatusModel _protagonist;
    private readonly ProtagonistInventoryModel _inventory;
    private readonly ShopStatusModel _shopStatus;
    private readonly PendingDeliveryModel _pendingDelivery;
    private readonly ItemCatalog _catalog;
    private readonly EffectContext _effectContext;
    private readonly Func<int> _getCurrentDay;
    private bool _isProcessing;

    public ShopService(ProtagonistStatusModel protagonist, ProtagonistInventoryModel inventory,
        ShopStatusModel shopStatus, PendingDeliveryModel pendingDelivery, ItemCatalog catalog,
        ProgressFlagModel progressFlags, Func<int> getCurrentDay)
    {
        _protagonist = protagonist;
        _inventory = inventory;
        _shopStatus = shopStatus;
        _pendingDelivery = pendingDelivery;
        _catalog = catalog;
        _getCurrentDay = getCurrentDay;
        _effectContext = new EffectContext
        {
            Protagonist = protagonist,
            Inventory = inventory,
            ProgressFlags = progressFlags
        };
    }

    /// <summary>購買一份；扣一次價格、限購計一次，網購在付款時只建立訂單。</summary>
    public PurchaseResult TryPurchaseItem(string itemID)
    {
        // 防止數量／金錢事件的同步回呼重入尚未完成的交易。
        if (_isProcessing) return PurchaseResult.PurchaseInProgress;
        var config = _catalog.GetItemConfig(itemID);
        if (config == null) return PurchaseResult.ItemNotFound;
        if (config.Price < 0 || config.PurchaseQuantity <= 0)
            return InvalidConfiguration(config, "價格不得為負數，每份數量必須大於 0。");
        if (_protagonist.Money < config.Price) return PurchaseResult.NotEnoughCoins;
        if (config.MaxPurchaseLimit > 0 &&
            _shopStatus.GetPurchasedCount(itemID) >= config.MaxPurchaseLimit)
            return PurchaseResult.PurchaseLimitReached;

        var effects = GetEffects(config);
        if (!ValidateReceipt(config, config.PurchaseQuantity, 1, effects, !config.IsOnlineOrder))
            return PurchaseResult.InvalidConfiguration;

        _isProcessing = true;
        try
        {
            if (!_protagonist.TryReduceMoney(config.Price)) return PurchaseResult.NotEnoughCoins;

            // 先記錄購買份數，讓後續入庫／效果事件看到正確的限購狀態。
            _shopStatus.IncreasePurchasedCount(itemID, 1);
            if (config.IsOnlineOrder)
                _pendingDelivery.PlaceOrder(itemID, config.PurchaseQuantity, _getCurrentDay(), 1);
            else
                Receive(config, config.PurchaseQuantity, 1, effects);

            Debug.Log($"[ShopService] 購買 {config.DisplayName} 一份，共 {config.PurchaseQuantity} 個。" +
                      (config.IsOnlineOrder ? "（隔天到貨）" : ""));
            return PurchaseResult.Success;
        }
        finally { _isProcessing = false; }
    }

    /// <summary>配送入口：依訂單保存的數量入庫，效果每份一次，不再扣款或增加限購次數。</summary>
    public bool TryDeliverOrder(DeliveryOrder order)
    {
        if (_isProcessing || order == null) return false;
        var config = _catalog.GetItemConfig(order.ItemID);
        if (config == null)
        {
            Debug.LogWarning($"[ShopService] 到貨商品不存在：{order.ItemID}，保留訂單待設定修復。");
            return false;
        }

        // 舊存檔未記錄份數：舊版每份一個，沿用原數量，不乘上新的 PurchaseQuantity。
        int purchaseCount = order.PurchaseCount > 0 ? order.PurchaseCount : order.Amount;
        var effects = GetEffects(config);
        if (!ValidateReceipt(config, order.Amount, purchaseCount, effects, true)) return false;

        _isProcessing = true;
        try
        {
            Receive(config, order.Amount, purchaseCount, effects);
            return true;
        }
        finally { _isProcessing = false; }
    }

    private static ItemEffect[] GetEffects(ItemConfigData config) =>
        config.AcquireEffects?.ToArray() ?? Array.Empty<ItemEffect>();

    private bool ValidateReceipt(ItemConfigData config, int amount, int purchaseCount,
        ItemEffect[] effects, bool checkInventoryCapacity)
    {
        if (amount <= 0 || purchaseCount <= 0)
        {
            InvalidConfiguration(config, "入庫數量與購買份數必須大於 0。");
            return false;
        }

        var additions = new Dictionary<string, long> { [config.ItemID] = amount };
        foreach (var effect in effects)
        {
            if (effect == null) continue;
            if (effect.IsRetired || effect.Target == EffectTarget.Heroine)
            {
                InvalidConfiguration(config, $"購得效果含已退役或需要送禮目標的效果：{effect.GetType().Name}。");
                return false;
            }
            if (effect is GrantItemEffect grant)
            {
                if (grant.Item == null || grant.Amount <= 0 ||
                    _catalog.GetItemConfig(grant.Item.ItemID) != grant.Item)
                {
                    InvalidConfiguration(config, "額外道具需指定目錄內的道具，且數量必須大於 0。");
                    return false;
                }
                additions.TryGetValue(grant.Item.ItemID, out long previous);
                additions[grant.Item.ItemID] = previous + (long)grant.Amount * purchaseCount;
            }
        }

        foreach (var pair in additions)
        {
            long current = checkInventoryCapacity ? _inventory.GetItemCount(pair.Key) : 0;
            if (current + pair.Value > int.MaxValue)
            {
                InvalidConfiguration(config, $"道具 {pair.Key} 的入庫數量超出可儲存範圍。");
                return false;
            }
        }
        return true;
    }

    private void Receive(ItemConfigData config, int amount, int purchaseCount, ItemEffect[] effects)
    {
        _inventory.AddItem(config.ItemID, amount);
        for (int purchase = 0; purchase < purchaseCount; purchase++)
        {
            foreach (var effect in effects)
                effect?.Apply(_effectContext);
        }
    }

    private static PurchaseResult InvalidConfiguration(ItemConfigData config, string reason)
    {
        Debug.LogError($"[ShopService] 商品 {config.ItemID} 設定無效：{reason}");
        return PurchaseResult.InvalidConfiguration;
    }
}

public enum PurchaseResult
{
    Success,
    ItemNotFound,
    NotEnoughCoins,
    PurchaseLimitReached,
    InvalidConfiguration,
    PurchaseInProgress
}
