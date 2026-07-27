using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 負責道具使用/送禮的核心 Service。
/// 處理流程：驗證目標 → 檢查條件 → 執行效果。
/// 
/// 【整合說明】
/// 此 Service 取代舊的 ItemEffectService。
/// 在 GameStatusService 中將 ItemEffectService 替換為 ItemUseService 即可。
/// </summary>
public class ItemUseService
{
    private readonly ItemCatalog itemCatalog;
    private readonly GameStatusService gameStatus;

    // ==========================================================
    // Localization Keys（對應 Text Table 的 Field Name）
    // ==========================================================
    private const string KEY_ITEM_NOT_FOUND = "USE_FAIL_ITEM_NOT_FOUND";
    private const string KEY_HEROINE_ONLY = "USE_FAIL_HEROINE_ONLY";
    private const string KEY_NO_SELF_EFFECT = "USE_FAIL_NO_SELF_EFFECT";
    private const string KEY_CONDITION_DEFAULT = "USE_FAIL_CONDITION_DEFAULT";

    public ItemUseService(ItemCatalog itemCatalog, GameStatusService gameStatus)
    {
        this.itemCatalog = itemCatalog;
        this.gameStatus = gameStatus;
    }

    // ==========================================================
    // 自用道具
    // ==========================================================

    /// <summary>
    /// 主角對自己使用道具。
    /// 回傳 UseItemResult，包含成功/失敗 以及失敗時的提示訊息 Key。
    /// </summary>
    public UseItemResult UseItemOnSelf(string itemID)
    {
        var config = itemCatalog.GetItemConfig(itemID);
        if (config == null)
        {
            Debug.LogError($"[ItemUseService] 找不到道具: {itemID}");
            return UseItemResult.Failed(KEY_ITEM_NOT_FOUND);
        }

        // 檢查 TargetingRule 是否允許自用
        if (config.TargetingRule.Category == TargetCategory.HeroineOnly)
        {
            Debug.LogWarning($"[ItemUseService] 道具 {itemID} 僅限送禮，不可自用");
            return UseItemResult.Failed(KEY_HEROINE_ONLY);
        }

        if (!config.CanUseSelf)
        {
            Debug.LogWarning($"[ItemUseService] 道具 {itemID} 沒有自用效果");
            return UseItemResult.Failed(KEY_NO_SELF_EFFECT);
        }

        // ── UseCondition 條件檢查 ──
        if (config.UseConditions != null)
        {
            foreach (var condition in config.UseConditions)
            {
                if (condition != null && !condition.IsMet(gameStatus.Protagonist))
                {
                    string failKey = !string.IsNullOrEmpty(condition.FailMessageKey)
                        ? condition.FailMessageKey
                        : KEY_CONDITION_DEFAULT;

                    Debug.Log($"[ItemUseService] 道具 {itemID} 使用條件不符: {failKey}");
                    return UseItemResult.Failed(failKey);
                }
            }
        }

        // 建立上下文（自用：Heroine 為 null）
        var ctx = new EffectContext
        {
            Protagonist = gameStatus.Protagonist,
            Heroine = null
        };

        // 執行所有自用效果 + 收集報告
        var records = new List<ValueChangeRecord>();

        foreach (var effect in config.SelfEffects)
        {
            if (effect != null)
            {
                effect.Apply(ctx);

                var record = effect.ReportChange(ctx);
                if (record.HasValue)
                    records.Add(record.Value);
            }
        }

        // 自動報告
        if (records.Count > 0)
        {
            ValueChangeReporter.Report(records);
        }

        return UseItemResult.Succeed;
    }

    /// <summary>
    /// 【給 UI 用】預先檢查自用道具是否能使用，不實際執行效果。
    /// </summary>
    public UseItemResult PreviewUseItemOnSelf(string itemID)
    {
        var config = itemCatalog.GetItemConfig(itemID);
        if (config == null) return UseItemResult.Failed("");
        if (!config.CanUseSelf) return UseItemResult.Failed("");

        if (config.UseConditions != null)
        {
            foreach (var condition in config.UseConditions)
            {
                if (condition != null && !condition.IsMet(gameStatus.Protagonist))
                {
                    string failKey = !string.IsNullOrEmpty(condition.FailMessageKey)
                        ? condition.FailMessageKey
                        : KEY_CONDITION_DEFAULT;
                    return UseItemResult.Failed(failKey);
                }
            }
        }

        return UseItemResult.Succeed;
    }


