using System;
using UnityEngine;

// ==========================================================
// 共用：進度解鎖系統的枚舉與資料結構
// 由 HeroineUnlockManager 與 SuspicionGateManager 共同使用
// ==========================================================

/// <summary>
/// 當條件達成時，要對 Progress 做甚麼操作
/// </summary>
public enum ProgressActionType
{
    /// <summary> 打開布林旗標 (AddPersistentFlag) </summary>
    SetFlagOn,
    /// <summary> 關閉布林旗標 (RemoveFlag) </summary>
    SetFlagOff,
    /// <summary> 設定數值變數 (SetValue) </summary>
    SetValue,
    /// <summary>
    /// 只作為條件使用，不對 Progress 做任何操作。
    /// 適合只想被 RequirementBlocker 引用當 UI 阻擋條件，不想污染 Flag 池的情境。
    /// HeroineUnlockManager 會跳過執行此類規則。
    /// </summary>
    OnlyCondition
}

/// <summary>
/// 比較運算子 (主要給 Suspicion 這類要判斷「低於/高於」閾值的場合使用)
/// </summary>
public enum ComparisonOp
{
    /// <summary> >= 大於等於 </summary>
    GreaterOrEqual,
    /// <summary> > 大於 </summary>
    Greater,
    /// <summary> <= 小於等於 </summary>
    LessOrEqual,
    /// <summary> < 小於 </summary>
    Less,
    /// <summary> == 等於 </summary>
    Equal
}

/// <summary>
/// 女主角解鎖規則的子類型：決定規則要看哪些數值
/// </summary>
public enum HeroineUnlockConditionType
{
    /// <summary> 只看 LewdnessLevel </summary>
    LewdnessOnly,
    /// <summary> 只看 BaseAffinityLevel </summary>
    AffinityOnly,
    /// <summary> 兩者都要達成 </summary>
    BothRequired
}
