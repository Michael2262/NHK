using UnityEngine;

/// <summary>
/// 舊效果的相容性標記；購得效果不接受需要女主角目標的舊效果。
/// </summary>
public enum EffectTarget
{
    Protagonist,
    Heroine,
    Any
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

    /// <summary>已退役效果不提供新增，也不得放入購得效果執行。</summary>
    public virtual bool IsRetired => false;

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
