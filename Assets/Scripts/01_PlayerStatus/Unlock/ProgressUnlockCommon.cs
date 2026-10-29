using System;
using UnityEngine;

// ==========================================================
// 共用：進度解鎖系統的枚舉與資料結構
// 由 ProgressUnlockManager 與 RequirementBlocker 共同使用
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
    /// ProgressUnlockManager 會跳過執行此類規則。
    /// </summary>
    OnlyCondition
}

/// <summary>
/// 比較運算子。
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
/// 解鎖條件要檢查的數值類型。
/// Libido / Trust / HCount / Affinity / Mood 屬於女主角，需搭配 heroineID；
/// RoomMessLevel 屬於主角，DayIndex 屬於時間系統，兩者都不需要 heroineID。
/// </summary>
public enum UnlockStatType
{
    // 保留現役項目的序列化編號；退役的 3～6、8 不再使用，避免舊資料被誤認。
    Libido = 0,
    Trust = 1,
    HCount = 2,
    RoomMessLevel = 7,
    Affinity = 9,
    Mood = 10,
    [InspectorName("DayIndex（天數 / Day X）")]
    DayIndex = 11
}

/// <summary>
/// 單一解鎖條件：「某數值 比較運算 閾值」。
/// 一條 Rule 可掛多個條件，全部達成 (All) 才算符合。
/// </summary>
[Serializable]
public class UnlockStatCondition
{
    [Tooltip(
        "要檢查的數值。\n" +
        "Libido / Trust / HCount / Affinity / Mood 查 Rule 指定的女主角；\n" +
        "RoomMessLevel 查主角，DayIndex 查目前遊戲天數（Day X）。"
    )]
    public UnlockStatType stat = UnlockStatType.Libido;

    [Tooltip("比較運算子。例如 DayIndex 想要「到第 X 天才解鎖」就選 GreaterOrEqual。")]
    public ComparisonOp op = ComparisonOp.GreaterOrEqual;

    [Tooltip("閾值")]
    public int threshold = 0;

    /// <summary> 此條件是否屬於女主角數值 (需要 Rule 上有 heroineID) </summary>
    public bool IsHeroineStat => ProgressUnlockUtility.IsHeroineStat(stat);

    public bool IsProtagonistStat => ProgressUnlockUtility.IsProtagonistStat(stat);
    public bool IsTimeStat => ProgressUnlockUtility.IsTimeStat(stat);

    /// <summary>
    /// 評估條件。所需的女主角、主角或時間 Model 為 null 時視為不達成。
    /// </summary>
    public bool IsMet(HeroineStatusModel heroine, ProtagonistStatusModel protagonist, TimeSystemModel time = null)
    {
        if (!ProgressUnlockUtility.TryGetStatValue(stat, heroine, protagonist, out int value, time))
            return false;
        return ProgressUnlockUtility.Compare(value, op, threshold);
    }

    /// <summary> 條件的文字摘要，例如 "Libido≥3" (Editor 視窗與 Debug 訊息用) </summary>
    public string ToSummary()
        => $"{stat}{ProgressUnlockUtility.GetOpSymbol(op)}{threshold}";
}

/// <summary>
/// 數值映射 (Sync / 鏡像)：讓某個 Progress Value 變數「永遠等於」某個數值的當下值。
/// 與門檻規則不同 —— 不判斷大小、不寫固定值，數值一變就把當前值忠實寫入 target。
///
/// 來源數值是女主角 (Libido / Trust / HCount / Affinity / Mood) 時需填 heroineID；
/// 來源是主角或時間數值時 heroineID 留空。
/// </summary>
[Serializable]
public class StatMirror
{
    [Tooltip("來源數值。女主角數值需搭配下方 heroineID；RoomMessLevel 與 DayIndex 忽略 heroineID。")]
    public UnlockStatType stat = UnlockStatType.Libido;

    [Tooltip("來源為女主角數值 (Libido / Trust / HCount / Affinity / Mood) 時必填；主角或時間數值留空。")]
    public string heroineID;

    [Tooltip("要忠實寫入當前值的 Progress Value 變數。")]
    public ProgressValueDefinition target;

    /// <summary> 此映射來源是否為女主角數值 (需要 heroineID) </summary>
    public bool IsHeroineStat => ProgressUnlockUtility.IsHeroineStat(stat);

    /// <summary> 映射的文字摘要，例如 "Libido → SisterLibido" (Debug 用) </summary>
    public string ToSummary()
        => $"{stat} → {(target != null ? target.FlagID : "(未設定)")}";
}

/// <summary>
/// 解鎖系統的共用小工具：數值取得 / 比較 / 顯示符號
/// </summary>
public static class ProgressUnlockUtility
{
    public static bool IsProtagonistStat(UnlockStatType stat) => stat == UnlockStatType.RoomMessLevel;
    public static bool IsTimeStat(UnlockStatType stat) => stat == UnlockStatType.DayIndex;

    /// <summary> 此數值類型是否屬於女主角 (HeroineStatusModel) </summary>
    public static bool IsHeroineStat(UnlockStatType stat)
    {
        return stat == UnlockStatType.Libido
            || stat == UnlockStatType.Trust
            || stat == UnlockStatType.Mood
            || stat == UnlockStatType.Affinity
            || stat == UnlockStatType.HCount;
    }

    /// <summary>
    /// 取出指定數值的當前值。對應的 Model 為 null 時回傳 false。
    /// </summary>
    public static bool TryGetStatValue(
        UnlockStatType stat,
        HeroineStatusModel heroine,
        ProtagonistStatusModel protagonist,
        out int value,
        TimeSystemModel time = null)
    {
        value = 0;

        switch (stat)
        {
            case UnlockStatType.Libido:
                if (heroine == null) return false;
                value = heroine.Libido;
                return true;
            case UnlockStatType.Trust:
                if (heroine == null) return false;
                value = heroine.Trust;
                return true;
            case UnlockStatType.Mood:
                if (heroine == null) return false;
                value = heroine.Mood;
                return true;
            case UnlockStatType.Affinity:
                if (heroine == null) return false;
                value = heroine.Affinity;
                return true;
            case UnlockStatType.HCount:
                if (heroine == null) return false;
                value = heroine.HCount;
                return true;

            case UnlockStatType.RoomMessLevel:
                if (protagonist == null) return false;
                value = protagonist.RoomMessLevel;
                return true;
            case UnlockStatType.DayIndex:
                if (time == null) return false;
                value = time.DayIndex;
                return true;

            default:
                return false;
        }
    }

    /// <summary> 依運算子比較 value 與 threshold </summary>
    public static bool Compare(int value, ComparisonOp op, int threshold)
    {
        switch (op)
        {
            case ComparisonOp.GreaterOrEqual: return value >= threshold;
            case ComparisonOp.Greater: return value > threshold;
            case ComparisonOp.LessOrEqual: return value <= threshold;
            case ComparisonOp.Less: return value < threshold;
            case ComparisonOp.Equal: return value == threshold;
            default: return false;
        }
    }

    /// <summary> 運算子的顯示符號 (Editor 視窗與摘要文字用) </summary>
    public static string GetOpSymbol(ComparisonOp op)
    {
        switch (op)
        {
            case ComparisonOp.GreaterOrEqual: return "≥";
            case ComparisonOp.Greater: return ">";
            case ComparisonOp.LessOrEqual: return "≤";
            case ComparisonOp.Less: return "<";
            case ComparisonOp.Equal: return "=";
            default: return "?";
        }
    }
}
