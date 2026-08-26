using System;
using System.Collections.Generic;
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
/// 行動次數增減。預設 -1（消耗一次行動），也可填正數補充。
/// 次數不會低於 0；歸 0 不會自動結束，由外部決定反應。
/// </summary>
[Serializable]
public class AdvMovesEffect : AdventureEffect
{
    [Tooltip("行動次數變化量，預設 -1（消耗一次）。可填正數補充")]
    public int Delta = -1;

    public override void Apply(AdventureContext ctx)
    {
        ctx.Run?.AddMoves(Delta);
    }

    public override AdventureChangeRecord? ReportChange(AdventureContext ctx)
        => new AdventureChangeRecord("Moves", Delta);
}

/// <summary>
/// 以 ID 調用 StatChangePackageDatabase 的數值變化套組（透過 StatChangeService 執行）。
/// 一個套組可含多筆變化，會全部回報給結果的變動清單。
/// </summary>
[Serializable]
public class AdvStatPackageEffect : AdventureEffect
{
    [Tooltip("StatChangePackageDatabase 裡的套組 ID")]
    public string PackageID;

    [Tooltip("套組若含女主角數值（Libido / Trust）才需要填；純主角套組留空")]
    public string HeroineID;

    // Apply 當下取得的結果，供 ReportChanges 回報（Apply → ReportChanges 是同步接續呼叫）
    private List<ValueChangeRecord> _lastRecords;

    public override void Apply(AdventureContext ctx)
    {
        _lastRecords = null;
        if (string.IsNullOrEmpty(PackageID)) return;

        var service = GameStatusService.Instance != null
            ? GameStatusService.Instance.StatChangeService
            : null;

        if (service == null)
        {
            Debug.LogWarning($"[AdvStatPackageEffect] 找不到 StatChangeService，跳過套組 '{PackageID}'");
            return;
        }

        _lastRecords = service.Apply(PackageID, string.IsNullOrEmpty(HeroineID) ? null : HeroineID);
    }

    public override IEnumerable<AdventureChangeRecord> ReportChanges(AdventureContext ctx)
    {
        if (_lastRecords == null) yield break;
        foreach (var r in _lastRecords)
            yield return new AdventureChangeRecord(r.resourceTypeKey, r.finalAmount);
    }
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
