
/// <summary>
/// 統一的主角數值分級。四項核心數值共用同一個 enum，
/// 各自的門檻可獨立調整，但預設一致。
/// </summary>

public enum StatusGrade
{
    Low,      // 預設 0～49
    Medium,   // 預設 50～79
    High,     // 預設 80～99
    Extreme   // 預設 100
}