    // ==========================================================
    // 送禮
    // ==========================================================

    /// <summary>
    /// 送禮給指定女主角。
    /// 回傳 GiftResult，包含成功/失敗 以及失敗時的 Dialogue System 對話 Title。
    /// </summary>
    public GiftResult GiveGift(string itemID, string heroineID)
    {
        var config = itemCatalog.GetItemConfig(itemID);
        if (config == null)
        {
            Debug.LogError($"[ItemUseService] 找不到道具: {itemID}");
            return GiftResult.InvalidTarget;
        }

        // ── 第 1 層：TargetingRule 驗證 ──
        if (!IsValidGiftTarget(config.TargetingRule, heroineID))
        {
            Debug.LogWarning($"[ItemUseService] 道具 {itemID} 不能送給 {heroineID}（TargetingRule 不允許）");
            return GiftResult.InvalidTarget;
        }

        // ── 取得女主角 Model ──
        if (!gameStatus.Heroines.TryGetValue(heroineID, out var heroine))
        {
            Debug.LogError($"[ItemUseService] 找不到女主角: {heroineID}");
            return GiftResult.InvalidTarget;
        }

        // ── 第 2 層：GiftCondition 條件檢查 ──
        var conditions = config.GetGiftConditionsFor(heroineID);
        foreach (var condition in conditions)
        {
            if (condition != null && !condition.IsMet(heroine))
            {
                string rejectTitle = !string.IsNullOrEmpty(condition.RejectConversationTitle)
                    ? condition.RejectConversationTitle
                    : "Chat/System/Gift_Rejected";

                Debug.Log($"[ItemUseService] 道具 {itemID} 送給 {heroineID} 被拒收: {rejectTitle}");
                return GiftResult.Rejected(rejectTitle);
            }
        }

        // ── 第 3 層：執行效果 + 收集報告 ──
        var ctx = new EffectContext
        {
            Protagonist = gameStatus.Protagonist,
            Heroine = heroine
        };

        var records = new List<ValueChangeRecord>();

        var effects = config.GetGiftEffectsFor(heroineID);
        foreach (var effect in effects)
        {
            if (effect != null)
            {
                effect.Apply(ctx);

                var record = effect.ReportChange(ctx);
                if (record.HasValue)
                    records.Add(record.Value);
            }
        }

        // ★ 自動報告數值變動
        if (records.Count > 0)
        {
            ValueChangeReporter.Report(records);
        }

        return GiftResult.Success;
    }


    // ==========================================================
    // 輔助方法
    // ==========================================================

    /// <summary>
    /// 檢查某位女主角是否為合法的送禮對象。
    /// </summary>
    private bool IsValidGiftTarget(ItemTargetingRule rule, string heroineID)
    {
        if (rule.Category == TargetCategory.ProtagonistOnly ||
            rule.Category == TargetCategory.NoBody)
        {
            return false;
        }

        if (rule.AllowedHeroineIDs != null && rule.AllowedHeroineIDs.Count > 0)
        {
            return rule.AllowedHeroineIDs.Contains(heroineID);
        }

        return true;
    }

    /// <summary>
    /// 【給 UI 用】預先檢查送禮是否會被拒收，不實際執行效果。
    /// </summary>
    public GiftResult PreviewGiftResult(string itemID, string heroineID)
    {
        var config = itemCatalog.GetItemConfig(itemID);
        if (config == null) return GiftResult.InvalidTarget;

        if (!IsValidGiftTarget(config.TargetingRule, heroineID))
            return GiftResult.InvalidTarget;

        if (!gameStatus.Heroines.TryGetValue(heroineID, out var heroine))
            return GiftResult.InvalidTarget;

        var conditions = config.GetGiftConditionsFor(heroineID);
        foreach (var condition in conditions)
        {
            if (condition != null && !condition.IsMet(heroine))
            {
                string rejectTitle = !string.IsNullOrEmpty(condition.RejectConversationTitle)
                    ? condition.RejectConversationTitle
                    : "Chat/System/Gift_Rejected";

                return GiftResult.Rejected(rejectTitle);
            }
        }

        return GiftResult.Success;
    }
}