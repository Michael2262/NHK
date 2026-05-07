using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// 定義道具可以指向的目標大分類
/// </summary>
public enum TargetCategory
{
    ProtagonistOnly, // 僅限主角
    HeroineOnly,     // 僅限女主角
    AnyCharacter,    // 主角或女主角
    NoBody           // 無特定目標 (例如材料、任務道具等)
}

/// <summary>
/// 一個可序列化的類別,用來封裝所有道具的目標規則
/// </summary>
[System.Serializable]
public class ItemTargetingRule
{
    [Tooltip("此道具可用的目標大分類")]
    public TargetCategory Category = TargetCategory.AnyCharacter;

    [Tooltip("如果 Category 包含女主角,用這個列表來限制「特定」女主角的 ID。\n" +
             "如果這個列表為空,則代表「所有」女主角都可用。")]
    public List<string> AllowedHeroineIDs = new List<string>();
}


/// <summary>
/// 收禮反應資料:收下禮物後要觸發的對話 + UnityEvent。
/// 同時用於「女主角專屬反應」、「物品預設反應」、「全域 Fallback 反應」三層。
/// </summary>
[System.Serializable]
public class GiftReaction
{
    [Header("反應 1:觸發對話")]
    [Tooltip("要觸發的 Pixel Crushers 對話標題(可選)")]
    public string DialogueConversationTitle;

    [Header("反應 2:觸發 Unity Event")]
    [Tooltip("要觸發的 Unity Event(可選)")]
    public UnityEvent ReactionEvent;

    /// <summary>
    /// 是否有任何反應內容(至少有對話或 Event 其一)。
    /// 都沒填時視為「未設定」,會 fallback 到上層。
    /// </summary>
    public bool HasAnyReaction =>
        !string.IsNullOrEmpty(DialogueConversationTitle) ||
        (ReactionEvent != null && ReactionEvent.GetPersistentEventCount() > 0);
}


/// <summary>
/// 道具的核心定義資料。
/// 只定義道具本身的屬性(名稱、價格、效果等),
/// 不包含任何商店相關的資訊(排序、解鎖條件等由 ItemDatabase 管理)。
/// </summary>
[CreateAssetMenu(menuName = "Game/Shop/Item Config")]
public class ItemConfigData : ScriptableObject
{
    // 所有 SO 都有一個 name,可以用作 ItemID
    public string ItemID => name;

    [Header("基礎屬性")]
    [Tooltip("變成只是給自己看的說明標題")]
    public string DisplayName = "道具名";
    [Tooltip("變成只是給自己看的道具敘述")]
    public string Description = "道具說明";

    [Tooltip("多語系 Text Table 中的 Key (Field Name)")]
    public string DisplayNameKey = "ITEM_NAME_DEFAULT";
    [Tooltip("多語系 Text Table 中的 Key (Field Name)")]
    public string DescriptionKey = "ITEM_DESC_DEFAULT";

    public int Price = 100;
    public Sprite Icon;

    [Header("購買與限制")]
    [Tooltip("此商品永久/每日/每週的最大購買上限。0 = 無限制。")]
    public int MaxPurchaseLimit = 0;
    public PurchaseFrequency ResetFrequency = PurchaseFrequency.None;

    [Header("配送模式")]
    [Tooltip("勾選後為網購模式，商品隔天 P0S1 到貨，而非現買現拿。")]
    public bool IsOnlineOrder;

    [Header("Targeting")]
    [Tooltip("定義這個道具可以對誰使用")]
    public ItemTargetingRule TargetingRule;

    // ==========================================================
    // 自用條件 — 使用 [SerializeReference] 內嵌
    // ==========================================================

    [Header("條件 - 自用(對主角)")]
    [Tooltip("主角使用此道具前的條件檢查(全部通過才能使用)。\n" +
             "留空 = 無條件限制。\n" +
             "例如:精力劑需要檢查剩餘次數。")]
    [SerializeReference] public List<UseCondition> UseConditions = new List<UseCondition>();

