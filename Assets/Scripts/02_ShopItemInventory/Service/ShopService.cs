using UnityEngine;

// 職責：處理所有與「購買商品」相關的核心業務邏輯。
// 它不關心 UI，只關心規則。
public class ShopService
{
    // 依賴的數據模型 (從 GameStatusService 傳入)
    private readonly ProtagonistStatusModel _protagonistStatusModel;
    private readonly ProtagonistInventoryModel _inventoryModel;
    private readonly ShopStatusModel _shopStatusModel;
    private readonly PendingDeliveryModel _pendingDeliveryModel;

    // 道具設定來源 (從 GameStatusService 傳入)：查 ID → ItemConfigData
    private readonly ItemCatalog _itemCatalog;

    // 用於取得當前天數（下單時需要）
    private readonly System.Func<int> _getCurrentDay;

    /// <summary>
    /// 建構函式，用來注入所有必要的依賴。
    /// </summary>
    public ShopService(
        ProtagonistStatusModel protagonistStatus,
        ProtagonistInventoryModel inventory,
        ShopStatusModel shopStatus,
        PendingDeliveryModel pendingDelivery,
        ItemCatalog itemCatalog,
        System.Func<int> getCurrentDay)
    {
        _protagonistStatusModel = protagonistStatus;
        _inventoryModel = inventory;
        _shopStatusModel = shopStatus;
        _pendingDeliveryModel = pendingDelivery;
        _itemCatalog = itemCatalog;
        _getCurrentDay = getCurrentDay;
    }

    /// <summary>
    /// 嘗試購買一個商品的核心方法。
    /// </summary>
    public PurchaseResult TryPurchaseItem(string itemID)
    {
        // 1. 驗證：商品是否存在？
        ItemConfigData itemConfig = _itemCatalog.GetItemConfig(itemID);
        if (itemConfig == null)
        {
            Debug.LogError($"[ShopService] 嘗試購買一個不存在的商品 ID: {itemID}");
            return PurchaseResult.ItemNotFound;
        }

        // 2. 規則檢查：金錢是否足夠？
        if (_protagonistStatusModel.Money < itemConfig.Price)
        {
            Debug.Log($"[ShopService] 金錢不足，無法購買 {itemConfig.DisplayName}。");
            return PurchaseResult.NotEnoughCoins;
        }

        // 3. 規則檢查：是否達到購買上限？
        if (itemConfig.MaxPurchaseLimit > 0)
        {
            int purchasedCount = _shopStatusModel.GetPurchasedCount(itemID);
            if (purchasedCount >= itemConfig.MaxPurchaseLimit)
            {
                Debug.Log($"[ShopService] 商品 {itemConfig.DisplayName} 已達購買上限。");
                return PurchaseResult.PurchaseLimitReached;
            }
        }

        // --- 執行購買流程 ---

        // 4. 扣除金錢
        if (!_protagonistStatusModel.TryReduceMoney(itemConfig.Price))
        {
            return PurchaseResult.NotEnoughCoins;
        }

        // 5. 入庫或下單
        if (itemConfig.IsOnlineOrder)
        {
            // 網購模式：不直接入庫，改為下訂單，隔天 P0S1 到貨
            _pendingDeliveryModel.PlaceOrder(itemID, 1, _getCurrentDay());
        }
        else
        {
            // 現買現拿：直接加入背包
            _inventoryModel.AddItem(itemID, 1);
        }

        // 6. 檢查是否為「無人使用的道具」(NoBody)，如果是，則直接套用被動效果
        //    ★ 注意：網購商品的 NoBody 被動效果在到貨入庫時「不會」自動觸發，
        //       因為被動效果語義上屬於「獲取當下」。
        //       如果未來需要到貨時也觸發，可在 PendingDeliveryModel.ProcessDeliveries 中擴充。
        if (!itemConfig.IsOnlineOrder &&
            itemConfig.TargetingRule != null &&
            itemConfig.TargetingRule.Category == TargetCategory.NoBody)
        {
            ApplyPassiveEffectsOnAcquire(itemConfig);
        }

        // 7. 更新商店的已購買次數
        _shopStatusModel.IncreasePurchasedCount(itemID, 1);

        Debug.Log($"[ShopService] 成功購買 {itemConfig.DisplayName}！" +
                  (itemConfig.IsOnlineOrder ? "（網購，隔天到貨）" : ""));
        return PurchaseResult.Success;
    }

    /// <summary>
    /// 處理 NoBody 類型道具在獲取時觸發的被動效果。
    /// 使用新的 ItemEffect + EffectContext 系統，
    /// NoBody 道具的 SelfEffects 會在購買時自動對主角執行。
    /// </summary>
    private void ApplyPassiveEffectsOnAcquire(ItemConfigData itemConfig)
    {
        if (itemConfig.SelfEffects == null || itemConfig.SelfEffects.Count == 0)
        {
            Debug.Log($"[ShopService] 道具 [{itemConfig.DisplayName}] 為 NoBody 類型但沒有 SelfEffects，跳過被動效果。");
            return;
        }

        Debug.Log($"[ShopService] 套用道具 [{itemConfig.DisplayName}] 的被動效果...");

        // 建立 EffectContext（NoBody 道具只對主角生效，Heroine 為 null）
        var ctx = new EffectContext
        {
            Protagonist = _protagonistStatusModel,
            Heroine = null
        };

        foreach (var effect in itemConfig.SelfEffects)
        {
            if (effect != null)
            {
                effect.Apply(ctx);
            }
        }
    }
}

/// <summary>
/// 定義購買操作可能的所有結果。
/// </summary>
public enum PurchaseResult
{
    Success,
    ItemNotFound,
    NotEnoughCoins,
    PurchaseLimitReached
}