using UnityEngine;

// 職責：處理所有與「購買商品」相關的核心業務邏輯。
// 它不關心 UI，只關心規則。
public class ShopService
{
    // 依賴的數據模型 (從 GameStatusService 傳入)
    private readonly ProtagonistStatusModel _protagonistStatusModel;
    private readonly ProtagonistInventoryModel _inventoryModel;
    private readonly ShopStatusModel _shopStatusModel;

    // 依賴的配置檔案 (從 GameStatusService 傳入)
    private readonly ItemDatabase _itemDatabase;

    /// <summary>
    /// 建構函式，用來注入所有必要的依賴。
    /// </summary>
    public ShopService(
        ProtagonistStatusModel protagonistStatus,
        ProtagonistInventoryModel inventory,
        ShopStatusModel shopStatus,
        ItemDatabase itemDatabase)
    {
        _protagonistStatusModel = protagonistStatus;
        _inventoryModel = inventory;
        _shopStatusModel = shopStatus;
        _itemDatabase = itemDatabase;
    }

    /// <summary>
    /// 嘗試購買一個商品的核心方法。
    /// </summary>
    public PurchaseResult TryPurchaseItem(string itemID)
    {
        // 1. 驗證：商品是否存在？
        ItemConfigData itemConfig = _itemDatabase.GetItemConfig(itemID);
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

        // 5. 增加道具到背包
        _inventoryModel.AddItem(itemID, 1);

        // 6. 檢查是否為「無人使用的道具」(NoBody)，如果是，則直接套用被動效果
        if (itemConfig.TargetingRule != null && itemConfig.TargetingRule.Category == TargetCategory.NoBody)
        {
            ApplyPassiveEffectsOnAcquire(itemConfig);
        }

        // 7. 更新商店的已購買次數
        _shopStatusModel.IncreasePurchasedCount(itemID, 1);

        Debug.Log($"[ShopService] 成功購買 {itemConfig.DisplayName}！");
        return PurchaseResult.Success;
    }

    // ▼▼▼【★ 修改：改用新的效果系統 ★】▼▼▼
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
    // ▲▲▲【修改結束】▲▲▲
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