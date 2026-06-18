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
/// 比較運算子 (Stress 這類要判斷「低於閾值才達成」的場合特別需要)
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
/// 前三項屬於女主角 (HeroineStatusModel，需搭配 Rule 上的 heroineID)；
/// 後四項屬於主角 (ProtagonistStatusModel)，與 heroineID 無關。
/// </summary>
public enum UnlockStatType
{
    // ───── 女主角數值 ─────
    Libido,
    Trust,
    HCount,

    // ───── 主角數值 ─────
    Stress,
    LifePower,
    Sociality,
    Dependency,
    RoomMessLevel
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
        "Libido / Trust / HCount 查 Rule 指定的女主角；\n" +
        "Stress / LifePower / Sociality / Dependency 查主角。"
    )]
    public UnlockStatType stat = UnlockStatType.Libido;

    [Tooltip("比較運算子。例如 Stress 想要「低於門檻才解鎖」就選 Less / LessOrEqual。")]
    public ComparisonOp op = ComparisonOp.GreaterOrEqual;

    [Tooltip("閾值")]
    public int threshold = 0;

    /// <summary> 此條件是否屬於女主角數值 (需要 Rule 上有 heroineID) </summary>
    public bool IsHeroineStat => ProgressUnlockUtility.IsHeroineStat(stat);

    /// <summary>
    /// 評估條件。需要的 Model 為 null 時 (女主角條件缺 heroine、主角條件缺 protagonist) 視為不達成。
    /// </summary>
    public bool IsMet(HeroineStatusModel heroine, ProtagonistStatusModel protagonist)
    {
        if (!ProgressUnlockUtility.TryGetStatValue(stat, heroine, protagonist, out int value))
            return false;
        return ProgressUnlockUtility.Compare(value, op, threshold);
    }

    /// <summary> 條件的文字摘要，例如 "Libido≥3" (Editor 視窗與 Debug 訊息用) </summary>
    public string ToSummary()
        => $"{stat}{ProgressUnlockUtility.GetOpSymbol(op)}{threshold}";
}

/// <summary>
/// 解鎖系統的共用小工具：數值取得 / 比較 / 顯示符號
/// </summary>
public static class ProgressUnlockUtility
{
    /// <summary> 此數值類型是否屬於女主角 (HeroineStatusModel) </summary>
    public static bool IsHeroineStat(UnlockStatType stat)
    {
        return stat == UnlockStatType.Libido
            || stat == UnlockStatType.Trust
            || stat == UnlockStatType.HCount;
    }

    /// <summary>
    /// 取出指定數值的當前值。對應的 Model 為 null 時回傳 false。
    /// </summary>
    public static bool TryGetStatValue(
        UnlockStatType stat,
        HeroineStatusModel heroine,
        ProtagonistStatusModel protagonist,
        out int value)
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
            case UnlockStatType.HCount:
                if (heroine == null) return false;
                value = heroine.HCount;
                return true;

            case UnlockStatType.Stress:
                if (protagonist == null) return false;
                value = protagonist.Stress;
                return true;
            case UnlockStatType.LifePower:
                if (protagonist == null) return false;
                value = protagonist.LifePower;
                return true;
            case UnlockStatType.Sociality:
                if (protagonist == null) return false;
                value = protagonist.Sociality;
                return true;
            case UnlockStatType.Dependency:
                if (protagonist == null) return false;
                value = protagonist.Dependency;
                return true;
            case UnlockStatType.RoomMessLevel:
                if (protagonist == null) return false;
                value = protagonist.RoomMessLevel;
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
