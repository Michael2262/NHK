using UnityEngine;

public enum TriggerBehavior
{
    Permanent,    // 滿足就開，不滿足也不關（但讀檔會補開）
    Toggle,       // 滿足就開，不滿足就關（回落機制）
    FireAndForget // 滿足的那一刻開一次，之後不管（手動關掉後，讀檔不會補開）
}

public enum CompareMode
{
    GreaterOrEqual, // >= (原有行為，適合連續數值如 Lewdness)
    Equal,          // == (適合離散值如 Phase Index)
    LessOrEqual,    // <=
    NotEqual        // != (未來可能用到)
}

[System.Serializable]
public class StatTriggerRule
{
    [Tooltip("比較模式：>=、==、<=、!=")]
    public CompareMode Compare = CompareMode.GreaterOrEqual;

    [Tooltip("門檻值")]
    public int Threshold;

    [Tooltip("目標 Flag 或 Value SO")]
    public ProgressBaseDefinition ProgressSO;

    public TriggerBehavior Behavior;

    [Header("數值設定 (僅在目標為 Value SO 時有效)")]
    public int TargetValue = 1;

    /// <summary>
    /// 根據 CompareMode 判斷是否滿足條件
    /// </summary>
    public bool Evaluate(int currentValue)
    {
        return Compare switch
        {
            CompareMode.GreaterOrEqual => currentValue >= Threshold,
            CompareMode.Equal => currentValue == Threshold,
            CompareMode.LessOrEqual => currentValue <= Threshold,
            CompareMode.NotEqual => currentValue != Threshold,
            _ => false
        };
    }
}