using UnityEngine;

/// <summary>
/// 標記這個效果適用於哪種目標，供 Editor 下拉選單篩選用。
/// </summary>
public enum EffectTarget
{
    Protagonist,  // 主角效果（顯示在 SelfEffects）
    Heroine,      // 女主角效果（顯示在 GiftEffects）
    Any           // 兩邊都顯示
}

/// <summary>
/// 所有道具效果的抽象基礎類別。
/// 使用 [SerializeReference] 內嵌在 ItemConfigData 中，不需要建立獨立的 SO 資產。
/// </summary>
[System.Serializable]
public abstract class ItemEffect
{
    /// <summary>
    /// 標記此效果適用的目標類型，供 Editor 篩選下拉選單用。
    /// 子類覆寫此屬性即可。
    /// </summary>
    public virtual EffectTarget Target => EffectTarget.Any;

    /// <summary>
    /// 執行效果。
    /// </summary>
    public abstract void Apply(EffectContext ctx);

    /// <summary>
    /// 回報此效果造成的數值變動，供 ValueChangeReporter 產生提示。
    /// 回傳 null 表示此效果不需要顯示報告（例如開 Flag、設定狀態等）。
    /// 子類覆寫此方法即可。
    /// </summary>
    public virtual ValueChangeRecord? ReportChange(EffectContext ctx) => null;
}