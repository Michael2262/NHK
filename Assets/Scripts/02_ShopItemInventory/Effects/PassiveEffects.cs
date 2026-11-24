using UnityEngine;

// ==========================================================
// 購得效果：實際收到商品時增加或扣除資源
// 現役：Money / SkillPoints；舊四維列舉編號保留但不再執行。
// 帶正負號增加或扣除資源。
// ==========================================================
[System.Serializable]
public class PermanentStatBoostEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Protagonist;

    public enum BoostStatType
    {
        [UnityEngine.InspectorName("已退役：Stress")] Stress,
        [UnityEngine.InspectorName("已退役：LifePower")] LifePower,
        [UnityEngine.InspectorName("已退役：Sociality")] Sociality,
        [UnityEngine.InspectorName("已退役：Dependency")] Dependency,
        Money,
        SkillPoints
    }

    public BoostStatType StatToBoost = BoostStatType.Money;
    public int Amount = 10;

    public override bool IsRetired => StatToBoost != BoostStatType.Money && StatToBoost != BoostStatType.SkillPoints;

    public override void Apply(EffectContext ctx)
    {
        switch (StatToBoost)
        {

            case BoostStatType.Money:
                ctx.Protagonist.AddMoney(Amount);
                break;
            case BoostStatType.SkillPoints:
                ctx.Protagonist.AddSkillPoints(Amount);
                break;
        }
    }

    public override ValueChangeRecord? ReportChange(EffectContext ctx)
    {
        if (StatToBoost != BoostStatType.Money && StatToBoost != BoostStatType.SkillPoints) return null;
        return new ValueChangeRecord
        {
            isHeroineResource = false,
            resourceTypeKey = StatToBoost.ToString(),
            finalAmount = Amount,
            effectResult = ValueChangeResult.EffectResult.Normal,
            heroineNameKey = ""
        };
    }
}

// ==========================================================
// 被動效果：啟動 Flag
// ==========================================================
[System.Serializable]
public class ActivateFlagEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Protagonist;
    [Tooltip("要啟動的 Flag 定義檔 (ScriptableObject)")]
    public ProgressFlagDefinition FlagToActivate;

    [Tooltip("Flag 類型：Persistent = 永久, Day = 每日重設, Phase = 每階段重設")]
    public FlagDurationType Duration = FlagDurationType.Persistent;

    public override void Apply(EffectContext ctx)
    {
        if (FlagToActivate == null)
        {
            Debug.LogWarning("[ActivateFlagEffect] FlagToActivate 未指定，跳過");
            return;
        }

        var progressFlags = ctx.ProgressFlags;

        switch (Duration)
        {
            case FlagDurationType.Persistent:
                progressFlags.AddPersistentFlag(FlagToActivate.FlagID);
                Debug.Log($"[ActivateFlagEffect] 啟動永久 Flag '{FlagToActivate.FlagID}'");
                break;
            case FlagDurationType.Day:
                progressFlags.AddDailyFlag(FlagToActivate.FlagID);
                Debug.Log($"[ActivateFlagEffect] 啟動每日 Flag '{FlagToActivate.FlagID}'");
                break;
            case FlagDurationType.Phase:
                progressFlags.AddPhaseFlag(FlagToActivate.FlagID);
                Debug.Log($"[ActivateFlagEffect] 啟動階段 Flag '{FlagToActivate.FlagID}'");
                break;
        }
    }
}

public enum FlagDurationType
{
    Persistent,
    Day,
    Phase
}

/// <summary>每份商品額外給予指定數量的道具；不執行該道具的購得效果。</summary>
[System.Serializable]
public class GrantItemEffect : ItemEffect
{
    [Tooltip("額外給予的道具，必須位於 Resources/SHOP/Item。")]
    public ItemConfigData Item;

    [Tooltip("每份商品額外給予幾個此道具。商品本身的數量請設定 PurchaseQuantity。")]
    [Min(1)] public int Amount = 1;

    public override void Apply(EffectContext ctx)
    {
        if (Item == null || Amount <= 0 || ctx.Inventory == null) return;
        ctx.Inventory.AddItem(Item.ItemID, Amount);
    }
}
