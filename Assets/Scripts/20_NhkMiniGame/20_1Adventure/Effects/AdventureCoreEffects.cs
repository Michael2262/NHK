using System;
using UnityEngine;

// ==========================================================
// 大冒險核心效果：主角數值 / 里程 / 道具
// 全部用「帶正負號的 Amount」，正=增加、負=減少，方便一個效果搞定 ±。
// ==========================================================

/// <summary>壓力：正=加壓力，負=減壓力。</summary>
[Serializable]
public class AdvStressEffect : AdventureEffect
{
    [Tooltip("正=加壓力，負=減壓力")]
    public int Amount = 10;

    public override void Apply(AdventureContext ctx)
    {
        if (ctx.Protagonist == null) return;
        if (Amount >= 0) ctx.Protagonist.AddStress(Amount);
        else ctx.Protagonist.ReduceStress(-Amount);
    }

    public override AdventureChangeRecord? ReportChange(AdventureContext ctx)
        => new AdventureChangeRecord("Stress", Amount);
}

/// <summary>社會性：正=增加，負=減少。</summary>
[Serializable]
public class AdvSocialityEffect : AdventureEffect
{
    [Tooltip("正=增加社會性，負=減少")]
    public int Amount = 5;

    public override void Apply(AdventureContext ctx)
    {
        if (ctx.Protagonist == null) return;
        if (Amount >= 0) ctx.Protagonist.AddSociality(Amount);
        else ctx.Protagonist.ReduceSociality(-Amount);
    }

    public override AdventureChangeRecord? ReportChange(AdventureContext ctx)
        => new AdventureChangeRecord("Sociality", Amount);
}

/// <summary>生活力：正=增加，負=減少。</summary>
[Serializable]
public class AdvLifePowerEffect : AdventureEffect
{
    [Tooltip("正=增加生活力，負=減少")]
    public int Amount = 5;

    public override void Apply(AdventureContext ctx)
    {
        if (ctx.Protagonist == null) return;
        if (Amount >= 0) ctx.Protagonist.AddLifePower(Amount);
        else ctx.Protagonist.ReduceLifePower(-Amount);
    }

    public override AdventureChangeRecord? ReportChange(AdventureContext ctx)
        => new AdventureChangeRecord("LifePower", Amount);
}

/// <summary>金錢：正=獲得，負=失去。</summary>
[Serializable]
public class AdvMoneyEffect : AdventureEffect
{
    [Tooltip("正=獲得金錢，負=失去")]
    public int Amount = 100;

    public override void Apply(AdventureContext ctx)
    {
        if (ctx.Protagonist == null) return;
        ctx.Protagonist.AddMoney(Amount);
    }

    public override AdventureChangeRecord? ReportChange(AdventureContext ctx)
        => new AdventureChangeRecord("Money", Amount);
}

/// <summary>
/// 里程增減。掛在成功效果清單 = 成功推進；掛在失敗效果清單 = 失敗才變動。
/// Delta 可為 +2 / +1 / 0 / -1 …
/// </summary>
[Serializable]
public class AdvMileageEffect : AdventureEffect
{
    [Tooltip("里程變化量，可為負數")]
    public int Delta = 1;

    public override void Apply(AdventureContext ctx)
    {
        ctx.Run?.AddMileage(Delta);
    }

    public override AdventureChangeRecord? ReportChange(AdventureContext ctx)
        => new AdventureChangeRecord("Mileage", Delta);
}

/// <summary>獲得道具（塞進主角背包）。</summary>
[Serializable]
public class AdvGiveItemEffect : AdventureEffect
{
    [Tooltip("要給的道具設定檔")]
    public ItemConfigData Item;

    [Tooltip("數量")]
    public int Count = 1;

    public override void Apply(AdventureContext ctx)
    {
        if (Item == null || ctx.Inventory == null) return;
        ctx.Inventory.AddItem(Item.ItemID, Count);
    }

    public override AdventureChangeRecord? ReportChange(AdventureContext ctx)
        => Item == null ? (AdventureChangeRecord?)null
                        : new AdventureChangeRecord(Item.DisplayNameKey, Count);
}