    // ==========================================================
    // 效果數據(三層結構)— 使用 [SerializeReference] 內嵌
    // ==========================================================

    [Header("效果 - 自用(對主角)")]
    [Tooltip("主角對自己使用此道具時觸發的效果列表。\n" +
             "留空 = 此道具不可自用。")]
    [SerializeReference] public List<ItemEffect> SelfEffects = new List<ItemEffect>();

    [Header("效果 - 送禮(預設)")]
    [Tooltip("送禮給女主角時的預設效果。\n" +
             "如果該女主角在 HeroineOverrides 中有專屬設定,則此列表會被覆寫。\n" +
             "留空 = 此道具不可送禮(除非有 Override)。")]
    [SerializeReference] public List<ItemEffect> DefaultGiftEffects = new List<ItemEffect>();

    [Header("反應 - 送禮(預設)")]
    [Tooltip("收下禮物後的預設反應(對話 + UnityEvent)。\n" +
             "如果該女主角在 HeroineOverrides 有填寫 Reaction,則會被覆寫。\n" +
             "此處也留空 = 會 fallback 到 HeroineReactionManager 的全域反應。")]
    public GiftReaction DefaultGiftReaction = new GiftReaction();

    [Header("送禮 - 女主角專屬覆寫")]
    [Tooltip("針對特定女主角的專屬條件 + 效果 + 反應。\n" +
             "有配置的女主角會使用此處的設定,完全取代 Default。")]
    public List<HeroineGiftOverride> HeroineOverrides = new List<HeroineGiftOverride>();


    // ==========================================================
    // 輔助方法
    // ==========================================================

    public List<ItemEffect> GetGiftEffectsFor(string heroineID)
    {
        var heroineOverride = HeroineOverrides.Find(o => o.HeroineID == heroineID);
        return heroineOverride != null ? heroineOverride.Effects : DefaultGiftEffects;
    }

    public List<GiftCondition> GetGiftConditionsFor(string heroineID)
    {
        var heroineOverride = HeroineOverrides.Find(o => o.HeroineID == heroineID);
        return heroineOverride?.Conditions ?? new List<GiftCondition>();
    }

    /// <summary>
    /// 取得指定女主角收到此禮物時的反應。
    /// 查詢順序:
    ///   1. HeroineOverride.Reaction(有實際內容才採用)
    ///   2. DefaultGiftReaction(有實際內容才採用)
    ///   3. null(由 HeroineReactionManager 進一步 fallback 到全域)
    /// </summary>
    public GiftReaction GetGiftReactionFor(string heroineID)
    {
        var heroineOverride = HeroineOverrides.Find(o => o.HeroineID == heroineID);

        if (heroineOverride != null &&
            heroineOverride.Reaction != null &&
            heroineOverride.Reaction.HasAnyReaction)
        {
            return heroineOverride.Reaction;
        }

        if (DefaultGiftReaction != null && DefaultGiftReaction.HasAnyReaction)
        {
            return DefaultGiftReaction;
        }

        return null;
    }

    public bool CanUseSelf => SelfEffects != null && SelfEffects.Count > 0;

    public bool CanGift =>
        (DefaultGiftEffects != null && DefaultGiftEffects.Count > 0) ||
        (HeroineOverrides != null && HeroineOverrides.Exists(o => o.Effects.Count > 0));

    // ==========================================================
    // Editor 自動載入 Icon
    // ==========================================================

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Icon != null) return;

        string path = $"SHOP/ItemImage/Item_{ItemID}";
        var found = Resources.Load<Sprite>(path);

        if (found != null)
        {
            Icon = found;
        }
        else
        {
            Debug.LogWarning($"[ItemConfigData] 找不到 Icon: Resources/{path}  (ItemID={ItemID})");
        }
    }
#endif
}

public enum PurchaseFrequency { None, Daily, Weekly }