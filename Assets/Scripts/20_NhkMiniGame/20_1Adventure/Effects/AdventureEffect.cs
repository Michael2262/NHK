using System;
using System.Collections.Generic;

/// <summary>
/// 所有「大冒險翻牌結果效果」的抽象基底。
/// 使用 [SerializeReference] 內嵌在 AdventureCardData 中，不需要獨立 SO 資產。
/// 完全獨立於商店的 ItemEffect —— 之後要擴充大冒險專屬效果，繼承這個就好。
/// </summary>
[Serializable]
public abstract class AdventureEffect
{
    /// <summary>執行效果。</summary>
    public abstract void Apply(AdventureContext ctx);

    /// <summary>
    /// 回報此效果造成的數值變動（單筆），供 UI 產生「壓力+10」之類的提示。
    /// 回傳 null 表示不需要顯示（例如結束大冒險、設 Flag 等）。
    /// </summary>
    public virtual AdventureChangeRecord? ReportChange(AdventureContext ctx) => null;

    /// <summary>
    /// 回報此效果造成的數值變動（可多筆）。
    /// 預設把單筆的 ReportChange 包成序列；一次會產生多筆變動的效果（例如數值套組）覆寫這支。
    /// </summary>
    public virtual IEnumerable<AdventureChangeRecord> ReportChanges(AdventureContext ctx)
    {
        var record = ReportChange(ctx);
        if (record.HasValue) yield return record.Value;
    }
}

/// <summary>
/// 單筆數值變動記錄。LabelKey 是多語系 Text Table 的 Key，Amount 為帶正負號的變動量。
/// </summary>
public struct AdventureChangeRecord
{
    public string LabelKey;
    public int Amount;

    public AdventureChangeRecord(string labelKey, int amount)
    {
        LabelKey = labelKey;
        Amount = amount;
    }
}
